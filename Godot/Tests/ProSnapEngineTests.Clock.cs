using System;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using Xunit;

namespace GridironGM.Tests;

public sealed partial class ProSnapEngineTests
{
    [Fact]
    public void TimeoutStopsClockWithoutASnapAndPreservesTheQueuedPlay()
    {
        var league = League(); var game = Game(league); Scrimmage(game); game.ProGame.ClockRunning = true;
        league.ActiveLiveGameSession = new() { Active = true, PendingResult = game, GameId = game.GameId };
        var live = new LiveGameSessionService(new() { ActiveLeague = league });
        Assert.True(live.SubmitDecision(new() { Timeout = "Use timeout", Offense = "Run" }, 0).Ok);
        live.SetPaused(false); var rng = game.ProGame.RandomState;
        var response = live.Advance(0); Assert.True(response.Ok, response.Error);
        Assert.Equal("team_timeout", response.Session.CurrentEvent.Outcome);
        Assert.Equal("home", response.Session.CurrentEvent.TimeoutTeamId);
        Assert.Equal(2, game.ProGame.HomeTimeouts); Assert.Equal(900, game.ProGame.ClockSeconds);
        Assert.Equal(1, game.ProGame.Down); Assert.Equal(35, game.ProGame.YardLine);
        Assert.Empty(game.BoxScore.PlayerStats); Assert.Equal(rng, game.ProGame.RandomState);
        Assert.Single(game.ProGame.Drives); Assert.Equal("Run", game.ProGame.PendingDecision.Offense);
        Assert.False(live.Advance(0).Ok);
        live.SetPaused(true); var before = Json(game);
        Assert.False(live.SubmitDecision(new() { Timeout = "Use timeout" }, 1).Ok);
        Assert.Equal(before, Json(game));
        live.SetPaused(false); Assert.True(live.Advance(1).Ok);
        Assert.Equal("Run", game.BoxScore.PlayByPlay.Last().OffensiveCall);
    }

    [Fact]
    public void TimeoutAndClockPlaysRespectIndependentManagementAndOffenseOwnership()
    {
        var league = League(); var game = Game(league); Scrimmage(game); game.ProGame.ClockRunning = true;
        var domains = league.Teams[0].Coaches[0].Authority.ControlledDomains;
        domains.Add(HeadCoachAuthorityService.OverallGameManagement);
        Assert.False(ProGameDecisionService.Validate(league, game, new() { Timeout = "Use timeout" }, out _));
        Assert.True(ProGameDecisionService.Validate(league, game, new() { Offense = "Kneel" }, out _));
        domains.Clear(); domains.Add(HeadCoachAuthorityService.OffensivePlayCalling);
        Assert.True(ProGameDecisionService.Validate(league, game, new() { Timeout = "Use timeout" }, out _));
        Assert.False(ProGameDecisionService.Validate(league, game, new() { Offense = "Spike" }, out _));
        game.ProGame.HomeTimeouts = 0;
        Assert.False(ProGameDecisionService.Validate(league, game, new() { Timeout = "Use timeout" }, out _));
        game.ProGame.RulesVersion = ProGameState.LegacyRulesVersion;
        Assert.DoesNotContain(ProGameDecisionService.Options(league, game), o => o.Key == "timeout");
        domains.Clear();
        Assert.False(ProGameDecisionService.Validate(league, game, new() { Offense = "Kneel" }, out _));
        Assert.False(ProGameDecisionService.Validate(league, game, new() { Timeout = "Use timeout" }, out _));
    }

    [Theory]
    [InlineData(1, "regular_season", 0)]
    [InlineData(2, "regular_season", 3)]
    [InlineData(4, "regular_season", 2)]
    [InlineData(4, "playoff", 3)]
    [InlineData(5, "playoff", 0)]
    [InlineData(6, "playoff", 3)]
    public void TimeoutBudgetsResetAtHalfAndCompetitionOvertimeBoundaries(int period, string type, int expected)
    {
        var league = League(); var game = Game(league, type: type); Scrimmage(game);
        game.ProGame.HomeTimeouts = game.ProGame.AwayTimeouts = 0;
        game.ProGame.Quarter = period; game.ProGame.ClockSeconds = 0;
        new ProSnapEngine(league, game).Step();
        Assert.Equal(expected, game.ProGame.HomeTimeouts); Assert.Equal(expected, game.ProGame.AwayTimeouts);
    }

