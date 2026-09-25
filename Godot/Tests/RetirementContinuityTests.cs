using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using Xunit;

namespace GridironGM.Tests;

public sealed class RetirementContinuityTests
{
    [Fact]
    public void UnsignedDecisionsAndLedgerOrderIgnoreCollectionOrderAndReplay()
    {
        var first = League();
        first.FreeAgents = Enumerable.Range(0, 200).Select(i => Player($"unsigned-{i}", since: 2022)).ToList();
        var second = Clone(first);
        second.FreeAgents.Reverse();
        second.Teams.Reverse();
        var service = new RetirementService();
        var a = service.GenerateRetirementsForCurrentSeason(first);
        var b = service.GenerateRetirementsForCurrentSeason(second);
        Assert.InRange(a.RetiredCount, 1, 199);
        Assert.Equal(JsonSerializer.Serialize(a.SeasonRecord), JsonSerializer.Serialize(b.SeasonRecord));
        Assert.Equal(JsonSerializer.Serialize(first.Transactions), JsonSerializer.Serialize(second.Transactions));
        Assert.All(a.SeasonRecord.Players, r => Assert.Equal("extended_free_agency", r.ReasonLabel));
        Assert.DoesNotContain(first.FreeAgents, p => a.SeasonRecord.Players.Any(r => r.PlayerId == p.PlayerId));
        var before = JsonSerializer.Serialize(first);
        Assert.True(service.GenerateRetirementsForCurrentSeason(first).Skipped);
        Assert.Equal(before, JsonSerializer.Serialize(first));
    }

    [Fact]
    public void FreshAndDevelopingEntrantsGetTimeWhileSustainedUnsignedCareersEnd()
    {
        var league = League();
        league.FreeAgents = Enumerable.Range(0, 200).Select(i => Player($"new-{i}", since: 2025)).ToList();
        Assert.Equal(0, new RetirementService().GenerateRetirementsForCurrentSeason(league).RetiredCount);
        league.SeasonYear += 5;
        var result = new RetirementService().GenerateRetirementsForCurrentSeason(league);
        Assert.InRange(result.RetiredCount, 1, 199);
        // A player with stronger bounded future value retains more opportunity for the same rolls.
        var stronger = League();
        stronger.SeasonYear = league.SeasonYear;
        stronger.FreeAgents = Enumerable.Range(0, 200).Select(i => Player($"new-{i}", since: 2025, overall: 85)).ToList();
        var strongResult = new RetirementService().GenerateRetirementsForCurrentSeason(stronger);
        Assert.True(strongResult.RetiredCount < result.RetiredCount);
        Assert.All(strongResult.SeasonRecord.Players, r => Assert.Contains(result.SeasonRecord.Players, weak => weak.PlayerId == r.PlayerId));
    }

    [Fact]
    public void RetirementPreservesIdentityStatsAndExistingDepthOrderWithoutResurrectingPlayer()
    {
        var league = League();
        var retired = Player("retiring", since: 2020);
        retired.Status = "Retired";
        retired.College = "Example College";
        retired.CollegePlayerId = "college-id";
        retired.CollegeCareerStats.Add(new CollegePlayerSeasonStats { SeasonYear = 2020, GamesPlayed = 12, PassingYards = 3600 });
        retired.CareerStats.Add(new PlayerSeasonStats { SeasonYear = 2024, GamesPlayed = 17, PassingYards = 5500 });
        retired.DevelopmentHistory.Add(new PlayerDevelopmentRecord { SeasonYear = 2025, OverallBefore = 61, OverallAfter = 60 });
        league.FreeAgents.Add(retired);
        league.RookieMinicamp.InvitedPlayerIds.Add(retired.PlayerId);
        var team = league.Teams[0];
        team.Roster.AddRange(new[] { Player("starter", overall: 60), Player("backup", overall: 90) });
        team.DepthChart["QB"] = new List<string> { "starter", "backup" };
        var result = new RetirementService().GenerateRetirementsForCurrentSeason(league);
        var record = Assert.Single(result.SeasonRecord.Players);
        Assert.Equal("Free agent", record.TeamName);
        Assert.Equal(retired.College, record.PlayerSnapshot.College);
        Assert.Equal(retired.CollegePlayerId, record.PlayerSnapshot.CollegePlayerId);
        Assert.Equal(3600, Assert.Single(record.PlayerSnapshot.CollegeCareerStats).PassingYards);
        Assert.Equal(61, Assert.Single(record.PlayerSnapshot.DevelopmentHistory).OverallBefore);
        Assert.Empty(league.FreeAgents);
        Assert.Empty(league.RookieMinicamp.InvitedPlayerIds);
        Assert.Equal(new[] { "starter", "backup" }, team.DepthChart["QB"]);
        retired.CareerStats[0].PassingYards = 1;
        Assert.Equal(5500, record.CareerStats[0].PassingYards);
        Assert.Equal(5500, record.PlayerSnapshot.CareerStats[0].PassingYards);
        var books = new RecordBookService(new GameCoreContext { ActiveLeague = league }).GetRecordBook();
        Assert.Contains(books.CareerRecords, r => r.SubjectId == retired.PlayerId);
        league.FreeAgents.Add(retired); // A stale ownership copy is removed by the completed checkpoint.
        new RetirementService().GenerateRetirementsForCurrentSeason(league);
        Assert.Empty(league.FreeAgents);
        Assert.Single(league.Transactions);
    }

