using System;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class ContractService
{
    private readonly GameCoreContext _context;

    public ContractService(GameCoreContext context)
    {
        _context = context;
    }

    public decimal GetCommittedSalary(TeamState team)
        => (team?.Roster ?? Enumerable.Empty<PlayerState>())
            .Concat(team?.InjuredReserve ?? Enumerable.Empty<PlayerState>())
            .Concat(team?.PracticeSquad ?? Enumerable.Empty<PlayerState>())
            .Where(player => player != null)
            .Sum(player => Math.Max(0m, player.Contract?.AnnualSalary ?? 0m));

    public decimal GetCapRoom(TeamState team)
    {
        var league = _context.ActiveLeague;
        return Math.Max(0m, (league?.SalaryCap ?? LeagueState.DefaultSalaryCap) - GetCommittedSalary(team));
    }

    public decimal GetRequiredAnnualSalary(PlayerState player, TeamState team)
    {
        if (player == null)
            return 0m;

        var baseSalary = 750_000m + Math.Max(0, player.Overall - 50) * 325_000m;
        var ageFactor = player.Age <= 25 ? 1.12m : player.Age >= 32 ? 0.88m : 1m;
        var moraleFactor = 1m + ((50 - Math.Clamp(player.Morale, 0, 100)) / 500m);
        var currentSalary = player.Contract?.AnnualSalary ?? 0m;
        return Math.Round(Math.Max(baseSalary * ageFactor * moraleFactor, currentSalary * 1.05m), 0, MidpointRounding.AwayFromZero);
    }

    public ContractTransactionResult SignFreeAgent(string playerId, string teamId, ContractOffer offer, string transactionRationale = null)
    {
        return new TransactionService(_context).SignFreeAgent(playerId, teamId, offer, this, transactionRationale);
    }

    public ContractTransactionResult ReleasePlayer(string playerId, string teamId = null)
    {
        return new TransactionService(_context).ReleasePlayer(playerId, teamId, this);
    }

    public ContractTransactionResult ReleasePlayers(System.Collections.Generic.IEnumerable<string> playerIds, string teamId = null)
    {
        return new TransactionService(_context).ReleasePlayers(playerIds, teamId, this);
    }

    public PlayerReleasePreviewDto PreviewRelease(string playerId, string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null) return new PlayerReleasePreviewDto { Error = "No active league loaded." };
        var resolvedTeamId = string.IsNullOrWhiteSpace(teamId) ? league.UserTeamId : teamId;
        var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, resolvedTeamId, StringComparison.OrdinalIgnoreCase));
        var player = team?.Roster?.FirstOrDefault(candidate => string.Equals(candidate?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (team == null || player == null) return new PlayerReleasePreviewDto { Error = "Team or rostered player was not found." };
        if (!ContractPhaseRules.CanManageRoster(league, out var phaseError)) return new PlayerReleasePreviewDto { Error = phaseError };

        var annualSalary = Math.Max(0m, player.Contract?.AnnualSalary ?? 0m);
        var payrollBefore = GetCommittedSalary(team);
        var capRoomBefore = GetCapRoom(team);
        return new PlayerReleasePreviewDto
        {
            Ok = true,
            PlayerId = player.PlayerId,
            PlayerName = player.Name,
            Position = player.Position,
            ContractType = player.Contract?.ContractType ?? "Standard",
            YearsRemaining = Math.Max(0, player.Contract?.YearsRemaining ?? 0),
            AnnualSalary = annualSalary,
            GuaranteedSalary = Math.Max(0m, player.Contract?.GuaranteedSalary ?? 0m),
            PayrollBefore = payrollBefore,
            PayrollAfter = Math.Max(0m, payrollBefore - annualSalary),
            CapRoomBefore = capRoomBefore,
            CapRoomAfter = Math.Min(league.SalaryCap, capRoomBefore + annualSalary),
            RosterCountBefore = team.Roster.Count,
            RosterCountAfter = Math.Max(0, team.Roster.Count - 1),
        };
    }

    public PracticeSquadActiveSigningPreviewDto PreviewPracticeSquadActiveSigning(string playerId, string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null) return new PracticeSquadActiveSigningPreviewDto { Error = "No active league loaded." };
        var resolvedTeamId = string.IsNullOrWhiteSpace(teamId) ? league.UserTeamId : teamId;
        var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, resolvedTeamId, StringComparison.OrdinalIgnoreCase));
        var player = team?.PracticeSquad?.FirstOrDefault(candidate => string.Equals(candidate?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (team == null || player == null) return new PracticeSquadActiveSigningPreviewDto { Error = "Team or practice-squad player was not found." };
        if (!ContractPhaseRules.CanManageRoster(league, out var phaseError)) return new PracticeSquadActiveSigningPreviewDto { Error = phaseError };
        if (team.Roster.Count >= RosterService.RosterLimit) return new PracticeSquadActiveSigningPreviewDto { Error = $"Roster is full at {RosterService.RosterLimit} players. Create an active-roster slot first." };

        var currentSalary = Math.Max(0m, player.Contract?.AnnualSalary ?? 0m);
        var activeSalary = Math.Max(TransactionService.ActiveRosterMinimumSalary, currentSalary);
        var capBefore = GetCapRoom(team);
        var capAfter = capBefore + currentSalary - activeSalary;
        if (capAfter < 0m) return new PracticeSquadActiveSigningPreviewDto { Error = "The permanent active-roster contract would exceed available cap room." };
        return new PracticeSquadActiveSigningPreviewDto
        {
            Ok = true,
            PlayerId = player.PlayerId,
            PlayerName = player.Name,
            Position = player.Position,
            CurrentContractType = player.Contract?.ContractType ?? "Practice Squad",
            CurrentAnnualSalary = currentSalary,
            NewAnnualSalary = activeSalary,
            RosterCountBefore = team.Roster.Count,
            RosterCountAfter = team.Roster.Count + 1,
            CapRoomBefore = capBefore,
            CapRoomAfter = capAfter,
        };
    }

    public ContractTransactionResult SignPracticeSquadPlayerToActiveRoster(string playerId, string teamId = null)
        => new TransactionService(_context).SignPracticeSquadPlayerToActiveRoster(playerId, teamId, this);

    public ContractTransactionResult ReSignPlayer(string playerId, string teamId, ContractOffer offer)
    {
        return new TransactionService(_context).ReSignPlayer(playerId, teamId, offer, this);
    }

    public ContractTransactionResult ApplyFranchiseTag(string playerId, string teamId = null)
    {
        return new TransactionService(_context).ApplyFranchiseTag(playerId, teamId, this);
    }

    public int ProcessContractExpirations()
    {
        return new TransactionService(_context).ProcessContractExpirations(this);
    }

    public void RefreshCapRoom(LeagueState league)
    {
        if (league == null)
            return;

        foreach (var team in league.Teams.Where(team => team != null))
            team.CapRoom = Math.Max(0m, league.SalaryCap - GetCommittedSalary(team));
    }

    public static void MigrateLegacyContracts(LeagueState league)
    {
        if (league == null)
            return;

        league.SalaryCap = league.SalaryCap <= 0m ? LeagueState.DefaultSalaryCap : league.SalaryCap;
        foreach (var team in league.Teams.Where(team => team != null && team.Roster != null && team.Roster.Count > 0))
        {
            if (team.Roster.Any(player => (player?.Contract?.AnnualSalary ?? 0m) > 0m))
                continue;

            var players = team.Roster.Where(player => player != null).ToList();
            var targetCommitments = Math.Max(0m, league.SalaryCap - team.CapRoom);
            var weights = players.Select(player => Math.Max(1, (player.Overall - 45) * (player.Overall - 45))).ToList();
            var totalWeight = weights.Sum();
            var assigned = 0m;
            for (var index = 0; index < players.Count; index++)
            {
                var annualSalary = index == players.Count - 1
                    ? targetCommitments - assigned
                    : Math.Round(targetCommitments * weights[index] / totalWeight, 0, MidpointRounding.AwayFromZero);
                assigned += annualSalary;
                players[index].Morale = players[index].Morale == 0 ? 50 : players[index].Morale;
                players[index].MoraleTrend = string.IsNullOrWhiteSpace(players[index].MoraleTrend) ? "Stable" : players[index].MoraleTrend;
                players[index].Contract = new PlayerContractState
                {
                    AnnualSalary = annualSalary,
                    GuaranteedSalary = Math.Round(annualSalary * 0.30m, 0),
                    YearsRemaining = 1 + (index % 4),
                    SignedSeason = league.SeasonYear,
                    ContractType = "Migrated",
                };
            }

            team.CapRoom = Math.Max(0m, league.SalaryCap - targetCommitments);
        }
    }

    private static ContractTransactionResult Failure(string message) => new() { Ok = false, Message = message };
}
