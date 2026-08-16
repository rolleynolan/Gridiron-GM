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
        var league = _context.ActiveLeague;
        if (league == null)
            return Failure("No active league loaded.");

        var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate.TeamId, teamId ?? league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var player = league.FreeAgents.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (team == null || player == null)
            return Failure("Team or free agent was not found.");
        if (offer == null || offer.Years < 1 || offer.Years > 5 || offer.AnnualSalary <= 0m || offer.GuaranteedSalary < 0m)
            return Failure("Offer must include 1-5 years, a positive annual salary, and non-negative guarantees.");

        var requiredSalary = GetRequiredAnnualSalary(player, team);
        var negotiation = league.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.Negotiation ?? 50;
        var gmDiscount = Math.Clamp((negotiation - 50) / 600m, -0.05m, 0.05m);
        var adjustedRequirement = Math.Round(requiredSalary * (1m - gmDiscount), 0, MidpointRounding.AwayFromZero);
        var capRoom = GetCapRoom(team);
        if (offer.AnnualSalary > capRoom)
            return new ContractTransactionResult { Ok = false, Message = "Offer exceeds available cap room.", RequiredAnnualSalary = adjustedRequirement, CapRoomAfterSigning = capRoom };
        if (offer.AnnualSalary < adjustedRequirement || offer.GuaranteedSalary < offer.AnnualSalary * 0.15m)
            return new ContractTransactionResult { Ok = true, Accepted = false, Message = "The player declined the offer.", RequiredAnnualSalary = adjustedRequirement, CapRoomAfterSigning = capRoom };

        player.Contract = new PlayerContractState
        {
            AnnualSalary = offer.AnnualSalary,
            GuaranteedSalary = offer.GuaranteedSalary,
            YearsRemaining = offer.Years,
            SignedSeason = league.SeasonYear,
            ContractType = "Free Agent Signing",
        };
        player.Status = "Active";
        player.Morale = Math.Clamp(player.Morale + 6, 0, 100);
        player.MoraleTrend = "Improving";
        team.Roster.Add(player);
        league.FreeAgents.Remove(player);
        team.CapRoom = GetCapRoom(team);

        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Free agent signed.", RequiredAnnualSalary = adjustedRequirement, CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ReleasePlayer(string playerId, string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = league?.Teams.FirstOrDefault(candidate => string.Equals(candidate.TeamId, teamId ?? league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var player = team?.Roster.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (team == null || player == null)
            return Failure("Team or rostered player was not found.");

        team.Roster.Remove(player);
        foreach (var depthChart in team.DepthChart.Values)
            depthChart.RemoveAll(id => string.Equals(id, player.PlayerId, StringComparison.OrdinalIgnoreCase));
        player.Status = "Free Agent";
        player.Morale = Math.Clamp(player.Morale - 8, 0, 100);
        player.MoraleTrend = "Declining";
        player.Contract = new PlayerContractState { ContractType = "Free Agent" };
        league.FreeAgents.Add(player);
        team.CapRoom = GetCapRoom(team);
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player released to free agency.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ReSignPlayer(string playerId, string teamId, ContractOffer offer)
    {
        var league = _context.ActiveLeague;
        var team = league?.Teams.FirstOrDefault(candidate => string.Equals(candidate.TeamId, teamId ?? league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        var player = team?.Roster.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (team == null || player == null)
            return Failure("Team or rostered player was not found.");
        if (offer == null || offer.Years < 1 || offer.Years > 5 || offer.AnnualSalary <= 0m || offer.GuaranteedSalary < 0m)
            return Failure("Offer must include 1-5 years, a positive annual salary, and non-negative guarantees.");

        var requiredSalary = GetRequiredAnnualSalary(player, team);
        var negotiation = league.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.Negotiation ?? 50;
        var adjustedRequirement = Math.Round(requiredSalary * (1m - Math.Clamp((negotiation - 50) / 600m, -0.05m, 0.05m)), 0, MidpointRounding.AwayFromZero);
        var capRoomAfterReplacingContract = GetCapRoom(team) + (player.Contract?.AnnualSalary ?? 0m) - offer.AnnualSalary;
        if (capRoomAfterReplacingContract < 0m)
            return new ContractTransactionResult { Ok = false, Message = "Offer exceeds available cap room.", RequiredAnnualSalary = adjustedRequirement, CapRoomAfterSigning = GetCapRoom(team) };
        if (offer.AnnualSalary < adjustedRequirement || offer.GuaranteedSalary < offer.AnnualSalary * 0.15m)
            return new ContractTransactionResult { Ok = true, Accepted = false, Message = "The player declined the extension.", RequiredAnnualSalary = adjustedRequirement, CapRoomAfterSigning = GetCapRoom(team) };

        player.Contract = new PlayerContractState { AnnualSalary = offer.AnnualSalary, GuaranteedSalary = offer.GuaranteedSalary, YearsRemaining = offer.Years, SignedSeason = league.SeasonYear, ContractType = "Extension" };
        player.Morale = Math.Clamp(player.Morale + 5, 0, 100);
        player.MoraleTrend = "Improving";
        team.CapRoom = GetCapRoom(team);
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player re-signed.", RequiredAnnualSalary = adjustedRequirement, CapRoomAfterSigning = team.CapRoom };
    }

    public int ProcessContractExpirations()
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return 0;
        if (league.LastContractExpirationSeason == league.SeasonYear)
            return 0;

        var expired = 0;
        foreach (var team in league.Teams.Where(team => team != null))
        {
            foreach (var player in team.Roster.ToList())
            {
                if (player?.Contract == null || player.Contract.YearsRemaining <= 0)
                    continue;

                player.Contract.YearsRemaining--;
                if (player.Contract.YearsRemaining > 0)
                    continue;

                team.Roster.Remove(player);
                foreach (var depthChart in team.DepthChart.Values)
                    depthChart.RemoveAll(id => string.Equals(id, player.PlayerId, StringComparison.OrdinalIgnoreCase));
                player.Status = "Free Agent";
                player.Contract = new PlayerContractState { ContractType = "Free Agent" };
                player.Morale = Math.Clamp(player.Morale - 3, 0, 100);
                player.MoraleTrend = "Declining";
                league.FreeAgents.Add(player);
                expired++;
            }
        }

        RefreshCapRoom(league);
        league.LastContractExpirationSeason = league.SeasonYear;
        return expired;
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
