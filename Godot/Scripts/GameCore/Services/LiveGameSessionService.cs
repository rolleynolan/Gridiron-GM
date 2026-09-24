using System;
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
        var game = string.IsNullOrWhiteSpace(gameId) ? new GameDayService(_context).GetCurrentUserGame()
            : league.Schedule.FirstOrDefault(g => string.Equals(g.GameId, gameId, StringComparison.OrdinalIgnoreCase));
        if (game == null || !GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId) || GameCoreStateHelper.IsFinal(game))
            return Fail("No playable user game was found.");
        if (league.Results.Any(r => r.GameId == game.GameId)) return Fail("This game is already final.");
        if (league.ActiveLiveGameSession?.Active == true)
            return league.ActiveLiveGameSession.GameId == game.GameId ? Snapshot(league.ActiveLiveGameSession) : Fail("Another live game is already in progress.");
        if (game.AbsoluteWeek != league.Calendar.AbsoluteWeek || game.DayIndex != league.Calendar.DayIndex)
            return Fail("Advance to the scheduled game day before starting a live game.");
        foreach (var team in league.Teams.Where(t => t.TeamId == game.HomeTeamId || t.TeamId == game.AwayTeamId))
            if (!team.Coaches.Any(c => c.Role == "Head Coach")) return Fail($"{team.Name} must appoint a Head Coach before playing.");
        league.ActiveLiveGameSession = new LiveGameSessionState
        {
            Active = true, IsPaused = true, GameId = game.GameId,
            PendingResult = ProSnapEngine.Create(league, game.GameId, game.HomeTeamId, game.AwayTeamId, game.AbsoluteWeek,
                game.PhaseWeek, game.Phase, game.GameType, game.WeekLabel, 3, game.GameType != "regular_season" && game.GameType != "preseason"),
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

    public LiveGameSessionResponse SubmitDecision(ProGameDecision decision, int expectedSequence)
    {
        var session = ResolveActive();
        if (session?.PendingResult.ProGame == null) return Fail("This game has no supported live play decisions.");
        if (!session.IsPaused) return Fail("Pause before choosing the next play.");
        if (expectedSequence != session.NextEventIndex) return Fail("The game has advanced. Review the current situation.");
        decision ??= new ProGameDecision();
        if (!ProGameDecisionService.Validate(_context.ActiveLeague, session.PendingResult, decision, out var error)) return Fail(error);
        session.PendingResult.ProGame.PendingDecision = new ProGameDecision
        {
            Offense = decision.Offense, Defense = decision.Defense, SpecialTeams = decision.SpecialTeams,
            FourthDown = decision.FourthDown, Tempo = decision.Tempo,
        };
        return Snapshot(session);
    }

    public LiveGameSessionResponse Advance(int? expectedSequence = null)
    {
        var session = ResolveActive();
        if (session == null)
        {
            var completed = _context.ActiveLeague?.ActiveLiveGameSession;
            return completed?.Completed == true ? Snapshot(completed) : Fail("No live game session is active.");
        }
        if (expectedSequence.HasValue && expectedSequence.Value != session.NextEventIndex)
            return Fail("The game has advanced; this command was not applied again.");
        if (session.IsPaused) return Fail("Resume the live game before advancing.");
        GamePlayEventState current;
        if (session.PendingResult.ProGame != null)
        {
            if (!ProGameDecisionService.Validate(_context.ActiveLeague, session.PendingResult, session.PendingResult.ProGame.PendingDecision, out var error))
                return Fail(error);
            try { current = new ProSnapEngine(_context.ActiveLeague, session.PendingResult).Step(); }
            catch (InvalidOperationException ex) { return Fail(ex.Message); }
            session.NextEventIndex = session.PendingResult.BoxScore.PlayByPlay.Count;
            if (session.PendingResult.ProGame.Completed) return Complete(session, current);
        }
        else
        {
            // Preserve legacy saved playback. Never infer a snap state from an aggregate result.
            var events = session.PendingResult.BoxScore.PlayByPlay;
            if (session.NextEventIndex >= events.Count) return Complete(session);
            current = events[session.NextEventIndex++];
            session.PlayedEvents.Add(current);
            if (session.NextEventIndex >= events.Count) return Complete(session, current);
        }
        return Snapshot(session, current);
    }

    public LiveGameSessionResponse ApplyDepthAdjustment(string action, string position, string playerId, string targetPlayerId = null)
    {
        var session = ResolveActive();
        if (session == null) return Fail("No live game session is active.");
        if (!session.IsPaused) return Fail("Pause the live game before changing the depth chart.");
        if (session.PendingResult.ProGame == null) return Fail("This legacy game uses saved playback. Depth changes are available after it finishes.");
        var teamId = _context.ActiveLeague.UserTeamId;
        var update = new DepthChartService(_context).UpdateDepthChart(action, position, playerId, teamId, targetPlayerId);
        if (!update.Ok) return Fail(update.Error);
        session.Adjustments.Add(new LiveGameAdjustmentState
        {
            AfterEventSequence = session.NextEventIndex, TeamId = teamId, Position = position ?? "",
            PlayerId = playerId ?? "", TargetPlayerId = targetPlayerId ?? "", Action = action ?? "",
        });
        return Snapshot(session);
    }

    private LiveGameSessionResponse Complete(LiveGameSessionState session, GamePlayEventState current = null)
    {
        GameDayService.CommitResult(_context.ActiveLeague, session.PendingResult);
        new ScheduleService(_context).RefreshStatuses(_context.ActiveLeague);
        session.Active = false; session.Completed = true; session.IsPaused = true;
        return Snapshot(session, current);
    }
    private LiveGameSessionState ResolveActive() => _context.ActiveLeague?.ActiveLiveGameSession is { Active: true } session ? session : null;
    private static LiveGameSessionResponse Fail(string error) => new() { Ok = false, Error = error };
    private LiveGameSessionResponse Snapshot(LiveGameSessionState session, GamePlayEventState current = null) => new()
    {
        Ok = true,
        Session = new LiveGameSessionDto
        {
            GameId = session.GameId, Active = session.Active, Completed = session.Completed, IsPaused = session.IsPaused,
            NextEventIndex = session.NextEventIndex, TotalEvents = session.PendingResult.BoxScore.PlayByPlay.Count,
            CurrentEvent = current, Result = GameCoreStateHelper.ToGameResultDto(session.PendingResult),
            PlayedEvents = session.PlayedEvents.ToList(),
            Decisions = ProGameDecisionService.Options(_context.ActiveLeague, session.PendingResult),
            Quarter = session.PendingResult.ProGame?.Quarter ?? 0,
            ClockSeconds = session.PendingResult.ProGame?.ClockSeconds ?? 0,
            YardLine = session.PendingResult.ProGame?.YardLine ?? 0,
            PossessionTeamId = session.PendingResult.ProGame?.PossessionTeamId ?? "",
            Situation = session.PendingResult.ProGame == null ? "Legacy saved playback" :
                $"{session.PendingResult.ProGame.Phase.ToUpperInvariant()} · {session.PendingResult.ProGame.Down} & {session.PendingResult.ProGame.Distance}",
        },
    };
}
