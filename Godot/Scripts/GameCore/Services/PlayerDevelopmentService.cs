using System;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class PlayerDevelopmentService
{
    public static void ApplyAnnualDevelopment(PlayerState player)
    {
        if (player == null)
            return;

        player.Potential = ResolvePotential(player);
        if (player.Age <= 23 && player.Overall < player.Potential)
            player.Overall = Math.Min(player.Potential, player.Overall + 2);
        else if (player.Age <= 27 && player.Overall < player.Potential)
            player.Overall++;
        else if (player.Age >= 35)
            player.Overall = Math.Max(40, player.Overall - 2);
        else if (player.Age >= 31)
            player.Overall = Math.Max(40, player.Overall - 1);
    }

    public static int ResolvePotential(PlayerState player)
    {
        if (player?.Potential > 0)
            return Math.Clamp(player.Potential, 40, 99);

        var ageUpside = Math.Max(0, 28 - Math.Max(0, player?.Age ?? 0));
        return Math.Clamp((player?.Overall ?? 50) + Math.Max(2, ageUpside / 2), 40, 99);
    }
}
