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
