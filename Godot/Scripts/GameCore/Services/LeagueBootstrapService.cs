using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class LeagueBootstrapService
{
    public const int PreseasonWeeks = 3;
    public const int PreseasonByeWeeks = 1;
    public const int RegularSeasonWeeks = 18;
    public const int RegularSeasonStartWeek = PreseasonWeeks + PreseasonByeWeeks + 1;
    public const int TotalSeasonWeeks = PreseasonWeeks + PreseasonByeWeeks + RegularSeasonWeeks;
    public const int TeamCount = 32;
    public const int PreseasonGamesPerWeek = TeamCount / 2;
    public const int RegularSeasonGamesPerTeam = 17;
    public const int RegularSeasonGameCount = (TeamCount * RegularSeasonGamesPerTeam) / 2;
    public const int ExpectedScheduleGameCount = (PreseasonWeeks * PreseasonGamesPerWeek) + RegularSeasonGameCount;
    public const int CoachesPerTeam = 5;
    public const int StartingProspectCount = 320;

    private static readonly (string Position, int Count, int BaseOverall)[] RosterPlan =
    {
        ("QB", 3, 77),
        ("RB", 4, 75),
        ("WR", 6, 76),
        ("TE", 3, 73),
        ("LT", 2, 74),
        ("LG", 2, 72),
        ("C", 2, 73),
        ("RG", 2, 72),
        ("RT", 2, 74),
        ("EDGE", 5, 76),
        ("DT", 4, 74),
        ("LB", 7, 73),
        ("CB", 5, 75),
        ("S", 4, 74),
        ("K", 1, 70),
        ("P", 1, 68),
    };

    private static readonly string[] CoachRoles = { "Head Coach", "Offensive Coordinator", "Defensive Coordinator", "Special Teams Coordinator", "Director of Player Personnel" };
    private static readonly string[] Colleges = { "North Valley", "Lakeshore State", "Western Tech", "Coastal University", "Pine Ridge", "Metro State", "Red River", "Summit College", "Atlantic State", "Prairie A&M", "Canyon University", "Great Lakes" };
    private static readonly (string Position, int BaseOverall)[] ProspectPlan =
    {
        ("QB", 68), ("RB", 66), ("WR", 67), ("TE", 65), ("LT", 64), ("LG", 63), ("C", 64), ("RG", 63), ("RT", 64),
        ("EDGE", 67), ("DT", 65), ("LB", 66), ("CB", 67), ("S", 65), ("K", 60), ("P", 59),
    };

    private readonly GameCoreContext _context;

    private static readonly TeamSeed[] TeamSeeds =
    {
        new("chi", "Chicago Blaze", "CHI", "North", "Atlas", 18_750_000m, 5),
        new("det", "Detroit Forge", "DET", "North", "Atlas", 14_500_000m, 9),
        new("min", "Minnesota Kings", "MIN", "North", "Atlas", 16_250_000m, 13),
        new("gb", "Green Bay Voyage", "GBY", "North", "Atlas", 15_300_000m, 17),
        new("dal", "Dallas Outlaws", "DAL", "South", "Atlas", 9_500_000m, 21),
        new("hou", "Houston Comets", "HOU", "South", "Atlas", 11_750_000m, 25),
        new("mem", "Memphis Sound", "MEM", "South", "Atlas", 13_900_000m, 29),
        new("atl", "Atlanta Pulse", "ATL", "South", "Atlas", 12_200_000m, 33),
        new("nyc", "New York Guardians", "NYG", "East", "Atlas", 12_250_000m, 37),
        new("bos", "Boston Harbor", "BOS", "East", "Atlas", 19_800_000m, 41),
        new("phi", "Philadelphia Foundry", "PHI", "East", "Atlas", 10_650_000m, 45),
        new("dc", "Capital District", "DCT", "East", "Atlas", 8_900_000m, 49),
        new("den", "Denver Summit", "DEN", "West", "Atlas", 17_200_000m, 53),
        new("lv", "Las Vegas Night", "LVG", "West", "Atlas", 20_100_000m, 57),
        new("sea", "Seattle Evergreen", "SEA", "West", "Atlas", 15_900_000m, 61),
        new("por", "Portland Pines", "POR", "West", "Atlas", 9_950_000m, 65),
        new("la", "Los Angeles Gold", "LAG", "Pacific", "Nova", 21_000_000m, 69),
        new("sf", "San Francisco Redwoods", "SFR", "Pacific", "Nova", 22_750_000m, 73),
        new("sd", "San Diego Breakers", "SDG", "Pacific", "Nova", 11_250_000m, 77),
        new("phx", "Phoenix Firebirds", "PHX", "Pacific", "Nova", 13_600_000m, 81),
        new("stl", "St. Louis Arches", "STL", "Central", "Nova", 16_800_000m, 85),
        new("kc", "Kansas City Crown", "KCC", "Central", "Nova", 18_400_000m, 89),
        new("okc", "Oklahoma Storm", "OKC", "Central", "Nova", 7_950_000m, 93),
        new("oma", "Omaha Plainsmen", "OMA", "Central", "Nova", 6_700_000m, 97),
        new("mia", "Miami Current", "MIA", "Coastal", "Nova", 14_900_000m, 101),
        new("orl", "Orlando Orbit", "ORL", "Coastal", "Nova", 10_250_000m, 105),
        new("tb", "Tampa Bay Tritons", "TBT", "Coastal", "Nova", 12_800_000m, 109),
        new("jax", "Jacksonville Armada", "JAX", "Coastal", "Nova", 8_300_000m, 113),
        new("buf", "Buffalo Lake Effect", "BUF", "Metro", "Nova", 9_700_000m, 117),
        new("pit", "Pittsburgh Iron", "PIT", "Metro", "Nova", 15_100_000m, 121),
        new("cle", "Cleveland Steam", "CLE", "Metro", "Nova", 10_900_000m, 125),
        new("cin", "Cincinnati Rivermen", "CIN", "Metro", "Nova", 11_400_000m, 129),
    };

    public LeagueBootstrapService(GameCoreContext context)
    {
        _context = context;
    }

    public LeagueState CreateTestLeague(string teamSeedPath = null, WorldDefinition world = null, GmProfile profile = null)
    {
        world ??= WorldDefinition.Standard();
        profile ??= new GmProfile();
        profile.Validate();
        var namePools = NamePoolService.Load(teamSeedPath);
        var teams = CreateLeagueTeams(teamSeedPath, world.Seed, namePools);
        var userTeam = teams[0];

        var league = new LeagueState
        {
            LeagueId = "vertical_slice",
            Name = "Gridiron GM Native League",
            SaveVersion = LeagueState.CurrentSaveVersion,
            SeasonYear = 2026,
            UserTeamId = userTeam.TeamId,
            FranchiseMetadata = new FranchiseMetadata { World = world, GmProfileSnapshot = profile.Snapshot() },
            Calendar = new CalendarState
            {
                Year = 2026,
                Week = 1,
                AbsoluteWeek = 1,
                PhaseWeek = 1,
                DayIndex = 0,
                Phase = "Preseason",
                CurrentDate = "2026-08-01",
                WeekLabel = ScheduleService.BuildCalendarWeekLabel(1),
            },
            Teams = teams,
            FreeAgents = BuildFreeAgents(world.Seed, namePools),
            CollegeProspects = BuildCollegeProspects(world.Seed, 2027, namePools),
            Schedule = new List<ScheduledGame>(),
            Results = new List<GameResult>(),
            PlayoffBracket = new PlayoffBracket(),
            LastContinueResult = new ContinueResult
            {
                Advanced = false,
                StopReason = "",
            },
        };

        league.Schedule = BuildDeterministicSchedule(league.Teams);
        new ContractService(_context).RefreshCapRoom(league);
        _context.ActiveLeague = league;
        new ScheduleService(_context).RefreshStatuses(league);
        return league;
    }

    public static List<ScheduledGame> BuildDeterministicSchedule(IReadOnlyList<TeamState> teams)
    {
        var schedule = new List<ScheduledGame>();
        if (teams == null || teams.Count < TeamCount)
            return schedule;

        var orderedTeams = teams
            .OrderBy(team => team.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(team => team.Division, StringComparer.OrdinalIgnoreCase)
            .ThenBy(team => team.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var atlasTeams = orderedTeams
            .Where(team => string.Equals(team.Conference, "Atlas", StringComparison.OrdinalIgnoreCase))
            .OrderBy(team => team.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var novaTeams = orderedTeams
            .Where(team => string.Equals(team.Conference, "Nova", StringComparison.OrdinalIgnoreCase))
            .OrderBy(team => team.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (atlasTeams.Count != TeamCount / 2 || novaTeams.Count != TeamCount / 2)
            return schedule;

        var preseasonRounds = BuildRoundRobinRounds(orderedTeams);
        for (var preseasonWeek = 1; preseasonWeek <= PreseasonWeeks; preseasonWeek++)
        {
            AddPairingsWeek(
                schedule,
                preseasonWeek,
                "preseason",
                preseasonRounds[preseasonWeek - 1],
                flipHomeAway: preseasonWeek % 2 == 0);
        }

        var atlasConferenceRounds = BuildRoundRobinRounds(atlasTeams);
        var novaConferenceRounds = BuildRoundRobinRounds(novaTeams);
        var absoluteWeek = RegularSeasonStartWeek;

        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 0);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[0], novaConferenceRounds[0], flipHomeAway: false);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 1);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[1], novaConferenceRounds[1], flipHomeAway: true);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 2);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[2], novaConferenceRounds[2], flipHomeAway: false);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 3);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[3], novaConferenceRounds[3], flipHomeAway: true);
        AddConferenceHalfWeek(schedule, absoluteWeek++, novaConferenceRounds[4], flipHomeAway: false);
        AddConferenceHalfWeek(schedule, absoluteWeek++, atlasConferenceRounds[4], flipHomeAway: true);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 4);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[5], novaConferenceRounds[5], flipHomeAway: false);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 5);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[6], novaConferenceRounds[6], flipHomeAway: true);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 6);
        AddConferenceWeek(schedule, absoluteWeek++, atlasConferenceRounds[7], novaConferenceRounds[7], flipHomeAway: false);
        AddCrossConferenceWeek(schedule, absoluteWeek++, atlasTeams, novaTeams, rotationOffset: 7);
        AddConferenceWeek(schedule, absoluteWeek, atlasConferenceRounds[8], novaConferenceRounds[8], flipHomeAway: true);

        return schedule;
    }

    private static List<TeamState> CreateLeagueTeams(string teamSeedPath, ulong worldSeed, GeneratedNamePools namePools)
    {
        var seeds = LoadTeamSeeds(teamSeedPath);
        return seeds
            .Select(seed => CreateTeam(
                seed.TeamId,
                seed.Name,
                seed.Abbreviation,
                seed.Division,
                seed.Conference,
                seed.CapRoom,
                seed.SeedOffset + (int)(worldSeed % 997),
                namePools))
            .ToList();
    }

    private static IReadOnlyList<TeamSeed> LoadTeamSeeds(string teamSeedPath)
    {
        if (string.IsNullOrWhiteSpace(teamSeedPath) || !File.Exists(teamSeedPath))
            return TeamSeeds;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var entries = JsonSerializer.Deserialize<List<TeamSeedAsset>>(File.ReadAllText(teamSeedPath), options);
            if (entries == null || entries.Count != TeamCount)
                return TeamSeeds;

            var seeds = entries
                .Where(entry =>
                    !string.IsNullOrWhiteSpace(entry.City)
                    && !string.IsNullOrWhiteSpace(entry.Name)
                    && !string.IsNullOrWhiteSpace(entry.Abbreviation)
                    && !string.IsNullOrWhiteSpace(entry.Conference)
                    && !string.IsNullOrWhiteSpace(entry.Division))
                .Select((entry, index) => new TeamSeed(
                    entry.Abbreviation.Trim().ToLowerInvariant(),
                    $"{entry.City.Trim()} {entry.Name.Trim()}",
                    entry.Abbreviation.Trim().ToUpperInvariant(),
                    entry.Division.Trim(),
                    entry.Conference.Trim(),
                    7_500_000m + (index * 425_000m),
                    5 + (index * 4)))
                .ToList();

            var hasUniqueAbbreviations = seeds
                .Select(seed => seed.Abbreviation)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() == TeamCount;
            var hasBalancedConferences = seeds
                .GroupBy(seed => seed.Conference, StringComparer.OrdinalIgnoreCase)
                .Count() == 2
                && seeds.GroupBy(seed => seed.Conference, StringComparer.OrdinalIgnoreCase).All(group => group.Count() == TeamCount / 2);

            return seeds.Count == TeamCount && hasUniqueAbbreviations && hasBalancedConferences
                ? seeds
                : TeamSeeds;
        }
        catch (IOException)
        {
            return TeamSeeds;
        }
        catch (JsonException)
        {
            return TeamSeeds;
        }
    }

    private static List<List<(TeamState HomeTeam, TeamState AwayTeam)>> BuildRoundRobinRounds(IReadOnlyList<TeamState> teams)
    {
        var rounds = new List<List<(TeamState HomeTeam, TeamState AwayTeam)>>();
        if (teams == null || teams.Count < 2 || teams.Count % 2 != 0)
            return rounds;

        var rotation = teams.ToList();
        var totalTeams = rotation.Count;
        var half = totalTeams / 2;
        for (var round = 0; round < totalTeams - 1; round++)
        {
            var pairings = new List<(TeamState HomeTeam, TeamState AwayTeam)>(half);
            for (var index = 0; index < half; index++)
            {
                pairings.Add((rotation[index], rotation[totalTeams - 1 - index]));
            }

            rounds.Add(pairings);
            rotation = new[] { rotation[0], rotation[^1] }
                .Concat(rotation.Skip(1).Take(totalTeams - 2))
                .ToList();
        }

        return rounds;
    }

    private static void AddCrossConferenceWeek(
        ICollection<ScheduledGame> schedule,
        int absoluteWeek,
        IReadOnlyList<TeamState> atlasTeams,
        IReadOnlyList<TeamState> novaTeams,
        int rotationOffset)
    {
        var pairings = new List<(TeamState HomeTeam, TeamState AwayTeam)>(atlasTeams.Count);
        for (var index = 0; index < atlasTeams.Count; index++)
        {
            var atlasTeam = atlasTeams[index];
            var novaTeam = novaTeams[(index + rotationOffset) % novaTeams.Count];
            pairings.Add(rotationOffset % 2 == 0
                ? (atlasTeam, novaTeam)
                : (novaTeam, atlasTeam));
        }

        AddPairingsWeek(schedule, absoluteWeek, "regular_season", pairings, flipHomeAway: false);
    }

    private static void AddConferenceWeek(
        ICollection<ScheduledGame> schedule,
        int absoluteWeek,
        IReadOnlyList<(TeamState HomeTeam, TeamState AwayTeam)> atlasPairings,
        IReadOnlyList<(TeamState HomeTeam, TeamState AwayTeam)> novaPairings,
        bool flipHomeAway)
    {
        var pairings = new List<(TeamState HomeTeam, TeamState AwayTeam)>(atlasPairings.Count + novaPairings.Count);
        pairings.AddRange(atlasPairings);
        pairings.AddRange(novaPairings);
        AddPairingsWeek(schedule, absoluteWeek, "regular_season", pairings, flipHomeAway);
    }

    private static void AddConferenceHalfWeek(
        ICollection<ScheduledGame> schedule,
        int absoluteWeek,
        IReadOnlyList<(TeamState HomeTeam, TeamState AwayTeam)> pairings,
        bool flipHomeAway)
    {
        AddPairingsWeek(schedule, absoluteWeek, "regular_season", pairings, flipHomeAway);
    }

    private static void AddPairingsWeek(
        ICollection<ScheduledGame> schedule,
        int absoluteWeek,
        string gameType,
        IReadOnlyList<(TeamState HomeTeam, TeamState AwayTeam)> pairings,
        bool flipHomeAway)
    {
        var phaseWeek = ScheduleService.GetDisplayWeek(gameType, absoluteWeek);
        var normalizedGameType = ScheduleService.NormalizeGameType(gameType);
        var slot = 1;
        foreach (var (scheduledHomeTeam, scheduledAwayTeam) in pairings)
        {
            if (scheduledHomeTeam == null || scheduledAwayTeam == null)
                continue;

            var homeTeam = flipHomeAway ? scheduledAwayTeam : scheduledHomeTeam;
            var awayTeam = flipHomeAway ? scheduledHomeTeam : scheduledAwayTeam;
            schedule.Add(new ScheduledGame
            {
                GameId = $"{(string.Equals(normalizedGameType, "preseason", StringComparison.OrdinalIgnoreCase) ? "ps" : "rs")}-{absoluteWeek:D2}-{slot:D2}",
                Week = absoluteWeek,
                AbsoluteWeek = absoluteWeek,
                PhaseWeek = phaseWeek,
                Phase = ScheduleService.GetPhaseForGameType(normalizedGameType),
                DayIndex = 2,
                GameType = normalizedGameType,
                WeekLabel = ScheduleService.BuildGameWeekLabel(normalizedGameType, absoluteWeek, phaseWeek),
                HomeTeamId = homeTeam.TeamId,
                AwayTeamId = awayTeam.TeamId,
                Status = "upcoming",
                HomeScore = null,
                AwayScore = null,
                Winner = "",
            });
            slot++;
        }
    }

    private readonly record struct TeamSeed(
        string TeamId,
        string Name,
        string Abbreviation,
        string Division,
        string Conference,
        decimal CapRoom,
        int SeedOffset);

    private sealed class TeamSeedAsset
    {
        public string City { get; set; } = "";
        public string Name { get; set; } = "";
        public string Abbreviation { get; set; } = "";
        public string Conference { get; set; } = "";
        public string Division { get; set; } = "";
    }

    private static TeamState CreateTeam(
        string teamId,
        string name,
        string abbreviation,
        string division,
        string conference,
        decimal capRoom,
        int seedOffset,
        GeneratedNamePools namePools)
    {
        var roster = BuildRoster(teamId, abbreviation, seedOffset, namePools);
        AssignStartingContracts(roster, LeagueState.DefaultSalaryCap - capRoom, seedOffset, 2026);
        return new TeamState
        {
            TeamId = teamId,
            Name = name,
            Abbreviation = abbreviation,
            Division = division,
            Conference = conference,
            Wins = 0,
            Losses = 0,
            Ties = 0,
            CapRoom = capRoom,
            Roster = roster,
            Coaches = BuildCoaches(teamId, seedOffset, namePools),
            DepthChart = BuildDepthChart(roster),
        };
    }

    private static List<CoachState> BuildCoaches(string teamId, int seedOffset, GeneratedNamePools namePools)
    {
        var coaches = new List<CoachState>(CoachesPerTeam);
        for (var index = 0; index < CoachesPerTeam; index++)
        {
            var value = StableValue(seedOffset, index, 43);
            coaches.Add(new CoachState
            {
                CoachId = $"{teamId}-coach-{index + 1}",
                Name = BuildName(namePools, value, StableValue(seedOffset, index, 59)),
                Role = CoachRoles[index],
                Overall = 58 + (value % 29),
                Age = 34 + ((value / 7) % 27),
            });
        }

        return coaches;
    }

    private static List<CollegeProspectState> BuildCollegeProspects(ulong worldSeed, int draftClassYear, GeneratedNamePools namePools)
    {
        var prospects = new List<CollegeProspectState>(StartingProspectCount);
        var seed = (int)(worldSeed % int.MaxValue);
        for (var index = 0; index < StartingProspectCount; index++)
        {
            var (position, baseOverall) = ProspectPlan[index % ProspectPlan.Length];
            var value = StableValue(seed, index, 97);
            var overall = Math.Clamp(baseOverall + ((value % 17) - 8), 52, 79);
            prospects.Add(new CollegeProspectState
            {
                ProspectId = $"draft-{draftClassYear}-{index + 1:D3}",
                Name = BuildName(namePools, value, StableValue(seed, index, 113)),
                Position = position,
                College = Colleges[(value / 13) % Colleges.Length],
                Overall = overall,
                Potential = Math.Clamp(overall + 4 + ((value / 29) % 18), overall, 95),
                Age = 20 + ((value / 31) % 3),
                DraftClassYear = draftClassYear,
            });
        }

        return prospects
            .OrderByDescending(prospect => prospect.Overall)
            .ThenByDescending(prospect => prospect.Potential)
            .ThenBy(prospect => prospect.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static List<PlayerState> BuildFreeAgents(ulong worldSeed, GeneratedNamePools namePools)
    {
        var players = new List<PlayerState>();
        var seed = (int)(worldSeed % int.MaxValue);
        for (var index = 0; index < 192; index++)
        {
            var (position, _, baseOverall) = RosterPlan[index % RosterPlan.Length];
            var value = StableValue(seed, index, 131);
            players.Add(new PlayerState
            {
                PlayerId = $"fa-{index + 1:D3}",
                Name = BuildName(namePools, value, StableValue(seed, index, 137)),
                Position = position,
                Overall = Math.Clamp(baseOverall - 7 + ((value % 15) - 7), 52, 77),
                Age = 23 + ((value / 11) % 11),
                Status = "Free Agent",
                Injury = "",
                Morale = 42 + ((value / 17) % 19),
                MoraleTrend = "Stable",
                Contract = new PlayerContractState { ContractType = "Free Agent" },
            });
        }

        return players.OrderByDescending(player => player.Overall).ThenBy(player => player.Name, StringComparer.Ordinal).ToList();
    }

    private static void AssignStartingContracts(IReadOnlyList<PlayerState> roster, decimal targetCommitments, int seedOffset, int seasonYear)
    {
        var weightedPlayers = roster.Select((player, index) => new
        {
            Player = player,
            Index = index,
            Weight = Math.Max(1, (player.Overall - 45) * (player.Overall - 45)),
        }).ToList();
        var totalWeight = weightedPlayers.Sum(entry => entry.Weight);
        var assigned = 0m;
        for (var listIndex = 0; listIndex < weightedPlayers.Count; listIndex++)
        {
            var entry = weightedPlayers[listIndex];
            var annualSalary = listIndex == weightedPlayers.Count - 1
                ? targetCommitments - assigned
                : Math.Round(targetCommitments * entry.Weight / totalWeight, 0, MidpointRounding.AwayFromZero);
            assigned += annualSalary;
            entry.Player.Morale = 45 + (StableValue(seedOffset, entry.Index, 149) % 22);
            entry.Player.MoraleTrend = "Stable";
            entry.Player.Contract = new PlayerContractState
            {
                AnnualSalary = annualSalary,
                GuaranteedSalary = Math.Round(annualSalary * (0.25m + (StableValue(seedOffset, entry.Index, 151) % 26) / 100m), 0),
                YearsRemaining = 1 + (StableValue(seedOffset, entry.Index, 157) % 4),
                SignedSeason = seasonYear,
                ContractType = "Standard",
            };
        }
    }

    private static string BuildName(GeneratedNamePools namePools, int firstSeed, int lastSeed)
        => $"{namePools.MaleFirstNames[PositiveIndex(firstSeed, namePools.MaleFirstNames.Count)]} {namePools.LastNames[PositiveIndex(lastSeed, namePools.LastNames.Count)]}";

    private static int PositiveIndex(int value, int count)
        => (int)((uint)value % (uint)count);

    private static int StableValue(int seed, int index, int salt)
    {
        unchecked
        {
            var value = (uint)seed;
            value ^= (uint)(index + 1) * 0x9E3779B9u;
            value ^= (uint)salt * 0x85EBCA6Bu;
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            return (int)(value & 0x7FFFFFFF);
        }
    }

    private static List<PlayerState> BuildRoster(string teamId, string abbreviation, int seedOffset, GeneratedNamePools namePools)
    {
        var roster = new List<PlayerState>();
        var positionUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rosterIndex = 0;

        foreach (var (position, count, baseOverall) in RosterPlan)
        {
            for (var depthIndex = 0; depthIndex < count; depthIndex++)
            {
                rosterIndex++;
                positionUsage[position] = positionUsage.TryGetValue(position, out var currentCount)
                    ? currentCount + 1
                    : 1;

                var playerSeed = seedOffset + (rosterIndex * 7) + (depthIndex * 3);
                var name = BuildName(
                    namePools,
                    StableValue(playerSeed, rosterIndex, 71),
                    StableValue(playerSeed, depthIndex + teamId.Length, 83));
                var overall = Math.Max(58, Math.Min(84, baseOverall - (depthIndex * 2) + ((playerSeed % 5) - 2)));
                var age = 22 + ((playerSeed + depthIndex) % 10);

                roster.Add(new PlayerState
                {
                    PlayerId = $"{teamId}-{position.ToLowerInvariant()}-{positionUsage[position]}",
                    Name = name,
                    Position = position,
                    Overall = overall,
                    Age = age,
                    Status = "Active",
                    Injury = string.Empty,
                });
            }
        }

        return roster
            .OrderBy(player => FootballPositionOrder.GetSortOrder(player.Position))
            .ThenBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(player => player.Overall)
            .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Dictionary<string, List<string>> BuildDepthChart(IEnumerable<PlayerState> roster)
    {
        return roster
            .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(player => player.Overall)
                    .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(player => player.PlayerId)
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);
    }
}
