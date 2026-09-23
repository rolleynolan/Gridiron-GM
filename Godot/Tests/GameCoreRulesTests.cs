using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using Xunit;

namespace GridironGM.Tests;

public sealed class GameCoreRulesTests
{
    [Theory]
    [InlineData(ScheduleService.ExclusiveNegotiationPendingPhase, false, true, false, true)]
    [InlineData(ScheduleService.FranchiseTagPendingPhase, false, false, true, false)]
    [InlineData(ScheduleService.FreeAgencyPendingPhase, true, true, false, true)]
    [InlineData(ScheduleService.TrainingCampPendingPhase, true, false, false, true)]
    [InlineData("Regular Season", true, false, false, true)]
    [InlineData(ScheduleService.SeasonCompletePhase, false, false, false, false)]
    public void ContractPhaseStatusMatchesLifecycleRules(
        string phase,
        bool canSign,
        bool canExtend,
        bool canTag,
        bool canManageRoster)
    {
        var league = LeagueInPhase(phase);

        var status = ContractPhaseRules.GetStatus(league);

        Assert.Equal(canSign, status.CanSignFreeAgents);
        Assert.Equal(canExtend, status.CanOfferExtensions);
        Assert.Equal(canTag, status.CanApplyFranchiseTag);
        Assert.Equal(canManageRoster, status.CanManageRoster);
        Assert.False(string.IsNullOrWhiteSpace(status.Explanation));
    }

    [Fact]
    public void RookieSigningRejectsVeterans()
    {
        var league = LeagueInPhase(ScheduleService.RookieSigningPendingPhase);
        var veteran = new PlayerState { PlayerId = "veteran", Age = 28 };

        var accepted = ContractPhaseRules.CanSignFreeAgent(league, veteran, out var error);

        Assert.False(accepted);
        Assert.Contains("only undrafted rookies", error);
    }

    [Fact]
    public void RegularSeasonDiagnosticsIgnorePreseasonAndPlayoffResults()
    {
        var league = new LeagueState
        {
            Results = new List<GameResult>
            {
                Result("regular", "Regular Season", 24, 20),
                Result("preseason", "Preseason", 70, 0),
                Result("playoff", "Playoffs", 3, 40),
            },
        };

        var diagnostics = SimulationDiagnosticsService.AnalyzeRegularSeason(league);

        Assert.Equal(1, diagnostics.CompletedRegularSeasonGames);
        Assert.Equal(22, diagnostics.PointsPerTeamGame);
        Assert.Equal(1, diagnostics.HomeWinRate);
        Assert.Equal(4, diagnostics.LargestScoreMargin);
    }

    [Fact]
    public void SimulatedTeamYardageStaysWithinFootballScale()
    {
        var context = Bootstrap();
        var game = context.ActiveLeague.Schedule.First();

        var response = new GameDayService(context).SimulateScheduledGame(game.GameId, allowUserTeamGame: true);

        Assert.True(response.Ok, response.Error);
        var result = context.ActiveLeague.Results.Single(candidate => candidate.GameId == game.GameId);
        Assert.InRange(result.BoxScore.TeamStats["total_yards_home"], 180, 625);
        Assert.InRange(result.BoxScore.TeamStats["total_yards_away"], 180, 625);
    }

    [Fact]
    public void SimulatedPlayerTouchdownsReconcileWithTeamBoxScore()
    {
        var context = Bootstrap();
        var game = context.ActiveLeague.Schedule.First();
        Assert.True(new GameDayService(context).SimulateScheduledGame(game.GameId, allowUserTeamGame: true).Ok);
        var result = context.ActiveLeague.Results.Single(candidate => candidate.GameId == game.GameId);

        AssertTouchdownsReconcile(result, result.HomeTeamId, "touchdowns_home");
        AssertTouchdownsReconcile(result, result.AwayTeamId, "touchdowns_away");

        static void AssertTouchdownsReconcile(GameResult result, string teamId, string teamStatKey)
        {
            var playerStats = result.BoxScore.PlayerStats.Where(stat => stat.TeamId == teamId).ToList();
            var passing = playerStats.Sum(stat => stat.PassingTouchdowns);
            var receiving = playerStats.Sum(stat => stat.ReceivingTouchdowns);
            var rushing = playerStats.Sum(stat => stat.RushingTouchdowns);
            Assert.Equal(passing, receiving);
            Assert.Equal(result.BoxScore.TeamStats[teamStatKey], passing + rushing);
        }
    }

