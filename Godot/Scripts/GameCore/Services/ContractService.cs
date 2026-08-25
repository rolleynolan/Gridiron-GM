using System;
using System.Linq;
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

    public ContractTransactionResult SignFreeAgent(string playerId, string teamId, ContractOffer offer)
    {
        return new TransactionService(_context).SignFreeAgent(playerId, teamId, offer, this);
    }

    public ContractTransactionResult ReleasePlayer(string playerId, string teamId = null)
    {
        return new TransactionService(_context).ReleasePlayer(playerId, teamId, this);
    }

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
