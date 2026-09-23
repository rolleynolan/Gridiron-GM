using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

// Repairs only immediate CPU starter shortages in the two roster-building phases.
// Mutations remain in TransactionService so normal cap, pool, roster, and phase rules apply.
public sealed class CpuRosterManagementService
{
    private readonly GameCoreContext _context;

    public CpuRosterManagementService(GameCoreContext context) => _context = context;

    public CpuRosterRepairResult RepairCpuStarterShortages()
    {
        var result = new CpuRosterRepairResult();
        var league = _context?.ActiveLeague;
        if (league == null || !IsRosterRepairPhase(league.Calendar?.Phase))
            return result;

        var contracts = new ContractService(_context);
        var transactions = new TransactionService(_context);
        foreach (var team in league.Teams
                     .Where(team => team != null && !string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(team => team.TeamId, StringComparer.OrdinalIgnoreCase))
        {
            result.TeamsEvaluated++;
            foreach (var requirement in DepthChartRules.RequiredStartersByPosition.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
            {
                var rosterCount = team.Roster.Count(player => string.Equals(player?.Position, requirement.Key, StringComparison.OrdinalIgnoreCase));
                while (rosterCount < requirement.Value && team.Roster.Count < RosterService.RosterLimit)
                {
                    var signed = false;
                    foreach (var player in league.FreeAgents
                                 .Where(candidate => string.Equals(candidate?.Position, requirement.Key, StringComparison.OrdinalIgnoreCase))
                                 .OrderByDescending(candidate => candidate.Overall)
                                 .ThenByDescending(candidate => candidate.Potential)
                                 .ThenBy(candidate => candidate.Age)
                                 .ThenBy(candidate => candidate.PlayerId, StringComparer.OrdinalIgnoreCase)
                                 .ToList())
                    {
                        var annualSalary = Math.Round(contracts.GetRequiredAnnualSalary(player, team) * 1.20m, 0, MidpointRounding.AwayFromZero);
                        var signing = transactions.SignFreeAgent(
                            player.PlayerId,
                            team.TeamId,
                            new ContractOffer
                            {
                                AnnualSalary = annualSalary,
                                GuaranteedSalary = Math.Round(annualSalary * 0.20m, 0, MidpointRounding.AwayFromZero),
                                Years = 1,
                            },
                            contracts,
                            $"CPU roster repair: {requirement.Key} starter shortage ({rosterCount}/{requirement.Value}) during {league.Calendar.Phase}.");
                        if (!signing.Ok || !signing.Accepted)
                            continue;

                        result.Signings++;
                        result.Rationales.Add($"{team.Abbreviation}: signed {player.Name} to address {requirement.Key} ({rosterCount}/{requirement.Value}).");
                        rosterCount++;
                        signed = true;
                        break;
                    }

                    if (!signed)
                        break;
                }
            }
        }

        return result;
    }

    private static bool IsRosterRepairPhase(string phase)
        => string.Equals(phase, ScheduleService.FreeAgencyPendingPhase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase);
}

public sealed class CpuRosterRepairResult
{
    public int TeamsEvaluated { get; set; }
    public int Signings { get; set; }
    public List<string> Rationales { get; set; } = new();
}
