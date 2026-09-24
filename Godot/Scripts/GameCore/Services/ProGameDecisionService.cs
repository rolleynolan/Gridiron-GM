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
        void Add(string key, string domain, bool relevant, string selected, params string[] choices)
        {
            var coach = HeadCoachAuthorityService.IsHeadCoachControlled(team, domain);
            options.Add(new GameDecisionOption { Key = key, Domain = domain, Controller = coach ? "Head Coach" : "GM",
                CanChoose = inGame && relevant && !coach && (state.ClockSeconds > 0 || state.Phase == "try"), Selected = selected, Choices = choices });
        }
        Add("offense", HeadCoachAuthorityService.OffensivePlayCalling, offense && state.Phase == "scrimmage", state.PendingDecision.Offense, "Run", "Pass");
        Add("defense", HeadCoachAuthorityService.DefensivePlayCalling, !offense && state.Phase == "scrimmage", state.PendingDecision.Defense, "Balanced", "Run focus", "Pass focus", "Blitz");
        Add("special", HeadCoachAuthorityService.SpecialTeamsDecisions, offense && (state.Phase != "scrimmage" || state.Down == 4), state.PendingDecision.SpecialTeams,
            state.Phase == "kickoff" ? new[] { "Deep kick", "Return kick" } : state.Phase == "try" ? new[] { "Extra point", "Two point" } : new[] { "Punt", "Field goal" });
        Add("fourth", HeadCoachAuthorityService.OverallGameManagement, offense && state.Phase == "scrimmage" && state.Down == 4, state.PendingDecision.FourthDown, "Go", "Kick");
        Add("tempo", HeadCoachAuthorityService.OverallGameManagement, offense && state.Phase == "scrimmage", state.PendingDecision.Tempo, "Normal", "Hurry", "Chew");
        return options;
    }

    public static bool Validate(LeagueState league, GameResult result, ProGameDecision decision, out string error)
    {
        foreach (var option in Options(league, result))
        {
            var selected = option.Key switch { "offense" => decision.Offense, "defense" => decision.Defense, "special" => decision.SpecialTeams, "fourth" => decision.FourthDown, _ => decision.Tempo };
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
        error = "";
        return true;
    }
}