    [Fact]
    public void WarningInterruptsRunoffAndKeepsInputWithoutConsumingRandomness()
    {
        var league = League(); var game = Game(league); Scrimmage(game); var state = game.ProGame;
        state.Quarter = 2; state.ClockSeconds = 124; state.ClockRunning = true;
        state.PendingDecision = new() { Offense = "Run", Tempo = "Normal" };
        var rng = state.RandomState; var engine = new ProSnapEngine(league, game);
        var warning = engine.Step();
        Assert.Equal("two_minute_warning", warning.Outcome); Assert.Equal(120, state.ClockSeconds);
        Assert.Equal(4, warning.ElapsedSeconds); Assert.Equal(2, state.LastWarningQuarter);
        Assert.Equal(rng, state.RandomState); Assert.Empty(warning.ParticipantIds); Assert.Equal(3, state.HomeTimeouts);
        Assert.Equal("Run", state.PendingDecision.Offense); Assert.False(state.ClockRunning);
        Assert.Equal("run", engine.Step().PlayType);
        Assert.Single(game.BoxScore.PlayByPlay, p => p.Outcome == "two_minute_warning");
    }

    [Fact]
    public void PlayThatCrossesWarningFinishesBeforeTheWarning()
    {
        var league = League(); var game = Game(league); Scrimmage(game);
        game.ProGame.Quarter = 2; game.ProGame.ClockSeconds = 124;
        game.ProGame.PendingDecision.Offense = "Run";
        var engine = new ProSnapEngine(league, game); var snap = engine.Step();
        Assert.Equal("run", snap.PlayType); Assert.InRange(snap.ClockSeconds, 115, 119);
        var warning = engine.Step(); Assert.Equal("two_minute_warning", warning.Outcome);
        Assert.Equal(snap.ClockSeconds, warning.ClockSeconds); Assert.Equal(0, warning.ElapsedSeconds);
    }

    [Theory]
    [InlineData(2, "home", 3)]
    [InlineData(4, "away", 1)]
    public void SpikeCostsADownAndAnAttemptWithoutYards(int down, string possessionAfter, int downAfter)
    {
        var league = League(); var game = Game(league); Scrimmage(game, 60, down, 7);
        game.ProGame.Quarter = 2; game.ProGame.LastWarningQuarter = 2; game.ProGame.ClockSeconds = 24; game.ProGame.ClockRunning = true;
        game.ProGame.PendingDecision = new() { Offense = "Spike", FourthDown = "Go", Tempo = "Hurry" };
        var play = new ProSnapEngine(league, game).Step();
        Assert.Equal("spike", play.Outcome); Assert.Equal(4, play.ElapsedSeconds); Assert.Equal(20, game.ProGame.ClockSeconds);
        Assert.Equal(1, play.StatChanges.Sum(s => s.PassAttempts)); Assert.Equal(0, play.StatChanges.Sum(s => s.Completions));
        Assert.Equal(0, play.YardsGained); Assert.False(game.ProGame.ClockRunning);
        Assert.Equal(possessionAfter, game.ProGame.PossessionTeamId); Assert.Equal(downAfter, game.ProGame.Down);
        Assert.Equal(down == 4, play.IsTurnover);
    }

    [Fact]
    public void WinningTeamCanKneelOutClockWhenDefenseCannotStopIt()
    {
        var league = League(); var game = Game(league); Scrimmage(game); var state = game.ProGame;
        state.Quarter = state.LastWarningQuarter = 4; state.ClockSeconds = 70; game.HomeScore = 7;
        state.HomeTimeouts = state.AwayTimeouts = 0;
        var engine = new ProSnapEngine(league, game); engine.Finish();
        Assert.True(state.Completed); Assert.Equal(7, game.HomeScore); Assert.Equal(0, game.AwayScore);
        Assert.Equal(2, game.BoxScore.PlayByPlay.Count(p => p.Outcome == "kneel"));
        Assert.Equal(-2, game.BoxScore.PlayerStats.Sum(s => s.RushingYards));
        Assert.Contains(game.BoxScore.PlayByPlay, p => p.Outcome == "clock_expired");
    }

    [Fact]
    public void DefensiveTimeoutInterruptsAPreparedPlayAndIsNotControlledByTheOpposingGm()
    {
        var league = League(); var game = Game(league); Scrimmage(game); var state = game.ProGame;
        state.Quarter = state.LastWarningQuarter = 4; state.ClockSeconds = 60; state.ClockRunning = true; game.HomeScore = 7;
        state.PendingDecision = new() { Offense = "Run", Timeout = "No timeout" };
        var play = new ProSnapEngine(league, game).Step();
        Assert.Equal("away", play.TimeoutTeamId); Assert.Equal(2, state.AwayTimeouts);
        Assert.Equal(3, state.HomeTimeouts); Assert.Equal(60, state.ClockSeconds); Assert.Equal("Run", state.PendingDecision.Offense);
    }

