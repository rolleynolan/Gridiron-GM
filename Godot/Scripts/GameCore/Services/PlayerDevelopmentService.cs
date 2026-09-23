using System;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class PlayerDevelopmentService
{
    public static void ApplyAnnualDevelopment(PlayerState player, int staffDevelopmentBonus = 0, int seasonYear = 0)
    {
        if (player == null)
            return;

        player.Potential = ResolvePotential(player);
        var before = player.Overall;
        var bonus = Math.Clamp(staffDevelopmentBonus, 0, 1);
        if (player.Age <= 23 && player.Overall < player.Potential)
            player.Overall = Math.Min(player.Potential, player.Overall + 2 + bonus);
        else if (player.Age <= 27 && player.Overall < player.Potential)
            player.Overall = Math.Min(player.Potential, player.Overall + 1 + bonus);
        else if (player.Age >= 35)
            player.Overall = Math.Max(40, player.Overall - 2);
        else if (player.Age >= 31)
            player.Overall = Math.Max(40, player.Overall - 1);
        if (seasonYear > 0)
        {
            player.DevelopmentHistory ??= new System.Collections.Generic.List<PlayerDevelopmentRecord>();
            player.DevelopmentHistory.RemoveAll(record => record != null && record.SeasonYear == seasonYear);
            player.DevelopmentHistory.Add(new PlayerDevelopmentRecord { SeasonYear = seasonYear, OverallBefore = before, OverallAfter = player.Overall, Note = staffDevelopmentBonus > 0 ? "Head Coach development support applied; potential remains the cap." : "Annual development and aging assessment." });
        }
    }

    public static int ResolvePotential(PlayerState player)
    {
        if (player?.Potential > 0)
            return Math.Clamp(player.Potential, 40, 99);

        var ageUpside = Math.Max(0, 28 - Math.Max(0, player?.Age ?? 0));
        return Math.Clamp((player?.Overall ?? 50) + Math.Max(2, ageUpside / 2), 40, 99);
    }
}
