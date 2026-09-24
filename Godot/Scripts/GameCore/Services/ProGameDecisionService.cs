using System;
using System.Collections.Generic;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class GameDecisionOption
{
    public string Key { get; set; } = "";
    public string Domain { get; set; } = "";
    public string Controller { get; set; } = "";
    public bool CanChoose { get; set; }
    public string Selected { get; set; } = "";
    public string[] Choices { get; set; } = Array.Empty<string>();
}

public static class ProGameDecisionService
{
    public static List<GameDecisionOption> Options(LeagueState league, GameResult result)
    {
        var options = new List<GameDecisionOption>();
        var state = result?.ProGame;
        if (state == null || state.Completed) return options;
        var team = GameCoreStateHelper.ResolveTeam(league, league.UserTeamId);
        var offense = state.PossessionTeamId == league.UserTeamId;
        var inGame = league.UserTeamId == result.HomeTeamId || league.UserTeamId == result.AwayTeamId;
        var clockRules = ProClockManagementService.Enabled(state);
        void Add(string key, string domain, bool relevant, string selected, params string[] choices)
        {
            var coach = HeadCoachAuthorityService.IsHeadCoachControlled(team, domain);
            options.Add(new GameDecisionOption { Key = key, Domain = domain, Controller = coach ? "Head Coach" : "GM",
                CanChoose = inGame && relevant && !coach && (state.ClockSeconds > 0 || state.Phase == "try"), Selected = selected, Choices = choices });
        }
        Add("offense", HeadCoachAuthorityService.OffensivePlayCalling, offense && state.Phase == "scrimmage", state.PendingDecision.Offense,
            clockRules ? new[] { "Run", "Pass", "Kneel", "Spike" } : new[] { "Run", "Pass" });
        Add("defense", HeadCoachAuthorityService.DefensivePlayCalling, !offense && state.Phase == "scrimmage", state.PendingDecision.Defense, "Balanced", "Run focus", "Pass focus", "Blitz");
        Add("special", HeadCoachAuthorityService.SpecialTeamsDecisions, offense && (state.Phase != "scrimmage" || state.Down == 4 || clockRules), state.PendingDecision.SpecialTeams,
            state.Phase == "kickoff" ? new[] { "Deep kick", "Return kick" } : state.Phase == "try" ? new[] { "Extra point", "Two point" } : new[] { "Punt", "Field goal" });
        Add("fourth", HeadCoachAuthorityService.OverallGameManagement, offense && state.Phase == "scrimmage" && (state.Down == 4 || clockRules), state.PendingDecision.FourthDown, "Go", "Kick");
        Add("tempo", HeadCoachAuthorityService.OverallGameManagement, offense && state.Phase == "scrimmage", state.PendingDecision.Tempo, "Normal", "Hurry", "Chew");
        if (clockRules) Add("timeout", HeadCoachAuthorityService.OverallGameManagement, ProClockManagementService.CanCallTimeout(result, league.UserTeamId), state.PendingDecision.Timeout, "Use timeout", "No timeout");
        return options;
    }

    public static bool Validate(LeagueState league, GameResult result, ProGameDecision decision, out string error)
    {
        foreach (var option in Options(league, result))
        {
            var selected = option.Key switch { "offense" => decision.Offense, "defense" => decision.Defense, "special" => decision.SpecialTeams, "fourth" => decision.FourthDown, "timeout" => decision.Timeout, _ => decision.Tempo };
            if (string.IsNullOrEmpty(selected)) continue;
            if (!option.CanChoose)
            {
                error = $"{HeadCoachAuthorityService.GetDisplayName(option.Domain)} is controlled by {option.Controller} or is unavailable for this play.";
                return false;
            }
            if (Array.IndexOf(option.Choices, selected) < 0)
            {
                error = $"Unsupported {option.Key} decision.";
                return false;
            }
        }
        if (decision.Offense is "Kneel" or "Spike" && !ProClockManagementService.HasQuarterback(league, result))
        {
            error = "An available quarterback is required for a kneel or spike."; return false;
        }
        if (!ProClockManagementService.Enabled(result.ProGame) && !string.IsNullOrEmpty(decision.Timeout))
        {
            error = "Timeout commands are unavailable under this saved game's rules."; return false;
        }
        error = "";
        return true;
    }
}
