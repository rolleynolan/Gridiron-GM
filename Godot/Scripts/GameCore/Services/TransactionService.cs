using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

// Owns rule-approved roster and contract mutations and records each committed change.
public sealed class TransactionService
{
    public const decimal ActiveRosterMinimumSalary = 750_000m;
    private readonly GameCoreContext _context;

    public TransactionService(GameCoreContext context)
    {
        _context = context;
    }

    public ContractTransactionResult SignFreeAgent(string playerId, string teamId, ContractOffer offer, ContractService contracts, string transactionRationale = null)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = league?.FreeAgents?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return Failure("Team or free agent was not found.");
        if (!ContractPhaseRules.CanSignFreeAgent(league, player, out var phaseError))
            return Failure(phaseError);
        if (!IsValidOffer(offer))
            return Failure("Offer must include 1-5 years, a positive annual salary, and non-negative guarantees.");
        var isUndraftedRookie = UndraftedFreeAgentService.IsUndraftedRookie(player);
        if (isUndraftedRookie && offer.Years != 3)
            return Failure("Undrafted rookie contracts require a three-year term.");

        var requiredSalary = GetAdjustedRequirement(league, player, team, contracts);
        var capRoom = contracts.GetCapRoom(team);
        if (offer.AnnualSalary > capRoom)
            return new ContractTransactionResult { Ok = false, Message = "Offer exceeds available cap room.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = capRoom };
        if (offer.AnnualSalary < requiredSalary || offer.GuaranteedSalary < offer.AnnualSalary * 0.15m)
            return new ContractTransactionResult { Ok = true, Accepted = false, Message = "The player declined the offer.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = capRoom };
        if (team.Roster.Count >= RosterService.RosterLimit)
            return Failure($"Roster is full at {RosterService.RosterLimit} players. Release a player before signing.");

        player.Contract = BuildContract(league, offer, isUndraftedRookie ? "Undrafted Rookie Contract" : "Free Agent Signing");
        player.Status = "Active";
        player.Morale = Math.Clamp(player.Morale + 6, 0, 100);
        player.MoraleTrend = "Improving";
        team.Roster.Add(player);
        league.FreeAgents.Remove(player);
        league.RookieMinicamp?.InvitedPlayerIds?.RemoveAll(id => SameId(id, player.PlayerId));
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        var details = $"{offer.Years}-year {(isUndraftedRookie ? "undrafted rookie " : "")}contract at {offer.AnnualSalary:0} annually.";
        if (!string.IsNullOrWhiteSpace(transactionRationale))
            details += $" {transactionRationale.Trim()}";
        Record(league, "free_agent_signed", team, player, details);
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

    public ContractTransactionResult ReleasePlayers(IEnumerable<string> playerIds, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        if (team == null)
            return Failure("Team was not found.");
        if (!CanMutateRoster(league, out var phaseError))
            return Failure(phaseError);

        var requestedIds = (playerIds ?? Enumerable.Empty<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requestedIds.Count == 0)
            return Failure("Select at least one rostered player for the proposed cut list.");

        var players = requestedIds
            .Select(id => team.Roster.FirstOrDefault(candidate => SameId(candidate?.PlayerId, id)))
            .ToList();
        if (players.Any(player => player == null))
            return Failure("The proposed cut list changed. Review the current roster before confirming again.");

        foreach (var player in players)
        {
            team.Roster.Remove(player);
            RemoveFromDepthChart(team, player.PlayerId);
            player.Status = "Free Agent";
            player.Morale = Math.Clamp(player.Morale - 8, 0, 100);
            player.MoraleTrend = "Declining";
            player.Contract = new PlayerContractState { ContractType = "Free Agent" };
            if (!league.FreeAgents.Any(candidate => SameId(candidate?.PlayerId, player.PlayerId)))
                league.FreeAgents.Add(player);
            Record(league, "player_released", team, player, "Released to free agency in the confirmed final cut-down batch.");
        }

        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        return new ContractTransactionResult
        {
            Ok = true,
            Accepted = true,
            Message = $"Confirmed {players.Count} roster cut(s).",
            CapRoomAfterSigning = team.CapRoom,
        };
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
            PlayerId = string.IsNullOrWhiteSpace(prospect.CollegePlayerId) ? $"rookie-{league.SeasonYear}-{prospect.ProspectId}" : prospect.CollegePlayerId,
            Name = prospect.Name,
            Position = prospect.Position,
            Trait = string.IsNullOrWhiteSpace(prospect.Trait) ? "Development-minded" : prospect.Trait,
            College = prospect.College,
            CollegePlayerId = prospect.CollegePlayerId,
            CollegeCareerStats = CopyCollegeCareer(prospect.CollegeCareerStats),
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

        var publicBoardRank = new CollegeBigBoardService(_context).GetStableAnalystRank(prospect.ProspectId, 50);
        var publicReaction = BuildPublicDraftReaction(team, prospect, pick, publicBoardRank);

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
            PublicBoardRank = publicBoardRank,
            PublicReaction = publicReaction,
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

    private static List<CollegePlayerSeasonStats> CopyCollegeCareer(IEnumerable<CollegePlayerSeasonStats> records)
        => (records ?? Enumerable.Empty<CollegePlayerSeasonStats>()).Where(record => record != null).Select(record => new CollegePlayerSeasonStats
        {
            SeasonYear = record.SeasonYear,
            TeamId = record.TeamId,
            GamesPlayed = record.GamesPlayed,
            PassingYards = record.PassingYards,
            RushingYards = record.RushingYards,
            ReceivingYards = record.ReceivingYards,
            Touchdowns = record.Touchdowns,
        }).ToList();

    private static string BuildPublicDraftReaction(TeamState team, CollegeProspectState prospect, DraftPickState pick, int publicBoardRank)
    {
        if (publicBoardRank > 0 && pick.OverallPick >= publicBoardRank + 10)
            return $"Analyst Board viewed {prospect.Name} as value at #{pick.OverallPick} relative to public rank #{publicBoardRank}; this is a draft-night opinion, not a career projection.";
        if (publicBoardRank > 0 && pick.OverallPick + 10 <= publicBoardRank)
            return $"Analyst Board viewed {prospect.Name} as an earlier selection than public rank #{publicBoardRank}; {team.Name} may be prioritizing its own fit and evaluation.";

        var required = DepthChartRules.GetRequiredStarters(prospect.Position);
        var rostered = team.Roster.Count(player => player != null && string.Equals(player.Position, prospect.Position, StringComparison.OrdinalIgnoreCase));
        return rostered <= required
            ? $"Analyst Board notes that {prospect.Name} addresses a thin {prospect.Position} room ({rostered} rostered for {required} required starter slot{(required == 1 ? "" : "s")})."
            : "";
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
        var waiver = new WaiverClaimState
        {
            Player = player,
            WaivedByTeamId = team.TeamId,
            SeasonYear = league.SeasonYear,
            ExpiresAbsoluteWeek = (league.Calendar?.AbsoluteWeek ?? 0) + 1,
        };
        league.Waivers.Add(waiver);
        SeedCpuWaiverClaims(league, waiver, contracts);
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

    public ContractTransactionResult SubmitWaiverClaim(string playerId, string teamId, ContractService contracts, string conditionalReleasePlayerId = null)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var waiver = league?.Waivers?.FirstOrDefault(candidate => SameId(candidate?.Player?.PlayerId, playerId));
        if (team == null || waiver?.Player == null) return Failure("Team or waived player was not found.");
        if (!CanMutateRoster(league, out var phaseError)) return Failure(phaseError);
        if (waiver.DeclinedTeamIds?.Any(id => SameId(id, team.TeamId)) == true) return Failure("This team already declined its confirmation opportunity for that player.");
        waiver.Claims ??= new List<WaiverClaimEntryState>();
        if (waiver.Claims.Any(claim => SameId(claim?.TeamId, team.TeamId))) return Failure("This team already submitted a claim for that player.");
        if (waiver.PendingConfirmation) return Failure("That waiver opportunity is already being resolved.");
        var conditionalRelease = string.IsNullOrWhiteSpace(conditionalReleasePlayerId) ? null : team.Roster.FirstOrDefault(player => SameId(player.PlayerId, conditionalReleasePlayerId));
        if (!string.IsNullOrWhiteSpace(conditionalReleasePlayerId) && conditionalRelease == null) return Failure("The conditional release player is no longer on this roster.");
        var projectedRosterCount = team.Roster.Count - (conditionalRelease == null ? 0 : 1) + 1;
        if (projectedRosterCount > RosterService.RosterLimit) return Failure($"Roster is full at {RosterService.RosterLimit} players. Attach a valid conditional release before submitting the claim.");
        var projectedCapRoom = contracts.GetCapRoom(team) + Math.Max(0m, conditionalRelease?.Contract?.AnnualSalary ?? 0m);
        if ((waiver.Player.Contract?.AnnualSalary ?? 0m) > projectedCapRoom) return Failure("Claim exceeds available cap room even after the conditional release.");

        waiver.Claims.Add(new WaiverClaimEntryState { TeamId = team.TeamId, ConditionalReleasePlayerId = conditionalRelease?.PlayerId ?? "" });
        var releaseNote = conditionalRelease == null ? "No conditional release attached." : $"Conditional release: {conditionalRelease.Name}.";
        Record(league, "waiver_claim_submitted", team, waiver.Player, $"Claim entered for resolution in authoritative waiver order. {releaseNote}");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = $"Claim submitted for resolution when the waiver period closes; no player has transferred. {releaseNote}", CapRoomAfterSigning = contracts.GetCapRoom(team) };
    }

    public ContractTransactionResult FinalizeWaiverClaim(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var waiver = league?.Waivers?.FirstOrDefault(candidate => SameId(candidate?.Player?.PlayerId, playerId));
        if (team == null || waiver?.Player == null || !waiver.PendingConfirmation || !SameId(waiver.PendingClaimTeamId, team.TeamId)) return Failure("No pending waiver confirmation was found for this team and player.");
        if (!CanMutateRoster(league, out var phaseError)) return Failure(phaseError);
        var conditionalRelease = string.IsNullOrWhiteSpace(waiver.ConditionalReleasePlayerId) ? null : team.Roster.FirstOrDefault(player => SameId(player.PlayerId, waiver.ConditionalReleasePlayerId));
        if (!string.IsNullOrWhiteSpace(waiver.ConditionalReleasePlayerId) && conditionalRelease == null) return Failure("The attached conditional release is no longer on this roster. Review the claim before finalizing.");
        var projectedRosterCount = team.Roster.Count - (conditionalRelease == null ? 0 : 1) + 1;
        if (projectedRosterCount > RosterService.RosterLimit) return Failure($"Roster is full at {RosterService.RosterLimit} players. Attach a valid conditional release before finalizing.");
        var projectedCapRoom = contracts.GetCapRoom(team) + Math.Max(0m, conditionalRelease?.Contract?.AnnualSalary ?? 0m);
        if ((waiver.Player.Contract?.AnnualSalary ?? 0m) > projectedCapRoom) return Failure("Claim now exceeds available cap room even after the conditional release.");

        if (conditionalRelease != null)
        {
            team.Roster.Remove(conditionalRelease);
            RemoveFromDepthChart(team, conditionalRelease.PlayerId);
            conditionalRelease.Status = "Free Agent";
            conditionalRelease.Morale = Math.Clamp(conditionalRelease.Morale - 8, 0, 100);
            conditionalRelease.MoraleTrend = "Declining";
            conditionalRelease.Contract = new PlayerContractState { ContractType = "Free Agent" };
            if (!league.FreeAgents.Any(player => SameId(player.PlayerId, conditionalRelease.PlayerId))) league.FreeAgents.Add(conditionalRelease);
        }

        waiver.PendingConfirmation = false;
        waiver.PendingClaimTeamId = "";
        waiver.ConditionalReleasePlayerId = "";
        waiver.Player.Status = "Active";
        team.Roster.Add(waiver.Player);
        league.Waivers.Remove(waiver);
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        if (conditionalRelease != null)
            Record(league, "player_released", team, conditionalRelease, $"Conditionally released after finalizing the waiver claim for {waiver.Player.Name}.");
        Record(league, "waiver_claimed", team, waiver.Player, "Claimed from waivers after explicit final confirmation.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Waiver claim completed.", CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult CancelWaiverClaim(string playerId, string teamId, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var waiver = league?.Waivers?.FirstOrDefault(candidate => SameId(candidate?.Player?.PlayerId, playerId));
        if (team == null || waiver?.Player == null || !waiver.PendingConfirmation || !SameId(waiver.PendingClaimTeamId, team.TeamId)) return Failure("No pending waiver confirmation was found for this team and player.");
        waiver.PendingConfirmation = false;
        waiver.PendingClaimTeamId = "";
        waiver.ConditionalReleasePlayerId = "";
        waiver.DeclinedTeamIds ??= new List<string>();
        if (!waiver.DeclinedTeamIds.Any(id => SameId(id, team.TeamId))) waiver.DeclinedTeamIds.Add(team.TeamId);
        Record(league, "waiver_claim_cancelled", team, waiver.Player, "Declined the winning waiver opportunity; the player remained on waivers for the next eligible claimant or expiry.");
        ResolveWaiverQueue(league, waiver, contracts);
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Waiver opportunity cancelled. The original claim queue advanced to the next eligible claimant or expiry.", CapRoomAfterSigning = contracts.GetCapRoom(team) };
    }

    public int ExpireWaivers()
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return 0;

        var due = league.Waivers
            .Where(waiver => waiver?.Player != null && !waiver.PendingConfirmation && waiver.ExpiresAbsoluteWeek <= (league.Calendar?.AbsoluteWeek ?? 0))
            .ToList();
        var resolved = 0;
        var contracts = new ContractService(_context);
        foreach (var waiver in due)
            if (ResolveWaiverQueue(league, waiver, contracts)) resolved++;
        return resolved;
    }

    private bool ResolveWaiverQueue(LeagueState league, WaiverClaimState waiver, ContractService contracts)
    {
        waiver.Claims ??= new List<WaiverClaimEntryState>();
        waiver.DeclinedTeamIds ??= new List<string>();
        var priority = new StandingsService(_context).BuildStandings(league)
            .AsEnumerable().Reverse().Select((standing, index) => new { standing.TeamId, index })
            .ToDictionary(item => item.TeamId, item => item.index, StringComparer.OrdinalIgnoreCase);
        var orderedClaims = waiver.Claims
            .Where(claim => claim != null && !string.IsNullOrWhiteSpace(claim.TeamId) && !waiver.DeclinedTeamIds.Any(teamId => SameId(teamId, claim.TeamId)))
            .OrderBy(claim => priority.TryGetValue(claim.TeamId, out var rank) ? rank : int.MaxValue)
            .ThenBy(claim => claim.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var claim in orderedClaims)
        {
            var team = ResolveTeam(league, claim.TeamId);
            if (team == null || SameId(team.TeamId, waiver.WaivedByTeamId))
            {
                waiver.DeclinedTeamIds.Add(claim.TeamId);
                continue;
            }
            var conditionalRelease = string.IsNullOrWhiteSpace(claim.ConditionalReleasePlayerId) ? null : team.Roster.FirstOrDefault(player => SameId(player.PlayerId, claim.ConditionalReleasePlayerId));
            var validRelease = string.IsNullOrWhiteSpace(claim.ConditionalReleasePlayerId) || conditionalRelease != null;
            var rosterValid = team.Roster.Count - (conditionalRelease == null ? 0 : 1) + 1 <= RosterService.RosterLimit;
            var capValid = (waiver.Player.Contract?.AnnualSalary ?? 0m) <= contracts.GetCapRoom(team) + Math.Max(0m, conditionalRelease?.Contract?.AnnualSalary ?? 0m);
            if (!validRelease || !rosterValid || !capValid)
            {
                waiver.DeclinedTeamIds.Add(claim.TeamId);
                continue;
            }

            waiver.PendingClaimTeamId = team.TeamId;
            waiver.PendingConfirmation = true;
            waiver.ConditionalReleasePlayerId = claim.ConditionalReleasePlayerId ?? "";
            if (SameId(team.TeamId, league.UserTeamId))
                return false;

            var cpuResult = FinalizeWaiverClaim(waiver.Player.PlayerId, team.TeamId, contracts);
            return cpuResult.Accepted;
        }

        waiver.Player.Status = "Free Agent";
        if (!league.FreeAgents.Any(player => SameId(player.PlayerId, waiver.Player.PlayerId))) league.FreeAgents.Add(waiver.Player);
        league.Waivers.Remove(waiver);
        return true;
    }

    private void SeedCpuWaiverClaims(LeagueState league, WaiverClaimState waiver, ContractService contracts)
    {
        if (waiver?.Player == null)
            return;

        var candidates = new List<(TeamState Team, PlayerState ConditionalRelease, int Improvement)>();
        foreach (var team in league.Teams.Where(team => !SameId(team.TeamId, league.UserTeamId) && !SameId(team.TeamId, waiver.WaivedByTeamId)))
        {
            var replacement = team.Roster
                .Where(player => string.Equals(player.Position, waiver.Player.Position, StringComparison.OrdinalIgnoreCase))
                .OrderBy(player => player.Overall)
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            var improvement = waiver.Player.Overall - (replacement?.Overall ?? 50);
            if (improvement < 4)
                continue;

            var conditionalRelease = team.Roster.Count >= RosterService.RosterLimit
                ? replacement ?? team.Roster.OrderBy(player => player.Overall).ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase).FirstOrDefault()
                : null;
            var projectedCapRoom = contracts.GetCapRoom(team) + Math.Max(0m, conditionalRelease?.Contract?.AnnualSalary ?? 0m);
            if ((waiver.Player.Contract?.AnnualSalary ?? 0m) > projectedCapRoom)
                continue;
            candidates.Add((team, conditionalRelease, improvement));
        }

        foreach (var candidate in candidates.OrderByDescending(candidate => candidate.Improvement).ThenBy(candidate => candidate.Team.TeamId, StringComparer.OrdinalIgnoreCase).Take(4))
        {
            waiver.Claims.Add(new WaiverClaimEntryState
            {
                TeamId = candidate.Team.TeamId,
                ConditionalReleasePlayerId = candidate.ConditionalRelease?.PlayerId ?? "",
            });
            var releaseNote = candidate.ConditionalRelease == null ? "No conditional release." : $"Conditional release: {candidate.ConditionalRelease.Name}.";
            Record(league, "waiver_claim_submitted", candidate.Team, waiver.Player, $"CPU claim entered from a {candidate.Improvement}-point position upgrade evaluation. {releaseNote}");
        }
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

    public decimal GetPracticeSquadRequiredSalary(string playerId, string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = ResolveTeam(league, teamId);
        var player = league?.FreeAgents?.FirstOrDefault(candidate => SameId(candidate?.PlayerId, playerId));
        if (team == null || player == null)
            return 0m;
        var samePositionCount = team.Roster.Count(candidate => string.Equals(candidate.Position, player.Position, StringComparison.OrdinalIgnoreCase))
            + (team.PracticeSquad?.Count(candidate => string.Equals(candidate.Position, player.Position, StringComparison.OrdinalIgnoreCase)) ?? 0);
        var abilityPremium = Math.Max(0, player.Overall - 60) * 15_000m;
        var opportunityAdjustment = samePositionCount <= 2 ? -50_000m : samePositionCount >= 5 ? 75_000m : 0m;
        var moraleAdjustment = player.Morale >= 70 ? 25_000m : player.Morale <= 40 ? -25_000m : 0m;
        return Math.Clamp(300_000m + abilityPremium + opportunityAdjustment + moraleAdjustment, 250_000m, 900_000m);
    }

    public ContractTransactionResult SignToPracticeSquad(string playerId, string teamId, ContractService contracts, decimal annualSalary = 0m)
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
        var requiredSalary = GetPracticeSquadRequiredSalary(playerId, team.TeamId);
        if (annualSalary <= 0m)
            annualSalary = requiredSalary;
        if (annualSalary < 250_000m || annualSalary > 1_000_000m)
            return new ContractTransactionResult { Ok = false, Message = "Practice-squad salary must be between $250,000 and $1,000,000.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = contracts.GetCapRoom(team) };
        if (annualSalary > contracts.GetCapRoom(team))
            return new ContractTransactionResult { Ok = false, Message = "Practice-squad offer exceeds available cap room.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = contracts.GetCapRoom(team) };
        if (annualSalary < requiredSalary)
            return new ContractTransactionResult { Ok = true, Accepted = false, Message = "The player declined the practice-squad offer based on pay and the projected opportunity at their position.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = contracts.GetCapRoom(team) };

        league.FreeAgents.Remove(player);
        player.Status = "Practice Squad";
        player.Contract = new PlayerContractState { AnnualSalary = annualSalary, GuaranteedSalary = 0m, YearsRemaining = 1, SignedSeason = league.SeasonYear, ContractType = "Practice Squad" };
        team.PracticeSquad.Add(player);
        contracts.RefreshCapRoom(league);
        Record(league, "practice_squad_signed", team, player, $"Accepted a one-year practice-squad offer at {GameCoreStateHelper.FormatCapRoom(annualSalary)} annually after evaluating pay and positional opportunity.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Player accepted the practice-squad offer.", RequiredAnnualSalary = requiredSalary, CapRoomAfterSigning = team.CapRoom };
    }

    public ContractTransactionResult ElevatePracticeSquadPlayer(string playerId, string teamId, ContractService contracts)
        => SignPracticeSquadPlayerToActiveRoster(playerId, teamId, contracts);

    public ContractTransactionResult SignPracticeSquadPlayerToActiveRoster(string playerId, string teamId, ContractService contracts)
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

        var currentSalary = Math.Max(0m, player.Contract?.AnnualSalary ?? 0m);
        var activeSalary = Math.Max(ActiveRosterMinimumSalary, currentSalary);
        if (contracts.GetCapRoom(team) + currentSalary - activeSalary < 0m)
            return Failure("The permanent active-roster contract would exceed available cap room.");

        team.PracticeSquad.Remove(player);
        player.Status = "Active";
        player.Contract = new PlayerContractState
        {
            AnnualSalary = activeSalary,
            GuaranteedSalary = 0m,
            YearsRemaining = Math.Max(1, player.Contract?.YearsRemaining ?? 1),
            SignedSeason = league.SeasonYear,
            ContractType = "Active Roster",
        };
        team.Roster.Add(player);
        InvalidateTrainingCampFinalization(league, team);
        contracts.RefreshCapRoom(league);
        Record(league, "practice_squad_signed_active", team, player, $"Signed permanently to the active roster at {activeSalary:0} annually.");
        return new ContractTransactionResult { Ok = true, Accepted = true, Message = "Practice-squad player signed permanently to the active roster.", CapRoomAfterSigning = team.CapRoom };
    }

    public void RecordTrainingCampDecision(TeamState team, string details)
    {
        var league = _context.ActiveLeague;
        if (league != null && team != null)
            Record(league, "training_camp_decision", team, null, details);
    }

    public TradeProposalResult SubmitUserTradeProposal(TradeProposal proposal, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        if (league == null || proposal == null)
            return TradeFailure("No active trade proposal is available.");
        if (!ContractPhaseRules.CanProposeTrades(league, out var phaseError))
            return TradeFailure(phaseError);
        if (!SameId(proposal.ProposingTeamId, league.UserTeamId))
            return TradeFailure("Only the user-controlled team can submit a trade proposal.");

        new DraftService(_context).PrepareDraftBoard();
        var proposer = ResolveTeam(league, proposal.ProposingTeamId);
        var receiver = ResolveTeam(league, proposal.ReceivingTeamId);
        if (proposer == null || receiver == null || SameId(proposer.TeamId, receiver.TeamId))
            return TradeFailure("Choose a different valid counterparty team.");

        var offeredPlayers = ResolveTradePlayers(proposer, proposal.ProposingPlayerIds, out var playerError);
        if (!string.IsNullOrWhiteSpace(playerError))
            return TradeFailure(playerError);
        var requestedPlayers = ResolveTradePlayers(receiver, proposal.ReceivingPlayerIds, out playerError);
        if (!string.IsNullOrWhiteSpace(playerError))
            return TradeFailure(playerError);
        var offeredPicks = ResolveTradePicks(league, proposer, proposal.ProposingPickOverallNumbers, out var pickError);
        if (!string.IsNullOrWhiteSpace(pickError))
            return TradeFailure(pickError);
        var requestedPicks = ResolveTradePicks(league, receiver, proposal.ReceivingPickOverallNumbers, out pickError);
        if (!string.IsNullOrWhiteSpace(pickError))
            return TradeFailure(pickError);
        if (offeredPlayers.Count + offeredPicks.Count == 0 || requestedPlayers.Count + requestedPicks.Count == 0)
            return TradeFailure("Each team must exchange at least one player or unused draft pick.");
        if (proposer.Roster.Count - offeredPlayers.Count + requestedPlayers.Count > RosterService.RosterLimit
            || receiver.Roster.Count - requestedPlayers.Count + offeredPlayers.Count > RosterService.RosterLimit)
            return TradeFailure($"Trade would exceed the {RosterService.RosterLimit}-player active-roster limit.");
        if (!HasCapRoomAfterTrade(league, proposer, offeredPlayers, requestedPlayers)
            || !HasCapRoomAfterTrade(league, receiver, requestedPlayers, offeredPlayers))
            return TradeFailure("Trade would exceed available salary-cap space.");

        var offeredValue = GetTradeValue(offeredPlayers, offeredPicks, league.Teams.Count);
        var requestedValue = GetTradeValue(requestedPlayers, requestedPicks, league.Teams.Count);
        var rationale = $"{receiver.Name} values your offer at {offeredValue} versus {requestedValue} requested; acceptance requires at least 92% of the requested value.";
        if (offeredValue * 100 < requestedValue * 92)
            return new TradeProposalResult { Ok = true, Accepted = false, Message = "Trade rejected.", Rationale = rationale, OfferedValue = offeredValue, RequestedValue = requestedValue };

        MovePlayers(proposer, receiver, offeredPlayers);
        MovePlayers(receiver, proposer, requestedPlayers);
        foreach (var pick in offeredPicks)
            pick.TeamId = receiver.TeamId;
        foreach (var pick in requestedPicks)
            pick.TeamId = proposer.TeamId;
        InvalidateTrainingCampFinalization(league, proposer);
        InvalidateTrainingCampFinalization(league, receiver);
        contracts.RefreshCapRoom(league);
        var details = $"Sent {DescribeAssets(offeredPlayers, offeredPicks)}; received {DescribeAssets(requestedPlayers, requestedPicks)}. {rationale}";
        Record(league, "trade_accepted", proposer, null, details);
        Record(league, "trade_accepted", receiver, null, $"Received {DescribeAssets(offeredPlayers, offeredPicks)}; sent {DescribeAssets(requestedPlayers, requestedPicks)}.");
        league.TradeMarket = new TradeMarketState();
        return new TradeProposalResult { Ok = true, Accepted = true, Message = "Trade accepted and completed.", Rationale = rationale, OfferedValue = offeredValue, RequestedValue = requestedValue };
    }

    // Deliberately read-only. Submission remains the sole transaction path.
    public TradeProposalPreview PreviewUserTradeProposal(TradeProposal proposal, ContractService contracts)
    {
        var league = _context.ActiveLeague;
        if (league == null || proposal == null)
            return PreviewFailure("No active trade proposal is available.");
        if (!ContractPhaseRules.CanProposeTrades(league, out var phaseError))
            return PreviewFailure(phaseError);
        if (!SameId(proposal.ProposingTeamId, league.UserTeamId))
            return PreviewFailure("Only the user-controlled team can preview a trade proposal.");

        var proposer = ResolveTeam(league, proposal.ProposingTeamId);
        var receiver = ResolveTeam(league, proposal.ReceivingTeamId);
        if (proposer == null || receiver == null || SameId(proposer.TeamId, receiver.TeamId))
            return PreviewFailure("Choose a different valid counterparty team.");
        var offeredPlayers = ResolveTradePlayers(proposer, proposal.ProposingPlayerIds, out var playerError);
        if (!string.IsNullOrWhiteSpace(playerError)) return PreviewFailure(playerError);
        var requestedPlayers = ResolveTradePlayers(receiver, proposal.ReceivingPlayerIds, out playerError);
        if (!string.IsNullOrWhiteSpace(playerError)) return PreviewFailure(playerError);
        var offeredPicks = ResolveTradePicks(league, proposer, proposal.ProposingPickOverallNumbers, out var pickError);
        if (!string.IsNullOrWhiteSpace(pickError)) return PreviewFailure(pickError);
        var requestedPicks = ResolveTradePicks(league, receiver, proposal.ReceivingPickOverallNumbers, out pickError);
        if (!string.IsNullOrWhiteSpace(pickError)) return PreviewFailure(pickError);
        if (offeredPlayers.Count + offeredPicks.Count == 0 || requestedPlayers.Count + requestedPicks.Count == 0)
            return PreviewFailure("Each team must exchange at least one player or unused draft pick.");

        var proposerRosterAfter = proposer.Roster.Count - offeredPlayers.Count + requestedPlayers.Count;
        var receiverRosterAfter = receiver.Roster.Count - requestedPlayers.Count + offeredPlayers.Count;
        var proposerCapAfter = GetCapRoomAfterTrade(league, proposer, offeredPlayers, requestedPlayers);
        var receiverCapAfter = GetCapRoomAfterTrade(league, receiver, requestedPlayers, offeredPlayers);
        var rosterValid = proposerRosterAfter <= RosterService.RosterLimit && receiverRosterAfter <= RosterService.RosterLimit;
        var capValid = proposerCapAfter >= 0m && receiverCapAfter >= 0m;
        var offeredValue = GetTradeValue(offeredPlayers, offeredPicks, league.Teams.Count);
        var requestedValue = GetTradeValue(requestedPlayers, requestedPicks, league.Teams.Count);
        var threshold = Math.Ceiling(requestedValue * 0.92m);
        var rationale = $"Package value: offer {offeredValue}, request {requestedValue}; {receiver.Name} requires at least {threshold:0}. " +
            $"Projected roster: {proposer.Name} {proposerRosterAfter}/{RosterService.RosterLimit}, {receiver.Name} {receiverRosterAfter}/{RosterService.RosterLimit}. " +
            $"Projected cap room: {proposer.Name} {GameCoreStateHelper.FormatCapRoom(proposerCapAfter)}, {receiver.Name} {GameCoreStateHelper.FormatCapRoom(receiverCapAfter)}.";
        var canSubmit = rosterValid && capValid;
        var message = !rosterValid ? $"Preview blocks submission: trade would exceed the {RosterService.RosterLimit}-player active-roster limit."
            : !capValid ? "Preview blocks submission: trade would exceed available salary-cap space."
            : "Preview is valid. Submit explicitly to request the counterparty decision.";
        return new TradeProposalPreview { Ok = true, CanSubmit = canSubmit, Message = message, Rationale = rationale, OfferedValue = offeredValue, RequestedValue = requestedValue, ProposerRosterAfter = proposerRosterAfter, ReceiverRosterAfter = receiverRosterAfter, ProposerCapRoomAfter = proposerCapAfter, ReceiverCapRoomAfter = receiverCapAfter };
    }

    private static TeamState ResolveTeam(LeagueState league, string teamId)
        => league?.Teams?.FirstOrDefault(candidate => SameId(candidate?.TeamId, teamId ?? league.UserTeamId));

    private static bool CanMutateRoster(LeagueState league, out string error)
    {
        return ContractPhaseRules.CanManageRoster(league, out error);
    }

    private static List<PlayerState> ResolveTradePlayers(TeamState team, IEnumerable<string> playerIds, out string error)
    {
        error = "";
        var ids = (playerIds ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count != (playerIds ?? Enumerable.Empty<string>()).Count(id => !string.IsNullOrWhiteSpace(id)))
        {
            error = "A player cannot be included more than once.";
            return new List<PlayerState>();
        }
        var players = ids.Select(id => team.Roster.FirstOrDefault(player => SameId(player?.PlayerId, id))).ToList();
        if (players.Any(player => player == null))
        {
            error = "Every traded player must belong to the offering team's active roster.";
            return new List<PlayerState>();
        }
        return players;
    }

    private static List<DraftPickState> ResolveTradePicks(LeagueState league, TeamState team, IEnumerable<int> overallNumbers, out string error)
    {
        error = "";
        var numbers = (overallNumbers ?? Enumerable.Empty<int>()).Where(number => number > 0).Distinct().ToList();
        if (numbers.Count != (overallNumbers ?? Enumerable.Empty<int>()).Count(number => number > 0))
        {
            error = "A draft pick cannot be included more than once.";
            return new List<DraftPickState>();
        }
        var picks = numbers.Select(number => league.Draft.Picks.FirstOrDefault(pick => pick != null && pick.OverallPick == number)).ToList();
        if (picks.Any(pick => pick == null || !SameId(pick.TeamId, team.TeamId) || !string.IsNullOrWhiteSpace(pick.ProspectId)))
        {
            error = "Every traded pick must be an unused pick owned by the offering team.";
            return new List<DraftPickState>();
        }
        return picks;
    }

    private static bool HasCapRoomAfterTrade(LeagueState league, TeamState team, IEnumerable<PlayerState> outgoing, IEnumerable<PlayerState> incoming)
        => GetCapRoomAfterTrade(league, team, outgoing, incoming) >= 0m;

    private static decimal GetCapRoomAfterTrade(LeagueState league, TeamState team, IEnumerable<PlayerState> outgoing, IEnumerable<PlayerState> incoming)
    {
        var currentSalary = team.Roster.Sum(player => Math.Max(0m, player?.Contract?.AnnualSalary ?? 0m));
        var outgoingSalary = outgoing.Sum(player => Math.Max(0m, player?.Contract?.AnnualSalary ?? 0m));
        var incomingSalary = incoming.Sum(player => Math.Max(0m, player?.Contract?.AnnualSalary ?? 0m));
        return league.SalaryCap - (currentSalary - outgoingSalary + incomingSalary);
    }

    private static int GetTradeValue(IEnumerable<PlayerState> players, IEnumerable<DraftPickState> picks, int teamCount)
        => players.Sum(player => Math.Max(0, player.Overall * 100 + player.Potential * 15 + (player.Age <= 25 ? 150 : player.Age >= 31 ? -100 : 0)))
            + picks.Sum(pick => Math.Max(100, (8 - pick.Round) * 1_200 + Math.Max(0, teamCount - pick.PickInRound) * 10));

    public static int GetReadOnlyTradeAssetValue(IEnumerable<PlayerState> players, IEnumerable<DraftPickState> picks, int teamCount)
        => GetTradeValue(players ?? Enumerable.Empty<PlayerState>(), picks ?? Enumerable.Empty<DraftPickState>(), teamCount);

    private static void MovePlayers(TeamState from, TeamState to, IEnumerable<PlayerState> players)
    {
        foreach (var player in players)
        {
            from.Roster.Remove(player);
            RemoveFromDepthChart(from, player.PlayerId);
            player.Status = "Active";
            to.Roster.Add(player);
        }
    }

    private static string DescribeAssets(IEnumerable<PlayerState> players, IEnumerable<DraftPickState> picks)
    {
        var names = players.Select(player => player.Name).Concat(picks.Select(pick => $"{pick.Round}-{pick.PickInRound} pick (#{pick.OverallPick})"));
        return string.Join(", ", names);
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

    private static TradeProposalResult TradeFailure(string message) => new() { Ok = false, Message = message, Rationale = message };
    private static TradeProposalPreview PreviewFailure(string message) => new() { Ok = false, Message = message, Rationale = message };
}
