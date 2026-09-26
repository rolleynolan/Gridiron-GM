using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using Xunit;

namespace GridironGM.Tests;

public sealed class MedicalContinuityTests
{
    [Theory]
    [InlineData("Active", 2)]
    [InlineData("IR", 2)]
    [InlineData("Practice Squad", 2)]
    [InlineData("Free Agent", 1)]
    [InlineData("Waived", 1)]
    public void EveryPoolRecoversOnlyElapsedDaysAndKeepsItsOwnershipStatus(string status, int rate)
    {
        var league = League();
        var player = Player(); player.Status = status;
        switch (status)
        {
            case "Active": league.Teams[0].Roster.Add(player); break;
            case "IR": league.Teams[0].InjuredReserve.Add(player); break;
            case "Practice Squad": league.Teams[0].PracticeSquad.Add(player); break;
            case "Free Agent": league.FreeAgents.Add(player); break;
            default: league.Waivers.Add(new WaiverClaimState { Player = player }); break;
        }
        PlayerInjuryService.InjurePlayer(league, player, "Shoulder strain", 10);
        PlayerInjuryService.RecoverThroughCurrentDate(league);
        Assert.Equal(10, player.CurrentInjury.DaysRemaining);
        Recover(league, "2026-08-04");
        Assert.Equal(10 - 3 * rate, player.CurrentInjury.DaysRemaining);
        var once = JsonSerializer.Serialize(player);
        PlayerInjuryService.RecoverThroughCurrentDate(league);
        Assert.Equal(once, JsonSerializer.Serialize(player));
        Recover(league, "2026-08-02");
        Assert.Equal(once, JsonSerializer.Serialize(player));
        Recover(league, "2026-09-01");
        Assert.False(player.CurrentInjury.IsActive);
        Assert.Equal(status, player.Status);
        Assert.Equal(rate == 2 ? "2026-08-06" : "2026-08-11", Assert.Single(player.InjuryHistory).RecoveredOn);
    }

    [Fact]
    public void SigningAndReleaseChangeFutureMedicalSupportWithoutReplayingElapsedTime()
    {
        var league = League(); var player = Player(); player.Status = "Free Agent";
        league.FreeAgents.Add(player);
        PlayerInjuryService.InjurePlayer(league, player, "Ankle sprain", 10);
        Recover(league, "2026-08-04");
        Assert.Equal(7, player.CurrentInjury.DaysRemaining);
        var context = new GameCoreContext { ActiveLeague = league };
        var contracts = new ContractService(context);
        var transactions = new TransactionService(context);
        var salary = Math.Ceiling(contracts.GetRequiredAnnualSalary(player, league.Teams[0]) * 1.1m);
        var result = transactions.SignFreeAgent(player.PlayerId, "user", new ContractOffer { Years = 2, AnnualSalary = salary, GuaranteedSalary = Math.Ceiling(salary * .2m) }, contracts);
        Assert.True(result.Accepted, result.Message);
        PlayerInjuryService.RecoverThroughCurrentDate(league);
        Assert.Equal(7, player.CurrentInjury.DaysRemaining);
        Recover(league, "2026-08-06");
        Assert.Equal(3, player.CurrentInjury.DaysRemaining);
        Assert.True(transactions.ReleasePlayer(player.PlayerId, "user", contracts).Accepted);
        Recover(league, "2026-08-20");
        Assert.Equal("2026-08-09", Assert.Single(player.InjuryHistory).RecoveredOn);
        Assert.Equal("Free Agent", player.Status);
    }

    [Fact]
    public void RecoveryCompletesOnlyTheMatchingInjuryEpisode()
    {
        var league = League(); var player = Player(); league.FreeAgents.Add(player);
        PlayerInjuryService.InjurePlayer(league, player, "Ankle sprain", 2);
        player.InjuryHistory.Add(new PlayerInjuryRecord { Name = "Older unrelated injury", GameId = "", OccurredOn = "2025-08-01" });
        Recover(league, "2026-08-10");
        Assert.Equal("2026-08-03", player.InjuryHistory[0].RecoveredOn);
        Assert.Empty(player.InjuryHistory[1].RecoveredOn);
    }

