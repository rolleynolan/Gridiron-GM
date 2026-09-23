using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// College injuries are deterministic and remain isolated from pro roster availability and recovery rules.
public static class CollegePlayerInjuryService
{
    private static readonly string[] InjuryNames = { "Hamstring strain", "Ankle sprain", "Shoulder strain", "Knee sprain" };

    public static bool IsAvailableForGame(CollegePlayerState player)
        => player != null && !player.IsRedshirted && !(player.CurrentInjury?.IsActive ?? false);

    public static void ApplyDeterministicGameInjury(CollegeUniverseState universe, CollegeScheduledGame game)
    {
        if (universe == null || game == null)
            return;

        var seed = StableValue(game.GameId);
        if (seed % 4 != 0)
            return;

        var candidates = (universe.Players ?? new List<CollegePlayerState>())
            .Where(player => player != null && (string.Equals(player.TeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase) || string.Equals(player.TeamId, game.AwayTeamId, StringComparison.OrdinalIgnoreCase)) && IsAvailableForGame(player))
            .OrderBy(player => player.PlayerId, StringComparer.Ordinal)
            .ToList();
        if (candidates.Count == 0)
            return;

        var player = candidates[seed % candidates.Count];
        var weeksOut = 1 + seed % 3;
        player.CurrentInjury = new CollegePlayerInjuryState { Name = InjuryNames[seed % InjuryNames.Length], WeeksRemaining = weeksOut, OccurredInWeek = game.ProAbsoluteWeek, GameId = game.GameId };
        player.InjuryHistory ??= new List<CollegePlayerInjuryRecord>();
        player.InjuryHistory.Add(new CollegePlayerInjuryRecord { SeasonYear = universe.SeasonYear, Name = player.CurrentInjury.Name, WeeksOut = weeksOut, OccurredInWeek = game.ProAbsoluteWeek, GameId = game.GameId });
    }

    public static void RecoverOneWeek(CollegeUniverseState universe, int enteringWeek)
    {
        if (universe == null)
            return;

        foreach (var player in (universe.Players ?? new List<CollegePlayerState>()).Where(player => player?.CurrentInjury?.IsActive == true))
        {
            player.CurrentInjury.WeeksRemaining--;
            if (player.CurrentInjury.WeeksRemaining > 0)
                continue;

            player.CurrentInjury.WeeksRemaining = 0;
            var record = player.InjuryHistory?.LastOrDefault(item => item != null && item.RecoveredInWeek == 0 && string.Equals(item.GameId, player.CurrentInjury.GameId, StringComparison.OrdinalIgnoreCase));
            if (record != null)
                record.RecoveredInWeek = enteringWeek;
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
