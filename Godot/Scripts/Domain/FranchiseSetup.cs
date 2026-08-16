#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace GridironGM.Domain
{
    public enum RosterSource
    {
        Standard,
        Generated
    }

    public sealed class GmAttributes
    {
        public const int MinimumRating = 20;
        public const int MaximumRating = 80;
        public const int MaximumTotal = 220;

        public int Negotiation { get; set; } = 50;
        public int PlayerManagement { get; set; } = 50;
        public int ScoutingJudgment { get; set; } = 50;
        public int Leadership { get; set; } = 50;

        public int Total => Negotiation + PlayerManagement + ScoutingJudgment + Leadership;

        public void Validate()
        {
            ValidateRating(Negotiation, nameof(Negotiation));
            ValidateRating(PlayerManagement, nameof(PlayerManagement));
            ValidateRating(ScoutingJudgment, nameof(ScoutingJudgment));
            ValidateRating(Leadership, nameof(Leadership));
            if (Total > MaximumTotal)
                throw new ArgumentException($"GM attribute total may not exceed {MaximumTotal}.");
        }

        public float ContractAttractivenessModifier => Scale(Negotiation, 0.05f);
        public float RetentionHappinessModifier => Scale(PlayerManagement, 5f);
        public float ScoutingUncertaintyModifier => -Scale(ScoutingJudgment, 0.20f);
        public float CultureModifier => Scale(Leadership, 5f);

        private static float Scale(int rating, float maximumEffect)
            => Math.Clamp((rating - 50) / 30f * maximumEffect, -maximumEffect, maximumEffect);

        private static void ValidateRating(int value, string name)
        {
            if (value < MinimumRating || value > MaximumRating)
                throw new ArgumentOutOfRangeException(name, $"{name} must be between {MinimumRating} and {MaximumRating}.");
        }
    }

    public sealed class CharacterDesign
    {
        public string Pronouns { get; set; } = "They/Them";
        public string HairStyle { get; set; } = "Short";
        public string HairColor { get; set; } = "Brown";
        public string SkinTone { get; set; } = "Medium";
        public string Outfit { get; set; } = "Team Polo";
    }

    public sealed class GmProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "User GM";
        public GmAttributes Attributes { get; set; } = new();
        public CharacterDesign Appearance { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Id))
                throw new ArgumentException("GM profile id is required.", nameof(Id));
            if (string.IsNullOrWhiteSpace(Name))
                throw new ArgumentException("GM name is required.", nameof(Name));
            Attributes ??= new GmAttributes();
            Appearance ??= new CharacterDesign();
            Attributes.Validate();
        }

        public GmProfile Snapshot()
        {
            Validate();
            return new GmProfile
            {
                Id = Id,
                Name = Name,
                Attributes = new GmAttributes
                {
                    Negotiation = Attributes.Negotiation,
                    PlayerManagement = Attributes.PlayerManagement,
                    ScoutingJudgment = Attributes.ScoutingJudgment,
                    Leadership = Attributes.Leadership
                },
                Appearance = new CharacterDesign
                {
                    Pronouns = Appearance.Pronouns,
                    HairStyle = Appearance.HairStyle,
                    HairColor = Appearance.HairColor,
                    SkinTone = Appearance.SkinTone,
                    Outfit = Appearance.Outfit
                },
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }

    public sealed class WorldDefinition
    {
        public const int CurrentGeneratorVersion = 1;
        public const ulong StandardWorldSeed = 0x4752494449524F4EUL;

        public RosterSource Source { get; set; } = RosterSource.Standard;
        public ulong Seed { get; set; } = StandardWorldSeed;
        public int GeneratorVersion { get; set; } = CurrentGeneratorVersion;

        public static WorldDefinition Standard() => new();
        public static WorldDefinition Generated(ulong seed) => new() { Source = RosterSource.Generated, Seed = seed };

        public void Validate()
        {
            if (GeneratorVersion <= 0 || GeneratorVersion > CurrentGeneratorVersion)
                throw new ArgumentOutOfRangeException(nameof(GeneratorVersion), "Unknown world generator version.");
            if (Source == RosterSource.Standard && Seed != StandardWorldSeed)
                throw new ArgumentException("The Standard Roster must use the fixed standard world seed.");
        }
    }

    public sealed class FranchiseMetadata
    {
        public int SaveSchemaVersion { get; set; } = 6;
        public WorldDefinition World { get; set; } = WorldDefinition.Standard();
        public GmProfile GmProfileSnapshot { get; set; } = new();

        public void Validate()
        {
            World ??= WorldDefinition.Standard();
            GmProfileSnapshot ??= new GmProfile();
            World.Validate();
            GmProfileSnapshot.Validate();
        }
    }

    public sealed class PlayerInjury
    {
        public string Status { get; set; } = "healthy";
        public string Name { get; set; } = "";
        public int DaysRemaining { get; set; }
        public string ReturnLabel { get; set; } = "";

        public bool IsHealthy => string.Equals(Status, "healthy", StringComparison.OrdinalIgnoreCase) || DaysRemaining <= 0;
    }

    public sealed class Player
    {
        public string Id { get; set; } = "";
        public string TeamId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Position { get; set; } = "";
        public int Age { get; set; }
        public int Overall { get; set; }
        public int Potential { get; set; }
        public int JerseyNumber { get; set; }
        public string RosterBucket { get; set; } = "active";
        public bool OnInjuredReserve { get; set; }
        public PlayerInjury Injury { get; set; } = new();
        public string ScoutConfidence { get; set; } = "Medium";
        public string ScoutSummary { get; set; } = "";
        public string ScoutReport { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }

    public sealed class DepthChartPosition
    {
        public string Position { get; set; } = "";
        public int RequiredStarters { get; set; }
        public List<string> PlayerIds { get; set; } = new();
    }

    public sealed class TeamDepthChart
    {
        public string TeamId { get; set; } = "";
        public List<DepthChartPosition> Positions { get; set; } = new();
    }

    public sealed class PositionCount
    {
        public string Position { get; set; } = "";
        public int Count { get; set; }
    }

    public sealed class RosterValidationResult
    {
        public bool IsValid { get; set; }
        public int RosterSize { get; set; }
        public int RosterLimit { get; set; }
        public int RequiredCuts { get; set; }
        public int InjuredCount { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<PositionCount> PositionCounts { get; set; } = new();
    }

    public sealed class TeamReadiness
    {
        public int EffectiveStrength { get; set; }
        public int MissingStarters { get; set; }
        public int UnavailablePlayers { get; set; }
        public int InjuryPenalty { get; set; }
        public Dictionary<string, List<Player>> AvailableByPosition { get; set; } = new();
    }

    public sealed class DepthChartMaintenanceNotice
    {
        public string TeamId { get; set; } = "";
        public string Type { get; set; } = "depth_chart_notice";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Severity { get; set; } = "info";
        public string PrimaryAction { get; set; } = "";
    }

    public sealed class PlayerRecoveryNotice
    {
        public string TeamId { get; set; } = "";
        public string PlayerId { get; set; } = "";
        public string PlayerName { get; set; } = "";
        public string Position { get; set; } = "";
    }

    public sealed class PositionRequirement
    {
        public PositionRequirement(string position, int count, int starters)
        {
            Position = position;
            Count = count;
            Starters = starters;
        }

        public string Position { get; }
        public int Count { get; }
        public int Starters { get; }
    }

    public sealed class SeasonGameSnapshot
    {
        public string Id { get; set; } = "";
        public int Week { get; set; }
        public string GameType { get; set; } = "";
        public string HomeTeamId { get; set; } = "";
        public string AwayTeamId { get; set; } = "";
        public int HomeScore { get; set; }
        public int AwayScore { get; set; }
        public bool Completed { get; set; }
        public int HomeSeed { get; set; }
        public int AwaySeed { get; set; }
    }

    public sealed class TeamStanding
    {
        public string TeamId { get; set; } = "";
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Ties { get; set; }
        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }
        public int PointDifferential => PointsFor - PointsAgainst;
        public double WinPct => Wins + Losses + Ties == 0 ? 0 : (Wins + (Ties * 0.5)) / (Wins + Losses + Ties);
    }

    public sealed class PlayoffSeed
    {
        public int Seed { get; set; }
        public string TeamId { get; set; } = "";
    }

    public sealed class TeamStandingContext
    {
        public LeagueTeamDefinition Team { get; set; } = new();
        public TeamStanding Standing { get; set; } = new();
        public int DivisionWins { get; set; }
        public int DivisionLosses { get; set; }
        public int DivisionTies { get; set; }
        public int ConferenceWins { get; set; }
        public int ConferenceLosses { get; set; }
        public int ConferenceTies { get; set; }
        public double DivisionWinPct => DivisionWins + DivisionLosses + DivisionTies == 0 ? 0 : (DivisionWins + (DivisionTies * 0.5)) / (DivisionWins + DivisionLosses + DivisionTies);
        public double ConferenceWinPct => ConferenceWins + ConferenceLosses + ConferenceTies == 0 ? 0 : (ConferenceWins + (ConferenceTies * 0.5)) / (ConferenceWins + ConferenceLosses + ConferenceTies);
    }

    public sealed class PlayoffRaceStatus
    {
        public string TeamId { get; set; } = "";
        public bool ClinchedDivision { get; set; }
        public bool ClinchedPlayoff { get; set; }
        public bool Eliminated { get; set; }
        public bool InHunt { get; set; }
        public int RemainingGames { get; set; }
        public int MaxWins { get; set; }
        public string StatusLabel { get; set; } = "";
        public string SpotLabel { get; set; } = "";
    }

    public sealed class TeamSeasonSummary
    {
        public string TeamId { get; set; } = "";
        public string Conference { get; set; } = "";
        public string Division { get; set; } = "";
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Ties { get; set; }
        public int PointsFor { get; set; }
        public int PointsAgainst { get; set; }
        public int DivisionRank { get; set; }
        public int ConferenceRank { get; set; }
        public int PlayoffSeed { get; set; }
    }

    public sealed class SeasonArchiveSummary
    {
        public int SeasonYear { get; set; }
        public string ChampionTeamId { get; set; } = "";
        public string RunnerUpTeamId { get; set; } = "";
        public string ChampionDisplayName { get; set; } = "";
        public string RunnerUpDisplayName { get; set; } = "";
        public int ChampionshipWeek { get; set; }
        public int ChampionScore { get; set; }
        public int RunnerUpScore { get; set; }
        public List<PlayoffSeed> PlayoffSeeds { get; set; } = new();
        public List<TeamSeasonSummary> Teams { get; set; } = new();
    }

    public sealed class LeagueCalendarMilestone
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public DateTime Date { get; set; }
        public string Phase { get; set; } = "";
    }

    public sealed class LeagueTeamDefinition
    {
        public string Id { get; set; } = "";
        public string City { get; set; } = "";
        public string Name { get; set; } = "";
        public string Abbreviation { get; set; } = "";
        public string Conference { get; set; } = "";
        public string Division { get; set; } = "";
        public int Strength { get; set; }
    }

    internal sealed class ScheduleRequest
    {
        public string HomeTeamId { get; set; } = "";
        public string AwayTeamId { get; set; } = "";
        public string PairKey { get; set; } = "";
        public int Priority { get; set; }
        public int MinimumRematchGap { get; set; }
        public List<int> PreferredWeeks { get; set; } = new();
    }

    internal sealed class DivisionScheduleRound
    {
        public string Id { get; set; } = "";
        public string LeftDivisionKey { get; set; } = "";
        public string RightDivisionKey { get; set; } = "";
        public List<(string homeTeamId, string awayTeamId)> Games { get; set; } = new();
        public List<int> PreferredWeeks { get; set; } = new();
    }

    public static class LeagueSliceFactory
    {
        public const int ActiveRosterLimit = 53;
        public const int PracticeSquadLimit = 8;
        public const int LeagueTeamCount = 32;
        public const int RegularSeasonWeeks = 18;
        public const int WildCardWeek = 19;
        public const int DivisionalWeek = 20;
        public const int ConferenceChampionshipWeek = 21;
        public const int ChampionshipWeek = 22;
        public const int MaxSeasonWeek = ChampionshipWeek;
        public const string RegularSeasonGameType = "regular_season";
        public const string PlayoffWildCardGameType = "playoff_wild_card";
        public const string PlayoffDivisionalGameType = "playoff_divisional";
        public const string PlayoffConferenceChampionshipGameType = "playoff_conference_championship";
        public const string ChampionshipGameType = "championship";

        private static readonly PositionRequirement[] ActiveRosterTemplate =
        {
            new("QB", 3, 1),
            new("RB", 5, 1),
            new("WR", 7, 3),
            new("TE", 4, 1),
            new("LT", 2, 1),
            new("LG", 2, 1),
            new("C", 2, 1),
            new("RG", 2, 1),
            new("RT", 2, 1),
            new("EDGE", 4, 2),
            new("DT", 4, 2),
            new("LB", 5, 2),
            new("CB", 5, 2),
            new("S", 4, 2),
            new("K", 1, 1),
            new("P", 1, 1),
        };

        private static readonly string[] FirstNames =
        {
            "Avery", "Blake", "Cameron", "Dakota", "Elliot", "Finley", "Graham", "Hayden", "Jordan", "Kai",
            "Logan", "Morgan", "Nico", "Parker", "Quinn", "Reese", "Sawyer", "Taylor", "Wesley", "Zion"
        };

        private static readonly string[] LastNames =
        {
            "Adams", "Bennett", "Coleman", "Daniels", "Ellis", "Foster", "Garner", "Hayes", "Iverson", "Jackson",
            "Keller", "Lawson", "Maddox", "Norris", "Owens", "Pierce", "Quincy", "Reeves", "Sutton", "Turner"
        };

        private static readonly LeagueTeamDefinition[] DefaultLeagueTeams =
        {
            new() { Id = "capital", City = "Capital", Name = "Sentinels", Abbreviation = "CAP", Conference = "Union", Division = "East", Strength = 71 },
            new() { Id = "harbor", City = "Harbor", Name = "Admirals", Abbreviation = "HBR", Conference = "Union", Division = "East", Strength = 73 },
            new() { Id = "atlantic", City = "Atlantic", Name = "Royals", Abbreviation = "ATL", Conference = "Union", Division = "East", Strength = 69 },
            new() { Id = "gotham", City = "Gotham", Name = "Guardians", Abbreviation = "GOT", Conference = "Union", Division = "East", Strength = 72 },
            new() { Id = "lake", City = "Lake", Name = "Hawks", Abbreviation = "LAK", Conference = "Union", Division = "North", Strength = 72 },
            new() { Id = "metro", City = "Metro", Name = "Comets", Abbreviation = "MET", Conference = "Union", Division = "North", Strength = 69 },
            new() { Id = "ridge", City = "Ridge", Name = "Wolves", Abbreviation = "RDG", Conference = "Union", Division = "North", Strength = 70 },
            new() { Id = "iron", City = "Iron", Name = "Miners", Abbreviation = "IRN", Conference = "Union", Division = "North", Strength = 68 },
            new() { Id = "mesa", City = "Mesa", Name = "Outlaws", Abbreviation = "MES", Conference = "Union", Division = "South", Strength = 68 },
            new() { Id = "bayou", City = "Bayou", Name = "Gators", Abbreviation = "BYU", Conference = "Union", Division = "South", Strength = 73 },
            new() { Id = "prairie", City = "Prairie", Name = "Bison", Abbreviation = "PRA", Conference = "Union", Division = "South", Strength = 66 },
            new() { Id = "delta", City = "Delta", Name = "Kings", Abbreviation = "DLT", Conference = "Union", Division = "South", Strength = 70 },
            new() { Id = "port", City = "Port", Name = "Tritons", Abbreviation = "POR", Conference = "Union", Division = "West", Strength = 75 },
            new() { Id = "bay", City = "Bay", Name = "Corsairs", Abbreviation = "BAY", Conference = "Union", Division = "West", Strength = 74 },
            new() { Id = "canyon", City = "Canyon", Name = "Blaze", Abbreviation = "CYN", Conference = "Union", Division = "West", Strength = 67 },
            new() { Id = "summit", City = "Summit", Name = "Peaks", Abbreviation = "SUM", Conference = "Union", Division = "West", Strength = 71 },
            new() { Id = "liberty", City = "Liberty", Name = "Bells", Abbreviation = "LIB", Conference = "Continental", Division = "East", Strength = 72 },
            new() { Id = "crown", City = "Crown", Name = "Monarchs", Abbreviation = "CRN", Conference = "Continental", Division = "East", Strength = 70 },
            new() { Id = "coast", City = "Coast", Name = "Breakers", Abbreviation = "CST", Conference = "Continental", Division = "East", Strength = 68 },
            new() { Id = "granite", City = "Granite", Name = "Pilots", Abbreviation = "GRA", Conference = "Continental", Division = "East", Strength = 74 },
            new() { Id = "forge", City = "Forge", Name = "Hammers", Abbreviation = "FRG", Conference = "Continental", Division = "North", Strength = 71 },
            new() { Id = "timber", City = "Timber", Name = "Stags", Abbreviation = "TIM", Conference = "Continental", Division = "North", Strength = 69 },
            new() { Id = "aurora", City = "Aurora", Name = "Foxes", Abbreviation = "AUR", Conference = "Continental", Division = "North", Strength = 67 },
            new() { Id = "river", City = "River", Name = "Rivermen", Abbreviation = "RIV", Conference = "Continental", Division = "North", Strength = 73 },
            new() { Id = "sol", City = "Sol", Name = "Firebirds", Abbreviation = "SOL", Conference = "Continental", Division = "South", Strength = 75 },
            new() { Id = "magnolia", City = "Magnolia", Name = "Storm", Abbreviation = "MAG", Conference = "Continental", Division = "South", Strength = 70 },
            new() { Id = "orbit", City = "Orbit", Name = "Rockets", Abbreviation = "ORB", Conference = "Continental", Division = "South", Strength = 68 },
            new() { Id = "copper", City = "Copper", Name = "Coyotes", Abbreviation = "COP", Conference = "Continental", Division = "South", Strength = 66 },
            new() { Id = "desert", City = "Desert", Name = "Vipers", Abbreviation = "DES", Conference = "Continental", Division = "West", Strength = 72 },
            new() { Id = "cascade", City = "Cascade", Name = "Owls", Abbreviation = "CAS", Conference = "Continental", Division = "West", Strength = 69 },
            new() { Id = "pacific", City = "Pacific", Name = "Phantoms", Abbreviation = "PAC", Conference = "Continental", Division = "West", Strength = 74 },
            new() { Id = "sierra", City = "Sierra", Name = "Gold", Abbreviation = "SIE", Conference = "Continental", Division = "West", Strength = 67 }
        };

        public static IReadOnlyList<PositionRequirement> DepthChartRequirements => ActiveRosterTemplate;

        public static List<LeagueTeamDefinition> CreateDefaultLeagueTeams()
            => DefaultLeagueTeams
                .Select(team => new LeagueTeamDefinition
                {
                    Id = team.Id,
                    City = team.City,
                    Name = team.Name,
                    Abbreviation = team.Abbreviation,
                    Conference = team.Conference,
                    Division = team.Division,
                    Strength = team.Strength
                })
                .ToList();

        public static List<SeasonGameSnapshot> CreatePrototypeRegularSeasonSchedule(IEnumerable<LeagueTeamDefinition> teams, int seasonYear = 2026)
        {
            var teamList = (teams ?? Enumerable.Empty<LeagueTeamDefinition>())
                .OrderBy(team => team.Conference, StringComparer.OrdinalIgnoreCase)
                .ThenBy(team => DivisionOrder(team.Division))
                .ThenBy(team => team.Id, StringComparer.Ordinal)
                .ToList();
            if (teamList.Count != LeagueTeamCount)
                throw new ArgumentException($"Schedule generation requires exactly {LeagueTeamCount} teams.", nameof(teams));

            var divisions = teamList
                .GroupBy(team => $"{team.Conference}:{team.Division}", StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            if (divisions.Count != 8 || divisions.Any(group => group.Value.Count != 4))
                throw new ArgumentException("Schedule generation requires eight divisions of four teams each.", nameof(teams));
            var games = new List<SeasonGameSnapshot>();
            var byeWeeks = BuildDivisionByeWeeks(divisions.Keys, seasonYear);

            foreach (var division in divisions.Values)
            {
                var ordered = division.OrderBy(team => team.Id, StringComparer.Ordinal).ToList();
                var rounds = RoundRobinPairings(ordered).ToList();
                for (var round = 0; round < rounds.Count; round++)
                {
                    foreach (var pairing in rounds[round])
                    {
                        games.Add(new SeasonGameSnapshot
                        {
                            Id = $"w{round + 1}-div-{pairing.homeTeamId}-{pairing.awayTeamId}",
                            Week = round + 1,
                            GameType = RegularSeasonGameType,
                            HomeTeamId = pairing.homeTeamId,
                            AwayTeamId = pairing.awayTeamId
                        });
                        games.Add(new SeasonGameSnapshot
                        {
                            Id = $"w{round + 15}-div-{pairing.awayTeamId}-{pairing.homeTeamId}",
                            Week = round + 15,
                            GameType = RegularSeasonGameType,
                            HomeTeamId = pairing.awayTeamId,
                            AwayTeamId = pairing.homeTeamId
                        });
                    }
                }
            }

            var roundsToSchedule = BuildMidseasonRounds(divisions, seasonYear);
            if (!TryAssignDivisionRounds(roundsToSchedule, byeWeeks, out var roundWeeks))
                throw new InvalidOperationException("Unable to assign staggered bye weeks to the regular-season schedule.");

            foreach (var round in roundsToSchedule)
            {
                var week = roundWeeks[round.Id];
                foreach (var game in round.Games)
                {
                    games.Add(new SeasonGameSnapshot
                    {
                        Id = $"w{week}-{round.Id}-{game.homeTeamId}-{game.awayTeamId}",
                        Week = week,
                        GameType = RegularSeasonGameType,
                        HomeTeamId = game.homeTeamId,
                        AwayTeamId = game.awayTeamId
                    });
                }
            }

            var seventeenthOffset = (Math.Abs(seasonYear) % 4 + 2) % 4;
            foreach (var division in DivisionNames())
            {
                var unionDivision = divisions[$"Union:{division}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                var continentalDivisionName = DivisionNames()[(DivisionOrder(division) + seventeenthOffset) % 4];
                var continentalDivision = divisions[$"Continental:{continentalDivisionName}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                for (var rank = 0; rank < unionDivision.Count; rank++)
                {
                    var unionHosts = (Math.Abs(seasonYear) + rank + DivisionOrder(division)) % 2 == 0;
                    games.Add(new SeasonGameSnapshot
                    {
                        Id = $"w18-17th-{unionDivision[rank].Id}-{continentalDivision[rank].Id}",
                        Week = 18,
                        GameType = RegularSeasonGameType,
                        HomeTeamId = unionHosts ? unionDivision[rank].Id : continentalDivision[rank].Id,
                        AwayTeamId = unionHosts ? continentalDivision[rank].Id : unionDivision[rank].Id
                    });
                }
            }

            var schedule = games
                .OrderBy(game => game.Week)
                .ThenBy(game => game.HomeTeamId, StringComparer.Ordinal)
                .ThenBy(game => game.AwayTeamId, StringComparer.Ordinal)
                .Select((game, index) => new SeasonGameSnapshot
                {
                    Id = $"w{game.Week}-g{index + 1:D3}",
                    Week = game.Week,
                    GameType = game.GameType,
                    HomeTeamId = game.HomeTeamId,
                    AwayTeamId = game.AwayTeamId
                })
                .ToList();
            if (!ValidateScheduleDistribution(schedule, teamList.Select(team => team.Id)))
                throw new InvalidOperationException("Constructed regular-season schedule failed validation.");

            return schedule;
        }

        public static bool IsRegularSeasonGame(string gameType)
            => string.IsNullOrWhiteSpace(gameType)
                || string.Equals(gameType, RegularSeasonGameType, StringComparison.OrdinalIgnoreCase);

        public static string DescribeWeek(int week)
            => week switch
            {
                WildCardWeek => "Wild Card",
                DivisionalWeek => "Divisional Round",
                ConferenceChampionshipWeek => "Conference Championship",
                ChampionshipWeek => "Championship",
                _ => $"Week {week}"
            };

        public static string DescribePhase(int currentWeek, IEnumerable<SeasonGameSnapshot> games)
        {
            var snapshots = games?.ToList() ?? new List<SeasonGameSnapshot>();
            if (currentWeek > ChampionshipWeek && snapshots.Any(game =>
                    string.Equals(game.GameType, ChampionshipGameType, StringComparison.OrdinalIgnoreCase) && game.Completed))
                return "Season Complete";
            if (currentWeek >= WildCardWeek)
                return "Playoffs";
            return "Regular Season";
        }

        public static string DescribeCalendarPhase(int seasonYear, DateTime currentDate, IEnumerable<SeasonGameSnapshot> games)
        {
            var normalizedDate = currentDate.Date;
            if (normalizedDate <= GetGameDayDate(seasonYear, RegularSeasonWeeks).Date)
                return "Regular Season";
            if (normalizedDate <= GetGameDayDate(seasonYear, ChampionshipWeek).Date)
                return "Playoffs";
            if (normalizedDate < GetNewLeagueYearDate(seasonYear).Date)
                return "Offseason";
            if (normalizedDate < GetFreeAgencyOpenDate(seasonYear).Date)
                return "League Year Reset";
            if (normalizedDate < GetDraftPrepDate(seasonYear).Date)
                return "Free Agency";
            if (normalizedDate < GetDraftWeekStartDate(seasonYear).AddDays(7).Date)
                return "Draft Week";
            if (normalizedDate < GetLeagueYearStartDate(seasonYear + 1).Date)
                return "Offseason";
            return "Regular Season";
        }

        public static DateTime GetLeagueYearStartDate(int seasonYear)
        {
            var start = new DateTime(seasonYear, 9, 1);
            while (start.DayOfWeek != DayOfWeek.Tuesday)
                start = start.AddDays(1);
            return start.Date;
        }

        public static DateTime GetWeekStartDate(int seasonYear, int week)
            => GetLeagueYearStartDate(seasonYear).AddDays(Math.Max(0, week - 1) * 7);

        public static DateTime GetGameDayDate(int seasonYear, int week)
            => GetWeekStartDate(seasonYear, week).AddDays(5);

        public static DateTime GetOffseasonOpenDate(int seasonYear)
            => GetGameDayDate(seasonYear, ChampionshipWeek).AddDays(1);

        public static DateTime GetRetirementDecisionsDate(int seasonYear)
            => NextWeekday(GetOffseasonOpenDate(seasonYear), DayOfWeek.Tuesday);

        public static DateTime GetNewLeagueYearDate(int seasonYear)
            => GetNthWeekdayOfMonth(seasonYear + 1, 3, DayOfWeek.Wednesday, 2);

        public static DateTime GetFreeAgencyOpenDate(int seasonYear)
            => GetNewLeagueYearDate(seasonYear).AddDays(1);

        public static DateTime GetDraftPrepDate(int seasonYear)
            => GetNthWeekdayOfMonth(seasonYear + 1, 4, DayOfWeek.Monday, 1);

        public static DateTime GetDraftWeekStartDate(int seasonYear)
            => GetNthWeekdayOfMonth(seasonYear + 1, 4, DayOfWeek.Monday, 4);

        public static int GetFootballWeekForDate(int seasonYear, DateTime currentDate, int maxWeek)
        {
            var start = GetLeagueYearStartDate(seasonYear);
            var normalizedDate = currentDate.Date;
            if (normalizedDate <= start)
                return 1;

            var elapsedDays = (normalizedDate - start).Days;
            var computedWeek = 1 + (elapsedDays / 7);
            return Math.Clamp(computedWeek, 1, Math.Max(1, maxWeek + 1));
        }

        public static IReadOnlyList<LeagueCalendarMilestone> GetLeagueCalendarMilestones(int seasonYear)
            => new List<LeagueCalendarMilestone>
            {
                new()
                {
                    Id = "regular_season_week_1",
                    Label = "Regular Season Opens",
                    Date = GetWeekStartDate(seasonYear, 1),
                    Phase = "Regular Season"
                },
                new()
                {
                    Id = "regular_season_week_5",
                    Label = "Regular Season Week 5",
                    Date = GetWeekStartDate(seasonYear, 5),
                    Phase = "Regular Season"
                },
                new()
                {
                    Id = "regular_season_week_9",
                    Label = "Regular Season Week 9",
                    Date = GetWeekStartDate(seasonYear, 9),
                    Phase = "Regular Season"
                },
                new()
                {
                    Id = "regular_season_week_13",
                    Label = "Regular Season Week 13",
                    Date = GetWeekStartDate(seasonYear, 13),
                    Phase = "Regular Season"
                },
                new()
                {
                    Id = "playoffs_start",
                    Label = "Playoffs Begin",
                    Date = GetWeekStartDate(seasonYear, WildCardWeek),
                    Phase = "Playoffs"
                },
                new()
                {
                    Id = "championship_game",
                    Label = "Championship Game",
                    Date = GetGameDayDate(seasonYear, ChampionshipWeek),
                    Phase = "Playoffs"
                },
                new()
                {
                    Id = "offseason_opens",
                    Label = "Offseason Opens",
                    Date = GetOffseasonOpenDate(seasonYear),
                    Phase = "Offseason"
                },
                new()
                {
                    Id = "retirement_decisions",
                    Label = "Retirement Decisions Begin",
                    Date = GetRetirementDecisionsDate(seasonYear),
                    Phase = "Offseason"
                },
                new()
                {
                    Id = "new_league_year",
                    Label = "League Year Resets",
                    Date = GetNewLeagueYearDate(seasonYear),
                    Phase = "Offseason"
                },
                new()
                {
                    Id = "free_agency_opens",
                    Label = "Free Agency Opens",
                    Date = GetFreeAgencyOpenDate(seasonYear),
                    Phase = "Free Agency"
                },
                new()
                {
                    Id = "draft_prep_opens",
                    Label = "Draft Prep Opens",
                    Date = GetDraftPrepDate(seasonYear),
                    Phase = "Draft"
                },
                new()
                {
                    Id = "draft_week",
                    Label = "Draft Week",
                    Date = GetDraftWeekStartDate(seasonYear),
                    Phase = "Draft"
                },
                new()
                {
                    Id = "next_season_opens",
                    Label = "Next Season Opens",
                    Date = GetLeagueYearStartDate(seasonYear + 1),
                    Phase = "Regular Season"
                }
            };

        public static IReadOnlyList<LeagueCalendarMilestone> GetLeagueCalendarMilestonesForDate(int seasonYear, DateTime currentDate)
            => GetLeagueCalendarMilestones(seasonYear)
                .Where(milestone => milestone.Date.Date == currentDate.Date)
                .OrderBy(milestone => milestone.Date)
                .ToList();

        public static LeagueCalendarMilestone? GetNextLeagueCalendarMilestone(int seasonYear, DateTime currentDate)
            => GetLeagueCalendarMilestones(seasonYear)
                .Where(milestone => milestone.Date.Date > currentDate.Date)
                .OrderBy(milestone => milestone.Date)
                .FirstOrDefault();

        private static DateTime NextWeekday(DateTime startDate, DayOfWeek dayOfWeek)
        {
            var current = startDate.Date;
            while (current.DayOfWeek != dayOfWeek)
                current = current.AddDays(1);
            return current;
        }

        private static DateTime GetNthWeekdayOfMonth(int year, int month, DayOfWeek dayOfWeek, int occurrence)
        {
            var current = new DateTime(year, month, 1);
            while (current.DayOfWeek != dayOfWeek)
                current = current.AddDays(1);
            return current.AddDays((Math.Max(1, occurrence) - 1) * 7);
        }

        public static SeasonArchiveSummary? BuildSeasonArchive(
            int seasonYear,
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games)
        {
            var teamList = teams?.ToList() ?? new List<LeagueTeamDefinition>();
            var standingList = standings?.ToList() ?? new List<TeamStanding>();
            var snapshots = games?.ToList() ?? new List<SeasonGameSnapshot>();
            var championship = snapshots
                .Where(game => game.Completed && string.Equals(game.GameType, ChampionshipGameType, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(game => game.Week)
                .FirstOrDefault();
            if (championship == null)
                return null;

            var championTeamId = ResolveWinnerTeamId(championship);
            var runnerUpTeamId = ResolveLoserTeamId(championship);
            if (string.IsNullOrWhiteSpace(championTeamId) || string.IsNullOrWhiteSpace(runnerUpTeamId))
                return null;

            var teamLookup = teamList.ToDictionary(team => team.Id, StringComparer.Ordinal);
            var divisionRanks = BuildDivisionRanks(teamList, standingList, snapshots);
            var conferenceRanks = BuildConferenceRanks(teamList, standingList, snapshots);
            var seedLookup = BuildConferencePlayoffSeedLookup(teamList, standingList, snapshots);

            return new SeasonArchiveSummary
            {
                SeasonYear = seasonYear,
                ChampionTeamId = championTeamId,
                RunnerUpTeamId = runnerUpTeamId,
                ChampionDisplayName = FormatTeamDisplayName(teamLookup, championTeamId),
                RunnerUpDisplayName = FormatTeamDisplayName(teamLookup, runnerUpTeamId),
                ChampionshipWeek = championship.Week,
                ChampionScore = ScoreForTeam(championship, championTeamId),
                RunnerUpScore = ScoreForTeam(championship, runnerUpTeamId),
                PlayoffSeeds = seedLookup
                    .OrderBy(entry => entry.Value)
                    .Select(entry => new PlayoffSeed { TeamId = entry.Key, Seed = entry.Value })
                    .ToList(),
                Teams = teamList
                    .Select(team =>
                    {
                        var standing = standingList.FirstOrDefault(item => string.Equals(item.TeamId, team.Id, StringComparison.Ordinal))
                            ?? new TeamStanding { TeamId = team.Id };
                        return new TeamSeasonSummary
                        {
                            TeamId = team.Id,
                            Conference = team.Conference,
                            Division = team.Division,
                            Wins = standing.Wins,
                            Losses = standing.Losses,
                            Ties = standing.Ties,
                            PointsFor = standing.PointsFor,
                            PointsAgainst = standing.PointsAgainst,
                            DivisionRank = divisionRanks.TryGetValue(team.Id, out var divisionRank) ? divisionRank : 0,
                            ConferenceRank = conferenceRanks.TryGetValue(team.Id, out var conferenceRank) ? conferenceRank : 0,
                            PlayoffSeed = seedLookup.TryGetValue(team.Id, out var playoffSeed) ? playoffSeed : 0
                        };
                    })
                    .OrderBy(summary => summary.Conference, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(summary => DivisionOrder(summary.Division))
                    .ThenBy(summary => summary.DivisionRank)
                    .ThenBy(summary => summary.ConferenceRank == 0 ? int.MaxValue : summary.ConferenceRank)
                    .ThenBy(summary => summary.TeamId, StringComparer.Ordinal)
                    .ToList()
            };
        }

        private static string ResolveWinnerTeamId(SeasonGameSnapshot game)
        {
            if (game == null || !game.Completed || game.HomeScore == game.AwayScore)
                return "";
            return game.HomeScore > game.AwayScore ? game.HomeTeamId : game.AwayTeamId;
        }

        private static string ResolveLoserTeamId(SeasonGameSnapshot game)
        {
            if (game == null || !game.Completed || game.HomeScore == game.AwayScore)
                return "";
            return game.HomeScore > game.AwayScore ? game.AwayTeamId : game.HomeTeamId;
        }

        private static int ScoreForTeam(SeasonGameSnapshot game, string teamId)
        {
            if (game == null || string.IsNullOrWhiteSpace(teamId))
                return 0;
            if (string.Equals(game.HomeTeamId, teamId, StringComparison.Ordinal))
                return game.HomeScore;
            if (string.Equals(game.AwayTeamId, teamId, StringComparison.Ordinal))
                return game.AwayScore;
            return 0;
        }

        private static string FormatTeamDisplayName(IReadOnlyDictionary<string, LeagueTeamDefinition> teamsById, string teamId)
        {
            if (string.IsNullOrWhiteSpace(teamId) || teamsById == null || !teamsById.TryGetValue(teamId, out var team))
                return teamId ?? "";
            return $"{team.City} {team.Name}".Trim();
        }

        private static Dictionary<string, int> BuildDivisionByeWeeks(IEnumerable<string> divisionKeys, int seasonYear)
        {
            var divisionSet = new HashSet<string>(divisionKeys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (divisionSet.Count != 8)
                throw new ArgumentException("Bye week assignment requires eight division keys.", nameof(divisionKeys));

            var conferencePairs = GetConferenceRotationPairs(seasonYear);
            var structuredByeWeeks = GetStructuredByeWeeks(seasonYear);
            var byeWeeks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var pairSlots = (Math.Abs(seasonYear) % 2) == 0
                ? new[]
                {
                    ("Continental", conferencePairs[0]),
                    ("Union", conferencePairs[0]),
                    ("Continental", conferencePairs[1]),
                    ("Union", conferencePairs[1])
                }
                : new[]
                {
                    ("Union", conferencePairs[0]),
                    ("Continental", conferencePairs[0]),
                    ("Union", conferencePairs[1]),
                    ("Continental", conferencePairs[1])
                };

            for (var index = 0; index < pairSlots.Length; index++)
            {
                var (conference, pair) = pairSlots[index];
                var leftDivisionKey = $"{conference}:{pair.Item1}";
                var rightDivisionKey = $"{conference}:{pair.Item2}";
                if (!divisionSet.Contains(leftDivisionKey) || !divisionSet.Contains(rightDivisionKey))
                    throw new ArgumentException("Bye week assignment requires the full league division set.", nameof(divisionKeys));

                byeWeeks[leftDivisionKey] = structuredByeWeeks[index];
                byeWeeks[rightDivisionKey] = structuredByeWeeks[index];
            }

            return byeWeeks;
        }

        private static int[] GetStructuredByeWeeks(int seasonYear)
            => (Math.Abs(seasonYear) % 3) switch
            {
                0 => new[] { 5, 7, 9, 11 },
                1 => new[] { 6, 8, 10, 12 },
                _ => new[] { 5, 6, 9, 10 }
            };

        private static (string, string)[] GetConferenceRotationPairs(int seasonYear)
            => (Math.Abs(seasonYear) % 3) switch
            {
                0 => new[] { ("East", "North"), ("South", "West") },
                1 => new[] { ("East", "South"), ("North", "West") },
                _ => new[] { ("East", "West"), ("North", "South") }
            };

        private static List<DivisionScheduleRound> BuildMidseasonRounds(
            IReadOnlyDictionary<string, List<LeagueTeamDefinition>> divisions,
            int seasonYear)
        {
            var rounds = new List<DivisionScheduleRound>();
            var conferencePairs = GetConferenceRotationPairs(seasonYear);
            var conferenceByeWeeks = BuildDivisionByeWeeks(divisions.Keys, seasonYear);
            var byeWeekSet = conferenceByeWeeks.Values.Distinct().OrderBy(week => week).ToList();
            var conferenceAnchorWeeks = new[] { 4 }.Concat(byeWeekSet).OrderBy(week => week).ToList();
            var crossConferenceWeeks = Enumerable.Range(5, 8)
                .Where(week => !byeWeekSet.Contains(week))
                .OrderBy(week => week)
                .ToList();
            if (conferenceAnchorWeeks.Count != 5 || crossConferenceWeeks.Count != 4)
                throw new InvalidOperationException("Structured bye template did not produce valid midseason week slots.");

            foreach (var conference in new[] { "Union", "Continental" })
            {
                foreach (var pair in conferencePairs)
                {
                    var leftDivisionKey = $"{conference}:{pair.Item1}";
                    var rightDivisionKey = $"{conference}:{pair.Item2}";
                    var left = divisions[leftDivisionKey].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                    var right = divisions[rightDivisionKey].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                    var leftHosts = (Math.Abs(seasonYear) + DivisionOrder(pair.Item1)) % 2 == 0;
                    var roundWeeks = conferenceAnchorWeeks
                        .Where(week => week != conferenceByeWeeks[leftDivisionKey])
                        .ToList();
                    for (var round = 0; round < 4; round++)
                    {
                        var games = new List<(string homeTeamId, string awayTeamId)>();
                        for (var index = 0; index < left.Count; index++)
                        {
                            var opponent = right[(index + round) % right.Count];
                            games.Add(leftHosts
                                ? (left[index].Id, opponent.Id)
                                : (opponent.Id, left[index].Id));
                        }

                        rounds.Add(new DivisionScheduleRound
                        {
                            Id = $"conf-{conference.ToLowerInvariant()}-{pair.Item1.ToLowerInvariant()}-{pair.Item2.ToLowerInvariant()}-{round + 1}",
                            LeftDivisionKey = leftDivisionKey,
                            RightDivisionKey = rightDivisionKey,
                            Games = games,
                            PreferredWeeks = new List<int> { roundWeeks[round] }
                        });
                    }
                }
            }

            var interConferenceOffset = Math.Abs(seasonYear) % 4;
            foreach (var division in DivisionNames())
            {
                var unionDivisionKey = $"Union:{division}";
                var continentalDivisionName = DivisionNames()[(DivisionOrder(division) + interConferenceOffset) % 4];
                var continentalDivisionKey = $"Continental:{continentalDivisionName}";
                var union = divisions[unionDivisionKey].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                var continental = divisions[continentalDivisionKey].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                var unionHosts = (Math.Abs(seasonYear) + DivisionOrder(division)) % 2 == 0;
                for (var round = 0; round < 4; round++)
                {
                    var games = new List<(string homeTeamId, string awayTeamId)>();
                    for (var index = 0; index < union.Count; index++)
                    {
                        var opponent = continental[(index + round) % continental.Count];
                        games.Add(unionHosts
                            ? (union[index].Id, opponent.Id)
                            : (opponent.Id, union[index].Id));
                    }

                    rounds.Add(new DivisionScheduleRound
                    {
                        Id = $"cross-{division.ToLowerInvariant()}-{continentalDivisionName.ToLowerInvariant()}-{round + 1}",
                        LeftDivisionKey = unionDivisionKey,
                        RightDivisionKey = continentalDivisionKey,
                        Games = games,
                        PreferredWeeks = new List<int> { crossConferenceWeeks[round] }
                    });
                }
            }

            var pairedSet = conferencePairs
                .SelectMany(pair => new[] { $"{pair.Item1}:{pair.Item2}", $"{pair.Item2}:{pair.Item1}" })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var samePlaceWeekGroups = (Math.Abs(seasonYear) % 3) switch
            {
                0 => new[]
                {
                    new[] { ("East", "South"), ("North", "West") },
                    new[] { ("East", "West"), ("North", "South") }
                },
                1 => new[]
                {
                    new[] { ("East", "North"), ("South", "West") },
                    new[] { ("East", "West"), ("North", "South") }
                },
                _ => new[]
                {
                    new[] { ("East", "North"), ("South", "West") },
                    new[] { ("East", "South"), ("North", "West") }
                }
            };
            foreach (var conference in new[] { "Union", "Continental" })
            {
                var divisionNames = DivisionNames();
                for (var i = 0; i < divisionNames.Length; i++)
                {
                    for (var j = i + 1; j < divisionNames.Length; j++)
                    {
                        var leftDivision = divisionNames[i];
                        var rightDivision = divisionNames[j];
                        if (pairedSet.Contains($"{leftDivision}:{rightDivision}"))
                            continue;

                        var leftDivisionKey = $"{conference}:{leftDivision}";
                        var rightDivisionKey = $"{conference}:{rightDivision}";
                        var left = divisions[leftDivisionKey].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                        var right = divisions[rightDivisionKey].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                        var games = new List<(string homeTeamId, string awayTeamId)>();
                        for (var rank = 0; rank < left.Count; rank++)
                        {
                            var leftHosts = (Math.Abs(seasonYear) + rank + DivisionOrder(leftDivision) + DivisionOrder(rightDivision)) % 2 == 0;
                            games.Add(leftHosts
                                ? (left[rank].Id, right[rank].Id)
                                : (right[rank].Id, left[rank].Id));
                        }

                        rounds.Add(new DivisionScheduleRound
                        {
                            Id = $"place-{conference.ToLowerInvariant()}-{leftDivision.ToLowerInvariant()}-{rightDivision.ToLowerInvariant()}",
                            LeftDivisionKey = leftDivisionKey,
                            RightDivisionKey = rightDivisionKey,
                            Games = games,
                            PreferredWeeks = new List<int>
                            {
                                samePlaceWeekGroups[0].Any(pair => pair.Item1 == leftDivision && pair.Item2 == rightDivision || pair.Item1 == rightDivision && pair.Item2 == leftDivision)
                                    ? 13
                                    : 14
                            }
                        });
                    }
                }
            }

            return rounds;
        }

        private static bool TryAssignDivisionRounds(
            IReadOnlyList<DivisionScheduleRound> rounds,
            IReadOnlyDictionary<string, int> byeWeeks,
            out Dictionary<string, int> assignedWeeks)
        {
            assignedWeeks = rounds.ToDictionary(round => round.Id, round => round.PreferredWeeks.Single(), StringComparer.Ordinal);

            var weekAssignments = new Dictionary<string, HashSet<int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var division in byeWeeks.Keys)
                weekAssignments[division] = new HashSet<int> { byeWeeks[division] };

            foreach (var round in rounds)
            {
                var week = assignedWeeks[round.Id];
                if (weekAssignments[round.LeftDivisionKey].Contains(week) || weekAssignments[round.RightDivisionKey].Contains(week))
                {
                    assignedWeeks = new Dictionary<string, int>(StringComparer.Ordinal);
                    return false;
                }

                if (!weekAssignments[round.LeftDivisionKey].Add(week) || !weekAssignments[round.RightDivisionKey].Add(week))
                {
                    assignedWeeks = new Dictionary<string, int>(StringComparer.Ordinal);
                    return false;
                }
            }

            return true;
        }

        private static IEnumerable<ScheduleRequest> BuildDivisionalRequests(IEnumerable<List<LeagueTeamDefinition>> divisionGroups)
        {
            var earlyWeeks = new[] { 1, 2, 3 };
            var lateWeeks = new[] { 15, 16, 17 };
            foreach (var division in divisionGroups)
            {
                var ordered = division.OrderBy(team => team.Id, StringComparer.Ordinal).ToList();
                var firstCycle = RoundRobinPairings(ordered).ToList();
                for (var round = 0; round < firstCycle.Count; round++)
                {
                    foreach (var pairing in firstCycle[round])
                    {
                        yield return new ScheduleRequest
                        {
                            HomeTeamId = pairing.homeTeamId,
                            AwayTeamId = pairing.awayTeamId,
                            PairKey = PairKey(pairing.homeTeamId, pairing.awayTeamId),
                            Priority = 0,
                            MinimumRematchGap = 8,
                            PreferredWeeks = new List<int> { earlyWeeks[round] }
                        };
                        yield return new ScheduleRequest
                        {
                            HomeTeamId = pairing.awayTeamId,
                            AwayTeamId = pairing.homeTeamId,
                            PairKey = PairKey(pairing.homeTeamId, pairing.awayTeamId),
                            Priority = 0,
                            MinimumRematchGap = 8,
                            PreferredWeeks = new List<int> { lateWeeks[round] }
                        };
                    }
                }
            }
        }

        private static IEnumerable<ScheduleRequest> BuildIntraConferenceDivisionRequests(
            IReadOnlyDictionary<string, List<LeagueTeamDefinition>> divisions,
            int seasonYear)
        {
            var sameConferenceRotation = new[]
            {
                new[] { ("East", "North"), ("South", "West") },
                new[] { ("East", "South"), ("North", "West") },
                new[] { ("East", "West"), ("North", "South") }
            };
            var rotation = sameConferenceRotation[Math.Abs(seasonYear) % sameConferenceRotation.Length];
            foreach (var conference in divisions.Values.Select(group => group[0].Conference).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                foreach (var pair in rotation)
                {
                    var left = divisions[$"{conference}:{pair.Item1}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                    var right = divisions[$"{conference}:{pair.Item2}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                    var homeFromLeft = (Math.Abs(seasonYear) + DivisionOrder(pair.Item1)) % 2 == 0;
                    for (var i = 0; i < left.Count; i++)
                    {
                        for (var j = 0; j < right.Count; j++)
                        {
                            var home = homeFromLeft ? left[i].Id : right[j].Id;
                            var away = homeFromLeft ? right[j].Id : left[i].Id;
                            yield return new ScheduleRequest
                            {
                                HomeTeamId = home,
                                AwayTeamId = away,
                                PairKey = PairKey(home, away),
                                Priority = 1,
                                PreferredWeeks = Enumerable.Range(4, 11).ToList()
                            };
                        }
                    }
                }
            }
        }

        private static IEnumerable<ScheduleRequest> BuildInterConferenceDivisionRequests(
            IReadOnlyDictionary<string, List<LeagueTeamDefinition>> divisions,
            int seasonYear)
        {
            var rotationOffset = Math.Abs(seasonYear) % 4;
            foreach (var division in DivisionNames())
            {
                var unionDivision = divisions[$"Union:{division}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                var continentalDivisionName = DivisionNames()[(DivisionOrder(division) + rotationOffset) % 4];
                var continentalDivision = divisions[$"Continental:{continentalDivisionName}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                var unionHosts = (Math.Abs(seasonYear) + DivisionOrder(division)) % 2 == 0;
                for (var i = 0; i < unionDivision.Count; i++)
                {
                    for (var j = 0; j < continentalDivision.Count; j++)
                    {
                        var home = unionHosts ? unionDivision[i].Id : continentalDivision[j].Id;
                        var away = unionHosts ? continentalDivision[j].Id : unionDivision[i].Id;
                        yield return new ScheduleRequest
                        {
                            HomeTeamId = home,
                            AwayTeamId = away,
                            PairKey = PairKey(home, away),
                            Priority = 2,
                            PreferredWeeks = Enumerable.Range(4, 11).ToList()
                        };
                    }
                }
            }
        }

        private static IEnumerable<ScheduleRequest> BuildSamePlaceConferenceRequests(
            IReadOnlyDictionary<string, List<LeagueTeamDefinition>> divisions,
            int seasonYear)
        {
            var sameConferenceRotation = new[]
            {
                new[] { ("East", "North"), ("South", "West") },
                new[] { ("East", "South"), ("North", "West") },
                new[] { ("East", "West"), ("North", "South") }
            };
            var pairedSet = sameConferenceRotation[Math.Abs(seasonYear) % sameConferenceRotation.Length]
                .SelectMany(pair => new[] { $"{pair.Item1}:{pair.Item2}", $"{pair.Item2}:{pair.Item1}" })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var conference in new[] { "Union", "Continental" })
            {
                var divisionNames = DivisionNames();
                for (var i = 0; i < divisionNames.Length; i++)
                {
                    for (var j = i + 1; j < divisionNames.Length; j++)
                    {
                        var leftDivision = divisionNames[i];
                        var rightDivision = divisionNames[j];
                        if (pairedSet.Contains($"{leftDivision}:{rightDivision}"))
                            continue;

                        var left = divisions[$"{conference}:{leftDivision}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                        var right = divisions[$"{conference}:{rightDivision}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                        for (var rank = 0; rank < left.Count; rank++)
                        {
                            var leftHosts = (Math.Abs(seasonYear) + rank + i + j) % 2 == 0;
                            var home = leftHosts ? left[rank].Id : right[rank].Id;
                            var away = leftHosts ? right[rank].Id : left[rank].Id;
                            yield return new ScheduleRequest
                            {
                                HomeTeamId = home,
                                AwayTeamId = away,
                                PairKey = PairKey(home, away),
                                Priority = 3,
                                PreferredWeeks = new List<int> { 10, 11, 12, 13, 14 }
                            };
                        }
                    }
                }
            }
        }

        private static IEnumerable<ScheduleRequest> BuildSeventeenthGameRequests(
            IReadOnlyDictionary<string, List<LeagueTeamDefinition>> divisions,
            int seasonYear)
        {
            var interConferenceOffset = Math.Abs(seasonYear) % 4;
            var seventeenthOffset = (interConferenceOffset + 2) % 4;
            foreach (var division in DivisionNames())
            {
                var unionDivision = divisions[$"Union:{division}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                var continentalDivisionName = DivisionNames()[(DivisionOrder(division) + seventeenthOffset) % 4];
                var continentalDivision = divisions[$"Continental:{continentalDivisionName}"].OrderBy(team => DivisionPlacementSort(team, seasonYear)).ToList();
                for (var rank = 0; rank < unionDivision.Count; rank++)
                {
                    var unionHosts = (Math.Abs(seasonYear) + rank + DivisionOrder(division)) % 2 == 0;
                    var home = unionHosts ? unionDivision[rank].Id : continentalDivision[rank].Id;
                    var away = unionHosts ? continentalDivision[rank].Id : unionDivision[rank].Id;
                    yield return new ScheduleRequest
                    {
                        HomeTeamId = home,
                        AwayTeamId = away,
                        PairKey = PairKey(home, away),
                        Priority = 4,
                        PreferredWeeks = new List<int> { 18 }
                    };
                }
            }
        }

        private static bool TryBuildSchedule(
            IReadOnlyList<ScheduleRequest> requests,
            int seasonYear,
            out List<SeasonGameSnapshot> schedule,
            out string failureReason)
        {
            var weekGames = Enumerable.Range(1, RegularSeasonWeeks).ToDictionary(week => week, _ => new List<SeasonGameSnapshot>());
            var pairWeeks = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            var fixedRequests = requests.Where(request => request.Priority == 0 || request.Priority == 4).ToList();
            foreach (var request in fixedRequests)
            {
                var week = request.PreferredWeeks[0];
                AddScheduledGame(weekGames, pairWeeks, week, request);
            }

            var remainingRequests = requests
                .Except(fixedRequests)
                .OrderBy(request => request.Priority)
                .ThenBy(request => request.HomeTeamId, StringComparer.Ordinal)
                .ThenBy(request => request.AwayTeamId, StringComparer.Ordinal)
                .ToList();

            for (var attempt = 0; attempt < 32; attempt++)
            {
                var localWeekGames = weekGames.ToDictionary(entry => entry.Key, entry => entry.Value.ToList());
                var localRemaining = remainingRequests.ToList();
                var success = true;
                var orderedRequests = localRemaining
                    .OrderBy(request => request.Priority)
                    .ThenBy(request => StableSeed.Hash32($"{request.HomeTeamId}:{request.AwayTeamId}:{attempt}:{seasonYear}"))
                    .ToList();

                foreach (var request in orderedRequests)
                {
                    var candidateWeeks = request.PreferredWeeks
                        .Concat(Enumerable.Range(4, 11))
                        .Distinct()
                        .OrderBy(week => localWeekGames[week].Count)
                        .ThenBy(week => StableSeed.Hash32($"{request.HomeTeamId}:{request.AwayTeamId}:{week}:{attempt}"))
                        .ToList();
                    var selectedWeek = candidateWeeks.FirstOrDefault(week => CanPlaceInWeek(localWeekGames[week], request));
                    if (selectedWeek == 0)
                    {
                        success = false;
                        break;
                    }

                    AddScheduledGame(localWeekGames, pairWeeks, selectedWeek, request);
                }

                if (!success)
                    continue;

                var games = localWeekGames.Values.SelectMany(list => list).OrderBy(game => game.Week).ThenBy(game => game.Id, StringComparer.Ordinal).ToList();
                if (!ValidateScheduleDistribution(games, requests.SelectMany(request => new[] { request.HomeTeamId, request.AwayTeamId }).Distinct(StringComparer.Ordinal)))
                    continue;

                schedule = games;
                failureReason = string.Empty;
                return true;
            }

            schedule = new List<SeasonGameSnapshot>();
            failureReason = "Greedy week assignment exhausted all attempts.";
            return false;
        }

        private static bool CanPlaceInWeek(IReadOnlyList<SeasonGameSnapshot> weekGames, ScheduleRequest request)
        {
            if (weekGames.Any(game =>
                    string.Equals(game.HomeTeamId, request.HomeTeamId, StringComparison.Ordinal)
                    || string.Equals(game.AwayTeamId, request.HomeTeamId, StringComparison.Ordinal)
                    || string.Equals(game.HomeTeamId, request.AwayTeamId, StringComparison.Ordinal)
                    || string.Equals(game.AwayTeamId, request.AwayTeamId, StringComparison.Ordinal)))
                return false;
            return weekGames.Count < 16;
        }

        private static bool ValidateScheduleDistribution(IEnumerable<SeasonGameSnapshot> games, IEnumerable<string> teamIds)
        {
            var schedule = games.ToList();
            var perTeamGames = schedule
                .SelectMany(game => new[] { (teamId: game.HomeTeamId, week: game.Week), (teamId: game.AwayTeamId, week: game.Week) })
                .GroupBy(entry => entry.teamId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
            foreach (var teamId in teamIds)
            {
                if (!perTeamGames.TryGetValue(teamId, out var entries) || entries.Count != 17)
                {
                    return false;
                }
                if (entries.Select(entry => entry.week).Distinct().Count() != 17)
                    return false;
            }

            return schedule.Count == LeagueTeamCount * 17 / 2;
        }

        private static void AddScheduledGame(
            IDictionary<int, List<SeasonGameSnapshot>> weekGames,
            IDictionary<string, List<int>> pairWeeks,
            int week,
            ScheduleRequest request)
        {
            var slot = weekGames[week].Count + 1;
            weekGames[week].Add(new SeasonGameSnapshot
            {
                Id = $"w{week}-g{slot:D2}",
                Week = week,
                GameType = RegularSeasonGameType,
                HomeTeamId = request.HomeTeamId,
                AwayTeamId = request.AwayTeamId
            });
            if (!pairWeeks.TryGetValue(request.PairKey, out var weeks))
            {
                weeks = new List<int>();
                pairWeeks[request.PairKey] = weeks;
            }
            weeks.Add(week);
        }

        private static IEnumerable<List<(string homeTeamId, string awayTeamId)>> RoundRobinPairings(IReadOnlyList<LeagueTeamDefinition> teams)
        {
            var rotation = teams.Select(team => team.Id).ToList();
            var rounds = new List<List<(string homeTeamId, string awayTeamId)>>();
            for (var round = 0; round < teams.Count - 1; round++)
            {
                var pairings = new List<(string homeTeamId, string awayTeamId)>();
                for (var index = 0; index < rotation.Count / 2; index++)
                {
                    var first = rotation[index];
                    var second = rotation[rotation.Count - 1 - index];
                    pairings.Add(index % 2 == 0 ? (first, second) : (second, first));
                }

                rounds.Add(pairings);
                var locked = rotation[0];
                var moved = rotation[^1];
                rotation.RemoveAt(rotation.Count - 1);
                rotation.Insert(1, moved);
                rotation[0] = locked;
            }

            return rounds;
        }

        private static string[] DivisionNames() => new[] { "East", "North", "South", "West" };
        private static int DivisionOrder(string division)
            => division switch
            {
                "East" => 0,
                "North" => 1,
                "South" => 2,
                "West" => 3,
                _ => 4
            };

        private static int DivisionPlacementSort(LeagueTeamDefinition team, int seasonYear)
            => (200 - team.Strength) * 10 + ((int)(StableSeed.Hash32($"{team.Id}:{seasonYear}") % 10));

        private static string PairKey(string left, string right)
            => string.CompareOrdinal(left, right) <= 0 ? $"{left}:{right}" : $"{right}:{left}";


        public static List<Player> CreatePlayersForTeam(string teamId, ulong worldSeed)
        {
            var players = new List<Player>();
            var activeIndex = 0;
            foreach (var requirement in ActiveRosterTemplate)
            {
                for (var slot = 0; slot < requirement.Count; slot++)
                    players.Add(CreatePlayer(teamId, worldSeed, requirement.Position, activeIndex++, "active", false));
            }

            var practiceSquadPositions = new[] { "QB", "RB", "WR", "WR", "TE", "LT", "DT", "CB" };
            for (var i = 0; i < practiceSquadPositions.Length; i++)
                players.Add(CreatePlayer(teamId, worldSeed, practiceSquadPositions[i], 100 + i, "practice_squad", false));

            var irPositions = new[] { "RB", "LB" };
            for (var i = 0; i < irPositions.Length; i++)
                players.Add(CreatePlayer(teamId, worldSeed, irPositions[i], 200 + i, "injured_reserve", true));

            return players;
        }

        public static TeamDepthChart CreateDepthChart(string teamId, IEnumerable<Player> players)
        {
            var depthChart = new TeamDepthChart { TeamId = teamId };
            foreach (var requirement in ActiveRosterTemplate)
            {
                depthChart.Positions.Add(new DepthChartPosition
                {
                    Position = requirement.Position,
                    RequiredStarters = requirement.Starters
                });
            }

            AutoFillDepthChart(depthChart, players);
            return depthChart;
        }

        public static void AutoFillDepthChart(TeamDepthChart depthChart, IEnumerable<Player> players)
        {
            if (depthChart == null)
                throw new ArgumentNullException(nameof(depthChart));

            var availablePlayers = players?
                .Where(p => string.Equals(p.RosterBucket, "active", StringComparison.OrdinalIgnoreCase) && !p.OnInjuredReserve)
                .ToList() ?? new List<Player>();

            foreach (var slot in depthChart.Positions)
            {
                slot.PlayerIds.Clear();
                var ordered = availablePlayers
                    .Where(player => string.Equals(player.Position, slot.Position, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(player => player.Overall)
                    .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(player => player.Id)
                    .ToList();
                slot.PlayerIds.AddRange(ordered);
            }
        }

        public static List<DepthChartMaintenanceNotice> AutoRepairDepthChart(TeamDepthChart depthChart, IEnumerable<Player> players, string teamDisplayName = "")
        {
            if (depthChart == null)
                throw new ArgumentNullException(nameof(depthChart));

            var roster = players?.ToList() ?? new List<Player>();
            var notices = new List<DepthChartMaintenanceNotice>();
            foreach (var slot in depthChart.Positions)
            {
                var removedPlayers = new List<string>();
                var preserved = new List<string>();
                foreach (var playerId in slot.PlayerIds)
                {
                    var player = roster.FirstOrDefault(item => string.Equals(item.Id, playerId, StringComparison.Ordinal));
                    if (player == null)
                    {
                        removedPlayers.Add("Unknown player");
                        continue;
                    }
                    if (!string.Equals(player.Position, slot.Position, StringComparison.OrdinalIgnoreCase))
                    {
                        removedPlayers.Add(player.Name);
                        continue;
                    }
                    if (!string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        removedPlayers.Add(player.Name);
                        continue;
                    }
                    if (player.OnInjuredReserve)
                    {
                        removedPlayers.Add(player.Name);
                        continue;
                    }
                    if (preserved.Contains(player.Id, StringComparer.Ordinal))
                        continue;
                    preserved.Add(player.Id);
                }

                var replacements = roster
                    .Where(player =>
                        string.Equals(player.Position, slot.Position, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase)
                        && !player.OnInjuredReserve
                        && !preserved.Contains(player.Id, StringComparer.Ordinal))
                    .OrderByDescending(player => player.Overall)
                    .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var addedPlayers = replacements
                    .Where(player => !preserved.Contains(player.Id, StringComparer.Ordinal))
                    .Select(player => player.Name)
                    .ToList();
                slot.PlayerIds = preserved.Concat(replacements.Select(player => player.Id)).ToList();

                if (removedPlayers.Count > 0 && addedPlayers.Count > 0)
                {
                    notices.Add(new DepthChartMaintenanceNotice
                    {
                        TeamId = depthChart.TeamId,
                        Type = "depth_chart_notice",
                        Title = $"{slot.Position} adjusted",
                        Description = $"{DisplayTeam(teamDisplayName, depthChart.TeamId)} removed {removedPlayers[0]} and promoted {addedPlayers[0]} at {slot.Position}.",
                        Severity = "warning",
                        PrimaryAction = "Review Depth Chart"
                    });
                }
                else if (removedPlayers.Count > 0)
                {
                    notices.Add(new DepthChartMaintenanceNotice
                    {
                        TeamId = depthChart.TeamId,
                        Type = "depth_chart_invalid",
                        Title = $"{slot.Position} needs attention",
                        Description = $"{DisplayTeam(teamDisplayName, depthChart.TeamId)} lost {removedPlayers[0]} at {slot.Position} and has no healthy replacement ready.",
                        Severity = "danger",
                        PrimaryAction = "Open Depth Chart"
                    });
                }
            }

            return notices;
        }

        public static bool ApplyDepthChartAction(TeamDepthChart depthChart, IEnumerable<Player> players, string position, string playerId, string action)
        {
            if (depthChart == null || string.IsNullOrWhiteSpace(position) || string.IsNullOrWhiteSpace(playerId))
                return false;

            var slot = depthChart.Positions.FirstOrDefault(item =>
                string.Equals(item.Position, position, StringComparison.OrdinalIgnoreCase));
            if (slot == null)
                return false;

            if (!slot.PlayerIds.Contains(playerId, StringComparer.Ordinal))
            {
                var isEligible = players != null && players.Any(player =>
                    string.Equals(player.Id, playerId, StringComparison.Ordinal)
                    && string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase)
                    && !player.OnInjuredReserve);
                if (!isEligible)
                    return false;
                slot.PlayerIds.Add(playerId);
            }

            var index = slot.PlayerIds.FindIndex(id => string.Equals(id, playerId, StringComparison.Ordinal));
            if (index < 0)
                return false;

            switch ((action ?? "").Trim().ToLowerInvariant())
            {
                case "move_up":
                    if (index == 0)
                        return false;
                    (slot.PlayerIds[index - 1], slot.PlayerIds[index]) = (slot.PlayerIds[index], slot.PlayerIds[index - 1]);
                    return true;
                case "move_down":
                    if (index >= slot.PlayerIds.Count - 1)
                        return false;
                    (slot.PlayerIds[index], slot.PlayerIds[index + 1]) = (slot.PlayerIds[index + 1], slot.PlayerIds[index]);
                    return true;
                case "set_starter":
                    if (index == 0)
                        return false;
                    slot.PlayerIds.RemoveAt(index);
                    slot.PlayerIds.Insert(0, playerId);
                    return true;
                default:
                    return false;
            }
        }

        public static List<string> ValidateDepthChart(TeamDepthChart depthChart, IEnumerable<Player>? players = null)
        {
            var issues = new List<string>();
            if (depthChart == null)
            {
                issues.Add("Depth chart unavailable.");
                return issues;
            }

            var roster = players?.ToList();

            foreach (var slot in DepthChartRequirements)
            {
                var position = depthChart.Positions.FirstOrDefault(item =>
                    string.Equals(item.Position, slot.Position, StringComparison.OrdinalIgnoreCase));
                var count = position?.PlayerIds?.Count ?? 0;
                if (count < slot.Starters)
                    issues.Add($"{slot.Position} needs {slot.Starters - count} more starter-ready player(s).");

                if (roster != null && position?.PlayerIds != null)
                {
                    var availableStarterCount = position.PlayerIds
                        .Select(playerId => roster.FirstOrDefault(player => string.Equals(player.Id, playerId, StringComparison.Ordinal)))
                        .Where(player => player != null
                            && string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase)
                            && !player.OnInjuredReserve)
                        .Take(slot.Starters)
                        .Count();
                    if (availableStarterCount < slot.Starters)
                        issues.Add($"{slot.Position} is missing {slot.Starters - availableStarterCount} playable starter(s).");
                }
            }

            return issues;
        }

        public static RosterValidationResult EvaluateRoster(IEnumerable<Player> players)
        {
            var roster = players?.ToList() ?? new List<Player>();
            var activePlayers = roster.Where(player => string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase)).ToList();
            var result = new RosterValidationResult
            {
                RosterSize = activePlayers.Count,
                RosterLimit = ActiveRosterLimit,
                RequiredCuts = Math.Max(0, activePlayers.Count - ActiveRosterLimit),
                InjuredCount = roster.Count(player => player.Injury != null && !player.Injury.IsHealthy),
                IsValid = true
            };

            foreach (var requirement in ActiveRosterTemplate)
            {
                var count = activePlayers.Count(player => string.Equals(player.Position, requirement.Position, StringComparison.OrdinalIgnoreCase));
                result.PositionCounts.Add(new PositionCount { Position = requirement.Position, Count = count });
            }

            if (result.RequiredCuts > 0)
                result.Issues.Add($"{result.RequiredCuts} player(s) over the 53-man roster limit.");

            foreach (var requirement in ActiveRosterTemplate)
            {
                var count = activePlayers.Count(player => string.Equals(player.Position, requirement.Position, StringComparison.OrdinalIgnoreCase));
                if (count == 0)
                    result.Issues.Add($"No active {requirement.Position} on the roster.");
            }

            result.IsValid = result.Issues.Count == 0;
            return result;
        }

        public static TeamReadiness EvaluateTeamReadiness(int baseStrength, IEnumerable<Player> players, TeamDepthChart depthChart, uint matchupSeed)
        {
            var roster = players?.ToList() ?? new List<Player>();
            var readiness = new TeamReadiness();
            var starterRatings = new List<int>();
            var usedPlayerIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var requirement in ActiveRosterTemplate)
            {
                var available = GetAvailablePlayersForPosition(depthChart, roster, requirement.Position, matchupSeed, usedPlayerIds);
                readiness.AvailableByPosition[requirement.Position] = available;
                var starters = available.Take(requirement.Starters).ToList();
                readiness.MissingStarters += Math.Max(0, requirement.Starters - starters.Count);
                starterRatings.AddRange(starters.Select(player => Math.Max(35, player.Overall - AvailabilityPenalty(player))));
                foreach (var starter in starters)
                    usedPlayerIds.Add(starter.Id);
            }

            readiness.UnavailablePlayers = roster.Count(player => !IsAvailableForGame(player, matchupSeed));
            readiness.InjuryPenalty = roster
                .Where(player => string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase))
                .Sum(AvailabilityPenalty);

            var starterAverage = starterRatings.Count == 0 ? 60 : (int)Math.Round(starterRatings.Average(), MidpointRounding.AwayFromZero);
            readiness.EffectiveStrength = Math.Clamp(
                baseStrength + ((starterAverage - 70) / 2) - (readiness.MissingStarters * 6) - (Math.Min(12, readiness.InjuryPenalty / 8)),
                45,
                95);
            return readiness;
        }

        public static bool IsAvailableForGame(Player player, uint matchupSeed)
        {
            if (player == null)
                return false;
            if (!string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase))
                return false;
            if (player.OnInjuredReserve)
                return false;
            if (player.Injury == null || player.Injury.IsHealthy)
                return true;

            var injuryStatus = (player.Injury.Status ?? "").Trim().ToLowerInvariant();
            var roll = StableSeed.Hash32($"{player.Id}:{matchupSeed}") % 100;
            return injuryStatus switch
            {
                "probable" => roll >= 10,
                "questionable" => roll >= 35,
                "doubtful" => roll >= 75,
                "out" => false,
                "ir" => false,
                _ => true
            };
        }

        public static int AvailabilityPenalty(Player player)
        {
            if (player == null || player.Injury == null || player.Injury.IsHealthy)
                return 0;
            if (player.OnInjuredReserve)
                return 25;

            var injuryStatus = (player.Injury.Status ?? "").Trim().ToLowerInvariant();
            return injuryStatus switch
            {
                "probable" => 2,
                "questionable" => 6,
                "doubtful" => 12,
                "out" => 25,
                "ir" => 25,
                _ => 4
            };
        }

        public static List<PlayerRecoveryNotice> AdvanceWeeklyRecovery(IEnumerable<Player> players)
        {
            var notices = new List<PlayerRecoveryNotice>();
            if (players == null)
                return notices;

            foreach (var player in players)
            {
                if (player?.Injury == null || player.Injury.IsHealthy)
                    continue;

                player.Injury.DaysRemaining = Math.Max(0, player.Injury.DaysRemaining - 7);
                if (player.Injury.DaysRemaining <= 0)
                {
                    player.Injury = new PlayerInjury();
                    player.OnInjuredReserve = false;
                    if (string.Equals(player.RosterBucket, "injured_reserve", StringComparison.OrdinalIgnoreCase))
                        player.RosterBucket = "active";
                    notices.Add(new PlayerRecoveryNotice
                    {
                        TeamId = player.TeamId,
                        PlayerId = player.Id,
                        PlayerName = player.Name,
                        Position = player.Position
                    });
                    continue;
                }

                if (player.OnInjuredReserve)
                    player.Injury.Status = "ir";
                else if (player.Injury.DaysRemaining <= 2)
                    player.Injury.Status = "probable";
                else if (player.Injury.DaysRemaining <= 5)
                    player.Injury.Status = "questionable";
                else
                    player.Injury.Status = "doubtful";

                player.Injury.ReturnLabel = player.Injury.DaysRemaining <= 7
                    ? "Soon"
                    : $"Week +{Math.Max(1, (int)Math.Ceiling(player.Injury.DaysRemaining / 7.0))}";
            }

            return notices;
        }

        public static int ApplyPostGameInjuries(IEnumerable<Player> availablePlayers, uint gameSeed)
        {
            if (availablePlayers == null)
                return 0;

            var injured = 0;
            foreach (var player in availablePlayers)
            {
                if (player == null || player.OnInjuredReserve)
                    continue;

                if (!string.Equals(player.RosterBucket, "active", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (player.Injury != null && !player.Injury.IsHealthy)
                    continue;

                var riskSeed = StableSeed.Hash32($"{player.Id}:{gameSeed}:postgame");
                var roll = (int)(riskSeed % 1000);
                if (roll >= 55)
                    continue;

                var severityRoll = (int)((riskSeed / 10) % 100);
                if (severityRoll < 7)
                {
                    player.OnInjuredReserve = true;
                    player.RosterBucket = "injured_reserve";
                    player.Injury = new PlayerInjury
                    {
                        Status = "ir",
                        Name = LongTermInjuryName(player.Position),
                        DaysRemaining = 21 + (int)(riskSeed % 29),
                        ReturnLabel = $"Week +{Math.Max(3, (int)((21 + (riskSeed % 29) + 6) / 7))}"
                    };
                }
                else if (severityRoll < 35)
                {
                    player.Injury = new PlayerInjury
                    {
                        Status = "doubtful",
                        Name = MinorInjuryName(player.Position),
                        DaysRemaining = 6 + (int)(riskSeed % 9),
                        ReturnLabel = "Soon"
                    };
                }
                else
                {
                    player.Injury = new PlayerInjury
                    {
                        Status = "questionable",
                        Name = "Bruised ribs",
                        DaysRemaining = 2 + (int)(riskSeed % 4),
                        ReturnLabel = "Soon"
                    };
                }

                injured++;
            }

            return injured;
        }

        public static int PositionSortOrder(string position)
        {
            if (string.IsNullOrWhiteSpace(position))
                return int.MaxValue;

            for (var index = 0; index < ActiveRosterTemplate.Length; index++)
            {
                if (string.Equals(ActiveRosterTemplate[index].Position, position, StringComparison.OrdinalIgnoreCase))
                    return index;
            }

            return int.MaxValue;
        }

        public static List<TeamStanding> BuildStandings(IEnumerable<string> teamIds, IEnumerable<SeasonGameSnapshot> games)
        {
            var teamList = (teamIds ?? Enumerable.Empty<string>()).Distinct(StringComparer.Ordinal).ToList();
            var regularSeasonGames = (games ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed && IsRegularSeasonGame(game.GameType))
                .ToList();
            var standings = teamList.ToDictionary(teamId => teamId, teamId => new TeamStanding { TeamId = teamId }, StringComparer.Ordinal);

            foreach (var game in regularSeasonGames)
            {
                if (!standings.TryGetValue(game.HomeTeamId, out var home) || !standings.TryGetValue(game.AwayTeamId, out var away))
                    continue;

                home.PointsFor += game.HomeScore;
                home.PointsAgainst += game.AwayScore;
                away.PointsFor += game.AwayScore;
                away.PointsAgainst += game.HomeScore;

                if (game.HomeScore > game.AwayScore)
                {
                    home.Wins++;
                    away.Losses++;
                }
                else if (game.AwayScore > game.HomeScore)
                {
                    away.Wins++;
                    home.Losses++;
                }
                else
                {
                    home.Ties++;
                    away.Ties++;
                }
            }

            return standings.Values
                .GroupBy(item => item.WinPct)
                .OrderByDescending(group => group.Key)
                .ThenByDescending(group => group.Max(item => item.Wins))
                .ThenBy(group => group.Min(item => item.Losses))
                .SelectMany(group => BreakStandingTies(group.ToList(), regularSeasonGames))
                .ToList();
        }

        public static List<PlayoffSeed> SelectConferencePlayoffSeeds(
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games,
            string conference)
        {
            var teamLookup = (teams ?? Enumerable.Empty<LeagueTeamDefinition>())
                .ToDictionary(team => team.Id, StringComparer.Ordinal);
            var conferenceTeams = teamLookup.Values
                .Where(team => string.Equals(team.Conference, conference, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var contexts = BuildStandingContexts(conferenceTeams, standings, games);
            if (contexts.Count == 0)
                return new List<PlayoffSeed>();

            var divisionWinners = contexts
                .GroupBy(context => context.Team.Division, StringComparer.OrdinalIgnoreCase)
                .Select(group => RankDivisionContexts(group.ToList(), games).First())
                .ToList();

            var rankedDivisionWinners = RankConferenceContexts(divisionWinners, games).ToList();
            var wildCards = RankConferenceContexts(
                    contexts.Where(context => rankedDivisionWinners.All(winner => !string.Equals(winner.Team.Id, context.Team.Id, StringComparison.Ordinal)))
                        .ToList(),
                    games)
                .Take(3)
                .ToList();

            return rankedDivisionWinners
                .Concat(wildCards)
                .Select((context, index) => new PlayoffSeed { Seed = index + 1, TeamId = context.Team.Id })
                .ToList();
        }

        public static Dictionary<string, int> BuildConferencePlayoffSeedLookup(
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games)
        {
            var teamList = (teams ?? Enumerable.Empty<LeagueTeamDefinition>()).ToList();
            return teamList
                .Select(team => team.Conference)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(conference => SelectConferencePlayoffSeeds(teamList, standings, games, conference))
                .ToDictionary(seed => seed.TeamId, seed => seed.Seed, StringComparer.Ordinal);
        }

        public static Dictionary<string, int> BuildDivisionRanks(
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games)
        {
            var teamList = (teams ?? Enumerable.Empty<LeagueTeamDefinition>()).ToList();
            var contexts = BuildStandingContexts(teamList, standings, games);
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var divisionGroup in contexts.GroupBy(context => $"{context.Team.Conference}:{context.Team.Division}", StringComparer.OrdinalIgnoreCase))
            {
                var ordered = RankDivisionContexts(divisionGroup.ToList(), games).ToList();
                for (var index = 0; index < ordered.Count; index++)
                    ranks[ordered[index].Team.Id] = index + 1;
            }

            return ranks;
        }

        public static Dictionary<string, int> BuildConferenceRanks(
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games)
        {
            var teamList = (teams ?? Enumerable.Empty<LeagueTeamDefinition>()).ToList();
            var contexts = BuildStandingContexts(teamList, standings, games);
            var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var conferenceGroup in contexts.GroupBy(context => context.Team.Conference, StringComparer.OrdinalIgnoreCase))
            {
                var ordered = RankConferenceContexts(conferenceGroup.ToList(), games).ToList();
                for (var index = 0; index < ordered.Count; index++)
                    ranks[ordered[index].Team.Id] = index + 1;
            }

            return ranks;
        }

        public static Dictionary<string, PlayoffRaceStatus> BuildPlayoffRaceStatuses(
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games)
        {
            var teamList = (teams ?? Enumerable.Empty<LeagueTeamDefinition>()).ToList();
            var standingList = (standings ?? Enumerable.Empty<TeamStanding>()).ToList();
            var snapshots = (games ?? Enumerable.Empty<SeasonGameSnapshot>()).ToList();
            var divisionRanks = BuildDivisionRanks(teamList, standingList, snapshots);
            var conferenceRanks = BuildConferenceRanks(teamList, standingList, snapshots);
            var seedLookup = BuildConferencePlayoffSeedLookup(teamList, standingList, snapshots);
            var standingsById = standingList.ToDictionary(standing => standing.TeamId, StringComparer.Ordinal);
            var remainingGames = BuildRemainingRegularSeasonGamesByTeam(teamList.Select(team => team.Id), snapshots);
            var teamLookup = teamList.ToDictionary(team => team.Id, StringComparer.Ordinal);
            var statuses = new Dictionary<string, PlayoffRaceStatus>(StringComparer.Ordinal);

            foreach (var team in teamList)
            {
                var standing = standingsById.TryGetValue(team.Id, out var resolvedStanding)
                    ? resolvedStanding
                    : new TeamStanding { TeamId = team.Id };
                var remaining = remainingGames.TryGetValue(team.Id, out var resolvedRemaining) ? resolvedRemaining : 0;
                var maxWins = standing.Wins + remaining;
                var divisionRank = divisionRanks.TryGetValue(team.Id, out var resolvedDivisionRank) ? resolvedDivisionRank : 0;
                var conferenceRank = conferenceRanks.TryGetValue(team.Id, out var resolvedConferenceRank) ? resolvedConferenceRank : 0;
                var playoffSeed = seedLookup.TryGetValue(team.Id, out var resolvedSeed) ? resolvedSeed : 0;
                var divisionOpponents = teamList.Where(other =>
                        !string.Equals(other.Id, team.Id, StringComparison.Ordinal)
                        && string.Equals(other.Conference, team.Conference, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(other.Division, team.Division, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var clinchedDivision = divisionOpponents.All(opponent =>
                {
                    var opponentStanding = standingsById.TryGetValue(opponent.Id, out var rivalStanding)
                        ? rivalStanding
                        : new TeamStanding { TeamId = opponent.Id };
                    var opponentRemaining = remainingGames.TryGetValue(opponent.Id, out var rivalRemaining) ? rivalRemaining : 0;
                    return standing.Wins > opponentStanding.Wins + opponentRemaining;
                });

                var canStillWinDivision = divisionRank == 1 || divisionOpponents.All(opponent =>
                {
                    var opponentStanding = standingsById.TryGetValue(opponent.Id, out var rivalStanding)
                        ? rivalStanding
                        : new TeamStanding { TeamId = opponent.Id };
                    return maxWins >= opponentStanding.Wins;
                });

                var sameConferenceTeams = teamList.Where(other =>
                        string.Equals(other.Conference, team.Conference, StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(other.Id, team.Id, StringComparison.Ordinal))
                    .ToList();
                var nonSeededConferenceTeams = sameConferenceTeams
                    .Where(other => !seedLookup.ContainsKey(other.Id))
                    .ToList();
                var clinchedPlayoff = clinchedDivision || (playoffSeed > 0 && nonSeededConferenceTeams.All(opponent =>
                {
                    var opponentStanding = standingsById.TryGetValue(opponent.Id, out var rivalStanding)
                        ? rivalStanding
                        : new TeamStanding { TeamId = opponent.Id };
                    var opponentRemaining = remainingGames.TryGetValue(opponent.Id, out var rivalRemaining) ? rivalRemaining : 0;
                    return standing.Wins > opponentStanding.Wins + opponentRemaining;
                }));

                var currentConferenceField = teamList
                    .Where(other => string.Equals(other.Conference, team.Conference, StringComparison.OrdinalIgnoreCase))
                    .Where(other => conferenceRanks.TryGetValue(other.Id, out var rank) && rank <= 7)
                    .ToList();
                var eliminationThreshold = currentConferenceField.Count == 0
                    ? 0
                    : currentConferenceField.Min(other =>
                    {
                        var otherStanding = standingsById.TryGetValue(other.Id, out var fieldStanding)
                            ? fieldStanding
                            : new TeamStanding { TeamId = other.Id };
                        return otherStanding.Wins;
                    });
                var eliminated = !clinchedPlayoff
                    && !canStillWinDivision
                    && currentConferenceField.Count >= 7
                    && maxWins < eliminationThreshold;

                var inHunt = !clinchedPlayoff && !eliminated && conferenceRank > 0 && conferenceRank <= 10;
                var statusLabel = clinchedDivision
                    ? "Clinched Division"
                    : clinchedPlayoff
                        ? "Clinched Playoff"
                        : eliminated
                            ? "Eliminated"
                            : playoffSeed > 0
                                ? playoffSeed <= 4 ? "Division Leader" : "Wild Card"
                                : inHunt
                                    ? "In Hunt"
                                    : "";
                var spotLabel = clinchedDivision
                    ? $"x-Seed {playoffSeed}"
                    : clinchedPlayoff
                        ? playoffSeed > 0 ? $"x-Seed {playoffSeed}" : "x-Clinched"
                        : eliminated
                            ? "E"
                            : playoffSeed > 0
                                ? playoffSeed <= 4 ? $"Seed {playoffSeed}" : $"WC {playoffSeed - 4}"
                                : divisionRank == 1 ? "Leader" : inHunt ? "Hunt" : "";

                statuses[team.Id] = new PlayoffRaceStatus
                {
                    TeamId = team.Id,
                    ClinchedDivision = clinchedDivision,
                    ClinchedPlayoff = clinchedPlayoff,
                    Eliminated = eliminated,
                    InHunt = inHunt,
                    RemainingGames = remaining,
                    MaxWins = maxWins,
                    StatusLabel = statusLabel,
                    SpotLabel = spotLabel
                };
            }

            return statuses;
        }

        public static List<SeasonGameSnapshot> CreateWildCardGames(IEnumerable<PlayoffSeed> seeds, string conference, int week = WildCardWeek)
        {
            var orderedSeeds = (seeds ?? Enumerable.Empty<PlayoffSeed>())
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (orderedSeeds.Count < 7)
                return new List<SeasonGameSnapshot>();

            return new List<SeasonGameSnapshot>
            {
                CreatePlayoffGame(week, PlayoffWildCardGameType, conference, 1, orderedSeeds[1], orderedSeeds[6]),
                CreatePlayoffGame(week, PlayoffWildCardGameType, conference, 2, orderedSeeds[2], orderedSeeds[5]),
                CreatePlayoffGame(week, PlayoffWildCardGameType, conference, 3, orderedSeeds[3], orderedSeeds[4])
            };
        }

        public static List<SeasonGameSnapshot> CreateDivisionalGames(IEnumerable<PlayoffSeed> seeds, IEnumerable<SeasonGameSnapshot> completedWildCardGames, string conference, int week = DivisionalWeek)
        {
            var orderedSeeds = (seeds ?? Enumerable.Empty<PlayoffSeed>())
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (orderedSeeds.Count < 7)
                return new List<SeasonGameSnapshot>();

            var topSeed = orderedSeeds[0];
            var wildCardWinners = (completedWildCardGames ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed
                    && string.Equals(game.GameType, PlayoffWildCardGameType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(ExtractConferenceFromPlayoffGameId(game.Id), conference, StringComparison.OrdinalIgnoreCase))
                .Select(ResolveWinner)
                .OfType<PlayoffSeed>()
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (wildCardWinners.Count < 3)
                return new List<SeasonGameSnapshot>();

            return new List<SeasonGameSnapshot>
            {
                CreatePlayoffGame(week, PlayoffDivisionalGameType, conference, 1, topSeed, wildCardWinners[^1]),
                CreatePlayoffGame(week, PlayoffDivisionalGameType, conference, 2, wildCardWinners[0], wildCardWinners[1])
            };
        }

        public static SeasonGameSnapshot? CreateConferenceChampionshipGame(IEnumerable<SeasonGameSnapshot> divisionalGames, string conference, int week = ConferenceChampionshipWeek)
        {
            var winners = (divisionalGames ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed
                    && string.Equals(game.GameType, PlayoffDivisionalGameType, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(ExtractConferenceFromPlayoffGameId(game.Id), conference, StringComparison.OrdinalIgnoreCase))
                .Select(ResolveWinner)
                .OfType<PlayoffSeed>()
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (winners.Count < 2)
                return null;

            return CreatePlayoffGame(week, PlayoffConferenceChampionshipGameType, conference, 1, winners[0], winners[1]);
        }

        public static SeasonGameSnapshot? CreateChampionshipGame(IEnumerable<SeasonGameSnapshot> conferenceChampionshipGames, int week = ChampionshipWeek)
        {
            var conferenceWinners = (conferenceChampionshipGames ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed && string.Equals(game.GameType, PlayoffConferenceChampionshipGameType, StringComparison.OrdinalIgnoreCase))
                .Select(ResolveWinner)
                .OfType<PlayoffSeed>()
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (conferenceWinners.Count < 2)
                return null;

            return new SeasonGameSnapshot
            {
                Id = $"w{week}-final",
                Week = week,
                GameType = ChampionshipGameType,
                HomeTeamId = conferenceWinners[0].TeamId,
                AwayTeamId = conferenceWinners[1].TeamId,
                HomeSeed = conferenceWinners[0].Seed,
                AwaySeed = conferenceWinners[1].Seed
            };
        }

        private static IEnumerable<TeamStanding> BreakStandingTies(IReadOnlyList<TeamStanding> tiedTeams, IReadOnlyList<SeasonGameSnapshot> regularSeasonGames)
        {
            if (tiedTeams.Count <= 1)
                return tiedTeams;

            var tiedIds = tiedTeams.Select(team => team.TeamId).ToHashSet(StringComparer.Ordinal);
            return tiedTeams
                .OrderByDescending(team => HeadToHeadWins(team.TeamId, tiedIds, regularSeasonGames))
                .ThenByDescending(team => HeadToHeadPointDifferential(team.TeamId, tiedIds, regularSeasonGames))
                .ThenByDescending(team => team.PointDifferential)
                .ThenByDescending(team => team.PointsFor)
                .ThenBy(team => team.TeamId, StringComparer.Ordinal);
        }

        private static List<TeamStandingContext> BuildStandingContexts(
            IEnumerable<LeagueTeamDefinition> teams,
            IEnumerable<TeamStanding> standings,
            IEnumerable<SeasonGameSnapshot> games)
        {
            var standingLookup = (standings ?? Enumerable.Empty<TeamStanding>())
                .ToDictionary(standing => standing.TeamId, StringComparer.Ordinal);
            var teamList = (teams ?? Enumerable.Empty<LeagueTeamDefinition>()).ToList();
            var teamLookup = teamList.ToDictionary(team => team.Id, StringComparer.Ordinal);
            var regularSeasonGames = (games ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed && IsRegularSeasonGame(game.GameType))
                .ToList();

            return teamList.Select(team =>
            {
                var context = new TeamStandingContext
                {
                    Team = team,
                    Standing = standingLookup.TryGetValue(team.Id, out var standing)
                        ? standing
                        : new TeamStanding { TeamId = team.Id }
                };

                foreach (var game in regularSeasonGames.Where(game => string.Equals(game.HomeTeamId, team.Id, StringComparison.Ordinal) || string.Equals(game.AwayTeamId, team.Id, StringComparison.Ordinal)))
                {
                    var opponentId = string.Equals(game.HomeTeamId, team.Id, StringComparison.Ordinal) ? game.AwayTeamId : game.HomeTeamId;
                    if (!teamLookup.TryGetValue(opponentId, out var opponent))
                        continue;

                    var won = string.Equals(game.HomeTeamId, team.Id, StringComparison.Ordinal)
                        ? game.HomeScore > game.AwayScore
                        : game.AwayScore > game.HomeScore;
                    var lost = string.Equals(game.HomeTeamId, team.Id, StringComparison.Ordinal)
                        ? game.HomeScore < game.AwayScore
                        : game.AwayScore < game.HomeScore;

                    if (string.Equals(opponent.Conference, team.Conference, StringComparison.OrdinalIgnoreCase))
                    {
                        if (won)
                            context.ConferenceWins++;
                        else if (lost)
                            context.ConferenceLosses++;
                        else
                            context.ConferenceTies++;
                    }

                    if (string.Equals(opponent.Conference, team.Conference, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(opponent.Division, team.Division, StringComparison.OrdinalIgnoreCase))
                    {
                        if (won)
                            context.DivisionWins++;
                        else if (lost)
                            context.DivisionLosses++;
                        else
                            context.DivisionTies++;
                    }
                }

                return context;
            }).ToList();
        }

        private static IEnumerable<TeamStandingContext> RankConferenceContexts(IReadOnlyList<TeamStandingContext> contexts, IEnumerable<SeasonGameSnapshot> games)
        {
            if (contexts.Count <= 1)
                return contexts;

            var regularSeasonGames = (games ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed && IsRegularSeasonGame(game.GameType))
                .ToList();
            return contexts
                .GroupBy(context => context.Standing.WinPct)
                .OrderByDescending(group => group.Key)
                .ThenByDescending(group => group.Max(item => item.Standing.Wins))
                .ThenBy(group => group.Min(item => item.Standing.Losses))
                .SelectMany(group => BreakConferenceStandingContextTies(group.ToList(), regularSeasonGames));
        }

        private static IEnumerable<TeamStandingContext> RankDivisionContexts(IReadOnlyList<TeamStandingContext> contexts, IEnumerable<SeasonGameSnapshot> games)
        {
            if (contexts.Count <= 1)
                return contexts;

            var regularSeasonGames = (games ?? Enumerable.Empty<SeasonGameSnapshot>())
                .Where(game => game.Completed && IsRegularSeasonGame(game.GameType))
                .ToList();
            return contexts
                .GroupBy(context => context.Standing.WinPct)
                .OrderByDescending(group => group.Key)
                .ThenByDescending(group => group.Max(item => item.Standing.Wins))
                .ThenBy(group => group.Min(item => item.Standing.Losses))
                .SelectMany(group => BreakDivisionStandingContextTies(group.ToList(), regularSeasonGames));
        }

        private static IEnumerable<TeamStandingContext> BreakConferenceStandingContextTies(IReadOnlyList<TeamStandingContext> tiedTeams, IReadOnlyList<SeasonGameSnapshot> regularSeasonGames)
        {
            if (tiedTeams.Count <= 1)
                return tiedTeams;

            var tiedIds = tiedTeams.Select(team => team.Team.Id).ToHashSet(StringComparer.Ordinal);
            return tiedTeams
                .OrderByDescending(team => HeadToHeadWinPct(team.Team.Id, tiedIds, regularSeasonGames))
                .ThenByDescending(team => team.ConferenceWinPct)
                .ThenByDescending(team => team.Standing.PointDifferential)
                .ThenByDescending(team => team.Standing.PointsFor)
                .ThenBy(team => team.Team.Id, StringComparer.Ordinal);
        }

        private static IEnumerable<TeamStandingContext> BreakDivisionStandingContextTies(IReadOnlyList<TeamStandingContext> tiedTeams, IReadOnlyList<SeasonGameSnapshot> regularSeasonGames)
        {
            if (tiedTeams.Count <= 1)
                return tiedTeams;

            var tiedIds = tiedTeams.Select(team => team.Team.Id).ToHashSet(StringComparer.Ordinal);
            return tiedTeams
                .OrderByDescending(team => HeadToHeadWinPct(team.Team.Id, tiedIds, regularSeasonGames))
                .ThenByDescending(team => team.DivisionWinPct)
                .ThenByDescending(team => team.ConferenceWinPct)
                .ThenByDescending(team => team.Standing.PointDifferential)
                .ThenByDescending(team => team.Standing.PointsFor)
                .ThenBy(team => team.Team.Id, StringComparer.Ordinal);
        }

        private static double HeadToHeadWinPct(string teamId, HashSet<string> tiedIds, IReadOnlyList<SeasonGameSnapshot> games)
        {
            var wins = 0;
            var losses = 0;
            var ties = 0;
            foreach (var game in games.Where(game => tiedIds.Contains(game.HomeTeamId) && tiedIds.Contains(game.AwayTeamId)))
            {
                var isHome = string.Equals(game.HomeTeamId, teamId, StringComparison.Ordinal);
                var isAway = string.Equals(game.AwayTeamId, teamId, StringComparison.Ordinal);
                if (!isHome && !isAway)
                    continue;

                var teamScore = isHome ? game.HomeScore : game.AwayScore;
                var opponentScore = isHome ? game.AwayScore : game.HomeScore;
                if (teamScore > opponentScore)
                    wins++;
                else if (teamScore < opponentScore)
                    losses++;
                else
                    ties++;
            }

            var total = wins + losses + ties;
            return total == 0 ? 0 : (wins + (ties * 0.5)) / total;
        }

        private static int HeadToHeadWins(string teamId, HashSet<string> tiedIds, IReadOnlyList<SeasonGameSnapshot> games)
            => games.Count(game =>
                tiedIds.Contains(game.HomeTeamId)
                && tiedIds.Contains(game.AwayTeamId)
                && ((string.Equals(game.HomeTeamId, teamId, StringComparison.Ordinal) && game.HomeScore > game.AwayScore)
                    || (string.Equals(game.AwayTeamId, teamId, StringComparison.Ordinal) && game.AwayScore > game.HomeScore)));

        private static int HeadToHeadPointDifferential(string teamId, HashSet<string> tiedIds, IReadOnlyList<SeasonGameSnapshot> games)
            => games.Where(game => tiedIds.Contains(game.HomeTeamId) && tiedIds.Contains(game.AwayTeamId))
                .Sum(game =>
                    string.Equals(game.HomeTeamId, teamId, StringComparison.Ordinal)
                        ? game.HomeScore - game.AwayScore
                        : string.Equals(game.AwayTeamId, teamId, StringComparison.Ordinal)
                            ? game.AwayScore - game.HomeScore
                            : 0);

        private static Dictionary<string, int> BuildRemainingRegularSeasonGamesByTeam(IEnumerable<string> teamIds, IEnumerable<SeasonGameSnapshot> games)
        {
            var remaining = (teamIds ?? Enumerable.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .ToDictionary(teamId => teamId, _ => 0, StringComparer.Ordinal);
            foreach (var game in (games ?? Enumerable.Empty<SeasonGameSnapshot>()).Where(game => !game.Completed && IsRegularSeasonGame(game.GameType)))
            {
                if (remaining.ContainsKey(game.HomeTeamId))
                    remaining[game.HomeTeamId]++;
                if (remaining.ContainsKey(game.AwayTeamId))
                    remaining[game.AwayTeamId]++;
            }

            return remaining;
        }

        private static PlayoffSeed? ResolveWinner(SeasonGameSnapshot game)
        {
            if (game == null || !game.Completed)
                return null;

            return game.HomeScore > game.AwayScore
                ? new PlayoffSeed { Seed = game.HomeSeed, TeamId = game.HomeTeamId }
                : new PlayoffSeed { Seed = game.AwaySeed, TeamId = game.AwayTeamId };
        }

        private static SeasonGameSnapshot CreatePlayoffGame(int week, string gameType, string conference, int slot, PlayoffSeed homeSeed, PlayoffSeed awaySeed)
            => new()
            {
                Id = $"w{week}-{conference.ToLowerInvariant()}-{slot}",
                Week = week,
                GameType = gameType,
                HomeTeamId = homeSeed.TeamId,
                AwayTeamId = awaySeed.TeamId,
                HomeSeed = homeSeed.Seed,
                AwaySeed = awaySeed.Seed
            };

        private static string ExtractConferenceFromPlayoffGameId(string gameId)
        {
            var parts = (gameId ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 3 ? parts[1] : string.Empty;
        }

        private static Player CreatePlayer(string teamId, ulong worldSeed, string position, int index, string rosterBucket, bool injuredReserve)
        {
            var baseSeed = MixSeed(worldSeed, StableSeed.Hash32($"{teamId}:{position}:{index}:{rosterBucket}"));
            var seed = unchecked((uint)baseSeed);
            var name = $"{Pick(FirstNames, ref seed)} {Pick(LastNames, ref seed)}";
            var age = 21 + (int)(StableSeed.Next(ref seed) % 13);
            var overallFloor = injuredReserve ? 58 : string.Equals(rosterBucket, "practice_squad", StringComparison.OrdinalIgnoreCase) ? 51 : 61;
            var overallCeiling = injuredReserve ? 77 : string.Equals(rosterBucket, "practice_squad", StringComparison.OrdinalIgnoreCase) ? 69 : 86;
            var overall = overallFloor + (int)(StableSeed.Next(ref seed) % (uint)(overallCeiling - overallFloor + 1));
            var potential = Math.Min(99, overall + 4 + (int)(StableSeed.Next(ref seed) % 12));
            var jersey = 1 + (int)(StableSeed.Next(ref seed) % 98);
            var confidenceRoll = (int)(StableSeed.Next(ref seed) % 3);
            var confidence = confidenceRoll == 0 ? "Low" : confidenceRoll == 1 ? "Medium" : "High";
            var injury = BuildInjury(position, injuredReserve, ref seed);

            return new Player
            {
                Id = $"{teamId}-{position.ToLowerInvariant()}-{index:D2}",
                TeamId = teamId,
                Name = name,
                Position = position,
                Age = age,
                Overall = overall,
                Potential = potential,
                JerseyNumber = jersey,
                RosterBucket = rosterBucket,
                OnInjuredReserve = injuredReserve,
                Injury = injury,
                ScoutConfidence = confidence,
                ScoutSummary = BuildScoutSummary(position, overall, potential),
                ScoutReport = BuildScoutReport(position, overall, potential, confidence, injuredReserve),
                Tags = BuildTags(position, overall, potential, injuredReserve)
            };
        }

        private static PlayerInjury BuildInjury(string position, bool injuredReserve, ref uint seed)
        {
            if (injuredReserve)
            {
                var days = 21 + (int)(StableSeed.Next(ref seed) % 35);
                return new PlayerInjury
                {
                    Status = "ir",
                    Name = position switch
                    {
                        "RB" => "High ankle sprain",
                        "LB" => "Shoulder tear",
                        _ => "Knee sprain"
                    },
                    DaysRemaining = days,
                    ReturnLabel = $"Week +{Math.Max(2, days / 7)}"
                };
            }

            var roll = (int)(StableSeed.Next(ref seed) % 100);
            if (roll < 10)
            {
                var days = 1 + (int)(StableSeed.Next(ref seed) % 6);
                return new PlayerInjury
                {
                    Status = roll < 5 ? "questionable" : "probable",
                    Name = "Soreness",
                    DaysRemaining = days,
                    ReturnLabel = "Soon"
                };
            }

            return new PlayerInjury();
        }

        private static List<string> BuildTags(string position, int overall, int potential, bool injuredReserve)
        {
            var tags = new List<string>();
            if (overall >= 80)
                tags.Add("Starter");
            if (potential - overall >= 8)
                tags.Add("Upside");
            if (string.Equals(position, "QB", StringComparison.OrdinalIgnoreCase))
                tags.Add("Field General");
            if (injuredReserve)
                tags.Add("Rehab");
            return tags;
        }

        private static string BuildScoutSummary(string position, int overall, int potential)
            => $"{(overall >= 80 ? "Day-one starter" : overall >= 70 ? "Rotation-ready contributor" : "Developmental depth piece")}. {position} with {potential - overall:+#;-#;0} upside relative to current play.";

        private static string BuildScoutReport(string position, int overall, int potential, string confidence, bool injuredReserve)
        {
            var healthLine = injuredReserve
                ? "Medical staff expects a longer recovery window before full practice."
                : "Available for normal workload with only routine wear-and-tear notes.";
            return $"{position} evaluation: current grade {overall}, upside {potential}, scout confidence {confidence}. {healthLine}";
        }

        private static string Pick(IReadOnlyList<string> values, ref uint seed)
            => values[(int)(StableSeed.Next(ref seed) % (uint)values.Count)];

        private static string DisplayTeam(string teamDisplayName, string teamId)
            => string.IsNullOrWhiteSpace(teamDisplayName) ? (string.IsNullOrWhiteSpace(teamId) ? "Team" : teamId) : teamDisplayName;

        private static string MinorInjuryName(string position) => position switch
        {
            "QB" => "Elbow soreness",
            "RB" => "Hamstring strain",
            "WR" => "Ankle sprain",
            "LB" => "Shoulder stinger",
            _ => "Muscle strain"
        };

        private static string LongTermInjuryName(string position) => position switch
        {
            "QB" => "Separated shoulder",
            "RB" => "High ankle sprain",
            "WR" => "Broken collarbone",
            "LB" => "Torn labrum",
            _ => "Knee sprain"
        };

        private static List<Player> GetAvailablePlayersForPosition(TeamDepthChart depthChart, IEnumerable<Player> players, string position, uint matchupSeed, HashSet<string> excludedPlayerIds)
        {
            var roster = players?.ToList() ?? new List<Player>();
            var ordered = new List<Player>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var positionSlot = depthChart?.Positions.FirstOrDefault(item =>
                string.Equals(item.Position, position, StringComparison.OrdinalIgnoreCase));

            if (positionSlot?.PlayerIds != null)
            {
                foreach (var playerId in positionSlot.PlayerIds)
                {
                    var player = roster.FirstOrDefault(item => string.Equals(item.Id, playerId, StringComparison.Ordinal));
                    if (player == null || seen.Contains(player.Id) || (excludedPlayerIds?.Contains(player.Id) ?? false))
                        continue;
                    if (!IsAvailableForGame(player, matchupSeed))
                        continue;
                    seen.Add(player.Id);
                    ordered.Add(player);
                }
            }

            var fallbackPlayers = roster
                .Where(player =>
                    string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase)
                    && !seen.Contains(player.Id)
                    && !(excludedPlayerIds?.Contains(player.Id) ?? false)
                    && IsAvailableForGame(player, matchupSeed))
                .OrderByDescending(player => player.Overall - AvailabilityPenalty(player))
                .ThenBy(player => player.Name, StringComparer.OrdinalIgnoreCase);

            ordered.AddRange(fallbackPlayers);
            return ordered;
        }

        private static ulong MixSeed(ulong left, uint right)
        {
            unchecked
            {
                return left ^ ((ulong)right << 17) ^ 0x9E3779B97F4A7C15UL;
            }
        }
    }

    public static class StableSeed
    {
        public static uint Hash32(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return hash;
            }
        }

        public static uint Next(ref uint value)
        {
            unchecked
            {
                value = value * 1664525 + 1013904223;
                return value;
            }
        }
    }
}
