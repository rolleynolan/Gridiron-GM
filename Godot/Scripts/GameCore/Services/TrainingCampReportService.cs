using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public static class TrainingCampReportService
{
    public static TrainingCampReportState Build(TeamState team)
    {
        var rows = (team?.Roster ?? new List<PlayerState>())
            .Where(player => player != null)
            .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var players = group.ToList();
                var available = players.Where(PlayerInjuryService.IsAvailableForGame).ToList();
                var required = DepthChartRules.GetRequiredStarters(group.Key);
                var averageOverall = available.Count == 0 ? 0 : (int)Math.Round(available.Average(player => player.Overall));
                var averagePotential = available.Count == 0 ? 0 : (int)Math.Round(available.Average(player => player.Potential));
                var averageFatigue = available.Count == 0 ? 0 : (int)Math.Round(available.Average(player => player.Fatigue));
                var shortage = Math.Max(0, required - available.Count);
                var upside = Math.Max(0, averagePotential - averageOverall);
                var needScore = (shortage * 100) + (players.Count - available.Count) * 15 + upside * 3 + averageFatigue;
                return new { NeedScore = needScore, Report = new TrainingCampPositionReport
                {
                    Position = group.Key,
                    RequiredStarters = required,
                    AvailablePlayers = available.Count,
                    UnavailablePlayers = players.Count - available.Count,
                    AverageOverall = averageOverall,
                    AveragePotential = averagePotential,
                    AverageFatigue = averageFatigue,
                    Recommendation = shortage > 0
                        ? $"Short {shortage} available starter(s); prioritize depth before finalizing."
                        : averageFatigue >= 30
                            ? "High fatigue group; use camp reps to improve readiness."
                            : upside >= 4
                                ? "Development upside is available in this group."
                                : "Roster group is stable for camp.",
                }};
            })
            .OrderByDescending(entry => entry.NeedScore)
            .ThenBy(entry => entry.Report.Position, StringComparer.OrdinalIgnoreCase)
            .Select(entry => entry.Report)
            .ToList();
        var recommended = rows.FirstOrDefault()?.Position ?? "";
        return new TrainingCampReportState
        {
            RecommendedFocusPosition = recommended,
            Summary = string.IsNullOrWhiteSpace(recommended)
                ? "No active-roster position groups are available for evaluation."
                : $"{recommended} is the recommended camp focus based on availability, depth, potential, and fatigue.",
            Positions = rows,
        };
    }
}
