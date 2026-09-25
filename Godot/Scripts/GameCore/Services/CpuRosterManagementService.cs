using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Proposes moves through the same contract, transaction, training, and depth services as the user.
public sealed partial class CpuRosterManagementService
{
    private readonly GameCoreContext _context;
    private LeagueState League => _context.ActiveLeague;
    private ContractService Contracts => new(_context);
    private TransactionService Transactions => new(_context);
    private static readonly string[] AnnualPhases =
    {
        ScheduleService.OffseasonPendingPhase, ScheduleService.ExclusiveNegotiationPendingPhase,
        ScheduleService.FreeAgencyPendingPhase, ScheduleService.DraftPrepPendingPhase,
        ScheduleService.DraftPendingPhase, ScheduleService.RookieSigningPendingPhase,
        ScheduleService.TrainingCampPendingPhase,
    };

    public CpuRosterManagementService(GameCoreContext context) => _context = context;
    private IEnumerable<TeamState> CpuTeams => League.Teams.Where(team => !Same(team.TeamId, League.UserTeamId))
        .OrderBy(team => team.TeamId, StringComparer.Ordinal);

    public CpuRosterRepairResult ProcessCurrentCheckpoint()
    {
        var result = new CpuRosterRepairResult();
        if (League == null || League.ActiveLiveGameSession?.Active == true) return result;
        var phase = League.Calendar.Phase;
        var annual = AnnualPhases.Contains(phase, StringComparer.OrdinalIgnoreCase);
        if (!annual && phase != "Preseason" && phase != "Regular Season" && phase != ScheduleService.PostseasonPendingPhase) return result;
        var key = annual ? phase : $"weekly:{phase}:{League.Calendar.AbsoluteWeek}";
        var teams = CpuTeams.Where(team => !State(team).CompletedCheckpoints.Contains(key, StringComparer.Ordinal)).ToList();
        if (teams.Count == 0) return result;
        result.TeamsEvaluated = teams.Count;
        var firstTransaction = League.Transactions.Count;
        var market = phase == ScheduleService.FreeAgencyPendingPhase || phase == ScheduleService.TrainingCampPendingPhase;
        var rookie = phase == ScheduleService.RookieSigningPendingPhase;
        var camp = phase == ScheduleService.TrainingCampPendingPhase;
        var canManage = ContractPhaseRules.CanManageRoster(League, out _);
        foreach (var team in teams)
        {
            if (canManage)
            {
                ReleaseIneligibleReserves(team);
                RestoreHealthyReserves(team);
                CutToLimit(team, camp ? RosterService.RosterLimit : RosterService.GetRosterLimit(League));
                RelieveCapPressure(team, DraftReserve(team));
            }
            if (phase == ScheduleService.ExclusiveNegotiationPendingPhase) RetainExpiringPlayers(team);
        }
        // One acquisition per club per round prevents early IDs draining whole position groups.
        if (market || !annual)
            for (var round = 0; round < 24; round++)
            {
                var changed = false;
                foreach (var team in teams) changed |= RepairOneEmergency(team, result);
                if (!changed) break;
            }
        if (market || rookie)
            for (var round = 0; round < (rookie ? 2 : 4); round++)
                foreach (var team in teams) AcquireOne(team, emergencyOnly: false, rookieOnly: rookie, result);
        if (canManage)
        {
            foreach (var waiver in League.Waivers.OrderBy(w => w.Player.PlayerId, StringComparer.Ordinal).ToList()) Transactions.SubmitRecommendedWaiverClaims(waiver);
            Transactions.ExpireWaivers();
            foreach (var team in teams)
            {
                if (camp) CutToLimit(team, RosterService.RosterLimit);
                new DepthChartService(_context).AutoFillDepthChart(team.TeamId);
                if (camp)
                {
                    var focus = FrontOfficeEvaluationService.AssessPositions(team).Where(n => n.Available > 0)
                        .OrderByDescending(n => n.Priority).ThenBy(n => n.Position, StringComparer.Ordinal).FirstOrDefault();
                    var training = new TrainingCampService(_context);
                    if (focus != null && !team.TrainingCamp.FocusApplied) training.ApplyPositionFocus(focus.Position, team.TeamId);
                    if (!team.TrainingCamp.RosterFinalized) training.FinalizeRoster(team.TeamId);
                }
            }
            if (camp || phase == "Preseason")
                for (var round = 0; round < 2; round++) foreach (var team in teams) AddDevelopmentalReserve(team);
        }
        foreach (var team in teams)
        {
            var missing = MissingStarters(team);
            var moves = League.Transactions.Skip(firstTransaction).Count(t => Same(t.TeamId, team.TeamId) && t.Type != "cpu_roster_review");
            if (annual || moves > 0 || missing.Count > 0)
                Transactions.RecordRosterDecision(team, $"CPU {phase} review: {moves} action(s). "
                    + (!canManage ? "Assessment only; roster transactions await their calendar window."
                        : missing.Count > 0 ? $"Unable to resolve legal shortage: {string.Join(", ", missing)}; no valid affordable move available."
                        : moves == 0 ? "Passed: no rule-approved action was warranted at this checkpoint." : "Available starter requirements satisfied."));
            State(team).CompletedCheckpoints.Add(key);
            result.Unresolved.AddRange(missing.Select(position => $"{team.Abbreviation}: {position}"));
        }
        return result;
    }

