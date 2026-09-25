using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed partial class CpuRosterManagementService
{
    private readonly Dictionary<string, (int Count, int Season, int Week, HashSet<string> Players)> _departureCache = new(StringComparer.OrdinalIgnoreCase);
    private bool RepairOneEmergency(TeamState team, CpuRosterRepairResult result)
    {
        if (!ContractPhaseRules.CanSignFreeAgents(League, out _) || MissingStarters(team).Count == 0) return false;
        RestoreHealthyReserves(team);
        var need = FrontOfficeEvaluationService.AssessPositions(team).FirstOrDefault(n => n.Available < n.RequiredStarters);
        if (need == null) return false;
        var reserve = team.PracticeSquad.Where(p => Same(p.Position, need.Position) && MedicallyAvailable(p))
            .OrderByDescending(FrontOfficeEvaluationService.PlayerValue).ThenBy(p => p.PlayerId, StringComparer.Ordinal).FirstOrDefault();
        if (reserve != null && team.Roster.Count < RosterService.RosterLimit
            && Transactions.SignPracticeSquadPlayerToActiveRoster(reserve.PlayerId, team.TeamId, Contracts).Accepted)
        {
            Transactions.RecordRosterDecision(team, $"Promoted {reserve.Name}: needed an available {need.Position} starter.");
            return true;
        }
        return AcquireOne(team, true, false, result);
    }

    private bool AcquireOne(TeamState team, bool emergencyOnly, bool rookieOnly, CpuRosterRepairResult result)
    {
        var needs = FrontOfficeEvaluationService.AssessPositions(team);
        var candidates = League.FreeAgents.Where(player => MedicallyAvailable(player) && !RecentlyDeparted(team, player)
                && (!rookieOnly || UndraftedFreeAgentService.IsUndraftedRookie(player)))
            .Select(player => (Player: player, Need: needs.FirstOrDefault(n => Same(n.Position, player.Position))))
            .Where(item => item.Need != null && (!emergencyOnly || item.Need.Available < item.Need.RequiredStarters)
                && IsMeaningfulAcquisition(team, item.Player, item.Need))
            .OrderByDescending(item => item.Need.Priority).ThenByDescending(item => FrontOfficeEvaluationService.PlayerValue(item.Player))
            .ThenBy(item => item.Player.PlayerId, StringComparer.Ordinal).ToList();
        foreach (var (player, need) in candidates)
        {
            if (!ContractPhaseRules.CanSignFreeAgent(League, player, out _)) continue;
            var salary = Contracts.GetRequiredAnnualSalary(player, team);
            var budgetReserve = emergencyOnly ? 0 : DraftReserve(team) + Math.Max(0, needs.Sum(n => Math.Max(0, n.RequiredStarters - n.Available)) - 1) * 1_500_000m;
            var release = ChooseConditionalRelease(team, player, salary + budgetReserve, RosterService.GetRosterLimit(League));
            if (team.Roster.Count >= RosterService.GetRosterLimit(League) && release == null
                || salary + budgetReserve > Contracts.GetCapRoom(team) + (release?.Contract?.AnnualSalary ?? 0m)) continue;
            var reason = need.Available < need.RequiredStarters ? $"CPU roster repair: {need.Position} starter shortage; needed an available starter."
                : need.Available < need.DesiredDepth ? $"CPU added {need.Position} depth for a thin position group."
                : $"CPU added a meaningful {need.Position} upgrade with a developmental or succession opportunity.";
            var offer = new ContractOffer { AnnualSalary = salary, GuaranteedSalary = Math.Ceiling(salary * .20m), Years = UndraftedFreeAgentService.IsUndraftedRookie(player) ? 3 : player.Age <= 27 ? 2 : 1 };
            if (!Transactions.SignFreeAgent(player.PlayerId, team.TeamId, offer, Contracts, reason, release?.PlayerId).Accepted) continue;
            result.Signings++;
            result.Rationales.Add($"{team.Abbreviation}: {reason}");
            return true;
        }
        return false;
    }

    public bool IsMeaningfulAcquisition(TeamState team, PlayerState player, CpuPositionNeed need = null)
        => !RecentlyDeparted(team, player) && FrontOfficeEvaluationService.IsUsefulAcquisition(team, player, need);

    private PlayerState ChooseConditionalRelease(TeamState team, PlayerState incoming, decimal cost, int limit)
        => FrontOfficeEvaluationService.ConditionalRelease(team, incoming, cost, limit, Contracts.GetCapRoom(team));
    public int CutToLimit(TeamState team, int limit)
    {
        if (Same(team.TeamId, League.UserTeamId) || !ContractPhaseRules.CanManageRoster(League, out _)) return 0;
        var cuts = 0;
        while (team.Roster.Count > limit)
        {
            var player = FrontOfficeEvaluationService.CutCandidates(team).FirstOrDefault();
            if (player == null) break;
            var reason = $"CPU final cuts: excess {player.Position} depth; protected available starters and retained stronger developmental options.";
            var move = player.Age <= 25 ? Transactions.PlaceOnWaivers(player.PlayerId, team.TeamId, Contracts, reason)
                : Transactions.ReleasePlayer(player.PlayerId, team.TeamId, Contracts, reason);
            if (!move.Accepted) break;
            cuts++;
        }
        return cuts;
    }

    public void RelieveCapPressure(TeamState team, decimal requiredRoom)
    {
        if (Same(team.TeamId, League.UserTeamId)) return;
        for (var count = 0; count < 53 && Contracts.GetCapRoom(team) < requiredRoom; count++)
        {
            var player = FrontOfficeEvaluationService.CutCandidates(team).Where(p => (p.Contract?.AnnualSalary ?? 0) > 0)
                .OrderByDescending(p => p.Contract.AnnualSalary).ThenBy(FrontOfficeEvaluationService.PlayerValue)
                .ThenBy(p => p.PlayerId, StringComparer.Ordinal).FirstOrDefault();
            if (player == null || !Transactions.ReleasePlayer(player.PlayerId, team.TeamId, Contracts,
                    "CPU cap pressure: released excess depth to preserve a legal budget for the roster and remaining draft contracts.").Accepted) break;
        }
    }

    private void RetainExpiringPlayers(TeamState team)
    {
        foreach (var player in team.Roster.Where(p => p.Contract?.YearsRemaining == 1 && p.Age < 33)
            .OrderByDescending(p => FrontOfficeEvaluationService.IsProtectedStarter(team, p))
            .ThenByDescending(FrontOfficeEvaluationService.PlayerValue).ThenBy(p => p.PlayerId, StringComparer.Ordinal).Take(6).ToList())
        {
            var salary = Contracts.GetRequiredAnnualSalary(player, team);
            if (salary > Contracts.GetCapRoom(team) + player.Contract.AnnualSalary - DraftReserve(team) - 8_000_000m) continue;
            Transactions.ReSignPlayer(player.PlayerId, team.TeamId,
                new ContractOffer { AnnualSalary = salary, GuaranteedSalary = Math.Ceiling(salary * .2m), Years = player.Age <= 27 ? 3 : 2 }, Contracts,
                $"CPU retention: preserved {player.Position} continuity ahead of contract expiration.");
        }
    }

    private void RestoreHealthyReserves(TeamState team)
    {
        if (!ContractPhaseRules.CanManageRoster(League, out _)) return;
        foreach (var player in team.InjuredReserve.Where(MedicallyAvailable).OrderByDescending(FrontOfficeEvaluationService.PlayerValue).ThenBy(p => p.PlayerId, StringComparer.Ordinal).ToList())
        {
            if (team.Roster.Count >= RosterService.RosterLimit)
            {
                var cut = FrontOfficeEvaluationService.CutCandidates(team).FirstOrDefault();
                if (cut == null || !Transactions.ReleasePlayer(cut.PlayerId, team.TeamId, Contracts, "CPU created a roster slot for a recovered reserve player.").Accepted) continue;
            }
            Transactions.ActivateFromInjuredReserve(player.PlayerId, team.TeamId, Contracts);
        }
        // IR preserves cap charges; only genuinely unavailable players can use it.
        if (team.Roster.Count >= RosterService.RosterLimit && MissingStarters(team).Count > 0)
            foreach (var injured in team.Roster.Where(p => !MedicallyAvailable(p) && p.CurrentInjury?.DaysRemaining >= 14).OrderBy(p => p.PlayerId, StringComparer.Ordinal).ToList())
                Transactions.MoveToInjuredReserve(injured.PlayerId, team.TeamId, Contracts);
    }

    private void ReleaseIneligibleReserves(TeamState team)
    {
        foreach (var player in team.PracticeSquad.Where(p => p.Age > 25).OrderBy(p => p.PlayerId, StringComparer.Ordinal).ToList())
            Transactions.ReleasePlayer(player.PlayerId, team.TeamId, Contracts, "CPU released a reserve who no longer meets practice-squad eligibility.");
    }

    private void AddDevelopmentalReserve(TeamState team)
    {
        if (team.PracticeSquad.Count >= 6 || Contracts.GetCapRoom(team) < 2_000_000m) return;
        var player = League.FreeAgents.Where(p => p.Age <= 25 && MedicallyAvailable(p) && !RecentlyDeparted(team, p)
                && team.PracticeSquad.All(reserve => !Same(reserve.Position, p.Position)))
            .OrderBy(p => team.Roster.Count(member => Same(member.Position, p.Position)) - FrontOfficeEvaluationService.DesiredDepth(p.Position))
            .ThenByDescending(FrontOfficeEvaluationService.PlayerValue).ThenBy(p => p.PlayerId, StringComparer.Ordinal).FirstOrDefault();
        if (player != null) Transactions.SignToPracticeSquad(player.PlayerId, team.TeamId, Contracts,
            transactionRationale: $"CPU retained young {player.Position} developmental depth and an internal emergency option.");
    }

    public bool ShouldConfirmWaiver(TeamState team, PlayerState player, PlayerState release)
        => FrontOfficeEvaluationService.ConfirmWaiver(League, team, player, release);
    private bool RecentlyDeparted(TeamState team, PlayerState player)
    {
        if (!_departureCache.TryGetValue(team.TeamId, out var cache) || cache.Count != League.Transactions.Count
            || cache.Season != League.SeasonYear || cache.Week != League.Calendar.AbsoluteWeek)
        {
            var ids = League.Transactions.Where(t => Same(t.TeamId, team.TeamId)
                && FrontOfficeEvaluationService.IsRecentDeparture(League, t))
                .Select(t => t.PlayerId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            cache = (League.Transactions.Count, League.SeasonYear, League.Calendar.AbsoluteWeek, ids);
            _departureCache[team.TeamId] = cache;
        }
        return cache.Players.Contains(player.PlayerId);
    }
}
