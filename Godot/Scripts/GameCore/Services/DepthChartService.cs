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

    private static string MutationRestriction(LeagueState league, TeamState team)
    {
        if (string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)
            && HeadCoachAuthorityService.IsHeadCoachControlled(team, HeadCoachAuthorityService.LineupAndDepthChart))
            return "The Head Coach controls lineup and depth chart under the current agreement.";
        if (league.ActiveLiveGameSession is { Active: true, IsPaused: false })
            return "Pause the live game before changing the depth chart.";
        return null;
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
            .Where(position => position.Players.Count(player => player.IsAvailable) < position.RequiredStarters)
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

        var restriction = MutationRestriction(league, team);
        if (restriction != null) return new TeamDepthChartResponse { Ok = false, Error = restriction };
        team.DepthChartLockedPositions ??= new List<string>();
        var priorChart = team.DepthChart.ToDictionary(pair => pair.Key, pair => pair.Value?.ToList() ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        team.DepthChart.Clear();
        foreach (var position in team.Roster
                     .Select(player => player.Position)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(position => FootballPositionOrder.GetSortOrder(position))
                     .ThenBy(position => position, StringComparer.OrdinalIgnoreCase))
        {
            var defaultOrder = team.Roster
                .Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(PlayerInjuryService.IsAvailableForGame)
                .ThenByDescending(player => player.Overall)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .Select(player => player.PlayerId)
                .ToList();
            if (team.DepthChartLockedPositions.Contains(position, StringComparer.OrdinalIgnoreCase)
                && priorChart.TryGetValue(position, out var savedOrder))
            {
                var valid = new HashSet<string>(defaultOrder, StringComparer.OrdinalIgnoreCase);
                var lockedOrder = savedOrder.Where(valid.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                lockedOrder.AddRange(defaultOrder.Where(playerId => !lockedOrder.Contains(playerId, StringComparer.OrdinalIgnoreCase)));
                team.DepthChart[position] = lockedOrder;
            }
            else
                team.DepthChart[position] = defaultOrder;
        }

        return GetTeamDepthChart(team.TeamId);
    }

    public TeamDepthChartResponse TogglePositionLock(string position, string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TeamDepthChartResponse { Ok = false, Error = "Team not found." };
        var restriction = MutationRestriction(league, team);
        if (restriction != null) return new TeamDepthChartResponse { Ok = false, Error = restriction };
        if (string.IsNullOrWhiteSpace(position) || !team.DepthChart.ContainsKey(position))
            return new TeamDepthChartResponse { Ok = false, Error = "Select a valid depth-chart position first." };

        team.DepthChartLockedPositions ??= new List<string>();
        var existing = team.DepthChartLockedPositions.FindIndex(value => string.Equals(value, position, StringComparison.OrdinalIgnoreCase));
        if (existing >= 0)
            team.DepthChartLockedPositions.RemoveAt(existing);
        else
            team.DepthChartLockedPositions.Add(position);
        return GetTeamDepthChart(team.TeamId);
    }

    public TeamDepthChartResponse UpdateDepthChart(string action, string position, string playerId, string teamId = null, string targetPlayerId = null)
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

        var restriction = MutationRestriction(league, team);
        if (restriction != null) return new TeamDepthChartResponse { Ok = false, Error = restriction };
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
            case "move_before":
                var targetIndex = group.FindIndex(candidate => string.Equals(candidate, targetPlayerId, StringComparison.OrdinalIgnoreCase));
                if (targetIndex < 0)
                    return new TeamDepthChartResponse { Ok = false, Error = "Drop target is not available in that position group." };
                group.RemoveAt(index);
                targetIndex = group.FindIndex(candidate => string.Equals(candidate, targetPlayerId, StringComparison.OrdinalIgnoreCase));
                group.Insert(Math.Max(0, targetIndex), playerId);
                break;
            case "move_after":
                var afterTargetIndex = group.FindIndex(candidate => string.Equals(candidate, targetPlayerId, StringComparison.OrdinalIgnoreCase));
                if (afterTargetIndex < 0)
                    return new TeamDepthChartResponse { Ok = false, Error = "Drop target is not available in that position group." };
                group.RemoveAt(index);
                afterTargetIndex = group.FindIndex(candidate => string.Equals(candidate, targetPlayerId, StringComparison.OrdinalIgnoreCase));
                group.Insert(Math.Min(group.Count, afterTargetIndex + 1), playerId);
                break;
            default:
                return new TeamDepthChartResponse
                {
                    Ok = false,
                    Error = "Unsupported depth chart action.",
                };
        }

        team.DepthChart[position] = group;
        if (string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            team.TrainingCamp ??= new TrainingCampState();
            if (!team.TrainingCamp.UserAdjustedPositions.Contains(position, StringComparer.OrdinalIgnoreCase))
                team.TrainingCamp.UserAdjustedPositions.Add(position);
        }
        return GetTeamDepthChart(team.TeamId);
    }

    private List<DepthChartPositionDto> BuildDepthChart(TeamState team)
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

            var availableIndex = 0;
            for (var index = 0; index < ids.Count; index++)
            {
                if (!playersById.TryGetValue(ids[index], out var player))
                    continue;
                var isAvailable = PlayerInjuryService.IsAvailableForGame(player);

                var evaluation = PlayerEvaluationProjectionService.Evaluate(_context.ActiveLeague, player);
                players.Add(new DepthChartPlayerDto
                {
                    PlayerId = player.PlayerId,
                    Name = player.Name,
                    Overall = player.Overall,
                    EstimatedOverall = evaluation.EstimatedOverall,
                    EstimatedOverallRange = evaluation.OverallRange,
                    ScoutingConfidence = evaluation.Confidence,
                    Status = player.Status,
                    Injury = player.Injury,
                    InjuryDaysRemaining = player.CurrentInjury?.DaysRemaining ?? 0,
                    IsAvailable = isAvailable,
                    Role = !isAvailable ? "Unavailable" : availableIndex++ < requiredStarters ? "Starter" : "Backup",
                    ContractSummary = player.Contract == null || player.Contract.AnnualSalary <= 0m
                        ? "Unavailable"
                        : $"${player.Contract.AnnualSalary / 1_000_000m:0.00}M · {player.Contract.YearsRemaining} yr",
                    Morale = player.Morale,
                    MoraleTrend = player.MoraleTrend ?? "Unavailable",
                    Potential = player.Potential,
                    PassingYards = player.SeasonStats?.PassingYards ?? 0,
                    RushingYards = player.SeasonStats?.RushingYards ?? 0,
                    ReceivingYards = player.SeasonStats?.ReceivingYards ?? 0,
                    Tackles = player.SeasonStats?.Tackles ?? 0,
                    Sacks = player.SeasonStats?.Sacks ?? 0,
                });
            }

            output.Add(new DepthChartPositionDto
            {
                Position = position,
                RequiredStarters = requiredStarters,
                IsLocked = team.DepthChartLockedPositions?.Contains(position, StringComparer.OrdinalIgnoreCase) == true,
                Players = players,
            });
        }

        return output;
    }

    private static List<string> EnsureDepthChartGroup(TeamState team, string position)
    {
        var rosterIds = team.Roster
            .Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(PlayerInjuryService.IsAvailableForGame)
            .ThenByDescending(player => player.Overall)
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
