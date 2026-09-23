using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class HeadCoachAuthorityService
{
    public const string OffensivePlayCalling = "offensive_play_calling";
    public const string DefensivePlayCalling = "defensive_play_calling";
    public const string SpecialTeamsDecisions = "special_teams_decisions";
    public const string OverallGameManagement = "overall_game_management";
    public const string OffensiveScheme = "offensive_scheme";
    public const string DefensiveScheme = "defensive_scheme";
    public const string OffensivePlaybook = "offensive_playbook";
    public const string DefensivePlaybook = "defensive_playbook";
    public const string WeeklyGamePlanning = "weekly_game_planning";
    public const string PracticeAndTraining = "practice_and_training";
    public const string LineupAndDepthChart = "lineup_and_depth_chart";
    public const string CoordinatorStaffing = "coordinator_staffing";

    public static readonly IReadOnlyList<string> Domains = new[]
    {
        OffensivePlayCalling,
        DefensivePlayCalling,
        SpecialTeamsDecisions,
        OverallGameManagement,
        OffensiveScheme,
        DefensiveScheme,
        OffensivePlaybook,
        DefensivePlaybook,
        WeeklyGamePlanning,
        PracticeAndTraining,
        LineupAndDepthChart,
        CoordinatorStaffing,
    };

    private static readonly HashSet<string> DomainSet = new(Domains, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [OffensivePlayCalling] = "Offensive play calling",
        [DefensivePlayCalling] = "Defensive play calling",
        [SpecialTeamsDecisions] = "Special-teams decisions",
        [OverallGameManagement] = "Overall game management",
        [OffensiveScheme] = "Offensive scheme",
        [DefensiveScheme] = "Defensive scheme",
        [OffensivePlaybook] = "Offensive playbook",
        [DefensivePlaybook] = "Defensive playbook",
        [WeeklyGamePlanning] = "Weekly game planning",
        [PracticeAndTraining] = "Practice and training",
        [LineupAndDepthChart] = "Lineup and depth chart",
        [CoordinatorStaffing] = "Coordinator selection and dismissal",
    };

    public static bool TryBuildAgreement(CoachState coach, string targetRole, IEnumerable<string> controlledDomains, out HeadCoachAuthorityState agreement, out string error)
    {
        agreement = new HeadCoachAuthorityState();
        if (coach == null || !string.Equals(targetRole, "Head Coach", StringComparison.OrdinalIgnoreCase))
        {
            error = "Only a Head Coach can negotiate control authority.";
            return false;
        }

        var requested = (controlledDomains ?? Array.Empty<string>())
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .Select(domain => domain.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var unknown = requested.FirstOrDefault(domain => !DomainSet.Contains(domain));
        if (!string.IsNullOrWhiteSpace(unknown))
        {
            error = $"Unknown Head Coach authority domain: {unknown}.";
            return false;
        }

        agreement.ControlledDomains = Domains.Where(domain => requested.Contains(domain, StringComparer.OrdinalIgnoreCase)).ToList();
        error = "";
        return true;
    }

    public static bool IsHeadCoachControlled(TeamState team, string domain)
    {
        if (team == null || string.IsNullOrWhiteSpace(domain) || !DomainSet.Contains(domain))
            return false;
        var headCoach = team.Coaches?.FirstOrDefault(coach => string.Equals(coach?.Role, "Head Coach", StringComparison.OrdinalIgnoreCase));
        return headCoach?.Authority?.ControlledDomains?.Contains(domain, StringComparer.OrdinalIgnoreCase) == true;
    }

    public static string GetDisplayName(string domain)
        => !string.IsNullOrWhiteSpace(domain) && DisplayNames.TryGetValue(domain, out var displayName)
            ? displayName
            : domain ?? string.Empty;

    public static void Normalize(CoachState coach)
    {
        if (coach == null)
            return;
        coach.Authority ??= new HeadCoachAuthorityState();
        if (!string.Equals(coach.Role, "Head Coach", StringComparison.OrdinalIgnoreCase))
        {
            coach.Authority.ControlledDomains = new List<string>();
            return;
        }

        coach.Authority.ControlledDomains = Domains
            .Where(domain => coach.Authority.ControlledDomains?.Contains(domain, StringComparer.OrdinalIgnoreCase) == true)
            .ToList();
    }
}
