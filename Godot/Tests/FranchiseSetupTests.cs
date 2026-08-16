using System;
using System.Linq;
using GridironGM.Domain;
using Xunit;

namespace GridironGM.Tests;

public sealed class FranchiseSetupTests
{
    [Fact]
    public void StandardWorldDefinitionIsFixedAndVersioned()
    {
        var first = WorldDefinition.Standard();
        var second = WorldDefinition.Standard();

        Assert.Equal(RosterSource.Standard, first.Source);
        Assert.Equal(WorldDefinition.StandardWorldSeed, first.Seed);
        Assert.Equal(first.Seed, second.Seed);
        Assert.Equal(first.GeneratorVersion, second.GeneratorVersion);
    }

    [Fact]
    public void GeneratedWorldDefinitionsKeepTheirOwnSeeds()
    {
        var first = WorldDefinition.Generated(101UL);
        var second = WorldDefinition.Generated(202UL);

        first.Validate();
        second.Validate();
        Assert.NotEqual(first.Seed, second.Seed);
        Assert.Equal(RosterSource.Generated, first.Source);
    }

    [Fact]
    public void ProfileSnapshotDoesNotChangeWhenTheReusableProfileChanges()
    {
        var profile = new GmProfile { Name = "Morgan Reed" };
        var snapshot = profile.Snapshot();

        profile.Name = "Changed Name";
        profile.Attributes.Negotiation = 70;

        Assert.Equal("Morgan Reed", snapshot.Name);
        Assert.Equal(50, snapshot.Attributes.Negotiation);
    }

    [Fact]
    public void GmModifiersStayWithinTheirDocumentedCaps()
    {
        var attributes = new GmAttributes
        {
            Negotiation = 80,
            PlayerManagement = 20,
            ScoutingJudgment = 80,
            Leadership = 20
        };

        attributes.Validate();

        Assert.InRange(attributes.ContractAttractivenessModifier, -0.05f, 0.05f);
        Assert.InRange(attributes.RetentionHappinessModifier, -5f, 5f);
        Assert.InRange(attributes.ScoutingUncertaintyModifier, -0.20f, 0.20f);
        Assert.InRange(attributes.CultureModifier, -5f, 5f);
    }

    [Fact]
    public void AttributeBudgetRejectsAnOverpoweredProfile()
    {
        var attributes = new GmAttributes
        {
            Negotiation = 80,
            PlayerManagement = 80,
            ScoutingJudgment = 80,
            Leadership = 80
        };

        Assert.Throws<ArgumentException>(attributes.Validate);
    }

