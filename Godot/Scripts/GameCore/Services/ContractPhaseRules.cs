using System;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class ContractPhaseRules
{
    public static ContractPhaseStatusDto GetStatus(LeagueState league)
    {
        if (league?.ActiveLiveGameSession?.Active == true)
            return new ContractPhaseStatusDto { Explanation = "Finish the live game before making roster or contract transactions." };
        var phase = league?.Calendar?.Phase ?? "";
        var rosterOpen = !IsRosterLocked(phase);
        var isFreeAgency = string.Equals(phase, ScheduleService.FreeAgencyPendingPhase, StringComparison.OrdinalIgnoreCase);
        var isExclusiveNegotiation = string.Equals(phase, ScheduleService.ExclusiveNegotiationPendingPhase, StringComparison.OrdinalIgnoreCase);
        var isFranchiseTag = string.Equals(phase, ScheduleService.FranchiseTagPendingPhase, StringComparison.OrdinalIgnoreCase);
        var isInSeason = string.Equals(phase, "Preseason", StringComparison.OrdinalIgnoreCase)
            || string.Equals(phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(phase, "Regular Season", StringComparison.OrdinalIgnoreCase);
        var isTrainingCamp = string.Equals(phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase);
        var canSign = rosterOpen && (isFreeAgency || isTrainingCamp || isInSeason);
        var canExtend = rosterOpen && (isExclusiveNegotiation || isFreeAgency);

        return new ContractPhaseStatusDto
        {
            CanSignFreeAgents = canSign,
            CanOfferExtensions = canExtend,
            CanApplyFranchiseTag = isFranchiseTag,
            CanManageRoster = rosterOpen,
            Explanation = isExclusiveNegotiation
                ? "Exclusive negotiation: extensions are available; free-agent signings open with the new league year."
                : isFranchiseTag
                    ? "Franchise tag: apply one fully guaranteed one-year tag to an eligible final-year active-roster player, then continue to process expirations."
                : isFreeAgency
                    ? "Free agency: sign players, make extensions, and manage the roster."
                    : isTrainingCamp
                        ? "Training camp: sign players and finalize the 53-player roster; extensions are closed."
                        : isInSeason
                            ? "In season: sign players and manage the roster; extensions reopen next offseason."
                            : rosterOpen
                                ? $"{ScheduleService.GetOffseasonPhaseLabel(phase)}: roster moves are available, but contract offers are closed."
                                : "Roster and contract actions are unavailable during this phase.",
        };
    }

    public static bool CanSignFreeAgents(LeagueState league, out string error)
    {
        var status = GetStatus(league);
        error = status.CanSignFreeAgents ? "" : status.Explanation;
        return status.CanSignFreeAgents;
    }

    public static bool CanSignFreeAgent(LeagueState league, PlayerState player, out string error)
    {
        if (league?.ActiveLiveGameSession?.Active == true)
            return CanSignFreeAgents(league, out error);
        if (string.Equals(league?.Calendar?.Phase, ScheduleService.RookieSigningPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            var allowed = UndraftedFreeAgentService.IsUndraftedRookie(player);
            error = allowed ? "" : "During rookie signing, only undrafted rookies may receive contracts.";
            return allowed;
        }
        return CanSignFreeAgents(league, out error);
    }

    public static bool CanOfferExtensions(LeagueState league, out string error)
    {
        var status = GetStatus(league);
        error = status.CanOfferExtensions ? "" : status.Explanation;
        return status.CanOfferExtensions;
    }

    public static bool CanApplyFranchiseTag(LeagueState league, out string error)
    {
        var status = GetStatus(league);
        error = status.CanApplyFranchiseTag ? "" : status.Explanation;
        return status.CanApplyFranchiseTag;
    }

    public static bool CanManageRoster(LeagueState league, out string error)
    {
        var status = GetStatus(league);
        error = status.CanManageRoster ? "" : status.Explanation;
        return status.CanManageRoster;
    }

    public static bool CanProposeTrades(LeagueState league, out string error)
    {
        if (league?.ActiveLiveGameSession?.Active == true)
        {
            error = "Finish the live game before making trades.";
            return false;
        }
        var phase = league?.Calendar?.Phase ?? "";
        var allowed = string.Equals(phase, ScheduleService.FreeAgencyPendingPhase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(phase, ScheduleService.DraftPrepPendingPhase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase);
        error = allowed ? "" : "Trades are available only during free agency, draft preparation, and the live draft.";
        return allowed;
    }

    private static bool IsRosterLocked(string phase)
        => string.Equals(phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ScheduleService.GetOffseasonPhaseKey(phase), ScheduleService.OffseasonPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ScheduleService.GetOffseasonPhaseKey(phase), ScheduleService.StaffCarouselPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ScheduleService.GetOffseasonPhaseKey(phase), ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ScheduleService.GetOffseasonPhaseKey(phase), ScheduleService.FranchiseTagPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ScheduleService.GetOffseasonPhaseKey(phase), ScheduleService.LeagueYearPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
}
