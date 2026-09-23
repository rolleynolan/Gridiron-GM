using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class LiveGameSessionService
{
    private readonly GameCoreContext _context;

    public LiveGameSessionService(GameCoreContext context) => _context = context;

    public LiveGameSessionResponse Start(string gameId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null) return Fail("No active league loaded.");
        var gameDay = new GameDayService(_context);
        var game = string.IsNullOrWhiteSpace(gameId)
            ? gameDay.GetCurrentUserGame()
            : league.Schedule.FirstOrDefault(entry => string.Equals(entry.GameId, gameId, StringComparison.OrdinalIgnoreCase));
        if (game == null || !GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId) || GameCoreStateHelper.IsFinal(game))
            return Fail("No playable user game was found.");
        if (league.Results.Any(result => string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase)))
            return Fail("This game is already final.");

        league.ActiveLiveGameSession ??= new LiveGameSessionState();
        if (league.ActiveLiveGameSession.Active && string.Equals(league.ActiveLiveGameSession.GameId, game.GameId, StringComparison.OrdinalIgnoreCase))
            return Snapshot(league.ActiveLiveGameSession);
        if (league.ActiveLiveGameSession.Active)
            return Fail("Another live game is already in progress.");

        var pending = GameDayService.SimulateMatchup(league, game.GameId, game.HomeTeamId, game.AwayTeamId, game.AbsoluteWeek, game.PhaseWeek, game.Phase, game.GameType, game.WeekLabel, game.DayIndex, 3, true);
        league.ActiveLiveGameSession = new LiveGameSessionState
        {
            Active = true,
            IsPaused = true,
            GameId = game.GameId,
            PendingResult = pending,
        };
        return Snapshot(league.ActiveLiveGameSession);
    }

    public LiveGameSessionResponse SetPaused(bool paused)
    {
        var session = ResolveActive();
        if (session == null) return Fail("No live game session is active.");
        session.IsPaused = paused;
        return Snapshot(session);
    }

    public LiveGameSessionResponse Advance()
    {
        var session = ResolveActive();
        if (session == null) return Fail("No live game session is active.");
        if (session.IsPaused) return Fail("Resume the live game before advancing playback.");
        var events = session.PendingResult?.BoxScore?.PlayByPlay ?? new List<GamePlayEventState>();
        if (session.NextEventIndex >= events.Count)
            return Complete(session);
        var current = events[session.NextEventIndex++];
        session.PlayedEvents.Add(ClonePlay(current));
        if (session.NextEventIndex >= events.Count)
            return Complete(session, current);
        return Snapshot(session, current);
    }

    public LiveGameSessionResponse ApplyDepthAdjustment(string action, string position, string playerId, string targetPlayerId = null)
    {
        var league = _context.ActiveLeague;
        var session = ResolveActive();
        if (session == null) return Fail("No live game session is active.");
        if (!session.IsPaused) return Fail("Pause the live game before changing the depth chart.");
        var teamId = league.UserTeamId;
        var update = new DepthChartService(_context).UpdateDepthChart(action, position, playerId, teamId, targetPlayerId);
        if (!update.Ok) return Fail(update.Error);

        session.Adjustments.Add(new LiveGameAdjustmentState
        {
            AfterEventSequence = session.PlayedEvents.LastOrDefault()?.Sequence ?? 0,
            TeamId = teamId,
            Position = position ?? "",
            PlayerId = playerId ?? "",
            TargetPlayerId = targetPlayerId ?? "",
            Action = action ?? "",
        });
        RebuildUnplayedOutcome(league, session);
        return Snapshot(session);
    }

    private LiveGameSessionResponse Complete(LiveGameSessionState session, GamePlayEventState current = null)
    {
        var league = _context.ActiveLeague;
        var existing = league.Results.FirstOrDefault(result => string.Equals(result.GameId, session.GameId, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            var result = session.PendingResult;
            PlayerInjuryService.ApplyDeterministicGameInjuries(league, result);
            GameDayService.AppendInjuryEvents(league, result);
            PlayerStatisticsService.ApplyGameFatigue(league, result);
            PlayerStatisticsService.ApplyRegularSeasonStats(league, result);
            league.Results.Add(result);
            var game = league.Schedule.First(entry => string.Equals(entry.GameId, session.GameId, StringComparison.OrdinalIgnoreCase));
            game.HomeScore = result.HomeScore; game.AwayScore = result.AwayScore; game.Winner = result.Winner; game.Status = "final";
            new ScheduleService(_context).RefreshStatuses(league);
        }
        session.Active = false;
        session.Completed = true;
        session.IsPaused = true;
        return Snapshot(session, current);
    }

    private void RebuildUnplayedOutcome(LeagueState league, LiveGameSessionState session)
    {
        var game = league.Schedule.First(entry => string.Equals(entry.GameId, session.GameId, StringComparison.OrdinalIgnoreCase));
        var revised = GameDayService.SimulateMatchup(league, game.GameId, game.HomeTeamId, game.AwayTeamId, game.AbsoluteWeek, game.PhaseWeek, game.Phase, game.GameType, game.WeekLabel, game.DayIndex, 3, true);
        var prefix = session.PendingResult.BoxScore.PlayByPlay.Take(session.NextEventIndex).Select(ClonePlay).ToList();
        var future = session.PendingResult.BoxScore.PlayByPlay.Skip(session.NextEventIndex).Select(ClonePlay).ToList();
        var currentHome = prefix.LastOrDefault()?.HomeScore ?? 0;
        var currentAway = prefix.LastOrDefault()?.AwayScore ?? 0;
        revised.HomeScore = Math.Max(currentHome, revised.HomeScore);
        revised.AwayScore = Math.Max(currentAway, revised.AwayScore);
        if (revised.HomeScore == revised.AwayScore) revised.HomeScore++;
        RewriteFutureScores(future, game.HomeTeamId, game.AwayTeamId, currentHome, currentAway, revised.HomeScore, revised.AwayScore);
        session.PendingResult.HomeScore = revised.HomeScore;
        session.PendingResult.AwayScore = revised.AwayScore;
        var home = GameCoreStateHelper.ResolveTeam(league, game.HomeTeamId);
        var away = GameCoreStateHelper.ResolveTeam(league, game.AwayTeamId);
        session.PendingResult.Winner = revised.HomeScore > revised.AwayScore ? home?.Name ?? game.HomeTeamId : away?.Name ?? game.AwayTeamId;
        session.PendingResult.Summary = $"{session.PendingResult.Winner} won {revised.HomeScore}-{revised.AwayScore}.";
        session.PendingResult.BoxScore.TeamStats = revised.BoxScore.TeamStats;
        session.PendingResult.BoxScore.PlayerStats = revised.BoxScore.PlayerStats;
        session.PendingResult.BoxScore.PlayByPlay = prefix.Concat(future).ToList();
        session.PendingResult.BoxScore.Final = $"{home?.Abbreviation ?? "HOME"} {revised.HomeScore}, {away?.Abbreviation ?? "AWAY"} {revised.AwayScore}";
        var finalEvent = session.PendingResult.BoxScore.PlayByPlay.LastOrDefault(play => play.ClockSeconds == 0);
        if (finalEvent != null)
            finalEvent.Description = $"Final: {home?.Abbreviation ?? "HOME"} {revised.HomeScore}, {away?.Abbreviation ?? "AWAY"} {revised.AwayScore}.";
        for (var index = 0; index < session.PendingResult.BoxScore.PlayByPlay.Count; index++) session.PendingResult.BoxScore.PlayByPlay[index].Sequence = index + 1;
    }

    private static void RewriteFutureScores(List<GamePlayEventState> future, string homeId, string awayId, int currentHome, int currentAway, int targetHome, int targetAway)
    {
        RewriteTeamFuture(future, homeId, true, currentHome, targetHome);
        RewriteTeamFuture(future, awayId, false, currentAway, targetAway);
        var home = currentHome; var away = currentAway;
        foreach (var play in future)
        {
            if (play.IsScoringPlay)
            {
                if (string.Equals(play.PossessionTeamId, homeId, StringComparison.OrdinalIgnoreCase)) home += Math.Max(0, play.HomeScore);
                else if (string.Equals(play.PossessionTeamId, awayId, StringComparison.OrdinalIgnoreCase)) away += Math.Max(0, play.AwayScore);
            }
            play.HomeScore = home; play.AwayScore = away;
        }
        var final = future.LastOrDefault(play => play.ClockSeconds == 0) ?? future.LastOrDefault();
        if (final != null) { final.HomeScore = targetHome; final.AwayScore = targetAway; }
    }

    private static void RewriteTeamFuture(List<GamePlayEventState> future, string teamId, bool home, int current, int target)
    {
        var scoring = future.Where(play => play.IsScoringPlay && string.Equals(play.PossessionTeamId, teamId, StringComparison.OrdinalIgnoreCase)).ToList();
        var remaining = Math.Max(0, target - current);
        if (scoring.Count == 0) return;
        for (var index = 0; index < scoring.Count; index++)
        {
            var points = index == scoring.Count - 1 ? remaining : Math.Min(7, remaining);
            remaining -= points;
            scoring[index].HomeScore = home ? points : 0;
            scoring[index].AwayScore = home ? 0 : points;
            scoring[index].Description = points == 7 ? "The drive ends in a touchdown and extra point." : $"The drive adds {points} points.";
        }
    }

    private LiveGameSessionState ResolveActive() => _context.ActiveLeague?.ActiveLiveGameSession is { Active: true } session ? session : null;
    private static LiveGameSessionResponse Fail(string error) => new() { Ok = false, Error = error ?? "Live game request failed." };
    private static LiveGameSessionResponse Snapshot(LiveGameSessionState session, GamePlayEventState current = null) => new()
    {
        Ok = true,
        Session = new LiveGameSessionDto
        {
            GameId = session.GameId,
            Active = session.Active,
            Completed = session.Completed,
            IsPaused = session.IsPaused,
            NextEventIndex = session.NextEventIndex,
            TotalEvents = session.PendingResult?.BoxScore?.PlayByPlay.Count ?? 0,
            CurrentEvent = current,
            Result = GameCoreStateHelper.ToGameResultDto(session.PendingResult),
            PlayedEvents = session.PlayedEvents.Select(ClonePlay).ToList(),
        },
    };

    private static GamePlayEventState ClonePlay(GamePlayEventState play) => new()
    {
        Sequence = play.Sequence, Quarter = play.Quarter, ClockSeconds = play.ClockSeconds, PossessionTeamId = play.PossessionTeamId,
        Down = play.Down, Distance = play.Distance, YardLine = play.YardLine, YardsGained = play.YardsGained, Description = play.Description,
        HomeScore = play.HomeScore, AwayScore = play.AwayScore, IsScoringPlay = play.IsScoringPlay, IsTurnover = play.IsTurnover, IsInjury = play.IsInjury,
    };
}