    [Fact]
    public void AwayWinnerSummaryListsWinningScoreFirst()
    {
        var league = new LeagueState
        {
            UserTeamId = "home",
            Teams = new List<TeamState>
            {
                new() { TeamId = "home", Name = "Home Club", Abbreviation = "HOM", Roster = new List<PlayerState> { new() { PlayerId = "home-qb", Position = "QB", Overall = 50 } } },
                new() { TeamId = "away", Name = "Away Club", Abbreviation = "AWY", Roster = new List<PlayerState> { new() { PlayerId = "away-qb", Position = "QB", Overall = 90 } } },
            },
            Schedule = new List<ScheduledGame>
            {
                new() { GameId = "away-win", AbsoluteWeek = 1, PhaseWeek = 1, DayIndex = 2, Phase = "Regular Season", GameType = "regular_season", HomeTeamId = "home", AwayTeamId = "away" },
            },
        };
        var context = new GameCoreContext { ActiveLeague = league };

        var response = new GameDayService(context).SimulateScheduledGame("away-win", allowUserTeamGame: true);

        Assert.True(response.Ok, response.Error);
        var result = Assert.Single(league.Results);
        Assert.True(result.AwayScore > result.HomeScore);
        Assert.Equal($"Away Club defeated Home Club, {result.AwayScore}-{result.HomeScore}.", result.Summary);
    }

    [Fact]
    public void StrongerLineupCannotLosePointsToRatingModulo()
    {
        static int SimulateHomeScore(int homeOverall)
        {
            var league = new LeagueState
            {
                UserTeamId = "home",
                Teams = new List<TeamState>
                {
                    new() { TeamId = "home", Name = "Home Club", Abbreviation = "HOM", Roster = new List<PlayerState> { new() { PlayerId = "home-qb", Position = "QB", Overall = homeOverall } } },
                    new() { TeamId = "away", Name = "Away Club", Abbreviation = "AWY", Roster = new List<PlayerState> { new() { PlayerId = "away-qb", Position = "QB", Overall = 65 } } },
                },
                Schedule = new List<ScheduledGame>
                {
                    new() { GameId = "strength-test", AbsoluteWeek = 8, PhaseWeek = 8, DayIndex = 2, Phase = "Regular Season", GameType = "regular_season", HomeTeamId = "home", AwayTeamId = "away" },
                },
            };
            var response = new GameDayService(new GameCoreContext { ActiveLeague = league }).SimulateScheduledGame("strength-test", allowUserTeamGame: true);
            Assert.True(response.Ok, response.Error);
            return league.Results.Single().HomeScore;
        }

        var averageScore = SimulateHomeScore(65);
        var strongerScore = SimulateHomeScore(75);

        Assert.True(strongerScore > averageScore, $"Expected stronger lineup to outscore the baseline, got {strongerScore} and {averageScore}.");
    }

