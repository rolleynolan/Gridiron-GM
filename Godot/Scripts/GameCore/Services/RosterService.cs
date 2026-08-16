using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class RosterService
{
    private const int RosterLimit = 53;
    private readonly GameCoreContext _context;

    public RosterService(GameCoreContext context)
    {
        _context = context;
    }

    public TeamRosterResponse GetTeamRoster(string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new TeamRosterResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
        {
            return new TeamRosterResponse
            {
                Ok = false,
                Error = "Team not found.",
            };
        }

        var injuries = team.Roster.Count(player => !string.IsNullOrWhiteSpace(player.Injury));
        var positionCounts = team.Roster
            .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => FootballPositionOrder.GetSortOrder(group.Key))
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PositionCountDto
            {
                Position = group.Key,
                Count = group.Count(),
            })
            .ToList();

        var roles = BuildDepthRoleMap(team);

        return new TeamRosterResponse
        {
            Ok = true,
            Team = new TeamIdentityDto
            {
                TeamId = team.TeamId,
                Name = team.Name,
                Abbreviation = team.Abbreviation,
            },
            RosterStatus = new RosterStatusDto
            {
                IsValid = team.Roster.Count <= RosterLimit,
                RosterSize = team.Roster.Count,
                RosterLimit = RosterLimit,
                RequiredCuts = Math.Max(0, team.Roster.Count - RosterLimit),
                OpenSlots = Math.Max(0, RosterLimit - team.Roster.Count),
                InjuredCount = injuries,
                Issues = team.Roster.Count <= RosterLimit
                    ? new List<string>()
                    : new List<string> { "Roster exceeds active limit." },
            },
            PositionCounts = positionCounts,
            Players = team.Roster
                .OrderBy(player => FootballPositionOrder.GetSortOrder(player.Position))
                .ThenBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(player => player.Overall)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => new PlayerRowDto
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    Position = player.Position,
                    Overall = player.Overall,
                    Age = player.Age,
                    Status = player.Status,
                    Injury = player.Injury,
                    DepthRole = roles.TryGetValue(player.PlayerId, out var role) ? role : "Depth",
                })
                .ToList(),
        };
    }

    private static Dictionary<string, string> BuildDepthRoleMap(TeamState team)
    {
        var roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in team.DepthChart)
        {
            for (var index = 0; index < pair.Value.Count; index++)
            {
                roles[pair.Value[index]] = index == 0 ? "Starter" : "Backup";
            }
        }

        return roles;
    }
}
