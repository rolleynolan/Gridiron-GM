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
        if (!ContractPhaseRules.CanSignFreeAgents(league, out var phaseError))
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
        InvalidateTrainingCampFinalization(league, team);
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
        InvalidateTrainingCampFinalization(league, team);
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
        if (!ContractPhaseRules.CanOfferExtensions(league, out var phaseError))
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

    public ContractTransactionResult ApplyFranchiseTag(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = team?.Roster?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Franchise tags apply only to a player on the active roster.");
        if (!ContractPhaseRules.CanApplyFranchiseTag(league, out var phaseError))
            return Failure(phaseError);
        if (team.FranchiseTagSeason == league.SeasonYear || !string.IsNullOrWhiteSpace(team.FranchiseTagPlayerId))
            return Failure("This team has already used its franchise tag this season.");
        if (team.Roster.Count > RosterService.RosterLimit)
            return Failure($"Roster exceeds the {RosterService.RosterLimit}-player active limit. Resolve the roster before applying a franchise tag.");
        if (player.Contract == null || player.Contract.YearsRemaining != 1)
            return Failure("Only an active-roster player entering the final contract year is eligible for the franchise tag.");

        var tagSalary = GetFranchiseTagSalary(player, team, contracts);
        var capRoomAfterReplacingContract = contracts.GetCapRoom(team) + Math.Max(0m, player.Contract.AnnualSalary) - tagSalary;
        if (capRoomAfterReplacingContract < 0m)
            return new ContractTransactionResult { Ok = false, Message = "Franchise tag exceeds available cap room.", RequiredAnnualSalary = tagSalary, CapRoomAfterSigning = contracts.GetCapRoom(team) };

        player.Contract = new PlayerContractState
        {
            AnnualSalary = tagSalary,
            GuaranteedSalary = tagSalary,
            YearsRemaining = 1,
            SignedSeason = league.SeasonYear,
            ContractType = "Franchise Tag",
        };
        player.Morale = Math.Clamp(player.Morale + 3, 0, 100);
        player.MoraleTrend = "Improving";
        team.FranchiseTagSeason = league.SeasonYear;
        team.FranchiseTagPlayerId = player.PlayerId;
        contracts.RefreshCapRoom(league);
        Record(league, "franchise_tag_applied", team, player, $"One-year fully guaranteed franchise tag at {tagSalary:0}.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = $"Franchise tag applied at {tagSalary:0} for one fully guaranteed year.", RequiredAnnualSalary = tagSalary, CapRoomAfterSigning = team.CapRoom };
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
                if (string.Equals(player.Contract.ContractType, "Franchise Tag", StringComparison.OrdinalIgnoreCase)
                    && player.Contract.SignedSeason == league.SeasonYear)
                    continue;

                player.Contract.YearsRemaining--;
                if (player.Contract.YearsRemaining > 0)
                    continue;

                team.Roster.Remove(player);
                RemoveFromDepthChart(team, player.PlayerId);
                player.Status = "Free Agent";
                var wasFranchiseTag = string.Equals(player.Contract.ContractType, "Franchise Tag", StringComparison.OrdinalIgnoreCase);
                player.Contract = new PlayerContractState { ContractType = "Free Agent" };
                player.Morale = Math.Clamp(player.Morale - 3, 0, 100);
                player.MoraleTrend = "Declining";
                league.FreeAgents.Add(player);
                InvalidateTrainingCampFinalization(league, team);
                Record(league, wasFranchiseTag ? "franchise_tag_expired" : "contract_expired", team, player, wasFranchiseTag ? "Franchise tag expired; player entered free agency." : "Contract expired; player entered free agency.");
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
            Potential = prospect.Potential,
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
        league.Draft.RecapEntries ??= new List<DraftClassRecapEntry>();
        league.Draft.RecapEntries.Add(new DraftClassRecapEntry
        {
            OverallPick = pick.OverallPick,
            Round = pick.Round,
            PickInRound = pick.PickInRound,
            TeamId = team.TeamId,
            TeamName = team.Name,
            ProspectId = prospect.ProspectId,
            PlayerId = player.PlayerId,
            Name = prospect.Name,
            Position = prospect.Position,
            College = prospect.College,
            Age = prospect.Age,
            ScoutedOverall = prospect.ScoutedOverall,
            ScoutedPotential = prospect.ScoutedPotential,
            ScoutingConfidence = prospect.ScoutingConfidence,
            CombineScore = prospect.CombineScore,
            ProDayScore = prospect.ProDayScore,
            ScoutingReport = prospect.ScoutingReport,
            Trait = prospect.Trait,
            InterviewSummary = prospect.InterviewSummary,
            RookiePlacement = "Active Roster",
            ContractAnnualSalary = player.Contract.AnnualSalary,
            ContractGuaranteedSalary = player.Contract.GuaranteedSalary,
            ContractYears = player.Contract.YearsRemaining,
            ContractType = player.Contract.ContractType,
        });
        new ContractService(_context).RefreshCapRoom(league);
        Record(league, "draft_pick_made", team, player, $"Round {pick.Round}, pick {pick.PickInRound} (overall {pick.OverallPick}).");
        error = "";
        return true;
    }

    public ContractTransactionResult PlaceOnWaivers(string playerId, string teamId, ContractService contracts)
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
        player.Status = "Waived";
        league.Waivers ??= new List<WaiverClaimState>();
        league.Waivers.Add(new WaiverClaimState
        {
            Player = player,
            WaivedByTeamId = team.TeamId,
            SeasonYear = league.SeasonYear,
            ExpiresAbsoluteWeek = (league.Calendar?.AbsoluteWeek ?? 0) + 1,
        });
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        Record(league, "player_waived", team, player, "Placed on waivers for one league week.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player placed on waivers.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ClaimWaiver(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var waiver = league?.Waivers?.FirstOrDefault(candidate => SameId(candidate?.Player?.PlayerId, playerId));
        if (team == null || waiver?.Player == null)
            return Failure("Team or waived player was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (team.Roster.Count >= RosterService.RosterLimit)
            return Failure($"Roster is full at {RosterService.RosterLimit} players. Release a player before claiming waivers.");
        if ((waiver.Player.Contract?.AnnualSalary ?? 0m) > contracts.GetCapRoom(team))
            return Failure("Claim exceeds available cap room.");

        waiver.Player.Status = "Active";
        team.Roster.Add(waiver.Player);
        league.Waivers.Remove(waiver);
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        Record(league, "waiver_claimed", team, waiver.Player, "Claimed from waivers.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Waiver claim completed.", CapRoomAfterSigning = team.CapRoom };
    }

    public int ExpireWaivers()
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return 0;

        var expired = league.Waivers
            .Where(waiver => waiver?.Player != null && waiver.ExpiresAbsoluteWeek <= (league.Calendar?.AbsoluteWeek ?? 0))
            .ToList();
        foreach (var waiver in expired)
        {
            waiver.Player.Status = "Free Agent";
            league.FreeAgents.Add(waiver.Player);
            league.Waivers.Remove(waiver);
        }
        return expired.Count;
    }

    public ContractTransactionResult MoveToInjuredReserve(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = team?.Roster?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or rostered player was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (string.IsNullOrWhiteSpace(player.Injury))
            return Failure("Only injured players can be moved to injured reserve.");

        team.Roster.Remove(player);
        RemoveFromDepthChart(team, player.PlayerId);
        player.Status = "IR";
        team.InjuredReserve ??= new List<PlayerState>();
        team.InjuredReserve.Add(player);
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        Record(league, "moved_to_ir", team, player, "Moved to injured reserve.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player moved to injured reserve.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ActivateFromInjuredReserve(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = team?.InjuredReserve?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or injured-reserve player was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (!string.IsNullOrWhiteSpace(player.Injury))
            return Failure("Player cannot be activated until the injury is cleared.");
        if (team.Roster.Count >= RosterService.RosterLimit)
            return Failure($"Roster is full at {RosterService.RosterLimit} players. Create an active-roster slot first.");

        team.InjuredReserve.Remove(player);
        player.Status = "Active";
        team.Roster.Add(player);
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        Record(league, "activated_from_ir", team, player, "Activated from injured reserve.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player activated from injured reserve.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult SignToPracticeSquad(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = league?.FreeAgents?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or free agent was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (player.Age > 25)
            return Failure("Practice squad eligibility is limited to players age 25 or younger.");
        team.PracticeSquad ??= new List<PlayerState>();
        if (team.PracticeSquad.Count >= 16)
            return Failure("Practice squad is full at 16 players.");

        league.FreeAgents.Remove(player);
        player.Status = "Practice Squad";
        player.Contract = new PlayerContractState { AnnualSalary = 300_000m, GuaranteedSalary = 0m, YearsRemaining = 1, SignedSeason = league.SeasonYear, ContractType = "Practice Squad" };
        team.PracticeSquad.Add(player);
        contracts.RefreshCapRoom(league);
        Record(league, "practice_squad_signed", team, player, "Signed to the practice squad.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player signed to practice squad.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ElevatePracticeSquadPlayer(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = team?.PracticeSquad?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or practice-squad player was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);
        if (team.Roster.Count >= RosterService.RosterLimit)
            return Failure($"Roster is full at {RosterService.RosterLimit} players. Create an active-roster slot first.");

        team.PracticeSquad.Remove(player);
        player.Status = "Active";
        team.Roster.Add(player);
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        Record(league, "practice_squad_elevated", team, player, "Elevated to the active roster.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Practice-squad player elevated.", CapRoomAfterSigning = team.CapRoom };
    }

    public void RecordTrainingCampDecision(TeamState team, string details)
    {
        var league = _context.ActiveLeague;
        if (league != null && team != null)
            Record(league, "training_camp_decision", team, null, details);
    }

    private static TeamState ResolveTeam(LeagueState league, string teamId)
        => league?.Teams?.FirstOrDefault(candidate => SameId(candidate?.TeamId, teamId ?? league.UserTeamId));

    private static bool CanMutateRoster(LeagueState league, out string error)
    {
        return ContractPhaseRules.CanManageRoster(league, out error);
    }

    private static bool IsValidOffer(ContractOffer offer)
        => offer != null && offer.Years is >= 1 and <= 5 && offer.AnnualSalary > 0m && offer.GuaranteedSalary >= 0m;

    private static decimal GetAdjustedRequirement(LeagueState league, PlayerState player, TeamState team, ContractService contracts)
    {
        var negotiation = league.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.Negotiation ?? 50;
        return Math.Round(contracts.GetRequiredAnnualSalary(player, team) * (1m - Math.Clamp((negotiation - 50) / 600m, -0.05m, 0.05m)), 0, MidpointRounding.AwayFromZero);
    }

    private static decimal GetFranchiseTagSalary(PlayerState player, TeamState team, ContractService contracts)
    {
        var requiredSalary = contracts.GetRequiredAnnualSalary(player, team);
        var currentSalary = Math.Max(0m, player.Contract?.AnnualSalary ?? 0m);
        return Math.Round(Math.Max(requiredSalary * 1.20m, currentSalary * 1.25m), 0, MidpointRounding.AwayFromZero);
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

    private static void InvalidateTrainingCampFinalization(LeagueState league, TeamState team)
    {
        if (string.Equals(league?.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase)
            && team?.TrainingCamp?.RosterFinalized == true)
        {
            team.TrainingCamp.RosterFinalized = false;
            team.TrainingCamp.Summary = "Roster changed after finalization; review and finalize training camp again.";
        }
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
