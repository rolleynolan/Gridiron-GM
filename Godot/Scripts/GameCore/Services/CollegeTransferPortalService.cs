using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Produces a bounded, deterministic, CPU-only portal plan between college seasons.
public static class CollegeTransferPortalService
{
    public const int MaximumAnnualTransfers = 16;

    public static List<CollegeTransferRecord> BuildPlan(
        int nextSeasonYear,
        CollegeUniverseState previousUniverse,
        IReadOnlyCollection<CollegePlayerState> returningPlayers,
        IReadOnlyList<CollegeTeamState> nextTeams)
    {
        if (previousUniverse == null || returningPlayers == null || nextTeams == null || nextTeams.Count < 2)
            return new List<CollegeTransferRecord>();

        var previousTeams = (previousUniverse.Teams ?? new List<CollegeTeamState>())
            .Where(team => team != null)
            .ToDictionary(team => team.TeamId, StringComparer.OrdinalIgnoreCase);
        var assignments = returningPlayers
            .Where(player => player != null && !string.IsNullOrWhiteSpace(player.PlayerId))
            .ToDictionary(player => player.PlayerId, player => player.TeamId, StringComparer.OrdinalIgnoreCase);
        var candidates = returningPlayers
            .Where(player => player != null && !player.IsRedshirted && player.CollegeYear < 5 && player.PlayableSeasonsUsed < 4)
            .GroupBy(player => player.TeamId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(player => TransferInterest(player, previousTeams.GetValueOrDefault(player.TeamId), nextSeasonYear))
                .ThenBy(player => player.PlayerId, StringComparer.Ordinal)
                .First())
            .Where(player => TransferInterest(player, previousTeams.GetValueOrDefault(player.TeamId), nextSeasonYear) >= 20)
            .OrderByDescending(player => TransferInterest(player, previousTeams.GetValueOrDefault(player.TeamId), nextSeasonYear))
            .ThenBy(player => player.PlayerId, StringComparer.Ordinal)
            .Take(MaximumAnnualTransfers)
            .ToList();

        var records = new List<CollegeTransferRecord>(candidates.Count);
        foreach (var player in candidates)
        {
            var destination = nextTeams
                .Where(team => team != null && !string.Equals(team.TeamId, player.TeamId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(team => PositionDepth(returningPlayers, assignments, team.TeamId, player.Position))
                .ThenByDescending(team => previousTeams.GetValueOrDefault(team.TeamId)?.Wins ?? 0)
                .ThenBy(team => StableValue($"{nextSeasonYear}-{player.PlayerId}-{team.TeamId}") % 31)
                .ThenBy(team => team.TeamId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (destination == null)
                continue;

            var fromTeam = previousTeams.GetValueOrDefault(player.TeamId);
            var destinationDepth = PositionDepth(returningPlayers, assignments, destination.TeamId, player.Position);
            records.Add(new CollegeTransferRecord
            {
                SeasonYear = nextSeasonYear,
                PlayerId = player.PlayerId,
                PlayerName = player.Name,
                Position = player.Position,
                FromTeamId = player.TeamId,
                ToTeamId = destination.TeamId,
                Reason = BuildReason(player, fromTeam, destinationDepth),
            });
            assignments[player.PlayerId] = destination.TeamId;
        }
        return records;
    }

    private static int TransferInterest(CollegePlayerState player, CollegeTeamState team, int seasonYear)
        => Math.Max(0, 10 - player.GamesPlayed) * 8
           + Math.Max(0, (team?.Losses ?? 0) - 5) * 3
           + Math.Max(0, player.Potential - player.Overall)
           + StableValue($"{seasonYear}-{player.PlayerId}-portal") % 20;

    private static int PositionDepth(
        IEnumerable<CollegePlayerState> players,
        IReadOnlyDictionary<string, string> assignments,
        string teamId,
        string position)
        => players.Count(player => player != null
            && assignments.TryGetValue(player.PlayerId, out var assignedTeamId)
            && string.Equals(assignedTeamId, teamId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase));

    private static string BuildReason(CollegePlayerState player, CollegeTeamState fromTeam, int destinationDepth)
    {
        if (player.GamesPlayed < CollegeUniverseService.RegularSeasonWeeks - 2)
            return $"Sought a clearer path to playing time at {player.Position}.";
        if ((fromTeam?.Losses ?? 0) >= 7)
            return "Sought a stronger competitive situation and a fresh program fit.";
        if (destinationDepth == 0)
            return $"Moved into an open {player.Position} opportunity with immediate eligibility.";
        return "Chose a new program fit with immediate eligibility.";
    }

    private static int StableValue(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in value ?? "") hash = (hash ^ character) * 16777619;
            return (int)(hash & 0x7fffffff);
        }
    }
}
