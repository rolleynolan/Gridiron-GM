using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class DepthChartService
{
    private readonly GameCoreContext _context;

    public DepthChartService(GameCoreContext context)
    {
        _context = context;
    }

    public TeamDepthChartResponse GetTeamDepthChart(string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Team not found.",
            };
        }

        var chart = BuildDepthChart(team);
        var issues = chart
            .Where(position => position.Players.Count < position.RequiredStarters)
            .Select(position => $"Missing starting {position.Position}.")
            .ToList();

        return new TeamDepthChartResponse
        {
            Ok = true,
            Team = new TeamIdentityDto
            {
                TeamId = team.TeamId,
                Name = team.Name,
                Abbreviation = team.Abbreviation,
            },
            DepthChartStatus = new DepthChartStatusDto
            {
                IsValid = issues.Count == 0,
                Issues = issues,
            },
            Positions = chart,
        };
    }

    public TeamDepthChartResponse AutoFillDepthChart(string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Team not found.",
            };
        }

        team.DepthChart.Clear();
        foreach (var position in team.Roster
                     .Select(player => player.Position)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(position => FootballPositionOrder.GetSortOrder(position))
                     .ThenBy(position => position, StringComparer.OrdinalIgnoreCase))
        {
            team.DepthChart[position] = team.Roster
                .Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(player => player.Overall)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => player.PlayerId)
                .ToList();
        }

        return GetTeamDepthChart(team.TeamId);
    }

    public TeamDepthChartResponse UpdateDepthChart(string action, string position, string playerId, string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Team not found.",
            };
        }

        if (string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(playerId))
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Position and player are required.",
            };
        }

        var player = team.Roster.FirstOrDefault(candidate =>
            string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (player == null)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Player not found.",
            };
        }

        if (!string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Player does not belong to that position group.",
            };
        }

        var group = EnsureDepthChartGroup(team, position);
        var index = group.FindIndex(candidate => string.Equals(candidate, playerId, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            return new TeamDepthChartResponse
            {
                Ok = false,
                Error = "Player is not available in that position group.",
            };
        }

        switch ((action ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "move_up":
                if (index > 0)
                    Swap(group, index, index - 1);
                break;
            case "move_down":
                if (index < group.Count - 1)
                    Swap(group, index, index + 1);
                break;
            case "set_starter":
                if (index > 0)
                {
                    group.RemoveAt(index);
                    group.Insert(0, playerId);
                }
                break;
            default:
                return new TeamDepthChartResponse
                {
                    Ok = false,
                    Error = "Unsupported depth chart action.",
                };
        }

        team.DepthChart[position] = group;
        return GetTeamDepthChart(team.TeamId);
    }

    private static List<DepthChartPositionDto> BuildDepthChart(TeamState team)
    {
        var playersById = team.Roster.ToDictionary(player => player.PlayerId, StringComparer.OrdinalIgnoreCase);
        var output = new List<DepthChartPositionDto>();
        var positions = team.Roster
            .Select(player => player.Position)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(position => FootballPositionOrder.GetSortOrder(position))
            .ThenBy(position => position, StringComparer.OrdinalIgnoreCase);

        foreach (var position in positions)
        {
            var ids = team.DepthChart.TryGetValue(position, out var group) && group.Count > 0
                ? group
                : team.Roster
                    .Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(player => player.Overall)
                    .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(player => player.PlayerId)
                    .ToList();

            var requiredStarters = DepthChartRules.GetRequiredStarters(position);
            var players = new List<DepthChartPlayerDto>();

            for (var index = 0; index < ids.Count; index++)
            {
                if (!playersById.TryGetValue(ids[index], out var player))
                    continue;

                players.Add(new DepthChartPlayerDto
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    Overall = player.Overall,
                    Status = player.Status,
                    Injury = player.Injury,
                    Role = index < requiredStarters ? "Starter" : "Backup",
                });
            }

            output.Add(new DepthChartPositionDto
            {
                Position = position,
                RequiredStarters = requiredStarters,
                Players = players,
            });
        }

        return output;
    }

    private static List<string> EnsureDepthChartGroup(TeamState team, string position)
    {
        var rosterIds = team.Roster
            .Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(player => player.Overall)
            .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
            .Select(player => player.PlayerId)
            .ToList();

        var existing = team.DepthChart.TryGetValue(position, out var group)
            ? group
            : new List<string>();

        var merged = existing
            .Where(id => rosterIds.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var rosterId in rosterIds)
        {
            if (!merged.Contains(rosterId, StringComparer.OrdinalIgnoreCase))
                merged.Add(rosterId);
        }

        team.DepthChart[position] = merged;
        return merged;
    }

    private static void Swap(List<string> group, int leftIndex, int rightIndex)
    {
        (group[leftIndex], group[rightIndex]) = (group[rightIndex], group[leftIndex]);
    }
}
