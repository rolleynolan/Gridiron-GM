using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class RosterService
{
    public const int RosterLimit = 53;
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

        var rosterLimit = GetRosterLimit(league);
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
                IsValid = team.Roster.Count <= rosterLimit,
                RosterSize = team.Roster.Count,
                RosterLimit = rosterLimit,
                RequiredCuts = Math.Max(0, team.Roster.Count - rosterLimit),
                OpenSlots = Math.Max(0, rosterLimit - team.Roster.Count),
                InjuredCount = injuries,
                Issues = team.Roster.Count <= rosterLimit
                    ? new List<string>()
                    : new List<string> { $"Roster exceeds the {rosterLimit}-player limit for this phase." },
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

    private static int GetRosterLimit(LeagueState league)
    {
        var phase = league?.Calendar?.Phase ?? "";
        return string.Equals(phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase)
               || string.Equals(phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase)
               || string.Equals(phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase)
            ? 90
            : RosterLimit;
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
