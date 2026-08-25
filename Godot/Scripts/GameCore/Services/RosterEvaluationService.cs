using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class RosterEvaluationService
{
    private readonly GameCoreContext _context;
    public RosterEvaluationService(GameCoreContext context) => _context = context;

    public RosterEvaluationResponse GetPlayerRoles(string teamId = null)
    {
        var team = GameCoreStateHelper.ResolveTeam(_context.ActiveLeague, teamId);
        if (team == null)
            return new RosterEvaluationResponse { Error = "Team not found." };

        var battles = team.TrainingCamp?.PositionBattles ?? new List<PositionBattleOutcome>();
        return new RosterEvaluationResponse
        {
            Ok = true,
            Players = team.Roster.Select(player =>
            {
                var available = PlayerInjuryService.IsAvailableForGame(player);
                var ids = team.DepthChart.TryGetValue(player.Position, out var depth) ? depth : new List<string>();
                var index = ids.FindIndex(id => string.Equals(id, player.PlayerId, StringComparison.OrdinalIgnoreCase));
                var starters = DepthChartRules.GetRequiredStarters(player.Position);
                var battle = battles.FirstOrDefault(entry => string.Equals(entry.WinnerPlayerId, player.PlayerId, StringComparison.OrdinalIgnoreCase));
                var role = !available ? "Unavailable" : index >= 0 && index < starters ? "Starter" : "Backup";
                var readiness = !available ? $"Out: {player.Injury}" : player.Fatigue >= 40 ? "Limited" : player.Fatigue >= 20 ? "Managing workload" : "Ready";
                var explanation = !available ? $"Unavailable for {player.CurrentInjury?.DaysRemaining ?? 0} day(s); depth chart promotes an available replacement."
                    : battle != null ? $"Battle winner. {battle.Explanation}"
                    : $"{role} at {player.Position} (depth #{Math.Max(1, index + 1)}); OVR {player.Overall}, POT {player.Potential}, fatigue {player.Fatigue}.";
                return new PlayerRoleFeedbackDto { PlayerId = player.PlayerId, Name = player.Name, Position = player.Position, Role = role, Readiness = readiness, Explanation = explanation };
            }).OrderBy(player => player.Position, StringComparer.OrdinalIgnoreCase).ThenBy(player => player.Role).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase).ToList(),
        };
    }
}
