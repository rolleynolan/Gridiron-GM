using System;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class RosterConstructionService
{
    private readonly GameCoreContext _context;

    public RosterConstructionService(GameCoreContext context)
    {
        _context = context;
    }

    public int ProcessCpuTrainingCampCuts()
    {
        var league = _context.ActiveLeague;
        if (league == null || !string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return 0;

        var contracts = new ContractService(_context);
        var released = 0;
        foreach (var team in league.Teams.Where(team => team != null && !string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)))
        {
            while (team.Roster.Count > RosterService.RosterLimit)
            {
                var player = team.Roster
                    .Where(candidate => candidate != null)
                    .OrderByDescending(candidate => team.Roster.Count(rosterPlayer => string.Equals(rosterPlayer?.Position, candidate.Position, StringComparison.OrdinalIgnoreCase)) > DepthChartRules.GetRequiredStarters(candidate.Position))
                    .ThenBy(candidate => candidate.Overall)
                    .ThenBy(candidate => candidate.Age)
                    .ThenBy(candidate => candidate.PlayerId, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (player == null || !contracts.ReleasePlayer(player.PlayerId, team.TeamId).Accepted)
                    break;
                released++;
            }
        }

        return released;
    }
}
