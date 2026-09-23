using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class PositionBattleService
{
    private readonly GameCoreContext _context;
    public PositionBattleService(GameCoreContext context) => _context = context;

    public IReadOnlyList<PositionBattleOutcome> Resolve(string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null || !string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<PositionBattleOutcome>();
        var camp = team.TrainingCamp ??= new TrainingCampState();
        if (camp.PositionBattles.Count > 0)
            return camp.PositionBattles;

        foreach (var group in team.Roster.Where(PlayerInjuryService.IsAvailableForGame).GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() < 2)
                continue;
            var ranked = group.Select(player => new { Player = player, Score = player.Overall * 10 + player.Potential - player.Fatigue * 2 + (string.Equals(camp.FocusPosition, group.Key, StringComparison.OrdinalIgnoreCase) ? 8 : 0) })
                .OrderByDescending(entry => entry.Score).ThenBy(entry => entry.Player.PlayerId, StringComparer.OrdinalIgnoreCase).ToList();
            if (ranked[0].Score - ranked[1].Score > 18)
                continue;
            var outcome = new PositionBattleOutcome
            {
                Position = group.Key,
                WinnerPlayerId = ranked[0].Player.PlayerId,
                WinnerName = ranked[0].Player.Name,
                RunnerUpName = ranked[1].Player.Name,
                Explanation = $"Staff currently favor {ranked[0].Player.Name} over {ranked[1].Player.Name} after weighing readiness, development outlook, and workload. The GM retains the depth-chart decision."
            };
            camp.PositionBattles.Add(outcome);
        }
        return camp.PositionBattles;
    }
}
