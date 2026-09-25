using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class RetirementService
{
    public sealed class RetirementGenerationResult
    {
        public bool Generated { get; set; }
        public bool Skipped { get; set; }
        public int RetiredCount { get; set; }
        public string Reason { get; set; } = "";
        public SeasonRetirementRecord SeasonRecord { get; set; }
    }

    public RetirementGenerationResult GenerateRetirementsForCurrentSeason(LeagueState league)
    {
        if (league == null)
        {
            return new RetirementGenerationResult
            {
                Skipped = true,
                Reason = "No active league loaded.",
            };
        }

        league.RetirementHistory ??= new List<SeasonRetirementRecord>();

        var existing = GetSeasonRetirementRecord(league, league.SeasonYear);
        if (existing != null && existing.Completed)
        {
            TransactionService.ApplyRetirements(league, existing, recordTransactions: false);
            return new RetirementGenerationResult
            {
                Skipped = true,
                RetiredCount = existing.RetiredCount,
                Reason = "Retirements already generated for this season.",
                SeasonRecord = existing,
            };
        }

        var seasonRecord = existing ?? new SeasonRetirementRecord
        {
            SeasonYear = league.SeasonYear,
            ProcessedPhase = ScheduleService.RetirementPendingPhaseKey,
            Completed = false,
        };

        seasonRecord.ProcessedPhase = ScheduleService.RetirementPendingPhaseKey;
        seasonRecord.Players ??= new List<PlayerRetirementRecord>();
        seasonRecord.Players.Clear();

        foreach (var team in league.Teams.Where(team => team != null).OrderBy(team => team.TeamId, StringComparer.Ordinal))
        {
            team.Roster ??= new List<PlayerState>();

            var remainingCountsByPosition = team.Roster
                .Where(player => player != null)
                .GroupBy(player => player.Position ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var player in team.Roster
                         .Where(player => player != null)
                         .OrderBy(player => player.PlayerId, StringComparer.Ordinal))
            {
                if (!ShouldRetirePlayer(player, team, league.SeasonYear))
                    continue;

                var position = player.Position ?? "";
                var requiredStarters = DepthChartRules.GetRequiredStarters(position);
                var remainingAtPosition = remainingCountsByPosition.TryGetValue(position, out var currentCount)
                    ? currentCount
                    : 0;
                if (remainingAtPosition - 1 < requiredStarters)
                    continue;

                remainingCountsByPosition[position] = remainingAtPosition - 1;
                seasonRecord.Players.Add(Snapshot(league, player, team, BuildReasonLabel(player)));
            }
        }

        // Only unrestricted, uniquely owned free agents enter this assessment. Pending
        // waivers and contracted reserve players retain their existing rights and rules.
        var ownership = league.Teams.SelectMany(t => t.Roster.Concat(t.InjuredReserve).Concat(t.PracticeSquad))
            .Concat(league.FreeAgents).Concat(league.Waivers.Select(w => w.Player))
            .Where(p => p != null).GroupBy(p => p.PlayerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        foreach (var player in league.FreeAgents.Where(p => p != null && !string.IsNullOrWhiteSpace(p.PlayerId))
                     .OrderBy(p => p.PlayerId, StringComparer.Ordinal))
        {
            if (ownership[player.PlayerId] != 1 || player.Contract?.YearsRemaining > 0) continue;
            if (player.UnsignedSinceSeasonYear <= 0 || player.UnsignedSinceSeasonYear > league.SeasonYear)
                player.UnsignedSinceSeasonYear = league.SeasonYear;
            var yearsUnsigned = league.SeasonYear - player.UnsignedSinceSeasonYear;
            var chance = UnsignedRetirementChance(player, yearsUnsigned);
            if (!string.Equals(player.Status, "Retired", StringComparison.OrdinalIgnoreCase)
                && GetDeterministicRoll(league.SeasonYear, player.PlayerId) >= chance) continue;
            seasonRecord.Players.Add(Snapshot(league, player, null,
                yearsUnsigned >= 2 && player.Age < 35 ? "extended_free_agency" : BuildReasonLabel(player)));
        }

        TransactionService.ApplyRetirements(league, seasonRecord, recordTransactions: true);
        seasonRecord.RetiredCount = seasonRecord.Players.Count(record => record != null);
        seasonRecord.Completed = true;

        if (existing == null)
            league.RetirementHistory.Add(seasonRecord);

        return new RetirementGenerationResult
        {
            Generated = true,
            RetiredCount = seasonRecord.RetiredCount,
            SeasonRecord = seasonRecord,
        };
    }

    public static SeasonRetirementRecord GetSeasonRetirementRecord(LeagueState league, int seasonYear)
    {
        return (league?.RetirementHistory ?? new List<SeasonRetirementRecord>())
            .Where(record => record != null && record.SeasonYear == seasonYear)
            .OrderByDescending(record => record.Completed)
            .ThenByDescending(record => record.RetiredCount)
            .FirstOrDefault();
    }

    public static void NormalizePersistence(LeagueState league, bool legacy)
    {
        // Loading never retires anyone or reconstructs an unknown unsigned career.
        foreach (var player in league.FreeAgents.Where(p => p != null))
        {
            if (legacy || player.UnsignedSinceSeasonYear < 0 || player.UnsignedSinceSeasonYear > league.SeasonYear)
                player.UnsignedSinceSeasonYear = league.SeasonYear;
        }
        foreach (var player in league.Teams.SelectMany(t => t.Roster.Concat(t.InjuredReserve).Concat(t.PracticeSquad))
                     .Concat(league.Waivers.Select(w => w.Player)).Where(p => p != null))
            player.UnsignedSinceSeasonYear = 0;
    }

    private static PlayerRetirementRecord Snapshot(LeagueState league, PlayerState player, TeamState team, string reason)
    {
        var snapshot = JsonSerializer.SerializeToElement(player).Deserialize<PlayerState>();
        snapshot.Status = "Retired";
        return new PlayerRetirementRecord
        {
            SeasonYear = league.SeasonYear, PlayerId = player.PlayerId, PlayerName = player.Name,
            TeamId = team?.TeamId ?? "", TeamName = team?.Name ?? "Free agent", Position = player.Position,
            Age = player.Age, Overall = player.Overall, ReasonLabel = reason,
            RetiredDuringPhase = ScheduleService.RetirementPendingPhaseKey, PlayerSnapshot = snapshot,
            CurrentSeasonStats = snapshot.SeasonStats?.Copy() ?? new PlayerSeasonStats(),
            CareerStats = (snapshot.CareerStats ?? new List<PlayerSeasonStats>()).Where(s => s != null).Select(s => s.Copy()).ToList(),
        };
    }

    private static double UnsignedRetirementChance(PlayerState player, int yearsUnsigned)
    {
        // Two observed full cycles protect new entrants. Bounded future value gives
        // developing players a longer opportunity; no fixed-size market cull is used.
        var ageChance = player.Age >= 30 ? GetRetirementChance(player, null) + .03d : 0d;
        if (yearsUnsigned < 2) return Math.Clamp(ageChance, 0d, .95d);
        var value = FrontOfficeEvaluationService.PlayerValue(player) / 4d;
        var opportunityFactor = value >= 80 ? .25d : value >= 70 ? .5d : 1d;
        var marketChance = Math.Min(.85d, .20d * (yearsUnsigned - 1)) * opportunityFactor;
        return Math.Clamp(ageChance + marketChance, 0d, .95d);
    }

    private static bool ShouldRetirePlayer(PlayerState player, TeamState team, int seasonYear)
    {
        if (player == null)
            return false;

        if (string.Equals((player.Status ?? "").Trim(), "Retired", StringComparison.OrdinalIgnoreCase))
            return true;

        var chance = GetRetirementChance(player, team);
        if (chance <= 0d)
            return false;

        return GetDeterministicRoll(seasonYear, player.PlayerId) < chance;
    }

    private static double GetRetirementChance(PlayerState player, TeamState team)
    {
        var age = Math.Max(0, player?.Age ?? 0);
        var chance = age switch
        {
            < 30 => 0d,
            30 => 0.005d,
            31 => 0.0125d,
            32 => 0.02d,
            33 => 0.0325d,
            34 => 0.05d,
            35 => 0.085d,
            36 => 0.145d,
            37 => 0.23d,
            38 => 0.35d,
            39 => 0.5d,
            40 => 0.66d,
            _ => 0.8d,
        };

        var overall = player?.Overall ?? 0;
        if (age >= 30)
        {
            chance += overall switch
            {
                <= 64 => 0.06d,
                <= 69 => 0.03d,
                <= 74 => 0.015d,
                >= 82 => -0.03d,
                >= 78 => -0.015d,
                _ => 0d,
            };
        }

        chance += NormalizePositionModifier(player?.Position);

        var injury = (player?.Injury ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(injury))
        {
            chance += injury.Contains("career", StringComparison.OrdinalIgnoreCase)
                || injury.Contains("neck", StringComparison.OrdinalIgnoreCase)
                || injury.Contains("spine", StringComparison.OrdinalIgnoreCase)
                || injury.Contains("achilles", StringComparison.OrdinalIgnoreCase)
                ? 0.45d
                : 0.05d;
        }

        return Math.Clamp(chance, 0d, 0.95d);
    }

    private static double NormalizePositionModifier(string position)
    {
        return (position ?? "").Trim().ToUpperInvariant() switch
        {
            "QB" => -0.01d,
            "K" => -0.025d,
            "P" => -0.02d,
            "RB" => 0.02d,
            "WR" => 0.01d,
            "CB" => 0.01d,
            "EDGE" => 0.01d,
            "DT" => 0.01d,
            "LB" => 0.01d,
            _ => 0d,
        };
    }

    private static string BuildReasonLabel(PlayerState player)
    {
        var injury = (player?.Injury ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(injury)
            && (injury.Contains("career", StringComparison.OrdinalIgnoreCase)
                || injury.Contains("neck", StringComparison.OrdinalIgnoreCase)
                || injury.Contains("spine", StringComparison.OrdinalIgnoreCase)
                || injury.Contains("achilles", StringComparison.OrdinalIgnoreCase)))
        {
            return "career_ending_injury";
        }

        var age = Math.Max(0, player?.Age ?? 0);
        return age switch
        {
            >= 38 => "late_career_decline",
            >= 35 => "veteran_retirement",
            >= 30 => "age_and_role_outlook",
            _ => "manual_or_special_case",
        };
    }

    private static double GetDeterministicRoll(int seasonYear, string playerId)
    {
        unchecked
        {
            var hash = seasonYear;
            foreach (var character in playerId ?? "")
                hash = (hash * 31) + character;

            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;

            var normalized = (uint)hash % 1000000u;
            return normalized / 1000000d;
        }
    }
}
