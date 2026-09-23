using System.Collections.Generic;
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
}
