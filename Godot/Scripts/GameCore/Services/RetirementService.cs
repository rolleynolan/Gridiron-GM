using System;
using System.Collections.Generic;
using System.Linq;
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
            ApplyRetirementRecordsToLeague(league, existing);
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

        var retiresByTeam = new Dictionary<string, List<PlayerState>>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in league.Teams.Where(team => team != null))
        {
            team.Roster ??= new List<PlayerState>();

            var remainingCountsByPosition = team.Roster
                .Where(player => player != null)
                .GroupBy(player => player.Position ?? "", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            foreach (var player in team.Roster
                         .Where(player => player != null)
                         .OrderBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase))
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

                if (!retiresByTeam.TryGetValue(team.TeamId ?? "", out var retiredPlayers))
                {
                    retiredPlayers = new List<PlayerState>();
                    retiresByTeam[team.TeamId ?? ""] = retiredPlayers;
                }

                retiredPlayers.Add(player);
                remainingCountsByPosition[position] = remainingAtPosition - 1;
                seasonRecord.Players.Add(new PlayerRetirementRecord
                {
                    SeasonYear = league.SeasonYear,
                    PlayerId = player.PlayerId ?? "",
                    PlayerName = player.Name ?? "",
                    TeamId = team.TeamId ?? "",
                    TeamName = team.Name ?? "",
                    Position = position,
                    Age = player.Age,
                    Overall = player.Overall,
                    ReasonLabel = BuildReasonLabel(player),
                    RetiredDuringPhase = ScheduleService.RetirementPendingPhaseKey,
                });
            }
        }

        foreach (var entry in retiresByTeam)
        {
            var team = league.Teams.FirstOrDefault(candidate =>
                string.Equals(candidate?.TeamId, entry.Key, StringComparison.OrdinalIgnoreCase));
            if (team == null)
                continue;

            var retiredIds = entry.Value
                .Where(player => player != null)
                .Select(player => player.PlayerId ?? "")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            team.Roster = team.Roster
                .Where(player => player != null && !retiredIds.Contains(player.PlayerId ?? ""))
                .ToList();
            RebuildDepthChart(team);
        }

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

    private static void ApplyRetirementRecordsToLeague(LeagueState league, SeasonRetirementRecord seasonRecord)
    {
        if (league == null || seasonRecord == null)
            return;

        var retiredIds = (seasonRecord.Players ?? new List<PlayerRetirementRecord>())
            .Where(record => record != null && !string.IsNullOrWhiteSpace(record.PlayerId))
            .Select(record => record.PlayerId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var team in league.Teams.Where(team => team != null))
        {
            team.Roster ??= new List<PlayerState>();
            if (retiredIds.Count > 0)
            {
                team.Roster = team.Roster
                    .Where(player => player != null && !retiredIds.Contains(player.PlayerId ?? ""))
                    .ToList();
            }

            RebuildDepthChart(team);
        }
    }

    private static void RebuildDepthChart(TeamState team)
    {
        team.DepthChart ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        team.DepthChart.Clear();
        foreach (var position in team.Roster
                     .Where(player => player != null)
                     .Select(player => player.Position ?? "")
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(position => FootballPositionOrder.GetSortOrder(position))
                     .ThenBy(position => position, StringComparer.OrdinalIgnoreCase))
        {
            team.DepthChart[position] = team.Roster
                .Where(player => player != null && string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(player => player.Overall)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => player.PlayerId ?? "")
                .ToList();
        }
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

        if (string.Equals((player?.Status ?? "").Trim(), "Free Agent", StringComparison.OrdinalIgnoreCase))
            chance += 0.03d;

        if (string.Equals((team?.Abbreviation ?? "").Trim(), "FA", StringComparison.OrdinalIgnoreCase))
            chance += 0.03d;

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
