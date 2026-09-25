using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class CpuRosterHealthReport
{
    public int SeasonYear { get; init; }
    public int MissingStarterTeams { get; init; }
    public int IllegalRosterSizes { get; init; }
    public int CapViolations { get; init; }
    public int DuplicateOwnership { get; init; }
    public int InvalidReserveContracts { get; init; }
    public int InvalidDepthEntries { get; init; }
    public int FreeAgents { get; init; }
    public int RetiredPlayersStillOwned { get; init; }
    public int UnsignedRetirements { get; init; }
    public int LongTermFreeAgents { get; init; }
    public int OldestFreeAgent { get; init; }
    public double AverageTransactions { get; init; }
    public List<string> Unresolved { get; init; } = new();
    public List<string> PositionDepth { get; init; } = new();
    public bool StructurallyValid => IllegalRosterSizes + CapViolations + DuplicateOwnership + InvalidReserveContracts + InvalidDepthEntries + RetiredPlayersStillOwned == 0;
    public override string ToString() => $"{SeasonYear}: missing starters={MissingStarterTeams}, illegal sizes={IllegalRosterSizes}, cap violations={CapViolations}, duplicate ownership={DuplicateOwnership}, reserve violations={InvalidReserveContracts}, invalid depth={InvalidDepthEntries}, retired still owned={RetiredPlayersStillOwned}; transactions/CPU={AverageTransactions:0.0}, free agents={FreeAgents}, unsigned retirements={UnsignedRetirements}, unsigned 3+ years={LongTermFreeAgents}, oldest FA={OldestFreeAgent}. Depth min/avg/max: {string.Join("; ", PositionDepth)}. Unresolved: {(Unresolved.Count == 0 ? "none" : string.Join("; ", Unresolved))}";
}

