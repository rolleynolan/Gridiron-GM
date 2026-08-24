using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Owns rule-approved roster and contract mutations and records each committed change.
public sealed class TransactionService
{
    private readonly GameCoreContext _context;

    public TransactionService(GameCoreContext context)
    {
        _context = context;
    }

    public ContractTransactionResult SignFreeAgent(string playerId, string teamId, ContractOffer offer, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = league?.FreeAgents?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or free agent was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (!IsValidOffer(offer))
            return Failure("Offer must include 1-5 years, a positive annual salary, and non-negative guarantees.");

        var requiredSalary = GetAdjustedRequirement(league, player, team, contracts);
        var capRoom = contracts.GetCapRoom(team);
        if (offer.AnnualSalary > capRoom)
            return new ContractTransactionResult { Ok = false, Message = "Offer exceeds available cap room.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = capRoom };
        if (offer.AnnualSalary < requiredSalary || offer.GuaranteedSalary < offer.AnnualSalary * 0.15m)
            return new ContractTransactionResult { Ok = true, Accepted = false, Message = "The player declined the offer.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = capRoom };
        if (team.Roster.Count >= RosterService.RosterLimit)
            return Failure($"Roster is full at {RosterService.RosterLimit} players. Release a player before signing.");

        player.Contract = BuildContract(league, offer, "Free Agent Signing");
        player.Status = "Active";
        player.Morale = Math.Clamp(player.Morale + 6, 0, 100);
        player.MoraleTrend = "Improving";
        team.Roster.Add(player);
        league.FreeAgents.Remove(player);
        contracts.RefreshCapRoom(league);
        Record(league, "free_agent_signed", team, player, $"{offer.Years}-year contract at {offer.AnnualSalary:0} annually.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Free agent signed.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ReleasePlayer(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = team?.Roster?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or rostered player was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);

        team.Roster.Remove(player);
        RemoveFromDepthChart(team, player.PlayerId);
        player.Status = "Free Agent";
        player.Morale = Math.Clamp(player.Morale - 8, 0, 100);
        player.MoraleTrend = "Declining";
        player.Contract = new PlayerContractState { ContractType = "Free Agent" };
        league.FreeAgents.Add(player);
        contracts.RefreshCapRoom(league);
        Record(league, "player_released", team, player, "Released to free agency.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player released to free agency.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ReSignPlayer(string playerId, string teamId, ContractOffer offer, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = team?.Roster?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or rostered player was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (!IsValidOffer(offer))
            return Failure("Offer must include 1-5 years, a positive annual salary, and non-negative guarantees.");

        var requiredSalary = GetAdjustedRequirement(league, player, team, contracts);
        var capRoom = contracts.GetCapRoom(team);
        var capRoomAfterReplacingContract = capRoom + Math.Max(0m, player.Contract?.AnnualSalary ?? 0m) - offer.AnnualSalary;
        if (capRoomAfterReplacingContract < 0m)
            return new ContractTransactionResult { Ok = false, Message = "Offer exceeds available cap room.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = capRoom };
        if (offer.AnnualSalary < requiredSalary || offer.GuaranteedSalary < offer.AnnualSalary * 0.15m)
            return new ContractTransactionResult { Ok = true, Accepted = false, Message = "The player declined the extension.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = capRoom };

        player.Contract = BuildContract(league, offer, "Extension");
        player.Morale = Math.Clamp(player.Morale + 5, 0, 100);
        player.MoraleTrend = "Improving";
        contracts.RefreshCapRoom(league);
        Record(league, "contract_extended", team, player, $"{offer.Years}-year extension at {offer.AnnualSalary:0} annually.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player re-signed.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = team.CapRoom };
    }

    public int ProcessContractExpirations(ContractService contracts)
    {
        var league = _context.ActiveLeague;
        if (league == null || league.LastContractExpirationSeason == league.SeasonYear)
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
                RemoveFromDepthChart(team, player.PlayerId);
                player.Status = "Free Agent";
                player.Contract = new PlayerContractState { ContractType = "Free Agent" };
                player.Morale = Math.Clamp(player.Morale - 3, 0, 100);
                player.MoraleTrend = "Declining";
                league.FreeAgents.Add(player);
                Record(league, "contract_expired", team, player, "Contract expired; player entered free agency.");
                expired++;
            }
        }

        contracts.RefreshCapRoom(league);
        league.LastContractExpirationSeason = league.SeasonYear;
        return expired;
    }

    public bool DraftRookie(DraftPickState pick, CollegeProspectState prospect, TeamState team, out string error)
    {
        var league = _context.ActiveLeague;
        if (league == null || pick == null || prospect == null || team == null)
        {
            error = "Draft selection data is incomplete.";
            return false;
        }
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            error = "Draft selections are only available during the draft.";
            return false;
        }
        if (team.Roster.Count >= 90)
        {
            error = "Offseason roster limit of 90 players has been reached.";
            return false;
        }
        if (!string.IsNullOrWhiteSpace(prospect.DraftedByTeamId) || !string.IsNullOrWhiteSpace(pick.ProspectId))
        {
            error = "This prospect or pick has already been used.";
            return false;
        }

        var player = new PlayerState
        {
            PlayerId = $"rookie-{league.SeasonYear}-{prospect.ProspectId}",
            Name = prospect.Name,
            Position = prospect.Position,
            Overall = prospect.Overall,
            Age = prospect.Age,
            Status = "Active",
            Morale = 55,
            MoraleTrend = "Improving",
            Contract = BuildRookieContract(league, pick.Round),
        };
        if (league.Teams.SelectMany(candidate => candidate?.Roster ?? Enumerable.Empty<PlayerState>())
            .Any(candidate => SameId(candidate?.PlayerId, player.PlayerId)))
        {
            error = "This rookie already belongs to a roster.";
            return false;
        }

        pick.ProspectId = prospect.ProspectId;
        pick.PlayerId = player.PlayerId;
        prospect.DraftedByTeamId = team.TeamId;
        team.Roster.Add(player);
        new ContractService(_context).RefreshCapRoom(league);
        Record(league, "draft_pick_made", team, player, $"Round {pick.Round}, pick {pick.PickInRound} (overall {pick.OverallPick}).");
        error = "";
        return true;
    }

    private static TeamState ResolveTeam(LeagueState league, string teamId)
        => league?.Teams?.FirstOrDefault(candidate => SameId(candidate?.TeamId, teamId ?? league.UserTeamId));

    private static bool CanMutateRoster(LeagueState league, out string error)
    {
        var phase = league?.Calendar?.Phase ?? "";
        if (string.Equals(phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase))
        {
            error = "Roster transactions are unavailable until the offseason opens.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool IsValidOffer(ContractOffer offer)
        => offer != null && offer.Years is >= 1 and <= 5 && offer.AnnualSalary > 0m && offer.GuaranteedSalary >= 0m;

    private static decimal GetAdjustedRequirement(LeagueState league, PlayerState player, TeamState team, ContractService contracts)
    {
        var negotiation = league.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.Negotiation ?? 50;
        return Math.Round(contracts.GetRequiredAnnualSalary(player, team) * (1m - Math.Clamp((negotiation - 50) / 600m, -0.05m, 0.05m)), 0, MidpointRounding.AwayFromZero);
    }

    private static PlayerContractState BuildContract(LeagueState league, ContractOffer offer, string type)
        => new() { AnnualSalary = offer.AnnualSalary, GuaranteedSalary = offer.GuaranteedSalary, YearsRemaining = offer.Years, SignedSeason = league.SeasonYear, ContractType = type };

    private static PlayerContractState BuildRookieContract(LeagueState league, int round)
    {
        var annualSalary = Math.Max(550_000m, 850_000m - (Math.Max(1, round) - 1) * 50_000m);
        return new PlayerContractState
        {
            AnnualSalary = annualSalary,
            GuaranteedSalary = annualSalary,
            YearsRemaining = 4,
            SignedSeason = league.SeasonYear,
            ContractType = "Rookie Draft Contract",
        };
    }

    private static void RemoveFromDepthChart(TeamState team, string playerId)
    {
        foreach (var depthChart in team.DepthChart.Values)
            depthChart.RemoveAll(id => SameId(id, playerId));
    }

    private static void Record(LeagueState league, string type, TeamState team, PlayerState player, string details)
    {
        league.Transactions ??= new List<TransactionRecord>();
        league.Transactions.Add(new TransactionRecord
        {
            TransactionId = $"{league.SeasonYear}-{league.Transactions.Count + 1:00000}",
            SeasonYear = league.SeasonYear,
            DateLabel = league.Calendar?.CurrentDate ?? "",
            Phase = league.Calendar?.Phase ?? "",
            Type = type,
            TeamId = team?.TeamId ?? "",
            TeamName = team?.Name ?? "",
            PlayerId = player?.PlayerId ?? "",
            PlayerName = player?.Name ?? "",
            Details = details,
        });
    }

    private static bool SameId(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static ContractTransactionResult Failure(string message) => new() { Ok = false, Message = message };
}
