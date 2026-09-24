using System;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

/// <summary>Pure clock-rule and staff recommendations; resolution and the event log remain in ProSnapEngine.</summary>
public static class ProClockManagementService
{
    public static bool Enabled(ProGameState state) => state?.RulesVersion == ProGameState.ClockRulesVersion;
    public static int Timeouts(GameResult game, string teamId) => teamId == game.HomeTeamId ? game.ProGame.HomeTimeouts : game.ProGame.AwayTimeouts;
    public static bool HasWarning(ProGameState state) => state.Quarter is 2 or 4
        || (state.Quarter >= 5 && (!state.RequireWinner || state.Quarter % 2 == 0));
    public static bool WarningDue(ProGameState state) => HasWarning(state) && state.LastWarningQuarter != state.Quarter
        && state.ClockSeconds is > 0 and <= 120;
    public static bool CanCallTimeout(GameResult game, string teamId) => Enabled(game.ProGame) && !game.ProGame.Completed
        && (teamId == game.HomeTeamId || teamId == game.AwayTeamId) && game.ProGame.Phase == "scrimmage"
        && game.ProGame.ClockRunning && game.ProGame.ClockSeconds > 0 && !WarningDue(game.ProGame) && Timeouts(game, teamId) > 0;
    public static bool HasQuarterback(LeagueState league, GameResult game) => league.Teams
        .First(t => t.TeamId == game.ProGame.PossessionTeamId).Roster.Any(p => p.Position == "QB"
            && PlayerInjuryService.IsAvailableForGame(p) && !game.ProGame.InjuredPlayerIds.Contains(p.PlayerId));
    private static int Deficit(GameResult game) => game.ProGame.PossessionTeamId == game.HomeTeamId
        ? game.AwayScore - game.HomeScore : game.HomeScore - game.AwayScore;
    private static string Opponent(GameResult game) => game.ProGame.PossessionTeamId == game.HomeTeamId ? game.AwayTeamId : game.HomeTeamId;
    // Postseason overtime continues across period boundaries; it does not create a deadline for the current drive.
    private static bool IsDeadline(ProGameState state) => state.Quarter is 2 or 4 || state.Quarter >= 5 && !state.RequireWinner;

    public static string TimeoutRecommendation(LeagueState league, GameResult game)
    {
        var state = game.ProGame;
        if (state.PendingDecision.Timeout == "Use timeout" && CanCallTimeout(game, league.UserTeamId)) return league.UserTeamId;
        bool Allowed(string id) => CanCallTimeout(game, id) && !(id == league.UserTeamId && state.PendingDecision.Timeout == "No timeout");
        var defense = Opponent(game);
        if (IsDeadline(state) && state.Quarter >= 4 && state.ClockSeconds <= 120 && Deficit(game) <= 0 && Allowed(defense)) return defense;
        if (IsDeadline(state) && (state.Quarter == 2 || Deficit(game) >= 0) && state.ClockSeconds <= 30
            && state.PendingDecision.Offense != "Spike" && Allowed(state.PossessionTeamId)) return state.PossessionTeamId;
        return "";
    }

    public static string TempoRecommendation(GameResult game)
    {
        var state = game.ProGame;
        return state.ClockSeconds <= 120 && IsDeadline(state)
            ? state.Quarter == 2 || Deficit(game) >= 0 ? "Hurry" : "Chew" : "Normal";
    }

    public static string DownRecommendation(GameResult game)
    {
        var state = game.ProGame; var deficit = Deficit(game);
        if (IsDeadline(state) && state.YardLine >= 60 && state.ClockSeconds <= 12
            && (state.Quarter == 2 || state.Quarter >= 4 && deficit is >= 0 and <= 3)) return "Kick";
        if (state.Down != 4) return "Go";
        if (state.YardLine >= 60 && state.Quarter >= 4 && state.ClockSeconds < 150 && deficit is >= 0 and <= 3) return "Kick";
        return state.YardLine > 50 && state.Distance <= 2 || deficit > 0 && state.Quarter >= 4 && state.ClockSeconds < 150 ? "Go" : "Kick";
    }

    public static string OffenseRecommendation(GameResult game)
    {
        var state = game.ProGame;
        var drainable = Math.Max(0, (4 - state.Down) * 40 - Timeouts(game, Opponent(game)) * 38);
        if (state.Quarter == 4 && Deficit(game) < 0 && state.ClockSeconds <= 120 && state.ClockSeconds <= drainable && state.YardLine > 1)
            return "Kneel";
        if (IsDeadline(state) && (state.Quarter == 2 || Deficit(game) >= 0) && state.ClockRunning
            && state.ClockSeconds is >= 10 and <= 30 && state.Down < 4 && state.YardLine >= 40
            && Timeouts(game, state.PossessionTeamId) == 0 && state.PendingDecision.Tempo != "Chew") return "Spike";
        return "";
    }

    public static int Runoff(ProGameState state, string tempo, string offenseCall) => !state.ClockRunning ? 0
        : tempo == "Chew" ? 38 : offenseCall == "Spike" ? 3 : tempo == "Hurry" ? 7 : 24;
}