public static class CpuRosterDiagnosticService
{
    public static CpuRosterHealthReport Analyze(GameCoreContext context, int transactionSeason = 0)
    {
        var league = context.ActiveLeague;
        var teams = league.Teams.Where(t => t.TeamId != league.UserTeamId).ToList();
        var contracts = new ContractService(context);
        var players = league.Teams.SelectMany(t => t.Roster.Concat(t.InjuredReserve).Concat(t.PracticeSquad))
            .Concat(league.FreeAgents).Concat(league.Waivers.Select(w => w.Player)).ToList();
        var missing = teams.SelectMany(t => CpuRosterManagementService.MissingStarters(t).Select(p => $"{t.Abbreviation} {p}")).ToList();
        var retirements = league.RetirementHistory.SelectMany(s => s.Players).ToList();
        var retiredIds = retirements.Select(r => r.PlayerId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return new CpuRosterHealthReport
        {
            SeasonYear = league.SeasonYear,
            MissingStarterTeams = teams.Count(t => CpuRosterManagementService.MissingStarters(t).Count > 0),
            IllegalRosterSizes = teams.Count(t => t.Roster.Count > RosterService.GetRosterLimit(league) || t.PracticeSquad.Count > 16),
            CapViolations = teams.Count(t => contracts.GetCommittedSalary(t) > league.SalaryCap),
            DuplicateOwnership = players.GroupBy(p => p.PlayerId, StringComparer.OrdinalIgnoreCase).Count(g => string.IsNullOrWhiteSpace(g.Key) || g.Count() > 1),
            InvalidReserveContracts = teams.Sum(t => t.PracticeSquad.Count(p => p.Age > 25 || p.Status != "Practice Squad" || p.Contract?.ContractType != "Practice Squad" || p.Contract.YearsRemaining <= 0)
                + t.InjuredReserve.Count(p => p.Status != "IR" || p.Contract?.YearsRemaining <= 0)
                + t.Roster.Count(p => p.Contract?.YearsRemaining <= 0 || p.Contract?.ContractType == "Practice Squad")),
            InvalidDepthEntries = teams.Sum(t => t.DepthChart.Sum(pair => pair.Value.Count(id => !t.Roster.Any(p => p.PlayerId == id && p.Position == pair.Key))
                + pair.Value.Count - pair.Value.Distinct(StringComparer.OrdinalIgnoreCase).Count())),
            FreeAgents = league.FreeAgents.Count,
            RetiredPlayersStillOwned = players.Count(p => retiredIds.Contains(p.PlayerId)),
            UnsignedRetirements = retirements.Count(r => r.SeasonYear == (transactionSeason == 0 ? league.SeasonYear : transactionSeason) && string.IsNullOrWhiteSpace(r.TeamId)),
            LongTermFreeAgents = league.FreeAgents.Count(p => p.UnsignedSinceSeasonYear > 0 && league.SeasonYear - p.UnsignedSinceSeasonYear >= 3),
            OldestFreeAgent = league.FreeAgents.Select(p => p.Age).DefaultIfEmpty().Max(),
            AverageTransactions = teams.Count == 0 ? 0 : league.Transactions.Count(t => t.SeasonYear == (transactionSeason == 0 ? league.SeasonYear : transactionSeason)
                && teams.Any(team => team.TeamId == t.TeamId) && t.Type is not ("cpu_roster_review" or "training_camp_decision" or "waiver_claim_submitted" or "waiver_claim_cancelled")) / (double)teams.Count,
            Unresolved = missing,
            PositionDepth = Utilities.DepthChartRules.RequiredStartersByPosition.Keys.OrderBy(p => p, StringComparer.Ordinal).Select(position =>
            {
                var counts = teams.Select(t => t.Roster.Count(p => p.Position == position && PlayerInjuryService.IsAvailableForGame(p))).ToList();
                return counts.Count == 0 ? position : $"{position} {counts.Min()}/{counts.Average():0.0}/{counts.Max()}";
            }).ToList(),
        };
    }

    // Endurance harness uses the real calendar, contracts, injuries, draft, and save loader.
    // Explicit user decisions below are diagnostic inputs, never a production user-team delegate.
    public static List<string> Run(string teamSeedPath, int seasons = 3, Action<string> progress = null)
    {
        if (seasons < 1 || seasons > 30) throw new ArgumentOutOfRangeException(nameof(seasons), "Choose 1-30 diagnostic seasons.");
        var context = new GameCoreContext();
        new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        var output = new List<string>();
        var saves = new GameCoreSaveService();
        var saveName = $"cpu_diagnostic_{Guid.NewGuid():N}.json";
        var watch = Stopwatch.StartNew();
        var firstYear = context.ActiveLeague.SeasonYear;
        var previousWeek = -1;
        try
        {
            for (var guard = 0; context.ActiveLeague.SeasonYear < firstYear + seasons; guard++)
            {
                Require(guard < seasons * 700, "CPU diagnostic exceeded its calendar progress bound.");
                var league = context.ActiveLeague;
                MakeDiagnosticUserDecisions(context);
                var year = league.SeasonYear;
                var step = new ContinueService(context).Continue(1);
                Require(step.Ok, $"{year} {league.Calendar.Phase}: {step.Error}");
                if (step.Result.StopReason == "game_day")
                {
                    var game = new GameDayService(context).SimulateCurrentUserGame();
                    Require(game.Ok, game.Error);
                }
                if (league.Calendar.AbsoluteWeek != previousWeek)
                {
                    var health = Analyze(context);
                    Require(health.StructurallyValid, health.ToString());
                    previousWeek = league.Calendar.AbsoluteWeek;
                    if (league.Calendar.AbsoluteWeek == LeagueBootstrapService.RegularSeasonStartWeek)
                    {
                        Require(health.MissingStarterTeams == 0, health.ToString());
                        var line = $"Week 1 {health}"; output.Add(line); progress?.Invoke(line);
                    }
                }
                if (league.SeasonYear == year) continue;
                var final = Analyze(context, year);
                Require(final.StructurallyValid && final.MissingStarterTeams == 0, final.ToString());
                var seasonLine = $"Completed {year}; entering {final}"; output.Add(seasonLine); progress?.Invoke(seasonLine);
                Require(saves.Save(context, saveName).Ok, "Diagnostic save failed.");
                var loaded = saves.Load(saveName);
                Require(loaded.Ok, loaded.Message);
                context.ActiveLeague = loaded.League;
                var count = loaded.League.Transactions.Count;
                new CpuRosterManagementService(context).ProcessCurrentCheckpoint();
                Require(count == loaded.League.Transactions.Count, "Reload replayed a completed CPU checkpoint.");
            }
            output.Add($"PASS {seasons} consecutive seasons, normal CPU contracts/expirations and daily recovery, annual disk reloads; {watch.Elapsed.TotalSeconds:0.0}s.");
            return output;
        }
        finally { saves.Delete(saveName); }
    }

    private static void MakeDiagnosticUserDecisions(GameCoreContext context)
    {
        var league = context.ActiveLeague;
        var team = league.Teams.Single(t => t.TeamId == league.UserTeamId);
        var contracts = new ContractService(context);
        var transactions = new TransactionService(context);
        foreach (var waiver in league.Waivers.Where(w => w.PendingConfirmation && w.PendingClaimTeamId == team.TeamId).ToList())
            transactions.CancelWaiverClaim(waiver.Player.PlayerId, team.TeamId, contracts);
        if (league.Calendar.Phase == ScheduleService.StaffCarouselPendingPhase && team.Coaches.All(c => c.Role != "Head Coach"))
        {
            var coach = league.AvailableCoaches.OrderByDescending(c => c.Overall).ThenBy(c => c.CoachId, StringComparer.Ordinal).First();
            Require(new StaffService(context).HireCoach(team.TeamId, "Head Coach", coach.CoachId).Ok, "Diagnostic user could not hire a head coach.");
        }
        if (ContractPhaseRules.CanManageRoster(league, out _))
        {
            var limit = league.Calendar.Phase == ScheduleService.TrainingCampPendingPhase ? 53 : RosterService.GetRosterLimit(league);
            var reserve = new CpuRosterManagementService(context).DraftReserve(team);
            while (team.Roster.Count > limit || contracts.GetCapRoom(team) < reserve)
            {
                var cut = FrontOfficeEvaluationService.CutCandidates(team).FirstOrDefault();
                Require(cut != null && contracts.ReleasePlayer(cut.PlayerId, team.TeamId).Accepted, "Diagnostic user could not resolve cap/capacity.");
            }
        }
        if (ContractPhaseRules.CanSignFreeAgents(league, out _))
        {
            foreach (var need in FrontOfficeEvaluationService.AssessPositions(team).Where(n => n.Available < n.RequiredStarters))
            {
                for (var count = need.Available; count < need.RequiredStarters; count++)
                {
                    var player = league.FreeAgents.Where(p => p.Position == need.Position && string.IsNullOrWhiteSpace(p.Injury) && p.CurrentInjury?.IsActive != true)
                        .OrderBy(p => contracts.GetRequiredAnnualSalary(p, team)).ThenBy(p => p.PlayerId, StringComparer.Ordinal).FirstOrDefault();
                    Require(player != null, $"Diagnostic user has no available {need.Position} candidate.");
                    var salary = Math.Ceiling(contracts.GetRequiredAnnualSalary(player, team) * 1.05m);
                    var cut = team.Roster.Count >= RosterService.GetRosterLimit(league) || salary > contracts.GetCapRoom(team)
                        ? FrontOfficeEvaluationService.CutCandidates(team, player).FirstOrDefault(p => salary <= contracts.GetCapRoom(team) + p.Contract.AnnualSalary) : null;
                    var signed = transactions.SignFreeAgent(player.PlayerId, team.TeamId,
                        new ContractOffer { AnnualSalary = salary, GuaranteedSalary = Math.Ceiling(salary * .2m), Years = UndraftedFreeAgentService.IsUndraftedRookie(player) ? 3 : 2 },
                        contracts, "Diagnostic user input: fill a starting vacancy.", cut?.PlayerId);
                    Require(signed.Accepted, $"Diagnostic user signing failed: {signed.Message}");
                }
            }
        }
        if (league.Calendar.Phase == ScheduleService.DraftPendingPhase)
        {
            var draft = new DraftService(context);
            draft.AdvanceCpuPicksUntilUserTurn();
            var pick = draft.GetCurrentPick();
            if (pick != null && pick.TeamId == team.TeamId && league.Calendar.Phase == ScheduleService.DraftPendingPhase)
            {
                var needs = FrontOfficeEvaluationService.AssessPositions(team).ToDictionary(n => n.Position);
                var prospect = league.CollegeProspects.Where(p => string.IsNullOrWhiteSpace(p.DraftedByTeamId))
                    .OrderByDescending(p => FrontOfficeEvaluationService.DraftValue(p, needs[DraftService.GetProPosition(p)])).ThenBy(p => p.ProspectId, StringComparer.Ordinal).First();
                Require(draft.MakePick(team.TeamId, prospect.ProspectId), draft.LastMessage);
            }
        }
        new DepthChartService(context).AutoFillDepthChart(team.TeamId);
        if (league.Calendar.Phase == ScheduleService.TrainingCampPendingPhase)
        {
            var camp = new TrainingCampService(context);
            if (!team.TrainingCamp.FocusApplied) camp.ApplyPositionFocus("QB", team.TeamId);
            var finalized = camp.FinalizeRoster(team.TeamId);
            Require(finalized.Ok, finalized.Message);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
