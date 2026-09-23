using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class PlayerInjuryService
{
    private static readonly string[] InjuryNames = { "Hamstring strain", "Ankle sprain", "Shoulder strain", "Knee sprain" };

    public static bool IsAvailableForGame(PlayerState player)
        => player != null
            && string.Equals(player.Status, "Active", StringComparison.OrdinalIgnoreCase)
            && !(player.CurrentInjury?.IsActive ?? false)
            && string.IsNullOrWhiteSpace(player.Injury);

    public static void ApplyDeterministicGameInjuries(LeagueState league, GameResult result)
    {
        if (league == null || result?.BoxScore?.PlayerStats == null || result.BoxScore.PlayerStats.Count == 0)
            return;

        var seed = StableValue(result.GameId);
        if (seed % 4 != 0)
            return;

        var line = result.BoxScore.PlayerStats
            .OrderBy(candidate => candidate.PlayerId, StringComparer.OrdinalIgnoreCase)
            .ElementAt(seed % result.BoxScore.PlayerStats.Count);
        var player = league.Teams.SelectMany(team => team?.Roster ?? new List<PlayerState>())
            .FirstOrDefault(candidate => string.Equals(candidate?.PlayerId, line.PlayerId, StringComparison.OrdinalIgnoreCase));
        if (player == null || !IsAvailableForGame(player))
            return;

        var daysOut = 3 + (seed % 12);
        InjurePlayer(league, player, InjuryNames[seed % InjuryNames.Length], daysOut, result.GameId);
    }

    public static void InjurePlayer(LeagueState league, PlayerState player, string injuryName, int daysOut, string gameId = "")
    {
        if (player == null || string.IsNullOrWhiteSpace(injuryName) || daysOut <= 0 || player.CurrentInjury?.IsActive == true)
            return;

        player.CurrentInjury = new PlayerInjuryState
        {
            Name = injuryName,
            DaysRemaining = daysOut,
            OccurredOn = league?.Calendar?.CurrentDate ?? "",
            GameId = gameId ?? "",
        };
        player.Injury = injuryName;
        if (string.Equals(player.Status, "Active", StringComparison.OrdinalIgnoreCase))
            player.Status = "Injured";
        player.InjuryHistory ??= new List<PlayerInjuryRecord>();
        player.InjuryHistory.Add(new PlayerInjuryRecord
        {
            SeasonYear = league?.SeasonYear ?? 0,
            Name = injuryName,
            DaysOut = daysOut,
            OccurredOn = player.CurrentInjury.OccurredOn,
            GameId = player.CurrentInjury.GameId,
        });
    }

    public static void RecoverOneDay(LeagueState league)
    {
        if (league == null)
            return;

        foreach (var player in AllTeamPlayers(league))
        {
            var injury = player.CurrentInjury;
            if (!(injury?.IsActive ?? false))
                continue;

            var team = league.Teams.FirstOrDefault(candidate => (candidate?.Roster ?? new List<PlayerState>()).Any(member => string.Equals(member.PlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase)) || (candidate?.InjuredReserve ?? new List<PlayerState>()).Any(member => string.Equals(member.PlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase)) || (candidate?.PracticeSquad ?? new List<PlayerState>()).Any(member => string.Equals(member.PlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase)));
            var medicalDirector = team?.Coaches?.FirstOrDefault(coach => string.Equals(coach.Role, "Medical Director", StringComparison.OrdinalIgnoreCase));
            injury.DaysRemaining -= medicalDirector?.Overall >= 85 ? 2 : 1;
            if (injury.DaysRemaining > 0)
                continue;

            injury.DaysRemaining = 0;
            player.Injury = "";
            if (string.Equals(player.Status, "Injured", StringComparison.OrdinalIgnoreCase))
                player.Status = "Active";
            var history = player.InjuryHistory?.LastOrDefault(record => string.IsNullOrWhiteSpace(record.RecoveredOn)
                && string.Equals(record.GameId, injury.GameId, StringComparison.OrdinalIgnoreCase));
            if (history != null)
                history.RecoveredOn = league.Calendar?.CurrentDate ?? "";
        }
    }

    public static void ClearForNewSeason(LeagueState league)
    {
        foreach (var player in AllTeamPlayers(league))
        {
            if (!(player.CurrentInjury?.IsActive ?? false))
                continue;

            player.CurrentInjury.DaysRemaining = 0;
            player.Injury = "";
            if (string.Equals(player.Status, "Injured", StringComparison.OrdinalIgnoreCase))
                player.Status = "Active";
            var history = player.InjuryHistory?.LastOrDefault(record => string.IsNullOrWhiteSpace(record.RecoveredOn)
                && string.Equals(record.GameId, player.CurrentInjury.GameId, StringComparison.OrdinalIgnoreCase));
            if (history != null)
                history.RecoveredOn = league.Calendar?.CurrentDate ?? "";
        }
    }

    private static IEnumerable<PlayerState> AllTeamPlayers(LeagueState league)
        => league.Teams.Where(team => team != null)
            .SelectMany(team => (team.Roster ?? new List<PlayerState>())
                .Concat(team.InjuredReserve ?? new List<PlayerState>())
                .Concat(team.PracticeSquad ?? new List<PlayerState>()))
            .Where(player => player != null)
            .GroupBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());

    private static int StableValue(string value)
    {
        var hash = 17;
        foreach (var character in value ?? "")
            hash = unchecked((hash * 31) + character);
        return hash == int.MinValue ? 0 : Math.Abs(hash);
    }
}
