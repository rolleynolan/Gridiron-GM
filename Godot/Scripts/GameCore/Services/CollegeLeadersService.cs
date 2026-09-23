using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Read-only leader tables derived from persisted college-season statistics.
public sealed class CollegeLeadersService
{
    private readonly GameCoreContext _context;
    public CollegeLeadersService(GameCoreContext context) => _context = context;

    public CollegeLeadersResult GetLeaders(int limit = 5)
    {
        var universe = _context?.ActiveLeague?.CollegeUniverse;
        if (universe == null)
            return new CollegeLeadersResult { Message = "College season unavailable." };

        limit = Math.Clamp(limit, 1, 20);
        var players = universe.Players ?? new List<CollegePlayerState>();
        var teams = universe.Teams ?? new List<CollegeTeamState>();
        var result = new CollegeLeadersResult { Ok = true, SeasonYear = universe.SeasonYear };
        result.Categories.Add(BuildCategory("Passing", "PASS YDS", players, teams, player => player.PassingYards, player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase), limit));
        result.Categories.Add(BuildCategory("Rushing", "RUSH YDS", players, teams, player => player.RushingYards, player => string.Equals(player.Position, "RB", StringComparison.OrdinalIgnoreCase), limit));
        result.Categories.Add(BuildCategory("Receiving", "REC YDS", players, teams, player => string.Equals(player.Position, "WR", StringComparison.OrdinalIgnoreCase) || string.Equals(player.Position, "TE", StringComparison.OrdinalIgnoreCase) ? player.ReceivingYards : 0, player => string.Equals(player.Position, "WR", StringComparison.OrdinalIgnoreCase) || string.Equals(player.Position, "TE", StringComparison.OrdinalIgnoreCase), limit));
        result.Categories.Add(BuildCategory("Touchdowns", "TD", players, teams, player => player.Touchdowns, player => new[] { "QB", "RB", "WR", "TE" }.Contains(player.Position, StringComparer.OrdinalIgnoreCase), limit));
        return result;
    }

    private static CollegeLeaderCategory BuildCategory(string name, string statLabel, IEnumerable<CollegePlayerState> players, IReadOnlyList<CollegeTeamState> teams, Func<CollegePlayerState, int> value, Func<CollegePlayerState, bool> include, int limit)
    {
        return new CollegeLeaderCategory
        {
            Name = name,
            StatLabel = statLabel,
            Leaders = players.Where(player => player != null && player.GamesPlayed > 0 && include(player))
                .OrderByDescending(value).ThenByDescending(player => player.Touchdowns).ThenBy(player => player.Name, StringComparer.Ordinal).ThenBy(player => player.PlayerId, StringComparer.Ordinal)
                .Take(limit)
                .Select(player => new CollegeLeaderEntry { PlayerId = player.PlayerId, PlayerName = player.Name, Position = player.Position, TeamName = teams.FirstOrDefault(team => string.Equals(team?.TeamId, player.TeamId, StringComparison.OrdinalIgnoreCase))?.Name ?? "", Value = value(player) })
                .ToList(),
        };
    }
}