    // Existing tools retain a structural-repair entry point. Optional moves use checkpoints only.
    public CpuRosterRepairResult RepairCpuStarterShortages()
    {
        var result = new CpuRosterRepairResult();
        if (League == null || !ContractPhaseRules.CanSignFreeAgents(League, out _)) return result;
        foreach (var team in CpuTeams)
        {
            result.TeamsEvaluated++;
            for (var round = 0; round < 24 && RepairOneEmergency(team, result); round++) { }
        }
        return result;
    }

    public bool PrepareForGame(string gameId, string homeTeamId, string awayTeamId, out string error)
    {
        error = "";
        if (League.ActiveLiveGameSession?.Active == true) return true;
        foreach (var team in CpuTeams.Where(team => Same(team.TeamId, homeTeamId) || Same(team.TeamId, awayTeamId)))
        {
            var key = $"game:{gameId}";
            if (!State(team).CompletedCheckpoints.Contains(key))
            {
                RestoreHealthyReserves(team);
                CutToLimit(team, RosterService.RosterLimit);
                RelieveCapPressure(team, 0);
                var result = new CpuRosterRepairResult();
                for (var round = 0; round < 24 && RepairOneEmergency(team, result); round++) { }
                new DepthChartService(_context).AutoFillDepthChart(team.TeamId);
                State(team).CompletedCheckpoints.Add(key);
            }
            var missing = MissingStarters(team);
            if (missing.Count == 0 && team.Roster.Count <= RosterService.RosterLimit && Contracts.GetCapRoom(team) >= 0) continue;
            error = $"{team.Name} cannot field a legal roster: " + (missing.Count > 0 ? $"missing available {string.Join(", ", missing)}. " : "")
                + "No legal roster repair is available; review the roster and market.";
            return false;
        }
        return true;
    }

    public decimal DraftReserve(TeamState team)
    {
        if (League.Calendar.Phase is not (ScheduleService.ExclusiveNegotiationPendingPhase or ScheduleService.FreeAgencyPendingPhase or ScheduleService.DraftPrepPendingPhase or ScheduleService.DraftPendingPhase)) return 0;
        var picks = League.Draft.Picks.Where(pick => Same(pick.TeamId, team.TeamId) && string.IsNullOrWhiteSpace(pick.ProspectId)).ToList();
        return picks.Count == 0 && League.Draft.Picks.Count == 0 ? 4_900_000m : picks.Sum(pick => TransactionService.GetRookieAnnualSalary(pick.Round));
    }

    private CpuRosterState State(TeamState team)
    {
        team.CpuRoster ??= new CpuRosterState();
        if (team.CpuRoster.SeasonYear != League.SeasonYear) team.CpuRoster = new CpuRosterState { SeasonYear = League.SeasonYear };
        return team.CpuRoster;
    }

    public static void NormalizePersistence(LeagueState league, bool legacy)
    {
        foreach (var team in league.Teams)
        {
            team.CpuRoster ??= new CpuRosterState();
            team.CpuRoster.CompletedCheckpoints ??= new List<string>();
            if (!legacy || Same(team.TeamId, league.UserTeamId)) continue;
            team.CpuRoster.SeasonYear = league.SeasonYear;
            if (!ScheduleService.IsOffseasonPlaceholderPhase(league.Calendar.Phase)) continue;
            var current = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(league.Calendar.Phase);
            foreach (var phase in AnnualPhases.Where(phase => ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(phase) <= current))
                if (!team.CpuRoster.CompletedCheckpoints.Contains(phase)) team.CpuRoster.CompletedCheckpoints.Add(phase);
        }
    }

    public static List<string> MissingStarters(TeamState team) => FrontOfficeEvaluationService.AssessPositions(team)
        .Where(n => n.Available < n.RequiredStarters).Select(n => n.Position).ToList();
    private static bool MedicallyAvailable(PlayerState player) => string.IsNullOrWhiteSpace(player.Injury) && player.CurrentInjury?.IsActive != true;
    private static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

public sealed class CpuRosterRepairResult
{
    public int TeamsEvaluated { get; set; }
    public int Signings { get; set; }
    public List<string> Rationales { get; set; } = new();
    public List<string> Unresolved { get; set; } = new();
}
