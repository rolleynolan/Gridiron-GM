using System;
using System.Collections.Generic;
using System.Linq;
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

    private static readonly string[] FirstNames =
    {
        "Evan", "Mason", "Noah", "Jalen", "Theo", "Cole", "Avery", "Brett",
        "Dylan", "Owen", "Grant", "Luca", "Rhett", "Miles", "Parker", "Cal",
        "Jace", "Ty", "Zane", "Nico", "Gavin", "Eli", "Milo", "Roman",
        "Hudson", "Carter", "Logan", "Declan", "Brooks", "Wyatt", "Sawyer", "Blake",
    };

    private static readonly string[] LastNames =
    {
        "Cross", "Pike", "Vale", "Frost", "Hart", "Mercer", "Stone", "North",
        "Reed", "Hale", "Wells", "Knox", "Shaw", "Gage", "Finn", "Ward",
        "Voss", "Boone", "Ellis", "Lane", "Price", "York", "Grant", "Rowe",
        "Bishop", "Sutton", "Drake", "Rhodes", "Bennett", "Holland", "Keller", "Maddox",
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

    public LeagueState CreateTestLeague()
    {
        var teams = CreateLeagueTeams();
        var userTeam = teams[0];

        var league = new LeagueState
        {
            LeagueId = "vertical_slice",
            Name = "Gridiron GM Native League",
            SaveVersion = LeagueState.CurrentSaveVersion,
            SeasonYear = 2026,
            UserTeamId = userTeam.TeamId,
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

    private static List<TeamState> CreateLeagueTeams()
    {
        return TeamSeeds
            .Select(seed => CreateTeam(
                seed.TeamId,
                seed.Name,
                seed.Abbreviation,
                seed.Division,
                seed.Conference,
                seed.CapRoom,
                seed.SeedOffset))
            .ToList();
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

    private static TeamState CreateTeam(
        string teamId,
        string name,
        string abbreviation,
        string division,
        string conference,
        decimal capRoom,
        int seedOffset)
    {
        var roster = BuildRoster(teamId, abbreviation, seedOffset);
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
            DepthChart = BuildDepthChart(roster),
        };
    }

    private static List<PlayerState> BuildRoster(string teamId, string abbreviation, int seedOffset)
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
                var firstName = FirstNames[playerSeed % FirstNames.Length];
                var lastName = LastNames[(playerSeed + rosterIndex + teamId.Length) % LastNames.Length];
                var name = $"{firstName} {lastName}";
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