    [Fact]
    public void ActiveRetirementRemovesOnlyDeparturesAndPreservesContractRightsSnapshot()
    {
        var league = League();
        var team = league.Teams[0];
        var retired = Player("retired-backup");
        retired.Status = "Retired";
        retired.Contract = new PlayerContractState { YearsRemaining = 2, AnnualSalary = 1_000_000m };
        team.Roster.AddRange(new[] { retired, Player("starter"), Player("backup", overall: 90) });
        team.DepthChart["QB"] = new List<string> { "starter", retired.PlayerId, "backup" };
        var record = Assert.Single(new RetirementService().GenerateRetirementsForCurrentSeason(league).SeasonRecord.Players);
        Assert.Equal(team.TeamId, record.TeamId);
        Assert.Equal(2, record.PlayerSnapshot.Contract.YearsRemaining);
        Assert.Equal(new[] { "starter", "backup" }, team.DepthChart["QB"]);
        Assert.Single(league.Transactions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SuccessfulAcquisitionResetsUnsignedTenureButRejectedOfferDoesNot(bool practice)
    {
        var league = League();
        league.Calendar.Phase = ScheduleService.TrainingCampPendingPhase;
        var player = Player("acquire", since: 2010);
        player.Age = 24;
        league.FreeAgents.Add(player);
        var context = new GameCoreContext { ActiveLeague = league };
        var contracts = new ContractService(context);
        var transactions = new TransactionService(context);
        if (practice)
        {
            var failure = transactions.SignToPracticeSquad(player.PlayerId, "user", contracts, 1m);
            Assert.False(failure.Accepted);
            Assert.Equal(2010, player.UnsignedSinceSeasonYear);
            var signed = transactions.SignToPracticeSquad(player.PlayerId, "user", contracts, 1_000_000m);
            Assert.True(signed.Accepted, signed.Message);
        }
        else
        {
            var offer = new ContractOffer { Years = 2, AnnualSalary = 1m, GuaranteedSalary = 1m };
            Assert.False(transactions.SignFreeAgent(player.PlayerId, "user", offer, contracts).Accepted);
            Assert.Equal(2010, player.UnsignedSinceSeasonYear);
            offer.AnnualSalary = 5_000_000m; offer.GuaranteedSalary = 1_000_000m;
            var signed = transactions.SignFreeAgent(player.PlayerId, "user", offer, contracts);
            Assert.True(signed.Accepted, signed.Message);
        }
        Assert.Equal(0, player.UnsignedSinceSeasonYear);
        Assert.True(transactions.ReleasePlayer(player.PlayerId, "user", contracts).Accepted);
        new RetirementService().GenerateRetirementsForCurrentSeason(league);
        Assert.Equal(league.SeasonYear, player.UnsignedSinceSeasonYear);
        Assert.Contains(player, league.FreeAgents);
    }

    [Fact]
    public void AssessmentDoesNotRetirePendingWaiversReservedOrDuplicateOwnedPlayers()
    {
        var league = League();
        var reserved = Player("reserved", since: 2010); reserved.Status = "Retired";
        league.Teams[0].InjuredReserve.Add(reserved);
        var waived = Player("waived", since: 2010); waived.Status = "Retired";
        league.Waivers.Add(new WaiverClaimState { Player = waived });
        var duplicate = Player("duplicate", since: 2010); duplicate.Status = "Retired";
        league.FreeAgents.Add(duplicate); league.Teams[0].PracticeSquad.Add(duplicate);
        var contracted = Player("contracted", since: 2010); contracted.Status = "Retired";
        contracted.Contract.YearsRemaining = 1; league.FreeAgents.Add(contracted);
        Assert.Equal(0, new RetirementService().GenerateRetirementsForCurrentSeason(league).RetiredCount);
        Assert.Single(league.Waivers);
        Assert.Single(league.Teams[0].InjuredReserve);
        Assert.Equal(2, league.FreeAgents.Count);
    }

    [Theory]
    [InlineData(35)]
    [InlineData(36)]
    public void NativeSavePreservesFutureDecisionsAndLegacyLoadDoesNotRetireOrReplay(int version)
    {
        var context = new GameCoreContext();
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json"))) directory = directory.Parent;
        Assert.NotNull(directory);
        new LeagueBootstrapService(context).CreateTestLeague(Path.Combine(directory.FullName, "Assets", "data_seed", "teams.json"));
        var league = context.ActiveLeague;
        league.FreeAgents.AddRange(Enumerable.Range(0, 100).Select(i => Player($"save-fa-{i}", since: 2020)));
        new RetirementService().GenerateRetirementsForCurrentSeason(league);
        league.SaveVersion = version;
        var savedRetirements = JsonSerializer.Serialize(league.RetirementHistory);
        var savedCount = league.FreeAgents.Count;
        var saves = new GameCoreSaveService();
        var file = $"retirement_test_{Guid.NewGuid():N}.json";
        try
        {
            Assert.True(saves.Save(context, file).Ok);
            var loaded = saves.Load(file);
            Assert.True(loaded.Ok, loaded.Message);
            Assert.Equal(36, loaded.League.SaveVersion);
            Assert.Equal(savedCount, loaded.League.FreeAgents.Count);
            Assert.Equal(savedRetirements, JsonSerializer.Serialize(loaded.League.RetirementHistory));
            var transactions = loaded.League.Transactions.Count;
            Assert.True(new RetirementService().GenerateRetirementsForCurrentSeason(loaded.League).Skipped);
            Assert.Equal(transactions, loaded.League.Transactions.Count);
            if (version < 36)
                Assert.All(loaded.League.FreeAgents, p => Assert.Equal(league.SeasonYear, p.UnsignedSinceSeasonYear));
            else
            {
                league.SeasonYear++; loaded.League.SeasonYear++;
                var expected = new RetirementService().GenerateRetirementsForCurrentSeason(league);
                var actual = new RetirementService().GenerateRetirementsForCurrentSeason(loaded.League);
                Assert.Equal(expected.SeasonRecord.Players.Select(p => p.PlayerId), actual.SeasonRecord.Players.Select(p => p.PlayerId));
            }
        }
        finally { saves.Delete(file); }
    }

    private static LeagueState League() => new()
    {
        UserTeamId = "user", SeasonYear = 2026,
        Calendar = new CalendarState { Phase = ScheduleService.RetirementPendingPhaseKey },
        Teams = new List<TeamState> { new() { TeamId = "user", Name = "User" } },
    };

    private static PlayerState Player(string id, int since = 0, int overall = 60) => new()
    {
        PlayerId = id, Name = id, Position = "QB", Age = 26, Overall = overall, Potential = overall,
        UnsignedSinceSeasonYear = since, Status = "Free Agent", Contract = new PlayerContractState { ContractType = "Free Agent" },
    };

    private static LeagueState Clone(LeagueState league) => JsonSerializer.Deserialize<LeagueState>(JsonSerializer.Serialize(league))!;
}
