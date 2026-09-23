using System;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class FrontOfficeEvaluationService
{
    private readonly GameCoreContext _context;

    public FrontOfficeEvaluationService(GameCoreContext context) => _context = context;

    public FrontOfficeEvaluationResponse EvaluateTeam(string teamId)
    {
        var league = _context?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, teamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
            return new FrontOfficeEvaluationResponse { Error = "Team evaluation is unavailable." };

        var roster = team.Roster ?? new();
        var contracts = new ContractService(_context);
        var needs = roster
            .GroupBy(player => player.Position ?? "")
            .Select(group => new { Position = group.Key, Gap = Math.Max(0, DepthChartRules.GetRequiredStarters(group.Key) - group.Count()) })
            .Where(item => item.Gap > 0)
            .OrderByDescending(item => item.Gap)
            .ThenBy(item => item.Position, StringComparer.OrdinalIgnoreCase)
            .Select(item => $"{item.Position} ({item.Gap} short)")
            .ToList();
        if (roster.Count < RosterService.RosterLimit)
            needs.Add($"Active roster ({RosterService.RosterLimit - roster.Count} open)");

        var averageAge = roster.Count == 0 ? 0 : (int)Math.Round(roster.Average(player => player.Age), MidpointRounding.AwayFromZero);
        var averagePotential = roster.Count == 0 ? 0 : (int)Math.Round(roster.Average(player => player.Potential), MidpointRounding.AwayFromZero);
        var expiring = roster.Count(player => player.Contract?.YearsRemaining == 1);
        var picks = league.Draft?.Picks?.Count(pick => pick != null && string.Equals(pick.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(pick.ProspectId)) ?? 0;
        var capRoom = contracts.GetCapRoom(team);
        var pressure = capRoom < 0m ? "over the cap" : capRoom < 5_000_000m ? "limited cap room" : "cap flexibility";
        var rationale = $"{team.Name} has {pressure}, {expiring} expiring contract(s), an average roster age of {averageAge}, average potential of {averagePotential}, and {picks} available draft pick(s). " +
            (needs.Count == 0 ? "No starter-depth shortages are currently identified." : $"Priority needs: {string.Join(", ", needs)}.");

        return new FrontOfficeEvaluationResponse
        {
            Ok = true,
            TeamId = team.TeamId,
            TeamName = team.Name,
            RosterSize = roster.Count,
            CapRoom = capRoom,
            AverageAge = averageAge,
            AveragePotential = averagePotential,
            ExpiringContracts = expiring,
            DraftPicksAvailable = picks,
            PositionNeeds = needs,
            Rationale = rationale,
        };
    }
}
