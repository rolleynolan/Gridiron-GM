using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using GridironGM.GameCore.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace GridironGM.Tests;

public sealed class CpuRosterTests
{
    private readonly ITestOutputHelper _output;
    public CpuRosterTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ThreeConsecutiveSeasonsRemainStructurallyLegalWithNormalExpirations()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var report = CpuRosterDiagnosticService.Run(Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json"));
        foreach (var line in report) _output.WriteLine(line);
        Assert.StartsWith("PASS 3 consecutive seasons", report.Last());
        Assert.Equal(3, report.Count(line => line.StartsWith("Completed ", StringComparison.Ordinal)));
    }

    [Fact]
    public void IdenticalStateAndReorderedCollectionsProduceIdenticalDecisions()
    {
        var first = Context();
        first.ActiveLeague.FreeAgents.AddRange(new[] { Player("qb-new", "QB", 85), Player("wr-new", "WR", 85), Player("dt-new", "DT", 85) });
        var second = Clone(first);
        second.ActiveLeague.Teams.Reverse();
        second.ActiveLeague.FreeAgents.Reverse();
        foreach (var team in second.ActiveLeague.Teams) team.Roster.Reverse();
        new CpuRosterManagementService(first).ProcessCurrentCheckpoint();
        new CpuRosterManagementService(second).ProcessCurrentCheckpoint();
        Assert.Equal(JsonSerializer.Serialize(first.ActiveLeague.Transactions), JsonSerializer.Serialize(second.ActiveLeague.Transactions));
        Assert.Equal(Ownership(first), Ownership(second));
    }

    [Fact]
    public void CheckpointDoesNotRepeatAndNeverTouchesUserRosterOrDepth()
    {
        var context = Context();
        var user = context.ActiveLeague.Teams[0];
        var before = JsonSerializer.Serialize(user);
        context.ActiveLeague.FreeAgents.Add(Player("upgrade", "QB", 90));
        var cpu = new CpuRosterManagementService(context);
        Assert.True(cpu.ProcessCurrentCheckpoint().Signings > 0);
        var completed = JsonSerializer.Serialize(context.ActiveLeague);
        cpu.ProcessCurrentCheckpoint();
        Assert.Equal(completed, JsonSerializer.Serialize(context.ActiveLeague));
        Assert.Equal(before, JsonSerializer.Serialize(user));
        Assert.DoesNotContain(context.ActiveLeague.Transactions, t => t.TeamId == user.TeamId);
    }

