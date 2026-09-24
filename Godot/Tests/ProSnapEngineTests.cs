using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using GridironGM.GameCore.Utilities;
using Xunit;

namespace GridironGM.Tests;

public sealed partial class ProSnapEngineTests
{
    private static T Clone<T>(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private static LeagueState League()
    {
        var league = new LeagueState { UserTeamId = "home" };
        foreach (var id in new[] { "home", "away" })
        {
            var team = new TeamState { TeamId = id, Name = id, Abbreviation = id.ToUpperInvariant() };
            team.Coaches.Add(new CoachState { Role = "Head Coach", Overall = 75 });
            foreach (var role in new[] { "QB", "RB", "WR", "TE", "LT", "LG", "C", "RG", "RT", "DE", "DT", "OLB", "MLB", "CB", "S", "K", "P" })
            {
                team.DepthChart[role] = new();
                for (var rank = 0; rank < 3; rank++)
                {
                    var player = new PlayerState { PlayerId = $"{id}-{role}-{rank}", Name = $"{id} {role} {rank}", Position = role, Overall = 75 - rank * 5 };
                    team.Roster.Add(player); team.DepthChart[role].Add(player.PlayerId);
                }
            }
            league.Teams.Add(team);
        }
        return league;
    }
    private static GameResult Game(LeagueState league, string id = "snap-test", string type = "regular_season")
        => ProSnapEngine.Create(league, id, "home", "away", 5, 1, "Regular Season", type, "Week 1", 3, type == "playoff");
    private static void Scrimmage(GameResult game, int yard = 35, int down = 1, int distance = 10)
    {
        var state = game.ProGame;
        state.Phase = "scrimmage"; state.PossessionTeamId = "home"; state.YardLine = yard; state.Down = down; state.Distance = distance;
        state.CurrentDrive = 1;
        state.Drives.Add(new GameDriveState { Number = 1, TeamId = "home", StartYardLine = yard, StartSequence = 1 });
    }
    private static (LeagueState League, GameResult Game, GamePlayEventState Play) FindPlay(Func<GamePlayEventState, bool> predicate, string offense = "Pass", int yard = 50, int down = 1, int distance = 10)
    {
        var league = League();
        for (ulong seed = 1; seed <= 5000; seed++)
        {
            var game = Game(league); Scrimmage(game, yard, down, distance);
            game.ProGame.RandomState = seed;
            game.ProGame.PendingDecision = new ProGameDecision { Offense = offense, FourthDown = "Go" };
            var play = new ProSnapEngine(league, game).Step();
            if (predicate(play)) return (league, game, play);
        }
        throw new Exception("Expected outcome not found in deterministic sample.");
    }

    [Fact]
    public void VersionOneReplayRemainsCompatibleWithItsOriginalRules()
    {
        var league = League(); var game = Game(league);
        game.ProGame.RulesVersion = "pro-snap-v1-2025";
        new ProSnapEngine(league, game).Finish();
        var projection = new
        {
            game.HomeScore, game.AwayScore, game.ProGame.RandomState, game.ProGame.Drives,
            game.BoxScore.TeamStats, game.BoxScore.PlayerStats,
            Events = game.BoxScore.PlayByPlay.Select(p => new
            {
                p.Sequence, p.Quarter, p.ClockSeconds, p.PossessionTeamId, p.YardLine, p.Down, p.Distance,
                p.PlayType, p.Outcome, p.YardsGained, p.HomeScore, p.AwayScore, p.Description,
                p.OffensiveCall, p.DefensiveCall, p.ManagementCall, p.SpecialTeamsCall,
                p.ParticipantIds, p.StatChanges, p.InjuredPlayerId,
            }),
        };
        var digest = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Json(projection))));
        Assert.Equal("77AC0A71650CA2084A577A4F204E3E5F4028FB74EFD6D5C2F15EDBBF9450EEE5", digest);
    }

    [Fact]
    public void IdenticalStateAndSeedReproduceEveryPlayAndStatistic()
    {
        var league = League(); var first = Game(league); var second = Clone(first);
        new ProSnapEngine(league, first).Finish(); new ProSnapEngine(Clone(league), second).Finish();
        Assert.Equal(Json(first), Json(second));
        Assert.InRange(first.BoxScore.PlayByPlay.Count, 100, 500);
        Assert.True(first.ProGame.Completed);
        Assert.All(first.ProGame.Drives, d => Assert.True(d.EndSequence >= d.StartSequence));
    }

    [Fact]
    public void FullGamesMaintainLegalClockFieldPossessionAndScore()
    {
        var league = League();
        for (var seed = 0; seed < 12; seed++)
        {
            var result = Game(league, $"legal-{seed}"); new ProSnapEngine(league, result).Finish();
            var home = 0; var away = 0; var previousQuarter = 1; var previousClock = 900;
            foreach (var p in result.BoxScore.PlayByPlay)
            {
                Assert.Contains(p.PossessionTeamId, new[] { "home", "away" });
                Assert.InRange(p.YardLine, 1, 99); Assert.InRange(p.Down, 1, 4); Assert.InRange(p.Distance, 1, 99);
                Assert.InRange(p.ClockSeconds, 0, 900); Assert.InRange(p.ElapsedSeconds, 0, p.StartClockSeconds);
                Assert.True(p.Quarter > previousQuarter || p.ClockSeconds <= previousClock);
                previousQuarter = p.Quarter; previousClock = p.ClockSeconds;
                Assert.Contains(p.Points, new[] { 0, 1, 2, 3, 6 });
                if (p.ScoringTeamId == "home") home += p.Points;
                if (p.ScoringTeamId == "away") away += p.Points;
                Assert.Equal(home, p.HomeScore); Assert.Equal(away, p.AwayScore);
            }
            Assert.Equal(home, result.HomeScore); Assert.Equal(away, result.AwayScore);
            Assert.True(result.BoxScore.PlayByPlay.Last().IsFinal);
            Assert.Equal(Enumerable.Range(1, result.BoxScore.PlayByPlay.Count), result.BoxScore.PlayByPlay.Select(p => p.Sequence));
        }
    }

    [Theory]
    [InlineData("completion")]
    [InlineData("incomplete")]
    [InlineData("sack")]
    [InlineData("interception")]
    [InlineData("fumble")]
    public void PassingOutcomesHaveRealParticipantsAndReconciledProduction(string outcome)
    {
        var (_, game, play) = FindPlay(p => p.Outcome == outcome);
        Assert.Contains("home-QB-0", play.ParticipantIds);
        Assert.Equal(play.StatChanges.Sum(s => s.PassingYards), play.StatChanges.Sum(s => s.ReceivingYards));
        Assert.Equal(play.StatChanges.Sum(s => s.SacksTaken) > 0 ? 0 : 1, play.StatChanges.Sum(s => s.PassAttempts));
        if (outcome == "interception") { Assert.Equal("away", game.ProGame.PossessionTeamId); Assert.True(play.IsTurnover); }
        if (outcome == "sack") Assert.Equal(-play.YardsGained, play.StatChanges.Sum(s => s.SackYardsLost));
        if (outcome == "incomplete") { Assert.Equal(2, game.ProGame.Down); Assert.Equal(10, game.ProGame.Distance); }
    }

    [Fact]
    public void FirstDownAndFailedFourthDownFollowActualGain()
    {
        var (_, first, gain) = FindPlay(p => p.YardsGained >= 10 && p.Outcome == "rush", "Run");
        Assert.True(gain.IsFirstDown); Assert.Equal(1, first.ProGame.Down); Assert.Equal(10, first.ProGame.Distance);
        var (_, failed, miss) = FindPlay(p => p.Outcome == "incomplete", "Pass", 40, 4, 10);
        Assert.True(miss.IsTurnover); Assert.Equal("away", failed.ProGame.PossessionTeamId);
        Assert.Equal(60, failed.ProGame.YardLine); Assert.Equal(1, failed.ProGame.Down);
        Assert.Equal("downs", failed.ProGame.Drives[0].Outcome);
    }

    [Theory]
    [InlineData("Deep kick", 35)]
    [InlineData("Return kick", 0)]
    public void KickoffsStartNewDrives(string call, int expectedSpot)
    {
        var league = League(); var game = Game(league); game.ProGame.PossessionTeamId = "home";
        game.ProGame.PendingDecision.SpecialTeams = call;
        var play = new ProSnapEngine(league, game).Step();
        Assert.Equal("away", game.ProGame.PossessionTeamId); Assert.Equal("scrimmage", game.ProGame.Phase);
        Assert.Single(game.ProGame.Drives); Assert.Equal(1, game.ProGame.Down);
        if (expectedSpot > 0) Assert.Equal(expectedSpot, game.ProGame.YardLine);
        else Assert.Equal(6, play.ElapsedSeconds);
    }

    [Theory]
    [InlineData("Punt")]
    [InlineData("Field goal")]
    public void SpecialTeamsEndPossessionAndCreditTheSpecialist(string call)
    {
        var league = League(); var game = Game(league); Scrimmage(game, 65, 4, 9);
        game.ProGame.PendingDecision = new() { FourthDown = "Kick", SpecialTeams = call };
        var play = new ProSnapEngine(league, game).Step();
        Assert.Equal(1, game.ProGame.Drives[0].EndSequence);
        if (call == "Punt") { Assert.Equal("away", game.ProGame.PossessionTeamId); Assert.Equal(1, play.StatChanges.Sum(s => s.Punts)); }
        else
        {
            Assert.Equal(1, play.StatChanges.Sum(s => s.FieldGoalAttempts));
            Assert.Equal(play.Points == 3 ? "kickoff" : "scrimmage", game.ProGame.Phase);
            if (play.Points == 0) Assert.Equal(42, game.ProGame.YardLine); // spot of kick, not scrimmage
        }
    }

    [Fact]
    public void TouchdownTryAndSafetyUseLegalScoresAndPossession()
    {
        var (league, game, touchdown) = FindPlay(p => p.Outcome == "touchdown", "Run", 99, 1, 1);
        Assert.Equal(6, touchdown.Points); Assert.Equal(1, touchdown.YardsGained); Assert.Equal("try", game.ProGame.Phase);
        var clock = game.ProGame.ClockSeconds; var engine = new ProSnapEngine(league, game);
        game.ProGame.PendingDecision.SpecialTeams = "Extra point";
        var attempt = engine.Step(); Assert.Equal(clock, game.ProGame.ClockSeconds);
        Assert.Equal(1, attempt.StatChanges.Sum(s => s.ExtraPointAttempts));
        Assert.Equal("kickoff", game.ProGame.Phase);
        var (_, safetyGame, safety) = FindPlay(p => p.Outcome == "safety", "Run", 1);
        Assert.Equal(2, safety.Points); Assert.Equal("away", safety.ScoringTeamId);
        Assert.Equal("home", safetyGame.ProGame.PossessionTeamId); // conceding team takes the free kick
        Assert.Equal("kickoff", safetyGame.ProGame.Phase);
    }

    [Fact]
    public void QuartersCarryPossessionAndHalftimeReversesReceiver()
    {
        var league = League(); var game = Game(league); Scrimmage(game, 47, 3, 7);
        game.ProGame.ClockSeconds = 0;
        var engine = new ProSnapEngine(league, game); engine.Step();
        Assert.Equal(2, game.ProGame.Quarter); Assert.Equal(900, game.ProGame.ClockSeconds);
        Assert.Equal(47, game.ProGame.YardLine); Assert.Equal(3, game.ProGame.Down);
        game.ProGame.ClockSeconds = 0; engine.Step();
        Assert.Equal(3, game.ProGame.Quarter); Assert.Equal("kickoff", game.ProGame.Phase);
        Assert.Equal(game.ProGame.OpeningReceiverId, game.ProGame.PossessionTeamId);
        Assert.Equal("halftime", game.ProGame.Drives[0].Outcome);
    }

    [Theory]
    [InlineData("preseason", 0)]
    [InlineData("regular_season", 600)]
    [InlineData("playoff", 900)]
    public void RegulationTiesUseCompetitionOvertimeRules(string type, int periodLength)
    {
        var league = League(); var game = Game(league, type: type); Scrimmage(game);
        game.ProGame.Quarter = 4; game.ProGame.ClockSeconds = 0;
        var engine = new ProSnapEngine(league, game); engine.Step();
        Assert.Equal(periodLength == 0, game.ProGame.Completed);
        if (periodLength == 0) { Assert.Equal("", game.Winner); return; }
        Assert.Equal(5, game.ProGame.Quarter); Assert.Equal(periodLength, game.ProGame.ClockSeconds);
        game.ProGame.ClockSeconds = 0; engine.Step();
        Assert.Equal(type == "regular_season", game.ProGame.Completed);
        if (type == "playoff") Assert.Equal(6, game.ProGame.Quarter);
    }

    [Fact]
    public void OvertimeFirstTouchdownAllowsResponseButSecondPossessionLossEndsGame()
    {
        var league = League(); var game = Game(league, type: "playoff"); Scrimmage(game);
        game.ProGame.Quarter = 5; game.ProGame.Phase = "try"; game.HomeScore = 6;
        new ProSnapEngine(league, game).Step();
        Assert.False(game.ProGame.Completed); Assert.Contains("home", game.ProGame.OvertimePossessions);
        new ProSnapEngine(league, game).Step();
        Assert.Equal("away", game.ProGame.PossessionTeamId);
        game.ProGame.Down = 4; game.ProGame.YardLine = 20;
        game.ProGame.PendingDecision = new() { FourthDown = "Kick", SpecialTeams = "Punt" };
        new ProSnapEngine(league, game).Step();
        Assert.True(game.ProGame.Completed); Assert.Equal("home", game.Winner);
    }

    [Fact]
    public void BoxScoreIsExactlyThePlayLogReduction()
    {
        var league = League(); var game = Game(league); new ProSnapEngine(league, game).Finish();
        var rebuilt = ProGameStatistics.Rebuild(game);
        Assert.Equal(Json(game.BoxScore.TeamStats), Json(rebuilt.TeamStats));
        Assert.Equal(Json(game.BoxScore.PlayerStats), Json(rebuilt.PlayerStats));
        Assert.Equal(game.HomeScore, rebuilt.TeamStats["points_home"]);
        Assert.Equal(game.AwayScore, rebuilt.TeamStats["points_away"]);
        Assert.All(rebuilt.PlayerStats, s => Assert.Equal(game.BoxScore.PlayByPlay.Count(p => p.ParticipantIds.Contains(s.PlayerId)), s.Snaps));
    }

    [Fact]
    public void SavedDepthAndInjuriesDetermineEveryParticipant()
    {
        var league = League(); var home = league.Teams[0];
        home.Roster.Single(p => p.PlayerId == "home-QB-0").Status = "Inactive";
        home.Roster.Single(p => p.PlayerId == "home-RB-0").CurrentInjury = new() { Name = "Sprain", DaysRemaining = 3 };
        home.DepthChart["QB"] = new() { "home-QB-0", "home-QB-2", "home-QB-1" };
        var game = Game(league); new ProSnapEngine(league, game).Finish();
        Assert.DoesNotContain(game.BoxScore.PlayerStats, s => s.PlayerId is "home-QB-0" or "home-RB-0");
        Assert.Contains(game.BoxScore.PlayerStats, s => s.PlayerId == "home-QB-2");
        Assert.DoesNotContain(game.BoxScore.PlayByPlay.First(p => p.PlayType is "run" or "pass" && p.OffensiveTeamId == "home").ParticipantIds, id => id == "home-QB-1");
    }

    [Fact]
    public void InjuriesPreventLaterParticipationAndPersistInThePlay()
    {
        var (league, game, injury) = FindPlay(p => p.IsInjury);
        var injured = injury.InjuredPlayerId;
        new ProSnapEngine(league, game).Finish();
        Assert.NotNull(injury.Injury);
        Assert.All(game.BoxScore.PlayByPlay.Skip(1), p => Assert.DoesNotContain(injured, p.ParticipantIds));
        Assert.False(league.Teams.SelectMany(t => t.Roster).Single(p => p.PlayerId == injured).CurrentInjury.IsActive); // pure resolver, effects commit once later
    }

    [Theory]
    [InlineData(HeadCoachAuthorityService.OffensivePlayCalling, "offense")]
    [InlineData(HeadCoachAuthorityService.DefensivePlayCalling, "defense")]
    [InlineData(HeadCoachAuthorityService.SpecialTeamsDecisions, "special")]
    [InlineData(HeadCoachAuthorityService.OverallGameManagement, "fourth")]
    [InlineData(HeadCoachAuthorityService.OverallGameManagement, "tempo")]
    public void CoachAuthorityRejectsOnlyItsSupportedDecisionDomain(string domain, string key)
    {
        var league = League(); var game = Game(league); Scrimmage(game, 65, 4, 2);
        if (key == "defense") game.ProGame.PossessionTeamId = "away";
        var choice = key switch
        {
            "offense" => new ProGameDecision { Offense = "Pass" }, "defense" => new() { Defense = "Blitz" },
            "special" => new() { SpecialTeams = "Field goal" }, "fourth" => new() { FourthDown = "Go" }, _ => new() { Tempo = "Hurry" },
        };
        Assert.True(ProGameDecisionService.Validate(league, game, choice, out _));
        league.Teams[0].Coaches[0].Authority.ControlledDomains.Add(domain);
        Assert.False(ProGameDecisionService.Validate(league, game, choice, out var error)); Assert.Contains("Head Coach", error);
        Assert.True(ProGameDecisionService.Validate(league, game, new(), out _));
        Assert.False(ProGameDecisionService.Options(league, game).Single(o => o.Key == key).CanChoose);
    }

    [Fact]
    public void StrongerRatingsImproveAverageOutcomesWithoutPromisingIndividualWins()
    {
        var weak = League(); var strong = Clone(weak);
        foreach (var p in weak.Teams[0].Roster) p.Overall = 45;
        foreach (var p in strong.Teams[0].Roster) p.Overall = 95;
        var weakMargin = 0; var strongMargin = 0;
        for (var i = 0; i < 40; i++)
        {
            var a = Game(weak, $"ratings-{i}"); var b = Game(strong, $"ratings-{i}");
            new ProSnapEngine(weak, a).Finish(); new ProSnapEngine(strong, b).Finish();
            weakMargin += a.HomeScore - a.AwayScore; strongMargin += b.HomeScore - b.AwayScore;
        }
        Assert.True(strongMargin > weakMargin + 200, $"Margins: {strongMargin} versus {weakMargin}");
    }

    [Fact]
    public void LiveReloadAndSubstitutionPreserveResolvedPlaysAndCompleteExactlyOnce()
    {
        var context = new GameCoreContext(); var league = new LeagueBootstrapService(context).CreateTestLeague();
        var scheduled = league.Schedule.First(g => g.GameType == "regular_season" && (g.HomeTeamId == league.UserTeamId || g.AwayTeamId == league.UserTeamId));
        league.Calendar.AbsoluteWeek = scheduled.AbsoluteWeek; league.Calendar.DayIndex = scheduled.DayIndex; league.Calendar.Phase = "Regular Season";
        var live = new LiveGameSessionService(context); Assert.True(live.Start(scheduled.GameId).Ok);
        Assert.True(live.SetPaused(false).Ok);
        for (var i = 0; i < 15; i++) Assert.True(live.Advance(i).Ok);
        Assert.False(live.Advance(14).Ok);
        live.SetPaused(true);
        var before = Json(league.ActiveLiveGameSession.PlayedEvents);
        var user = league.Teams.Single(t => t.TeamId == league.UserTeamId);
        var qb = user.Roster.Last(p => p.Position == "QB" && PlayerInjuryService.IsAvailableForGame(p));
        Assert.True(live.ApplyDepthAdjustment("set_starter", "QB", qb.PlayerId).Ok);
        Assert.Equal(before, Json(league.ActiveLiveGameSession.PlayedEvents));
        var saves = new GameCoreSaveService(); var name = $"snap-test-{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(saves.Save(context, name).Ok); Assert.True(saves.Save(context, name).Ok); // atomic replacement
            var loaded = saves.Load(name); Assert.True(loaded.Ok, loaded.Message);
            Assert.Equal(before, Json(loaded.League.ActiveLiveGameSession.PlayedEvents));
            var restoredContext = new GameCoreContext { ActiveLeague = loaded.League }; var restored = new LiveGameSessionService(restoredContext);
            live.SetPaused(false); restored.SetPaused(false);
            for (var guard = 0; league.ActiveLiveGameSession.Active && guard < 1000; guard++)
            {
                var sequence = league.ActiveLiveGameSession.NextEventIndex;
                Assert.True(live.Advance(sequence).Ok); Assert.True(restored.Advance(sequence).Ok);
            }
            Assert.True(league.ActiveLiveGameSession.Completed);
            Assert.Equal(Json(league.ActiveLiveGameSession.PendingResult), Json(loaded.League.ActiveLiveGameSession.PendingResult));
            var result = league.Results.Single(r => r.GameId == scheduled.GameId);
            Assert.Contains(result.BoxScore.PlayByPlay.Skip(15), p => p.ParticipantIds.Contains(qb.PlayerId));
            var stats = Json(user.Roster.Select(p => p.SeasonStats)); var health = Json(user.Roster.Select(p => new { p.Fatigue, p.InjuryHistory }));
            Assert.True(live.Advance().Session.Completed); Assert.True(new GameDayService(context).SimulateCurrentUserGame(scheduled.GameId).Ok);
            Assert.Equal(stats, Json(user.Roster.Select(p => p.SeasonStats))); Assert.Equal(health, Json(user.Roster.Select(p => new { p.Fatigue, p.InjuryHistory })));
            Assert.Equal("final", scheduled.Status);
            var standings = new StandingsService(context).BuildStandings(league);
            Assert.Equal(2, standings.Sum(s => s.Wins + s.Losses + s.Ties));
            Assert.All(user.Roster.Where(p => result.BoxScore.PlayerStats.Any(s => s.PlayerId == p.PlayerId)), p => Assert.Equal(1, p.SeasonStats.GamesPlayed));
        }
        finally { saves.Delete(name); }
    }

    [Fact]
    public void QueuedDecisionsReloadExactlyAndAreRevalidatedBeforeResolving()
    {
        var league = League(); var game = Game(league); Scrimmage(game);
        league.ActiveLiveGameSession = new() { Active = true, GameId = game.GameId, PendingResult = game };
        var live = new LiveGameSessionService(new() { ActiveLeague = league });
        Assert.True(live.SubmitDecision(new() { Offense = "Pass", Tempo = "Hurry" }, 0).Ok);
        var restoredLeague = Clone(league); var restored = new LiveGameSessionService(new() { ActiveLeague = restoredLeague });
        live.SetPaused(false); restored.SetPaused(false);
        Assert.True(live.Advance(0).Ok); Assert.True(restored.Advance(0).Ok);
        Assert.Equal(Json(game), Json(restoredLeague.ActiveLiveGameSession.PendingResult));
        Assert.Equal("Pass", game.BoxScore.PlayByPlay[0].OffensiveCall);
        Assert.False(live.Advance(0).Ok); Assert.Single(game.BoxScore.PlayByPlay);
        live.SetPaused(true);
        var option = live.SetPaused(true).Session.Decisions.First(o => o.CanChoose && o.Key is "offense" or "defense");
        var pending = option.Key == "offense" ? new ProGameDecision { Offense = "Run" } : new ProGameDecision { Defense = "Blitz" };
        Assert.True(live.SubmitDecision(pending, 1).Ok);
        league.Teams[0].Coaches[0].Authority.ControlledDomains.Add(option.Domain);
        var before = Json(game); live.SetPaused(false);
        Assert.False(live.Advance(1).Ok); Assert.Equal(before, Json(game));
    }

    [Fact]
    public void LiveSessionBlocksCalendarAndCompetingSimulationCommands()
    {
        var context = new GameCoreContext(); var league = new LeagueBootstrapService(context).CreateTestLeague();
        var scheduled = league.Schedule.First(g => g.HomeTeamId == league.UserTeamId || g.AwayTeamId == league.UserTeamId);
        var live = new LiveGameSessionService(context); var quick = new GameDayService(context);
        Assert.False(live.Start(scheduled.GameId).Ok);
        Assert.False(quick.SimulateCurrentUserGame(scheduled.GameId).Ok);
        league.Calendar.AbsoluteWeek = scheduled.AbsoluteWeek; league.Calendar.DayIndex = scheduled.DayIndex;
        Assert.True(live.Start(scheduled.GameId).Ok); var before = Json(league.Calendar);
        Assert.False(new ContinueService(context).Continue(7).Result.Advanced);
        Assert.False(quick.SimulateScheduledGame(league.Schedule.Last().GameId, true).Ok);
        var playoffs = new PlayoffService(context);
        Assert.False(playoffs.SimulateWildCardRound(league).Ok);
        Assert.False(playoffs.SimulateDivisionalRound(league).Ok);
        Assert.False(playoffs.SimulateConferenceChampionshipRound(league).Ok);
        Assert.False(playoffs.SimulateLeagueChampionshipRound(league).Ok);
        Assert.Equal(before, Json(league.Calendar)); Assert.Empty(league.Results);
    }

    [Fact]
    public void CommittingAnInjuryAndItsStatisticsTwiceHasExactlyOneEffect()
    {
        var (league, game, injury) = FindPlay(p => p.IsInjury);
        league.ActiveLiveGameSession = new() { Active = true, IsPaused = false, GameId = game.GameId, PendingResult = game, NextEventIndex = 1 };
        var live = new LiveGameSessionService(new() { ActiveLeague = league });
        for (var guard = 0; league.ActiveLiveGameSession.Active && guard < 1000; guard++)
            Assert.True(live.Advance(league.ActiveLiveGameSession.NextEventIndex).Ok);
        Assert.True(league.ActiveLiveGameSession.Completed);
        var player = league.Teams.SelectMany(t => t.Roster).Single(p => p.PlayerId == injury.InjuredPlayerId);
        Assert.Equal(injury.Injury.DaysRemaining, player.CurrentInjury.DaysRemaining);
        Assert.Single(player.InjuryHistory);
        var before = Json(league.Teams);
        Assert.True(live.Advance().Session.Completed);
        Assert.Equal(before, Json(league.Teams)); Assert.Single(league.Results);
    }

    [Fact]
    public void OvertimePeriodProjectionReconcilesWithTheFinalScore()
    {
        var league = League(); var game = Game(league); Scrimmage(game);
        game.ProGame.Quarter = 4; game.ProGame.ClockSeconds = 0;
        new ProSnapEngine(league, game).Finish();
        var dto = GameCoreStateHelper.ToGameResultDto(game);
        var periods = Assert.IsType<Dictionary<string, int[]>>(dto.BoxScore["quarter_scores"]);
        Assert.Equal(5, periods["home"].Length);
        Assert.Equal(game.HomeScore, periods["home"].Sum()); Assert.Equal(game.AwayScore, periods["away"].Sum());
        Assert.True((bool)dto.BoxScore["quarter_scores_known"]);
        Assert.False((bool)GameCoreStateHelper.ToGameResultDto(new GameResult()).BoxScore["quarter_scores_known"]);
    }

    [Fact]
    public void LegacyCompletedPlayoffRepairPreservesScoresWithoutNewHealthOrStats()
    {
        var context = new GameCoreContext(); var league = new LeagueBootstrapService(context).CreateTestLeague();
        var quick = new GameDayService(context);
        foreach (var game in league.Schedule.Where(g => g.GameType == "regular_season"))
            Assert.True(quick.SimulateScheduledGame(game.GameId, true).Ok);
        league.Calendar.AbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + 1; league.Calendar.DayIndex = 0;
        var playoffs = new PlayoffService(context); var response = playoffs.SimulateWildCardRound(league);
        Assert.True(response.Ok, response.Error);
        var games = league.PlayoffBracket.ConferenceBrackets.SelectMany(c => c.Rounds).Where(r => r.Round == PlayoffService.WildCardRound).SelectMany(r => r.Games).ToList();
        var ids = games.Select(g => g.GameId).ToHashSet();
        league.Results.RemoveAll(r => ids.Contains(r.GameId));
        var before = Json(league.Teams);
        Assert.True(playoffs.SimulateWildCardRound(league).Ok); Assert.Equal(before, Json(league.Teams));
        foreach (var game in games)
        {
            var result = league.Results.Single(r => r.GameId == game.GameId);
            Assert.Equal(game.HomeScore, result.HomeScore); Assert.Equal(game.AwayScore, result.AwayScore);
            Assert.Empty(result.BoxScore.PlayByPlay); Assert.Empty(result.BoxScore.PlayerStats); Assert.Null(result.ProGame);
        }
    }

    [Fact]
    public void LegacySavedPlaybackIsPreservedWithoutInventingSnapHistory()
    {
        var context = new GameCoreContext(); var league = new LeagueBootstrapService(context).CreateTestLeague();
        var scheduled = league.Schedule.First(g => g.HomeTeamId == league.UserTeamId || g.AwayTeamId == league.UserTeamId);
        var oldResult = new GameResult { GameId = scheduled.GameId, HomeTeamId = scheduled.HomeTeamId, AwayTeamId = scheduled.AwayTeamId, HomeScore = 7 };
        oldResult.BoxScore.PlayByPlay.Add(new GamePlayEventState { Sequence = 1, Quarter = 4, ClockSeconds = 0, HomeScore = 7, Description = "Legacy final" });
        league.SaveVersion = 32;
        league.ActiveLiveGameSession = new() { Active = true, GameId = scheduled.GameId, PendingResult = oldResult };
        var save = new GameCoreSaveService(); var name = $"legacy-snap-{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(save.Save(context, name).Ok); var loaded = save.Load(name); Assert.True(loaded.Ok, loaded.Message);
            Assert.Null(loaded.League.ActiveLiveGameSession.PendingResult.ProGame);
            var restored = new GameCoreContext { ActiveLeague = loaded.League }; var live = new LiveGameSessionService(restored);
            Assert.False(live.ApplyDepthAdjustment("set_starter", "QB", "missing").Ok);
            live.SetPaused(false); Assert.True(live.Advance().Session.Completed); Assert.True(live.Advance().Session.Completed);
            var result = loaded.League.Results.Single(r => r.GameId == scheduled.GameId);
            Assert.Single(result.BoxScore.PlayByPlay); Assert.Equal(7, result.HomeScore); Assert.Empty(result.BoxScore.PlayerStats);
        }
        finally { save.Delete(name); }
    }
}
