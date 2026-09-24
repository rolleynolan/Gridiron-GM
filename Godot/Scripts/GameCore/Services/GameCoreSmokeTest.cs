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
    public static GameCoreSmokeTestResult Run(string teamSeedPath = null)
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
            var contractService = new ContractService(context);
            var saveService = new GameCoreSaveService();
            const string smokeSaveName = "native_smoke_test_save.json";

            currentStep = "Bootstrap league";
            var league = bootstrap.CreateTestLeague(teamSeedPath);
            Require(league != null && context.ActiveLeague != null && league.Teams.Count == LeagueBootstrapService.TeamCount, $"Bootstrap should create {LeagueBootstrapService.TeamCount} teams.");
            Require(league.Results.Count == 0, "Fresh bootstrap should not seed completed results.");
            Require(league.SalaryCap == LeagueState.DefaultSalaryCap, "Fresh league should use the configured salary cap.");
            Require(league.Teams.All(team => team.Roster.All(player => player.Contract != null && player.Contract.AnnualSalary > 0m && player.Contract.YearsRemaining > 0)), "Every rostered player should begin with an active contract.");
            Require(league.Teams.All(team => team.Roster.All(player => player.Morale >= 0 && player.Morale <= 100)), "Every rostered player should begin with valid morale.");
            Require(league.Teams.All(team => team.Roster.All(player => !string.IsNullOrWhiteSpace(player.Trait))), "Every generated rostered player should begin with a deterministic trait.");
            Require(league.Teams.All(team => Math.Abs(contractService.GetCapRoom(team) - team.CapRoom) < 1m), "Team cap room should match active contract commitments.");
            Require(league.FreeAgents.Count == 192 && league.FreeAgents.All(player => string.Equals(player.Status, "Free Agent", StringComparison.OrdinalIgnoreCase)), "Fresh league should include a free-agent pool.");
            Require(league.Teams.All(team => team.Coaches != null && team.Coaches.Count == LeagueBootstrapService.CoachesPerTeam), "Each team should start with a complete coaching staff.");
            Require(league.AvailableCoaches.Count == 40 && league.AvailableCoaches.All(coach => string.Equals(coach.Role, "Available Staff", StringComparison.OrdinalIgnoreCase)), "Fresh league should include a persisted staff market.");
            var staffService = new StaffService(context);
            var staffTeam = league.Teams.First(team => team.TeamId == league.UserTeamId);
            var staffCoach = staffTeam.Coaches.First();
            var closedStaffChange = staffService.ReleaseCoach(staffTeam.TeamId, staffCoach.CoachId);
            Require(!closedStaffChange.Ok && closedStaffChange.Message.Contains("Staff Carousel", StringComparison.OrdinalIgnoreCase), "Staff changes should reject attempts outside the staff-carousel phase.");
            league.Calendar.Phase = ScheduleService.StaffCarouselPendingPhase;
            var releasedRole = staffCoach.Role;
            var releasedStaffChange = staffService.ReleaseCoach(staffTeam.TeamId, staffCoach.CoachId);
            Require(releasedStaffChange.Ok && !staffTeam.Coaches.Any(coach => coach.CoachId == staffCoach.CoachId) && league.AvailableCoaches.Any(coach => coach.CoachId == staffCoach.CoachId), "Released staff should leave the team and enter the staff market.");
            var hireCandidate = league.AvailableCoaches.First(coach => coach.CoachId != staffCoach.CoachId);
            var hiredStaffChange = staffService.HireCoach(staffTeam.TeamId, releasedRole, hireCandidate.CoachId);
            Require(hiredStaffChange.Ok && staffTeam.Coaches.Any(coach => coach.CoachId == hireCandidate.CoachId && coach.Role == releasedRole) && !league.AvailableCoaches.Any(coach => coach.CoachId == hireCandidate.CoachId), "Hiring should fill only the vacant role and remove the candidate from the staff market.");
            Require(league.Transactions.Any(transaction => transaction.Type == "staff_released" && transaction.StaffId == staffCoach.CoachId) && league.Transactions.Any(transaction => transaction.Type == "staff_hired" && transaction.StaffId == hireCandidate.CoachId), "Staff changes should write persisted transaction records.");
            league.Calendar.Phase = "Preseason";
            var developmentProbe = new PlayerState { Overall = 70, Potential = 80, Age = 24 };
            PlayerDevelopmentService.ApplyAnnualDevelopment(developmentProbe, 1);
            Require(developmentProbe.Overall == 72, "An elite Head Coach development bonus should add no more than one annual development point.");
            Require(league.CollegeProspects.Count == LeagueBootstrapService.StartingProspectCount, "Fresh world should include the full starting college prospect class.");
            Require(league.CollegeUniverse != null && league.CollegeUniverse.Teams.Count == CollegeTeamCatalog.TeamCount && league.CollegeUniverse.Players.Count > league.CollegeProspects.Count && league.CollegeUniverse.Schedule.Count == SimulationBenchmarkService.ProjectedCollegeSeasonGames, "Fresh world should include the full persisted college competition, players, and schedule.");
            var collegeService = new CollegeUniverseService(context);
            collegeService.AdvanceToProWeek(1);
            var collegeWeekOneResults = league.CollegeUniverse.Results.Count;
            Require(collegeWeekOneResults > 0 && league.CollegeUniverse.Teams.All(team => team.Ranking > 0) && league.CollegeUniverse.Players.Any(player => player.GamesPlayed > 0), "College weekly advancement should resolve results, rankings, and player statistics.");
            Require(league.CollegeUniverse.Teams.All(team => league.CollegeUniverse.Players.Any(player => player.TeamId == team.TeamId && player.IsRedshirted && player.CollegeYear == 1 && player.PlayableSeasonsUsed == 0 && player.GamesPlayed == 0)), "Every college program should carry a persisted first-year redshirt who does not consume playable eligibility or game statistics.");
            collegeService.AdvanceToProWeek(1);
            Require(league.CollegeUniverse.Results.Count == collegeWeekOneResults, "College weekly advancement should be idempotent for an already processed pro week.");
            const string collegeSaveName = "native_smoke_college_universe.json";
            Require(saveService.Save(context, collegeSaveName).Ok, "College-universe smoke save should succeed.");
            var collegeLoad = saveService.Load(collegeSaveName);
            Require(collegeLoad.Ok && collegeLoad.League.CollegeUniverse.Results.Count == collegeWeekOneResults && collegeLoad.League.CollegeUniverse.Teams.All(team => team.Ranking > 0) && collegeLoad.League.CollegeUniverse.Players.Count(player => player.IsRedshirted) == CollegeTeamCatalog.TeamCount && collegeLoad.League.Teams.First(team => team.TeamId == staffTeam.TeamId).Coaches.Any(coach => coach.CoachId == hireCandidate.CoachId) && collegeLoad.League.Transactions.Any(transaction => transaction.Type == "staff_hired" && transaction.StaffId == hireCandidate.CoachId), "College results, redshirt eligibility, staff changes, and rankings should persist through save/load.");
            Require(saveService.Delete(collegeSaveName).Ok, "College-universe smoke save should clean up.");
            var legacyCollegeContext = new GameCoreContext();
            var legacyCollegeLeague = new LeagueBootstrapService(legacyCollegeContext).CreateTestLeague(teamSeedPath);
            legacyCollegeLeague.CollegeUniverse = null;
            legacyCollegeLeague.SaveVersion = LeagueState.CurrentSaveVersion - 1;
            const string collegeMigrationSaveName = "native_smoke_college_migration.json";
            Require(saveService.Save(legacyCollegeContext, collegeMigrationSaveName).Ok, "Legacy college migration smoke save should succeed.");
            var migratedCollegeLoad = saveService.Load(collegeMigrationSaveName);
            Require(migratedCollegeLoad.Ok && migratedCollegeLoad.League.CollegeUniverse?.Teams.Count == CollegeTeamCatalog.TeamCount && migratedCollegeLoad.League.SaveVersion == LeagueState.CurrentSaveVersion, "Legacy saves should receive a normalized persisted college universe.");
            Require(saveService.Delete(collegeMigrationSaveName).Ok, "College migration smoke save should clean up.");
            Pass(result, "College universe foundation");
            currentStep = "College league leaders foundation";
            ValidateCollegeLeaders(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College postseason projections foundation";
            ValidateCollegePostseasonProjections(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College player development foundation";
            ValidateCollegePlayerDevelopment(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College news foundation";
            ValidateCollegeNews(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College player injuries foundation";
            ValidateCollegePlayerInjuries(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "Public college big-board foundation";
            ValidateCollegeBigBoards(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College postseason simulation foundation";
            ValidateCollegePostseason(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College awards foundation";
            ValidateCollegeAwards(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "College draft-pipeline lifecycle";
            ValidateCollegeDraftPipeline(teamSeedPath);
            Pass(result, currentStep);
            currentStep = "Bootstrap league";
            var draftEvaluationService = new ProspectEvaluationService(context);
            var draftEvaluation = draftEvaluationService.GetEvaluation(league.CollegeProspects.First().ProspectId);
            var repeatDraftEvaluation = draftEvaluationService.GetEvaluation(league.CollegeProspects.First().ProspectId);
            Require(draftEvaluation != null && draftEvaluation.KnownFacts.Contains("Public combine", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(draftEvaluation.EstimatedOverall) && !string.IsNullOrWhiteSpace(draftEvaluation.EstimatedPotential) && !string.IsNullOrWhiteSpace(draftEvaluation.Confidence) && !string.IsNullOrWhiteSpace(draftEvaluation.Report) && !string.IsNullOrWhiteSpace(draftEvaluation.Trait) && !string.IsNullOrWhiteSpace(draftEvaluation.Interview), "Prospect evaluation should distinguish public information from complete scouting estimates.");
            Require(string.Equals(draftEvaluation.Report, repeatDraftEvaluation.Report, StringComparison.Ordinal) && string.Equals(draftEvaluation.EstimatedOverall, repeatDraftEvaluation.EstimatedOverall, StringComparison.Ordinal), "Prospect evaluation should be deterministic across repeated draft-board preparation.");
            const string evaluationSaveName = "native_smoke_prospect_evaluation.json";
            Require(saveService.Save(context, evaluationSaveName).Ok, "Prospect evaluation smoke save should succeed.");
            var evaluationLoad = saveService.Load(evaluationSaveName);
            Require(evaluationLoad.Ok && evaluationLoad.League.CollegeProspects.First(prospect => prospect.ProspectId == draftEvaluation.ProspectId).ScoutingReport == league.CollegeProspects.First(prospect => prospect.ProspectId == draftEvaluation.ProspectId).ScoutingReport, "Prospect evaluation should persist through save/load.");
            Require(saveService.Delete(evaluationSaveName).Ok, "Prospect evaluation smoke save should clean up.");
            Require(league.FranchiseMetadata.World.Source == RosterSource.Standard && league.FranchiseMetadata.World.Seed == WorldDefinition.StandardSeed, "Default new game should use the fixed Standard roster seed.");
            var baselineWorld = new LeagueBootstrapService(new GameCoreContext()).CreateTestLeague(teamSeedPath, WorldDefinition.Standard());
            var matchingWorld = new LeagueBootstrapService(new GameCoreContext()).CreateTestLeague(teamSeedPath, WorldDefinition.Standard());
            var generatedWorld = new LeagueBootstrapService(new GameCoreContext()).CreateTestLeague(teamSeedPath, WorldDefinition.Generated(987654321UL));
            Require(string.Equals(SnapshotWorldPopulation(baselineWorld), SnapshotWorldPopulation(matchingWorld), StringComparison.Ordinal), "Standard roster generation should be repeatable.");
            Require(!string.Equals(SnapshotWorldPopulation(baselineWorld), SnapshotWorldPopulation(generatedWorld), StringComparison.Ordinal), "Generated roster seed should produce a different starting world.");
            if (!string.IsNullOrWhiteSpace(teamSeedPath))
            {
                Require(league.Teams.Any(team => string.Equals(team.Name, "Chicago Cyclones", StringComparison.Ordinal)), "Seeded team data did not load Chicago Cyclones.");
                Require(league.Teams.Any(team => string.Equals(team.Abbreviation, "ATL", StringComparison.OrdinalIgnoreCase)), "Seeded team data did not load team abbreviations.");
            }
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

            currentStep = "Injury depth advisory";
            ValidateInjuryDepthAdvisory(teamSeedPath);
            Pass(result, currentStep);

            currentStep = "AI roster management foundation";
            ValidateCpuRosterManagement(teamSeedPath);
            Pass(result, currentStep);

            currentStep = "Contract negotiation foundation";
            var contractContext = new GameCoreContext();
            var contractLeague = new LeagueBootstrapService(contractContext).CreateTestLeague(teamSeedPath);
            var contractTestService = new ContractService(contractContext);
            var contractTeam = contractLeague.Teams.OrderByDescending(contractTestService.GetCapRoom).First();
            var playerHistoryService = new PlayerHistoryService(contractContext);
            var legacyHistoryPlayer = contractTeam.Roster.First();
            legacyHistoryPlayer.SeasonStats = null;
            legacyHistoryPlayer.CareerStats = null;
            var legacyHistory = playerHistoryService.GetPlayerHistory(legacyHistoryPlayer.PlayerId, contractTeam.TeamId);
            Require(legacyHistory.Ok && legacyHistory.CurrentSeason.SeasonYear == contractLeague.SeasonYear && legacyHistory.CareerSeasons.Count == 0, "Player history should safely provide an empty current season for legacy players without statistics.");
            legacyHistoryPlayer.SeasonStats = new PlayerSeasonStats { SeasonYear = contractLeague.SeasonYear, GamesPlayed = 2, RushingYards = 40 };
            legacyHistoryPlayer.CareerStats = new List<PlayerSeasonStats>
            {
                new() { SeasonYear = contractLeague.SeasonYear - 2, GamesPlayed = 12, RushingYards = 500 },
                new() { SeasonYear = contractLeague.SeasonYear - 1, GamesPlayed = 15, RushingYards = 700 },
            };
            var multiSeasonHistory = playerHistoryService.GetPlayerHistory(legacyHistoryPlayer.PlayerId, contractTeam.TeamId);
            Require(multiSeasonHistory.CurrentSeason.GamesPlayed == 2 && multiSeasonHistory.CareerSeasons.Select(stats => stats.SeasonYear).SequenceEqual(new[] { contractLeague.SeasonYear - 1, contractLeague.SeasonYear - 2 }), "Player history should separate live totals from ordered archived seasons.");
            const string playerHistorySaveName = "native_smoke_player_history.json";
            Require(saveService.Save(contractContext, playerHistorySaveName).Ok, "Player-history smoke save should succeed.");
            var playerHistoryLoad = saveService.Load(playerHistorySaveName);
            var loadedPlayerHistory = new PlayerHistoryService(new GameCoreContext { ActiveLeague = playerHistoryLoad.League }).GetPlayerHistory(legacyHistoryPlayer.PlayerId, contractTeam.TeamId);
            Require(playerHistoryLoad.Ok && loadedPlayerHistory.Ok && loadedPlayerHistory.CareerSeasons.Count == 2 && loadedPlayerHistory.CurrentSeason.RushingYards == 40, "Player history should persist live and archived totals through save/load.");
            Require(saveService.Delete(playerHistorySaveName).Ok, "Player-history smoke save should clean up.");
            var contractPlayer = contractLeague.FreeAgents.First();
            var preseasonContractStatus = ContractPhaseRules.GetStatus(contractLeague);
            Require(preseasonContractStatus.CanSignFreeAgents && !preseasonContractStatus.CanOfferExtensions, "Preseason contract policy should allow signings but close extensions.");
            var requiredSalary = contractTestService.GetRequiredAnnualSalary(contractPlayer, contractTeam);
            var declined = contractTestService.SignFreeAgent(contractPlayer.PlayerId, contractTeam.TeamId, new ContractOffer
            {
                AnnualSalary = requiredSalary * 0.5m,
                GuaranteedSalary = 0m,
                Years = 2,
            });
            Require(declined.Ok && !declined.Accepted, "Low free-agent offer should be declined without a transaction.");
            Require(contractLeague.Transactions.Count == 0, "Declined offers must not create a transaction record.");
            var releasedPlayer = contractTeam.Roster.First();
            var releasePreview = contractTestService.PreviewRelease(releasedPlayer.PlayerId, contractTeam.TeamId);
            Require(releasePreview.Ok
                    && releasePreview.RosterCountAfter == releasePreview.RosterCountBefore - 1
                    && releasePreview.PayrollAfter == releasePreview.PayrollBefore - releasedPlayer.Contract.AnnualSalary
                    && releasePreview.CapRoomAfter == Math.Min(contractLeague.SalaryCap, releasePreview.CapRoomBefore + releasedPlayer.Contract.AnnualSalary)
                    && contractTeam.Roster.Contains(releasedPlayer)
                    && contractLeague.Transactions.Count == 0,
                "Release preview should report authoritative roster/payroll/cap effects without mutating league state.");
            var released = contractTestService.ReleasePlayer(releasedPlayer.PlayerId, contractTeam.TeamId);
            Require(released.Ok && released.Accepted, released.Message);
            Require(contractTeam.Roster.Count == releasePreview.RosterCountAfter && contractTestService.GetCapRoom(contractTeam) == releasePreview.CapRoomAfter, "Confirmed release should match its previewed roster and cap effects.");
            Require(contractLeague.Transactions.Count == 1 && string.Equals(contractLeague.Transactions[0].Type, "player_released", StringComparison.Ordinal), "Release should create a transaction record.");
            var rosterCountBeforeSigning = contractTeam.Roster.Count;
            var accepted = contractTestService.SignFreeAgent(contractPlayer.PlayerId, contractTeam.TeamId, new ContractOffer
            {
                AnnualSalary = requiredSalary * 1.15m,
                GuaranteedSalary = requiredSalary * 0.30m,
                Years = 2,
            });
            Require(accepted.Ok && accepted.Accepted, accepted.Message);
            contractLeague.Calendar.Phase = ScheduleService.ExclusiveNegotiationPendingPhase;
            var exclusiveSigning = contractTestService.SignFreeAgent(contractLeague.FreeAgents.First().PlayerId, contractTeam.TeamId, new ContractOffer
            {
                AnnualSalary = requiredSalary,
                GuaranteedSalary = requiredSalary * 0.30m,
                Years = 1,
            });
            Require(!exclusiveSigning.Ok && exclusiveSigning.Message.Contains("Exclusive negotiation", StringComparison.OrdinalIgnoreCase), "Exclusive negotiation should block free-agent signing with an explainable phase message.");
            contractLeague.Calendar.Phase = "Preseason";
            Require(contractTeam.Roster.Count == rosterCountBeforeSigning + 1 && !contractLeague.FreeAgents.Any(player => player.PlayerId == contractPlayer.PlayerId), "Accepted free agent should move into the signing team's roster.");
            Require(Math.Abs(contractTestService.GetCapRoom(contractTeam) - contractTeam.CapRoom) < 1m, "Signing should refresh the team's cap room.");
            Require(contractLeague.Transactions.Count == 2 && string.Equals(contractLeague.Transactions[1].Type, "free_agent_signed", StringComparison.Ordinal), "Accepted signing should create a transaction record.");
            var waived = contractTestService.ReleasePlayer(contractTeam.Roster.First().PlayerId, contractTeam.TeamId);
            Require(waived.Ok && waived.Accepted, waived.Message);
            var waiverPlayer = contractTeam.Roster.First();
            var waiverResult = new TransactionService(contractContext).PlaceOnWaivers(waiverPlayer.PlayerId, contractTeam.TeamId, contractTestService);
            Require(waiverResult.Ok && waiverResult.Accepted && contractLeague.Waivers.Count == 1, "Waived player should leave the active roster and enter the waiver pool.");
            contractLeague.Waivers.Single(waiver => waiver.Player.PlayerId == waiverPlayer.PlayerId).Claims.Clear();
            var waiverPriority = new StandingsService(contractContext).BuildStandings(contractLeague).AsEnumerable().Reverse().Select(standing => standing.TeamId).Where(teamId => teamId != contractTeam.TeamId).ToList();
            var claimTeam = contractLeague.Teams.First(team => team.TeamId == waiverPriority[0]);
            Require(contractTestService.ReleasePlayer(claimTeam.Roster.First().PlayerId, claimTeam.TeamId).Accepted, "Claiming team should be able to create an active-roster opening.");
            var originalUserTeamId = contractLeague.UserTeamId;
            contractLeague.UserTeamId = claimTeam.TeamId;
            var waiverTransactions = new TransactionService(contractContext);
            var conditionalReleasePlayer = claimTeam.Roster.First();
            var claimResult = waiverTransactions.SubmitWaiverClaim(waiverPlayer.PlayerId, claimTeam.TeamId, contractTestService, conditionalReleasePlayer.PlayerId);
            var pendingWaiver = contractLeague.Waivers.Single(waiver => waiver.Player.PlayerId == waiverPlayer.PlayerId);
            Require(claimResult.Ok && claimResult.Accepted && !pendingWaiver.PendingConfirmation && pendingWaiver.Claims.Any(claim => claim.TeamId == claimTeam.TeamId && claim.ConditionalReleasePlayerId == conditionalReleasePlayer.PlayerId) && claimTeam.Roster.Contains(conditionalReleasePlayer) && !claimTeam.Roster.Any(player => player.PlayerId == waiverPlayer.PlayerId), "Submitting a waiver claim should persist its queue entry and conditional release without moving either player.");
            contractLeague.Calendar.AbsoluteWeek++;
            ScheduleService.NormalizeCalendar(contractLeague.Calendar);
            Require(waiverTransactions.ExpireWaivers() == 0 && pendingWaiver.PendingConfirmation && pendingWaiver.PendingClaimTeamId == claimTeam.TeamId && pendingWaiver.ConditionalReleasePlayerId == conditionalReleasePlayer.PlayerId, "Closing the waiver period should resolve the original queue and pause for the winning user claim.");
            var waiverDashboard = new DashboardService(contractContext).GetDashboardState();
            Require(waiverDashboard.Dashboard.ActionItems.Any(item => item.Type == "waiver_claim_confirmation"), "A pending winning waiver claim should appear as an Action Required dashboard item.");
            var waiverContinue = new ContinueService(contractContext).Continue();
            Require(waiverContinue.Ok && !waiverContinue.Result.Advanced && waiverContinue.Result.StopReason == "waiver_claim_confirmation", "A pending waiver confirmation should block time advancement.");
            const string pendingWaiverSaveName = "native_smoke_pending_waiver.json";
            Require(saveService.Save(contractContext, pendingWaiverSaveName).Ok, "Pending waiver confirmation should save.");
            var loadedPendingWaiver = saveService.Load(pendingWaiverSaveName);
            Require(loadedPendingWaiver.Ok && loadedPendingWaiver.League.Waivers.Any(waiver => waiver.Player.PlayerId == waiverPlayer.PlayerId && waiver.PendingConfirmation && waiver.PendingClaimTeamId == claimTeam.TeamId && waiver.ConditionalReleasePlayerId == conditionalReleasePlayer.PlayerId), "Pending waiver confirmation and its conditional release should survive save/load.");
            Require(saveService.Delete(pendingWaiverSaveName).Ok, "Pending waiver confirmation smoke save should clean up.");
            var finalizedClaim = waiverTransactions.FinalizeWaiverClaim(waiverPlayer.PlayerId, claimTeam.TeamId, contractTestService);
            Require(finalizedClaim.Ok && finalizedClaim.Accepted && claimTeam.Roster.Any(player => player.PlayerId == waiverPlayer.PlayerId) && !claimTeam.Roster.Contains(conditionalReleasePlayer) && contractLeague.FreeAgents.Contains(conditionalReleasePlayer) && !contractLeague.Waivers.Any(waiver => waiver.Player.PlayerId == waiverPlayer.PlayerId), "Final confirmation should atomically execute the conditional release and waiver transfer.");
            Require(contractLeague.Transactions.Any(transaction => transaction.Type == "player_released" && transaction.PlayerId == conditionalReleasePlayer.PlayerId), "A finalized conditional waiver release should create a transaction record.");
            var queuedWaiverPlayer = contractTeam.Roster.First();
            Require(waiverTransactions.PlaceOnWaivers(queuedWaiverPlayer.PlayerId, contractTeam.TeamId, contractTestService).Accepted, "Second waiver placement should succeed for queue validation.");
            contractLeague.Waivers.Single(waiver => waiver.Player.PlayerId == queuedWaiverPlayer.PlayerId).Claims.Clear();
            var cancelledConditionalRelease = claimTeam.Roster.First(player => player.PlayerId != waiverPlayer.PlayerId);
            var nextClaimTeam = contractLeague.Teams.First(team => team.TeamId == waiverPriority[1]);
            var nextConditionalRelease = nextClaimTeam.Roster.First();
            Require(waiverTransactions.SubmitWaiverClaim(queuedWaiverPlayer.PlayerId, claimTeam.TeamId, contractTestService, cancelledConditionalRelease.PlayerId).Accepted, "User queue entry should succeed for cancellation validation.");
            Require(waiverTransactions.SubmitWaiverClaim(queuedWaiverPlayer.PlayerId, nextClaimTeam.TeamId, contractTestService, nextConditionalRelease.PlayerId).Accepted, "A competing CPU queue entry should persist before resolution.");
            contractLeague.Calendar.AbsoluteWeek++;
            ScheduleService.NormalizeCalendar(contractLeague.Calendar);
            Require(waiverTransactions.ExpireWaivers() == 0, "The user should receive the first winning opportunity in the original waiver order.");
            var cancelledClaim = waiverTransactions.CancelWaiverClaim(queuedWaiverPlayer.PlayerId, claimTeam.TeamId, contractTestService);
            Require(cancelledClaim.Ok && cancelledClaim.Accepted && claimTeam.Roster.Contains(cancelledConditionalRelease) && nextClaimTeam.Roster.Any(player => player.PlayerId == queuedWaiverPlayer.PlayerId) && !nextClaimTeam.Roster.Contains(nextConditionalRelease) && !contractLeague.Waivers.Any(waiver => waiver.Player.PlayerId == queuedWaiverPlayer.PlayerId), "Cancelling the first opportunity should preserve its conditional release and pass the original queue to the next eligible CPU claimant.");
            var cpuMarketPlayer = contractTeam.Roster.First();
            cpuMarketPlayer.Overall = 99;
            Require(waiverTransactions.PlaceOnWaivers(cpuMarketPlayer.PlayerId, contractTeam.TeamId, contractTestService).Accepted, "High-value waiver placement should succeed for CPU claim-generation validation.");
            var cpuMarketWaiver = contractLeague.Waivers.Single(waiver => waiver.Player.PlayerId == cpuMarketPlayer.PlayerId);
            Require(cpuMarketWaiver.Claims.Count > 0 && cpuMarketWaiver.Claims.All(claim => claim.TeamId != contractLeague.UserTeamId && claim.TeamId != contractTeam.TeamId), "CPU teams should submit a bounded set of explainable claims without controlling the user team or waiving team.");
            contractLeague.Calendar.AbsoluteWeek++;
            ScheduleService.NormalizeCalendar(contractLeague.Calendar);
            Require(waiverTransactions.ExpireWaivers() == 1 && contractLeague.Teams.Any(team => team.TeamId != contractTeam.TeamId && team.Roster.Any(player => player.PlayerId == cpuMarketPlayer.PlayerId)), "The highest-priority valid CPU claimant should finalize the resolved waiver.");
            var expiringWaiverPlayer = contractTeam.Roster.First();
            expiringWaiverPlayer.Overall = 1;
            Require(waiverTransactions.PlaceOnWaivers(expiringWaiverPlayer.PlayerId, contractTeam.TeamId, contractTestService).Accepted, "Unclaimed waiver placement should succeed for expiry validation.");
            contractLeague.Waivers.Single(waiver => waiver.Player.PlayerId == expiringWaiverPlayer.PlayerId).Claims.Clear();
            contractLeague.UserTeamId = originalUserTeamId;
            contractLeague.Calendar.AbsoluteWeek++;
            ScheduleService.NormalizeCalendar(contractLeague.Calendar);
            Require(waiverTransactions.ExpireWaivers() == 1 && contractLeague.FreeAgents.Any(player => player.PlayerId == expiringWaiverPlayer.PlayerId), "Expired waivers should enter free agency.");
            var depthChartForInjury = new DepthChartService(contractContext);
            Require(depthChartForInjury.AutoFillDepthChart(contractTeam.TeamId).Ok, "Depth chart should auto-fill before injury validation.");
            var substitutionPosition = contractTeam.Roster
                .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
                .First(group => group.Count() > DepthChartRules.GetRequiredStarters(group.Key)).Key;
            var unavailablePlayer = contractTeam.Roster.First(player => string.Equals(player.Position, substitutionPosition, StringComparison.OrdinalIgnoreCase));
            PlayerInjuryService.InjurePlayer(contractLeague, unavailablePlayer, "Hamstring strain", 2, "smoke-injury");
            Require(!PlayerInjuryService.IsAvailableForGame(unavailablePlayer) && unavailablePlayer.InjuryHistory.Count == 1, "Injury should create an unavailable player and history record.");
            var injuryDepthChart = depthChartForInjury.GetTeamDepthChart(contractTeam.TeamId);
            var injuryPositionGroup = injuryDepthChart.Positions.First(position => string.Equals(position.Position, substitutionPosition, StringComparison.OrdinalIgnoreCase));
            Require(injuryDepthChart.DepthChartStatus.IsValid
                    && injuryPositionGroup.Players.Any(player => player.PlayerId == unavailablePlayer.PlayerId && !player.IsAvailable && player.Role == "Unavailable")
                    && injuryPositionGroup.Players.Count(player => player.IsAvailable && player.Role == "Starter") >= injuryPositionGroup.RequiredStarters,
                "Depth chart should retain visible injury context while substituting an available backup.");
            var injuryGame = GameDayService.SimulateMatchup(contractLeague, "smoke-injury-game", contractTeam.TeamId, contractLeague.Teams.First(team => team.TeamId != contractTeam.TeamId).TeamId, 1, 1, "Preseason", "preseason", "Preseason Week 1", 0, 3, true);
            Require(injuryGame.BoxScore.PlayerStats.All(line => line.PlayerId != unavailablePlayer.PlayerId), "Unavailable players should not receive game stat lines.");
            PlayerInjuryService.RecoverOneDay(contractLeague);
            Require(unavailablePlayer.CurrentInjury.DaysRemaining == 1, "Injury recovery should advance one day at a time.");
            PlayerInjuryService.RecoverOneDay(contractLeague);
            Require(PlayerInjuryService.IsAvailableForGame(unavailablePlayer) && unavailablePlayer.InjuryHistory.Single().RecoveredOn == contractLeague.Calendar.CurrentDate, "Recovered players should become available and retain injury history.");
            var medicalDirector = contractTeam.Coaches.First(coach => string.Equals(coach.Role, "Medical Director", StringComparison.OrdinalIgnoreCase));
            medicalDirector.Overall = 85;
            var medicalRecoveryPlayer = contractTeam.Roster.First(player => player.PlayerId != unavailablePlayer.PlayerId);
            PlayerInjuryService.InjurePlayer(contractLeague, medicalRecoveryPlayer, "Ankle sprain", 2, "smoke-medical");
            PlayerInjuryService.RecoverOneDay(contractLeague);
            Require(PlayerInjuryService.IsAvailableForGame(medicalRecoveryPlayer), "An elite Medical Director should remove at most one additional recovery day.");
            var conditioningCoach = contractTeam.Coaches.First(coach => string.Equals(coach.Role, "Strength & Conditioning Coach", StringComparison.OrdinalIgnoreCase));
            conditioningCoach.Overall = 85;
            var conditioningPlayer = contractTeam.Roster.First(player => player.PlayerId != unavailablePlayer.PlayerId && player.PlayerId != medicalRecoveryPlayer.PlayerId);
            conditioningPlayer.Fatigue = 10;
            PlayerStatisticsService.RecoverOneDay(contractLeague);
            Require(conditioningPlayer.Fatigue == 5, "An elite Strength & Conditioning Coach should add at most one point to daily fatigue recovery.");
            var deterministicInjuryResult = new GameResult { GameId = "a", BoxScore = injuryGame.BoxScore };
            PlayerInjuryService.ApplyDeterministicGameInjuries(contractLeague, deterministicInjuryResult);
            Require(contractLeague.Teams.SelectMany(team => team.Roster).Any(player => player.CurrentInjury.IsActive && player.CurrentInjury.GameId == "a"), "Deterministic game injury generation should create an injury.");
            for (var recoveryDay = 0; recoveryDay < 14; recoveryDay++)
                PlayerInjuryService.RecoverOneDay(contractLeague);
            var injuredPlayer = contractTeam.Roster.First(player => player.PlayerId != unavailablePlayer.PlayerId);
            PlayerInjuryService.InjurePlayer(contractLeague, injuredPlayer, "Knee sprain", 1, "smoke-ir");
            var irResult = new TransactionService(contractContext).MoveToInjuredReserve(injuredPlayer.PlayerId, contractTeam.TeamId, contractTestService);
            Require(irResult.Ok && irResult.Accepted && contractTeam.InjuredReserve.Count == 1, "Injured player should move to injured reserve.");
            PlayerInjuryService.RecoverOneDay(contractLeague);
            var activateResult = new TransactionService(contractContext).ActivateFromInjuredReserve(injuredPlayer.PlayerId, contractTeam.TeamId, contractTestService);
            Require(activateResult.Ok && activateResult.Accepted && contractTeam.InjuredReserve.Count == 0, "Cleared injured-reserve player should activate into an open roster slot.");
            var practiceSquadPlayer = contractLeague.FreeAgents.First(player => player.Age <= 25);
            practiceSquadPlayer.Overall = Math.Max(75, practiceSquadPlayer.Overall);
            var practiceSquadTransactions = new TransactionService(contractContext);
            var practiceSquadAsk = practiceSquadTransactions.GetPracticeSquadRequiredSalary(practiceSquadPlayer.PlayerId, contractTeam.TeamId);
            var declinedPracticeSquadOffer = practiceSquadTransactions.SignToPracticeSquad(practiceSquadPlayer.PlayerId, contractTeam.TeamId, contractTestService, practiceSquadAsk - 25_000m);
            Require(declinedPracticeSquadOffer.Ok && !declinedPracticeSquadOffer.Accepted && contractLeague.FreeAgents.Contains(practiceSquadPlayer), "An eligible player should be able to decline an under-market practice-squad offer without moving pools.");
            var practiceSquadResult = practiceSquadTransactions.SignToPracticeSquad(practiceSquadPlayer.PlayerId, contractTeam.TeamId, contractTestService, practiceSquadAsk);
            Require(practiceSquadResult.Ok && practiceSquadResult.Accepted && contractTeam.PracticeSquad.Count == 1 && practiceSquadPlayer.Contract.AnnualSalary == practiceSquadAsk, "An eligible player should accept a sufficient practice-squad offer and retain its negotiated salary.");
            const string practiceSquadOfferSaveName = "native_smoke_practice_squad_offer.json";
            Require(saveService.Save(contractContext, practiceSquadOfferSaveName).Ok, "Accepted practice-squad terms should save.");
            var loadedPracticeSquadOffer = saveService.Load(practiceSquadOfferSaveName);
            var loadedPracticeSquadPlayer = loadedPracticeSquadOffer.League?.Teams.FirstOrDefault(team => team.TeamId == contractTeam.TeamId)?.PracticeSquad.FirstOrDefault(player => player.PlayerId == practiceSquadPlayer.PlayerId);
            Require(loadedPracticeSquadOffer.Ok && loadedPracticeSquadPlayer?.Contract?.ContractType == "Practice Squad" && loadedPracticeSquadPlayer.Contract.AnnualSalary == practiceSquadAsk, "Accepted practice-squad salary and destination should survive save/load.");
            Require(saveService.Delete(practiceSquadOfferSaveName).Ok, "Practice-squad offer smoke save should clean up.");
            var activeSigningPreview = contractTestService.PreviewPracticeSquadActiveSigning(practiceSquadPlayer.PlayerId, contractTeam.TeamId);
            Require(activeSigningPreview.Ok && activeSigningPreview.RosterCountAfter == activeSigningPreview.RosterCountBefore + 1 && contractTeam.PracticeSquad.Contains(practiceSquadPlayer), "Permanent practice-squad promotion preview should be read-only and expose roster consequences.");
            var elevationResult = contractTestService.SignPracticeSquadPlayerToActiveRoster(practiceSquadPlayer.PlayerId, contractTeam.TeamId);
            Require(elevationResult.Ok && elevationResult.Accepted && contractTeam.PracticeSquad.Count == 0 && contractTeam.Roster.Contains(practiceSquadPlayer) && practiceSquadPlayer.Contract.ContractType == "Active Roster" && practiceSquadPlayer.Contract.AnnualSalary >= TransactionService.ActiveRosterMinimumSalary, "Practice-squad player should receive a permanent active-roster contract after explicit confirmation.");
            var transactionHistory = new DashboardService(contractContext).GetTransactionHistory();
            Require(transactionHistory.Ok && transactionHistory.Transactions.Any(transaction => transaction.Type == "waiver_claimed") && transactionHistory.Transactions.Any(transaction => transaction.Type == "practice_squad_signed_active"), "Transaction history should expose roster-management actions.");
            contractLeague.Calendar.Phase = "Preseason";
            var tagTarget = contractTeam.Roster.OrderBy(player => player.Overall).First();
            tagTarget.Contract.YearsRemaining = 1;
            var invalidPhaseTag = contractTestService.ApplyFranchiseTag(tagTarget.PlayerId, contractTeam.TeamId);
            Require(!invalidPhaseTag.Ok && invalidPhaseTag.Message.Contains("In season", StringComparison.OrdinalIgnoreCase), "Franchise tags should reject attempts outside the franchise-tag phase with an explanation.");
            contractLeague.Calendar.Phase = ScheduleService.FranchiseTagPendingPhase;
            Require(ContractPhaseRules.GetStatus(contractLeague).CanApplyFranchiseTag, "Franchise-tag phase should expose tag availability.");
            var nonExpiringPlayer = contractTeam.Roster.First(player => player.PlayerId != tagTarget.PlayerId && player.Contract.YearsRemaining > 1);
            var invalidEligibilityTag = contractTestService.ApplyFranchiseTag(nonExpiringPlayer.PlayerId, contractTeam.TeamId);
            Require(!invalidEligibilityTag.Ok && invalidEligibilityTag.Message.Contains("final contract year", StringComparison.OrdinalIgnoreCase), "Franchise tags should require an eligible final-year active-roster player.");
            var tagResult = contractTestService.ApplyFranchiseTag(tagTarget.PlayerId, contractTeam.TeamId);
            Require(tagResult.Ok && tagResult.Accepted && tagTarget.Contract.ContractType == "Franchise Tag" && tagTarget.Contract.YearsRemaining == 1 && tagTarget.Contract.GuaranteedSalary == tagTarget.Contract.AnnualSalary && contractTeam.FranchiseTagPlayerId == tagTarget.PlayerId, tagResult.Message);
            Require(contractLeague.Transactions.Any(transaction => transaction.Type == "franchise_tag_applied" && transaction.PlayerId == tagTarget.PlayerId), "Applying a franchise tag should create a transaction-history record.");
            var duplicateTag = contractTestService.ApplyFranchiseTag(nonExpiringPlayer.PlayerId, contractTeam.TeamId);
            Require(!duplicateTag.Ok && duplicateTag.Message.Contains("already used", StringComparison.OrdinalIgnoreCase), "Teams should be limited to one franchise tag per season.");
            const string tagSaveName = "native_smoke_tag_save.json";
            var tagSave = saveService.Save(contractContext, tagSaveName);
            Require(tagSave.Ok, tagSave.Message);
            var tagLoad = saveService.Load(tagSaveName);
            Require(tagLoad.Ok && tagLoad.League.Teams.First(team => team.TeamId == contractTeam.TeamId).FranchiseTagPlayerId == tagTarget.PlayerId && tagLoad.League.Teams.First(team => team.TeamId == contractTeam.TeamId).Roster.First(player => player.PlayerId == tagTarget.PlayerId).Contract.ContractType == "Franchise Tag", "Franchise-tag state should persist through save/load.");
            Require(saveService.Delete(tagSaveName).Ok, "Franchise-tag smoke save should clean up.");
            Require(new TransactionService(contractContext).ProcessContractExpirations(contractTestService) >= 0 && contractTeam.Roster.Any(player => player.PlayerId == tagTarget.PlayerId), "A franchise tag should remain active through the offseason in which it is applied.");
            contractLeague.SeasonYear++;
            Require(new TransactionService(contractContext).ProcessContractExpirations(contractTestService) >= 1 && !contractTeam.Roster.Any(player => player.PlayerId == tagTarget.PlayerId) && contractLeague.FreeAgents.Any(player => player.PlayerId == tagTarget.PlayerId) && contractLeague.Transactions.Any(transaction => transaction.Type == "franchise_tag_expired" && transaction.PlayerId == tagTarget.PlayerId), "Franchise tags should expire into free agency during the following offseason.");
            Pass(result, currentStep);

            currentStep = "Trade proposal foundation";
            var tradeContext = new GameCoreContext();
            var tradeLeague = new LeagueBootstrapService(tradeContext).CreateTestLeague(teamSeedPath);
            tradeLeague.Calendar.Phase = ScheduleService.FreeAgencyPendingPhase;
            var tradeService = new TransactionService(tradeContext);
            var tradeContracts = new ContractService(tradeContext);
            new DraftService(tradeContext).PrepareDraftBoard();
            var tradeUser = tradeLeague.Teams.First(team => team.TeamId == tradeLeague.UserTeamId);
            var tradePartner = tradeLeague.Teams.First(team => team.TeamId != tradeUser.TeamId);
            var frontOffice = new FrontOfficeEvaluationService(tradeContext);
            var firstFrontOfficeReport = frontOffice.EvaluateTeam(tradePartner.TeamId);
            var repeatedFrontOfficeReport = frontOffice.EvaluateTeam(tradePartner.TeamId);
            Require(firstFrontOfficeReport.Ok && firstFrontOfficeReport.Rationale == repeatedFrontOfficeReport.Rationale && firstFrontOfficeReport.PositionNeeds.SequenceEqual(repeatedFrontOfficeReport.PositionNeeds), "CPU front-office reports should be deterministic and read-only.");
            const string frontOfficeSaveName = "native_smoke_front_office.json";
            Require(saveService.Save(tradeContext, frontOfficeSaveName).Ok, "Front-office smoke save should succeed.");
            var loadedFrontOffice = saveService.Load(frontOfficeSaveName);
            var loadedFrontOfficeReport = new FrontOfficeEvaluationService(new GameCoreContext { ActiveLeague = loadedFrontOffice.League }).EvaluateTeam(tradePartner.TeamId);
            Require(loadedFrontOffice.Ok && loadedFrontOfficeReport.Ok && loadedFrontOfficeReport.Rationale == firstFrontOfficeReport.Rationale, "CPU front-office reports should regenerate identically after save/load.");
            Require(saveService.Delete(frontOfficeSaveName).Ok, "Front-office smoke save should clean up.");
            var userPick = tradeLeague.Draft.Picks.First(pick => pick.TeamId == tradeUser.TeamId);
            var partnerPick = tradeLeague.Draft.Picks.First(pick => pick.TeamId == tradePartner.TeamId);
            Require(userPick.OriginalTeamId == tradeUser.TeamId && partnerPick.OriginalTeamId == tradePartner.TeamId, "Draft picks should retain immutable original ownership when trade assets are prepared.");
            var marketTransactionsBefore = tradeLeague.Transactions.Count;
            var marketPlayer = tradeUser.Roster.OrderBy(player => player.Overall).First();
            var market = new TradeMarketService(tradeContext).Submit(new[] { marketPlayer.PlayerId }, new[] { userPick.OverallPick });
            Require(market.Ok && market.Submitted && market.OfferedAssets.Count == 2 && market.Offers.Count is > 0 and <= 5, "Submitting owned assets to the trade market should produce a bounded set of concrete CPU offers.");
            Require(userPick.TeamId == tradeUser.TeamId && tradeLeague.Transactions.Count == marketTransactionsBefore, "Trade-market offer generation must not move assets or create completed transaction records.");
            var rejectedMarket = new TradeMarketService(tradeContext).RejectOffer(market.Offers[0].OfferId);
            Require(rejectedMarket.Ok && rejectedMarket.Offers.Any(offer => offer.OfferId == market.Offers[0].OfferId && offer.Status == "rejected"), "A received trade-market offer should remain persisted after the user rejects it.");
            market = new TradeMarketService(tradeContext).Submit(new[] { marketPlayer.PlayerId }, new[] { userPick.OverallPick });
            Require(market.Offers.All(offer => offer.Status == "open"), "Resubmitting a shopping package should replace prior responses with fresh open offers.");
            const string tradeMarketSaveName = "native_smoke_trade_market.json";
            Require(saveService.Save(tradeContext, tradeMarketSaveName).Ok, "Trade-market submission should save.");
            var loadedTradeMarket = saveService.Load(tradeMarketSaveName);
            Require(loadedTradeMarket.Ok && loadedTradeMarket.League.TradeMarket.Submitted && loadedTradeMarket.League.TradeMarket.Offers.Count == market.Offers.Count, "Trade-market submission and offers should persist through save/load.");
            Require(saveService.Delete(tradeMarketSaveName).Ok, "Trade-market smoke save should clean up.");
            var marketAcceptContext = new GameCoreContext();
            var marketAcceptLeague = new LeagueBootstrapService(marketAcceptContext).CreateTestLeague(teamSeedPath);
            marketAcceptLeague.Calendar.Phase = ScheduleService.FreeAgencyPendingPhase;
            new DraftService(marketAcceptContext).PrepareDraftBoard();
            var marketAcceptUser = marketAcceptLeague.Teams.First(team => team.TeamId == marketAcceptLeague.UserTeamId);
            var marketAcceptPlayer = marketAcceptUser.Roster.OrderBy(player => player.Overall).First();
            var marketAcceptPick = marketAcceptLeague.Draft.Picks.First(pick => pick.TeamId == marketAcceptUser.TeamId);
            var actionableMarket = new TradeMarketService(marketAcceptContext);
            var actionableSubmission = actionableMarket.Submit(new[] { marketAcceptPlayer.PlayerId }, new[] { marketAcceptPick.OverallPick });
            var acceptedMarketOffer = actionableMarket.AcceptOffer(actionableSubmission.Offers.First().OfferId);
            Require(acceptedMarketOffer.Ok && acceptedMarketOffer.Accepted && !marketAcceptLeague.TradeMarket.Submitted && marketAcceptLeague.Transactions.Count(transaction => transaction.Type == "trade_accepted") == 2, "Accepting a concrete trade-market response should complete the normal validated transaction and clear stale responses.");
            var rejectedTrade = tradeService.SubmitUserTradeProposal(new TradeProposal
            {
                ProposingTeamId = tradeUser.TeamId,
                ReceivingTeamId = tradePartner.TeamId,
                ProposingPlayerIds = new List<string> { tradeUser.Roster.OrderBy(player => player.Overall).First().PlayerId },
                ReceivingPlayerIds = new List<string> { tradePartner.Roster.OrderByDescending(player => player.Overall).First().PlayerId },
                ReceivingPickOverallNumbers = new List<int> { partnerPick.OverallPick },
            }, tradeContracts);
            Require(rejectedTrade.Ok && !rejectedTrade.Accepted && rejectedTrade.Rationale.Contains("values", StringComparison.OrdinalIgnoreCase) && userPick.TeamId == tradeUser.TeamId, "Under-value trade proposals should be rejected with a deterministic rationale and no mutation.");
            var invalidOwnershipTrade = tradeService.SubmitUserTradeProposal(new TradeProposal
            {
                ProposingTeamId = tradeUser.TeamId,
                ReceivingTeamId = tradePartner.TeamId,
                ProposingPickOverallNumbers = new List<int> { partnerPick.OverallPick },
                ReceivingPickOverallNumbers = new List<int> { partnerPick.OverallPick },
            }, tradeContracts);
            Require(!invalidOwnershipTrade.Ok && invalidOwnershipTrade.Message.Contains("owned", StringComparison.OrdinalIgnoreCase), $"Trade validation should reject picks that are not owned by the offering team. Actual: {invalidOwnershipTrade.Message}");
            var offeredPlayer = tradeUser.Roster.OrderByDescending(player => player.Overall).First();
            var requestedPlayer = tradePartner.Roster.OrderBy(player => player.Overall).First();
            var previewProposal = new TradeProposal
            {
                ProposingTeamId = tradeUser.TeamId,
                ReceivingTeamId = tradePartner.TeamId,
                ProposingPlayerIds = new List<string> { offeredPlayer.PlayerId },
                ProposingPickOverallNumbers = new List<int> { userPick.OverallPick },
                ReceivingPlayerIds = new List<string> { requestedPlayer.PlayerId },
                ReceivingPickOverallNumbers = new List<int> { partnerPick.OverallPick },
            };
            var rosterBeforePreview = tradeUser.Roster.Count;
            var transactionCountBeforePreview = tradeLeague.Transactions.Count;
            var preview = tradeService.PreviewUserTradeProposal(previewProposal, tradeContracts);
            Require(preview.Ok && preview.CanSubmit && preview.OfferedValue > 0 && preview.RequestedValue > 0 && preview.ProposerRosterAfter == rosterBeforePreview && preview.ReceiverRosterAfter == tradePartner.Roster.Count && preview.Rationale.Contains("Projected cap room", StringComparison.OrdinalIgnoreCase), "Trade preview should provide deterministic package value and cap/roster impact.");
            Require(tradeUser.Roster.Any(player => player.PlayerId == offeredPlayer.PlayerId) && tradePartner.Roster.Any(player => player.PlayerId == requestedPlayer.PlayerId) && userPick.TeamId == tradeUser.TeamId && partnerPick.TeamId == tradePartner.TeamId && tradeLeague.Transactions.Count == transactionCountBeforePreview, "Trade preview must not mutate rosters, pick ownership, or transaction history.");
            var invalidPreview = tradeService.PreviewUserTradeProposal(new TradeProposal { ProposingTeamId = tradeUser.TeamId, ReceivingTeamId = tradePartner.TeamId, ProposingPickOverallNumbers = new List<int> { partnerPick.OverallPick }, ReceivingPickOverallNumbers = new List<int> { partnerPick.OverallPick } }, tradeContracts);
            Require(!invalidPreview.Ok && invalidPreview.Message.Contains("owned", StringComparison.OrdinalIgnoreCase), "Trade preview should explain invalid asset ownership before submission.");
            var acceptedTrade = tradeService.SubmitUserTradeProposal(previewProposal, tradeContracts);
            Require(acceptedTrade.Ok && acceptedTrade.Accepted && tradeUser.Roster.Any(player => player.PlayerId == requestedPlayer.PlayerId) && tradePartner.Roster.Any(player => player.PlayerId == offeredPlayer.PlayerId), acceptedTrade.Message);
            Require(userPick.TeamId == tradePartner.TeamId && partnerPick.TeamId == tradeUser.TeamId && tradeLeague.Transactions.Count(transaction => transaction.Type == "trade_accepted") == 2, "Accepted trades should exchange pick ownership and record both teams' transaction history.");
            Require(!tradeLeague.TradeMarket.Submitted && tradeLeague.TradeMarket.Offers.Count == 0, "Completing any user trade should invalidate the prior shopping package and its offers.");
            const string tradeSaveName = "native_smoke_trade_assets.json";
            Require(saveService.Save(tradeContext, tradeSaveName).Ok, "Trade-asset smoke save should succeed.");
            var loadedTrade = saveService.Load(tradeSaveName);
            Require(loadedTrade.Ok && loadedTrade.League.Draft.Picks.First(pick => pick.OverallPick == userPick.OverallPick).TeamId == tradePartner.TeamId && loadedTrade.League.Draft.Picks.First(pick => pick.OverallPick == userPick.OverallPick).OriginalTeamId == tradeUser.TeamId, "Traded draft-pick ownership should persist through save/load.");
            Require(saveService.Delete(tradeSaveName).Ok, "Trade-asset smoke save should clean up.");
            var preRolloverMarket = new TradeMarketService(tradeContext).Submit(new[] { requestedPlayer.PlayerId }, new[] { partnerPick.OverallPick });
            Require(preRolloverMarket.Ok && preRolloverMarket.Submitted, "A new owned package should be submittable after the completed trade.");
            tradeLeague.Calendar.Phase = ScheduleService.TrainingCampPendingPhase;
            var tradeCamp = new TrainingCampService(tradeContext);
            Require(new DepthChartService(tradeContext).AutoFillDepthChart(tradeUser.TeamId).Ok, "Trade rollover should restore a valid user depth chart.");
            Require(tradeCamp.ApplyPositionFocus(tradeUser.Roster.First().Position, tradeUser.TeamId).Ok && tradeCamp.FinalizeRoster(tradeUser.TeamId).Ok, "Trade rollover should finalize the user training-camp roster.");
            Require(new SeasonRolloverService(tradeContext).StartNextSeason(out _), "Trade rollover should start the next season.");
            Require(!tradeLeague.TradeMarket.Submitted && tradeLeague.TradeMarket.Offers.Count == 0, "Season rollover should clear stale trade-market packages and offers.");
            Require(tradeLeague.HistoricalDrafts.Any(draft => draft.DraftYear == tradeLeague.SeasonYear - 1 && draft.Picks.Any(pick => pick.OverallPick == userPick.OverallPick && pick.TeamId == tradePartner.TeamId && pick.OriginalTeamId == tradeUser.TeamId)), "Rollover should archive traded pick ownership without rewriting original ownership.");
            Pass(result, currentStep);

            currentStep = "CPU front-office evaluation";
            var rolloverFrontOfficeReport = frontOffice.EvaluateTeam(tradePartner.TeamId);
            Require(rolloverFrontOfficeReport.Ok && rolloverFrontOfficeReport.TeamId == tradePartner.TeamId && rolloverFrontOfficeReport.DraftPicksAvailable >= 0, "CPU front-office reports should remain available after lifecycle rollover without mutating team state.");
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

            currentStep = "Drag-order depth chart";
            var reorderGroup = filledDepthChart.Positions.First(position => position.Players.Count > 1);
            var draggedPlayer = reorderGroup.Players[^1];
            var dropTarget = reorderGroup.Players[0];
            var reorderedDepthChart = depthChartService.UpdateDepthChart("move_before", reorderGroup.Position, draggedPlayer.PlayerId, null, dropTarget.PlayerId);
            var reorderedGroup = reorderedDepthChart.Positions.First(position => string.Equals(position.Position, reorderGroup.Position, StringComparison.OrdinalIgnoreCase));
            Require(reorderedDepthChart.Ok && reorderedGroup.Players[0].PlayerId == draggedPlayer.PlayerId, "Depth chart drag-order request should persist the player before its drop target.");
            var movedDownPlayer = reorderedGroup.Players[0];
            var moveAfterTarget = reorderedGroup.Players[^1];
            var movedDownDepthChart = depthChartService.UpdateDepthChart("move_after", reorderGroup.Position, movedDownPlayer.PlayerId, null, moveAfterTarget.PlayerId);
            var movedDownGroup = movedDownDepthChart.Positions.First(position => string.Equals(position.Position, reorderGroup.Position, StringComparison.OrdinalIgnoreCase));
            Require(movedDownDepthChart.Ok && movedDownGroup.Players[^1].PlayerId == movedDownPlayer.PlayerId, "Depth chart drag-order request should support moving a player down after its drop target.");
            var restoreAfterDropTest = depthChartService.UpdateDepthChart("move_before", reorderGroup.Position, movedDownPlayer.PlayerId, null, dropTarget.PlayerId);
            Require(restoreAfterDropTest.Ok, restoreAfterDropTest.Error);
            reorderedGroup = restoreAfterDropTest.Positions.First(position => string.Equals(position.Position, reorderGroup.Position, StringComparison.OrdinalIgnoreCase));
            Pass(result, currentStep);

            currentStep = "Locked depth chart auto-fill";
            var lockedOrder = reorderedGroup.Players.Select(player => player.PlayerId).ToArray();
            var lockResult = depthChartService.TogglePositionLock(reorderGroup.Position);
            Require(lockResult.Ok && lockResult.Positions.First(position => string.Equals(position.Position, reorderGroup.Position, StringComparison.OrdinalIgnoreCase)).IsLocked, "Depth chart position should report its locked state.");
            const string depthLockSaveName = "native_smoke_depth_lock.json";
            Require(saveService.Save(context, depthLockSaveName).Ok, "Locked depth chart should save successfully.");
            var loadedDepthLock = saveService.Load(depthLockSaveName);
            var loadedDepthLockTeam = loadedDepthLock.League?.Teams.FirstOrDefault(team => team.TeamId == context.ActiveLeague.UserTeamId);
            Require(loadedDepthLock.Ok && loadedDepthLockTeam?.DepthChartLockedPositions.Contains(reorderGroup.Position, StringComparer.OrdinalIgnoreCase) == true, "Depth chart position locks should persist through save/load.");
            Require(saveService.Delete(depthLockSaveName).Ok, "Depth chart lock smoke save should clean up.");
            var lockedAutoFill = depthChartService.AutoFillDepthChart();
            var lockedAutoFillGroup = lockedAutoFill.Positions.First(position => string.Equals(position.Position, reorderGroup.Position, StringComparison.OrdinalIgnoreCase));
            Require(lockedAutoFill.Ok && lockedAutoFillGroup.IsLocked && lockedAutoFillGroup.Players.Select(player => player.PlayerId).SequenceEqual(lockedOrder), "Auto-Fill should preserve the user-defined order of locked positions.");
            Require(depthChartService.TogglePositionLock(reorderGroup.Position).Ok, "Depth chart position should unlock cleanly after validation.");
            Pass(result, currentStep);

            currentStep = "Incremental live game session";
            ValidateIncrementalLiveGameSession(teamSeedPath);
            Pass(result, currentStep);

            currentStep = "Dashboard-to-postgame saved lifecycle";
            ValidateDashboardToPostgameLifecycle(teamSeedPath);
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
            var liveQbGroup = depthChartService.GetTeamDepthChart().Positions.First(position => string.Equals(position.Position, "QB", StringComparison.OrdinalIgnoreCase));
            var liveQbs = liveQbGroup.Players.Where(player => player.IsAvailable).ToList();
            Require(liveQbs.Count > 1, "Game-day depth validation requires at least two available quarterbacks in the smoke roster.");
            var promotedGameDayQuarterback = liveQbs[^1];
            Require(depthChartService.UpdateDepthChart("move_before", "QB", promotedGameDayQuarterback.PlayerId, null, liveQbs[0].PlayerId).Ok, "Game-day quarterback reorder should succeed before simulation.");
            var simulated = gameDayService.SimulateCurrentUserGame(gameDay.Game.GameId);
            Require(simulated.Ok, simulated.Error);
            Require(simulated.Result.BoxScore.Count > 0, "Simulated game missing box score.");
            simulated.Result.BoxScore.TryGetValue("player_stats", out var preseasonPlayerStats);
            var lines = preseasonPlayerStats as List<PlayerGameStats>;
            Require(lines != null && lines.Count > 0, "Simulated game missing player box-score lines.");
            Require(lines.Any(line => line.PlayerId == promotedGameDayQuarterback.PlayerId && string.Equals(line.Position, "QB", StringComparison.OrdinalIgnoreCase)), "Game simulation should use the saved top available quarterback from the depth chart.");
            simulated.Result.BoxScore.TryGetValue("play_by_play", out var preseasonPlayByPlay);
            var plays = preseasonPlayByPlay as List<GamePlayEventState>;
            Require(plays != null
                && plays.Count >= 9
                && plays.Select(play => play.Sequence).SequenceEqual(Enumerable.Range(1, plays.Count))
                && plays[^1].ClockSeconds == 0
                && plays[^1].HomeScore == simulated.Result.HomeScore
                && plays[^1].AwayScore == simulated.Result.AwayScore,
                "Simulated game should expose an ordered authoritative playback timeline ending at the final score.");
            const string playbackSaveName = "native_smoke_game_playback.json";
            Require(saveService.Save(context, playbackSaveName).Ok, "Game playback timeline should save successfully.");
            var loadedPlayback = saveService.Load(playbackSaveName);
            var loadedPlaybackResult = loadedPlayback.League?.Results.FirstOrDefault(result => result.GameId == simulated.Result.GameId);
            Require(loadedPlayback.Ok
                && loadedPlaybackResult?.BoxScore?.PlayByPlay.Count == plays.Count
                && loadedPlaybackResult.BoxScore.PlayByPlay[^1].HomeScore == simulated.Result.HomeScore
                && loadedPlaybackResult.BoxScore.PlayByPlay[^1].AwayScore == simulated.Result.AwayScore,
                "Authoritative game playback should persist through save/load without changing the final score.");
            Require(saveService.Delete(playbackSaveName).Ok, "Game playback smoke save should clean up.");
            Require(context.ActiveLeague.Teams.SelectMany(team => team.Roster).Any(player => player.Fatigue > 0), "Game participants should gain fatigue.");
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
            var openingWeekReminder = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, "opening_week_readiness", StringComparison.OrdinalIgnoreCase));
            Require(openingWeekReminder != null && openingWeekReminder.Description.Contains("Review", StringComparison.OrdinalIgnoreCase) && string.Equals(openingWeekReminder.PrimaryAction, "Review Roster", StringComparison.Ordinal), "Regular-season Week 1 should expose the non-mutating roster/depth readiness reminder.");
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
            var balanceDiagnostics = SimulationDiagnosticsService.AnalyzeRegularSeason(context.ActiveLeague);
            Require(balanceDiagnostics.CompletedRegularSeasonGames == LeagueBootstrapService.RegularSeasonGameCount && balanceDiagnostics.PointsPerTeamGame > 0d && balanceDiagnostics.LargestScoreMargin >= 0, "Regular-season balance diagnostics should derive valid persisted-result measurements.");
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

            currentStep = "Advance offseason and free agency";
            DraftClassRecapEntry draftRecapSnapshot = null;
            foreach (var expectedPhase in BuildExpectedOffseasonPlaceholderPhases().Skip(1))
            {
                if (string.Equals(expectedPhase, ScheduleService.RookieSigningPendingPhase, StringComparison.Ordinal)
                    && string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase))
                {
                    dashboard = dashboardService.GetDashboardState();
                    Require(dashboard.Ok, dashboard.Error);
                    ValidateOffseasonPlaceholderDashboard(dashboard.Dashboard, expectedPhase);
                    continue;
                }

                var phaseAdvance = continueService.Continue();
                Require(phaseAdvance.Ok, phaseAdvance.Error);
                var expectedStopReason = string.Equals(expectedPhase, ScheduleService.FreeAgencyPendingPhase, StringComparison.OrdinalIgnoreCase)
                    ? "free_agency_open"
                    : ScheduleService.GetOffseasonPhaseKey(expectedPhase);
                Require(string.Equals(phaseAdvance.Result.StopReason, expectedStopReason, StringComparison.OrdinalIgnoreCase), $"Expected {expectedStopReason} while advancing offseason, got {phaseAdvance.Result.StopReason}.");
                Require(phaseAdvance.Result.GamesSimulated == 0, $"Advancing to {expectedPhase} should not simulate games.");
                Require(string.Equals(context.ActiveLeague.Calendar.Phase, expectedPhase, StringComparison.OrdinalIgnoreCase), $"Expected offseason phase {expectedPhase}, got {context.ActiveLeague.Calendar.Phase}.");

                dashboard = dashboardService.GetDashboardState();
                Require(dashboard.Ok, dashboard.Error);
                ValidateOffseasonPlaceholderDashboard(dashboard.Dashboard, expectedPhase);

                if (string.Equals(expectedPhase, ScheduleService.FranchiseTagPendingPhase, StringComparison.Ordinal))
                    Require(context.ActiveLeague.LastContractExpirationSeason != context.ActiveLeague.SeasonYear, "Contract expirations should remain pending until franchise-tag decisions are complete.");

                if (string.Equals(expectedPhase, ScheduleService.LeagueYearPendingPhase, StringComparison.Ordinal))
                    Require(context.ActiveLeague.LastContractExpirationSeason == context.ActiveLeague.SeasonYear, "Contract expirations should process once after the franchise-tag phase.");

                if (string.Equals(expectedPhase, ScheduleService.FreeAgencyPendingPhase, StringComparison.Ordinal))
                {
                    var userTeam = context.ActiveLeague.Teams.First(team => string.Equals(team.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
                    var offseasonReleasedPlayer = userTeam.Roster.First(player => !string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase));
                    var releaseResult = contractService.ReleasePlayer(offseasonReleasedPlayer.PlayerId, userTeam.TeamId);
                    Require(releaseResult.Ok && releaseResult.Accepted, releaseResult.Message);
                    var freeAgent = context.ActiveLeague.FreeAgents.OrderBy(player => contractService.GetRequiredAnnualSalary(player, userTeam)).First();
                    var requirement = contractService.GetRequiredAnnualSalary(freeAgent, userTeam);
                    var signingResult = contractService.SignFreeAgent(freeAgent.PlayerId, userTeam.TeamId, new ContractOffer
                    {
                        AnnualSalary = requirement * 1.15m,
                        GuaranteedSalary = requirement * 0.30m,
                        Years = 2,
                    });
                    Require(signingResult.Ok && signingResult.Accepted, signingResult.Message);
                    Require(context.ActiveLeague.Transactions.Any(transaction => string.Equals(transaction.Type, "free_agent_signed", StringComparison.OrdinalIgnoreCase)), "Offseason free-agent signing should be recorded.");
                }

                if (string.Equals(expectedPhase, ScheduleService.DraftPrepPendingPhase, StringComparison.Ordinal))
                {
                    var draftPrepReminder = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, ScheduleService.DraftPrepPendingPhaseKey, StringComparison.OrdinalIgnoreCase));
                    Require(draftPrepReminder != null && string.Equals(draftPrepReminder.PrimaryAction, "Review Team Draft Board", StringComparison.Ordinal) && draftPrepReminder.Description.Contains("No approval", StringComparison.OrdinalIgnoreCase), "Draft preparation should provide a non-blocking inbox reminder linked to the private Team Draft Board.");
                    var draftBoard = new DraftService(context);
                    draftBoard.PrepareDraftBoard();
                    var boardProspects = context.ActiveLeague.CollegeProspects.Where(prospect => prospect != null && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId)).Take(2).ToList();
                    Require(boardProspects.Count == 2 && draftBoard.AddToUserBoard(boardProspects[0].ProspectId) && draftBoard.AddToUserBoard(boardProspects[1].ProspectId), "Team draft board should accept available prospects.");
                    Require(draftBoard.MoveOnUserBoard(boardProspects[1].ProspectId, -1) && context.ActiveLeague.Draft.UserBoardProspectIds[0] == boardProspects[1].ProspectId, "Team draft board should preserve explicit manual ranking.");
                    Require(draftBoard.MoveOnUserBoard(boardProspects[1].ProspectId, boardProspects[0].ProspectId, true) && context.ActiveLeague.Draft.UserBoardProspectIds[1] == boardProspects[1].ProspectId, "Team draft board should support drag-style downward placement after a target.");
                    Require(draftBoard.MoveOnUserBoard(boardProspects[1].ProspectId, boardProspects[0].ProspectId, false) && context.ActiveLeague.Draft.UserBoardProspectIds[0] == boardProspects[1].ProspectId, "Team draft board should support drag-style upward placement before a target.");
                    Require(draftBoard.SetUserBoardContext(boardProspects[1].ProspectId, "target", "Priority fit after private evaluation.", "Day One"), "Team draft board should save private tags, notes, and tiers.");
                    var teamBoardNeed = draftBoard.GetUserBoardNeedContext(boardProspects[1].ProspectId);
                    Require(teamBoardNeed != null && new[] { "High", "Medium", "Low" }.Contains(teamBoardNeed.NeedLevel) && teamBoardNeed.Explanation.Contains("rostered", StringComparison.OrdinalIgnoreCase), "Team draft board should derive transparent roster-need context without hidden prospect truth.");
                    const string teamDraftBoardSaveName = "native_smoke_team_draft_board.json";
                    Require(saveService.Save(context, teamDraftBoardSaveName).Ok, "Team draft board should save.");
                    var loadedTeamDraftBoard = saveService.Load(teamDraftBoardSaveName);
                    Require(loadedTeamDraftBoard.Ok && loadedTeamDraftBoard.League.Draft.UserBoardProspectIds.Take(2).SequenceEqual(new[] { boardProspects[1].ProspectId, boardProspects[0].ProspectId }) && loadedTeamDraftBoard.League.Draft.UserBoardTags[boardProspects[1].ProspectId] == "target" && loadedTeamDraftBoard.League.Draft.UserBoardNotes[boardProspects[1].ProspectId].Contains("Priority fit", StringComparison.Ordinal) && loadedTeamDraftBoard.League.Draft.UserBoardTiers[boardProspects[1].ProspectId] == "Day One", "Team draft board membership, order, tags, private notes, and named tiers should survive save/load.");
                    Require(saveService.Delete(teamDraftBoardSaveName).Ok, "Team draft board smoke save should clean up.");
                }

                if (string.Equals(expectedPhase, ScheduleService.DraftPendingPhase, StringComparison.Ordinal))
                {
                    var draftService = new DraftService(context);
                    var liveUserPick = draftService.GetCurrentPick();
                    Require(liveUserPick != null && string.Equals(liveUserPick.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase), "Live draft should reach the user's owned current pick before draft-day trade checks.");
                    var draftDayMarket = new TradeMarketService(context).Submit(Array.Empty<string>(), new[] { liveUserPick.OverallPick }, "Any");
                    Require(draftDayMarket.Ok && draftDayMarket.Submitted && draftDayMarket.OfferedAssets.Any(asset => asset.Contains($"#{liveUserPick.OverallPick}", StringComparison.Ordinal)), "The live draft should allow the current owned pick to enter the existing validated trade market without executing a trade.");
                    Require(draftDayMarket.Offers.Any(offer => offer.PartnerAssets.Any(asset => asset.StartsWith("Pick #", StringComparison.OrdinalIgnoreCase))), "A submitted live-draft pick should receive at least one value-valid trade-down response containing another unused pick.");
                    Require(new TradeMarketService(context).Withdraw().Ok && !context.ActiveLeague.TradeMarket.Submitted, "Draft-day market package should be withdrawable without changing pick ownership.");
                    var draftSelectionCount = 0;
                    while (string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
                    {
                        var currentPick = draftService.GetCurrentPick();
                        Require(currentPick != null && string.Equals(currentPick.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase), "Draft should stop only when the user team is on the clock.");
                        var prospect = context.ActiveLeague.CollegeProspects.First(candidate => candidate != null && string.IsNullOrWhiteSpace(candidate.DraftedByTeamId));
                        Require(draftService.MakePick(context.ActiveLeague.UserTeamId, prospect.ProspectId), draftService.LastMessage);
                        draftSelectionCount++;
                        Require(draftSelectionCount <= DraftService.DraftRounds, "User draft selections exceeded the configured round count.");
                    }

                    var expectedPickCount = context.ActiveLeague.Teams.Count * DraftService.DraftRounds;
                    Require(draftSelectionCount == DraftService.DraftRounds, "User team should receive one selection in every round.");
                    Require(context.ActiveLeague.Draft.IsCompleted && context.ActiveLeague.Draft.Picks.Count == expectedPickCount, "Draft should complete all seven rounds for every team.");
                    Require(context.ActiveLeague.Draft.Picks.All(pick => !string.IsNullOrWhiteSpace(pick.ProspectId) && !string.IsNullOrWhiteSpace(pick.PlayerId)), "Every completed draft pick should own a prospect and rookie player id.");
                    Require(context.ActiveLeague.Draft.RecapEntries.Count == expectedPickCount && context.ActiveLeague.Draft.RecapEntries.All(entry => entry.OverallPick > 0 && !string.IsNullOrWhiteSpace(entry.Name) && !string.IsNullOrWhiteSpace(entry.ScoutingReport) && !string.IsNullOrWhiteSpace(entry.ContractType)), "Every completed draft pick should capture immutable evaluation and rookie-contract context.");
                    Require(context.ActiveLeague.Draft.RecapEntries.Any(entry => entry.PublicBoardRank > 0 && !string.IsNullOrWhiteSpace(entry.PublicReaction)) && context.ActiveLeague.Draft.RecapEntries.Where(entry => !string.IsNullOrWhiteSpace(entry.PublicReaction)).All(entry => entry.PublicReaction.Contains("Analyst Board", StringComparison.Ordinal)), "Notable selections should persist clearly attributed public-board reactions without claiming hidden truth or future outcomes.");
                    context.ActiveLeague.Draft.UseShortDraftAnnouncements = true;
                    draftRecapSnapshot = context.ActiveLeague.Draft.RecapEntries.First();
                    Require(context.ActiveLeague.Transactions.Count(transaction => string.Equals(transaction.Type, "draft_pick_made", StringComparison.OrdinalIgnoreCase)) == expectedPickCount, "Every draft pick should create a transaction record.");
                    Require(context.ActiveLeague.Teams.All(team => team.Roster.Count(player => player.Contract?.ContractType == "Rookie Draft Contract") == DraftService.DraftRounds), "Every team should receive seven rostered rookies.");
                    Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase), "Completed draft should transition to rookie signing.");
                    var undraftedMarket = context.ActiveLeague.FreeAgents.Where(UndraftedFreeAgentService.IsUndraftedRookie).ToList();
                    Require(undraftedMarket.Count > 0 && context.ActiveLeague.CollegeProspects.All(prospect => !string.IsNullOrWhiteSpace(prospect.DraftedByTeamId) || prospect.DraftClassYear > context.ActiveLeague.SeasonYear + 1), "Draft completion should open the undrafted rookie market before training camp and remove those players from the draft pool.");
                    var rookieSigningTeam = context.ActiveLeague.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId);
                    var releasedForUdfa = rookieSigningTeam.Roster.OrderByDescending(player => player.Contract?.AnnualSalary ?? 0m).First();
                    Require(new ContractService(context).ReleasePlayer(releasedForUdfa.PlayerId, rookieSigningTeam.TeamId).Accepted, "UDFA signing smoke setup should create one active-roster opening through the normal release path.");
                    var undraftedTarget = undraftedMarket.OrderByDescending(player => player.Overall).First();
                    var undraftedContracts = new ContractService(context);
                    var undraftedRequirement = undraftedContracts.GetRequiredAnnualSalary(undraftedTarget, rookieSigningTeam);
                    var minicamp = new RookieMinicampService(context);
                    var invite = minicamp.Invite(undraftedTarget.PlayerId);
                    Require(invite.Ok && invite.Accepted && minicamp.GetState().InvitedPlayerIds.Contains(undraftedTarget.PlayerId), "The UDFA market should persist an explicit rookie-minicamp invitation without signing or removing the player.");
                    const string minicampSaveName = "native_smoke_rookie_minicamp.json";
                    Require(saveService.Save(context, minicampSaveName).Ok, "Rookie-minicamp invitation should save.");
                    var loadedMinicamp = saveService.Load(minicampSaveName);
                    Require(loadedMinicamp.Ok && loadedMinicamp.League.RookieMinicamp.InvitedPlayerIds.Contains(undraftedTarget.PlayerId), "Rookie-minicamp invitation should survive save/load.");
                    Require(saveService.Delete(minicampSaveName).Ok, "Rookie-minicamp invitation smoke save should clean up.");
                    var invalidUdfaTerm = undraftedContracts.SignFreeAgent(undraftedTarget.PlayerId, rookieSigningTeam.TeamId, new ContractOffer { AnnualSalary = undraftedRequirement, GuaranteedSalary = undraftedRequirement * .30m, Years = 2 });
                    Require(!invalidUdfaTerm.Ok && invalidUdfaTerm.Message.Contains("three-year", StringComparison.OrdinalIgnoreCase), "Undrafted rookie signing should explain its required contract type and term.");
                    var signedUdfa = undraftedContracts.SignFreeAgent(undraftedTarget.PlayerId, rookieSigningTeam.TeamId, new ContractOffer { AnnualSalary = undraftedRequirement, GuaranteedSalary = undraftedRequirement * .30m, Years = 3 });
                    Require(signedUdfa.Ok && signedUdfa.Accepted && rookieSigningTeam.Roster.Any(player => player.PlayerId == undraftedTarget.PlayerId && player.Contract.ContractType == "Undrafted Rookie Contract" && player.Contract.YearsRemaining == 3) && !minicamp.GetState().InvitedPlayerIds.Contains(undraftedTarget.PlayerId), "Rookie-signing phase should allow a validated undrafted rookie contract, and signing should close any redundant minicamp invitation.");
                }

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

            currentStep = "Training camp cuts and new-season handoff";
            var completedSeasonYear = context.ActiveLeague.SeasonYear;
            var overallsBeforeRollover = context.ActiveLeague.Teams
                .SelectMany(team => team.Roster)
                .ToDictionary(player => player.PlayerId, player => player.Overall, StringComparer.OrdinalIgnoreCase);
            var rolloverInjuryPlayer = context.ActiveLeague.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId).Roster.First();
            PlayerInjuryService.InjurePlayer(context.ActiveLeague, rolloverInjuryPlayer, "Shoulder strain", 14, "rollover-injury");
            var trainingCamp = new TrainingCampService(context);
            var blockedRollover = new SeasonRolloverService(context).StartNextSeason(out var blockedRolloverMessage);
            Require(!blockedRollover && blockedRolloverMessage.Contains("training-camp focus", StringComparison.OrdinalIgnoreCase), "Training camp should require a focus and finalized roster before rollover.");
            var userTrainingCampTeam = context.ActiveLeague.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId);
            Require(new DepthChartService(context).AutoFillDepthChart(userTrainingCampTeam.TeamId).Ok, "Training-camp depth chart should auto-fill before finalization.");
            var cutGroup = userTrainingCampTeam.Roster
                .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
                .First(group => group.Count(PlayerInjuryService.IsAvailableForGame) > DepthChartRules.GetRequiredStarters(group.Key));
            var cutTarget = cutGroup.Where(PlayerInjuryService.IsAvailableForGame).OrderBy(player => player.Overall).ThenByDescending(player => player.Age).First();
            var rosterBeforeCutPreview = userTrainingCampTeam.Roster.Count;
            var cutPreview = trainingCamp.PreviewRosterCuts(new[] { cutTarget.PlayerId }, userTrainingCampTeam.TeamId);
            Require(cutPreview.Ok && cutPreview.RosterCountBefore == rosterBeforeCutPreview && cutPreview.RosterCountAfter == rosterBeforeCutPreview - 1 && userTrainingCampTeam.Roster.Contains(cutTarget), "Final cut-down preview should be read-only and report the projected roster count.");
            var cutCommit = trainingCamp.ConfirmRosterCuts(new[] { cutTarget.PlayerId }, userTrainingCampTeam.TeamId);
            Require(cutCommit.Ok && !userTrainingCampTeam.Roster.Contains(cutTarget) && context.ActiveLeague.FreeAgents.Any(player => player.PlayerId == cutTarget.PlayerId), cutCommit.Message);
            Require(context.ActiveLeague.Transactions.Any(transaction => transaction.Type == "player_released" && transaction.PlayerId == cutTarget.PlayerId), "Confirmed final cut-down should record each released player transaction.");
            var campReport = trainingCamp.GenerateReport(userTrainingCampTeam.TeamId);
            Require(campReport.Ok && campReport.Status.Report.Positions.Count > 0 && !string.IsNullOrWhiteSpace(campReport.Status.Report.RecommendedFocusPosition), "Training camp should generate an actionable roster report.");
            Require(campReport.Status.Report.Positions.Any(position => position.UnavailablePlayers > 0), "Training-camp report should include injury availability.");
            var campReportSave = saveService.Save(context, smokeSaveName);
            Require(campReportSave.Ok, campReportSave.Message);
            smokeSaveCreated = true;
            var campReportLoad = saveService.Load(smokeSaveName);
            Require(campReportLoad.Ok && campReportLoad.League.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId).TrainingCamp.Report.Positions.Count > 0, "Training-camp report should persist through save/load.");
            var depthBeforeBattleAssessments = string.Join("|", userTrainingCampTeam.DepthChart.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}:{string.Join(",", pair.Value)}"));
            var battles = new PositionBattleService(context).Resolve(userTrainingCampTeam.TeamId);
            Require(battles.All(battle => !string.IsNullOrWhiteSpace(battle.Explanation)), "Position battles should produce explainable outcomes.");
            var depthAfterBattleAssessments = string.Join("|", userTrainingCampTeam.DepthChart.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}:{string.Join(",", pair.Value)}"));
            Require(string.Equals(depthBeforeBattleAssessments, depthAfterBattleAssessments, StringComparison.Ordinal), "Position-battle staff assessments must not change a GM-controlled depth chart.");
            Require(saveService.Save(context, smokeSaveName).Ok && saveService.Load(smokeSaveName).League.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId).TrainingCamp.PositionBattles.Count == battles.Count, "Position-battle outcomes should persist through save/load.");
            var roleFeedback = new RosterEvaluationService(context).GetPlayerRoles(userTrainingCampTeam.TeamId);
            Require(roleFeedback.Ok && roleFeedback.Players.Count == userTrainingCampTeam.Roster.Count && roleFeedback.Players.Any(player => player.Role == "Unavailable" && player.Explanation.Contains("replacement", StringComparison.OrdinalIgnoreCase)), "Roster evaluation should explain roles, readiness, and unavailable-player substitutions.");
            Require(roleFeedback.Players.All(player => !string.IsNullOrWhiteSpace(player.PlayerId) && !string.IsNullOrWhiteSpace(player.Role) && !string.IsNullOrWhiteSpace(player.Readiness) && !string.IsNullOrWhiteSpace(player.Explanation)), "Roster evaluation should provide complete selected-player presentation fields.");
            var playerFocusTarget = userTrainingCampTeam.Roster.First(player => PlayerInjuryService.IsAvailableForGame(player) && player.Overall < player.Potential);
            var playerFocusOverall = playerFocusTarget.Overall;
            var playerFocusResult = trainingCamp.ApplyPlayerFocus(playerFocusTarget.PlayerId, userTrainingCampTeam.TeamId);
            Require(playerFocusResult.Ok && userTrainingCampTeam.TrainingCamp.PlayerFocusApplied && userTrainingCampTeam.TrainingCamp.FocusPlayerId == playerFocusTarget.PlayerId && playerFocusTarget.Overall == playerFocusOverall + 1, playerFocusResult.Message);
            Require(!trainingCamp.ApplyPlayerFocus(userTrainingCampTeam.Roster.First(player => player.PlayerId != playerFocusTarget.PlayerId).PlayerId, userTrainingCampTeam.TeamId).Ok, "Training camp should accept only one player-focus assignment per offseason.");
            Require(saveService.Save(context, smokeSaveName).Ok && saveService.Load(smokeSaveName).League.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId).TrainingCamp.FocusPlayerId == playerFocusTarget.PlayerId, "Training-camp player focus should persist through save/load.");
            var focusPosition = userTrainingCampTeam.Roster.First(player => PlayerInjuryService.IsAvailableForGame(player)).Position;
            var focusResult = trainingCamp.ApplyPositionFocus(focusPosition, userTrainingCampTeam.TeamId);
            Require(focusResult.Ok && userTrainingCampTeam.TrainingCamp.FocusApplied, focusResult.Message);
            var finalizeCamp = trainingCamp.FinalizeRoster(userTrainingCampTeam.TeamId);
            var quarterbackStatus = string.Join(", ", userTrainingCampTeam.Roster.Where(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase)).Select(player => $"{player.PlayerId}:{player.Status}:{player.Injury}"));
            Require(finalizeCamp.Ok && userTrainingCampTeam.TrainingCamp.RosterFinalized, $"{finalizeCamp.Message} QBs={quarterbackStatus}");
            var newSeasonContinue = continueService.Continue();
            Require(newSeasonContinue.Ok, newSeasonContinue.Error);
            Require(string.Equals(newSeasonContinue.Result.StopReason, "new_season_started", StringComparison.OrdinalIgnoreCase), $"Expected new_season_started, got {newSeasonContinue.Result.StopReason}.");
            Require(context.ActiveLeague.SeasonYear == completedSeasonYear + 1, "Season rollover should advance the season year.");
            Require(string.Equals(context.ActiveLeague.Calendar.Phase, "Preseason", StringComparison.OrdinalIgnoreCase) && context.ActiveLeague.Calendar.AbsoluteWeek == 1, "Season rollover should begin preseason at week 1.");
            Require(context.ActiveLeague.Results.Count == 0 && context.ActiveLeague.PlayoffBracket.ConferenceBrackets.Count == 0, "New season should start with no results or playoff bracket.");
            Require(context.ActiveLeague.Teams.All(team => team.Roster.Count <= RosterService.RosterLimit), "Training camp should leave every team at the active roster limit.");
            Require(context.ActiveLeague.HistoricalDrafts.Any(draft => draft != null && draft.DraftYear == completedSeasonYear && draft.IsCompleted), "Completed draft should be archived during rollover.");
            var archivedDraft = context.ActiveLeague.HistoricalDrafts.Single(draft => draft != null && draft.DraftYear == completedSeasonYear);
            Require(archivedDraft.RecapEntries.Count == archivedDraft.Picks.Count && draftRecapSnapshot != null, "Rollover should archive a recap for every completed draft selection.");
            var archivedRecap = archivedDraft.RecapEntries.First(entry => entry.OverallPick == draftRecapSnapshot.OverallPick);
            Require(archivedRecap.Name == draftRecapSnapshot.Name && archivedRecap.ScoutingReport == draftRecapSnapshot.ScoutingReport && archivedRecap.ContractAnnualSalary == draftRecapSnapshot.ContractAnnualSalary && archivedRecap.PublicBoardRank == draftRecapSnapshot.PublicBoardRank && archivedRecap.PublicReaction == draftRecapSnapshot.PublicReaction, "Archived draft recaps should preserve pre-rollover evaluation, public-reaction, and rookie-contract snapshots.");
            var draftedRookie = context.ActiveLeague.Teams.SelectMany(team => team.Roster).FirstOrDefault(player => player.PlayerId == archivedRecap.PlayerId);
            if (draftedRookie != null)
            {
                draftedRookie.Overall += 1;
                Require(archivedRecap.ScoutingReport == draftRecapSnapshot.ScoutingReport && archivedRecap.ContractAnnualSalary == draftRecapSnapshot.ContractAnnualSalary, "Later rookie development must not rewrite an archived draft recap.");
            }
            var recapHistory = dashboardService.GetLeagueHistory();
            Require(recapHistory.Ok && recapHistory.Seasons.First(season => season.SeasonYear == completedSeasonYear).DraftClass.Count == archivedDraft.RecapEntries.Count, "League history should expose the archived draft-class recap.");
            Require(context.ActiveLeague.HistoricalSeasons.Count(record => record != null && record.SeasonYear == completedSeasonYear) == 1, "Completed season history should survive rollover.");
            var rolloverPlayers = context.ActiveLeague.Teams.SelectMany(team => team.Roster);
            Require(rolloverPlayers.Any(player => player.CareerStats.Any(stat => stat.SeasonYear == completedSeasonYear && stat.GamesPlayed > 0)), "Rollover should archive completed player season totals.");
            var archivedStatsPlayer = rolloverPlayers.First(player => player.CareerStats.Any(stat => stat.SeasonYear == completedSeasonYear && stat.GamesPlayed > 0));
            var rolloverHistory = new PlayerHistoryService(context).GetPlayerHistory(archivedStatsPlayer.PlayerId, context.ActiveLeague.Teams.First(team => team.Roster.Any(player => player.PlayerId == archivedStatsPlayer.PlayerId)).TeamId);
            Require(rolloverHistory.Ok && rolloverHistory.CurrentSeason.SeasonYear == context.ActiveLeague.SeasonYear && rolloverHistory.CurrentSeason.GamesPlayed == 0 && rolloverHistory.CareerSeasons.Any(stats => stats.SeasonYear == completedSeasonYear && stats.GamesPlayed > 0), "Rollover player history should present archived seasons separately from new live totals.");
            Require(rolloverPlayers.All(player => player.SeasonStats.GamesPlayed == 0), "New player season totals should start at zero after rollover.");
            Require(rolloverPlayers.All(player => player.Fatigue == 0), "Rollover should clear player fatigue.");
            Require(rolloverPlayers.All(player => !player.CurrentInjury.IsActive && string.IsNullOrWhiteSpace(player.Injury)), "Rollover should clear active injuries.");
            Require(context.ActiveLeague.Teams.All(team => !team.TrainingCamp.FocusApplied && !team.TrainingCamp.RosterFinalized), "Rollover should reset training-camp decisions for the new season.");
            Require(context.ActiveLeague.Teams.All(team => team.TrainingCamp.Report.Positions.Count == 0), "Rollover should clear training-camp reports for the new season.");
            Require(context.ActiveLeague.Draft.UseShortDraftAnnouncements, "The user's shortened draft-announcement preference should persist into the next season.");
            Require(context.ActiveLeague.Teams.All(team => team.TrainingCamp.PositionBattles.Count == 0 && team.TrainingCamp.UserAdjustedPositions.Count == 0), "Rollover should reset position-battle state.");
            Require(rolloverPlayers.Any(player => overallsBeforeRollover.TryGetValue(player.PlayerId, out var priorOverall) && player.Overall != priorOverall), "Rollover should apply age and potential-based player development.");
            var stableRetirements = RetirementService.GetSeasonRetirementRecord(context.ActiveLeague, completedSeasonYear);
            Require(stableRetirements != null && stableRetirements.Completed && stableRetirements.RetiredCount == retiredPlayerCount, "Retirement history should survive rollover.");
            Require(context.ActiveLeague.CollegeProspects.Count == LeagueBootstrapService.StartingProspectCount && context.ActiveLeague.CollegeProspects.All(prospect => prospect.DraftClassYear == context.ActiveLeague.SeasonYear + 1), "Rollover should create the next prospect class.");
            Pass(result, currentStep);

            currentStep = "Three-season continuity";
            ValidateThreeSeasonContinuity(teamSeedPath);
            Pass(result, currentStep);

            currentStep = "Historical record book";
            var recordBookSnapshot = new RecordBookService(context).GetRecordBook();
            Require(recordBookSnapshot.Ok && recordBookSnapshot.SeasonRecords.Count > 0 && recordBookSnapshot.CareerRecords.Count > 0 && recordBookSnapshot.FranchiseRecords.Count > 0, "Record book should derive season, career, and franchise records from authoritative history.");
            Require(recordBookSnapshot.CareerRecords.All(entry => entry.Value > 0 && !string.IsNullOrWhiteSpace(entry.SubjectName)) && recordBookSnapshot.FranchiseRecords.All(entry => entry.Value > 0 && !string.IsNullOrWhiteSpace(entry.SubjectName)), "Derived record-book entries should be complete and non-empty.");
            Pass(result, currentStep);

            currentStep = "Historical archive browser";
            var archiveSnapshot = dashboardService.GetHistoricalArchive();
            Require(archiveSnapshot.Ok && archiveSnapshot.RecordBook?.Ok == true && archiveSnapshot.Championships.Count == 1 && archiveSnapshot.Championships[0].SeasonYear == completedSeasonYear && archiveSnapshot.Retirements.All(retirement => !string.IsNullOrWhiteSpace(retirement.PlayerName)), "Historical archive should expose read-only records, championships, and retirement history.");
            Pass(result, currentStep);

            currentStep = "Season awards";
            var awardSeason = context.ActiveLeague.HistoricalSeasons.Single(record => record.SeasonYear == completedSeasonYear);
            Require(awardSeason.Awards.Count == 3 && awardSeason.Awards.Select(award => award.AwardName).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 3 && awardSeason.Awards.All(award => !string.IsNullOrWhiteSpace(award.PlayerId) && !string.IsNullOrWhiteSpace(award.Summary) && award.Score > 0), "Completed seasons should persist a deterministic compact award slate.");
            var awardSnapshot = string.Join("|", awardSeason.Awards.Select(award => $"{award.AwardName}:{award.PlayerId}:{award.Score}"));
            const string awardsMigrationSaveName = "native_smoke_awards_migration.json";
            awardSeason.Awards = null;
            context.ActiveLeague.SaveVersion = LeagueState.CurrentSaveVersion - 1;
            Require(saveService.Save(context, awardsMigrationSaveName).Ok, "Legacy awards smoke save should succeed.");
            var awardsMigrationLoad = saveService.Load(awardsMigrationSaveName);
            Require(awardsMigrationLoad.Ok && awardsMigrationLoad.League.HistoricalSeasons.Single(record => record.SeasonYear == completedSeasonYear).Awards.Count == 3, "Legacy season archives should safely derive missing awards on load.");
            Require(string.Join("|", awardsMigrationLoad.League.HistoricalSeasons.Single(record => record.SeasonYear == completedSeasonYear).Awards.Select(award => $"{award.AwardName}:{award.PlayerId}:{award.Score}")) == awardSnapshot, "Migrated awards should remain deterministic.");
            Require(saveService.Delete(awardsMigrationSaveName).Ok, "Awards migration smoke save should clean up.");
            context.ActiveLeague = awardsMigrationLoad.League;
            Pass(result, currentStep);

            currentStep = "Save native league";
            var persistedInjuryPlayer = context.ActiveLeague.Teams.First(team => team.TeamId == context.ActiveLeague.UserTeamId).Roster.First();
            PlayerInjuryService.InjurePlayer(context.ActiveLeague, persistedInjuryPlayer, "Ankle sprain", 5, "persistence-injury");
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
            Require(loadedContext.ActiveLeague.SalaryCap == context.ActiveLeague.SalaryCap, "Loaded league did not preserve the salary cap.");
            Require(loadedContext.ActiveLeague.FreeAgents.Count == context.ActiveLeague.FreeAgents.Count, "Loaded league did not preserve free agents.");
            Require(loadedContext.ActiveLeague.Transactions.Count == context.ActiveLeague.Transactions.Count, "Loaded league did not preserve transaction history.");
            Require(loadedContext.ActiveLeague.Transactions.Any(transaction => transaction.Type == "training_camp_decision"), "Loaded league did not preserve training-camp decision history.");
            var loadedInjuryPlayer = loadedContext.ActiveLeague.Teams.SelectMany(team => team.Roster).First(player => player.PlayerId == persistedInjuryPlayer.PlayerId);
            Require(loadedInjuryPlayer.CurrentInjury.IsActive && loadedInjuryPlayer.CurrentInjury.DaysRemaining == 5 && loadedInjuryPlayer.InjuryHistory.Any(record => record.GameId == "persistence-injury"), "Loaded league did not preserve injury timing and history.");
            Require(loadedContext.ActiveLeague.HistoricalDrafts.Any(draft => draft != null && draft.DraftYear == completedSeasonYear && draft.IsCompleted), "Loaded league did not preserve completed draft history.");
            Require(loadedContext.ActiveLeague.HistoricalDrafts.Single(draft => draft.DraftYear == completedSeasonYear).RecapEntries.Count > 0, "Loaded league did not preserve immutable draft-class recaps.");
            Require(loadedContext.ActiveLeague.Teams.All(team => team.Roster.All(player => player.Contract != null && player.Morale >= 0 && player.Morale <= 100)), "Loaded league did not preserve player contract and morale data.");
            Require(loadedContext.ActiveLeague.Teams.All(team => team.Roster.All(player => !string.IsNullOrWhiteSpace(player.Trait))), "Loaded league did not preserve player traits.");
            Require(loadedContext.ActiveLeague.Teams.Any(team => team.Roster.Any(player => player.DevelopmentHistory.Any(record => record.SeasonYear > 0 && !string.IsNullOrWhiteSpace(record.Note)))), "Loaded league did not preserve annual player development history.");
            Require(loadedContext.ActiveLeague.CollegeProspects.Count == context.ActiveLeague.CollegeProspects.Count, "Loaded league did not preserve college prospects.");
            var loadedRecordBook = new RecordBookService(loadedContext).GetRecordBook();
            Require(loadedRecordBook.Ok && loadedRecordBook.SeasonRecords.Count == recordBookSnapshot.SeasonRecords.Count && loadedRecordBook.CareerRecords.Count == recordBookSnapshot.CareerRecords.Count && loadedRecordBook.FranchiseRecords.Count == recordBookSnapshot.FranchiseRecords.Count && loadedRecordBook.CareerRecords.First().Value == recordBookSnapshot.CareerRecords.First().Value, "Record book should derive identically after save/load.");
            Require(loadedContext.ActiveLeague.HistoricalSeasons.Single(record => record.SeasonYear == completedSeasonYear).Awards.Count == 3, "Loaded league should preserve archived season awards.");
            var loadedArchive = loadedDashboardService.GetHistoricalArchive();
            Require(loadedArchive.Ok && loadedArchive.Championships.Count == archiveSnapshot.Championships.Count && loadedArchive.Retirements.Count == archiveSnapshot.Retirements.Count && loadedArchive.RecordBook?.CareerRecords.Count == archiveSnapshot.RecordBook.CareerRecords.Count, "Historical archive should derive consistently after save/load.");
            var loadedProspectEvaluation = new ProspectEvaluationService(loadedContext).GetEvaluation(loadedContext.ActiveLeague.CollegeProspects.First().ProspectId);
            Require(loadedProspectEvaluation != null && loadedProspectEvaluation.KnownFacts.Contains("Public pro day", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(loadedProspectEvaluation.Report), "Rolled-over prospect classes should load with usable deterministic evaluations.");
            Require(loadedContext.ActiveLeague.Teams.All(team => team.Coaches.Count == LeagueBootstrapService.CoachesPerTeam), "Loaded league did not preserve coaching staffs.");
            var loadedRoleFeedback = new RosterEvaluationService(loadedContext).GetPlayerRoles(loadedContext.ActiveLeague.UserTeamId);
            Require(loadedRoleFeedback.Ok && loadedRoleFeedback.Players.Count > 0 && loadedRoleFeedback.Players.All(player => !string.IsNullOrWhiteSpace(player.Role) && !string.IsNullOrWhiteSpace(player.Readiness) && !string.IsNullOrWhiteSpace(player.Explanation)), "Loaded league should derive complete roster role feedback from persisted state.");
            var loadedArchivedStatsPlayer = loadedContext.ActiveLeague.Teams.SelectMany(team => team.Roster).First(player => player.PlayerId == archivedStatsPlayer.PlayerId);
            var loadedRolloverHistory = new PlayerHistoryService(loadedContext).GetPlayerHistory(loadedArchivedStatsPlayer.PlayerId, loadedContext.ActiveLeague.Teams.First(team => team.Roster.Any(player => player.PlayerId == loadedArchivedStatsPlayer.PlayerId)).TeamId);
            Require(loadedRolloverHistory.Ok && loadedRolloverHistory.CareerSeasons.Any(stats => stats.SeasonYear == completedSeasonYear && stats.GamesPlayed > 0), "Loaded league should retain archived player season history.");
            Require(loadedContext.ActiveLeague.FranchiseMetadata.World.Seed == context.ActiveLeague.FranchiseMetadata.World.Seed, "Loaded league did not preserve the world seed.");
            var loadedStandings = loadedStandingsService.GetStandings();
            Require(loadedStandings.Ok, loadedStandings.Error);
            var loadedUserStanding = loadedStandings.Standings.FirstOrDefault(row => string.Equals(row.TeamId, loadedContext.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
            Require(loadedUserStanding != null, "Loaded standings missing user team.");
            Require(loadedUserStanding.PointsFor == 0 && loadedUserStanding.PointsAgainst == 0, "New-season standings should start at zero after load.");
            var loadedDashboard = loadedDashboardService.GetDashboardState();
            Require(loadedDashboard.Ok, loadedDashboard.Error);
            Require(loadedDashboard.Dashboard.PlayoffBracket != null, "Loaded dashboard should expose playoff bracket DTO.");
            Require(loadedDashboard.Dashboard.PlayoffBracket.ConferenceBrackets.Count == 0, "New season should not retain a playoff bracket.");
            Require(string.Equals(loadedContext.ActiveLeague.Calendar.Phase, "Preseason", StringComparison.OrdinalIgnoreCase), "Loaded league should remain at preseason after rollover.");
            Require(loadedContext.ActiveLeague.Teams.SelectMany(team => team.Roster).Any(player => player.CareerStats.Any(stat => stat.SeasonYear == completedSeasonYear && stat.GamesPlayed > 0)), "Loaded league should retain archived player season totals.");
            var loadedHistoryResponse = loadedDashboardService.GetLeagueHistory();
            Require(loadedHistoryResponse.Ok && loadedHistoryResponse.Seasons.Any(season => season.SeasonYear == completedSeasonYear), "Loaded history should retain the completed season.");
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
            var loadedRetirements = RetirementService.GetSeasonRetirementRecord(loadedContext.ActiveLeague, completedSeasonYear);
            Require(loadedRetirements != null && loadedRetirements.Completed, "Loaded save should preserve retirement history.");
            Require(loadedRetirements.RetiredCount == retiredPlayerCount, "Loaded save should preserve retirement count.");
            ValidateRetirementResults(loadedContext.ActiveLeague, loadedRetirements);
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

    private static void ValidateIncrementalLiveGameSession(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var depth = new DepthChartService(context);
        Require(depth.AutoFillDepthChart().Ok, "Live-game session setup should auto-fill the user depth chart.");
        var continued = new ContinueService(context).Continue(14);
        Require(continued.Ok && string.Equals(continued.Result.StopReason, "game_day", StringComparison.OrdinalIgnoreCase), "Live-game session setup should reach game day.");
        var live = new LiveGameSessionService(context);
        var started = live.Start();
        Require(started.Ok && started.Session.Active && started.Session.IsPaused && started.Session.TotalEvents == 0 && league.Results.All(result => result.GameId != started.Session.GameId), "Starting a live game should create a paused incremental session without prematurely committing the result.");
        var conflictingFullSim = new GameDayService(context).SimulateCurrentUserGame(started.Session.GameId);
        Require(!conflictingFullSim.Ok && conflictingFullSim.Error.Contains("live game session", StringComparison.OrdinalIgnoreCase), "Full-game simulation should not bypass an active incremental session.");

        const string saveName = "native_smoke_live_session.json";
        var saves = new GameCoreSaveService();
        Require(saves.Save(context, saveName).Ok, "Active live-game session should save.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && loaded.League.ActiveLiveGameSession.Active && loaded.League.ActiveLiveGameSession.GameId == started.Session.GameId && loaded.League.ActiveLiveGameSession.NextEventIndex == 0, "Active live-game session should persist through save/load.");
        Require(saves.Delete(saveName).Ok, "Live-game session smoke save should clean up.");

        Require(live.SetPaused(false).Ok, "Live-game session should resume.");
        var firstAdvance = live.Advance();
        Require(firstAdvance.Ok && firstAdvance.Session.CurrentEvent != null && league.Results.All(result => result.GameId != started.Session.GameId), "Advancing a live game should expose one event without committing the final result.");
        var firstDescription = firstAdvance.Session.CurrentEvent.Description;
        Require(live.SetPaused(true).Ok, "Live-game session should pause for adjustments.");
        var qbGroup = depth.GetTeamDepthChart().Positions.First(position => string.Equals(position.Position, "QB", StringComparison.OrdinalIgnoreCase));
        var availableQbs = qbGroup.Players.Where(player => player.IsAvailable).ToList();
        Require(availableQbs.Count > 1, "Live-game adjustment smoke roster should include two available quarterbacks.");
        var adjustment = live.ApplyDepthAdjustment("move_before", "QB", availableQbs[^1].PlayerId, availableQbs[0].PlayerId);
        Require(adjustment.Ok && league.ActiveLiveGameSession.Adjustments.Count == 1 && league.ActiveLiveGameSession.PlayedEvents[0].Description == firstDescription, "A paused depth adjustment should be recorded without rewriting an already played event.");
        Require(live.SetPaused(false).Ok, "Adjusted live-game session should resume.");
        LiveGameSessionResponse advance = null;
        var guard = 0;
        while (league.ActiveLiveGameSession.Active && guard++ < 1000)
        {
            advance = live.Advance();
            Require(advance.Ok, advance.Error);
        }
        Require(guard < 1000 && advance?.Session.Completed == true, "Incremental live-game session should reach completion.");
        Require(league.Results.Count(result => result.GameId == started.Session.GameId) == 1, "Live-game completion should commit exactly one result.");
        Require(league.Schedule.First(game => game.GameId == started.Session.GameId).Status == "final", "Live-game completion should finalize the scheduled game.");
    }

    private static void ValidateDashboardToPostgameLifecycle(string teamSeedPath)
    {
        const string saveName = "native_smoke_vertical_slice.json";
        var saves = new GameCoreSaveService();
        try
        {
            var context = new GameCoreContext();
            var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
            var dashboard = new DashboardService(context).GetDashboardState();
            var roster = new RosterService(context).GetTeamRoster();
            var depth = new DepthChartService(context);
            Require(dashboard.Ok && roster.Ok && roster.Players.Count == 53, "Vertical slice should begin with a bound dashboard and legal roster.");
            var filled = depth.AutoFillDepthChart();
            var group = filled.Positions.First(position => position.Players.Count > 1);
            var moved = group.Players[^1];
            Require(depth.UpdateDepthChart("move_before", group.Position, moved.PlayerId, null, group.Players[0].PlayerId).Ok, "Vertical slice should persist a direct depth reorder.");
            Require(depth.TogglePositionLock(group.Position).Ok, "Vertical slice should persist the chosen position lock.");
            Require(saves.Save(context, saveName).Ok, "Vertical slice should save after roster and depth decisions.");

            var rosterReload = saves.Load(saveName);
            Require(rosterReload.Ok, "Vertical slice roster/depth reload should succeed.");
            var reloadedContext = new GameCoreContext { ActiveLeague = rosterReload.League };
            var reloadedDepth = new DepthChartService(reloadedContext).GetTeamDepthChart();
            var reloadedGroup = reloadedDepth.Positions.First(position => string.Equals(position.Position, group.Position, StringComparison.OrdinalIgnoreCase));
            Require(reloadedGroup.IsLocked && reloadedGroup.Players[0].PlayerId == moved.PlayerId, "Reload should preserve the GM's depth order and lock before game day.");

            var continued = new ContinueService(reloadedContext).Continue(14);
            Require(continued.Ok && string.Equals(continued.Result.StopReason, "game_day", StringComparison.OrdinalIgnoreCase), "Vertical slice should stop at the scheduled user game.");
            var live = new LiveGameSessionService(reloadedContext);
            var started = live.Start();
            Require(started.Ok && started.Session.Active && started.Session.IsPaused, "Vertical slice should enter a paused live observer session.");
            Require(live.SetPaused(false).Ok && live.Advance().Ok && live.Advance().Ok && live.SetPaused(true).Ok, "Vertical slice should advance and pause the live game.");
            Require(saves.Save(reloadedContext, saveName).Ok, "Vertical slice should save while the live game is paused.");

            var liveReload = saves.Load(saveName);
            Require(liveReload.Ok && liveReload.League.ActiveLiveGameSession.Active && liveReload.League.ActiveLiveGameSession.NextEventIndex == 2, "Reload should restore exact live playback progress.");
            var liveContext = new GameCoreContext { ActiveLeague = liveReload.League };
            var resumedLive = new LiveGameSessionService(liveContext);
            var liveDepth = new DepthChartService(liveContext).GetTeamDepthChart();
            var qbGroup = liveDepth.Positions.First(position => string.Equals(position.Position, "QB", StringComparison.OrdinalIgnoreCase));
            var qbs = qbGroup.Players.Where(player => player.IsAvailable).ToList();
            Require(qbs.Count > 1 && resumedLive.ApplyDepthAdjustment("set_starter", "QB", qbs[^1].PlayerId).Ok, "Reloaded live session should accept a validated future-only starter change.");
            Require(resumedLive.SetPaused(false).Ok, "Reloaded live session should resume.");
            var guard = 0;
            LiveGameSessionResponse final = null;
            while (liveContext.ActiveLeague.ActiveLiveGameSession.Active && guard++ < 1000)
            {
                final = resumedLive.Advance();
                Require(final.Ok, final.Error);
            }
            Require(final?.Session.Completed == true && guard < 1000, "Reloaded live session should reach postgame.");
            var completedGameId = final.Session.GameId;
            Require(liveContext.ActiveLeague.Results.Count(result => result.GameId == completedGameId) == 1, "Vertical slice should commit the completed result once.");
            var postgame = new GameDayService(liveContext).GetGameResult(completedGameId);
            Require(postgame.Ok
                && postgame.Result.BoxScore.TryGetValue("play_by_play", out var log)
                && log is List<GamePlayEventState> plays
                && plays.Count > 0
                && plays[^1].HomeScore == postgame.Result.HomeScore
                && plays[^1].AwayScore == postgame.Result.AwayScore,
                "Postgame should expose the immutable final score and complete authoritative game log.");
            Require(saves.Save(liveContext, saveName).Ok, "Vertical slice should save the completed postgame state.");

            var postgameReload = saves.Load(saveName);
            var postgameContext = new GameCoreContext { ActiveLeague = postgameReload.League };
            var reloadedResult = new GameDayService(postgameContext).GetGameResult(completedGameId);
            var refreshedDashboard = new DashboardService(postgameContext).GetDashboardState();
            Require(postgameReload.Ok && reloadedResult.Ok && refreshedDashboard.Ok
                && postgameContext.ActiveLeague.Results.Count(result => result.GameId == completedGameId) == 1
                && refreshedDashboard.Dashboard.RecentResults.Any(result => result.GameId == completedGameId),
                "Postgame reload should retain one immutable result and return it to the refreshed dashboard.");
        }
        finally
        {
            saves.Delete(saveName);
        }
    }

    private static void ValidateThreeSeasonContinuity(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var saveService = new GameCoreSaveService();
        const string saveName = "native_smoke_three_season.json";
        try
        {
            // Keep this endurance scenario focused on lifecycle stability rather than free-agency decision quality.
            foreach (var player in league.Teams.SelectMany(team => team.Roster))
                player.Contract.YearsRemaining = Math.Max(4, player.Contract.YearsRemaining);

            var startingYear = league.SeasonYear;
            for (var completedSeasonCount = 0; completedSeasonCount < 3; completedSeasonCount++)
            {
                var seasonYear = league.SeasonYear;
                var gameDay = new GameDayService(context);
                foreach (var game in league.Schedule.Where(game => game != null).OrderBy(game => game.AbsoluteWeek).ThenBy(game => game.GameId, StringComparer.OrdinalIgnoreCase))
                {
                    var simulated = gameDay.SimulateScheduledGame(game.GameId, allowUserTeamGame: true);
                    Require(simulated.Ok, simulated.Error);
                }
                Require(league.Results.Count == LeagueBootstrapService.ExpectedScheduleGameCount, $"Season {seasonYear} should resolve every scheduled pro game exactly once.");

                var college = new CollegeUniverseService(context);
                for (var collegeWeek = 1; collegeWeek <= CollegeUniverseService.RegularSeasonWeeks; collegeWeek++)
                    college.AdvanceToProWeek(collegeWeek);
                var collegeResultCount = league.CollegeUniverse.Results.Count;
                Require(collegeResultCount == league.CollegeUniverse.Schedule.Count && league.CollegeUniverse.Teams.All(team => team.Ranking > 0) && league.CollegeUniverse.Players.Any(player => player.GamesPlayed > 0), $"Season {seasonYear} college universe should complete deterministically.");
                college.AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
                Require(league.CollegeUniverse.Results.Count == collegeResultCount, $"Season {seasonYear} college advancement should remain idempotent.");

                league.Calendar.AbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + 1;
                league.Calendar.Week = league.Calendar.AbsoluteWeek;
                league.Calendar.DayIndex = 0;
                ScheduleService.NormalizeCalendar(league.Calendar);
                var continueService = new ContinueService(context);
                var guard = 0;
                while (!string.Equals(league.Calendar.Phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase))
                {
                    var advanced = continueService.Continue();
                    Require(advanced.Ok, advanced.Error);
                    Require(++guard < 8, $"Season {seasonYear} playoffs did not complete.");
                }
                Require(league.PlayoffBracket.LeagueChampionRecord != null && !string.IsNullOrWhiteSpace(league.PlayoffBracket.LeagueChampionRecord.ChampionTeamId), $"Season {seasonYear} should record a league champion.");

                guard = 0;
                while (!string.Equals(league.Calendar.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(league.Calendar.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
                    {
                        var draft = new DraftService(context);
                        var pick = draft.GetCurrentPick();
                        Require(pick != null && string.Equals(pick.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase), $"Season {seasonYear} draft should stop for the user team.");
                        var prospect = league.CollegeProspects.FirstOrDefault(candidate => candidate != null && string.IsNullOrWhiteSpace(candidate.DraftedByTeamId));
                        Require(prospect != null && draft.MakePick(league.UserTeamId, prospect.ProspectId), draft.LastMessage);
                    }
                    else
                    {
                        var advanced = continueService.Continue();
                        Require(advanced.Ok, advanced.Error);
                    }
                    Require(++guard < 600, $"Season {seasonYear} offseason did not reach training camp.");
                }

                var userTeam = league.Teams.First(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
                var contracts = new ContractService(context);
                PrepareUserTrainingCampRoster(context, userTeam, contracts);
                new RosterConstructionService(context).ProcessCpuTrainingCampCuts();
                var depth = new DepthChartService(context);
                Require(depth.AutoFillDepthChart(userTeam.TeamId).Ok, $"Season {seasonYear} should auto-fill a legal user depth chart before rollover.");
                var camp = new TrainingCampService(context);
                var focus = userTeam.Roster.First(player => PlayerInjuryService.IsAvailableForGame(player)).Position;
                var focusResult = camp.ApplyPositionFocus(focus, userTeam.TeamId);
                Require(focusResult.Ok, $"Season {seasonYear} training-camp focus failed: {focusResult.Message}");
                var finalizeResult = camp.FinalizeRoster(userTeam.TeamId);
                Require(finalizeResult.Ok, $"Season {seasonYear} training-camp finalization failed: {finalizeResult.Message}");
                Require(new SeasonRolloverService(context).StartNextSeason(out var rolloverMessage), rolloverMessage);

                Require(league.SeasonYear == seasonYear + 1 && league.HistoricalSeasons.Count(record => record != null && record.SeasonYear == seasonYear) == 1, $"Season {seasonYear} rollover should preserve exactly one history record.");
                Require(league.HistoricalDrafts.Count(draft => draft != null && draft.DraftYear == seasonYear) == 1 && league.CollegeProspects.Count == LeagueBootstrapService.StartingProspectCount && league.CollegeProspects.All(prospect => prospect.DraftClassYear == league.SeasonYear + 1), $"Season {seasonYear} rollover should archive the draft and create one next-year draft pool.");
                Require(league.CollegeUniverse.SeasonYear == league.SeasonYear && league.CollegeUniverse.LastAdvancedAbsoluteWeek == 0 && league.CollegeUniverse.Results.Count == 0 && league.CollegeUniverse.Teams.Count == CollegeTeamCatalog.TeamCount, $"Season {seasonYear} rollover should reset the persisted college universe.");
                Require(league.CollegeSeasonArchives.Count(record => record != null && record.SeasonYear == seasonYear) == 1 && league.CollegeSeasonArchives.Single(record => record.SeasonYear == seasonYear).PostseasonGames.Count == 15, $"Season {seasonYear} rollover should retain immutable college postseason context.");
                Require(league.Teams.All(team => team.Roster.Count <= RosterService.RosterLimit && new ContractService(context).GetCapRoom(team) >= 0m), $"Season {seasonYear} rollover should preserve legal roster and cap state.");
                var recordBook = new RecordBookService(context).GetRecordBook();
                Require(recordBook.Ok && recordBook.SeasonRecords.Count > 0 && recordBook.CareerRecords.Count > 0 && recordBook.FranchiseRecords.Count > 0, $"Season {seasonYear} should retain a complete derived record book after rollover.");
                Require(league.HistoricalSeasons.Single(record => record.SeasonYear == seasonYear).Awards.Count == 3, $"Season {seasonYear} should archive a compact deterministic awards slate.");
                var archive = new DashboardService(context).GetHistoricalArchive();
                Require(archive.Ok && archive.Championships.Count == completedSeasonCount + 1 && archive.RecordBook?.Ok == true, $"Season {seasonYear} should remain browseable through the read-only historical archive.");

                Require(saveService.Save(context, saveName).Ok, $"Season {seasonYear} continuity save should succeed.");
                var loaded = saveService.Load(saveName);
                Require(loaded.Ok && loaded.League != null && loaded.League.SeasonYear == league.SeasonYear && loaded.League.HistoricalSeasons.Count(record => record != null && record.SeasonYear >= startingYear) == completedSeasonCount + 1, $"Season {seasonYear} continuity save/load should preserve accumulated history.");
                context.ActiveLeague = loaded.League;
                league = context.ActiveLeague;
            }
        }
        finally
        {
            saveService.Delete(saveName);
        }
    }

    private static void ValidateCollegePostseason(string teamSeedPath)
    {
        var context = new GameCoreContext(); new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(context).AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        var postseason = context.ActiveLeague.CollegeUniverse.Postseason;
        Require(postseason.Completed && postseason.RuleVersion == CollegePostseasonService.RuleVersion && postseason.Games.Count == 15 && postseason.Games.Count(game => game.Stage != "Bowl") == 11 && postseason.Games.Count(game => game.Stage == "Bowl") == 4 && postseason.Games.All(game => !string.IsNullOrWhiteSpace(game.WinnerTeamId)), "Completed college regular seasons should generate a deterministic persisted 12-team postseason slate.");
        var snapshot = string.Join("|", postseason.Games.Select(game => $"{game.Label}:{game.WinnerTeamId}:{game.HomeScore}:{game.AwayScore}"));
        var saves = new GameCoreSaveService(); const string saveName = "native_smoke_college_postseason.json";
        Require(saves.Save(context, saveName).Ok, "College-postseason smoke save should succeed."); var loaded = saves.Load(saveName);
        Require(loaded.Ok && string.Join("|", loaded.League.CollegeUniverse.Postseason.Games.Select(game => $"{game.Label}:{game.WinnerTeamId}:{game.HomeScore}:{game.AwayScore}")) == snapshot, "College postseason should survive save/load.");
        Require(saves.Delete(saveName).Ok, "College-postseason smoke save should clean up.");
    }

    private static void ValidateCollegeBigBoards(string teamSeedPath)
    {
        var context = new GameCoreContext();
        new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(context).AdvanceToProWeek(4);
        var boards = new CollegeBigBoardService(context).GetBoards();
        Require(boards.Ok && boards.Boards.Count == 2 && boards.Boards.All(board => board.Entries.Count > 0) && !boards.Boards[0].Entries.Select(entry => entry.ProspectId).SequenceEqual(boards.Boards[1].Entries.Select(entry => entry.ProspectId)), "Public college boards should provide distinct deterministic analyst and media rankings without private scouting output.");
        var snapshot = string.Join("|", boards.Boards.Select(board => $"{board.Name}:{string.Join(",", board.Entries.Select(entry => entry.ProspectId))}"));
        var saves = new GameCoreSaveService();
        const string saveName = "native_smoke_college_big_boards.json";
        Require(saves.Save(context, saveName).Ok, "College-big-boards smoke save should succeed.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && string.Join("|", new CollegeBigBoardService(new GameCoreContext { ActiveLeague = loaded.League }).GetBoards().Boards.Select(board => $"{board.Name}:{string.Join(",", board.Entries.Select(entry => entry.ProspectId))}")) == snapshot, "Public college boards should remain deterministic through save/load.");
        Require(saves.Delete(saveName).Ok, "College-big-boards smoke save should clean up.");
    }

    private static void ValidateCollegePlayerInjuries(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var college = new CollegeUniverseService(context);
        college.AdvanceToProWeek(1);
        Require(league.CollegeUniverse.Players.Any(player => player.InjuryHistory.Count > 0), "College game simulation should create deterministic injury history.");
        var unavailable = league.CollegeUniverse.Players.OrderBy(player => player.PlayerId, StringComparer.Ordinal).First();
        var gamesBefore = unavailable.GamesPlayed;
        unavailable.CurrentInjury = new CollegePlayerInjuryState { Name = "Smoke-test strain", WeeksRemaining = 2, OccurredInWeek = 1, GameId = "smoke-college-injury" };
        unavailable.InjuryHistory.Add(new CollegePlayerInjuryRecord { SeasonYear = league.SeasonYear, Name = unavailable.CurrentInjury.Name, WeeksOut = 2, OccurredInWeek = 1, GameId = unavailable.CurrentInjury.GameId });
        college.AdvanceToProWeek(2);
        Require(unavailable.GamesPlayed == gamesBefore && unavailable.CurrentInjury.IsActive && unavailable.CurrentInjury.WeeksRemaining == 1, "College injuries should exclude unavailable players from the next game and recover by college week.");
        college.AdvanceToProWeek(3);
        Require(!unavailable.CurrentInjury.IsActive && unavailable.InjuryHistory.Any(record => record.GameId == "smoke-college-injury" && record.RecoveredInWeek == 3), "College injury recovery should record the recovered week.");

        var snapshot = string.Join("|", league.CollegeUniverse.Players.OrderBy(player => player.PlayerId, StringComparer.Ordinal).Select(player => $"{player.PlayerId}:{player.GamesPlayed}:{string.Join(",", player.InjuryHistory.Select(record => $"{record.Name}:{record.WeeksOut}:{record.RecoveredInWeek}"))}"));
        var saves = new GameCoreSaveService();
        const string saveName = "native_smoke_college_injuries.json";
        Require(saves.Save(context, saveName).Ok, "College-injuries smoke save should succeed.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && string.Join("|", loaded.League.CollegeUniverse.Players.OrderBy(player => player.PlayerId, StringComparer.Ordinal).Select(player => $"{player.PlayerId}:{player.GamesPlayed}:{string.Join(",", player.InjuryHistory.Select(record => $"{record.Name}:{record.WeeksOut}:{record.RecoveredInWeek}"))}")) == snapshot, "College injuries should survive save/load.");
        Require(saves.Delete(saveName).Ok, "College-injuries smoke save should clean up.");

        var legacyContext = new GameCoreContext();
        var legacyLeague = new LeagueBootstrapService(legacyContext).CreateTestLeague(teamSeedPath);
        legacyLeague.SaveVersion = LeagueState.CurrentSaveVersion - 1;
        legacyLeague.CollegeUniverse.Players.First().CurrentInjury = null;
        legacyLeague.CollegeUniverse.Players.First().InjuryHistory = null;
        const string migrationSaveName = "native_smoke_college_injuries_migration.json";
        Require(saves.Save(legacyContext, migrationSaveName).Ok, "Legacy college-injuries smoke save should succeed.");
        var migrated = saves.Load(migrationSaveName);
        Require(migrated.Ok && migrated.League.SaveVersion == LeagueState.CurrentSaveVersion && migrated.League.CollegeUniverse.Players.All(player => player.CurrentInjury != null && player.InjuryHistory != null), "Legacy college saves should safely receive empty injury state and history.");
        Require(saves.Delete(migrationSaveName).Ok, "College-injuries migration smoke save should clean up.");
    }

    private static void ValidateCollegeNews(string teamSeedPath)
    {
        var context = new GameCoreContext();
        new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(context).AdvanceToProWeek(4);
        var news = new CollegeNewsService(context).GetNews();
        Require(news.Ok && news.Items.Count > 0 && news.Items.Any(item => item.Category == "RESULT") && news.Items.Any(item => item.Category == "RANKING") && news.Items.Any(item => item.Category == "PERFORMANCE"), "College news should derive result, ranking, and performance hooks from authoritative college state.");
        var snapshot = string.Join("|", news.Items.Select(item => $"{item.Category}:{item.ProAbsoluteWeek}:{item.Headline}:{item.Detail}"));
        var saves = new GameCoreSaveService();
        const string saveName = "native_smoke_college_news.json";
        Require(saves.Save(context, saveName).Ok, "College-news smoke save should succeed.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && string.Join("|", new CollegeNewsService(new GameCoreContext { ActiveLeague = loaded.League }).GetNews().Items.Select(item => $"{item.Category}:{item.ProAbsoluteWeek}:{item.Headline}:{item.Detail}")) == snapshot, "College news should remain deterministic through save/load.");
        Require(saves.Delete(saveName).Ok, "College-news smoke save should clean up.");
    }

    private static void ValidateCollegePlayerDevelopment(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(context).AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        var developed = league.CollegeUniverse.Players.Where(player => player != null).ToList();
        Require(developed.Count > 0 && developed.All(player => player.DevelopmentHistory.Count(record => record.SeasonYear == league.SeasonYear) == 1 && player.Overall >= 40 && player.Overall <= player.Potential), "Completed college seasons should apply one bounded development record to every active college player.");
        var snapshot = string.Join("|", developed.OrderBy(player => player.PlayerId, StringComparer.Ordinal).Select(player => $"{player.PlayerId}:{player.Overall}:{player.DevelopmentHistory.Single(record => record.SeasonYear == league.SeasonYear).Reason}"));
        new CollegeUniverseService(context).AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        Require(string.Join("|", league.CollegeUniverse.Players.OrderBy(player => player.PlayerId, StringComparer.Ordinal).Select(player => $"{player.PlayerId}:{player.Overall}:{player.DevelopmentHistory.Single(record => record.SeasonYear == league.SeasonYear).Reason}")) == snapshot, "College development should be idempotent after the completed season.");
        var saves = new GameCoreSaveService();
        const string saveName = "native_smoke_college_development.json";
        Require(saves.Save(context, saveName).Ok, "College-development smoke save should succeed.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && string.Join("|", loaded.League.CollegeUniverse.Players.OrderBy(player => player.PlayerId, StringComparer.Ordinal).Select(player => $"{player.PlayerId}:{player.Overall}:{player.DevelopmentHistory.Single(record => record.SeasonYear == loaded.League.SeasonYear).Reason}")) == snapshot, "College development history should survive save/load.");
        Require(saves.Delete(saveName).Ok, "College-development smoke save should clean up.");

        var legacyContext = new GameCoreContext();
        var legacyLeague = new LeagueBootstrapService(legacyContext).CreateTestLeague(teamSeedPath);
        legacyLeague.SaveVersion = LeagueState.CurrentSaveVersion - 1;
        legacyLeague.CollegeUniverse.Players.First().DevelopmentHistory = null;
        const string migrationSaveName = "native_smoke_college_development_migration.json";
        Require(saves.Save(legacyContext, migrationSaveName).Ok, "Legacy college-development smoke save should succeed.");
        var migrated = saves.Load(migrationSaveName);
        Require(migrated.Ok && migrated.League.SaveVersion == LeagueState.CurrentSaveVersion && migrated.League.CollegeUniverse.Players.All(player => player.DevelopmentHistory != null), "Legacy college saves should safely receive an empty development history.");
        Require(saves.Delete(migrationSaveName).Ok, "College-development migration smoke save should clean up.");
    }

    private static void ValidateCollegePostseasonProjections(string teamSeedPath)
    {
        var context = new GameCoreContext();
        new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(context).AdvanceToProWeek(4);
        var projections = new CollegePostseasonProjectionService(context).GetProjections();
        Require(projections.Ok && projections.RuleVersion == CollegePostseasonService.RuleVersion && projections.FirstRoundByes.Count == 4 && projections.PlayoffMatchups.Count == 4 && projections.BowlMatchups.Count == 4 && projections.FirstRoundByes.Concat(projections.PlayoffMatchups.SelectMany(matchup => new[] { matchup.Home, matchup.Away })).Select(team => team.TeamId).Distinct().Count() == 12 && projections.FirstRoundByes.Concat(projections.PlayoffMatchups.SelectMany(matchup => new[] { matchup.Home, matchup.Away })).Count(team => team.SelectionReason == "Conference champion auto-bid") == 5, "College postseason projections should derive a versioned 12-team field with five conference-champion auto-bids, four byes, and four bowls from current rankings.");
        var snapshot = SnapshotCollegeProjections(projections);
        var saves = new GameCoreSaveService();
        const string saveName = "native_smoke_college_postseason_projections.json";
        Require(saves.Save(context, saveName).Ok, "College-postseason-projections smoke save should succeed.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && SnapshotCollegeProjections(new CollegePostseasonProjectionService(new GameCoreContext { ActiveLeague = loaded.League }).GetProjections()) == snapshot, "College postseason projections should remain deterministic through save/load.");
        Require(saves.Delete(saveName).Ok, "College-postseason-projections smoke save should clean up.");
    }

    private static string SnapshotCollegeProjections(CollegePostseasonProjectionResult projections)
        => $"{projections.RuleVersion}|{string.Join(",", projections.FirstRoundByes.Select(team => team.TeamId))}|{string.Join("|", projections.PlayoffMatchups.Concat(projections.BowlMatchups).Select(matchup => $"{matchup.Label}:{matchup.Home.TeamId}:{matchup.Away.TeamId}"))}";

    private static void ValidateCollegeLeaders(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(context).AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        var leaders = new CollegeLeadersService(context).GetLeaders();
        Require(leaders.Ok && leaders.SeasonYear == league.SeasonYear && leaders.Categories.Count == 4 && leaders.Categories.All(category => category.Leaders.Count > 0 && category.Leaders.SequenceEqual(category.Leaders.OrderByDescending(entry => entry.Value).ThenByDescending(entry => league.CollegeUniverse.Players.First(player => player.PlayerId == entry.PlayerId).Touchdowns).ThenBy(entry => entry.PlayerName, StringComparer.Ordinal).ThenBy(entry => entry.PlayerId, StringComparer.Ordinal))), "College leader tables should be complete and deterministic from authoritative season statistics.");
        var snapshot = string.Join("|", leaders.Categories.Select(category => $"{category.Name}:{string.Join(",", category.Leaders.Select(entry => $"{entry.PlayerId}:{entry.Value}"))}"));
        var saves = new GameCoreSaveService();
        const string saveName = "native_smoke_college_leaders.json";
        Require(saves.Save(context, saveName).Ok, "College-leaders smoke save should succeed.");
        var loaded = saves.Load(saveName);
        Require(loaded.Ok && string.Join("|", new CollegeLeadersService(new GameCoreContext { ActiveLeague = loaded.League }).GetLeaders().Categories.Select(category => $"{category.Name}:{string.Join(",", category.Leaders.Select(entry => $"{entry.PlayerId}:{entry.Value}"))}")) == snapshot, "College leader tables should remain deterministic through save/load.");
        Require(saves.Delete(saveName).Ok, "College-leaders smoke save should clean up.");
    }

    private static void ValidateCollegeAwards(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var college = new CollegeUniverseService(context);
        college.AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        var awards = league.CollegeUniverse.Awards;
        Require(awards.Count == 3 && awards.Select(award => award.AwardName).Distinct(StringComparer.Ordinal).Count() == 3 && awards.All(award => !string.IsNullOrWhiteSpace(award.PlayerId) && !string.IsNullOrWhiteSpace(award.Summary) && award.Score > 0), "Completed college schedules should persist a compact deterministic awards slate.");
        var snapshot = string.Join("|", awards.Select(award => $"{award.AwardName}:{award.PlayerId}:{award.Score}"));
        college.AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        Require(string.Join("|", league.CollegeUniverse.Awards.Select(award => $"{award.AwardName}:{award.PlayerId}:{award.Score}")) == snapshot, "College awards should remain immutable after repeated advancement.");

        var saves = new GameCoreSaveService();
        const string awardsSaveName = "native_smoke_college_awards.json";
        Require(saves.Save(context, awardsSaveName).Ok, "College-awards smoke save should succeed.");
        var loaded = saves.Load(awardsSaveName);
        Require(loaded.Ok && string.Join("|", loaded.League.CollegeUniverse.Awards.Select(award => $"{award.AwardName}:{award.PlayerId}:{award.Score}")) == snapshot, "College awards should survive save/load.");
        Require(saves.Delete(awardsSaveName).Ok, "College-awards smoke save should clean up.");

        var legacyContext = new GameCoreContext();
        var legacyLeague = new LeagueBootstrapService(legacyContext).CreateTestLeague(teamSeedPath);
        new CollegeUniverseService(legacyContext).AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        legacyLeague.SaveVersion = LeagueState.CurrentSaveVersion - 1;
        legacyLeague.CollegeUniverse.Awards = null;
        const string migrationSaveName = "native_smoke_college_awards_migration.json";
        Require(saves.Save(legacyContext, migrationSaveName).Ok, "Legacy college-awards smoke save should succeed.");
        var migrated = saves.Load(migrationSaveName);
        Require(migrated.Ok && migrated.League.SaveVersion == LeagueState.CurrentSaveVersion && migrated.League.CollegeUniverse.Awards.Count == 3, "Legacy completed college seasons should safely derive missing awards on load.");
        Require(string.Join("|", migrated.League.CollegeUniverse.Awards.Select(award => $"{award.AwardName}:{award.PlayerId}:{award.Score}")) == snapshot, "Migrated college awards should remain deterministic.");
        Require(saves.Delete(migrationSaveName).Ok, "College-awards migration smoke save should clean up.");
    }

    private static void ValidateCollegeDraftPipeline(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var college = new CollegeUniverseService(context);
        college.AdvanceToProWeek(CollegeUniverseService.RegularSeasonWeeks);
        var juniors = league.CollegeUniverse.Players.Where(player => player.ClassYear == 3 && !league.CollegeProspects.Any(prospect => string.Equals(prospect.CollegePlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase))).Take(2).ToList();
        Require(juniors.Count == 2, "College draft-pipeline smoke setup requires two unlisted juniors.");
        juniors[0].Overall = 82; juniors[0].Potential = 90; juniors[0].Touchdowns = 8;
        juniors[1].Overall = 58; juniors[1].Potential = 62; juniors[1].Touchdowns = 0;

        var pipeline = new CollegeDraftPipelineService(context);
        var result = pipeline.FinalizeCurrentDraftClass();
        Require(result.DeclaredCount > 0 && result.ReturnedCount > 0 && result.AddedToDraftPool > 0, "Completed college season should deterministically record declarations, returns, and new draft-pool entries.");
        Require(juniors[0].DraftDecision == "Declared" && juniors[1].DraftDecision == "Returned", "College declaration rules should distinguish high-projection juniors from returning juniors.");
        var declaredProspect = league.CollegeProspects.Single(prospect => string.Equals(prospect.CollegePlayerId, juniors[0].PlayerId, StringComparison.OrdinalIgnoreCase));
        Require(declaredProspect.DeclarationStatus == "Declared" && !string.IsNullOrWhiteSpace(declaredProspect.DeclarationRationale) && !string.IsNullOrWhiteSpace(declaredProspect.DraftStock), "Declared college players should enter the pro draft pool with explainable decision and stock context.");
        Require(!league.CollegeUniverse.Players.Any(player => string.Equals(player.PlayerId, juniors[0].PlayerId, StringComparison.OrdinalIgnoreCase)), "A declared player must leave the college-owned player pool when the linked draft record becomes authoritative.");
        Require(!league.CollegeProspects.Any(prospect => string.Equals(prospect.CollegePlayerId, juniors[1].PlayerId, StringComparison.OrdinalIgnoreCase)), "Returning college players must remain outside the pro draft pool.");
        Require(league.CollegeProspects.Where(prospect => !string.IsNullOrWhiteSpace(prospect.CollegePlayerId)).Select(prospect => prospect.CollegePlayerId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == league.CollegeProspects.Count(prospect => !string.IsNullOrWhiteSpace(prospect.CollegePlayerId)), "College players must map to at most one draft-pool record.");
        Require(pipeline.FinalizeCurrentDraftClass().AlreadyFinalized && league.CollegeProspects.Count(prospect => string.Equals(prospect.CollegePlayerId, juniors[0].PlayerId, StringComparison.OrdinalIgnoreCase)) == 1, "Repeated draft-class finalization should be idempotent.");
        var evaluation = new ProspectEvaluationService(context).GetEvaluation(declaredProspect.ProspectId);
        Require(evaluation != null && evaluation.KnownFacts.Contains("Draft outlook:", StringComparison.OrdinalIgnoreCase) && evaluation.KnownFacts.Contains(declaredProspect.DraftStock, StringComparison.OrdinalIgnoreCase), "Scouting evaluation should expose compact public draft-stock context without hidden ratings.");

        const string collegePipelineSaveName = "native_smoke_college_pipeline.json";
        var saves = new GameCoreSaveService();
        Require(saves.Save(context, collegePipelineSaveName).Ok, "College draft-pipeline smoke save should succeed.");
        var loaded = saves.Load(collegePipelineSaveName);
        Require(loaded.Ok && loaded.League.CollegeUniverse.DraftClassFinalized && loaded.League.CollegeProspects.Any(prospect => string.Equals(prospect.CollegePlayerId, juniors[0].PlayerId, StringComparison.OrdinalIgnoreCase) && prospect.DeclarationRationale == declaredProspect.DeclarationRationale), "College declaration and draft-stock context should survive save/load.");
        Require(saves.Delete(collegePipelineSaveName).Ok, "College draft-pipeline smoke save should clean up.");

        var legacyContext = new GameCoreContext();
        var legacyLeague = new LeagueBootstrapService(legacyContext).CreateTestLeague(teamSeedPath);
        legacyLeague.SaveVersion = LeagueState.CurrentSaveVersion - 1;
        legacyLeague.CollegeProspects.First().DraftStock = null;
        legacyLeague.CollegeUniverse.Players.First().DraftDecision = null;
        const string collegePipelineMigrationSaveName = "native_smoke_college_pipeline_migration.json";
        Require(saves.Save(legacyContext, collegePipelineMigrationSaveName).Ok, "Legacy college-pipeline smoke save should succeed.");
        var migrated = saves.Load(collegePipelineMigrationSaveName);
        Require(migrated.Ok && !string.IsNullOrWhiteSpace(migrated.League.CollegeProspects.First().DraftStock) && !string.IsNullOrWhiteSpace(migrated.League.CollegeUniverse.Players.First().DraftDecision) && migrated.League.SaveVersion == LeagueState.CurrentSaveVersion, "Legacy college saves should safely receive declaration and draft-stock defaults.");
        Require(saves.Delete(collegePipelineMigrationSaveName).Ok, "College-pipeline migration smoke save should clean up.");
    }

    private static void ValidateCpuRosterManagement(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var contracts = new ContractService(context);
        var cpuTeam = league.Teams
            .Where(team => !string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contracts.GetCapRoom)
            .ThenBy(team => team.TeamId, StringComparer.OrdinalIgnoreCase)
            .First();
        var userRosterIds = league.Teams
            .First(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase))
            .Roster.Select(player => player.PlayerId).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray();

        league.Calendar.Phase = ScheduleService.FreeAgencyPendingPhase;
        foreach (var quarterback in cpuTeam.Roster.Where(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase)).ToList())
            Require(contracts.ReleasePlayer(quarterback.PlayerId, cpuTeam.TeamId).Accepted, "CPU repair smoke setup should release CPU quarterbacks through transactions.");
        var repairService = new CpuRosterManagementService(context);
        var freeAgencyRepair = repairService.RepairCpuStarterShortages();
        var freeAgentQuarterbacks = league.FreeAgents.Count(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase));
        Require(freeAgencyRepair.Signings > 0 && cpuTeam.Roster.Any(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase)), $"CPU free-agency repair should fill an immediate starter shortage (signings={freeAgencyRepair.Signings}, cap={contracts.GetCapRoom(cpuTeam)}, free-agent QBs={freeAgentQuarterbacks}).");
        Require(league.Transactions.Any(transaction => string.Equals(transaction.Type, "free_agent_signed", StringComparison.OrdinalIgnoreCase)
            && string.Equals(transaction.TeamId, cpuTeam.TeamId, StringComparison.OrdinalIgnoreCase)
            && transaction.Details.Contains("CPU roster repair: QB starter shortage", StringComparison.OrdinalIgnoreCase)), "CPU repair signing should retain an inspectable rationale in the normal transaction record.");
        Require(!repairService.RepairCpuStarterShortages().Rationales.Any(), "Repeated CPU free-agency repair should be deterministic and idempotent after the shortage is resolved.");
        Require(league.Teams.First(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)).Roster
            .Select(player => player.PlayerId).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).SequenceEqual(userRosterIds), "CPU roster repair must never alter the user roster.");

        const string cpuRepairSaveName = "native_smoke_cpu_roster_repair.json";
        var saves = new GameCoreSaveService();
        Require(saves.Save(context, cpuRepairSaveName).Ok, "CPU roster repair save should succeed.");
        var loaded = saves.Load(cpuRepairSaveName);
        Require(loaded.Ok && loaded.League.Transactions.Any(transaction => transaction.Details.Contains("CPU roster repair", StringComparison.OrdinalIgnoreCase)), "CPU repair rationale should survive save/load.");
        Require(saves.Delete(cpuRepairSaveName).Ok, "CPU roster repair smoke save should clean up.");

        league.Calendar.Phase = ScheduleService.TrainingCampPendingPhase;
        foreach (var quarterback in cpuTeam.Roster.Where(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase)).ToList())
            Require(contracts.ReleasePlayer(quarterback.PlayerId, cpuTeam.TeamId).Accepted, "Training-camp repair smoke setup should release CPU quarterbacks through transactions.");
        var campRepair = repairService.RepairCpuStarterShortages();
        Require(campRepair.Signings > 0 && cpuTeam.Roster.Any(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase)), "CPU training-camp repair should fill an immediate starter shortage.");
        Require(league.Teams.All(team => team.Roster.Count <= RosterService.RosterLimit && contracts.GetCapRoom(team) >= 0m), "CPU repair must preserve roster-capacity and cap rules.");
    }

    private static string SnapshotWorldPopulation(LeagueState league)
    {
        var coaches = string.Join("|", league.Teams.SelectMany(team => team.Coaches).Select(coach => $"{coach.Name}:{coach.Overall}"));
        var prospects = string.Join("|", league.CollegeProspects.Take(24).Select(prospect => $"{prospect.Name}:{prospect.Position}:{prospect.Overall}:{prospect.Potential}"));
        return $"{coaches}#{prospects}";
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
        var regularSeasonPlayers = context.ActiveLeague.Teams
            .SelectMany(team => team.Roster)
            .Where(player => player.SeasonStats != null && player.SeasonStats.SeasonYear == context.ActiveLeague.SeasonYear)
            .ToList();
        Require(regularSeasonPlayers.Any(player => player.SeasonStats.GamesPlayed > 0), "Regular-season player totals were not aggregated.");
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
        Require(string.Equals(trainingCampRun.Result.StopReason, ScheduleService.DraftPendingPhaseKey, StringComparison.OrdinalIgnoreCase), $"Expected user-controlled draft stop, got {trainingCampRun.Result.StopReason}.");
        Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.DraftPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");
        Require(trainingCampRun.Result.GamesSimulated == 0, "Draft stop should not simulate games.");
        var simUntilDraftService = new DraftService(context);
        while (string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            var currentPick = simUntilDraftService.GetCurrentPick();
            Require(currentPick != null && string.Equals(currentPick.TeamId, context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase), "Draft should wait for the user team's next selection.");
            var prospect = context.ActiveLeague.CollegeProspects.First(candidate => candidate != null && string.IsNullOrWhiteSpace(candidate.DraftedByTeamId));
            Require(simUntilDraftService.MakePick(context.ActiveLeague.UserTeamId, prospect.ProspectId), simUntilDraftService.LastMessage);
        }

        trainingCampRun = continueService.ContinueUntil("training_camp");
        Require(trainingCampRun.Ok, trainingCampRun.Error);
        Require(string.Equals(trainingCampRun.Result.StopReason, "reached_training_camp", StringComparison.OrdinalIgnoreCase), $"Expected reached_training_camp after draft completion, got {trainingCampRun.Result.StopReason}.");
        Require(string.Equals(context.ActiveLeague.Calendar.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase), $"Expected {ScheduleService.TrainingCampPendingPhase}, got {context.ActiveLeague.Calendar.Phase}.");

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
        var tiedGames = regularSeasonResults.Count(game => game.HomeScore == game.AwayScore);
        Require(totalWins == LeagueBootstrapService.RegularSeasonGameCount - tiedGames, "Every decisive regular-season game should produce exactly one win.");
        Require(totalLosses == totalWins, "Every regular-season win should have a corresponding loss.");
        Require(totalTies == 2 * tiedGames, "Each regular-season tie should appear once for each team.");

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

    private static void ValidateInjuryDepthAdvisory(string teamSeedPath)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var team = league.Teams.First(candidate => candidate.TeamId == league.UserTeamId);
        var depthService = new DepthChartService(context);
        var depth = depthService.AutoFillDepthChart(team.TeamId);
        Require(depth.Ok && depth.DepthChartStatus.IsValid, "Injury-advisory setup requires a valid depth chart.");

        var group = depth.Positions.First(position => position.Players.Count(player => player.IsAvailable) > position.RequiredStarters);
        var reserves = group.Players.Where(player => player.IsAvailable).Skip(group.RequiredStarters).ToList();
        foreach (var reserve in reserves)
        {
            var player = team.Roster.First(candidate => candidate.PlayerId == reserve.PlayerId);
            PlayerInjuryService.InjurePlayer(league, player, "Advisory test injury", 7, $"advisory-{player.PlayerId}");
        }

        var updatedDepth = depthService.GetTeamDepthChart(team.TeamId);
        Require(updatedDepth.DepthChartStatus.IsValid, "A legal but thin injury unit should remain depth-chart valid.");
        var dashboard = new DashboardService(context).GetDashboardState();
        var advisory = dashboard.Dashboard.ActionItems.FirstOrDefault(item => string.Equals(item.Type, "injury_depth_advisory", StringComparison.OrdinalIgnoreCase));
        Require(advisory != null && advisory.Description.Contains(group.Position, StringComparison.OrdinalIgnoreCase) && string.Equals(advisory.PrimaryAction, "Review Depth Chart", StringComparison.Ordinal), "A legal unit with no available reserve should produce a contextual depth advisory.");
        Require(team.DepthChart[group.Position].SequenceEqual(depth.Positions.First(position => position.Position == group.Position).Players.Select(player => player.PlayerId), StringComparer.OrdinalIgnoreCase), "Generating an injury-depth advisory must not change the saved order.");
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
        Require(dashboard != null, "Dashboard is required for offseason validation.");
        Require(dashboard.Calendar != null, "Dashboard calendar is required for offseason validation.");
        Require(dashboard.NextGame != null, "Dashboard next-game block is required for offseason validation.");
        Require(string.Equals(dashboard.Calendar.Phase, expectedPhase, StringComparison.Ordinal), $"Dashboard calendar phase should be {expectedPhase}, got {dashboard.Calendar.Phase}.");
        Require(string.Equals(dashboard.Calendar.WeekLabel, expectedPhase, StringComparison.Ordinal), $"Dashboard calendar label should be {expectedPhase}, got {dashboard.Calendar.WeekLabel}.");
        Require(string.Equals(dashboard.NextGame.HeaderNextLabel, $"Next: {expectedPhase}", StringComparison.Ordinal), $"Unexpected offseason header label: {dashboard.NextGame.HeaderNextLabel}");
        Require(string.Equals(dashboard.NextGame.HeaderOpponentLabel, "Next opponent: TBD", StringComparison.Ordinal), $"Unexpected offseason opponent header: {dashboard.NextGame.HeaderOpponentLabel}");
        Require(string.Equals(dashboard.NextGame.Opponent, "TBD", StringComparison.Ordinal), $"Unexpected offseason opponent value: {dashboard.NextGame.Opponent}");
        Require(string.IsNullOrWhiteSpace(dashboard.NextGame.GameId), $"Offseason phase {expectedPhase} should not expose a next game id.");
        var actionItem = dashboard.ActionItems.FirstOrDefault(item =>
            string.Equals(item.Type, ScheduleService.GetOffseasonPhaseKey(expectedPhase), StringComparison.OrdinalIgnoreCase));
        Require(actionItem != null, $"Dashboard should expose an action item for {expectedPhase}.");
        var expectedTitle = string.Equals(expectedPhase, ScheduleService.DraftPrepPendingPhase, StringComparison.Ordinal)
            ? "Draft Board Review Reminder"
            : expectedPhase;
        Require(string.Equals(actionItem.Title, expectedTitle, StringComparison.Ordinal), $"Unexpected offseason action title for {expectedPhase}: {actionItem?.Title}");

        if (string.Equals(expectedPhase, ScheduleService.RetirementPendingPhase, StringComparison.Ordinal))
        {
            Require(string.Equals(actionItem.Description, "Retirement decisions pending.", StringComparison.Ordinal), $"Unexpected retirement placeholder description: {actionItem?.Description}");
            Require(string.Equals(actionItem.PrimaryAction, "Continue to process retirements", StringComparison.Ordinal), $"Unexpected retirement placeholder action text: {actionItem?.PrimaryAction}");
            return;
        }

        Require(!string.IsNullOrWhiteSpace(actionItem.Description), $"Offseason action description should explain {expectedPhase}.");
        if (string.Equals(expectedPhase, ScheduleService.FreeAgencyPendingPhase, StringComparison.Ordinal))
            Require(string.Equals(actionItem.PrimaryAction, "Open Free Agency", StringComparison.Ordinal), $"Unexpected free-agency action text: {actionItem?.PrimaryAction}");
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

    private static void PrepareUserTrainingCampRoster(GameCoreContext context, TeamState team, ContractService contracts)
    {
        var shortages = DepthChartRules.RequiredStartersByPosition
            .SelectMany(requirement => Enumerable.Repeat(
                requirement.Key,
                Math.Max(0, requirement.Value - team.Roster.Count(player => string.Equals(player.Position, requirement.Key, StringComparison.OrdinalIgnoreCase) && PlayerInjuryService.IsAvailableForGame(player)))))
            .ToList();
        var targetBeforeSignings = Math.Max(0, RosterService.RosterLimit - shortages.Count);
        while (team.Roster.Count > targetBeforeSignings)
        {
            var availableCounts = team.Roster
                .Where(PlayerInjuryService.IsAvailableForGame)
                .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
            var releaseCandidate = team.Roster
                .OrderBy(player => player.Overall)
                .ThenBy(player => player.Age)
                .FirstOrDefault(player => !PlayerInjuryService.IsAvailableForGame(player)
                    || !DepthChartRules.RequiredStartersByPosition.TryGetValue(player.Position ?? "", out var required)
                    || availableCounts.GetValueOrDefault(player.Position ?? "") > required);
            Require(releaseCandidate != null, "Training-camp smoke setup could not create roster space without removing a required starter.");
            var release = contracts.ReleasePlayer(releaseCandidate.PlayerId, team.TeamId);
            Require(release.Ok && release.Accepted, release.Message);
        }

        foreach (var position in shortages)
        {
            var freeAgent = context.ActiveLeague.FreeAgents
                .Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase) && PlayerInjuryService.IsAvailableForGame(player))
                .OrderBy(player => contracts.GetRequiredAnnualSalary(player, team))
                .ThenByDescending(player => player.Overall)
                .FirstOrDefault();
            Require(freeAgent != null, $"Training-camp smoke setup could not find an available {position}.");
            var requirement = contracts.GetRequiredAnnualSalary(freeAgent, team);
            var signing = contracts.SignFreeAgent(freeAgent.PlayerId, team.TeamId, new ContractOffer
            {
                AnnualSalary = requirement * 1.15m,
                GuaranteedSalary = requirement * 0.30m,
                Years = 1,
            }, "Smoke-test starter coverage");
            Require(signing.Ok && signing.Accepted, signing.Message);
        }
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
