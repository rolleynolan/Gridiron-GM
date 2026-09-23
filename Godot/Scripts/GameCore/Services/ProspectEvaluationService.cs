using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Generates durable scouting observations without exposing a prospect's hidden ratings as fact.
public sealed class ProspectEvaluationService
{
    private static readonly string[] Traits = { "Competitive finisher", "Quick learner", "Physical profile", "Assignment discipline", "High motor", "Developmental upside" };
    private static readonly string[] Interviews = { "Prepared and direct in interviews.", "Confident communicator with clear goals.", "Thoughtful about scheme fit and development.", "Reserved but well prepared in interviews.", "Team-oriented answers and steady presence.", "Competitive interview with a strong work ethic." };

    private readonly GameCoreContext _context;

    public ProspectEvaluationService(GameCoreContext context) => _context = context;

    public ProspectEvaluationDto GetEvaluation(string prospectId)
    {
        var league = _context.ActiveLeague;
        var prospect = league?.CollegeProspects?.FirstOrDefault(candidate => string.Equals(candidate?.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase));
        if (prospect == null)
            return null;

        EnsureEvaluations(league, new[] { prospect });
        var staffModifier = GetPersonnelScoutingModifier(league);
        var effectiveConfidence = Math.Clamp(prospect.ScoutingConfidence + staffModifier, 1, 100);
        var spread = GetEstimateSpread(effectiveConfidence);
        return new ProspectEvaluationDto
        {
            ProspectId = prospect.ProspectId,
            KnownFacts = $"{prospect.Position} | Age {prospect.Age} | {prospect.College}\nPublic combine: {prospect.CombineScore}/100 | Public pro day: {prospect.ProDayScore}/100\nDraft outlook: {prospect.DraftStock} | {prospect.DeclarationStatus}\n{prospect.DeclarationRationale}",
            EstimatedOverall = FormatRange(prospect.ScoutedOverall, spread),
            EstimatedPotential = FormatRange(prospect.ScoutedPotential, spread + 1),
            Confidence = $"{GetConfidenceLabel(effectiveConfidence)} ({effectiveConfidence}/100; personnel staff {staffModifier:+#;-#;0})",
            Report = prospect.ScoutingReport,
            Trait = prospect.Trait,
            Interview = prospect.InterviewSummary,
        };
    }

    public static void EnsureEvaluations(LeagueState league, IEnumerable<CollegeProspectState> prospects = null)
    {
        if (league == null)
            return;

        var scouting = league.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.ScoutingJudgment ?? 50;
        foreach (var prospect in prospects ?? league.CollegeProspects ?? Enumerable.Empty<CollegeProspectState>())
        {
            if (prospect == null)
                continue;

            var seed = StableValue(prospect.ProspectId, 17);
            var error = Math.Max(1, 12 - ((scouting - 20) / 6));
            prospect.ScoutedOverall = prospect.ScoutedOverall > 0
                ? prospect.ScoutedOverall
                : Math.Clamp(prospect.Overall + ((seed % (error * 2 + 1)) - error), 40, 99);
            prospect.ScoutedPotential = prospect.ScoutedPotential > 0
                ? Math.Max(prospect.ScoutedPotential, prospect.ScoutedOverall)
                : Math.Clamp(prospect.Potential + (((seed / 7) % (error * 2 + 1)) - error), prospect.ScoutedOverall, 99);
            prospect.ScoutingConfidence = prospect.ScoutingConfidence > 0
                ? Math.Clamp(prospect.ScoutingConfidence, 1, 100)
                : Math.Clamp(42 + scouting / 2 + (seed % 19) - 9, 25, 95);
            prospect.CombineScore = prospect.CombineScore > 0 ? Math.Clamp(prospect.CombineScore, 1, 100) : 55 + (seed % 41);
            prospect.ProDayScore = prospect.ProDayScore > 0 ? Math.Clamp(prospect.ProDayScore, 1, 100) : 55 + ((seed / 23) % 41);
            prospect.Trait = string.IsNullOrWhiteSpace(prospect.Trait) ? Traits[(seed / 43) % Traits.Length] : prospect.Trait;
            prospect.InterviewSummary = string.IsNullOrWhiteSpace(prospect.InterviewSummary) ? Interviews[(seed / 71) % Interviews.Length] : prospect.InterviewSummary;
            if (string.IsNullOrWhiteSpace(prospect.ScoutingReport))
            {
                var spread = GetEstimateSpread(prospect.ScoutingConfidence);
                prospect.ScoutingReport = $"Estimated OVR {FormatRange(prospect.ScoutedOverall, spread)}; estimated potential {FormatRange(prospect.ScoutedPotential, spread + 1)}. {GetConfidenceLabel(prospect.ScoutingConfidence)} confidence.";
            }
        }
    }

    private static int StableValue(string value, int salt)
    {
        unchecked
        {
            uint hash = (uint)(17 + salt);
            foreach (var character in value ?? "")
                hash = hash * 31 + character;
            return (int)(hash % 10_000);
        }
    }

    private static int GetEstimateSpread(int confidence)
        => confidence >= 80 ? 3 : confidence >= 60 ? 5 : 8;

    private static string FormatRange(int estimate, int spread)
        => $"{Math.Clamp(estimate - spread, 40, 99)}-{Math.Clamp(estimate + spread, 40, 99)}";

    private static string GetConfidenceLabel(int confidence)
        => confidence >= 80 ? "High" : confidence >= 60 ? "Medium" : "Low";

    private static int GetPersonnelScoutingModifier(LeagueState league)
    {
        var team = league?.Teams?.FirstOrDefault(item => string.Equals(item.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var personnelDirector = team?.Coaches?.FirstOrDefault(coach => string.Equals(coach.Role, "Director of Player Personnel", StringComparison.OrdinalIgnoreCase));
        // Cap the visible clarity benefit; this changes only presentation confidence, never stored estimates or hidden ratings.
        return personnelDirector == null ? 0 : Math.Clamp((personnelDirector.Overall - 65) / 4, -3, 6);
    }
}