    [Fact]
    public void TeamRostersAreDeterministicForTheSameSeed()
    {
        var first = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var second = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first[0].Name, second[0].Name);
        Assert.Equal(first[0].Overall, second[0].Overall);
        Assert.Equal(first[0].Potential, second[0].Potential);
        Assert.Equal(first[0].Position, second[0].Position);
    }

    [Fact]
    public void AutoFillDepthChartCreatesStartersAtEveryRequiredPosition()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var depthChart = LeagueSliceFactory.CreateDepthChart("lake", roster);

        var issues = LeagueSliceFactory.ValidateDepthChart(depthChart);

        Assert.Empty(issues);
        Assert.All(LeagueSliceFactory.DepthChartRequirements, requirement =>
        {
            var slot = Assert.Single(depthChart.Positions, position => position.Position == requirement.Position);
            Assert.True(slot.PlayerIds.Count >= requirement.Starters);
        });
    }

    [Fact]
    public void SetStarterPromotesTheSelectedPlayerToTheTopOfTheDepthChart()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var depthChart = LeagueSliceFactory.CreateDepthChart("lake", roster);
        var qbSlot = Assert.Single(depthChart.Positions, position => position.Position == "QB");
        Assert.True(qbSlot.PlayerIds.Count >= 2);
        var promotedPlayerId = qbSlot.PlayerIds[1];

        var changed = LeagueSliceFactory.ApplyDepthChartAction(depthChart, roster, "QB", promotedPlayerId, "set_starter");

        Assert.True(changed);
        Assert.Equal(promotedPlayerId, qbSlot.PlayerIds[0]);
    }

    [Fact]
    public void RosterValidationSeparatesActiveRosterFromPracticeSquadAndIr()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);

        var validation = LeagueSliceFactory.EvaluateRoster(roster);

        Assert.Equal(53, validation.RosterSize);
        Assert.Equal(0, validation.RequiredCuts);
        Assert.True(validation.InjuredCount >= 2);
        Assert.True(validation.IsValid);
    }

    [Fact]
    public void TeamReadinessDropsWhenStartingQuarterbackMovesToInjuredReserve()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var depthChart = LeagueSliceFactory.CreateDepthChart("lake", roster);
        foreach (var qbPlayer in roster.Where(player => player.Position == "QB"))
            qbPlayer.Overall = 55;
        var eliteStarterId = Assert.Single(depthChart.Positions, position => position.Position == "QB").PlayerIds[0];
        Assert.Single(roster, player => player.Id == eliteStarterId).Overall = 92;
        var healthyReadiness = LeagueSliceFactory.EvaluateTeamReadiness(72, roster, depthChart, 1234);

        var startingQuarterbackId = eliteStarterId;
        var quarterback = Assert.Single(roster, player => player.Id == startingQuarterbackId);
        quarterback.OnInjuredReserve = true;
        quarterback.RosterBucket = "injured_reserve";
        quarterback.Injury = new PlayerInjury { Status = "ir", Name = "Shoulder", DaysRemaining = 28, ReturnLabel = "Week +4" };
        var injuredReadiness = LeagueSliceFactory.EvaluateTeamReadiness(72, roster, depthChart, 1234);

        Assert.True(injuredReadiness.EffectiveStrength < healthyReadiness.EffectiveStrength);
        Assert.DoesNotContain(injuredReadiness.AvailableByPosition["QB"], player => player.Id == startingQuarterbackId);
    }

    [Fact]
    public void QuestionableStarterMayBeUnavailableButBackupCanStillFillTheSpot()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var depthChart = LeagueSliceFactory.CreateDepthChart("lake", roster);
        var qbSlot = Assert.Single(depthChart.Positions, position => position.Position == "QB");
        var starter = Assert.Single(roster, player => player.Id == qbSlot.PlayerIds[0]);
        starter.Injury = new PlayerInjury { Status = "questionable", Name = "Ankle", DaysRemaining = 3, ReturnLabel = "Soon" };

        var readiness = LeagueSliceFactory.EvaluateTeamReadiness(72, roster, depthChart, 0);

        Assert.True(readiness.AvailableByPosition.ContainsKey("QB"));
        Assert.NotEmpty(readiness.AvailableByPosition["QB"]);
        Assert.True(readiness.EffectiveStrength >= 45);
    }

    [Fact]
    public void WeeklyRecoveryClearsShortTermAndIrInjuriesWhenTimeExpires()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var shortTerm = roster.First(player => player.Position == "WR" && player.RosterBucket == "active");
        shortTerm.Injury = new PlayerInjury { Status = "questionable", Name = "Ankle sprain", DaysRemaining = 5, ReturnLabel = "Soon" };
        var irPlayer = roster.First(player => player.Position == "RB" && player.RosterBucket == "injured_reserve");
        irPlayer.OnInjuredReserve = true;
        irPlayer.Injury = new PlayerInjury { Status = "ir", Name = "High ankle sprain", DaysRemaining = 7, ReturnLabel = "Week +1" };

        var recovered = LeagueSliceFactory.AdvanceWeeklyRecovery(roster);

        Assert.True(recovered.Count >= 2);
        Assert.True(shortTerm.Injury.IsHealthy);
        Assert.True(irPlayer.Injury.IsHealthy);
        Assert.False(irPlayer.OnInjuredReserve);
        Assert.Equal("active", irPlayer.RosterBucket);
    }

    [Fact]
    public void PostGameInjuriesCanCreateNewUnavailablePlayers()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed)
            .Where(player => player.RosterBucket == "active")
            .Take(12)
            .ToList();

        foreach (var player in roster)
            player.Injury = new PlayerInjury();

        var injured = 0;
        for (uint seed = 1000; seed < 1100 && injured == 0; seed++)
            injured = LeagueSliceFactory.ApplyPostGameInjuries(roster, seed);

        Assert.True(injured > 0);
        Assert.Contains(roster, player => !player.Injury.IsHealthy || player.OnInjuredReserve);
    }

    [Fact]
    public void AutoRepairDepthChartRemovesUnavailableAssignedStarter()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var depthChart = LeagueSliceFactory.CreateDepthChart("lake", roster);
        var qbSlot = Assert.Single(depthChart.Positions, position => position.Position == "QB");
        var startingQuarterbackId = qbSlot.PlayerIds[0];
        var starter = Assert.Single(roster, player => player.Id == startingQuarterbackId);
        starter.OnInjuredReserve = true;
        starter.RosterBucket = "injured_reserve";
        starter.Injury = new PlayerInjury { Status = "ir", Name = "Shoulder", DaysRemaining = 28, ReturnLabel = "Week +4" };

        LeagueSliceFactory.AutoRepairDepthChart(depthChart, roster);

        Assert.DoesNotContain(startingQuarterbackId, qbSlot.PlayerIds);
        Assert.NotEmpty(qbSlot.PlayerIds);
    }

    [Fact]
    public void DepthChartValidationFlagsPlayableStarterGap()
    {
        var roster = LeagueSliceFactory.CreatePlayersForTeam("lake", WorldDefinition.StandardWorldSeed);
        var depthChart = LeagueSliceFactory.CreateDepthChart("lake", roster);
        var kickerSlot = Assert.Single(depthChart.Positions, position => position.Position == "K");
        var kicker = Assert.Single(roster, player => player.Id == kickerSlot.PlayerIds[0]);
        kicker.OnInjuredReserve = true;
        kicker.RosterBucket = "injured_reserve";
        kicker.Injury = new PlayerInjury { Status = "ir", Name = "Groin", DaysRemaining = 14, ReturnLabel = "Week +2" };

        var issues = LeagueSliceFactory.ValidateDepthChart(depthChart, roster);

        Assert.Contains(issues, issue => issue.Contains("K", StringComparison.Ordinal) && issue.Contains("playable", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StandingsUseHeadToHeadToBreakRegularSeasonTies()
    {
        var games = new[]
        {
            new SeasonGameSnapshot { Id = "g1", Week = 1, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "a", AwayTeamId = "b", HomeScore = 24, AwayScore = 17, Completed = true },
            new SeasonGameSnapshot { Id = "g2", Week = 1, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "c", AwayTeamId = "d", HomeScore = 27, AwayScore = 14, Completed = true },
            new SeasonGameSnapshot { Id = "g3", Week = 2, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "a", AwayTeamId = "c", HomeScore = 20, AwayScore = 28, Completed = true },
            new SeasonGameSnapshot { Id = "g4", Week = 2, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "d", HomeScore = 30, AwayScore = 13, Completed = true }
        };

        var standings = LeagueSliceFactory.BuildStandings(new[] { "a", "b", "c", "d" }, games);

        Assert.Equal(new[] { "c", "a", "b", "d" }, standings.Select(item => item.TeamId).ToArray());
    }

    [Fact]
    public void ConferencePlayoffSeedingTakesDivisionWinnersThenWildCards()
    {
        var teams = new[]
        {
            new LeagueTeamDefinition { Id = "a", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "b", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "c", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "d", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "e", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "f", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "g", Conference = "Union", Division = "West" },
            new LeagueTeamDefinition { Id = "h", Conference = "Union", Division = "West" }
        };
        var standings = new[]
        {
            new TeamStanding { TeamId = "a", Wins = 13, Losses = 4 },
            new TeamStanding { TeamId = "b", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "c", Wins = 12, Losses = 5 },
            new TeamStanding { TeamId = "d", Wins = 9, Losses = 8 },
            new TeamStanding { TeamId = "e", Wins = 11, Losses = 6 },
            new TeamStanding { TeamId = "f", Wins = 8, Losses = 9 },
            new TeamStanding { TeamId = "g", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "h", Wins = 7, Losses = 10 }
        };

        var seeds = LeagueSliceFactory.SelectConferencePlayoffSeeds(teams, standings, Array.Empty<SeasonGameSnapshot>(), "Union");

        Assert.Equal(new[] { "a", "c", "e", "g", "b", "d", "f" }, seeds.Select(seed => seed.TeamId).ToArray());
    }

    [Fact]
    public void DivisionTitleUsesDivisionRecordAfterSplitHeadToHead()
    {
        var teams = new[]
        {
            new LeagueTeamDefinition { Id = "a", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "b", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "c", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "d", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "e", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "f", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "g", Conference = "Union", Division = "West" }
        };
        var standings = new[]
        {
            new TeamStanding { TeamId = "a", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "b", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "c", Wins = 7, Losses = 10 },
            new TeamStanding { TeamId = "d", Wins = 6, Losses = 11 },
            new TeamStanding { TeamId = "e", Wins = 11, Losses = 6 },
            new TeamStanding { TeamId = "f", Wins = 9, Losses = 8 },
            new TeamStanding { TeamId = "g", Wins = 8, Losses = 9 }
        };
        var games = new[]
        {
            new SeasonGameSnapshot { Id = "g1", Week = 1, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "a", AwayTeamId = "b", HomeScore = 24, AwayScore = 17, Completed = true },
            new SeasonGameSnapshot { Id = "g2", Week = 9, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "a", HomeScore = 27, AwayScore = 20, Completed = true },
            new SeasonGameSnapshot { Id = "g3", Week = 2, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "a", AwayTeamId = "c", HomeScore = 21, AwayScore = 17, Completed = true },
            new SeasonGameSnapshot { Id = "g4", Week = 3, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "a", AwayTeamId = "d", HomeScore = 14, AwayScore = 10, Completed = true },
            new SeasonGameSnapshot { Id = "g5", Week = 4, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "c", AwayTeamId = "a", HomeScore = 13, AwayScore = 24, Completed = true },
            new SeasonGameSnapshot { Id = "g6", Week = 5, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "d", AwayTeamId = "a", HomeScore = 9, AwayScore = 16, Completed = true },
            new SeasonGameSnapshot { Id = "g7", Week = 6, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "c", HomeScore = 21, AwayScore = 20, Completed = true },
            new SeasonGameSnapshot { Id = "g8", Week = 7, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "d", HomeScore = 10, AwayScore = 13, Completed = true },
            new SeasonGameSnapshot { Id = "g9", Week = 8, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "c", AwayTeamId = "b", HomeScore = 17, AwayScore = 23, Completed = true },
            new SeasonGameSnapshot { Id = "g10", Week = 10, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "d", AwayTeamId = "b", HomeScore = 20, AwayScore = 27, Completed = true }
        };

        var seeds = LeagueSliceFactory.SelectConferencePlayoffSeeds(teams, standings, games, "Union");

        Assert.Contains(seeds, seed => seed.TeamId == "a" && seed.Seed <= 4);
        Assert.Contains(seeds, seed => seed.TeamId == "b" && seed.Seed >= 5);
    }

    [Fact]
    public void WildCardOrderingUsesConferenceRecordForTiedTeams()
    {
        var teams = new[]
        {
            new LeagueTeamDefinition { Id = "a", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "b", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "c", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "d", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "e", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "f", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "g", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "h", Conference = "Union", Division = "West" },
            new LeagueTeamDefinition { Id = "i", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "j", Conference = "Union", Division = "West" }
        };
        var standings = new[]
        {
            new TeamStanding { TeamId = "a", Wins = 12, Losses = 5 },
            new TeamStanding { TeamId = "b", Wins = 9, Losses = 8 },
            new TeamStanding { TeamId = "c", Wins = 11, Losses = 6 },
            new TeamStanding { TeamId = "d", Wins = 9, Losses = 8 },
            new TeamStanding { TeamId = "e", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "f", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "g", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "h", Wins = 10, Losses = 7 },
            new TeamStanding { TeamId = "i", Wins = 8, Losses = 9 },
            new TeamStanding { TeamId = "j", Wins = 8, Losses = 9 }
        };
        var games = new[]
        {
            new SeasonGameSnapshot { Id = "g1", Week = 1, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "e", AwayTeamId = "c", HomeScore = 20, AwayScore = 17, Completed = true },
            new SeasonGameSnapshot { Id = "g2", Week = 2, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "e", AwayTeamId = "d", HomeScore = 23, AwayScore = 16, Completed = true },
            new SeasonGameSnapshot { Id = "g3", Week = 3, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "e", AwayTeamId = "g", HomeScore = 14, AwayScore = 21, Completed = true },
            new SeasonGameSnapshot { Id = "g4", Week = 4, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "e", AwayTeamId = "h", HomeScore = 27, AwayScore = 20, Completed = true },
            new SeasonGameSnapshot { Id = "g5", Week = 5, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "a", HomeScore = 14, AwayScore = 17, Completed = true },
            new SeasonGameSnapshot { Id = "g6", Week = 6, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "b", HomeScore = 17, AwayScore = 20, Completed = true },
            new SeasonGameSnapshot { Id = "g7", Week = 7, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "g", HomeScore = 24, AwayScore = 21, Completed = true },
            new SeasonGameSnapshot { Id = "g8", Week = 8, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "h", HomeScore = 21, AwayScore = 18, Completed = true }
        };

        var seeds = LeagueSliceFactory.SelectConferencePlayoffSeeds(teams, standings, games, "Union");

        Assert.True(Array.IndexOf(seeds.Select(seed => seed.TeamId).ToArray(), "e") < Array.IndexOf(seeds.Select(seed => seed.TeamId).ToArray(), "f"));
    }

    [Fact]
    public void PlayoffRaceStatusMarksClinchedDivisionWhenRivalsCannotCatchLeader()
    {
        var teams = new[]
        {
            new LeagueTeamDefinition { Id = "a", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "b", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "c", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "d", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "e", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "f", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "g", Conference = "Union", Division = "West" },
            new LeagueTeamDefinition { Id = "h", Conference = "Union", Division = "West" }
        };
        var standings = new[]
        {
            new TeamStanding { TeamId = "a", Wins = 14, Losses = 0 },
            new TeamStanding { TeamId = "b", Wins = 10, Losses = 4 },
            new TeamStanding { TeamId = "c", Wins = 11, Losses = 3 },
            new TeamStanding { TeamId = "d", Wins = 8, Losses = 6 },
            new TeamStanding { TeamId = "e", Wins = 9, Losses = 5 },
            new TeamStanding { TeamId = "f", Wins = 7, Losses = 7 },
            new TeamStanding { TeamId = "g", Wins = 9, Losses = 5 },
            new TeamStanding { TeamId = "h", Wins = 6, Losses = 8 }
        };
        var remainingGames = new[]
        {
            new SeasonGameSnapshot { Id = "u1", Week = 15, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "c", Completed = false },
            new SeasonGameSnapshot { Id = "u2", Week = 16, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "d", Completed = false },
            new SeasonGameSnapshot { Id = "u3", Week = 17, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "b", AwayTeamId = "e", Completed = false }
        };

        var statuses = LeagueSliceFactory.BuildPlayoffRaceStatuses(teams, standings, remainingGames);

        Assert.True(statuses["a"].ClinchedDivision);
        Assert.Equal("Clinched Division", statuses["a"].StatusLabel);
    }

    [Fact]
    public void PlayoffRaceStatusMarksTeamEliminatedWhenConferenceFieldIsOutOfReach()
    {
        var teams = new[]
        {
            new LeagueTeamDefinition { Id = "a", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "b", Conference = "Union", Division = "East" },
            new LeagueTeamDefinition { Id = "c", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "d", Conference = "Union", Division = "North" },
            new LeagueTeamDefinition { Id = "e", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "f", Conference = "Union", Division = "South" },
            new LeagueTeamDefinition { Id = "g", Conference = "Union", Division = "West" },
            new LeagueTeamDefinition { Id = "h", Conference = "Union", Division = "West" }
        };
        var standings = new[]
        {
            new TeamStanding { TeamId = "a", Wins = 12, Losses = 2 },
            new TeamStanding { TeamId = "b", Wins = 10, Losses = 4 },
            new TeamStanding { TeamId = "c", Wins = 11, Losses = 3 },
            new TeamStanding { TeamId = "d", Wins = 9, Losses = 5 },
            new TeamStanding { TeamId = "e", Wins = 10, Losses = 4 },
            new TeamStanding { TeamId = "f", Wins = 5, Losses = 9 },
            new TeamStanding { TeamId = "g", Wins = 9, Losses = 5 },
            new TeamStanding { TeamId = "h", Wins = 9, Losses = 5 }
        };
        var remainingGames = new[]
        {
            new SeasonGameSnapshot { Id = "u1", Week = 15, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "a", Completed = false },
            new SeasonGameSnapshot { Id = "u2", Week = 16, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "b", Completed = false },
            new SeasonGameSnapshot { Id = "u3", Week = 17, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "f", AwayTeamId = "c", Completed = false }
        };

        var statuses = LeagueSliceFactory.BuildPlayoffRaceStatuses(teams, standings, remainingGames);

        Assert.True(statuses["f"].Eliminated);
        Assert.Equal("Eliminated", statuses["f"].StatusLabel);
    }

    [Fact]
    public void WildCardAndDivisionalBracketFollowConferenceSeedOrder()
    {
        var seeds = new[]
        {
            new PlayoffSeed { Seed = 1, TeamId = "a" },
            new PlayoffSeed { Seed = 2, TeamId = "b" },
            new PlayoffSeed { Seed = 3, TeamId = "c" },
            new PlayoffSeed { Seed = 4, TeamId = "d" },
            new PlayoffSeed { Seed = 5, TeamId = "e" },
            new PlayoffSeed { Seed = 6, TeamId = "f" },
            new PlayoffSeed { Seed = 7, TeamId = "g" }
        };

        var wildCardGames = LeagueSliceFactory.CreateWildCardGames(seeds, "Union");
        var completedWildCardGames = new[]
        {
            new SeasonGameSnapshot
            {
                Id = wildCardGames[0].Id, Week = LeagueSliceFactory.WildCardWeek, GameType = LeagueSliceFactory.PlayoffWildCardGameType,
                HomeTeamId = "b", AwayTeamId = "g", HomeSeed = 2, AwaySeed = 7, HomeScore = 27, AwayScore = 17, Completed = true
            },
            new SeasonGameSnapshot
            {
                Id = wildCardGames[1].Id, Week = LeagueSliceFactory.WildCardWeek, GameType = LeagueSliceFactory.PlayoffWildCardGameType,
                HomeTeamId = "c", AwayTeamId = "f", HomeSeed = 3, AwaySeed = 6, HomeScore = 21, AwayScore = 24, Completed = true
            },
            new SeasonGameSnapshot
            {
                Id = wildCardGames[2].Id, Week = LeagueSliceFactory.WildCardWeek, GameType = LeagueSliceFactory.PlayoffWildCardGameType,
                HomeTeamId = "d", AwayTeamId = "e", HomeSeed = 4, AwaySeed = 5, HomeScore = 24, AwayScore = 20, Completed = true
            }
        };

        var divisionalGames = LeagueSliceFactory.CreateDivisionalGames(seeds, completedWildCardGames, "Union");

        Assert.Equal(3, wildCardGames.Count);
        Assert.Equal(("b", "g"), (wildCardGames[0].HomeTeamId, wildCardGames[0].AwayTeamId));
        Assert.Equal(("c", "f"), (wildCardGames[1].HomeTeamId, wildCardGames[1].AwayTeamId));
        Assert.Equal(("d", "e"), (wildCardGames[2].HomeTeamId, wildCardGames[2].AwayTeamId));
        Assert.Equal(2, divisionalGames.Count);
        Assert.Equal(("a", "f"), (divisionalGames[0].HomeTeamId, divisionalGames[0].AwayTeamId));
        Assert.Equal(("b", "d"), (divisionalGames[1].HomeTeamId, divisionalGames[1].AwayTeamId));
    }

    [Fact]
    public void ChampionshipGameUsesConferenceWinnersAndPreservesHigherSeedHomeField()
    {
        var conferenceChampionshipGames = new[]
        {
            new SeasonGameSnapshot
            {
                Id = "w20-union-1", Week = LeagueSliceFactory.ConferenceChampionshipWeek, GameType = LeagueSliceFactory.PlayoffConferenceChampionshipGameType,
                HomeTeamId = "a", AwayTeamId = "d", HomeSeed = 1, AwaySeed = 4, HomeScore = 27, AwayScore = 13, Completed = true
            },
            new SeasonGameSnapshot
            {
                Id = "w20-continental-1", Week = LeagueSliceFactory.ConferenceChampionshipWeek, GameType = LeagueSliceFactory.PlayoffConferenceChampionshipGameType,
                HomeTeamId = "m", AwayTeamId = "n", HomeSeed = 2, AwaySeed = 6, HomeScore = 24, AwayScore = 20, Completed = true
            }
        };

        var championship = LeagueSliceFactory.CreateChampionshipGame(conferenceChampionshipGames);

        Assert.NotNull(championship);
        Assert.Equal(LeagueSliceFactory.ChampionshipGameType, championship.GameType);
        Assert.Equal("a", championship.HomeTeamId);
        Assert.Equal("m", championship.AwayTeamId);
        Assert.Equal(1, championship.HomeSeed);
        Assert.Equal(2, championship.AwaySeed);
    }

    [Fact]
    public void SeasonArchiveCapturesChampionRunnerUpAndTeamRanks()
    {
        var teams = LeagueSliceFactory.CreateDefaultLeagueTeams();
        var standings = teams
            .Select(team => new TeamStanding { TeamId = team.Id, Wins = 0, Losses = 17, Ties = 0, PointsFor = 200, PointsAgainst = 400 })
            .ToList();

        standings.Single(item => item.TeamId == "capital").Wins = 13;
        standings.Single(item => item.TeamId == "capital").Losses = 4;
        standings.Single(item => item.TeamId == "capital").PointsFor = 410;
        standings.Single(item => item.TeamId == "capital").PointsAgainst = 290;

        standings.Single(item => item.TeamId == "harbor").Wins = 11;
        standings.Single(item => item.TeamId == "harbor").Losses = 6;
        standings.Single(item => item.TeamId == "harbor").PointsFor = 360;
        standings.Single(item => item.TeamId == "harbor").PointsAgainst = 310;

        standings.Single(item => item.TeamId == "liberty").Wins = 12;
        standings.Single(item => item.TeamId == "liberty").Losses = 5;
        standings.Single(item => item.TeamId == "liberty").PointsFor = 395;
        standings.Single(item => item.TeamId == "liberty").PointsAgainst = 300;

        var games = new[]
        {
            new SeasonGameSnapshot
            {
                Id = "title",
                Week = LeagueSliceFactory.ChampionshipWeek,
                GameType = LeagueSliceFactory.ChampionshipGameType,
                HomeTeamId = "capital",
                AwayTeamId = "liberty",
                HomeScore = 27,
                AwayScore = 20,
                Completed = true,
                HomeSeed = 1,
                AwaySeed = 1
            }
        };

        var archive = LeagueSliceFactory.BuildSeasonArchive(2026, teams, standings, games);

        Assert.NotNull(archive);
        Assert.Equal(2026, archive.SeasonYear);
        Assert.Equal("capital", archive.ChampionTeamId);
        Assert.Equal("liberty", archive.RunnerUpTeamId);
        Assert.Equal("Capital Sentinels", archive.ChampionDisplayName);
        Assert.Equal(27, archive.ChampionScore);
        Assert.Equal(20, archive.RunnerUpScore);

        var championSeason = archive.Teams.Single(item => item.TeamId == "capital");
        Assert.Equal(1, championSeason.DivisionRank);
        Assert.Equal(1, championSeason.ConferenceRank);
        Assert.True(championSeason.PlayoffSeed > 0);
    }

    [Fact]
    public void StandingsIgnorePlayoffResults()
    {
        var games = new[]
        {
            new SeasonGameSnapshot { Id = "reg1", Week = 1, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "a", AwayTeamId = "b", HomeScore = 21, AwayScore = 14, Completed = true },
            new SeasonGameSnapshot { Id = "reg2", Week = 1, GameType = LeagueSliceFactory.RegularSeasonGameType, HomeTeamId = "c", AwayTeamId = "d", HomeScore = 17, AwayScore = 10, Completed = true },
            new SeasonGameSnapshot { Id = "wild", Week = LeagueSliceFactory.WildCardWeek, GameType = LeagueSliceFactory.PlayoffWildCardGameType, HomeTeamId = "b", AwayTeamId = "a", HomeScore = 35, AwayScore = 7, Completed = true }
        };

        var standings = LeagueSliceFactory.BuildStandings(new[] { "a", "b", "c", "d" }, games);

        Assert.Equal(1, standings.Single(item => item.TeamId == "a").Wins);
        Assert.Equal(0, standings.Single(item => item.TeamId == "b").Wins);
    }

    [Fact]
    public void DefaultLeagueContainsThirtyTwoTeamsAcrossTwoConferencesAndEightDivisions()
    {
        var teams = LeagueSliceFactory.CreateDefaultLeagueTeams();

        Assert.Equal(LeagueSliceFactory.LeagueTeamCount, teams.Count);
        Assert.Equal(2, teams.Select(team => team.Conference).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(8, teams.Select(team => $"{team.Conference}:{team.Division}").Distinct(StringComparer.Ordinal).Count());
        Assert.All(teams, team => Assert.False(string.IsNullOrWhiteSpace(team.Abbreviation)));
    }

    [Fact]
    public void LeagueCalendarStartsOnTuesdayAndUsesSundayGameDays()
    {
        var start = LeagueSliceFactory.GetLeagueYearStartDate(2026);
        var weekOneGameDay = LeagueSliceFactory.GetGameDayDate(2026, 1);

        Assert.Equal(new DateTime(2026, 9, 1), start);
        Assert.Equal(DayOfWeek.Tuesday, start.DayOfWeek);
        Assert.Equal(new DateTime(2026, 9, 6), weekOneGameDay);
        Assert.Equal(DayOfWeek.Sunday, weekOneGameDay.DayOfWeek);
    }

    [Fact]
    public void FootballWeekAdvancesFromCalendarDate()
    {
        var weekOneStart = LeagueSliceFactory.GetWeekStartDate(2026, 1);
        var weekTwoStart = LeagueSliceFactory.GetWeekStartDate(2026, 2);

        Assert.Equal(1, LeagueSliceFactory.GetFootballWeekForDate(2026, weekOneStart, LeagueSliceFactory.MaxSeasonWeek));
        Assert.Equal(1, LeagueSliceFactory.GetFootballWeekForDate(2026, weekOneStart.AddDays(6), LeagueSliceFactory.MaxSeasonWeek));
        Assert.Equal(2, LeagueSliceFactory.GetFootballWeekForDate(2026, weekTwoStart, LeagueSliceFactory.MaxSeasonWeek));
    }

    [Fact]
    public void LeagueCalendarMilestonesExposeTodayAndNextLeagueDates()
    {
        var seasonStart = LeagueSliceFactory.GetLeagueYearStartDate(2026);

        var today = LeagueSliceFactory.GetLeagueCalendarMilestonesForDate(2026, seasonStart);
        var next = LeagueSliceFactory.GetNextLeagueCalendarMilestone(2026, seasonStart);

        var openingMilestone = Assert.Single(today);
        Assert.Equal("regular_season_week_1", openingMilestone.Id);
        Assert.NotNull(next);
        Assert.Equal("regular_season_week_5", next.Id);
        Assert.True(next.Date > openingMilestone.Date);
    }

    [Fact]
    public void OffseasonCalendarMilestonesFollowExpectedOrder()
    {
        var championship = LeagueSliceFactory.GetGameDayDate(2026, LeagueSliceFactory.ChampionshipWeek);
        var offseasonOpen = LeagueSliceFactory.GetOffseasonOpenDate(2026);
        var retirements = LeagueSliceFactory.GetRetirementDecisionsDate(2026);
        var leagueYearReset = LeagueSliceFactory.GetNewLeagueYearDate(2026);
        var freeAgency = LeagueSliceFactory.GetFreeAgencyOpenDate(2026);
        var draftPrep = LeagueSliceFactory.GetDraftPrepDate(2026);
        var draftWeek = LeagueSliceFactory.GetDraftWeekStartDate(2026);
        var nextSeason = LeagueSliceFactory.GetLeagueYearStartDate(2027);

        Assert.Equal(championship.AddDays(1), offseasonOpen);
        Assert.Equal(DayOfWeek.Tuesday, retirements.DayOfWeek);
        Assert.Equal(DayOfWeek.Wednesday, leagueYearReset.DayOfWeek);
        Assert.Equal(3, leagueYearReset.Month);
        Assert.Equal(leagueYearReset.AddDays(1), freeAgency);
        Assert.Equal(DayOfWeek.Monday, draftPrep.DayOfWeek);
        Assert.Equal(DayOfWeek.Monday, draftWeek.DayOfWeek);
        Assert.True(offseasonOpen < retirements);
        Assert.True(retirements < leagueYearReset);
        Assert.True(leagueYearReset < freeAgency);
        Assert.True(freeAgency < draftPrep);
        Assert.True(draftPrep < draftWeek);
        Assert.True(draftWeek < nextSeason);
    }

    [Fact]
    public void PrototypeRegularSeasonScheduleCreatesEighteenWeeksWithStructuredByeWeeks()
    {
        var teams = LeagueSliceFactory.CreateDefaultLeagueTeams();

        var schedule = LeagueSliceFactory.CreatePrototypeRegularSeasonSchedule(teams);

        Assert.Equal(LeagueSliceFactory.LeagueTeamCount * 17 / 2, schedule.Count);
        Assert.Equal(LeagueSliceFactory.RegularSeasonWeeks, schedule.Max(game => game.Week));
        Assert.Equal(LeagueSliceFactory.RegularSeasonWeeks, schedule.Select(game => game.Week).Distinct().Count());
        Assert.All(teams, team =>
        {
            var gamesForTeam = schedule.Count(game => game.HomeTeamId == team.Id || game.AwayTeamId == team.Id);
            Assert.Equal(17, gamesForTeam);
        });
        var weeklyGameCounts = Enumerable.Range(1, LeagueSliceFactory.RegularSeasonWeeks)
            .ToDictionary(week => week, week => schedule.Count(game => game.Week == week));

        Assert.Equal(12, weeklyGameCounts[6]);
        Assert.Equal(12, weeklyGameCounts[8]);
        Assert.Equal(12, weeklyGameCounts[10]);
        Assert.Equal(12, weeklyGameCounts[12]);
        Assert.All(weeklyGameCounts.Where(entry => entry.Key is not (6 or 8 or 10 or 12)), entry => Assert.Equal(16, entry.Value));
    }
}
