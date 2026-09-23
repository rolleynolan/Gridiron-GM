using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class PlayerStatisticsService
{
    public static void ApplyGameFatigue(LeagueState league, GameResult result)
    {
        if (league == null || result?.BoxScore?.PlayerStats == null)
            return;

        var players = league.Teams
            .Where(team => team != null)
            .SelectMany(team => team.Roster ?? new List<PlayerState>())
            .Where(player => player != null && !string.IsNullOrWhiteSpace(player.PlayerId))
            .GroupBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var playerId in result.BoxScore.PlayerStats
                     .Where(line => line != null && !string.IsNullOrWhiteSpace(line.PlayerId))
                     .Select(line => line.PlayerId)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (players.TryGetValue(playerId, out var player))
                player.Fatigue = Math.Clamp(player.Fatigue + 15, 0, 100);
        }
    }

    public static void RecoverOneDay(LeagueState league)
    {
        if (league == null)
            return;

        foreach (var team in league.Teams.Where(team => team != null))
        {
            var conditioningBonus = team.Coaches?.Any(coach => coach != null
                && string.Equals(coach.Role, "Strength & Conditioning Coach", StringComparison.OrdinalIgnoreCase)
                && coach.Overall >= 85) == true ? 1 : 0;
            foreach (var player in (team.Roster ?? new List<PlayerState>())
                         .Concat(team.InjuredReserve ?? new List<PlayerState>())
                         .Concat(team.PracticeSquad ?? new List<PlayerState>())
                         .Where(player => player != null)
                         .GroupBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
                         .Select(group => group.First()))
                player.Fatigue = Math.Max(0, Math.Clamp(player.Fatigue, 0, 100) - 4 - conditioningBonus);
        }
    }

    public static void ApplyRegularSeasonStats(LeagueState league, GameResult result)
    {
        if (league == null || result?.BoxScore?.PlayerStats == null
            || !string.Equals(result.GameType, "regular_season", StringComparison.OrdinalIgnoreCase))
            return;

        var players = league.Teams
            .Where(team => team != null)
            .SelectMany(team => team.Roster ?? new List<PlayerState>())
            .Where(player => player != null && !string.IsNullOrWhiteSpace(player.PlayerId))
            .GroupBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        foreach (var line in result.BoxScore.PlayerStats)
        {
            if (line == null || !players.TryGetValue(line.PlayerId ?? "", out var player))
                continue;

            player.SeasonStats ??= new PlayerSeasonStats();
            if (player.SeasonStats.SeasonYear != league.SeasonYear)
                player.SeasonStats = new PlayerSeasonStats { SeasonYear = league.SeasonYear };
            player.SeasonStats.Add(line);
        }
    }
}