    [Theory]
    [InlineData(36)]
    [InlineData(37)]
    public void SaveMigrationKeepsRemainingRecoveryAndReloadCannotAwardItTwice(int version)
    {
        var context = Bootstrap(); var league = context.ActiveLeague;
        var player = league.FreeAgents[0];
        PlayerInjuryService.InjurePlayer(league, player, "Knee sprain", 100);
        Recover(league, "2026-08-11");
        Assert.Equal(90, player.CurrentInjury.DaysRemaining);
        league.SaveVersion = version;
        if (version < 37) player.CurrentInjury.RecoveryProcessedThrough = "";
        var saves = new GameCoreSaveService(); var name = $"medical_test_{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(saves.Save(context, name).Ok);
            var loaded = saves.Load(name); Assert.True(loaded.Ok, loaded.Message);
            var restored = loaded.League.FreeAgents.Single(p => p.PlayerId == player.PlayerId);
            Assert.Equal(90, restored.CurrentInjury.DaysRemaining);
            PlayerInjuryService.RecoverThroughCurrentDate(loaded.League);
            Assert.Equal(90, restored.CurrentInjury.DaysRemaining);
            Recover(loaded.League, "2026-08-21");
            Recover(league, "2026-08-21");
            Assert.Equal(80, restored.CurrentInjury.DaysRemaining);
            if (version == 37) Assert.Equal(JsonSerializer.Serialize(player.CurrentInjury), JsonSerializer.Serialize(restored.CurrentInjury));
        }
        finally { saves.Delete(name); }
    }

    [Fact]
    public void OffseasonContinueRecoversHealthAndFatigueAcrossEveryPool()
    {
        var context = Bootstrap(); var league = context.ActiveLeague;
        league.Calendar.AbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.OffseasonPendingPhase);
        league.Calendar.Phase = ScheduleService.OffseasonPendingPhase;
        var player = league.FreeAgents[0]; player.Fatigue = 20;
        PlayerInjuryService.InjurePlayer(league, player, "Ankle sprain", 10);
        var result = new ContinueService(context).Continue();
        Assert.True(result.Ok, result.Error);
        Assert.Equal("2026-08-02", league.Calendar.CurrentDate);
        Assert.Equal(9, player.CurrentInjury.DaysRemaining);
        Assert.Equal(16, player.Fatigue);
    }

    [Fact]
    public void LongInjurySurvivesRolloverAndUserIrStillRequiresExplicitActivation()
    {
        var context = Bootstrap(); var league = context.ActiveLeague;
        league.Calendar.AbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.TrainingCampPendingPhase);
        league.Calendar.Phase = ScheduleService.TrainingCampPendingPhase;
        league.Calendar.CurrentDate = "2027-07-20";
        var team = league.Teams.Single(t => t.TeamId == league.UserTeamId);
        team.Coaches.Clear();
        var player = team.Roster.First(p => p.Position == "QB");
        PlayerInjuryService.InjurePlayer(league, player, "Shoulder strain", 240, "long-injury");
        var transactions = new TransactionService(context); var contracts = new ContractService(context);
        Assert.True(transactions.MoveToInjuredReserve(player.PlayerId, team.TeamId, contracts).Accepted);
        Assert.True(new DepthChartService(context).AutoFillDepthChart(team.TeamId).Ok);
        var camp = new TrainingCampService(context);
        Assert.True(camp.ApplyPositionFocus("QB", team.TeamId).Ok);
        var finalized = camp.FinalizeRoster(team.TeamId); Assert.True(finalized.Ok, finalized.Message);
        Assert.True(new SeasonRolloverService(context).StartNextSeason(out var message), message);
        Assert.Equal("2027-08-01", league.Calendar.CurrentDate);
        Assert.Equal(228, player.CurrentInjury.DaysRemaining);
        Assert.Equal("IR", player.Status);
        Assert.Empty(player.InjuryHistory.Single().RecoveredOn);
        Assert.False(transactions.ActivateFromInjuredReserve(player.PlayerId, team.TeamId, contracts).Accepted);
        Recover(league, "2028-04-01");
        Assert.False(player.CurrentInjury.IsActive);
        Assert.Equal("IR", player.Status);
        Assert.Contains(player, team.InjuredReserve);
        Assert.True(transactions.ActivateFromInjuredReserve(player.PlayerId, team.TeamId, contracts).Accepted);
        Assert.Equal("Active", player.Status);
    }

    [Fact]
    public void PlayoffRoundsAdvanceDatesAndRecoveryOnceIncludingLegacyAnchorAndReload()
    {
        var context = Bootstrap(); var league = context.ActiveLeague;
        var games = new GameDayService(context);
        foreach (var game in league.Schedule.Where(g => g.GameType == "regular_season"))
            Assert.True(games.SimulateScheduledGame(game.GameId, true).Ok);
        league.Calendar.AbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + 1;
        league.Calendar.DayIndex = 0; league.Calendar.CurrentDate = "2027-01-10";
        var playoffs = new PlayoffService(context); Assert.True(playoffs.EnsureBracketGenerated(league, out _));
        var patient = league.FreeAgents.First(p => !p.CurrentInjury.IsActive);
        PlayerInjuryService.InjurePlayer(league, patient, "Knee sprain", 30);
        Assert.True(playoffs.SimulateWildCardRound(league).Ok);
        Assert.Equal(30, patient.CurrentInjury.DaysRemaining);
        // Represents a migrated bracket that completed Wild Card before dates were persisted.
        league.PlayoffBracket.CalendarAnchorDate = "";
        Assert.True(playoffs.SimulateDivisionalRound(league).Ok);
        Assert.Equal("2027-01-17", league.Calendar.CurrentDate);
        Assert.Equal(23, patient.CurrentInjury.DaysRemaining);
        Assert.True(playoffs.SimulateDivisionalRound(league).Ok);
        Assert.Equal(23, patient.CurrentInjury.DaysRemaining);
        var restored = JsonSerializer.Deserialize<LeagueState>(JsonSerializer.Serialize(league))!;
        var resumed = new PlayoffService(new GameCoreContext { ActiveLeague = restored });
        Assert.True(resumed.SimulateConferenceChampionshipRound(restored).Ok);
        Assert.Equal("2027-01-24", restored.Calendar.CurrentDate);
        Assert.Equal(16, restored.FreeAgents.Single(p => p.PlayerId == patient.PlayerId).CurrentInjury.DaysRemaining);
    }

    [Fact]
    public void MedicalReportPublishesDatedRangeClearanceAndAutomaticTreatmentWithoutMutation()
    {
        var league = League(); var team = league.Teams[0]; var player = Player(); team.Roster.Add(player);
        PlayerInjuryService.InjurePlayer(league, player, "Shoulder strain", 10);
        var before = JsonSerializer.Serialize(league);

        var report = PlayerMedicalReportService.Build(league, team, player, false);

        Assert.Equal("Shoulder strain", report.Diagnosis);
        Assert.Equal("Diagnosis recorded", report.EvaluationStatus);
        Assert.Equal("Not medically cleared", report.ClearanceStatus);
        Assert.Equal("2026-08-05 to 2026-08-07", report.EstimatedClearanceWindow);
        Assert.Contains("staff selected", report.Treatment);
        Assert.Contains("not guaranteed", report.EstimateContext);
        Assert.Contains("Unavailable for game selection", report.Restrictions);
        Assert.Equal(before, JsonSerializer.Serialize(league));
    }

    [Fact]
    public void MedicalProjectionUsesTheSamePersistedStaffRateAsRecovery()
    {
        var league = League(); var team = league.Teams[0]; var player = Player(); team.Roster.Add(player);
        PlayerInjuryService.InjurePlayer(league, player, "Knee sprain", 10);
        var supported = PlayerMedicalReportService.Build(league, team, player, false);
        team.Coaches.Clear();
        var ordinary = PlayerMedicalReportService.Build(league, team, player, false);

        Assert.Equal("2026-08-05 to 2026-08-07", supported.EstimatedClearanceWindow);
        Assert.Equal("2026-08-09 to 2026-08-13", ordinary.EstimatedClearanceWindow);
    }

    [Fact]
    public void ClearedIrPlayerStillReportsActivationRequirement()
    {
        var league = League(); var team = league.Teams[0]; var player = Player(); player.Status = "IR"; team.InjuredReserve.Add(player);

        var report = PlayerMedicalReportService.Build(league, team, player, true);

        Assert.False(report.HasActiveInjury);
        Assert.Equal("No active injury", report.Diagnosis);
        Assert.Equal("Medically cleared; roster activation required", report.ClearanceStatus);
        Assert.Contains("Not game-eligible", report.Restrictions);
    }

    [Fact]
    public void IncompleteLegacyInjuryDoesNotInventARecoveryDate()
    {
        var league = League(); var team = league.Teams[0]; var player = Player(); player.Injury = "Legacy injury"; player.Status = "Injured"; team.Roster.Add(player);

        var report = PlayerMedicalReportService.Build(league, team, player, false);

        Assert.True(report.HasActiveInjury);
        Assert.Equal("Legacy injury", report.Diagnosis);
        Assert.Equal("Recovery timetable under evaluation", report.EvaluationStatus);
        Assert.Equal("Pending medical evaluation", report.EstimatedClearanceWindow);
    }

    private static void Recover(LeagueState league, string date)
    {
        league.Calendar.CurrentDate = date;
        PlayerInjuryService.RecoverThroughCurrentDate(league);
    }
    private static PlayerState Player() => new() { PlayerId = "patient", Name = "Patient", Position = "QB", Age = 24, Overall = 65, Potential = 70 };
    private static LeagueState League() => new()
    {
        UserTeamId = "user", Teams = new List<TeamState> { new() { TeamId = "user", Name = "User", Coaches = new() { new() { Role = "Medical Director", Overall = 85 } } } },
    };
    private static GameCoreContext Bootstrap()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var context = new GameCoreContext();
        new LeagueBootstrapService(context).CreateTestLeague(Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json"));
        return context;
    }
}
