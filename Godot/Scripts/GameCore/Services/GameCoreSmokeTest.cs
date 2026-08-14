using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class GameCoreSmokeTestResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public List<string> Steps { get; set; } = new();
}

public static class GameCoreSmokeTest
{
    public static GameCoreSmokeTestResult Run()
    {
        var result = new GameCoreSmokeTestResult();
        var currentStep = "Initialize services";
        var smokeSaveCreated = false;

        try
        {
            var context = new GameCoreContext();
            var bootstrap = new LeagueBootstrapService(context);
            var dashboardService = new DashboardService(context);
            var rosterService = new RosterService(context);
            var depthChartService = new DepthChartService(context);
            var continueService = new ContinueService(context);
            var gameDayService = new GameDayService(context);
            var standingsService = new StandingsService(context);
            var scheduleService = new ScheduleService(context);
            var saveService = new GameCoreSaveService();
            const string smokeSaveName = "native_smoke_test_save.json";

            currentStep = "Bootstrap league";
            var league = bootstrap.CreateTestLeague();
            Require(league != null && context.ActiveLeague != null && league.Teams.Count == LeagueBootstrapService.TeamCount, $"Bootstrap should create {LeagueBootstrapService.TeamCount} teams.");
            Require(league.Results.Count == 0, "Fresh bootstrap should not seed completed results.");
            Require(league.Schedule.Count == LeagueBootstrapService.ExpectedScheduleGameCount, $"Expected a full native schedule with {LeagueBootstrapService.ExpectedScheduleGameCount} games, got {league.Schedule.Count}.");
            Require(league.Schedule.Count(game => string.Equals(game.GameType, "preseason", StringComparison.OrdinalIgnoreCase)) == LeagueBootstrapService.PreseasonWeeks * LeagueBootstrapService.PreseasonGamesPerWeek, "Unexpected preseason game count.");
            Require(league.Schedule.Count(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase)) == LeagueBootstrapService.RegularSeasonGameCount, $"Expected {LeagueBootstrapService.RegularSeasonGameCount} regular-season games.");
            Require(!league.Schedule.Any(game => string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)), "Fresh bootstrap should not mark any scheduled game final.");
            var preseasonGame = league.Schedule.FirstOrDefault(game => string.Equals(game.GameType, "preseason", StringComparison.OrdinalIgnoreCase));
            var firstRegularSeasonGame = league.Schedule.FirstOrDefault(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase));
            Require(preseasonGame != null && preseasonGame.AbsoluteWeek == 1 && preseasonGame.PhaseWeek == 1, "Preseason should begin at absolute week 1 / phase week 1.");
            Require(firstRegularSeasonGame != null, "Missing first regular-season game.");
            Require(firstRegularSeasonGame.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek, $"Regular season should start at absolute week {LeagueBootstrapService.RegularSeasonStartWeek}, got {firstRegularSeasonGame.AbsoluteWeek}.");
            Require(firstRegularSeasonGame.PhaseWeek == 1, $"First regular-season game should display as week 1, got {firstRegularSeasonGame.PhaseWeek}.");
            Require(string.Equals(firstRegularSeasonGame.WeekLabel, "Regular Season Week 1", StringComparison.Ordinal), $"Unexpected first regular-season label: {firstRegularSeasonGame.WeekLabel}");
            ValidateLeagueScheduleStructure(league, scheduleService);
            Pass(result, currentStep);

            currentStep = "Dashboard state";
            var dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.Team.Name.Length > 0, "Dashboard missing team name.");
            Require(dashboard.Dashboard.Calendar.Week > 0, "Dashboard missing calendar week.");
            Require(dashboard.Dashboard.Calendar.AbsoluteWeek == 1, "Dashboard missing absolute week.");
            Require(dashboard.Dashboard.Calendar.PhaseWeek == 1, "Dashboard missing phase week.");
            Require(!string.IsNullOrWhiteSpace(dashboard.Dashboard.Calendar.CurrentDate), "Dashboard missing current date.");
            Require(!string.IsNullOrWhiteSpace(dashboard.Dashboard.Calendar.WeekLabel), "Dashboard missing week label.");
            Require(string.Equals(dashboard.Dashboard.Calendar.WeekLabel, "Week 1 - Preseason", StringComparison.Ordinal), $"Unexpected starting calendar label: {dashboard.Dashboard.Calendar.WeekLabel}");
            Require(string.Equals(dashboard.Dashboard.Team.Record, "0-0", StringComparison.OrdinalIgnoreCase), $"Expected clean record, got {dashboard.Dashboard.Team.Record}.");
            Require(dashboard.Dashboard.RecentResults.Count == 0, "Fresh dashboard should not show recent results.");
            Require(dashboard.Dashboard.PlayoffBracket != null, "Dashboard should always expose a playoff bracket DTO.");
            Require(dashboard.Dashboard.PlayoffBracket.ConferenceBrackets.Count == 0, "Fresh dashboard should not expose postseason seeds before bracket generation.");
            Require(string.Equals(dashboard.Dashboard.PlayoffSummaryText, "Playoff bracket not generated yet.", StringComparison.Ordinal), $"Unexpected pre-postseason playoff summary: {dashboard.Dashboard.PlayoffSummaryText}");
            Require(dashboard.Dashboard.NextGame.GameId.Length > 0, "Fresh dashboard missing next game.");
            Require(string.Equals(dashboard.Dashboard.NextGame.WeekLabel, "Preseason Week 1", StringComparison.Ordinal), $"Unexpected next-game label: {dashboard.Dashboard.NextGame.WeekLabel}");
            Require(dashboard.Dashboard.TeamStatus.RosterSize == 53, $"Expected 53-man roster, got {dashboard.Dashboard.TeamStatus.RosterSize}.");
            Require(dashboard.Dashboard.TeamStatus.Injuries == 0, "Fresh roster should not include injuries.");
            Pass(result, currentStep);

            currentStep = "History starts empty";
            ValidateEmptySeasonHistory(context.ActiveLeague, dashboardService);
            Pass(result, currentStep);

            currentStep = "Standings start clean";
            var standings = standingsService.GetStandings();
            Require(standings.Ok, standings.Error);
            Require(standings.Standings.Count == league.Teams.Count, "Standings missing teams.");
            Require(standings.Standings.All(row =>
                row.Wins == 0
                && row.Losses == 0
                && row.Ties == 0
                && row.PointsFor == 0
                && row.PointsAgainst == 0), "Fresh standings should be 0-0-0 with zero PF/PA.");
            Pass(result, currentStep);

            currentStep = "Roster state";
            var roster = rosterService.GetTeamRoster();
            Require(roster.Ok, roster.Error);
            Require(roster.Players.Count == 53, $"Expected 53 rostered players, got {roster.Players.Count}.");
            Require(roster.RosterStatus != null && roster.RosterStatus.IsValid, "Fresh roster should be valid.");
            Require(roster.RosterStatus.RequiredCuts == 0, "Fresh roster should not require cuts.");
            Require(roster.RosterStatus.OpenSlots == 0, "Fresh roster should not have open slots.");
            var firstKnownPositions = roster.Players
                .Select(player => player.Position)
                .Where(position => !string.IsNullOrWhiteSpace(position))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();
            Require(firstKnownPositions.Length >= 4, "Roster ordering did not expose enough positions.");
            Require(string.Equals(firstKnownPositions[0], "QB", StringComparison.OrdinalIgnoreCase), $"Expected roster to begin with QB, got {firstKnownPositions[0]}.");
            Require(string.Equals(firstKnownPositions[1], "RB", StringComparison.OrdinalIgnoreCase), $"Expected roster to list RB after QB, got {firstKnownPositions[1]}.");
            Require(string.Equals(firstKnownPositions[2], "WR", StringComparison.OrdinalIgnoreCase) || string.Equals(firstKnownPositions[2], "FB", StringComparison.OrdinalIgnoreCase), $"Unexpected third roster position {firstKnownPositions[2]}.");
            Pass(result, currentStep);

            currentStep = "Depth chart state";
            var depthChart = depthChartService.GetTeamDepthChart();
            Require(depthChart.Ok, depthChart.Error);
            Require(depthChart.Positions.Count > 0, "Depth chart snapshot missing positions.");
            Require(depthChart.DepthChartStatus != null && depthChart.DepthChartStatus.IsValid, "Fresh depth chart should start valid.");
            Pass(result, currentStep);

            currentStep = "Auto-fill depth chart";
            var filledDepthChart = depthChartService.AutoFillDepthChart();
            Require(filledDepthChart.Ok, filledDepthChart.Error);
            Require(filledDepthChart.DepthChartStatus != null && filledDepthChart.DepthChartStatus.IsValid, "Auto-filled depth chart is not valid.");
            Pass(result, currentStep);

            currentStep = "Sim Until behavior";
            ValidateSimUntilBehavior();
            Pass(result, currentStep);

            currentStep = "Continue to game_day";
            var continueResult = continueService.Continue(14);
            Require(continueResult.Ok, continueResult.Error);
            Require(string.Equals(continueResult.Result.StopReason, "game_day", StringComparison.OrdinalIgnoreCase), $"Stopped at {continueResult.Result.StopReason} instead of game_day.");
            Pass(result, currentStep);

            currentStep = "Game day state";
            var gameDay = gameDayService.GetCurrentGameDayState();
            Require(gameDay.Ok, gameDay.Error);
            Require(gameDay.Game.GameId.Length > 0, "Game day state missing current game.");
            Pass(result, currentStep);

            currentStep = "Sim current game";
            var simulated = gameDayService.SimulateCurrentUserGame(gameDay.Game.GameId);
            Require(simulated.Ok, simulated.Error);
            Require(simulated.Result.BoxScore.Count > 0, "Simulated game missing box score.");
            Require(string.Equals(simulated.Result.WeekLabel, "Preseason Week 1", StringComparison.Ordinal), $"Unexpected first result label: {simulated.Result.WeekLabel}");
            Require(string.Equals(simulated.Result.Phase, "Preseason", StringComparison.Ordinal), $"Unexpected first result phase: {simulated.Result.Phase}");
            Pass(result, currentStep);

            currentStep = "Schedule final status";
            var schedule = scheduleService.GetTeamSchedule();
            Require(schedule.Ok, schedule.Error);
            Require(schedule.Schedule.Count == LeagueBootstrapService.PreseasonWeeks + LeagueBootstrapService.RegularSeasonGamesPerTeam, $"User schedule should contain 20 total games, got {schedule.Schedule.Count}.");
            Require(schedule.Schedule.Any(game => string.Equals(game.GameType, "preseason", StringComparison.OrdinalIgnoreCase) && game.Week == 1 && game.AbsoluteWeek == 1), "Schedule should expose preseason week 1.");
            Require(schedule.Schedule.Any(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase) && game.Week == 1 && game.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek), $"Schedule should expose regular season week 1 at absolute week {LeagueBootstrapService.RegularSeasonStartWeek}.");
            Require(schedule.Schedule.Any(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase) && game.Week == LeagueBootstrapService.RegularSeasonWeeks && game.AbsoluteWeek == LeagueBootstrapService.TotalSeasonWeeks && string.Equals(game.WeekLabel, "Regular Season Week 18", StringComparison.Ordinal)), "Schedule should expose a correctly labeled regular-season week 18.");
            Require(schedule.Schedule.Any(game =>
                string.Equals(game.GameId, gameDay.Game.GameId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)), "Current game did not update to final on the schedule.");
            Pass(result, currentStep);

            currentStep = "Preseason standings ignored";
            standings = standingsService.GetStandings();
            Require(standings.Ok, standings.Error);
            var userStanding = standings.Standings.FirstOrDefault(row => string.Equals(row.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
            Require(userStanding != null, "User team missing from standings.");
            Require(userStanding.PointsFor == 0 && userStanding.PointsAgainst == 0, "Preseason should not affect regular-season PF/PA.");
            Require(userStanding.Wins == 0 && userStanding.Losses == 0 && userStanding.Ties == 0, "Preseason should not affect regular-season standings.");
            Pass(result, currentStep);

            currentStep = "Dashboard recent results";
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.RecentResults.Any(game => string.Equals(game.GameId, gameDay.Game.GameId, StringComparison.OrdinalIgnoreCase)), "Dashboard recent results missing completed game.");
            Pass(result, currentStep);

            currentStep = "Duplicate simulation stays idempotent";
            var resultsBeforeDuplicateSim = context.ActiveLeague.Results.Count;
            var duplicateSimulation = gameDayService.SimulateScheduledGame(gameDay.Game.GameId, allowUserTeamGame: true);
            Require(duplicateSimulation.Ok, duplicateSimulation.Error);
            Require(context.ActiveLeague.Results.Count == resultsBeforeDuplicateSim, "Simulating the same completed game twice should not create duplicate results.");
            Pass(result, currentStep);

            currentStep = "Finish preseason weeks";
            SimCurrentAbsoluteWeek(context, continueService, gameDayService, 1);
            SimCurrentAbsoluteWeek(context, continueService, gameDayService, 2);
            SimCurrentAbsoluteWeek(context, continueService, gameDayService, 3);
            Require(context.ActiveLeague.Results.Count(resultEntry => string.Equals(resultEntry.GameType, "preseason", StringComparison.OrdinalIgnoreCase)) == LeagueBootstrapService.PreseasonWeeks * LeagueBootstrapService.PreseasonGamesPerWeek, "Expected all preseason games to complete once.");
            standings = standingsService.GetStandings();
            Require(standings.Ok, standings.Error);
            Require(standings.Standings.All(row =>
                row.Wins == 0
                && row.Losses == 0
                && row.Ties == 0
                && row.PointsFor == 0
                && row.PointsAgainst == 0), "Preseason completion should still leave regular-season standings at 0-0-0.");
            Pass(result, currentStep);

            currentStep = "Advance through transition bye";
            Require(context.ActiveLeague.Calendar.AbsoluteWeek == LeagueBootstrapService.PreseasonWeeks + 1, $"Expected transition bye at absolute week {LeagueBootstrapService.PreseasonWeeks + 1}, got {context.ActiveLeague.Calendar.AbsoluteWeek}.");
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(string.Equals(dashboard.Dashboard.Calendar.Phase, "Preseason Bye", StringComparison.OrdinalIgnoreCase), $"Expected preseason bye phase, got {dashboard.Dashboard.Calendar.Phase}.");
            Require(string.Equals(dashboard.Dashboard.Calendar.WeekLabel, "Week 4 - Preseason Bye", StringComparison.Ordinal), $"Unexpected transition-bye label: {dashboard.Dashboard.Calendar.WeekLabel}");
            var resultsBeforeBye = context.ActiveLeague.Results.Count;
            AdvanceUntilAbsoluteWeek(context, continueService, gameDayService, LeagueBootstrapService.RegularSeasonStartWeek);
            Require(context.ActiveLeague.Results.Count == resultsBeforeBye, "Transition bye should not create fake results.");
            Require(!context.ActiveLeague.Results.Any(resultEntry => resultEntry.AbsoluteWeek == LeagueBootstrapService.PreseasonWeeks + 1), "Transition bye should not produce week 4 results.");
            Pass(result, currentStep);

            currentStep = "Reach regular season week 1";
            Require(context.ActiveLeague.Calendar.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek, $"Expected absolute week {LeagueBootstrapService.RegularSeasonStartWeek}, got {context.ActiveLeague.Calendar.AbsoluteWeek}.");

            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(string.Equals(context.ActiveLeague.Calendar.Phase, "Regular Season", StringComparison.OrdinalIgnoreCase), $"Expected regular season, got {context.ActiveLeague.Calendar.Phase}.");
            Require(context.ActiveLeague.Calendar.PhaseWeek == 1, $"Expected regular season week 1, got {context.ActiveLeague.Calendar.PhaseWeek}.");
            Require(context.ActiveLeague.Calendar.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek, $"Expected absolute week {LeagueBootstrapService.RegularSeasonStartWeek}, got {context.ActiveLeague.Calendar.AbsoluteWeek}.");
            Require(string.Equals(dashboard.Dashboard.Calendar.WeekLabel, "Week 1 - Regular Season", StringComparison.Ordinal), $"Unexpected regular-season calendar label: {dashboard.Dashboard.Calendar.WeekLabel}");
            if (dashboard.Dashboard.NextGame != null)
            {
                var nextGame = context.ActiveLeague.Schedule.FirstOrDefault(game =>
                    string.Equals(game.GameId, dashboard.Dashboard.NextGame.GameId, StringComparison.OrdinalIgnoreCase));
                Require(nextGame != null, "Dashboard next game should exist in the schedule.");
                Require(dashboard.Dashboard.NextGame.Week == nextGame.PhaseWeek, "Next game should use phase-relative week numbering.");
                Require(dashboard.Dashboard.NextGame.AbsoluteWeek == nextGame.AbsoluteWeek, "Dashboard next game absolute week is inconsistent.");
                Require(string.Equals(dashboard.Dashboard.NextGame.WeekLabel, nextGame.WeekLabel, StringComparison.Ordinal), "Dashboard next game label is inconsistent.");
            }
            Pass(result, currentStep);

            currentStep = "Sim first regular-season week";
            SimCurrentAbsoluteWeek(context, continueService, gameDayService, LeagueBootstrapService.RegularSeasonStartWeek);
            standings = standingsService.GetStandings();
            Require(standings.Ok, standings.Error);
            userStanding = standings.Standings.FirstOrDefault(row => string.Equals(row.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
            Require(userStanding != null, "User team missing after regular-season week 1.");
            Require(userStanding.Wins + userStanding.Losses + userStanding.Ties == 1, "First regular-season week should count exactly one game for the user team.");
            ValidateSimulatedResultLabels(context.ActiveLeague);
            Pass(result, currentStep);

            currentStep = "Sim full regular season";
            SimRegularSeasonThroughCompletion(context, continueService, gameDayService);
            Require(context.ActiveLeague.Results.Select(entry => entry.GameId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == context.ActiveLeague.Results.Count, "Completed results should not contain duplicate game ids.");
            Require(context.ActiveLeague.Results.Count(resultEntry => string.Equals(resultEntry.GameType, "regular_season", StringComparison.OrdinalIgnoreCase)) == LeagueBootstrapService.RegularSeasonGameCount, $"Expected {LeagueBootstrapService.RegularSeasonGameCount} regular-season results.");
            ValidateFinalRegularSeasonStandings(context.ActiveLeague, standingsService);
            Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase)
                || string.Equals(context.ActiveLeague.Calendar.Phase, "Offseason", StringComparison.OrdinalIgnoreCase), $"Expected a safe post-regular-season phase, got {context.ActiveLeague.Calendar.Phase}.");
            Require(string.Equals(context.ActiveLeague.Calendar.WeekLabel, ScheduleService.PostseasonPendingWeekLabel, StringComparison.Ordinal)
                || string.Equals(context.ActiveLeague.Calendar.Phase, "Offseason", StringComparison.OrdinalIgnoreCase), $"Unexpected post-regular-season week label: {context.ActiveLeague.Calendar.WeekLabel}");
            ValidatePlayoffBracket(context.ActiveLeague);
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.PlayoffBracket != null, "Dashboard should expose playoff bracket DTO at postseason pending.");
            Require(dashboard.Dashboard.PlayoffBracket.ConferenceBrackets.Count == 2, $"Expected 2 conference brackets in dashboard DTO, got {dashboard.Dashboard.PlayoffBracket.ConferenceBrackets.Count}.");
            Require(dashboard.Dashboard.PlayoffBracket.ConferenceBrackets.All(entry => entry.Seeds.Count == 7), "Dashboard DTO should expose 7 seeds per conference.");
            Require(dashboard.Dashboard.PlayoffBracket.ConferenceBrackets.All(entry =>
                entry.Rounds.Count == 1
                && entry.Rounds[0].Games.Count == 3), "Dashboard DTO should expose 3 wild card games per conference.");
            Require(!string.IsNullOrWhiteSpace(dashboard.Dashboard.PlayoffSummaryText), "Dashboard should expose non-empty playoff summary text at postseason pending.");
            Require(!string.Equals(dashboard.Dashboard.PlayoffSummaryText, "Playoff bracket not generated yet.", StringComparison.Ordinal), "Dashboard should not expose fallback playoff summary once the bracket exists.");
            Require(dashboard.Dashboard.NextGame != null, "Dashboard should expose next-game header labels at postseason pending.");
            Require(!string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Week 0 vs TBD", StringComparison.Ordinal), "Postseason pending header should not show Week 0 vs TBD.");
            Require(
                string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Playoffs Pending", StringComparison.Ordinal)
                || string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Wild Card Round", StringComparison.Ordinal)
                || string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Wild Card Bye", StringComparison.Ordinal),
                $"Unexpected postseason next header label: {dashboard.Dashboard.NextGame.HeaderNextLabel}");
            Require(!string.IsNullOrWhiteSpace(dashboard.Dashboard.NextGame.HeaderOpponentLabel), "Postseason pending header should expose an opponent label.");
            var availableWeekKeys = context.ActiveLeague.Results
                .Select(resultEntry => $"{NormalizeResultsSeasonKey(resultEntry.GameType)}:{(resultEntry.AbsoluteWeek > 0 ? resultEntry.AbsoluteWeek : resultEntry.Week)}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var preferredWeekKey = DashboardController.GetPreferredResultsWeekKey(availableWeekKeys, availableWeekKeys);
            Require(string.Equals(preferredWeekKey, $"regular:{LeagueBootstrapService.TotalSeasonWeeks}", StringComparison.Ordinal), $"Unexpected preferred postseason results week key: {preferredWeekKey}");
            var preferredWeekLabel = context.ActiveLeague.Results
                .Where(resultEntry => string.Equals($"{NormalizeResultsSeasonKey(resultEntry.GameType)}:{(resultEntry.AbsoluteWeek > 0 ? resultEntry.AbsoluteWeek : resultEntry.Week)}", preferredWeekKey, StringComparison.OrdinalIgnoreCase))
                .Select(resultEntry => resultEntry.WeekLabel)
                .FirstOrDefault(label => !string.IsNullOrWhiteSpace(label));
            Require(string.Equals(preferredWeekLabel, "Regular Season Week 18", StringComparison.Ordinal), $"Unexpected preferred postseason results label: {preferredWeekLabel}");
            var postseasonAction = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, "postseason_pending", StringComparison.OrdinalIgnoreCase));
            Require(postseasonAction != null, "Dashboard should expose a postseason pending action item.");
            Require(string.Equals(postseasonAction.Title, "Action Required: Simulate the Wild Card round.", StringComparison.Ordinal), $"Unexpected postseason action title: {postseasonAction?.Title}");
            var savedBracketSnapshot = SnapshotBracket(context.ActiveLeague.PlayoffBracket);
            standings = standingsService.GetStandings();
            Require(standings.Ok, standings.Error);
            var standingsSnapshot = SnapshotRegularSeasonStandings(standings);
            var postseasonContinue = continueService.Continue();
            Require(postseasonContinue.Ok, postseasonContinue.Error);
            Require(string.Equals(postseasonContinue.Result.StopReason, PlayoffService.WildCardCompletedStopReason, StringComparison.OrdinalIgnoreCase), $"Expected {PlayoffService.WildCardCompletedStopReason} on Continue, got {postseasonContinue.Result.StopReason}.");
            Require(postseasonContinue.Result.GamesSimulated == 6, $"Expected 6 Wild Card games simulated, got {postseasonContinue.Result.GamesSimulated}.");
            ValidateWildCardResults(context.ActiveLeague, standingsService, standingsSnapshot);
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.PlayoffSummaryText.Contains("advance", StringComparison.OrdinalIgnoreCase), "Dashboard playoff summary should reflect completed Wild Card winners.");
            Require(dashboard.Dashboard.PlayoffSummaryText.Contains("Divisional Round", StringComparison.OrdinalIgnoreCase), "Dashboard playoff summary should include scheduled Divisional Round games after Wild Card completion.");
            Require(!string.IsNullOrWhiteSpace(dashboard.Dashboard.NextGame.HeaderOpponentLabel), "Post-Wild Card opponent header should remain populated.");
            Require(string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Divisional Round", StringComparison.Ordinal)
                || string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Divisional Round Pending", StringComparison.Ordinal)
                || string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Wild Card Bye", StringComparison.Ordinal), $"Unexpected post-Wild Card next header: {dashboard.Dashboard.NextGame.HeaderNextLabel}");
            postseasonAction = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, "postseason_pending", StringComparison.OrdinalIgnoreCase));
            Require(postseasonAction != null, "Dashboard should retain postseason action item after Wild Card completion.");
            Require(string.Equals(postseasonAction.Title, "Action Required: Simulate the Divisional round.", StringComparison.Ordinal), $"Unexpected post-Wild Card action title: {postseasonAction?.Title}");
            Require(!string.Equals(SnapshotBracket(context.ActiveLeague.PlayoffBracket), savedBracketSnapshot, StringComparison.Ordinal), "Wild Card simulation should change the playoff bracket snapshot.");
            savedBracketSnapshot = SnapshotBracket(context.ActiveLeague.PlayoffBracket);
            var duplicateWildCardRun = continueService.Continue();
            Require(duplicateWildCardRun.Ok, duplicateWildCardRun.Error);
            Require(string.Equals(duplicateWildCardRun.Result.StopReason, PlayoffService.DivisionalCompletedStopReason, StringComparison.OrdinalIgnoreCase), $"Expected repeat Continue to stop at {PlayoffService.DivisionalCompletedStopReason}, got {duplicateWildCardRun.Result.StopReason}.");
            Require(duplicateWildCardRun.Result.GamesSimulated == 4, "Second Continue should simulate the 4 Divisional games.");
            ValidateDivisionalResults(context.ActiveLeague, standingsService, standingsSnapshot);
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.PlayoffSummaryText.Contains("Conference Championship", StringComparison.OrdinalIgnoreCase), "Dashboard playoff summary should include scheduled Conference Championship games after Divisional completion.");
            Require(!string.IsNullOrWhiteSpace(dashboard.Dashboard.NextGame.HeaderOpponentLabel), "Post-Divisional opponent header should remain populated before Conference Championship.");
            Require(string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Conference Championship", StringComparison.Ordinal)
                || string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Conference Championship Pending", StringComparison.Ordinal), $"Unexpected post-Divisional next header before Conference Championship sim: {dashboard.Dashboard.NextGame.HeaderNextLabel}");
            postseasonAction = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, "postseason_pending", StringComparison.OrdinalIgnoreCase));
            Require(postseasonAction != null, "Dashboard should retain postseason action item after Divisional completion.");
            Require(string.Equals(postseasonAction.Title, "Action Required: Simulate the Conference Championship.", StringComparison.Ordinal), $"Unexpected post-Divisional action title before Conference Championship sim: {postseasonAction?.Title}");
            Require(!string.Equals(SnapshotBracket(context.ActiveLeague.PlayoffBracket), savedBracketSnapshot, StringComparison.Ordinal), "Divisional simulation should change the playoff bracket snapshot.");
            savedBracketSnapshot = SnapshotBracket(context.ActiveLeague.PlayoffBracket);
            var conferenceChampionshipRun = continueService.Continue();
            Require(conferenceChampionshipRun.Ok, conferenceChampionshipRun.Error);
            Require(string.Equals(conferenceChampionshipRun.Result.StopReason, PlayoffService.ConferenceChampionshipCompletedStopReason, StringComparison.OrdinalIgnoreCase), $"Expected {PlayoffService.ConferenceChampionshipCompletedStopReason} after Conference Championship sim, got {conferenceChampionshipRun.Result.StopReason}.");
            Require(conferenceChampionshipRun.Result.GamesSimulated == 2, "Third Continue should simulate the 2 Conference Championship games.");
            ValidateConferenceChampionshipResults(context.ActiveLeague, standingsService, standingsSnapshot);
            Require(!string.Equals(SnapshotBracket(context.ActiveLeague.PlayoffBracket), savedBracketSnapshot, StringComparison.Ordinal), "Conference Championship simulation should change the playoff bracket snapshot.");
            savedBracketSnapshot = SnapshotBracket(context.ActiveLeague.PlayoffBracket);
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.PlayoffSummaryText.Contains("League Championship", StringComparison.OrdinalIgnoreCase), "Dashboard playoff summary should include the scheduled League Championship after Conference Championship completion.");
            Require(string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: League Championship", StringComparison.Ordinal)
                || string.Equals(dashboard.Dashboard.NextGame.HeaderNextLabel, "Next: League Championship Pending", StringComparison.Ordinal), $"Unexpected post-Conference Championship next header: {dashboard.Dashboard.NextGame.HeaderNextLabel}");
            Require(string.Equals(dashboard.Dashboard.NextGame.HeaderOpponentLabel, "Next opponent: TBD", StringComparison.Ordinal), $"Unexpected post-Conference Championship opponent header: {dashboard.Dashboard.NextGame.HeaderOpponentLabel}");
            postseasonAction = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, "postseason_pending", StringComparison.OrdinalIgnoreCase));
            Require(postseasonAction != null, "Dashboard should retain postseason action item after Conference Championship completion.");
            Require(string.Equals(postseasonAction.Title, "Action Required: Simulate the League Championship.", StringComparison.Ordinal), $"Unexpected post-Conference Championship action title: {postseasonAction?.Title}");

            var leagueChampionshipRun = continueService.Continue();
            Require(leagueChampionshipRun.Ok, leagueChampionshipRun.Error);
            Require(string.Equals(leagueChampionshipRun.Result.StopReason, PlayoffService.LeagueChampionshipCompletedStopReason, StringComparison.OrdinalIgnoreCase), $"Expected {PlayoffService.LeagueChampionshipCompletedStopReason} after League Championship sim, got {leagueChampionshipRun.Result.StopReason}.");
            Require(leagueChampionshipRun.Result.GamesSimulated == 1, "Fourth Continue should simulate the 1 League Championship game.");
            ValidateLeagueChampionshipResults(context.ActiveLeague, standingsService, standingsSnapshot);
            Require(!string.Equals(SnapshotBracket(context.ActiveLeague.PlayoffBracket), savedBracketSnapshot, StringComparison.Ordinal), "League Championship simulation should change the playoff bracket snapshot.");
            savedBracketSnapshot = SnapshotBracket(context.ActiveLeague.PlayoffBracket);

            var duplicatePostseasonRun = continueService.ContinueUntil("playoffs_start");
            Require(duplicatePostseasonRun.Ok, duplicatePostseasonRun.Error);
            Require(string.Equals(duplicatePostseasonRun.Result.StopReason, "reached_playoffs", StringComparison.OrdinalIgnoreCase), $"Expected reached_playoffs on repeat Sim Until, got {duplicatePostseasonRun.Result.StopReason}.");
            Require(string.Equals(SnapshotBracket(context.ActiveLeague.PlayoffBracket), savedBracketSnapshot, StringComparison.Ordinal), "Repeat Sim Until should not overwrite the playoff bracket.");
            var offseasonAttempt = continueService.ContinueUntil("offseason_start");
            Require(offseasonAttempt.Ok, offseasonAttempt.Error);
            Require(string.Equals(offseasonAttempt.Result.StopReason, "reached_offseason", StringComparison.OrdinalIgnoreCase), $"Expected safe terminal offseason stop after League Championship, got {offseasonAttempt.Result.StopReason}.");
            Require(offseasonAttempt.Result.GamesSimulated == 0, "Offseason attempt after League Championship completion should not re-sim completed playoff games.");
            dashboard = dashboardService.GetDashboardState();
            Require(dashboard.Ok, dashboard.Error);
            Require(dashboard.Dashboard.PlayoffSummaryText.Contains("League Championship Results", StringComparison.OrdinalIgnoreCase), "Dashboard playoff summary should reflect completed League Championship results.");
            Require(dashboard.Dashboard.PlayoffSummaryText.Contains("League Champion:", StringComparison.OrdinalIgnoreCase), "Dashboard playoff summary should surface the league champion.");
            ValidateSeasonHistorySnapshot(context.ActiveLeague, dashboard.Dashboard);
            Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.OffseasonPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.OffseasonPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");
            var historyResponse = dashboardService.GetLeagueHistory();
            ValidateLeagueHistoryResponse(context.ActiveLeague, historyResponse);
            var championTeamId = context.ActiveLeague.PlayoffBracket.LeagueChampionRecord.ChampionTeamId;
            var runnerUpTeamId = context.ActiveLeague.PlayoffBracket.LeagueChampionRecord.RunnerUpTeamId;
            var scheduleCount = context.ActiveLeague.Schedule.Count;
            var regularSeasonResultCount = context.ActiveLeague.Results.Count(resultEntry => string.Equals(resultEntry.GameType, "regular_season", StringComparison.OrdinalIgnoreCase));
            var playoffResultCount = context.ActiveLeague.Results.Count(resultEntry => string.Equals(resultEntry.GameType, "playoffs", StringComparison.OrdinalIgnoreCase));
            var seasonHistoryCount = context.ActiveLeague.HistoricalSeasons.Count(record => record != null && record.SeasonYear == context.ActiveLeague.SeasonYear);
            var retirementHistoryCount = 0;
            var retiredPlayerCount = 0;
            ValidateOffseasonPlaceholderDashboard(dashboard.Dashboard, ScheduleService.OffseasonPendingPhase);
            ValidateOffseasonInvariants(context.ActiveLeague, championTeamId, runnerUpTeamId, seasonHistoryCount, scheduleCount, regularSeasonResultCount, playoffResultCount, retirementHistoryCount, retiredPlayerCount);

            currentStep = "Advance offseason placeholders";
            foreach (var expectedPhase in BuildExpectedOffseasonPlaceholderPhases().Skip(1))
            {
                var phaseAdvance = continueService.Continue();
                Require(phaseAdvance.Ok, phaseAdvance.Error);
                Require(string.Equals(phaseAdvance.Result.StopReason, ScheduleService.GetOffseasonPhaseKey(expectedPhase), StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.GetOffseasonPhaseKey(expectedPhase)} while advancing offseason placeholders, got {phaseAdvance.Result.StopReason}.");
                Require(phaseAdvance.Result.GamesSimulated == 0, $"Advancing to {expectedPhase} should not simulate games.");
                Require(string.Equals(context.ActiveLeague.Calendar.Phase, expectedPhase, StringComparison.OrdinalIgnoreCase), $"Expected offseason phase {expectedPhase}, got {context.ActiveLeague.Calendar.Phase}.");

                dashboard = dashboardService.GetDashboardState();
                Require(dashboard.Ok, dashboard.Error);
                ValidateOffseasonPlaceholderDashboard(dashboard.Dashboard, expectedPhase);

                if (string.Equals(expectedPhase, ScheduleService.RetirementPendingPhase, StringComparison.Ordinal))
                {
                    Require(RetirementService.GetSeasonRetirementRecord(context.ActiveLeague, context.ActiveLeague.SeasonYear) == null, "Retirements should not run before retirement pending is processed.");
                    retirementHistoryCount = 0;
                    retiredPlayerCount = 0;
                }
                else if (string.Equals(expectedPhase, ScheduleService.ExclusiveNegotiationPendingPhase, StringComparison.Ordinal))
                {
                    var seasonRetirements = RetirementService.GetSeasonRetirementRecord(context.ActiveLeague, context.ActiveLeague.SeasonYear);
                    Require(seasonRetirements != null && seasonRetirements.Completed, "Retirements should be generated while advancing out of retirement pending.");
                    retirementHistoryCount = 1;
                    retiredPlayerCount = seasonRetirements.RetiredCount;
                    ValidateRetirementResults(context.ActiveLeague, seasonRetirements);
                }
                else if (retirementHistoryCount > 0)
                {
                    var seasonRetirements = RetirementService.GetSeasonRetirementRecord(context.ActiveLeague, context.ActiveLeague.SeasonYear);
                    Require(seasonRetirements != null && seasonRetirements.Completed, "Retirement history should persist through later offseason phases.");
                    Require(seasonRetirements.RetiredCount == retiredPlayerCount, "Later offseason phases should not add more retirements for the same season.");
                    ValidateRetirementResults(context.ActiveLeague, seasonRetirements);
                }

                ValidateOffseasonInvariants(context.ActiveLeague, championTeamId, runnerUpTeamId, seasonHistoryCount, scheduleCount, regularSeasonResultCount, playoffResultCount, retirementHistoryCount, retiredPlayerCount);
            }
            Pass(result, currentStep);

            currentStep = "Training camp placeholder is terminal";
            var duplicateTrainingCampContinue = continueService.Continue();
            Require(duplicateTrainingCampContinue.Ok, duplicateTrainingCampContinue.Error);
            Require(string.Equals(duplicateTrainingCampContinue.Result.StopReason, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase), $"Unexpected repeat continue stop reason at training camp pending: {duplicateTrainingCampContinue.Result.StopReason}.");
            Require(!duplicateTrainingCampContinue.Result.Advanced, "Repeat continue at training camp pending should be idempotent.");
            Require(duplicateTrainingCampContinue.Result.GamesSimulated == 0, "Repeat continue at training camp pending should not simulate extra games.");
            Require(context.ActiveLeague.HistoricalSeasons.Count(record => record != null && record.SeasonYear == context.ActiveLeague.SeasonYear) == 1, "Repeat continue should not duplicate season history snapshots.");
            var stableRetirements = RetirementService.GetSeasonRetirementRecord(context.ActiveLeague, context.ActiveLeague.SeasonYear);
            Require(stableRetirements != null && stableRetirements.Completed, "Retirement history should remain available after reaching training camp.");
            Require(stableRetirements.RetiredCount == retiredPlayerCount, "Repeat continue after retirement should not generate additional retirements.");
            ValidateRetirementResults(context.ActiveLeague, stableRetirements);
            ValidateOffseasonInvariants(context.ActiveLeague, championTeamId, runnerUpTeamId, seasonHistoryCount, scheduleCount, regularSeasonResultCount, playoffResultCount, retirementHistoryCount, retiredPlayerCount);
            Pass(result, currentStep);

            currentStep = "Save native league";
            standings = standingsService.GetStandings();
            Require(standings.Ok, standings.Error);
            userStanding = standings.Standings.FirstOrDefault(row => string.Equals(row.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
            Require(userStanding != null, "User team missing from standings before save.");
            var saveResult = saveService.Save(context, smokeSaveName);
            Require(saveResult.Ok, saveResult.Message);
            smokeSaveCreated = true;
            Pass(result, currentStep);

            currentStep = "Load native league";
            var loadResult = saveService.Load(smokeSaveName);
            Require(loadResult.Ok && loadResult.League != null, loadResult.Message);
            var loadedContext = new GameCoreContext
            {
                ActiveLeague = loadResult.League,
            };
            var loadedDashboardService = new DashboardService(loadedContext);
            var loadedRosterService = new RosterService(loadedContext);
            var loadedDepthChartService = new DepthChartService(loadedContext);
            var loadedStandingsService = new StandingsService(loadedContext);
            Require(loadedContext.ActiveLeague.Results.Count == context.ActiveLeague.Results.Count, "Loaded league result count does not match saved league.");
            var loadedStandings = loadedStandingsService.GetStandings();
            Require(loadedStandings.Ok, loadedStandings.Error);
            var loadedUserStanding = loadedStandings.Standings.FirstOrDefault(row => string.Equals(row.TeamId, loadedContext.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
            Require(loadedUserStanding != null, "Loaded standings missing user team.");
            Require(loadedUserStanding.PointsFor == userStanding.PointsFor && loadedUserStanding.PointsAgainst == userStanding.PointsAgainst, "Loaded standings did not preserve PF/PA.");
            var loadedDashboard = loadedDashboardService.GetDashboardState();
            Require(loadedDashboard.Ok, loadedDashboard.Error);
            ValidatePlayoffBracket(loadedContext.ActiveLeague);
            ValidateWildCardResults(loadedContext.ActiveLeague, loadedStandingsService, standingsSnapshot);
            ValidateDivisionalResults(loadedContext.ActiveLeague, loadedStandingsService, standingsSnapshot);
            ValidateConferenceChampionshipResults(loadedContext.ActiveLeague, loadedStandingsService, standingsSnapshot);
            ValidateLeagueChampionshipResults(loadedContext.ActiveLeague, loadedStandingsService, standingsSnapshot);
            ValidateSeasonHistorySnapshot(loadedContext.ActiveLeague, loadedDashboard.Dashboard);
            Require(loadedDashboard.Dashboard.PlayoffBracket != null, "Loaded dashboard should expose playoff bracket DTO.");
            Require(loadedDashboard.Dashboard.PlayoffBracket.ConferenceBrackets.Count == 2, "Loaded dashboard playoff bracket should retain both conferences.");
            Require(!string.IsNullOrWhiteSpace(loadedDashboard.Dashboard.PlayoffSummaryText), "Loaded dashboard should retain playoff summary text.");
            Require(string.Equals(loadedDashboard.Dashboard.PlayoffSummaryText, dashboard.Dashboard.PlayoffSummaryText, StringComparison.Ordinal), "Loaded dashboard playoff summary does not match saved playoff summary.");
            Require(!string.Equals(loadedDashboard.Dashboard.NextGame.HeaderNextLabel, "Next: Week 0 vs TBD", StringComparison.Ordinal), "Loaded postseason dashboard header should not show Week 0 vs TBD.");
            Require(string.Equals(loadedContext.ActiveLeague.Calendar.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase), $"Loaded league should remain in {ScheduleService.TrainingCampPendingPhase}, got {loadedContext.ActiveLeague.Calendar.Phase}.");
            ValidateOffseasonPlaceholderDashboard(loadedDashboard.Dashboard, ScheduleService.TrainingCampPendingPhase);
            var loadedPostseasonAction = loadedDashboard.Dashboard.ActionItems.FirstOrDefault(item =>
                string.Equals(item.Type, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase));
            Require(loadedPostseasonAction != null, "Loaded dashboard should retain the training camp pending action item.");
            Require(string.Equals(loadedPostseasonAction.Description, "Training camp systems are not implemented yet.", StringComparison.Ordinal), $"Unexpected loaded terminal action description: {loadedPostseasonAction?.Description}");
            var loadedHistoryResponse = loadedDashboardService.GetLeagueHistory();
            ValidateLeagueHistoryResponse(loadedContext.ActiveLeague, loadedHistoryResponse);
            Require(string.Equals(SnapshotBracket(loadedContext.ActiveLeague.PlayoffBracket), SnapshotBracket(context.ActiveLeague.PlayoffBracket), StringComparison.Ordinal), "Loaded playoff bracket does not match saved playoff bracket.");
            var expectedRecentResultIds = context.ActiveLeague.Results
                .Where(game => string.Equals(game.HomeTeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(game.AwayTeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(game => game.AbsoluteWeek > 0 ? game.AbsoluteWeek : game.Week)
                .ThenByDescending(game => game.GameId, StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .Select(game => game.GameId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            Require(loadedDashboard.Dashboard.RecentResults.Any(game => expectedRecentResultIds.Contains(game.GameId)), "Loaded dashboard recent results missing current completed games.");
            var loadedSchedule = new ScheduleService(loadedContext).GetTeamSchedule();
            Require(loadedSchedule.Ok, loadedSchedule.Error);
            Require(loadedSchedule.Schedule.Any(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase) && game.Week == 1), "Loaded schedule lost regular-season display week normalization.");
            ValidateLeagueScheduleStructure(loadedContext.ActiveLeague, new ScheduleService(loadedContext));
            var loadedRoster = loadedRosterService.GetTeamRoster();
            Require(loadedRoster.Ok, loadedRoster.Error);
            Require(loadedRoster.Players.Count == context.ActiveLeague.Teams.First(team => string.Equals(team.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase)).Roster.Count, "Loaded roster size changed after save/load.");
            var loadedDepthChart = loadedDepthChartService.GetTeamDepthChart();
            Require(loadedDepthChart.Ok, loadedDepthChart.Error);
            Require(loadedDepthChart.Positions.Count > 0, "Loaded depth chart is missing.");
            var loadedRetirements = RetirementService.GetSeasonRetirementRecord(loadedContext.ActiveLeague, loadedContext.ActiveLeague.SeasonYear);
            Require(loadedRetirements != null && loadedRetirements.Completed, "Loaded save should preserve retirement history.");
            Require(loadedRetirements.RetiredCount == retiredPlayerCount, "Loaded save should preserve retirement count.");
            ValidateRetirementResults(loadedContext.ActiveLeague, loadedRetirements);
            ValidateOffseasonInvariants(loadedContext.ActiveLeague, championTeamId, runnerUpTeamId, seasonHistoryCount, scheduleCount, regularSeasonResultCount, playoffResultCount, retirementHistoryCount, retiredPlayerCount);
            Pass(result, currentStep);

            currentStep = "Clean up save";
            var deleteResult = saveService.Delete(smokeSaveName);
            Require(deleteResult.Ok, deleteResult.Message);
            smokeSaveCreated = false;
            Pass(result, currentStep);

            result.Ok = true;
            result.Message = "C# GameCore smoke test passed.";
            return result;
        }
        catch (Exception ex)
        {
            if (smokeSaveCreated)
            {
                try
                {
                    new GameCoreSaveService().Delete("native_smoke_test_save.json");
                }
                catch
                {
                }
            }

            result.Ok = false;
            result.Message = $"Failed at {currentStep}: {ex.Message}";
            if (result.Steps.Count == 0 || !string.Equals(result.Steps[^1], $"FAIL {currentStep}: {ex.Message}", StringComparison.Ordinal))
                result.Steps.Add($"FAIL {currentStep}: {ex.Message}");
            return result;
        }
    }

    private static void Pass(GameCoreSmokeTestResult result, string step)
    {
        result.Steps.Add($"PASS {step}");
    }

    private static void ValidateSimUntilBehavior()
    {
        var context = new GameCoreContext();
        var bootstrap = new LeagueBootstrapService(context);
        var league = bootstrap.CreateTestLeague();
        var continueService = new ContinueService(context);
        var gameDayService = new GameDayService(context);
        var depthChartService = new DepthChartService(context);
        var standingsService = new StandingsService(context);

        var autoFill = depthChartService.AutoFillDepthChart();
        Require(autoFill.Ok, autoFill.Error);

        var preseasonToRegularSeason = continueService.ContinueUntil("regular_season_week", 1);
        Require(preseasonToRegularSeason.Ok, preseasonToRegularSeason.Error);
        Require(string.Equals(preseasonToRegularSeason.Result.StopReason, "reached_requested_week", StringComparison.OrdinalIgnoreCase), $"Expected reached_requested_week, got {preseasonToRegularSeason.Result.StopReason}.");
        Require(preseasonToRegularSeason.Result.GamesSimulated == LeagueBootstrapService.PreseasonWeeks * LeagueBootstrapService.PreseasonGamesPerWeek, $"Expected {LeagueBootstrapService.PreseasonWeeks * LeagueBootstrapService.PreseasonGamesPerWeek} preseason games simmed, got {preseasonToRegularSeason.Result.GamesSimulated}.");
        Require(preseasonToRegularSeason.Result.WeeksAdvanced >= LeagueBootstrapService.RegularSeasonStartWeek - 1, $"Expected to advance into regular season week 1, got {preseasonToRegularSeason.Result.WeeksAdvanced} weeks.");
        Require(context.ActiveLeague.Calendar.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek, $"Expected regular season to start at absolute week {LeagueBootstrapService.RegularSeasonStartWeek}, got {context.ActiveLeague.Calendar.AbsoluteWeek}.");
        Require(!context.ActiveLeague.Results.Any(result => result.AbsoluteWeek == LeagueBootstrapService.PreseasonWeeks + 1), "Transition bye should not create fake week 4 results during Sim Until.");
        Require(context.ActiveLeague.Results.Count == LeagueBootstrapService.PreseasonWeeks * LeagueBootstrapService.PreseasonGamesPerWeek, "Sim Until should complete each preseason game exactly once.");

        var resultsBeforeDuplicateRun = context.ActiveLeague.Results.Count;
        var duplicateRun = continueService.ContinueUntil("regular_season_week", 1);
        Require(duplicateRun.Ok, duplicateRun.Error);
        Require(string.Equals(duplicateRun.Result.StopReason, "reached_requested_week", StringComparison.OrdinalIgnoreCase), $"Expected duplicate run to report reached_requested_week, got {duplicateRun.Result.StopReason}.");
        Require(context.ActiveLeague.Results.Count == resultsBeforeDuplicateRun, "Re-running the same Sim Until target should not duplicate results.");

        var weekOneContinue = continueService.Continue(14);
        Require(weekOneContinue.Ok, weekOneContinue.Error);
        Require(string.Equals(weekOneContinue.Result.StopReason, "game_day", StringComparison.OrdinalIgnoreCase), $"Expected regular-season continue to stop at game_day, got {weekOneContinue.Result.StopReason}.");
        var userGame = gameDayService.GetCurrentUserGame();
        Require(userGame != null, "Expected a current user game in regular-season week 1.");
        var userGameResult = gameDayService.SimulateCurrentUserGame(userGame.GameId);
        Require(userGameResult.Ok, userGameResult.Error);
        ContinueResponse weekBoundaryPause = null;
        var boundaryGuard = 0;
        while (context.ActiveLeague.Calendar.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek)
        {
            boundaryGuard++;
            Require(boundaryGuard <= 64, "Normal continue failed to stop at the next week boundary.");
            weekBoundaryPause = continueService.Continue(14);
            Require(weekBoundaryPause.Ok, weekBoundaryPause.Error);
            if (string.Equals(weekBoundaryPause.Result.StopReason, "week_advanced", StringComparison.OrdinalIgnoreCase))
                break;
        }
        Require(weekBoundaryPause != null && string.Equals(weekBoundaryPause.Result.StopReason, "week_advanced", StringComparison.OrdinalIgnoreCase), $"Expected week_advanced after finishing regular-season week 1, got {weekBoundaryPause?.Result?.StopReason}.");
        Require(context.ActiveLeague.Calendar.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek + 1, $"Expected to stop at regular-season week 2, got absolute week {context.ActiveLeague.Calendar.AbsoluteWeek}.");

        var postseasonRun = continueService.ContinueUntil("offseason_start");
        Require(postseasonRun.Ok, postseasonRun.Error);
        Require(string.Equals(postseasonRun.Result.StopReason, "reached_offseason", StringComparison.OrdinalIgnoreCase), $"Expected reached_offseason, got {postseasonRun.Result.StopReason}.");
        Require(!string.Equals(postseasonRun.Result.StopReason, "max_iterations_reached", StringComparison.OrdinalIgnoreCase), "Sim Until should not exhaust its iteration guard.");
        Require(context.ActiveLeague.Results.Select(entry => entry.GameId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == context.ActiveLeague.Results.Count, "Sim Until should not create duplicate result ids.");
        Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.OffseasonPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.OffseasonPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");
        ValidateFinalRegularSeasonStandings(league, standingsService);
        ValidatePlayoffBracket(context.ActiveLeague);
        ValidateWildCardResults(context.ActiveLeague, standingsService, SnapshotRegularSeasonStandings(standingsService.GetStandings()));
        ValidateDivisionalResults(context.ActiveLeague, standingsService, SnapshotRegularSeasonStandings(standingsService.GetStandings()));
        ValidateConferenceChampionshipResults(context.ActiveLeague, standingsService, SnapshotRegularSeasonStandings(standingsService.GetStandings()));
        ValidateLeagueChampionshipResults(context.ActiveLeague, standingsService, SnapshotRegularSeasonStandings(standingsService.GetStandings()));

        var freeAgencyRun = continueService.ContinueUntil("free_agency");
        Require(freeAgencyRun.Ok, freeAgencyRun.Error);
        Require(string.Equals(freeAgencyRun.Result.StopReason, "reached_free_agency", StringComparison.OrdinalIgnoreCase), $"Expected reached_free_agency, got {freeAgencyRun.Result.StopReason}.");
        Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.FreeAgencyPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.FreeAgencyPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");
        Require(freeAgencyRun.Result.GamesSimulated == 0, "Free agency placeholder should not simulate games.");
        Require(freeAgencyRun.Result.EventsProcessed.Any(@event =>
            string.Equals(@event.Type, "retirements_generated", StringComparison.OrdinalIgnoreCase)
            || string.Equals(@event.Type, "retirements_skipped", StringComparison.OrdinalIgnoreCase)), "Sim Until free_agency should process retirement pending on the way through.");
        var simUntilRetirements = RetirementService.GetSeasonRetirementRecord(context.ActiveLeague, context.ActiveLeague.SeasonYear);
        Require(simUntilRetirements != null && simUntilRetirements.Completed, "Sim Until free_agency should generate retirement history for the current season.");
        ValidateRetirementResults(context.ActiveLeague, simUntilRetirements);

        var draftRun = continueService.ContinueUntil("draft");
        Require(draftRun.Ok, draftRun.Error);
        Require(string.Equals(draftRun.Result.StopReason, "reached_draft", StringComparison.OrdinalIgnoreCase), $"Expected reached_draft, got {draftRun.Result.StopReason}.");
        Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.DraftPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");
        Require(draftRun.Result.GamesSimulated == 0, "Draft placeholder should not simulate games.");

        var trainingCampRun = continueService.ContinueUntil("training_camp");
        Require(trainingCampRun.Ok, trainingCampRun.Error);
        Require(string.Equals(trainingCampRun.Result.StopReason, "reached_training_camp", StringComparison.OrdinalIgnoreCase), $"Expected reached_training_camp, got {trainingCampRun.Result.StopReason}.");
        Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.TrainingCampPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");
        Require(trainingCampRun.Result.GamesSimulated == 0, "Training camp placeholder should not simulate games.");

        var duplicateTrainingCampRun = continueService.ContinueUntil("training_camp");
        Require(duplicateTrainingCampRun.Ok, duplicateTrainingCampRun.Error);
        Require(string.Equals(duplicateTrainingCampRun.Result.StopReason, "reached_training_camp", StringComparison.OrdinalIgnoreCase), $"Expected repeat training camp target to return reached_training_camp, got {duplicateTrainingCampRun.Result.StopReason}.");
        Require(duplicateTrainingCampRun.Result.GamesSimulated == 0, "Repeat training camp target should remain idempotent.");
    }

    private static void ValidateLeagueScheduleStructure(GridironGM.GameCore.Models.LeagueState league, ScheduleService scheduleService)
    {
        Require(league != null, "League is required for schedule validation.");
        Require(scheduleService != null, "Schedule service is required for schedule validation.");

        var scheduleByWeek = league.Schedule
            .GroupBy(game => game.AbsoluteWeek)
            .ToDictionary(group => group.Key, group => group.ToList());

        for (var week = 1; week <= LeagueBootstrapService.PreseasonWeeks; week++)
        {
            Require(scheduleByWeek.TryGetValue(week, out var preseasonGames), $"Missing preseason week {week}.");
            Require(preseasonGames.Count == LeagueBootstrapService.PreseasonGamesPerWeek, $"Preseason week {week} should have {LeagueBootstrapService.PreseasonGamesPerWeek} games.");
            Require(preseasonGames.All(game =>
                string.Equals(game.GameType, "preseason", StringComparison.OrdinalIgnoreCase)
                && game.PhaseWeek == week
                && string.Equals(game.Phase, "Preseason", StringComparison.Ordinal)
                && string.Equals(game.WeekLabel, $"Preseason Week {week}", StringComparison.Ordinal)), $"Preseason week {week} metadata is inconsistent.");
        }

        Require(!scheduleByWeek.ContainsKey(LeagueBootstrapService.PreseasonWeeks + 1), "Transition bye week should not contain scheduled games.");

        for (var absoluteWeek = LeagueBootstrapService.RegularSeasonStartWeek; absoluteWeek <= LeagueBootstrapService.TotalSeasonWeeks; absoluteWeek++)
        {
            Require(scheduleByWeek.TryGetValue(absoluteWeek, out var regularSeasonGames), $"Missing regular-season absolute week {absoluteWeek}.");
            var phaseWeek = absoluteWeek - LeagueBootstrapService.RegularSeasonStartWeek + 1;
            var expectedGameCount = phaseWeek is 9 or 10 ? 8 : 16;
            Require(regularSeasonGames.Count == expectedGameCount, $"Regular-season week {phaseWeek} should have {expectedGameCount} games, got {regularSeasonGames.Count}.");
            Require(regularSeasonGames.All(game =>
                string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase)
                && game.PhaseWeek == phaseWeek
                && string.Equals(game.Phase, "Regular Season", StringComparison.Ordinal)
                && string.Equals(game.WeekLabel, $"Regular Season Week {phaseWeek}", StringComparison.Ordinal)), $"Regular-season week {phaseWeek} metadata is inconsistent.");
        }

        foreach (var team in league.Teams)
        {
            var teamSchedule = scheduleService.GetTeamSchedule(team.TeamId);
            Require(teamSchedule.Ok, $"Schedule lookup failed for {team.TeamId}: {teamSchedule.Error}");
            Require(teamSchedule.Schedule.Count == LeagueBootstrapService.PreseasonWeeks + LeagueBootstrapService.RegularSeasonGamesPerTeam, $"{team.TeamId} should have 20 scheduled games.");

            var preseasonGames = teamSchedule.Schedule
                .Where(game => string.Equals(game.GameType, "preseason", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Require(preseasonGames.Count == LeagueBootstrapService.PreseasonWeeks, $"{team.TeamId} should have 3 preseason games.");

            var regularSeasonGames = teamSchedule.Schedule
                .Where(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Require(regularSeasonGames.Count == LeagueBootstrapService.RegularSeasonGamesPerTeam, $"{team.TeamId} should have 17 regular-season games.");

            var regularSeasonWeeks = regularSeasonGames
                .Select(game => game.PhaseWeek)
                .Distinct()
                .OrderBy(week => week)
                .ToList();
            Require(regularSeasonWeeks.Count == LeagueBootstrapService.RegularSeasonGamesPerTeam, $"{team.TeamId} should appear in 17 distinct regular-season weeks.");
            Require(Enumerable.Range(1, LeagueBootstrapService.RegularSeasonWeeks).Count(week => !regularSeasonWeeks.Contains(week)) == 1, $"{team.TeamId} should have exactly one regular-season bye.");
            Require(!teamSchedule.Schedule.Any(game => game.AbsoluteWeek == LeagueBootstrapService.PreseasonWeeks + 1), $"{team.TeamId} should not have a game during the preseason transition bye.");

            foreach (var scheduleRow in teamSchedule.Schedule)
            {
                var backingGame = league.Schedule.FirstOrDefault(game => string.Equals(game.GameId, scheduleRow.GameId, StringComparison.OrdinalIgnoreCase));
                Require(backingGame != null, $"Missing backing game for schedule row {scheduleRow.GameId}.");
                Require(scheduleRow.AbsoluteWeek == backingGame.AbsoluteWeek, $"Absolute week mismatch for {scheduleRow.GameId}.");
                Require(scheduleRow.PhaseWeek == backingGame.PhaseWeek, $"Phase week mismatch for {scheduleRow.GameId}.");
                Require(string.Equals(scheduleRow.Phase, backingGame.Phase, StringComparison.Ordinal), $"Phase mismatch for {scheduleRow.GameId}.");
                Require(string.Equals(scheduleRow.WeekLabel, backingGame.WeekLabel, StringComparison.Ordinal), $"Week label mismatch for {scheduleRow.GameId}.");
                Require(!string.IsNullOrWhiteSpace(scheduleRow.Opponent), $"Opponent missing for {scheduleRow.GameId}.");
                Require(scheduleRow.HomeAway is "home" or "away", $"Home/away missing for {scheduleRow.GameId}.");
            }
        }
    }

    private static void AdvanceUntilAbsoluteWeek(
        GameCoreContext context,
        ContinueService continueService,
        GameDayService gameDayService,
        int targetAbsoluteWeek)
    {
        var guard = 0;
        while (context.ActiveLeague.Calendar.AbsoluteWeek < targetAbsoluteWeek)
        {
            guard++;
            Require(guard <= 256, $"AdvanceUntilAbsoluteWeek exceeded safety limit before reaching week {targetAbsoluteWeek}.");

            var currentGame = gameDayService.GetCurrentUserGame();
            if (currentGame != null)
            {
                var simulationResult = gameDayService.SimulateCurrentUserGame(currentGame.GameId);
                Require(simulationResult.Ok, simulationResult.Error);
                continue;
            }

            var continueResult = continueService.Continue(14);
            Require(continueResult.Ok, continueResult.Error);
            if (context.ActiveLeague.Calendar.AbsoluteWeek >= targetAbsoluteWeek)
                break;
        }
    }

    private static void SimCurrentAbsoluteWeek(
        GameCoreContext context,
        ContinueService continueService,
        GameDayService gameDayService,
        int targetAbsoluteWeek)
    {
        Require(context.ActiveLeague.Calendar.AbsoluteWeek == targetAbsoluteWeek, $"Expected to sim absolute week {targetAbsoluteWeek}, got {context.ActiveLeague.Calendar.AbsoluteWeek}.");

        var guard = 0;
        while (context.ActiveLeague.Calendar.AbsoluteWeek == targetAbsoluteWeek)
        {
            guard++;
            Require(guard <= 256, $"SimCurrentAbsoluteWeek exceeded safety limit in week {targetAbsoluteWeek}.");

            var currentGame = gameDayService.GetCurrentUserGame();
            if (currentGame != null)
            {
                Require(currentGame.AbsoluteWeek == targetAbsoluteWeek, $"User game leaked to week {currentGame.AbsoluteWeek} while simming week {targetAbsoluteWeek}.");
                var simulationResult = gameDayService.SimulateCurrentUserGame(currentGame.GameId);
                Require(simulationResult.Ok, simulationResult.Error);
                continue;
            }

            var continueResult = continueService.Continue(14);
            Require(continueResult.Ok, continueResult.Error);
        }
    }

    private static void SimRegularSeasonThroughCompletion(
        GameCoreContext context,
        ContinueService continueService,
        GameDayService gameDayService)
    {
        var currentWeek = context.ActiveLeague.Calendar.AbsoluteWeek;
        while (currentWeek >= LeagueBootstrapService.RegularSeasonStartWeek
            && currentWeek <= LeagueBootstrapService.TotalSeasonWeeks)
        {
            SimCurrentAbsoluteWeek(context, continueService, gameDayService, currentWeek);
            currentWeek = context.ActiveLeague.Calendar.AbsoluteWeek;
        }
    }

    private static void ValidateSimulatedResultLabels(GridironGM.GameCore.Models.LeagueState league)
    {
        var resultsByAbsoluteWeek = league.Results
            .GroupBy(result => result.AbsoluteWeek)
            .ToDictionary(group => group.Key, group => group.ToList());

        Require(resultsByAbsoluteWeek.ContainsKey(1), "Expected simulated results for preseason week 1.");
        Require(resultsByAbsoluteWeek.ContainsKey(2), "Expected simulated results for preseason week 2.");
        Require(resultsByAbsoluteWeek.ContainsKey(LeagueBootstrapService.RegularSeasonStartWeek), "Expected simulated results for regular-season week 1.");

        Require(resultsByAbsoluteWeek[1].All(result => string.Equals(result.WeekLabel, "Preseason Week 1", StringComparison.Ordinal)), "Preseason week 1 results are mislabeled.");
        Require(resultsByAbsoluteWeek[2].All(result => string.Equals(result.WeekLabel, "Preseason Week 2", StringComparison.Ordinal)), "Preseason week 2 results are mislabeled.");
        Require(resultsByAbsoluteWeek[LeagueBootstrapService.RegularSeasonStartWeek].All(result => string.Equals(result.WeekLabel, "Regular Season Week 1", StringComparison.Ordinal)), "Regular-season week 1 results are mislabeled.");

        var distinctLabels = league.Results
            .Select(result => result.WeekLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Require(distinctLabels.Count >= 3, "Simmed results should produce multiple distinct week labels.");
    }

    private static void ValidateFinalRegularSeasonStandings(
        GridironGM.GameCore.Models.LeagueState league,
        StandingsService standingsService)
    {
        var standings = standingsService.GetStandings();
        Require(standings.Ok, standings.Error);
        Require(standings.Standings.Count == LeagueBootstrapService.TeamCount, "Final standings should include all teams.");

        var regularSeasonResults = league.Results
            .Where(result => string.Equals(result.GameType, "regular_season", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Require(regularSeasonResults.Count == LeagueBootstrapService.RegularSeasonGameCount, $"Expected {LeagueBootstrapService.RegularSeasonGameCount} regular-season results.");
        Require(!regularSeasonResults.Any(result => result.AbsoluteWeek < LeagueBootstrapService.RegularSeasonStartWeek || result.AbsoluteWeek > LeagueBootstrapService.TotalSeasonWeeks), "Regular-season results should stay within the regular-season week window.");

        var totalWins = standings.Standings.Sum(row => row.Wins);
        var totalLosses = standings.Standings.Sum(row => row.Losses);
        var totalTies = standings.Standings.Sum(row => row.Ties);
        Require(totalWins == LeagueBootstrapService.RegularSeasonGameCount, $"Expected {LeagueBootstrapService.RegularSeasonGameCount} total regular-season wins, got {totalWins}.");
        Require(totalLosses == LeagueBootstrapService.RegularSeasonGameCount, $"Expected {LeagueBootstrapService.RegularSeasonGameCount} total regular-season losses, got {totalLosses}.");
        Require(totalTies == 0, $"Expected 0 counted ties in current native sim, got {totalTies}.");

        foreach (var row in standings.Standings)
        {
            var countedGames = row.Wins + row.Losses + row.Ties;
            Require(countedGames == LeagueBootstrapService.RegularSeasonGamesPerTeam, $"{row.TeamId} should have {LeagueBootstrapService.RegularSeasonGamesPerTeam} counted regular-season games, got {countedGames}.");
        }
    }

    private static void ValidatePlayoffBracket(GridironGM.GameCore.Models.LeagueState league)
    {
        Require(league?.PlayoffBracket != null, "League should expose a playoff bracket at postseason pending.");
        var bracket = league.PlayoffBracket;
        Require(bracket.SeasonYear == league.SeasonYear, "Playoff bracket season year should match league season year.");
        Require(bracket.GeneratedFromAbsoluteWeek == LeagueBootstrapService.TotalSeasonWeeks + 1, $"Playoff bracket should be generated at absolute week {LeagueBootstrapService.TotalSeasonWeeks + 1}.");
        Require(string.Equals(bracket.GeneratedAtPhaseLabel, ScheduleService.PostseasonPendingWeekLabel, StringComparison.Ordinal), $"Unexpected playoff bracket phase label: {bracket.GeneratedAtPhaseLabel}");
        Require(bracket.ConferenceBrackets.Count == 2, $"Expected 2 conference brackets, got {bracket.ConferenceBrackets.Count}.");

        var totalTeams = 0;
        foreach (var conferenceBracket in bracket.ConferenceBrackets.OrderBy(entry => entry.Conference, StringComparer.OrdinalIgnoreCase))
        {
            Require(conferenceBracket.Seeds.Count == 7, $"{conferenceBracket.Conference} should have 7 playoff teams.");
            Require(conferenceBracket.Seeds.Select(seed => seed.TeamId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 7, $"{conferenceBracket.Conference} playoff seeds must be unique.");
            Require(conferenceBracket.Seeds.Count(seed => seed.IsDivisionWinner) == 4, $"{conferenceBracket.Conference} should have 4 division winners.");
            Require(conferenceBracket.Seeds.Count(seed => !seed.IsDivisionWinner) == 3, $"{conferenceBracket.Conference} should have 3 wild cards.");
            Require(conferenceBracket.Seeds.Where(seed => seed.IsDivisionWinner).Select(seed => seed.Seed).OrderBy(seed => seed).SequenceEqual(new[] { 1, 2, 3, 4 }), $"{conferenceBracket.Conference} division winners must be seeds 1-4.");
            Require(conferenceBracket.Seeds.Where(seed => !seed.IsDivisionWinner).Select(seed => seed.Seed).OrderBy(seed => seed).SequenceEqual(new[] { 5, 6, 7 }), $"{conferenceBracket.Conference} wild cards must be seeds 5-7.");
            Require(conferenceBracket.Rounds.Count >= 1, $"{conferenceBracket.Conference} should expose at least the Wild Card round.");
            var wildCardRound = conferenceBracket.Rounds.FirstOrDefault(round => string.Equals(round.Round, PlayoffService.WildCardRound, StringComparison.OrdinalIgnoreCase));
            Require(wildCardRound != null, $"{conferenceBracket.Conference} should expose a Wild Card round.");
            Require(string.Equals(wildCardRound.Round, "Wild Card", StringComparison.Ordinal), $"{conferenceBracket.Conference} first playoff round should be Wild Card.");
            Require(wildCardRound.Games.Count == 3, $"{conferenceBracket.Conference} wild card round should have 3 games.");
            Require(!wildCardRound.Games.Any(game => game.HomeSeed == 1 || game.AwaySeed == 1), $"{conferenceBracket.Conference} seed 1 should have a bye.");
            Require(wildCardRound.Games.Any(game => game.HomeSeed == 2 && game.AwaySeed == 7), $"{conferenceBracket.Conference} missing 2 vs 7.");
            Require(wildCardRound.Games.Any(game => game.HomeSeed == 3 && game.AwaySeed == 6), $"{conferenceBracket.Conference} missing 3 vs 6.");
            Require(wildCardRound.Games.Any(game => game.HomeSeed == 4 && game.AwaySeed == 5), $"{conferenceBracket.Conference} missing 4 vs 5.");
            totalTeams += conferenceBracket.Seeds.Count;
        }

        Require(totalTeams == 14, $"Expected 14 total playoff teams, got {totalTeams}.");
        Require(bracket.LeagueChampionshipRound != null, "Playoff bracket should expose a league championship round container.");
    }

    private static string SnapshotBracket(GridironGM.GameCore.Models.PlayoffBracket bracket)
    {
        if (bracket == null)
            return "";

        return string.Join("|", bracket.ConferenceBrackets
            .OrderBy(entry => entry.Conference, StringComparer.OrdinalIgnoreCase)
            .Select(entry => string.Concat(
                entry.Conference,
                ":",
                string.Join(",", entry.Seeds.OrderBy(seed => seed.Seed).Select(seed => $"{seed.Seed}-{seed.TeamId}-{seed.IsDivisionWinner}")),
                ":",
                string.Join(",", entry.Rounds.SelectMany(round => round.Games).OrderBy(game => game.HomeSeed).ThenBy(game => game.AwaySeed).Select(game => $"{game.HomeSeed}v{game.AwaySeed}:{game.HomeTeamId}-{game.AwayTeamId}:{game.Status}:{game.HomeScore}-{game.AwayScore}:{game.WinnerTeamId}")))))
            + $"|league:{string.Join(",", (bracket.LeagueChampionshipRound?.Games ?? new List<GridironGM.GameCore.Models.PlayoffGame>()).Select(game => $"{game.HomeTeamId}-{game.AwayTeamId}:{game.Status}:{game.HomeScore}-{game.AwayScore}:{game.WinnerTeamId}"))}"
            + $"|champion:{bracket.LeagueChampionRecord?.ChampionTeamId}:{bracket.LeagueChampionRecord?.RunnerUpTeamId}:{bracket.LeagueChampionRecord?.ChampionScore}-{bracket.LeagueChampionRecord?.RunnerUpScore}";
    }

    private static void ValidateWildCardResults(
        GridironGM.GameCore.Models.LeagueState league,
        StandingsService standingsService,
        string standingsSnapshotBeforeWildCard)
    {
        var wildCardGames = league.PlayoffBracket.ConferenceBrackets
            .SelectMany(entry => entry.Rounds)
            .Where(round => string.Equals(round.Round, PlayoffService.WildCardRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games)
            .OrderBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ToList();
        Require(league.PlayoffBracket.ConferenceBrackets.All(entry =>
            entry.Rounds.Where(round => string.Equals(round.Round, PlayoffService.WildCardRound, StringComparison.OrdinalIgnoreCase))
                .All(round => string.Equals(round.Status, "completed", StringComparison.OrdinalIgnoreCase))), "Conference Wild Card rounds should be marked completed.");

        Require(wildCardGames.Count == 6, $"Expected 6 Wild Card games, got {wildCardGames.Count}.");
        Require(wildCardGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)), "All Wild Card games should be completed.");
        Require(wildCardGames.All(game => game.HomeScore.HasValue && game.AwayScore.HasValue), "All Wild Card games should have scores.");
        Require(wildCardGames.All(game => !string.IsNullOrWhiteSpace(game.WinnerTeamId)), "All Wild Card games should have a winner.");
        Require(wildCardGames.All(game => !string.IsNullOrWhiteSpace(game.LoserTeamId)), "All Wild Card games should have a loser.");
        Require(wildCardGames.All(game => game.HomeScore != game.AwayScore), "Wild Card games should not end tied.");
        Require(!wildCardGames.Any(game => game.HomeSeed == 1 || game.AwaySeed == 1), "Seed 1 teams must not play in the Wild Card round.");

        var playoffResults = league.Results
            .Where(result => string.Equals(result.GameType, "playoffs", StringComparison.OrdinalIgnoreCase)
                && string.Equals(result.WeekLabel, "Playoffs - Wild Card", StringComparison.Ordinal))
            .OrderBy(result => result.GameId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Require(playoffResults.Count == 6, $"Expected 6 persisted Wild Card results, got {playoffResults.Count}.");
        Require(playoffResults.All(result => result.HomeScore != result.AwayScore), "Persisted Wild Card results should not end tied.");

        var standingsSnapshotAfterWildCard = SnapshotRegularSeasonStandings(standingsService.GetStandings());
        Require(string.Equals(standingsSnapshotAfterWildCard, standingsSnapshotBeforeWildCard, StringComparison.Ordinal), "Wild Card simulation should not change regular-season standings.");
    }

    private static void ValidateDivisionalResults(
        GridironGM.GameCore.Models.LeagueState league,
        StandingsService standingsService,
        string standingsSnapshotBeforeDivisional)
    {
        var divisionalGames = league.PlayoffBracket.ConferenceBrackets
            .SelectMany(entry => entry.Rounds)
            .Where(round => string.Equals(round.Round, PlayoffService.DivisionalRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games)
            .OrderBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ToList();
        Require(league.PlayoffBracket.ConferenceBrackets.All(entry =>
            entry.Rounds.Where(round => string.Equals(round.Round, PlayoffService.DivisionalRound, StringComparison.OrdinalIgnoreCase))
                .All(round => string.Equals(round.Status, "completed", StringComparison.OrdinalIgnoreCase))), "Conference Divisional rounds should be marked completed.");
        Require(divisionalGames.Count == 4, $"Expected 4 Divisional games, got {divisionalGames.Count}.");
        Require(divisionalGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)), "All Divisional games should be completed.");
        Require(divisionalGames.All(game => game.HomeScore.HasValue && game.AwayScore.HasValue), "All Divisional games should have scores.");
        Require(divisionalGames.All(game => !string.IsNullOrWhiteSpace(game.WinnerTeamId) && !string.IsNullOrWhiteSpace(game.LoserTeamId)), "All Divisional games should have winner and loser ids.");
        Require(divisionalGames.All(game => game.HomeScore != game.AwayScore), "Divisional games should not end tied.");
        Require(divisionalGames.Count(game => game.HomeSeed == 1) == 2, "Seed 1 teams should both host in the Divisional Round.");

        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets)
        {
            var seedsByTeamId = conferenceBracket.Seeds.ToDictionary(seed => seed.TeamId, StringComparer.OrdinalIgnoreCase);
            var wildCardWinners = conferenceBracket.Rounds
                .Where(round => string.Equals(round.Round, PlayoffService.WildCardRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games)
                .Select(game => seedsByTeamId[string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase) ? game.HomeTeamId : game.AwayTeamId])
                .OrderBy(seed => seed.Seed)
                .ToList();
            var remainingSeeds = new List<int> { 1 };
            remainingSeeds.AddRange(wildCardWinners.Select(seed => seed.Seed));
            remainingSeeds = remainingSeeds.OrderBy(seed => seed).ToList();

            var expectedPairs = new[]
            {
                $"{remainingSeeds[0]}v{remainingSeeds[3]}",
                $"{remainingSeeds[1]}v{remainingSeeds[2]}",
            };
            var actualPairs = conferenceBracket.Rounds
                .Where(round => string.Equals(round.Round, PlayoffService.DivisionalRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games)
                .OrderBy(game => game.HomeSeed)
                .ThenBy(game => game.AwaySeed)
                .Select(game => $"{game.HomeSeed}v{game.AwaySeed}")
                .ToArray();
            Require(actualPairs.SequenceEqual(expectedPairs), $"{conferenceBracket.Conference} Divisional matchups should follow highest-vs-lowest remaining seed logic.");
        }

        var playoffResults = league.Results
            .Where(result => string.Equals(result.GameType, "playoffs", StringComparison.OrdinalIgnoreCase)
                && string.Equals(result.WeekLabel, "Divisional Round", StringComparison.Ordinal))
            .OrderBy(result => result.GameId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Require(playoffResults.Count == 4, $"Expected 4 persisted Divisional results, got {playoffResults.Count}.");
        Require(playoffResults.All(result => result.HomeScore != result.AwayScore), "Persisted Divisional results should not end tied.");

        var standingsSnapshotAfterDivisional = SnapshotRegularSeasonStandings(standingsService.GetStandings());
        Require(string.Equals(standingsSnapshotAfterDivisional, standingsSnapshotBeforeDivisional, StringComparison.Ordinal), "Divisional simulation should not change regular-season standings.");
    }

    private static void ValidateConferenceChampionshipResults(
        GridironGM.GameCore.Models.LeagueState league,
        StandingsService standingsService,
        string standingsSnapshotBeforeConferenceChampionship)
    {
        var conferenceGames = league.PlayoffBracket.ConferenceBrackets
            .SelectMany(entry => entry.Rounds)
            .Where(round => string.Equals(round.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games)
            .OrderBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ToList();
        Require(league.PlayoffBracket.ConferenceBrackets.All(entry =>
            entry.Rounds.Where(round => string.Equals(round.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
                .All(round => string.Equals(round.Status, "completed", StringComparison.OrdinalIgnoreCase))), "Conference Championship rounds should be marked completed.");
        Require(conferenceGames.Count == 2, $"Expected 2 Conference Championship games, got {conferenceGames.Count}.");
        Require(conferenceGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)), "All Conference Championship games should be completed.");
        Require(conferenceGames.All(game => game.HomeScore.HasValue && game.AwayScore.HasValue), "All Conference Championship games should have scores.");
        Require(conferenceGames.All(game => !string.IsNullOrWhiteSpace(game.WinnerTeamId) && !string.IsNullOrWhiteSpace(game.LoserTeamId)), "All Conference Championship games should have winner and loser ids.");
        Require(conferenceGames.All(game => game.HomeScore != game.AwayScore), "Conference Championship games should not end tied.");
        Require(conferenceGames.GroupBy(game => game.Conference, StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 1), "Each conference should have exactly one Conference Championship game.");

        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets)
        {
            var seedsByTeamId = conferenceBracket.Seeds.ToDictionary(seed => seed.TeamId, StringComparer.OrdinalIgnoreCase);
            var divisionalWinners = conferenceBracket.Rounds
                .Where(round => string.Equals(round.Round, PlayoffService.DivisionalRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games)
                .Select(game => seedsByTeamId[game.WinnerTeamId])
                .OrderBy(seed => seed.Seed)
                .ToList();
            Require(divisionalWinners.Count == 2, $"{conferenceBracket.Conference} should produce 2 Divisional winners.");

            var conferenceGame = conferenceBracket.Rounds
                .Where(round => string.Equals(round.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games)
                .Single();
            Require(conferenceGame.HomeSeed == divisionalWinners[0].Seed, $"{conferenceBracket.Conference} higher remaining seed should host the Conference Championship.");
            Require(conferenceGame.AwaySeed == divisionalWinners[1].Seed, $"{conferenceBracket.Conference} lower remaining seed should be the Conference Championship road team.");
        }

        var playoffResults = league.Results
            .Where(result => string.Equals(result.GameType, "playoffs", StringComparison.OrdinalIgnoreCase)
                && string.Equals(result.WeekLabel, "Conference Championship", StringComparison.Ordinal))
            .OrderBy(result => result.GameId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Require(playoffResults.Count == 2, $"Expected 2 persisted Conference Championship results, got {playoffResults.Count}.");
        Require(playoffResults.All(result => result.HomeScore != result.AwayScore), "Persisted Conference Championship results should not end tied.");

        var standingsSnapshotAfterConferenceChampionship = SnapshotRegularSeasonStandings(standingsService.GetStandings());
        Require(string.Equals(standingsSnapshotAfterConferenceChampionship, standingsSnapshotBeforeConferenceChampionship, StringComparison.Ordinal), "Conference Championship simulation should not change regular-season standings.");
    }

    private static void ValidateLeagueChampionshipResults(
        GridironGM.GameCore.Models.LeagueState league,
        StandingsService standingsService,
        string standingsSnapshotBeforeLeagueChampionship)
    {
        var leagueRound = league.PlayoffBracket.LeagueChampionshipRound;
        Require(leagueRound != null, "League Championship round should exist.");
        Require(string.Equals(leagueRound.Round, PlayoffService.LeagueChampionshipRound, StringComparison.OrdinalIgnoreCase), "League Championship round should use the expected label.");
        Require(string.Equals(leagueRound.Status, "completed", StringComparison.OrdinalIgnoreCase), "League Championship round should be marked completed.");
        Require(leagueRound.Games.Count == 1, $"Expected 1 League Championship game, got {leagueRound.Games.Count}.");

        var leagueGame = leagueRound.Games.Single();
        Require(string.Equals(leagueGame.Status, "completed", StringComparison.OrdinalIgnoreCase), "League Championship game should be completed.");
        Require(leagueGame.NeutralSite, "League Championship should be marked neutral site.");
        Require(leagueGame.HomeScore.HasValue && leagueGame.AwayScore.HasValue, "League Championship should have scores.");
        Require(leagueGame.HomeScore != leagueGame.AwayScore, "League Championship should not end tied.");
        Require(!string.IsNullOrWhiteSpace(leagueGame.WinnerTeamId) && !string.IsNullOrWhiteSpace(leagueGame.LoserTeamId), "League Championship should have winner and loser ids.");
        Require(string.Equals(leagueGame.RoundLabel, PlayoffService.LeagueChampionshipRound, StringComparison.Ordinal), $"Unexpected League Championship round label: {leagueGame.RoundLabel}");

        var conferenceWinners = league.PlayoffBracket.ConferenceBrackets
            .SelectMany(entry => entry.Rounds)
            .Where(round => string.Equals(round.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games)
            .Select(game => game.WinnerTeamId)
            .OrderBy(teamId => teamId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var leagueTeams = new[] { leagueGame.HomeTeamId, leagueGame.AwayTeamId }
            .OrderBy(teamId => teamId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Require(leagueTeams.SequenceEqual(conferenceWinners), "League Championship teams should be the two Conference Championship winners.");

        var leagueResults = league.Results
            .Where(result => string.Equals(result.GameType, "playoffs", StringComparison.OrdinalIgnoreCase)
                && string.Equals(result.WeekLabel, PlayoffService.LeagueChampionshipRound, StringComparison.Ordinal))
            .OrderBy(result => result.GameId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        Require(leagueResults.Count == 1, $"Expected 1 persisted League Championship result, got {leagueResults.Count}.");
        Require(leagueResults[0].HomeScore != leagueResults[0].AwayScore, "Persisted League Championship result should not end tied.");

        var championRecord = league.PlayoffBracket.LeagueChampionRecord;
        Require(championRecord != null, "League champion record should exist.");
        Require(championRecord.SeasonYear == league.SeasonYear, "League champion record season year should match league season year.");
        Require(string.Equals(championRecord.ChampionshipHomeTeamId, leagueGame.HomeTeamId, StringComparison.OrdinalIgnoreCase), "Champion record should preserve championship home team id.");
        Require(string.Equals(championRecord.ChampionshipAwayTeamId, leagueGame.AwayTeamId, StringComparison.OrdinalIgnoreCase), "Champion record should preserve championship away team id.");
        Require(string.Equals(championRecord.ChampionTeamId, leagueGame.WinnerTeamId, StringComparison.OrdinalIgnoreCase), "Champion record should preserve champion team id.");
        Require(string.Equals(championRecord.RunnerUpTeamId, leagueGame.LoserTeamId, StringComparison.OrdinalIgnoreCase), "Champion record should preserve runner-up team id.");
        Require(!string.IsNullOrWhiteSpace(championRecord.ChampionTeamName), "Champion record should preserve champion team name.");
        Require(!string.IsNullOrWhiteSpace(championRecord.RunnerUpTeamName), "Champion record should preserve runner-up team name.");
        Require(championRecord.ChampionScore > championRecord.RunnerUpScore, "Champion record should preserve a winning score.");
        Require(string.Equals(championRecord.CompletedPhaseLabel, PlayoffService.LeagueChampionshipRound, StringComparison.Ordinal), $"Unexpected champion record phase label: {championRecord.CompletedPhaseLabel}");
        Require(
            string.Equals(league.Calendar.Phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase)
            || ScheduleService.IsOffseasonPlaceholderPhase(league.Calendar.Phase),
            $"League should stop in {ScheduleService.SeasonCompletePhase} or an offseason placeholder phase, got {league.Calendar.Phase}.");
        Require(
            string.Equals(league.Calendar.WeekLabel, ScheduleService.SeasonCompleteWeekLabel, StringComparison.Ordinal)
            || ScheduleService.IsOffseasonPlaceholderPhase(league.Calendar.WeekLabel),
            $"Unexpected terminal week label: {league.Calendar.WeekLabel}");

        var standingsSnapshotAfterLeagueChampionship = SnapshotRegularSeasonStandings(standingsService.GetStandings());
        Require(string.Equals(standingsSnapshotAfterLeagueChampionship, standingsSnapshotBeforeLeagueChampionship, StringComparison.Ordinal), "League Championship simulation should not change regular-season standings.");
    }

    private static void ValidateEmptySeasonHistory(
        GridironGM.GameCore.Models.LeagueState league,
        DashboardService dashboardService)
    {
        Require(league.HistoricalSeasons != null, "Fresh league should expose a history collection.");
        Require(league.HistoricalSeasons.Count == 0, $"Fresh league should not have completed seasons, got {league.HistoricalSeasons.Count}.");
        var historyResponse = dashboardService.GetLeagueHistory();
        Require(historyResponse != null && historyResponse.Ok, historyResponse?.Error ?? "League history response unavailable.");
        Require(historyResponse.Seasons != null && historyResponse.Seasons.Count == 0, $"Fresh league history response should be empty, got {historyResponse?.Seasons?.Count ?? -1}.");
    }

    private static void ValidateLeagueHistoryResponse(
        GridironGM.GameCore.Models.LeagueState league,
        LeagueHistoryResponse historyResponse)
    {
        Require(historyResponse != null && historyResponse.Ok, historyResponse?.Error ?? "League history response unavailable.");
        Require(historyResponse.Seasons != null, "League history response should expose seasons.");
        Require(historyResponse.Seasons.Count == 1, $"Expected exactly 1 completed season in history response, got {historyResponse.Seasons.Count}.");

        var historySeason = historyResponse.Seasons[0];
        var savedSeason = league.HistoricalSeasons
            .Where(record => record != null && record.SeasonYear == league.SeasonYear)
            .Single();
        Require(historySeason.SeasonYear == savedSeason.SeasonYear, "History response season year should match the saved season.");
        Require(string.Equals(historySeason.ChampionTeamId, savedSeason.ChampionTeamId, StringComparison.OrdinalIgnoreCase), "History response champion id should match the saved season.");
        Require(string.Equals(historySeason.RunnerUpTeamId, savedSeason.RunnerUpTeamId, StringComparison.OrdinalIgnoreCase), "History response runner-up id should match the saved season.");
        Require(string.Equals(historySeason.ChampionTeamName, savedSeason.ChampionTeamName, StringComparison.Ordinal), "History response champion name should match the saved season.");
        Require(string.Equals(historySeason.RunnerUpTeamName, savedSeason.RunnerUpTeamName, StringComparison.Ordinal), "History response runner-up name should match the saved season.");
        Require(string.Equals(historySeason.ChampionshipGameLabel, savedSeason.ChampionshipGameLabel, StringComparison.Ordinal), "History response championship label should match the saved season.");
        Require(historySeason.ChampionshipWinnerScore == savedSeason.ChampionshipWinnerScore, "History response winner score should match the saved season.");
        Require(historySeason.ChampionshipRunnerUpScore == savedSeason.ChampionshipRunnerUpScore, "History response runner-up score should match the saved season.");
        Require(historySeason.TotalRegularSeasonGames == LeagueBootstrapService.RegularSeasonGameCount, $"History response should preserve {LeagueBootstrapService.RegularSeasonGameCount} regular-season games, got {historySeason.TotalRegularSeasonGames}.");
        Require(historySeason.TotalPlayoffGames == 13, $"History response should preserve 13 playoff games, got {historySeason.TotalPlayoffGames}.");
        Require(historySeason.TeamRecords.Count == LeagueBootstrapService.TeamCount, $"History response should preserve {LeagueBootstrapService.TeamCount} team records, got {historySeason.TeamRecords.Count}.");
        Require(historySeason.PlayoffSeeds.Count == 14, $"History response should preserve 14 playoff seeds, got {historySeason.PlayoffSeeds.Count}.");
        Require(historySeason.PlayoffResults.Count == 13, $"History response should preserve 13 playoff results, got {historySeason.PlayoffResults.Count}.");
        Require(historySeason.PlayoffResults.Count(result => string.Equals(result.Round, PlayoffService.LeagueChampionshipRound, StringComparison.OrdinalIgnoreCase)) == 1, "History response should preserve 1 League Championship result.");
    }

    private static void ValidateSeasonHistorySnapshot(
        GridironGM.GameCore.Models.LeagueState league,
        DashboardDto dashboard)
    {
        Require(league.HistoricalSeasons != null, "League should expose a season history collection.");
        var seasonHistory = league.HistoricalSeasons
            .Where(record => record != null && record.SeasonYear == league.SeasonYear)
            .ToList();
        Require(seasonHistory.Count == 1, $"Expected exactly 1 season history record for season {league.SeasonYear}, got {seasonHistory.Count}.");

        var snapshot = seasonHistory[0];
        var championRecord = league.PlayoffBracket.LeagueChampionRecord;
        var leagueGame = league.PlayoffBracket.LeagueChampionshipRound.Games.Single();
        Require(snapshot.SeasonYear == league.SeasonYear, "Season history year should match league season year.");
        Require(string.Equals(snapshot.ChampionTeamId, championRecord.ChampionTeamId, StringComparison.OrdinalIgnoreCase), "Season history champion id should match league champion record.");
        Require(string.Equals(snapshot.ChampionTeamName, championRecord.ChampionTeamName, StringComparison.Ordinal), "Season history champion name should match league champion record.");
        Require(string.Equals(snapshot.RunnerUpTeamId, championRecord.RunnerUpTeamId, StringComparison.OrdinalIgnoreCase), "Season history runner-up id should match league champion record.");
        Require(string.Equals(snapshot.RunnerUpTeamName, championRecord.RunnerUpTeamName, StringComparison.Ordinal), "Season history runner-up name should match league champion record.");
        Require(snapshot.ChampionshipWinnerScore == championRecord.ChampionScore, "Season history winner score should match league champion record.");
        Require(snapshot.ChampionshipRunnerUpScore == championRecord.RunnerUpScore, "Season history runner-up score should match league champion record.");
        Require(string.Equals(snapshot.ChampionshipGameLabel, PlayoffService.LeagueChampionshipRound, StringComparison.Ordinal), $"Unexpected season history championship label: {snapshot.ChampionshipGameLabel}");
        Require(snapshot.TeamRecords.Count == LeagueBootstrapService.TeamCount, $"Season history should preserve {LeagueBootstrapService.TeamCount} team records, got {snapshot.TeamRecords.Count}.");
        Require(snapshot.TotalRegularSeasonGames == LeagueBootstrapService.RegularSeasonGameCount, $"Season history should preserve {LeagueBootstrapService.RegularSeasonGameCount} regular-season games, got {snapshot.TotalRegularSeasonGames}.");
        Require(snapshot.TotalPlayoffGames == 13, $"Season history should preserve 13 playoff games, got {snapshot.TotalPlayoffGames}.");
        Require(snapshot.PlayoffSeeds.Count == 14, $"Season history should preserve 14 playoff seeds, got {snapshot.PlayoffSeeds.Count}.");
        Require(snapshot.PlayoffSeeds.GroupBy(seed => seed.Conference, StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 7), "Season history should preserve 7 seeds per conference.");
        Require(snapshot.PlayoffResults.Count == 13, $"Season history should preserve 13 playoff results, got {snapshot.PlayoffResults.Count}.");
        Require(snapshot.PlayoffResults.Count(result => string.Equals(result.Round, PlayoffService.WildCardRound, StringComparison.OrdinalIgnoreCase)) == 6, "Season history should preserve 6 Wild Card results.");
        Require(snapshot.PlayoffResults.Count(result => string.Equals(result.Round, "Divisional", StringComparison.OrdinalIgnoreCase) || string.Equals(result.Round, "Divisional Round", StringComparison.OrdinalIgnoreCase)) == 4, "Season history should preserve 4 Divisional results.");
        Require(snapshot.PlayoffResults.Count(result => string.Equals(result.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase)) == 2, "Season history should preserve 2 Conference Championship results.");
        Require(snapshot.PlayoffResults.Count(result => string.Equals(result.Round, PlayoffService.LeagueChampionshipRound, StringComparison.OrdinalIgnoreCase)) == 1, "Season history should preserve 1 League Championship result.");
        var savedLeagueChampionship = snapshot.PlayoffResults.Single(result => string.Equals(result.Round, PlayoffService.LeagueChampionshipRound, StringComparison.OrdinalIgnoreCase));
        Require(savedLeagueChampionship.HomeScore == leagueGame.HomeScore.GetValueOrDefault() && savedLeagueChampionship.AwayScore == leagueGame.AwayScore.GetValueOrDefault(), "Season history League Championship score should match the completed game.");
        Require(dashboard.SeasonCompletionSummary != null && dashboard.SeasonCompletionSummary.IsAvailable, "Dashboard should expose season completion summary.");
        Require(string.Equals(dashboard.SeasonCompletionSummary.ChampionTeamName, championRecord.ChampionTeamName, StringComparison.Ordinal), "Dashboard season completion summary champion should match the saved snapshot.");
        Require(string.Equals(dashboard.SeasonCompletionSummary.RunnerUpTeamName, championRecord.RunnerUpTeamName, StringComparison.Ordinal), "Dashboard season completion summary runner-up should match the saved snapshot.");
        Require(dashboard.SeasonCompletionSummary.ChampionshipResultLine.Contains(championRecord.ChampionTeamName, StringComparison.Ordinal), "Dashboard season completion summary should include the champion name.");
        Require(dashboard.SeasonCompletionSummary.ChampionshipResultLine.Contains(championRecord.RunnerUpTeamName, StringComparison.Ordinal), "Dashboard season completion summary should include the runner-up name.");
    }

    private static IEnumerable<string> BuildExpectedOffseasonPlaceholderPhases()
    {
        yield return ScheduleService.OffseasonPendingPhase;
        yield return ScheduleService.StaffCarouselPendingPhase;
        yield return ScheduleService.RetirementPendingPhase;
        yield return ScheduleService.ExclusiveNegotiationPendingPhase;
        yield return ScheduleService.FranchiseTagPendingPhase;
        yield return ScheduleService.LeagueYearPendingPhase;
        yield return ScheduleService.FreeAgencyPendingPhase;
        yield return ScheduleService.DraftPrepPendingPhase;
        yield return ScheduleService.DraftPendingPhase;
        yield return ScheduleService.RookieSigningPendingPhase;
        yield return ScheduleService.TrainingCampPendingPhase;
    }

    private static void ValidateOffseasonPlaceholderDashboard(DashboardDto dashboard, string expectedPhase)
    {
        Require(dashboard != null, "Dashboard is required for offseason placeholder validation.");
        Require(dashboard.Calendar != null, "Dashboard calendar is required for offseason placeholder validation.");
        Require(dashboard.NextGame != null, "Dashboard next-game block is required for offseason placeholder validation.");
        Require(string.Equals(dashboard.Calendar.Phase, expectedPhase, StringComparison.Ordinal), $"Dashboard calendar phase should be {expectedPhase}, got {dashboard.Calendar.Phase}.");
        Require(string.Equals(dashboard.Calendar.WeekLabel, expectedPhase, StringComparison.Ordinal), $"Dashboard calendar label should be {expectedPhase}, got {dashboard.Calendar.WeekLabel}.");
        Require(string.Equals(dashboard.NextGame.HeaderNextLabel, $"Next: {expectedPhase}", StringComparison.Ordinal), $"Unexpected offseason header label: {dashboard.NextGame.HeaderNextLabel}");
        Require(string.Equals(dashboard.NextGame.HeaderOpponentLabel, "Next opponent: TBD", StringComparison.Ordinal), $"Unexpected offseason opponent header: {dashboard.NextGame.HeaderOpponentLabel}");
        Require(string.Equals(dashboard.NextGame.Opponent, "TBD", StringComparison.Ordinal), $"Unexpected offseason opponent value: {dashboard.NextGame.Opponent}");
        Require(string.IsNullOrWhiteSpace(dashboard.NextGame.GameId), $"Offseason placeholder {expectedPhase} should not expose a next game id.");
        var actionItem = dashboard.ActionItems.FirstOrDefault(item =>
            string.Equals(item.Type, ScheduleService.GetOffseasonPhaseKey(expectedPhase), StringComparison.OrdinalIgnoreCase));
        Require(actionItem != null, $"Dashboard should expose an action item for {expectedPhase}.");
        Require(string.Equals(actionItem.Title, expectedPhase, StringComparison.Ordinal), $"Unexpected offseason action title for {expectedPhase}: {actionItem?.Title}");

        if (string.Equals(expectedPhase, ScheduleService.TrainingCampPendingPhase, StringComparison.Ordinal))
        {
            Require(string.Equals(actionItem.Description, "Training camp systems are not implemented yet.", StringComparison.Ordinal), $"Unexpected training camp placeholder description: {actionItem?.Description}");
            Require(string.Equals(actionItem.PrimaryAction, "Training camp systems are not implemented yet.", StringComparison.Ordinal), $"Unexpected training camp placeholder action text: {actionItem?.PrimaryAction}");
            return;
        }

        if (string.Equals(expectedPhase, ScheduleService.RetirementPendingPhase, StringComparison.Ordinal))
        {
            Require(string.Equals(actionItem.Description, "Retirement decisions pending.", StringComparison.Ordinal), $"Unexpected retirement placeholder description: {actionItem?.Description}");
            Require(string.Equals(actionItem.PrimaryAction, "Continue to process retirements", StringComparison.Ordinal), $"Unexpected retirement placeholder action text: {actionItem?.PrimaryAction}");
            return;
        }

        Require(string.Equals(actionItem.Description, $"{expectedPhase} is not implemented yet. Continue to move through the placeholder offseason flow.", StringComparison.Ordinal), $"Unexpected offseason placeholder description for {expectedPhase}: {actionItem?.Description}");
        Require(string.Equals(actionItem.PrimaryAction, "Continue to next offseason phase", StringComparison.Ordinal), $"Unexpected offseason placeholder action text for {expectedPhase}: {actionItem?.PrimaryAction}");
    }

    private static void ValidateOffseasonInvariants(
        GridironGM.GameCore.Models.LeagueState league,
        string championTeamId,
        string runnerUpTeamId,
        int expectedSeasonHistoryCount,
        int expectedScheduleCount,
        int expectedRegularSeasonResultCount,
        int expectedPlayoffResultCount,
        int expectedRetirementHistoryCount,
        int expectedRetiredPlayerCount)
    {
        Require(league != null, "League is required for offseason invariant validation.");
        Require(string.Equals(league.PlayoffBracket.LeagueChampionRecord.ChampionTeamId, championTeamId, StringComparison.OrdinalIgnoreCase), "Offseason placeholder flow should not change the champion team.");
        Require(string.Equals(league.PlayoffBracket.LeagueChampionRecord.RunnerUpTeamId, runnerUpTeamId, StringComparison.OrdinalIgnoreCase), "Offseason placeholder flow should not change the runner-up team.");
        Require(league.HistoricalSeasons.Count(record => record != null && record.SeasonYear == league.SeasonYear) == expectedSeasonHistoryCount, $"Offseason placeholder flow should preserve exactly {expectedSeasonHistoryCount} season history record(s) for the current season.");
        Require(league.Schedule.Count == expectedScheduleCount, $"Offseason placeholder flow should preserve {expectedScheduleCount} scheduled games.");
        Require(league.Results.Count(resultEntry => string.Equals(resultEntry.GameType, "regular_season", StringComparison.OrdinalIgnoreCase)) == expectedRegularSeasonResultCount, $"Offseason placeholder flow should preserve {expectedRegularSeasonResultCount} regular-season results.");
        Require(league.Results.Count(resultEntry => string.Equals(resultEntry.GameType, "playoffs", StringComparison.OrdinalIgnoreCase)) == expectedPlayoffResultCount, $"Offseason placeholder flow should preserve {expectedPlayoffResultCount} playoff results.");
        var actualRetirementHistoryCount = (league.RetirementHistory ?? new List<SeasonRetirementRecord>())
            .Count(record => record != null && record.SeasonYear == league.SeasonYear);
        var seasonRetirements = RetirementService.GetSeasonRetirementRecord(league, league.SeasonYear);
        var actualRetiredPlayerCount = seasonRetirements?.RetiredCount ?? 0;
        Require(actualRetirementHistoryCount == expectedRetirementHistoryCount, $"Offseason placeholder flow should preserve {expectedRetirementHistoryCount} retirement history record(s) for the current season.");
        Require(actualRetiredPlayerCount == expectedRetiredPlayerCount, $"Offseason placeholder flow should preserve {expectedRetiredPlayerCount} retired player record(s) for the current season.");
    }

    private static void ValidateRetirementResults(
        GridironGM.GameCore.Models.LeagueState league,
        SeasonRetirementRecord seasonRetirements)
    {
        Require(league != null, "League is required for retirement validation.");
        Require(seasonRetirements != null, "Season retirement history is required for retirement validation.");
        Require(seasonRetirements.Completed, "Season retirement history should be marked complete.");
        Require(string.Equals(seasonRetirements.ProcessedPhase, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase), $"Unexpected retirement processed phase: {seasonRetirements.ProcessedPhase}");
        Require(seasonRetirements.RetiredCount == seasonRetirements.Players.Count(record => record != null), "Retirement count should match persisted player retirement records.");

        var activePlayerIds = league.Teams
            .Where(team => team != null)
            .SelectMany(team => team.Roster ?? new List<GridironGM.GameCore.Models.PlayerState>())
            .Where(player => player != null)
            .Select(player => player.PlayerId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var retirement in seasonRetirements.Players.Where(record => record != null))
        {
            Require(!string.IsNullOrWhiteSpace(retirement.PlayerId), "Retirement records must preserve player ids.");
            Require(!activePlayerIds.Contains(retirement.PlayerId), $"Retired player {retirement.PlayerId} still appears on an active roster.");
            Require(string.Equals(retirement.RetiredDuringPhase, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase), $"Unexpected retirement phase marker for {retirement.PlayerId}: {retirement.RetiredDuringPhase}");
        }
    }

    private static string SnapshotRegularSeasonStandings(StandingsResponse standings)
    {
        Require(standings != null && standings.Ok, standings?.Error ?? "Standings snapshot unavailable.");
        return string.Join("|", standings.Standings
            .OrderBy(row => row.TeamId, StringComparer.OrdinalIgnoreCase)
            .Select(row => $"{row.TeamId}:{row.Wins}-{row.Losses}-{row.Ties}:{row.PointsFor}-{row.PointsAgainst}"));
    }

    private static string NormalizeResultsSeasonKey(string gameType)
    {
        return (gameType ?? "").Trim().ToLowerInvariant() switch
        {
            "preseason" => "preseason",
            "regular_season" => "regular",
            "playoffs" => "playoffs",
            "postseason" => "playoffs",
            _ => "regular",
        };
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