    [Theory]
    [InlineData("Kneel")]
    [InlineData("Spike")]
    public void ClockPlaysNeverUseAnUnavailableQuarterback(string call)
    {
        var league = League(); var game = Game(league); Scrimmage(game);
        foreach (var qb in league.Teams[0].Roster.Where(p => p.Position == "QB")) qb.Status = "Inactive";
        Assert.False(ProGameDecisionService.Validate(league, game, new() { Offense = call }, out _));
        game.ProGame.PendingDecision.Offense = call; var before = Json(game);
        Assert.Throws<InvalidOperationException>(() => new ProSnapEngine(league, game).Step());
        Assert.Equal(before, Json(game));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void FinalSecondsCanProduceAFieldGoalBeforeFourthDown(int down)
    {
        var league = League(); var game = Game(league); Scrimmage(game, 85, down, 10);
        game.ProGame.Quarter = game.ProGame.LastWarningQuarter = 2; game.ProGame.ClockSeconds = 5;
        var play = new ProSnapEngine(league, game).Step();
        Assert.Equal("field_goal", play.PlayType); Assert.Equal(1, play.StatChanges.Sum(s => s.FieldGoalAttempts));
        Assert.Contains(play.Points, new[] { 0, 3 }); Assert.Equal(down, play.StartDown);
    }

    [Fact]
    public void TimeoutAndQueuedPlaySurviveDiskReloadWithoutDuplicateCharges()
    {
        var context = new GameCoreContext(); var league = new LeagueBootstrapService(context).CreateTestLeague();
        var game = league.Schedule.First(g => g.HomeTeamId == league.UserTeamId || g.AwayTeamId == league.UserTeamId);
        league.Calendar.AbsoluteWeek = game.AbsoluteWeek; league.Calendar.DayIndex = game.DayIndex;
        var live = new LiveGameSessionService(context); Assert.True(live.Start(game.GameId).Ok); live.SetPaused(false);
        for (var i = 0; i < 30 && !ProClockManagementService.CanCallTimeout(league.ActiveLiveGameSession.PendingResult, league.UserTeamId); i++)
            Assert.True(live.Advance(league.ActiveLiveGameSession.NextEventIndex).Ok);
        live.SetPaused(true); Assert.True(live.SubmitDecision(new() { Timeout = "Use timeout" }, league.ActiveLiveGameSession.NextEventIndex).Ok);
        var saves = new GameCoreSaveService(); var name = $"clock-test-{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(saves.Save(context, name).Ok); var loaded = saves.Load(name); Assert.True(loaded.Ok, loaded.Message);
            var restored = new LiveGameSessionService(new() { ActiveLeague = loaded.League });
            var sequence = league.ActiveLiveGameSession.NextEventIndex; live.SetPaused(false); restored.SetPaused(false);
            Assert.True(live.Advance(sequence).Ok); Assert.True(restored.Advance(sequence).Ok);
            Assert.Equal(Json(league.ActiveLiveGameSession.PendingResult), Json(loaded.League.ActiveLiveGameSession.PendingResult));
            Assert.Equal(2, ProClockManagementService.Timeouts(loaded.League.ActiveLiveGameSession.PendingResult, league.UserTeamId));
            Assert.False(restored.Advance(sequence).Ok);
            Assert.True(saves.Save(new() { ActiveLeague = loaded.League }, name).Ok);
            var again = saves.Load(name); Assert.True(again.Ok, again.Message);
            Assert.Single(again.League.ActiveLiveGameSession.PlayedEvents, p => p.TimeoutTeamId == league.UserTeamId);
        }
        finally { saves.Delete(name); }
    }

    [Fact]
    public void ExplicitGoOverridesEarlyKickRecommendation()
    {
        var league = League(); var game = Game(league); Scrimmage(game, 85);
        game.ProGame.Quarter = game.ProGame.LastWarningQuarter = 2; game.ProGame.ClockSeconds = 5;
        game.ProGame.PendingDecision = new() { FourthDown = "Go", Offense = "Run" };
        Assert.Equal("run", new ProSnapEngine(league, game).Step().PlayType);
    }

    [Fact]
    public void ASpikeDoesNotOverrideTheManagementChoiceToChewClock()
    {
        var league = League(); var game = Game(league); Scrimmage(game);
        game.ProGame.ClockSeconds = 60; game.ProGame.ClockRunning = true;
        game.ProGame.PendingDecision = new() { Offense = "Spike", Tempo = "Chew" };
        var play = new ProSnapEngine(league, game).Step();
        Assert.Equal("spike", play.Outcome); Assert.Equal(39, play.ElapsedSeconds);
        Assert.Equal(21, game.ProGame.ClockSeconds);
    }

    [Fact]
    public void PlayoffOvertimePeriodBoundaryDoesNotTriggerUrgentClockStrategy()
    {
        var league = League(); var game = Game(league, type: "playoff"); Scrimmage(game, 75);
        game.ProGame.Quarter = 5; game.ProGame.ClockSeconds = 10; game.ProGame.ClockRunning = true;
        Assert.Equal("Normal", ProClockManagementService.TempoRecommendation(game));
        Assert.Equal("", ProClockManagementService.TimeoutRecommendation(league, game));
        Assert.Equal("Go", ProClockManagementService.DownRecommendation(game));
        game.ProGame.HomeTimeouts = 0;
        Assert.Equal("", ProClockManagementService.OffenseRecommendation(game));
    }
}
