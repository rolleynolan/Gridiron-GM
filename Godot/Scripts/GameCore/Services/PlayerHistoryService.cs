using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

// Projects authoritative live and archived player statistics without mutating them.
public sealed class PlayerHistoryService
{
    private readonly GameCoreContext _context;

    public PlayerHistoryService(GameCoreContext context) => _context = context;

    public PlayerHistoryResponse GetPlayerHistory(string playerId, string teamId = null)
    {
        var league = _context?.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        var player = team == null ? null : FindPlayer(team, playerId);
        if (player == null)
            return new PlayerHistoryResponse { Error = "Player history is unavailable." };

        var currentYear = league.SeasonYear;
        var current = player.SeasonStats != null && player.SeasonStats.SeasonYear == currentYear
            ? player.SeasonStats
            : new PlayerSeasonStats { SeasonYear = currentYear };
        return new PlayerHistoryResponse
        {
            Ok = true,
            PlayerName = player.Name ?? "",
            CurrentSeason = Map(current),
            CareerSeasons = (player.CareerStats ?? new List<PlayerSeasonStats>())
                .Where(stats => stats != null && stats.SeasonYear > 0 && stats.SeasonYear != currentYear)
                .OrderByDescending(stats => stats.SeasonYear)
                .Select(Map)
                .ToList(),
        };
    }

    private static PlayerState FindPlayer(TeamState team, string playerId)
        => (team.Roster ?? new List<PlayerState>())
            .Concat(team.InjuredReserve ?? new List<PlayerState>())
            .Concat(team.PracticeSquad ?? new List<PlayerState>())
            .FirstOrDefault(player => string.Equals(player?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));

    private static PlayerSeasonHistoryDto Map(PlayerSeasonStats stats)
        => new()
        {
            SeasonYear = stats?.SeasonYear ?? 0, GamesPlayed = stats?.GamesPlayed ?? 0, PassingYards = stats?.PassingYards ?? 0, PassingTouchdowns = stats?.PassingTouchdowns ?? 0,
            RushingYards = stats?.RushingYards ?? 0, RushingTouchdowns = stats?.RushingTouchdowns ?? 0, ReceivingYards = stats?.ReceivingYards ?? 0, ReceivingTouchdowns = stats?.ReceivingTouchdowns ?? 0,
            Tackles = stats?.Tackles ?? 0, Sacks = stats?.Sacks ?? 0, Interceptions = stats?.Interceptions ?? 0,
        };
}
