using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Applies one small, deterministic development pass after a completed college season.
public static class CollegePlayerDevelopmentService
{
    public static void ApplyCompletedSeasonDevelopment(LeagueState league)
    {
        var universe = league?.CollegeUniverse;
        if (universe?.Schedule?.Count == 0 || universe.Schedule.Any(game => game == null || !string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)))
            return;

        foreach (var player in universe.Players?.Where(player => player != null).OrderBy(player => player.PlayerId, StringComparer.Ordinal) ?? Enumerable.Empty<CollegePlayerState>())
        {
            player.DevelopmentHistory ??= new List<CollegePlayerDevelopmentRecord>();
            if (player.DevelopmentHistory.Any(record => record != null && record.SeasonYear == universe.SeasonYear))
                continue;

            var before = Math.Clamp(player.Overall, 40, 99);
            player.Potential = Math.Clamp(Math.Max(player.Potential <= 0 ? before : player.Potential, before), 40, 99);
            var totalYards = player.PassingYards + player.RushingYards + player.ReceivingYards;
            var productive = player.Touchdowns >= 5 || totalYards >= 900;
            var baseGain = player.ClassYear <= 2 ? 2 : 1;
            var breakoutGain = productive && StableValue($"{universe.SeasonYear}-{player.PlayerId}") % 4 == 0 ? 1 : 0;
            var after = Math.Min(player.Potential, before + baseGain + breakoutGain);
            player.Overall = after;
            player.DevelopmentHistory.Add(new CollegePlayerDevelopmentRecord
            {
                SeasonYear = universe.SeasonYear,
                OverallBefore = before,
                OverallAfter = after,
                Reason = after == before ? "Reached current potential." : productive ? "Season development with productive-stat bonus." : "Season development.",
            });
        }
    }

    private static int StableValue(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in value ?? "") hash = (hash ^ character) * 16777619;
            return (int)(hash & 0x7fffffff);
        }
    }
}