    [Fact]
    public void FullScheduleScoringRemainsWithinBroadFootballScale()
    {
        var context = Bootstrap();
        var gameDay = new GameDayService(context);
        var regularSeasonGames = context.ActiveLeague.Schedule
            .Where(game => string.Equals(game.GameType, "regular_season", StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var game in regularSeasonGames)
            Assert.True(gameDay.SimulateScheduledGame(game.GameId, allowUserTeamGame: true).Ok);

        var diagnostics = SimulationDiagnosticsService.AnalyzeRegularSeason(context.ActiveLeague);
        Assert.Equal(LeagueBootstrapService.RegularSeasonGameCount, diagnostics.CompletedRegularSeasonGames);
        Assert.InRange(diagnostics.PointsPerTeamGame, 12, 40);
        Assert.InRange(diagnostics.HomeWinRate, 0.30, 0.80);
        Assert.InRange(diagnostics.LargestScoreMargin, 1, 40);
    }

    [Fact]
    public void FailedBatchReleaseIsAtomic()
    {
        var context = Bootstrap();
        var league = context.ActiveLeague;
        var team = league.Teams[0];
        var player = team.Roster[0];
        var rosterCount = team.Roster.Count;
        var freeAgentCount = league.FreeAgents.Count;
        var transactionCount = league.Transactions.Count;

        var result = new TransactionService(context).ReleasePlayers(
            new[] { player.PlayerId, "missing-player" },
            team.TeamId,
            new ContractService(context));

        Assert.False(result.Ok);
        Assert.Equal(rosterCount, team.Roster.Count);
        Assert.Equal(freeAgentCount, league.FreeAgents.Count);
        Assert.Equal(transactionCount, league.Transactions.Count);
        Assert.Contains(team.Roster, candidate => candidate.PlayerId == player.PlayerId);
    }

    [Fact]
    public void DraftSelectionCannotCreateDuplicateOwnership()
    {
        var context = Bootstrap();
        var league = context.ActiveLeague;
        league.Calendar.Phase = ScheduleService.DraftPendingPhase;
        new DraftService(context).PrepareDraftBoard();
        var pick = league.Draft.Picks.First(candidate => string.IsNullOrWhiteSpace(candidate.ProspectId));
        var prospect = league.CollegeProspects.First(candidate => string.IsNullOrWhiteSpace(candidate.DraftedByTeamId));
        var team = league.Teams.First(candidate => candidate.TeamId == pick.TeamId);
        var transactions = new TransactionService(context);

        Assert.True(transactions.DraftRookie(pick, prospect, team, out var firstError), firstError);
        Assert.False(transactions.DraftRookie(pick, prospect, team, out _));

        var playerId = $"rookie-{league.SeasonYear}-{prospect.ProspectId}";
        Assert.Equal(1, league.Teams.SelectMany(candidate => candidate.Roster).Count(player => player.PlayerId == playerId));
        Assert.Equal(team.TeamId, prospect.DraftedByTeamId);
    }

    [Fact]
    public void RepeatedWaiverRequestCannotDuplicatePlayerOwnership()
    {
        var context = Bootstrap();
        var league = context.ActiveLeague;
        var team = league.Teams[0];
        var player = team.Roster[0];
        var transactions = new TransactionService(context);
        var contracts = new ContractService(context);

        Assert.True(transactions.PlaceOnWaivers(player.PlayerId, team.TeamId, contracts).Ok);
        Assert.False(transactions.PlaceOnWaivers(player.PlayerId, team.TeamId, contracts).Ok);

        Assert.DoesNotContain(league.Teams.SelectMany(candidate => candidate.Roster), candidate => candidate.PlayerId == player.PlayerId);
        Assert.DoesNotContain(league.FreeAgents, candidate => candidate.PlayerId == player.PlayerId);
        Assert.Single(league.Waivers, waiver => waiver.Player.PlayerId == player.PlayerId);
    }

    [Fact]
    public void SaveLoadMigratesLegacyCollectionsAndVersion()
    {
        var saveName = $"migration_test_{Guid.NewGuid():N}.json";
        var saveService = new GameCoreSaveService();
        var legacy = new LeagueState
        {
            SaveVersion = 1,
            Teams = new List<TeamState> { new() { TeamId = "legacy", Name = "Legacy Team", Abbreviation = "LEG" } },
            FreeAgents = null!,
            Schedule = null!,
            Results = null!,
            Transactions = null!,
        };

        try
        {
            Assert.True(saveService.Save(new GameCoreContext { ActiveLeague = legacy }, saveName).Ok);
            var loaded = saveService.Load(saveName);

            Assert.True(loaded.Ok, loaded.Message);
            Assert.Equal(LeagueState.CurrentSaveVersion, loaded.League.SaveVersion);
            Assert.NotNull(loaded.League.Teams);
            Assert.NotNull(loaded.League.FreeAgents);
            Assert.NotNull(loaded.League.Schedule);
            Assert.NotNull(loaded.League.Results);
            Assert.NotNull(loaded.League.Transactions);
        }
        finally
        {
            saveService.Delete(saveName);
        }
    }

    [Fact]
    public void LiveGameCompletionIsIdempotent()
    {
        var context = Bootstrap();
        var league = context.ActiveLeague;
        var game = league.Schedule.First(candidate =>
            candidate.HomeTeamId == league.UserTeamId || candidate.AwayTeamId == league.UserTeamId);
        var sessions = new LiveGameSessionService(context);

        var firstStart = sessions.Start(game.GameId);
        var secondStart = sessions.Start(game.GameId);
        Assert.True(firstStart.Ok, firstStart.Error);
        Assert.True(secondStart.Ok, secondStart.Error);
        Assert.Equal(firstStart.Session.GameId, secondStart.Session.GameId);

        Assert.True(sessions.SetPaused(false).Ok);
        LiveGameSessionResponse response;
        do
        {
            response = sessions.Advance();
            Assert.True(response.Ok, response.Error);
        } while (response.Session.Active);

        Assert.Single(league.Results, result => result.GameId == game.GameId);
        Assert.False(sessions.Advance().Ok);
        Assert.Single(league.Results, result => result.GameId == game.GameId);
    }

    [Fact]
    public void DivisionHeadToHeadOutranksPointDifferential()
    {
        var league = TiebreakLeague();
        league.Results.Add(Game("head-to-head", "alpha", "beta", 17, 14));
        var alpha = Standing("alpha", pointDifferential: -40);
        var beta = Standing("beta", pointDifferential: 100);

        var ranked = new PlayoffTiebreakerService(league, new[] { alpha, beta }).RankDivision(new[] { alpha, beta });

        Assert.Equal("alpha", ranked[0].TeamId);
    }

    [Fact]
    public void DivisionRecordBreaksSplitHeadToHeadTie()
    {
        var league = TiebreakLeague();
        league.Results.Add(Game("alpha-beta-1", "alpha", "beta", 21, 10));
        league.Results.Add(Game("alpha-beta-2", "beta", "alpha", 24, 17));
        league.Results.Add(Game("alpha-gamma", "alpha", "gamma", 20, 13));
        league.Results.Add(Game("beta-gamma", "beta", "gamma", 10, 20));
        var standings = new[] { Standing("alpha"), Standing("beta"), Standing("gamma", wins: 5, losses: 12) };

        var ranked = new PlayoffTiebreakerService(league, standings).RankDivision(standings.Take(2));

        Assert.Equal("alpha", ranked[0].TeamId);
    }

    [Fact]
    public void MultiTeamWildcardHeadToHeadSweepSelectsSweepingClub()
    {
        var league = TiebreakLeague();
        league.Teams[1].Division = "South";
        league.Teams[2].Division = "East";
        league.Results.Add(Game("alpha-beta", "alpha", "beta", 24, 17));
        league.Results.Add(Game("alpha-gamma", "gamma", "alpha", 14, 20));
        var alpha = Standing("alpha");
        var beta = Standing("beta"); beta.Division = "South";
        var gamma = Standing("gamma"); gamma.Division = "East";

        var ranked = new PlayoffTiebreakerService(league, new[] { alpha, beta, gamma }).RankWildCards(new[] { alpha, beta, gamma });

        Assert.Equal("alpha", ranked[0].TeamId);
    }

    [Fact]
    public void MultiTeamWildcardHeadToHeadSweepEliminatesSweptClub()
    {
        var league = TiebreakLeague();
        league.Teams[1].Division = "South";
        league.Teams[2].Division = "East";
        league.Results.Add(Game("alpha-beta", "beta", "alpha", 24, 17));
        league.Results.Add(Game("alpha-gamma", "alpha", "gamma", 14, 20));
        var alpha = Standing("alpha");
        var beta = Standing("beta"); beta.Division = "South";
        var gamma = Standing("gamma"); gamma.Division = "East";

        var ranked = new PlayoffTiebreakerService(league, new[] { alpha, beta, gamma }).RankWildCards(new[] { alpha, beta, gamma });

        Assert.NotEqual("alpha", ranked[0].TeamId);
    }

    [Fact]
    public void LegacyResultsWithoutGameIdsRemainDistinctForTiebreaks()
    {
        var league = TiebreakLeague();
        league.Results.Add(Game("", "alpha", "beta", 21, 10));
        league.Results.Add(Game("", "beta", "alpha", 24, 17));
        league.Results.Add(Game("", "alpha", "gamma", 20, 13));
        league.Results.Add(Game("", "beta", "gamma", 10, 20));
        var standings = new[] { Standing("alpha"), Standing("beta"), Standing("gamma", wins: 5, losses: 12) };

        var ranked = new PlayoffTiebreakerService(league, standings).RankDivision(standings.Take(2));

        Assert.Equal("alpha", ranked[0].TeamId);
    }

    [Fact]
    public void BenchmarkHarnessMeasuresDetailedGameWorkload()
    {
        var report = SimulationBenchmarkService.Run(FindTeamSeedPath());

        Assert.Equal(1, report.SingleGame.Games);
        Assert.Equal(SimulationBenchmarkService.ProWeekGames, report.ProWeek.Games);
        Assert.Equal(SimulationBenchmarkService.ProSeasonGames, report.ProSeason.Games);
        Assert.Equal(SimulationBenchmarkService.ProjectedCollegeSeasonGames, report.ProjectedCollegeSeason.Games);
        Assert.True(report.ProSeason.AllocatedBytes > 0);
        Assert.True(report.ProjectedCollegeSeason.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void HeadCoachAuthorityAcceptsOnlyCanonicalHeadCoachDomains()
    {
        var candidate = new CoachState { CoachId = "candidate", Role = "Available Staff" };

        var accepted = HeadCoachAuthorityService.TryBuildAgreement(
            candidate,
            "Head Coach",
            new[]
            {
                HeadCoachAuthorityService.DefensivePlayCalling,
                HeadCoachAuthorityService.OffensivePlayCalling,
                HeadCoachAuthorityService.DefensivePlayCalling,
            },
            out var agreement,
            out var error);

        Assert.True(accepted, error);
        Assert.Equal(
            new[] { HeadCoachAuthorityService.OffensivePlayCalling, HeadCoachAuthorityService.DefensivePlayCalling },
            agreement.ControlledDomains);
        Assert.DoesNotContain("personnel_packages", HeadCoachAuthorityService.Domains);
        Assert.DoesNotContain("roster_transactions", HeadCoachAuthorityService.Domains);
    }

    [Fact]
    public void LowerLevelCoachCannotReceiveNegotiatedAuthority()
    {
        var candidate = new CoachState { CoachId = "candidate", Role = "Available Staff" };

        var accepted = HeadCoachAuthorityService.TryBuildAgreement(
            candidate,
            "Offensive Coordinator",
            new[] { HeadCoachAuthorityService.OffensivePlayCalling },
            out var agreement,
            out var error);

        Assert.False(accepted);
        Assert.Empty(agreement.ControlledDomains);
        Assert.Contains("Only a Head Coach", error);
    }

    [Fact]
    public void AuthorityOwnershipFollowsActiveHeadCoachAgreement()
    {
        var team = new TeamState
        {
            TeamId = "authority-team",
            Coaches = new List<CoachState>
            {
                new()
                {
                    CoachId = "head-coach",
                    Role = "Head Coach",
                    Authority = new HeadCoachAuthorityState
                    {
                        ControlledDomains = new List<string> { HeadCoachAuthorityService.OverallGameManagement },
                    },
                },
                new()
                {
                    CoachId = "coordinator",
                    Role = "Offensive Coordinator",
                    Authority = new HeadCoachAuthorityState
                    {
                        ControlledDomains = new List<string> { HeadCoachAuthorityService.OffensivePlayCalling },
                    },
                },
            },
        };

        HeadCoachAuthorityService.Normalize(team.Coaches[1]);

        Assert.True(HeadCoachAuthorityService.IsHeadCoachControlled(team, HeadCoachAuthorityService.OverallGameManagement));
        Assert.False(HeadCoachAuthorityService.IsHeadCoachControlled(team, HeadCoachAuthorityService.OffensivePlayCalling));
        Assert.Empty(team.Coaches[1].Authority.ControlledDomains);
    }

    [Fact]
    public void HeadCoachAuthoritySurvivesSaveLoadAndLowerStaffAuthorityIsCleared()
    {
        var context = Bootstrap();
        var team = context.ActiveLeague.Teams[0];
        var headCoach = team.Coaches.First(coach => coach.Role == "Head Coach");
        var coordinator = team.Coaches.First(coach => coach.Role == "Offensive Coordinator");
        headCoach.Authority.ControlledDomains.Add(HeadCoachAuthorityService.OffensivePlaybook);
        coordinator.Authority.ControlledDomains.Add(HeadCoachAuthorityService.OffensivePlayCalling);
        var saveName = $"authority_test_{Guid.NewGuid():N}.json";
        var saves = new GameCoreSaveService();

        try
        {
            Assert.True(saves.Save(context, saveName).Ok);
            var loaded = saves.Load(saveName);
            Assert.True(loaded.Ok, loaded.Message);
            var loadedTeam = loaded.League.Teams.First(candidate => candidate.TeamId == team.TeamId);
            var loadedHeadCoach = loadedTeam.Coaches.First(coach => coach.Role == "Head Coach");
            var loadedCoordinator = loadedTeam.Coaches.First(coach => coach.Role == "Offensive Coordinator");
            Assert.Contains(HeadCoachAuthorityService.OffensivePlaybook, loadedHeadCoach.Authority.ControlledDomains);
            Assert.Empty(loadedCoordinator.Authority.ControlledDomains);
            Assert.Equal(LeagueState.CurrentSaveVersion, loaded.League.SaveVersion);
        }
        finally
        {
            saves.Delete(saveName);
        }
    }

    [Fact]
    public void ReleasingHeadCoachClearsExpiredAuthorityAgreement()
    {
        var context = Bootstrap();
        context.ActiveLeague.Calendar.Phase = ScheduleService.StaffCarouselPendingPhase;
        var team = context.ActiveLeague.Teams.First(candidate => candidate.TeamId == context.ActiveLeague.UserTeamId);
        var headCoach = team.Coaches.First(coach => coach.Role == "Head Coach");
        headCoach.Authority.ControlledDomains.Add(HeadCoachAuthorityService.CoordinatorStaffing);

        var result = new StaffService(context).ReleaseCoach(team.TeamId, headCoach.CoachId);

        Assert.True(result.Ok, result.Message);
        Assert.Equal("Available Staff", headCoach.Role);
        Assert.Empty(headCoach.Authority.ControlledDomains);
        Assert.Contains(headCoach, context.ActiveLeague.AvailableCoaches);
    }

    [Fact]
    public void HiringLowerLevelCoachCannotCarryAuthorityIntoRole()
    {
        var context = Bootstrap();
        context.ActiveLeague.Calendar.Phase = ScheduleService.StaffCarouselPendingPhase;
        var team = context.ActiveLeague.Teams.First(candidate => candidate.TeamId == context.ActiveLeague.UserTeamId);
        var current = team.Coaches.First(coach => coach.Role == "Offensive Coordinator");
        var staff = new StaffService(context);
        Assert.True(staff.ReleaseCoach(team.TeamId, current.CoachId).Ok);
        var candidate = context.ActiveLeague.AvailableCoaches.First(coach => coach.CoachId != current.CoachId);
        candidate.Authority.ControlledDomains.Add(HeadCoachAuthorityService.OffensivePlayCalling);

        var result = staff.HireCoach(team.TeamId, "Offensive Coordinator", candidate.CoachId);

        Assert.True(result.Ok, result.Message);
        Assert.Equal("Offensive Coordinator", candidate.Role);
        Assert.Empty(candidate.Authority.ControlledDomains);
    }

    [Fact]
    public void StaffHiringRejectsUnsupportedRoleWithoutMutatingCandidate()
    {
        var context = Bootstrap();
        context.ActiveLeague.Calendar.Phase = ScheduleService.StaffCarouselPendingPhase;
        var team = context.ActiveLeague.Teams.First(candidate => candidate.TeamId == context.ActiveLeague.UserTeamId);
        var candidate = context.ActiveLeague.AvailableCoaches.First();
        var originalMarketCount = context.ActiveLeague.AvailableCoaches.Count;

        var result = new StaffService(context).HireCoach(team.TeamId, "Personnel Packages Coach", candidate.CoachId);

        Assert.False(result.Ok);
        Assert.Contains("not supported", result.Message);
        Assert.Equal("Available Staff", candidate.Role);
        Assert.Equal(originalMarketCount, context.ActiveLeague.AvailableCoaches.Count);
        Assert.DoesNotContain(team.Coaches, coach => coach.CoachId == candidate.CoachId);
    }

    private static LeagueState LeagueInPhase(string phase)
        => new()
        {
            Calendar = new CalendarState { Phase = phase },
        };

    private static GameResult Result(string id, string phase, int homeScore, int awayScore)
        => new()
        {
            GameId = id,
            Phase = phase,
            GameType = phase == "Regular Season"
                ? "regular_season"
                : phase == "Preseason"
                    ? "preseason"
                    : "playoffs",
            HomeScore = homeScore,
            AwayScore = awayScore,
        };

    private static GameCoreContext Bootstrap()
    {
        var context = new GameCoreContext();
        new LeagueBootstrapService(context).CreateTestLeague(FindTeamSeedPath());
        return context;
    }

    private static string FindTeamSeedPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json");
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Unable to locate the test team seed from the test output directory.");
    }

    private static LeagueState TiebreakLeague()
        => new()
        {
            SeasonYear = 2026,
            Teams = new List<TeamState>
            {
                new() { TeamId = "alpha", Name = "Alpha", Division = "North", Conference = "Atlas" },
                new() { TeamId = "beta", Name = "Beta", Division = "North", Conference = "Atlas" },
                new() { TeamId = "gamma", Name = "Gamma", Division = "North", Conference = "Atlas" },
            },
        };

    private static TeamStanding Standing(string id, int wins = 10, int losses = 7, int pointDifferential = 0)
        => new()
        {
            TeamId = id,
            TeamName = id,
            Wins = wins,
            Losses = losses,
            WinPct = wins / (double)(wins + losses),
            PointDifferential = pointDifferential,
            Division = "North",
            Conference = "Atlas",
        };

    private static GameResult Game(string id, string home, string away, int homeScore, int awayScore)
        => new()
        {
            GameId = id,
            Phase = "Regular Season",
            GameType = "regular_season",
            HomeTeamId = home,
            AwayTeamId = away,
            HomeScore = homeScore,
            AwayScore = awayScore,
        };
}