    [Fact]
    public void SaveReloadKeepsCheckpointsAndSameFutureDecisions()
    {
        var context = Context();
        context.ActiveLeague.FreeAgents.Add(Player("upgrade", "QB", 90));
        new CpuRosterManagementService(context).ProcessCurrentCheckpoint();
        var saves = new GameCoreSaveService();
        var file = $"cpu_roster_test_{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(saves.Save(context, file).Ok);
            var loaded = saves.Load(file);
            Assert.True(loaded.Ok, loaded.Message);
            var restored = new GameCoreContext { ActiveLeague = loaded.League };
            var count = restored.ActiveLeague.Transactions.Count;
            new CpuRosterManagementService(restored).ProcessCurrentCheckpoint();
            Assert.Equal(count, restored.ActiveLeague.Transactions.Count);
            context.ActiveLeague.Calendar.Phase = restored.ActiveLeague.Calendar.Phase = ScheduleService.TrainingCampPendingPhase;
            new CpuRosterManagementService(context).ProcessCurrentCheckpoint();
            new CpuRosterManagementService(restored).ProcessCurrentCheckpoint();
            Assert.Equal(Ownership(context), Ownership(restored));
            Assert.Equal(context.ActiveLeague.Transactions.Select(t => t.Details), restored.ActiveLeague.Transactions.Select(t => t.Details));
        }
        finally { saves.Delete(file); }
    }

    [Fact]
    public void LegacyMigrationDoesNotReplayCurrentOffseasonWork()
    {
        var context = Context();
        context.ActiveLeague.SaveVersion = 34;
        CpuRosterManagementService.NormalizePersistence(context.ActiveLeague, true);
        var before = JsonSerializer.Serialize(context.ActiveLeague);
        new CpuRosterManagementService(context).ProcessCurrentCheckpoint();
        Assert.Equal(before, JsonSerializer.Serialize(context.ActiveLeague));
        Assert.Contains(ScheduleService.ExclusiveNegotiationPendingPhase, Cpu(context).CpuRoster.CompletedCheckpoints);
    }

    [Fact]
    public void MissingPositionAndInjuredStartersAreVisibleToBothValidationAndPlanning()
    {
        var context = Context();
        var team = Cpu(context);
        team.Roster.RemoveAll(p => p.Position == "QB");
        foreach (var player in team.Roster.Where(p => p.Position == "DT")) Injure(player);
        var needs = FrontOfficeEvaluationService.AssessPositions(team);
        Assert.Equal(100, needs.Single(n => n.Position == "QB").Priority);
        Assert.Equal(0, needs.Single(n => n.Position == "DT").Available);
        var depth = new DepthChartService(context).GetTeamDepthChart(team.TeamId);
        Assert.Contains("Missing starting QB.", depth.DepthChartStatus.Issues);
        Assert.Contains("Missing starting DT.", depth.DepthChartStatus.Issues);
        var report = new FrontOfficeEvaluationService(context).EvaluateTeam(team.TeamId);
        Assert.DoesNotContain("potential", report.Rationale, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmergencyReplacementHonorsInjuryAndUniqueOwnership()
    {
        var context = Context("Regular Season");
        var team = Cpu(context);
        foreach (var qb in team.Roster.Where(p => p.Position == "QB")) Injure(qb);
        var replacement = Player("replacement", "QB", 72);
        context.ActiveLeague.FreeAgents.Add(replacement);
        var cpu = new CpuRosterManagementService(context);
        Assert.True(cpu.PrepareForGame("game-1", team.TeamId, "user", out var error), error);
        Assert.Contains(replacement, team.Roster);
        Assert.DoesNotContain(replacement, context.ActiveLeague.FreeAgents);
        Assert.Contains(replacement.PlayerId, team.DepthChart["QB"]);
        Assert.Single(context.ActiveLeague.Transactions, t => t.Type == "free_agent_signed");
        Assert.True(cpu.PrepareForGame("game-1", team.TeamId, "user", out _));
        Assert.Single(context.ActiveLeague.Transactions, t => t.Type == "free_agent_signed");
    }

    [Fact]
    public void InternalPracticeSquadReplacementUsesPermanentActiveContract()
    {
        var context = Context("Regular Season");
        var team = Cpu(context);
        foreach (var qb in team.Roster.Where(p => p.Position == "QB")) Injure(qb);
        var reserve = Player("reserve", "QB", 62);
        reserve.Status = "Practice Squad";
        reserve.Contract.ContractType = "Practice Squad";
        reserve.Contract.AnnualSalary = 300_000;
        team.PracticeSquad.Add(reserve);
        Assert.True(new CpuRosterManagementService(context).PrepareForGame("ps-game", team.TeamId, "user", out var error), error);
        Assert.Empty(team.PracticeSquad);
        Assert.Contains(reserve, team.Roster);
        Assert.Equal("Active Roster", reserve.Contract.ContractType);
        Assert.Equal(TransactionService.ActiveRosterMinimumSalary, reserve.Contract.AnnualSalary);
    }

    [Fact]
    public void NegligibleUpgradesAreRejectedAndDepthShortagesAreFilled()
    {
        var context = Context();
        var team = Cpu(context);
        var cpu = new CpuRosterManagementService(context);
        Assert.False(cpu.IsMeaningfulAcquisition(team, Player("negligible", "QB", 66)));
        team.Roster.Remove(team.Roster.Last(p => p.Position == "QB"));
        Assert.True(cpu.IsMeaningfulAcquisition(team, Player("depth", "QB", 64)));
        context.ActiveLeague.FreeAgents.Add(Player("depth", "QB", 64));
        cpu.ProcessCurrentCheckpoint();
        Assert.Equal(2, team.Roster.Count(p => p.Position == "QB"));
    }

    [Fact]
    public void CapDeficitCannotBeHiddenOrUsedForConditionalMoves()
    {
        var context = Context();
        var team = Cpu(context);
        var contracts = new ContractService(context);
        context.ActiveLeague.SalaryCap = contracts.GetCommittedSalary(team) - 1_000_000;
        Assert.Equal(-1_000_000, contracts.GetCapRoom(team));
        var candidate = Player("costly", "QB", 90);
        context.ActiveLeague.FreeAgents.Add(candidate);
        var release = team.Roster.Last(p => p.Position == "QB");
        var result = new TransactionService(context).SignFreeAgent(candidate.PlayerId, team.TeamId,
            new ContractOffer { AnnualSalary = 10_000_000, GuaranteedSalary = 2_000_000, Years = 2 }, contracts,
            conditionalReleasePlayerId: release.PlayerId);
        Assert.False(result.Accepted);
        Assert.Contains(release, team.Roster);
        Assert.Contains(candidate, context.ActiveLeague.FreeAgents);
        new CpuRosterManagementService(context).RelieveCapPressure(team, 0);
        Assert.True(contracts.GetCapRoom(team) >= 0);
        Assert.Empty(CpuRosterManagementService.MissingStarters(team));
    }

    [Fact]
    public void DeclinedConditionalOfferPreservesBothPlayers()
    {
        var context = Context();
        var team = Cpu(context);
        var candidate = Player("target", "QB", 90);
        context.ActiveLeague.FreeAgents.Add(candidate);
        var release = team.Roster.Last(p => p.Position == "QB");
        var before = JsonSerializer.Serialize(context.ActiveLeague);
        var result = new TransactionService(context).SignFreeAgent(candidate.PlayerId, team.TeamId,
            new ContractOffer { AnnualSalary = 1, GuaranteedSalary = 1, Years = 1 }, new ContractService(context), conditionalReleasePlayerId: release.PlayerId);
        Assert.False(result.Accepted);
        Assert.Equal(before, JsonSerializer.Serialize(context.ActiveLeague));
    }

    [Fact]
    public void CooldownPreventsImmediateReacquisition()
    {
        var context = Context();
        var team = Cpu(context);
        var player = team.Roster.Last(p => p.Position == "QB");
        Assert.True(new ContractService(context).ReleasePlayer(player.PlayerId, team.TeamId).Accepted);
        var cpu = new CpuRosterManagementService(context);
        Assert.False(cpu.IsMeaningfulAcquisition(team, player));
        context.ActiveLeague.Calendar.AbsoluteWeek += 5;
        Assert.True(cpu.IsMeaningfulAcquisition(team, player));
    }

    [Fact]
    public void ExpirationsIncludeBothReservePoolsButPreserveNewExtensions()
    {
        var context = Context(ScheduleService.ExclusiveNegotiationPendingPhase);
        var team = Cpu(context);
        var active = team.Roster.First();
        var salary = new ContractService(context).GetRequiredAnnualSalary(active, team);
        Assert.True(new ContractService(context).ReSignPlayer(active.PlayerId, team.TeamId,
            new ContractOffer { AnnualSalary = salary, GuaranteedSalary = salary / 5, Years = 3 }).Accepted);
        var ir = Player("ir", "QB"); ir.Status = "IR"; ir.Contract.YearsRemaining = 1;
        var ps = Player("ps", "RB"); ps.Status = "Practice Squad"; ps.Contract.YearsRemaining = 1;
        team.InjuredReserve.Add(ir); team.PracticeSquad.Add(ps);
        var contracts = new ContractService(context);
        Assert.Equal(2, contracts.ProcessContractExpirations());
        Assert.Equal(3, active.Contract.YearsRemaining);
        Assert.Empty(team.InjuredReserve); Assert.Empty(team.PracticeSquad);
        Assert.Contains(ir, context.ActiveLeague.FreeAgents); Assert.Contains(ps, context.ActiveLeague.FreeAgents);
        Assert.Equal(0, contracts.ProcessContractExpirations());
    }

    [Fact]
    public void RetentionIsBoundedAndIdempotent()
    {
        var context = Context(ScheduleService.ExclusiveNegotiationPendingPhase);
        foreach (var player in Cpu(context).Roster) player.Contract.YearsRemaining = 1;
        var cpu = new CpuRosterManagementService(context);
        cpu.ProcessCurrentCheckpoint();
        Assert.InRange(context.ActiveLeague.Transactions.Count(t => t.Type == "contract_extended"), 1, 6);
        var before = context.ActiveLeague.Transactions.Count;
        cpu.ProcessCurrentCheckpoint();
        Assert.Equal(before, context.ActiveLeague.Transactions.Count);
    }

    [Fact]
    public void CampCutsProtectRequiredStartersAndDevelopmentalEligibility()
    {
        var context = Context(ScheduleService.TrainingCampPendingPhase);
        var team = Cpu(context);
        for (var i = 0; i < 18; i++) team.Roster.Add(Player($"excess-{i:00}", "RB", 40));
        context.ActiveLeague.FreeAgents.Add(Player("young", "LT", 62));
        var oldReserve = Player("old-reserve", "CB", 55); oldReserve.Age = 26; oldReserve.Status = "Practice Squad";
        team.PracticeSquad.Add(oldReserve);
        new CpuRosterManagementService(context).ProcessCurrentCheckpoint();
        Assert.True(team.Roster.Count <= 53);
        Assert.Empty(CpuRosterManagementService.MissingStarters(team));
        Assert.True(team.TrainingCamp.RosterFinalized);
        Assert.DoesNotContain(oldReserve, team.PracticeSquad);
        Assert.All(team.PracticeSquad, player => { Assert.True(player.Age <= 25); Assert.Equal("Practice Squad", player.Contract.ContractType); });
        Assert.NotEmpty(context.ActiveLeague.Waivers);
        Assert.Equal(Ownership(context).Split('|').Length, Ownership(context).Split('|').Distinct().Count());
    }

    [Fact]
    public void DraftBalancesUrgentNeedAgainstValueAndReevaluatesAfterEachPick()
    {
        var context = Context(ScheduleService.DraftPendingPhase);
        var league = context.ActiveLeague;
        var team = Cpu(context);
        team.Roster.RemoveAll(p => p.Position is "QB" or "WR");
        league.CollegeProspects = new() { Prospect("qb", "QB", 72), Prospect("qb2", "QB", 71), Prospect("wr", "WR", 72), Prospect("best", "DT", 99) };
        league.Draft = new() { DraftYear = league.SeasonYear, Picks = new()
        {
            new() { OverallPick = 1, Round = 1, TeamId = team.TeamId },
            new() { OverallPick = 2, Round = 1, TeamId = team.TeamId },
            new() { OverallPick = 3, Round = 2, TeamId = team.TeamId },
            new() { OverallPick = 4, Round = 2, TeamId = "user" },
        }};
        new DraftService(context).AdvanceCpuPicksUntilUserTurn();
        Assert.Equal("best", league.Draft.Picks[0].ProspectId);
        Assert.Equal("qb", league.Draft.Picks[1].ProspectId);
        Assert.Equal("wr", league.Draft.Picks[2].ProspectId);
        Assert.Contains(league.Transactions, t => t.Type == "draft_pick_made" && t.Details.Contains("CPU"));
        Assert.True(new ContractService(context).GetCapRoom(team) >= 0);
    }

    [Fact]
    public void InvalidDraftContractCannotCreateAPlayer()
    {
        var context = Context(ScheduleService.DraftPendingPhase);
        new DraftService(context).PrepareDraftBoard();
        var team = Cpu(context);
        var pick = context.ActiveLeague.Draft.Picks.First(p => p.TeamId == team.TeamId);
        var prospect = Prospect("candidate", "QB", 90);
        context.ActiveLeague.CollegeProspects.Add(prospect);
        context.ActiveLeague.SalaryCap = new ContractService(context).GetCommittedSalary(team);
        Assert.False(new TransactionService(context).DraftRookie(pick, prospect, team, out var error));
        Assert.Contains("cap", error);
        Assert.Empty(pick.ProspectId);
        Assert.DoesNotContain(team.Roster, p => p.Name == prospect.Name);
    }

    [Fact]
    public void NoValidCandidateIsSafeAndPersistentlyIdempotent()
    {
        var context = Context();
        var team = Cpu(context);
        team.Roster.RemoveAll(p => p.Position == "QB");
        var cpu = new CpuRosterManagementService(context);
        var result = cpu.ProcessCurrentCheckpoint();
        Assert.Contains("cpu: QB", result.Unresolved);
        var before = JsonSerializer.Serialize(context.ActiveLeague);
        cpu.ProcessCurrentCheckpoint();
        Assert.Equal(before, JsonSerializer.Serialize(context.ActiveLeague));
        Assert.False(cpu.PrepareForGame("blocked", team.TeamId, "user", out var error));
        Assert.Contains("missing available QB", error);
    }

    [Fact]
    public void WaiverPriorityAndConditionalReleaseUseTheNormalQueue()
    {
        var context = Context("Regular Season");
        var league = context.ActiveLeague;
        var user = league.Teams[0];
        var team = Cpu(context);
        var donor = Team("donor"); league.Teams.Add(donor);
        user.Wins = 0; user.Losses = 8; team.Wins = 1; team.Losses = 7;
        var player = donor.Roster.First(p => p.Position == "QB"); player.Overall = 95;
        var transactions = new TransactionService(context); var contracts = new ContractService(context);
        Assert.True(transactions.PlaceOnWaivers(player.PlayerId, donor.TeamId, contracts).Accepted);
        var waiver = league.Waivers.Single(); waiver.Claims.Clear();
        var release = team.Roster.Last(p => p.Position == "QB");
        Assert.True(transactions.SubmitWaiverClaim(player.PlayerId, user.TeamId, contracts).Accepted);
        Assert.True(transactions.SubmitWaiverClaim(player.PlayerId, team.TeamId, contracts, release.PlayerId).Accepted);
        league.Calendar.AbsoluteWeek++;
        transactions.ExpireWaivers();
        Assert.True(waiver.PendingConfirmation); Assert.Equal(user.TeamId, waiver.PendingClaimTeamId);
        Assert.Contains(release, team.Roster);
        user.Wins = 15; team.Losses = 15; // Changing standings cannot reroll the already resolved queue.
        Assert.True(transactions.CancelWaiverClaim(player.PlayerId, user.TeamId, contracts).Accepted);
        Assert.Contains(player, team.Roster); Assert.DoesNotContain(release, team.Roster);
        Assert.Contains(release, league.FreeAgents); Assert.Empty(league.Waivers);
        Assert.Equal(new[] { "user", "cpu" }, waiver.ResolutionOrder);
    }

    [Fact]
    public void WaiverExpiryDropsInheritedContractAndCannotDuplicatePoolOwnership()
    {
        var context = Context("Regular Season");
        var player = Cpu(context).Roster.First(); player.Overall = 1;
        var transactions = new TransactionService(context);
        transactions.PlaceOnWaivers(player.PlayerId, "cpu", new ContractService(context));
        context.ActiveLeague.Waivers.Single().Claims.Clear();
        context.ActiveLeague.Calendar.AbsoluteWeek++;
        Assert.Equal(1, transactions.ExpireWaivers());
        Assert.Equal(0, player.Contract.AnnualSalary);
        Assert.Equal("Free Agent", player.Contract.ContractType);
        Assert.Equal(0, transactions.ExpireWaivers());
        Assert.Single(context.ActiveLeague.FreeAgents, p => p.PlayerId == player.PlayerId);
    }

    [Theory]
    [InlineData("OT", "LT", "RT")]
    [InlineData("OG", "LG", "RG")]
    public void CollegeLinemenEnterOneSupportedProPosition(string collegePosition, string left, string right)
    {
        var context = Context(ScheduleService.DraftPendingPhase);
        var team = Cpu(context);
        new DraftService(context).PrepareDraftBoard();
        var prospect = Prospect("lineman", collegePosition, 75);
        context.ActiveLeague.CollegeProspects.Add(prospect);
        var pick = context.ActiveLeague.Draft.Picks.First(p => p.TeamId == team.TeamId);
        Assert.True(new TransactionService(context).DraftRookie(pick, prospect, team, out var error), error);
        var player = team.Roster.Single(p => p.PlayerId == pick.PlayerId);
        Assert.Contains(player.Position, new[] { left, right });
        Assert.Equal(DraftService.GetProPosition(prospect), player.Position);
        Assert.Contains(player.PlayerId, team.DepthChart[player.Position]);
    }

    [Fact]
    public void RookieCheckpointSignsOnlyLegalThreeYearUdfas()
    {
        var context = Context(ScheduleService.RookieSigningPendingPhase);
        var rookie = Player("udfa", "QB", 90);
        rookie.Status = "Undrafted Free Agent";
        rookie.Contract = new() { ContractType = "Undrafted Free Agent" };
        var veteran = Player("veteran", "WR", 95); veteran.Age = 30;
        context.ActiveLeague.FreeAgents.AddRange(new[] { rookie, veteran });
        new CpuRosterManagementService(context).ProcessCurrentCheckpoint();
        Assert.Contains(rookie, Cpu(context).Roster);
        Assert.Equal("Undrafted Rookie Contract", rookie.Contract.ContractType);
        Assert.Equal(3, rookie.Contract.YearsRemaining);
        Assert.Contains(veteran, context.ActiveLeague.FreeAgents);
    }

    [Fact]
    public void WaiversResolveAcrossSeasonBoundaryAndFreeAgentsRecover()
    {
        var context = Context(ScheduleService.TrainingCampPendingPhase);
        var player = Cpu(context).Roster.Last(); player.Overall = 1;
        Injure(player);
        var transactions = new TransactionService(context);
        Assert.True(transactions.PlaceOnWaivers(player.PlayerId, "cpu", new ContractService(context)).Accepted);
        context.ActiveLeague.Waivers.Single().Claims.Clear();
        context.ActiveLeague.SeasonYear++;
        context.ActiveLeague.Calendar.Phase = "Preseason"; context.ActiveLeague.Calendar.AbsoluteWeek = 1;
        Assert.Equal(1, transactions.ExpireWaivers());
        PlayerInjuryService.NormalizeRecoveryPersistence(context.ActiveLeague, legacy: false);
        context.ActiveLeague.Calendar.CurrentDate = DateTime.Parse(context.ActiveLeague.Calendar.CurrentDate).AddDays(21).ToString("yyyy-MM-dd");
        PlayerInjuryService.RecoverThroughCurrentDate(context.ActiveLeague);
        Assert.Empty(player.Injury);
        Assert.False(player.CurrentInjury.IsActive);
        Assert.Equal("Free Agent", player.Status);
    }

    [Fact]
    public void DuplicatePoolOwnershipAndEssentialConditionalCutsAreRejected()
    {
        var context = Context();
        var team = Cpu(context);
        var player = Player("duplicate", "QB", 95);
        context.ActiveLeague.FreeAgents.Add(player);
        team.PracticeSquad.Add(player);
        var before = JsonSerializer.Serialize(context.ActiveLeague);
        Assert.False(new ContractService(context).SignFreeAgent(player.PlayerId, team.TeamId,
            new ContractOffer { AnnualSalary = 20_000_000, GuaranteedSalary = 5_000_000, Years = 2 }).Accepted);
        Assert.Equal(before, JsonSerializer.Serialize(context.ActiveLeague));
        var kicker = team.Roster.Single(p => p.Position == "K");
        Assert.False(new CpuRosterManagementService(context).ShouldConfirmWaiver(team, Player("qb-in", "QB", 90), kicker));
    }

    [Fact]
    public void DraftMarketReserveProtectsUpcomingContracts()
    {
        var context = Context();
        var team = Cpu(context);
        context.ActiveLeague.SalaryCap = new ContractService(context).GetCommittedSalary(team) + 5_000_000m;
        var upgrade = Player("upgrade", "QB", 99); context.ActiveLeague.FreeAgents.Add(upgrade);
        new DraftService(context).PrepareDraftBoard();
        var cpu = new CpuRosterManagementService(context);
        cpu.ProcessCurrentCheckpoint();
        Assert.True(new ContractService(context).GetCapRoom(team) >= cpu.DraftReserve(team));
        Assert.Contains(upgrade, context.ActiveLeague.FreeAgents);
    }

    private static GameCoreContext Context(string phase = ScheduleService.FreeAgencyPendingPhase)
    {
        var league = new LeagueState { UserTeamId = "user", Teams = new() { Team("user"), Team("cpu") } };
        league.Calendar.Phase = phase;
        league.Calendar.AbsoluteWeek = ScheduleService.IsOffseasonPlaceholderPhase(phase) ? ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(phase) : 5;
        var context = new GameCoreContext { ActiveLeague = league };
        new ContractService(context).RefreshCapRoom(league);
        return context;
    }
    private static TeamState Team(string id)
    {
        var team = new TeamState { TeamId = id, Name = id, Abbreviation = id };
        foreach (var position in DepthChartRules.RequiredStartersByPosition.Keys)
        {
            var count = FrontOfficeEvaluationService.DesiredDepth(position);
            for (var i = 0; i < count; i++) team.Roster.Add(Player($"{id}-{position}-{i}", position));
            team.DepthChart[position] = team.Roster.Where(p => p.Position == position).Select(p => p.PlayerId).ToList();
        }
        return team;
    }
    private static TeamState Cpu(GameCoreContext context) => context.ActiveLeague.Teams.Single(t => t.TeamId == "cpu");
    private static PlayerState Player(string id, string position, int overall = 65) => new()
    {
        PlayerId = id, Name = id, Position = position, Overall = overall, Potential = overall + 5, Age = 24, Status = "Active", Morale = 50,
        Contract = new() { AnnualSalary = 750_000, YearsRemaining = 3, ContractType = "Standard" },
    };
    private static CollegeProspectState Prospect(string id, string position, int overall) => new()
    { ProspectId = id, Name = id, Position = position, Overall = overall, Potential = overall, Age = 22, DraftClassYear = 2027 };
    private static void Injure(PlayerState player) { player.Injury = "Ankle sprain"; player.CurrentInjury = new() { Name = "Ankle sprain", DaysRemaining = 21 }; }
    private static GameCoreContext Clone(GameCoreContext context) => new() { ActiveLeague = JsonSerializer.Deserialize<LeagueState>(JsonSerializer.Serialize(context.ActiveLeague))! };
    private static string Ownership(GameCoreContext context) => string.Join("|", context.ActiveLeague.Teams
        .SelectMany(t => t.Roster.Concat(t.PracticeSquad).Concat(t.InjuredReserve)).Concat(context.ActiveLeague.FreeAgents)
        .Concat(context.ActiveLeague.Waivers.Select(w => w.Player)).Select(p => p.PlayerId).OrderBy(id => id, StringComparer.Ordinal));
}
