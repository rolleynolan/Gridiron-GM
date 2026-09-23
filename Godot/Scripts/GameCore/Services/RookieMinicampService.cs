using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class RookieMinicampService
{
    public const int InvitationLimit = 10;
    private readonly GameCoreContext _context;

    public RookieMinicampService(GameCoreContext context) => _context = context;

    public RookieMinicampState GetState()
    {
        var league = _context.ActiveLeague;
        if (league == null) return new RookieMinicampState();
        league.RookieMinicamp ??= new RookieMinicampState();
        league.RookieMinicamp.InvitedPlayerIds ??= new List<string>();
        return league.RookieMinicamp;
    }

    public ContractTransactionResult Invite(string playerId)
    {
        var league = _context.ActiveLeague;
        if (league == null) return Failure("No active league loaded.");
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase))
            return Failure("Rookie-minicamp invitations are available during Rookie Signing.");
        var player = league.FreeAgents.FirstOrDefault(candidate => string.Equals(candidate?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (!UndraftedFreeAgentService.IsUndraftedRookie(player)) return Failure("Only unsigned undrafted rookies can receive a minicamp invitation.");
        var state = GetState();
        if (state.SeasonYear != league.SeasonYear) { state.SeasonYear = league.SeasonYear; state.InvitedPlayerIds.Clear(); }
        if (state.InvitedPlayerIds.Contains(player.PlayerId, StringComparer.OrdinalIgnoreCase)) return Failure("This player is already invited to rookie minicamp.");
        if (state.InvitedPlayerIds.Count >= InvitationLimit) return Failure($"Rookie minicamp is full at {InvitationLimit} invitations.");
        state.InvitedPlayerIds.Add(player.PlayerId);
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = $"{player.Name} invited to rookie minicamp." };
    }

    public ContractTransactionResult Withdraw(string playerId)
    {
        var league = _context.ActiveLeague;
        var state = league?.RookieMinicamp;
        var existing = state?.InvitedPlayerIds?.FirstOrDefault(id => string.Equals(id, playerId, StringComparison.OrdinalIgnoreCase));
        if (existing == null) return Failure("This player does not have an active rookie-minicamp invitation.");
        state.InvitedPlayerIds.Remove(existing);
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Rookie-minicamp invitation withdrawn." };
    }

    private static ContractTransactionResult Failure(string message) => new() { Ok = false, Accepted = false, Message = message };
}
