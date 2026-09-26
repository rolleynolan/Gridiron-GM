using System;
using System.Collections.Generic;
using System.Globalization;
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
            RecoveryProcessedThrough = league?.Calendar?.CurrentDate ?? "",
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

    public static void RecoverThroughCurrentDate(LeagueState league)
    {
        if (league == null || !TryDate(league.Calendar?.CurrentDate, out var today))
            return;

        var medicalRates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in league.Teams.OrderBy(t => t.TeamId, StringComparer.Ordinal))
        {
            var rate = GetDailyRecoveryRate(team);
            foreach (var player in team.Roster.Concat(team.InjuredReserve).Concat(team.PracticeSquad))
                medicalRates.TryAdd(player.PlayerId, rate);
        }
        foreach (var player in AllTeamPlayers(league))
        {
            var injury = player.CurrentInjury;
            if (!(injury?.IsActive ?? false))
                continue;
            if (!TryDate(injury.RecoveryProcessedThrough, out var prior))
            {
                // Unknown history is observed from today, never retroactively healed.
                injury.RecoveryProcessedThrough = FormatDate(today);
                continue;
            }
            if (today <= prior) continue;
            var days = (today - prior).Days;
            var rate = medicalRates.GetValueOrDefault(player.PlayerId, 1);
            var recoveryDays = (int)Math.Ceiling(injury.DaysRemaining / (double)rate);
            injury.DaysRemaining = (int)Math.Max(0L, injury.DaysRemaining - (long)days * rate);
            injury.RecoveryProcessedThrough = FormatDate(today);
            if (injury.DaysRemaining > 0)
                continue;

            injury.DaysRemaining = 0;
            player.Injury = "";
            if (string.Equals(player.Status, "Injured", StringComparison.OrdinalIgnoreCase))
                player.Status = "Active";
            var history = player.InjuryHistory?.LastOrDefault(record => string.IsNullOrWhiteSpace(record.RecoveredOn)
                && string.Equals(record.GameId, injury.GameId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(record.OccurredOn, injury.OccurredOn, StringComparison.Ordinal)
                && string.Equals(record.Name, injury.Name, StringComparison.Ordinal));
            if (history != null)
                history.RecoveredOn = FormatDate(prior.AddDays(recoveryDays));
        }
    }

    public static void NormalizeRecoveryPersistence(LeagueState league, bool legacy)
    {
        if (!TryDate(league.Calendar?.CurrentDate, out var today)) return;
        foreach (var player in AllTeamPlayers(league))
        {
            if (player.CurrentInjury?.IsActive != true) continue;
            if (legacy || !TryDate(player.CurrentInjury.RecoveryProcessedThrough, out _))
                player.CurrentInjury.RecoveryProcessedThrough = FormatDate(today);
        }
    }

    internal static bool TryDate(string text, out DateTime date)
        => DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    internal static string FormatDate(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static int GetDailyRecoveryRate(TeamState team)
        => team?.Coaches?.Any(c => string.Equals(c.Role, "Medical Director", StringComparison.OrdinalIgnoreCase) && c.Overall >= 85) == true ? 2 : 1;

    private static IEnumerable<PlayerState> AllTeamPlayers(LeagueState league)
        => league.Teams.Where(team => team != null)
            .SelectMany(team => (team.Roster ?? new List<PlayerState>())
                .Concat(team.InjuredReserve ?? new List<PlayerState>())
                .Concat(team.PracticeSquad ?? new List<PlayerState>()))
            .Concat(league.FreeAgents ?? new List<PlayerState>())
            .Concat((league.Waivers ?? new List<WaiverClaimState>()).Select(waiver => waiver.Player))
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
