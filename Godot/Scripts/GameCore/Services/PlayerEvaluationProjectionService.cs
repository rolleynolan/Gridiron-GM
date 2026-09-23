using System;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Produces deterministic staff-facing estimates without exposing simulation ratings.
// The current model uses the saved GM scouting attribute as the staff-knowledge proxy;
// a later persistent scouting system can replace this projection without changing UI DTOs.
public static class PlayerEvaluationProjectionService
{
    public static PlayerEvaluationProjection Evaluate(LeagueState league, PlayerState player)
    {
        if (player == null)
            return new PlayerEvaluationProjection();

        var judgment = Math.Clamp(league?.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.ScoutingJudgment ?? 50, 20, 80);
        var spread = judgment >= 70 ? 2 : judgment >= 55 ? 4 : judgment >= 40 ? 6 : 8;
        var season = league?.SeasonYear ?? 0;
        var overall = ClampRating(player.Overall + StableOffset($"{player.PlayerId}|{season}|overall", spread));
        var potential = ClampRating(player.Potential + StableOffset($"{player.PlayerId}|{season}|potential", spread + 1));
        var confidence = judgment >= 65 ? "High" : judgment >= 45 ? "Medium" : "Low";
        return new PlayerEvaluationProjection
        {
            EstimatedOverall = overall,
            EstimatedPotential = potential,
            Confidence = confidence,
            Range = spread,
        };
    }

    private static int StableOffset(string value, int spread)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in value ?? "")
            {
                hash ^= character;
                hash *= 16777619;
            }
            return (int)(hash % (uint)(spread * 2 + 1)) - spread;
        }
    }

    private static int ClampRating(int value) => Math.Clamp(value, 1, 100);
}

public sealed class PlayerEvaluationProjection
{
    public int EstimatedOverall { get; set; }
    public int EstimatedPotential { get; set; }
    public string Confidence { get; set; } = "Low";
    public int Range { get; set; }
    public string OverallRange => FormatRange(EstimatedOverall, Range);
    public string PotentialRange => FormatRange(EstimatedPotential, Range + 1);

    private static string FormatRange(int estimate, int spread)
        => $"{Math.Clamp(estimate - spread, 1, 100)}-{Math.Clamp(estimate + spread, 1, 100)}";
}
