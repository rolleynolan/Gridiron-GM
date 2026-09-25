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

        var cpu = new CpuRosterManagementService(_context);
        var released = 0;
        foreach (var team in league.Teams.Where(team => team != null && !string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)).OrderBy(team => team.TeamId, StringComparer.Ordinal))
        {
            released += cpu.CutToLimit(team, RosterService.RosterLimit);
        }

        return released;
    }
}
