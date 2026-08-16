using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class PlayoffService
{
    public const string WildCardRound = "Wild Card";
    public const string DivisionalRound = "Divisional";
    public const string ConferenceChampionshipRound = "Conference Championship";
    public const string LeagueChampionshipRound = "League Championship";
    public const string WildCardCompletedStopReason = "divisional_round_pending";
    public const string DivisionalCompletedStopReason = "conference_championship_pending";
    public const string ConferenceChampionshipCompletedStopReason = "league_championship_pending";
    public const string LeagueChampionshipCompletedStopReason = "season_complete";

    private readonly GameCoreContext _context;
    private readonly StandingsService _standingsService;

    public PlayoffService(GameCoreContext context)
    {
        _context = context;
        _standingsService = new StandingsService(context);
    }

    public bool EnsureBracketGenerated(LeagueState league, out string reason)
    {
        reason = "";
        if (league == null)
        {
            reason = "No active league loaded.";
            return false;
        }

        ScheduleService.NormalizeCalendar(league.Calendar);
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            reason = $"Playoff bracket generation skipped outside {ScheduleService.PostseasonPendingPhase}.";
            return false;
        }

        if (IsBracketValid(league.PlayoffBracket, league))
        {
            reason = "Playoff bracket already exists.";
            return true;
        }

        if (!TryBuildBracket(league, out var bracket, out reason))
            return false;

        league.PlayoffBracket = bracket;
        return true;
    }

    public PlayoffBracketDto GetPlayoffBracketDto(LeagueState league)
    {
        if (league == null)
            return new PlayoffBracketDto();

        EnsureBracketGenerated(league, out _);
        EnsureDivisionalRoundGenerated(league, out _);
        EnsureConferenceChampionshipRoundGenerated(league, out _);
        EnsureLeagueChampionshipRoundGenerated(league, out _);
        return ToDto(league.PlayoffBracket);
    }

    public string FormatBracketSummary(LeagueState league)
        => FormatBracketSummary(GetPlayoffBracketDto(league));

    public static string FormatBracketSummary(PlayoffBracketDto bracket)
    {
        if (bracket == null || bracket.ConferenceBrackets == null || bracket.ConferenceBrackets.Count == 0)
            return "Playoff bracket not generated yet.";

        var builder = new StringBuilder();
        foreach (var conferenceBracket in bracket.ConferenceBrackets
                     .Where(entry => entry != null)
                     .OrderBy(entry => entry.Conference, StringComparer.OrdinalIgnoreCase))
        {
            if (builder.Length > 0)
                builder.AppendLine().AppendLine();

            builder.AppendLine(string.IsNullOrWhiteSpace(conferenceBracket.Conference) ? "Conference" : conferenceBracket.Conference);

            var seeds = (conferenceBracket.Seeds ?? new List<PlayoffSeedDto>())
                .OrderBy(seed => seed.Seed)
                .ToDictionary(seed => seed.Seed);
            if (seeds.TryGetValue(1, out var topSeed))
                builder.AppendLine($"1. {topSeed.TeamName} - BYE");

            foreach (var seed in seeds.Values.Where(seed => seed.Seed is >= 2 and <= 7))
                builder.AppendLine($"{seed.Seed}. {seed.TeamName}");

            foreach (var round in (conferenceBracket.Rounds ?? new List<PlayoffRoundDto>())
                         .Where(round => round?.Games != null && round.Games.Count > 0)
                         .OrderBy(round => GetRoundOrder(round.Round)))
            {
                builder.AppendLine(BuildRoundHeading(round.Round, round.Status));
                foreach (var game in round.Games
                             .OrderBy(game => game.HomeSeed)
                             .ThenBy(game => game.AwaySeed))
                {
                    if (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                        && game.HomeScore.HasValue
                        && game.AwayScore.HasValue)
                    {
                        var winnerLabel = ResolveWinnerLabel(game);
                        builder.AppendLine($"{game.HomeSeed}. {game.HomeTeamName} {game.HomeScore}-{game.AwayScore} {game.AwayTeamName} ({winnerLabel})");
                        continue;
                    }

                    builder.AppendLine($"{game.HomeSeed}. {game.HomeTeamName} host {game.AwaySeed}. {game.AwayTeamName}");
                }
            }
        }

        if (bracket.LeagueChampionshipRound?.Games != null && bracket.LeagueChampionshipRound.Games.Count > 0)
        {
            if (builder.Length > 0)
                builder.AppendLine().AppendLine();

            builder.AppendLine(BuildRoundHeading(bracket.LeagueChampionshipRound.Round, bracket.LeagueChampionshipRound.Status));
            foreach (var game in bracket.LeagueChampionshipRound.Games
                         .Where(game => game != null)
                         .OrderBy(game => game.HomeTeamName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(game => game.AwayTeamName, StringComparer.OrdinalIgnoreCase))
            {
                if (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                    && game.HomeScore.HasValue
                    && game.AwayScore.HasValue)
                {
                    var winnerLabel = ResolveWinnerLabel(game);
                    builder.AppendLine($"{game.HomeTeamName} {game.HomeScore}-{game.AwayScore} {game.AwayTeamName} ({winnerLabel})");
                    continue;
                }

                builder.AppendLine($"{NormalizeBracketTeamName(game.HomeTeamName)} vs {NormalizeBracketTeamName(game.AwayTeamName)}");
            }
        }

        if (!string.IsNullOrWhiteSpace(bracket.LeagueChampionRecord?.ChampionTeamName))
        {
            if (builder.Length > 0)
                builder.AppendLine().AppendLine();
            builder.AppendLine($"League Champion: {bracket.LeagueChampionRecord.ChampionTeamName}");
        }

        return builder.Length == 0 ? "Playoff bracket not generated yet." : builder.ToString();
    }

    public WildCardSimulationResult SimulateWildCardRound(LeagueState league)
    {
        if (league == null)
        {
            return new WildCardSimulationResult
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        if (!EnsureBracketGenerated(league, out var bracketReason))
        {
            return new WildCardSimulationResult
            {
                Ok = false,
                Error = bracketReason,
            };
        }

        var bracket = league.PlayoffBracket;
        var wildCardGames = (bracket?.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            .SelectMany(conference => conference?.Rounds ?? new List<PlayoffRound>())
            .Where(round => string.Equals(round?.Round, WildCardRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new List<PlayoffGame>())
            .Where(game => game != null)
            .OrderBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ToList();

        if (wildCardGames.Count != 6)
        {
            return new WildCardSimulationResult
            {
                Ok = false,
                Error = $"Wild Card round requires 6 games; found {wildCardGames.Count}.",
            };
        }

        var simulatedGames = 0;
        foreach (var game in wildCardGames)
        {
            NormalizePlayoffGame(game);
            if (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(game.WinnerTeamId)
                && game.HomeScore.HasValue
                && game.AwayScore.HasValue)
            {
                EnsureLinkedPlayoffResult(league, game);
                continue;
            }

            var existing = league.Results.FirstOrDefault(result =>
                string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase));
            GameResult result;
            if (existing != null)
            {
                ScheduleService.NormalizeResult(existing);
                result = existing;
            }
            else
            {
                result = GameDayService.SimulateMatchup(
                    league,
                    game.GameId,
                    game.HomeTeamId,
                    game.AwayTeamId,
                    game.AbsoluteWeek,
                    game.PhaseWeek,
                    game.Phase,
                    game.GameType,
                    game.RoundLabel,
                    dayIndex: 6,
                    homeFieldBonus: 3,
                    requireWinner: true);
                league.Results.Add(result);
                simulatedGames++;
            }

            ApplyResultToPlayoffGame(game, result);
        }

        foreach (var conferenceBracket in bracket.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
        {
            UpdateRoundStatuses(conferenceBracket);
        }

        EnsureDivisionalRoundGenerated(league, out _);

        return new WildCardSimulationResult
        {
            Ok = true,
            SimulatedGames = simulatedGames,
            AlreadyCompleted = simulatedGames == 0,
        };
    }

    public bool EnsureDivisionalRoundGenerated(LeagueState league, out string reason)
    {
        reason = "";
        if (league?.PlayoffBracket?.ConferenceBrackets == null || league.PlayoffBracket.ConferenceBrackets.Count == 0)
        {
            reason = "Playoff bracket not generated yet.";
            return false;
        }

        var wildCardGames = league.PlayoffBracket.ConferenceBrackets
            .SelectMany(conference => conference?.Rounds ?? new List<PlayoffRound>())
            .Where(round => string.Equals(round?.Round, WildCardRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new List<PlayoffGame>())
            .Where(game => game != null)
            .ToList();
        if (wildCardGames.Count != 6 || wildCardGames.Any(game => !string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Divisional Round cannot be generated before all Wild Card games are completed.";
            return false;
        }

        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets.Where(entry => entry != null))
        {
            conferenceBracket.Rounds ??= new List<PlayoffRound>();
            var existingDivisionalRound = conferenceBracket.Rounds.FirstOrDefault(round =>
                string.Equals(round?.Round, DivisionalRound, StringComparison.OrdinalIgnoreCase));
            if (existingDivisionalRound != null)
            {
                existingDivisionalRound.Games ??= new List<PlayoffGame>();
                if (existingDivisionalRound.Games.Count == 2)
                {
                    foreach (var game in existingDivisionalRound.Games)
                        NormalizePlayoffGame(game);
                    continue;
                }

                if (existingDivisionalRound.Games.Any(game => game != null
                        && (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                            || game.HomeScore.HasValue
                            || game.AwayScore.HasValue)))
                {
                    reason = $"{conferenceBracket.Conference} Divisional Round is malformed and cannot be safely regenerated.";
                    return false;
                }
            }

            var seedsByNumber = (conferenceBracket.Seeds ?? new List<PlayoffSeed>())
                .Where(seed => seed != null)
                .ToDictionary(seed => seed.Seed);
            if (!seedsByNumber.TryGetValue(1, out var topSeed))
            {
                reason = $"{conferenceBracket.Conference} missing seed 1 for Divisional Round generation.";
                return false;
            }

            var wildCardWinners = conferenceBracket.Rounds
                .Where(round => string.Equals(round?.Round, WildCardRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games ?? new List<PlayoffGame>())
                .Where(game => game != null && !string.IsNullOrWhiteSpace(game.WinnerTeamId))
                .Select(game =>
                {
                    var winningSeed = string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase)
                        ? game.HomeSeed
                        : game.AwaySeed;
                    return seedsByNumber.TryGetValue(winningSeed, out var seed) ? seed : null;
                })
                .Where(seed => seed != null)
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (wildCardWinners.Count != 3)
            {
                reason = $"{conferenceBracket.Conference} requires 3 Wild Card winners to generate the Divisional Round.";
                return false;
            }

            var remainingSeeds = new List<PlayoffSeed> { topSeed };
            remainingSeeds.AddRange(wildCardWinners);
            remainingSeeds = remainingSeeds
                .OrderBy(seed => seed.Seed)
                .ToList();

            var divisionalGames = new List<PlayoffGame>
            {
                BuildPlayoffGame(conferenceBracket.Conference, DivisionalRound, remainingSeeds[0], remainingSeeds[3]),
                BuildPlayoffGame(conferenceBracket.Conference, DivisionalRound, remainingSeeds[1], remainingSeeds[2]),
            };

            if (existingDivisionalRound == null)
            {
                conferenceBracket.Rounds.Add(new PlayoffRound
                {
                    Round = DivisionalRound,
                    Status = "scheduled",
                    Games = divisionalGames,
                });
            }
            else
            {
                existingDivisionalRound.Round = DivisionalRound;
                existingDivisionalRound.Status = "scheduled";
                existingDivisionalRound.Games = divisionalGames;
            }

            UpdateRoundStatuses(conferenceBracket);
        }

        reason = "Divisional Round generated.";
        return true;
    }

    public PlayoffRoundSimulationResult SimulateDivisionalRound(LeagueState league)
    {
        if (league == null)
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        if (!EnsureBracketGenerated(league, out var bracketReason))
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = bracketReason,
            };
        }

        if (!EnsureDivisionalRoundGenerated(league, out var divisionalReason))
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = divisionalReason,
            };
        }

        var divisionalGames = (league.PlayoffBracket?.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            .SelectMany(conference => conference?.Rounds ?? new List<PlayoffRound>())
            .Where(round => string.Equals(round?.Round, DivisionalRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new List<PlayoffGame>())
            .Where(game => game != null)
            .OrderBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ToList();
        if (divisionalGames.Count != 4)
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = $"Divisional Round requires 4 games; found {divisionalGames.Count}.",
            };
        }

        var simulatedGames = 0;
        foreach (var game in divisionalGames)
        {
            NormalizePlayoffGame(game);
            if (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(game.WinnerTeamId)
                && game.HomeScore.HasValue
                && game.AwayScore.HasValue)
            {
                EnsureLinkedPlayoffResult(league, game);
                continue;
            }

            var existing = league.Results.FirstOrDefault(result =>
                string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase));
            GameResult result;
            if (existing != null)
            {
                ScheduleService.NormalizeResult(existing);
                result = existing;
            }
            else
            {
                result = GameDayService.SimulateMatchup(
                    league,
                    game.GameId,
                    game.HomeTeamId,
                    game.AwayTeamId,
                    game.AbsoluteWeek,
                    game.PhaseWeek,
                    game.Phase,
                    game.GameType,
                    game.RoundLabel,
                    dayIndex: 6,
                    homeFieldBonus: 3,
                    requireWinner: true);
                league.Results.Add(result);
                simulatedGames++;
            }

            ApplyResultToPlayoffGame(game, result);
        }

        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            UpdateRoundStatuses(conferenceBracket);

        EnsureConferenceChampionshipRoundGenerated(league, out _);

        return new PlayoffRoundSimulationResult
        {
            Ok = true,
            SimulatedGames = simulatedGames,
            AlreadyCompleted = simulatedGames == 0,
        };
    }

    public bool EnsureConferenceChampionshipRoundGenerated(LeagueState league, out string reason)
    {
        reason = "";
        if (league?.PlayoffBracket?.ConferenceBrackets == null || league.PlayoffBracket.ConferenceBrackets.Count == 0)
        {
            reason = "Playoff bracket not generated yet.";
            return false;
        }

        var divisionalGames = league.PlayoffBracket.ConferenceBrackets
            .SelectMany(conference => conference?.Rounds ?? new List<PlayoffRound>())
            .Where(round => string.Equals(round?.Round, DivisionalRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new List<PlayoffGame>())
            .Where(game => game != null)
            .ToList();
        if (divisionalGames.Count != 4 || divisionalGames.Any(game => !string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Conference Championship cannot be generated before all Divisional games are completed.";
            return false;
        }

        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets.Where(entry => entry != null))
        {
            conferenceBracket.Rounds ??= new List<PlayoffRound>();
            var existingConferenceRound = conferenceBracket.Rounds.FirstOrDefault(round =>
                string.Equals(round?.Round, ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase));
            if (existingConferenceRound != null)
            {
                existingConferenceRound.Games ??= new List<PlayoffGame>();
                if (existingConferenceRound.Games.Count == 1)
                {
                    foreach (var game in existingConferenceRound.Games)
                        NormalizePlayoffGame(game);
                    continue;
                }

                if (existingConferenceRound.Games.Any(game => game != null
                        && (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                            || game.HomeScore.HasValue
                            || game.AwayScore.HasValue)))
                {
                    reason = $"{conferenceBracket.Conference} Conference Championship is malformed and cannot be safely regenerated.";
                    return false;
                }
            }

            var seedsByTeamId = (conferenceBracket.Seeds ?? new List<PlayoffSeed>())
                .Where(seed => seed != null)
                .ToDictionary(seed => seed.TeamId, StringComparer.OrdinalIgnoreCase);
            var divisionalWinners = conferenceBracket.Rounds
                .Where(round => string.Equals(round?.Round, DivisionalRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games ?? new List<PlayoffGame>())
                .Where(game => game != null && !string.IsNullOrWhiteSpace(game.WinnerTeamId))
                .Select(game => seedsByTeamId.TryGetValue(game.WinnerTeamId, out var seed) ? seed : null)
                .Where(seed => seed != null)
                .OrderBy(seed => seed.Seed)
                .ToList();
            if (divisionalWinners.Count != 2)
            {
                reason = $"{conferenceBracket.Conference} requires 2 Divisional winners to generate the Conference Championship.";
                return false;
            }

            var conferenceGame = BuildPlayoffGame(conferenceBracket.Conference, ConferenceChampionshipRound, divisionalWinners[0], divisionalWinners[1]);
            if (existingConferenceRound == null)
            {
                conferenceBracket.Rounds.Add(new PlayoffRound
                {
                    Round = ConferenceChampionshipRound,
                    Status = "scheduled",
                    Games = new List<PlayoffGame> { conferenceGame },
                });
            }
            else
            {
                existingConferenceRound.Round = ConferenceChampionshipRound;
                existingConferenceRound.Status = "scheduled";
                existingConferenceRound.Games = new List<PlayoffGame> { conferenceGame };
            }

            UpdateRoundStatuses(conferenceBracket);
        }

        reason = "Conference Championship generated.";
        return true;
    }

    public PlayoffRoundSimulationResult SimulateConferenceChampionshipRound(LeagueState league)
    {
        if (league == null)
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        if (!EnsureBracketGenerated(league, out var bracketReason))
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = bracketReason,
            };
        }

        if (!EnsureConferenceChampionshipRoundGenerated(league, out var conferenceReason))
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = conferenceReason,
            };
        }

        var conferenceChampionshipGames = (league.PlayoffBracket?.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            .SelectMany(conference => conference?.Rounds ?? new List<PlayoffRound>())
            .Where(round => string.Equals(round?.Round, ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new List<PlayoffGame>())
            .Where(game => game != null)
            .OrderBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ToList();
        if (conferenceChampionshipGames.Count != 2)
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = $"Conference Championship requires 2 games; found {conferenceChampionshipGames.Count}.",
            };
        }

        var simulatedGames = 0;
        foreach (var game in conferenceChampionshipGames)
        {
            NormalizePlayoffGame(game);
            if (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(game.WinnerTeamId)
                && game.HomeScore.HasValue
                && game.AwayScore.HasValue)
            {
                EnsureLinkedPlayoffResult(league, game);
                continue;
            }

            var existing = league.Results.FirstOrDefault(result =>
                string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase));
            GameResult result;
            if (existing != null)
            {
                ScheduleService.NormalizeResult(existing);
                result = existing;
            }
            else
            {
                result = GameDayService.SimulateMatchup(
                    league,
                    game.GameId,
                    game.HomeTeamId,
                    game.AwayTeamId,
                    game.AbsoluteWeek,
                    game.PhaseWeek,
                    game.Phase,
                    game.GameType,
                    game.RoundLabel,
                    dayIndex: 6,
                    homeFieldBonus: 3,
                    requireWinner: true);
                league.Results.Add(result);
                simulatedGames++;
            }

            ApplyResultToPlayoffGame(game, result);
        }

        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            UpdateRoundStatuses(conferenceBracket);

        EnsureLeagueChampionshipRoundGenerated(league, out _);

        return new PlayoffRoundSimulationResult
        {
            Ok = true,
            SimulatedGames = simulatedGames,
            AlreadyCompleted = simulatedGames == 0,
        };
    }

    public bool EnsureLeagueChampionshipRoundGenerated(LeagueState league, out string reason)
    {
        reason = "";
        if (league?.PlayoffBracket == null)
        {
            reason = "Playoff bracket not generated yet.";
            return false;
        }

        var conferenceGames = (league.PlayoffBracket.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            .SelectMany(conference => conference?.Rounds ?? new List<PlayoffRound>())
            .Where(round => string.Equals(round?.Round, ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new List<PlayoffGame>())
            .Where(game => game != null)
            .ToList();
        if (conferenceGames.Count != 2 || conferenceGames.Any(game => !string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "League Championship cannot be generated before both Conference Championship games are completed.";
            return false;
        }

        league.PlayoffBracket.LeagueChampionshipRound ??= new PlayoffRound();
        league.PlayoffBracket.LeagueChampionshipRound.Games ??= new List<PlayoffGame>();

        if (league.PlayoffBracket.LeagueChampionshipRound.Games.Count == 1)
        {
            foreach (var game in league.PlayoffBracket.LeagueChampionshipRound.Games)
                NormalizePlayoffGame(game);

            if (string.Equals(league.PlayoffBracket.LeagueChampionshipRound.Status, "completed", StringComparison.OrdinalIgnoreCase))
                EnsureLeagueChampionRecord(league, league.PlayoffBracket.LeagueChampionshipRound.Games[0]);

            reason = "League Championship already exists.";
            return true;
        }

        if (league.PlayoffBracket.LeagueChampionshipRound.Games.Any(game => game != null
                && (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
                    || game.HomeScore.HasValue
                    || game.AwayScore.HasValue)))
        {
            reason = "League Championship is malformed and cannot be safely regenerated.";
            return false;
        }

        var winners = new List<PlayoffSeed>();
        foreach (var conferenceBracket in league.PlayoffBracket.ConferenceBrackets.Where(entry => entry != null))
        {
            var seedsByTeamId = (conferenceBracket.Seeds ?? new List<PlayoffSeed>())
                .Where(seed => seed != null)
                .ToDictionary(seed => seed.TeamId, StringComparer.OrdinalIgnoreCase);
            var conferenceChampion = conferenceBracket.Rounds
                .Where(round => string.Equals(round?.Round, ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
                .SelectMany(round => round.Games ?? new List<PlayoffGame>())
                .Where(game => game != null && !string.IsNullOrWhiteSpace(game.WinnerTeamId))
                .Select(game => seedsByTeamId.TryGetValue(game.WinnerTeamId, out var seed) ? seed : null)
                .FirstOrDefault(seed => seed != null);
            if (conferenceChampion == null)
            {
                reason = $"{conferenceBracket.Conference} is missing a conference champion.";
                return false;
            }

            winners.Add(conferenceChampion);
        }

        if (winners.Count != 2)
        {
            reason = "League Championship requires exactly two conference champions.";
            return false;
        }

        winners = winners
            .OrderBy(seed => seed.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(seed => seed.TeamName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(seed => seed.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var gameRecord = BuildLeagueChampionshipGame(winners[0], winners[1]);
        league.PlayoffBracket.LeagueChampionshipRound.Round = LeagueChampionshipRound;
        league.PlayoffBracket.LeagueChampionshipRound.Status = "scheduled";
        league.PlayoffBracket.LeagueChampionshipRound.Games = new List<PlayoffGame> { gameRecord };
        reason = "League Championship generated.";
        return true;
    }

    public PlayoffRoundSimulationResult SimulateLeagueChampionshipRound(LeagueState league)
    {
        if (league == null)
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        if (!EnsureBracketGenerated(league, out var bracketReason))
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = bracketReason,
            };
        }

        if (!EnsureLeagueChampionshipRoundGenerated(league, out var roundReason))
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = roundReason,
            };
        }

        var round = league.PlayoffBracket.LeagueChampionshipRound ?? new PlayoffRound();
        round.Games ??= new List<PlayoffGame>();
        if (round.Games.Count != 1)
        {
            return new PlayoffRoundSimulationResult
            {
                Ok = false,
                Error = $"League Championship requires 1 game; found {round.Games.Count}.",
            };
        }

        var game = round.Games[0];
        NormalizePlayoffGame(game);
        var simulatedGames = 0;
        if (string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(game.WinnerTeamId)
            && game.HomeScore.HasValue
            && game.AwayScore.HasValue)
        {
            EnsureLinkedPlayoffResult(league, game);
        }
        else
        {
            var existing = league.Results.FirstOrDefault(result =>
                string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase));
            GameResult result;
            if (existing != null)
            {
                ScheduleService.NormalizeResult(existing);
                result = existing;
            }
            else
            {
                result = GameDayService.SimulateMatchup(
                    league,
                    game.GameId,
                    game.HomeTeamId,
                    game.AwayTeamId,
                    game.AbsoluteWeek,
                    game.PhaseWeek,
                    game.Phase,
                    game.GameType,
                    game.RoundLabel,
                    dayIndex: 6,
                    homeFieldBonus: 0,
                    requireWinner: true);
                league.Results.Add(result);
                simulatedGames++;
            }

            ApplyResultToPlayoffGame(game, result);
        }

        round.Status = string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase) ? "completed" : "scheduled";
        EnsureLeagueChampionRecord(league, game);
        MoveLeagueToSeasonComplete(league);

        return new PlayoffRoundSimulationResult
        {
            Ok = true,
            SimulatedGames = simulatedGames,
            AlreadyCompleted = simulatedGames == 0,
        };
    }

    private bool TryBuildBracket(LeagueState league, out PlayoffBracket bracket, out string reason)
    {
        bracket = null;
        reason = "";

        var standings = _standingsService.BuildStandings(league);
        var regularSeasonResults = league.Results
            .Where(ScheduleService.CountsTowardRegularSeasonStandings)
            .GroupBy(result => result.GameId ?? "", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        if (regularSeasonResults.Count != LeagueBootstrapService.RegularSeasonGameCount)
        {
            reason = $"Regular season incomplete: expected {LeagueBootstrapService.RegularSeasonGameCount} final results, found {regularSeasonResults.Count}.";
            return false;
        }

        if (standings.Any(standing => standing.Wins + standing.Losses + standing.Ties != LeagueBootstrapService.RegularSeasonGamesPerTeam))
        {
            reason = $"Regular season incomplete: each team must have {LeagueBootstrapService.RegularSeasonGamesPerTeam} counted regular-season games.";
            return false;
        }

        var teamsByConference = standings
            .GroupBy(standing => standing.Conference ?? "", StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key))
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (teamsByConference.Count != 2)
        {
            reason = $"Playoff bracket generation requires 2 conferences; found {teamsByConference.Count}.";
            return false;
        }

        var conferenceBrackets = new List<PlayoffConferenceBracket>();
        foreach (var conferenceGroup in teamsByConference)
        {
            var conferenceStandings = conferenceGroup.ToList();
            if (conferenceStandings.Count < 7)
            {
                reason = $"{conferenceGroup.Key} does not have enough teams for 7 playoff seeds.";
                return false;
            }

            var divisionGroups = conferenceStandings
                .GroupBy(standing => standing.Division ?? "", StringComparer.OrdinalIgnoreCase)
                .Where(group => !string.IsNullOrWhiteSpace(group.Key))
                .ToList();
            if (divisionGroups.Count != 4)
            {
                reason = $"{conferenceGroup.Key} requires exactly 4 divisions for NFL-style seeding; found {divisionGroups.Count}.";
                return false;
            }

            var divisionWinners = divisionGroups
                .Select(group => RankStandings(group).First())
                .ToList();
            var rankedDivisionWinners = RankStandings(divisionWinners)
                .Take(4)
                .ToList();
            var divisionWinnerIds = new HashSet<string>(rankedDivisionWinners.Select(standing => standing.TeamId), StringComparer.OrdinalIgnoreCase);
            var wildCards = RankStandings(conferenceStandings.Where(standing => !divisionWinnerIds.Contains(standing.TeamId)))
                .Take(3)
                .ToList();
            if (rankedDivisionWinners.Count != 4 || wildCards.Count != 3)
            {
                reason = $"{conferenceGroup.Key} could not produce 4 division winners and 3 wild cards.";
                return false;
            }

            var seeds = new List<PlayoffSeed>();
            for (var index = 0; index < rankedDivisionWinners.Count; index++)
                seeds.Add(ToSeed(rankedDivisionWinners[index], index + 1, isDivisionWinner: true));
            for (var index = 0; index < wildCards.Count; index++)
                seeds.Add(ToSeed(wildCards[index], index + 5, isDivisionWinner: false));

            var wildCardRound = new PlayoffRound
            {
                Round = WildCardRound,
                Status = "scheduled",
                Games = new List<PlayoffGame>
                {
                    BuildPlayoffGame(conferenceGroup.Key, WildCardRound, seeds.First(seed => seed.Seed == 2), seeds.First(seed => seed.Seed == 7)),
                    BuildPlayoffGame(conferenceGroup.Key, WildCardRound, seeds.First(seed => seed.Seed == 3), seeds.First(seed => seed.Seed == 6)),
                    BuildPlayoffGame(conferenceGroup.Key, WildCardRound, seeds.First(seed => seed.Seed == 4), seeds.First(seed => seed.Seed == 5)),
                },
            };

            conferenceBrackets.Add(new PlayoffConferenceBracket
            {
                Conference = conferenceGroup.Key,
                Seeds = seeds.OrderBy(seed => seed.Seed).ToList(),
                Rounds = new List<PlayoffRound> { wildCardRound },
            });
        }

        bracket = new PlayoffBracket
        {
            SeasonYear = league.SeasonYear,
            GeneratedFromAbsoluteWeek = league.Calendar?.AbsoluteWeek ?? 0,
            GeneratedAtPhaseLabel = league.Calendar?.WeekLabel ?? ScheduleService.PostseasonPendingWeekLabel,
            ConferenceBrackets = conferenceBrackets,
            LeagueChampionshipRound = new PlayoffRound(),
            LeagueChampionRecord = new LeagueChampionRecord(),
        };

        return true;
    }

    private static PlayoffGame BuildPlayoffGame(string conference, string round, PlayoffSeed homeSeed, PlayoffSeed awaySeed)
    {
        var phaseWeek = GetPlayoffRoundPhaseWeek(round);
        var roundLabel = BuildRoundLabel(round, phaseWeek);
        return new PlayoffGame
        {
            GameId = BuildPlayoffGameId(conference, round, homeSeed.Seed, awaySeed.Seed),
            Round = round,
            RoundLabel = roundLabel,
            Conference = conference,
            AbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + phaseWeek,
            PhaseWeek = phaseWeek,
            Phase = "Playoffs",
            GameType = "playoffs",
            HomeSeed = homeSeed.Seed,
            AwaySeed = awaySeed.Seed,
            HomeTeamId = homeSeed.TeamId,
            AwayTeamId = awaySeed.TeamId,
            HomeTeamName = homeSeed.TeamName,
            AwayTeamName = awaySeed.TeamName,
            NeutralSite = false,
            Status = "scheduled",
            WinnerTeamId = "",
            LoserTeamId = "",
        };
    }

    private static PlayoffGame BuildLeagueChampionshipGame(PlayoffSeed homeSeed, PlayoffSeed awaySeed)
    {
        return new PlayoffGame
        {
            GameId = $"league_championship_{(homeSeed.TeamId ?? "").Trim().ToLowerInvariant()}_vs_{(awaySeed.TeamId ?? "").Trim().ToLowerInvariant()}",
            Round = LeagueChampionshipRound,
            RoundLabel = LeagueChampionshipRound,
            Conference = "League",
            AbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + 4,
            PhaseWeek = 4,
            Phase = "Playoffs",
            GameType = "playoffs",
            HomeSeed = homeSeed.Seed,
            AwaySeed = awaySeed.Seed,
            HomeTeamId = homeSeed.TeamId,
            AwayTeamId = awaySeed.TeamId,
            HomeTeamName = homeSeed.TeamName,
            AwayTeamName = awaySeed.TeamName,
            NeutralSite = true,
            Status = "scheduled",
            WinnerTeamId = "",
            LoserTeamId = "",
        };
    }

    private static PlayoffSeed ToSeed(TeamStanding standing, int seed, bool isDivisionWinner)
    {
        return new PlayoffSeed
        {
            Seed = seed,
            TeamId = standing.TeamId,
            TeamName = standing.TeamName,
            Conference = standing.Conference,
            Division = standing.Division,
            IsDivisionWinner = isDivisionWinner,
            Wins = standing.Wins,
            Losses = standing.Losses,
            Ties = standing.Ties,
            WinPercentage = standing.WinPct,
            PointDifferential = standing.PointDifferential,
            PointsFor = standing.PointsFor,
        };
    }

    internal static List<TeamStanding> RankStandings(IEnumerable<TeamStanding> standings)
    {
        // TODO: Replace this deterministic placeholder ordering with full NFL tiebreakers.
        return standings
            .Where(standing => standing != null)
            .OrderByDescending(standing => standing.WinPct)
            .ThenByDescending(standing => standing.Wins)
            .ThenByDescending(standing => standing.PointDifferential)
            .ThenByDescending(standing => standing.PointsFor)
            .ThenBy(standing => standing.TeamName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(standing => standing.TeamId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsBracketValid(PlayoffBracket bracket, LeagueState league)
    {
        if (bracket == null || league == null)
            return false;
        if (bracket.SeasonYear != league.SeasonYear)
            return false;
        if (bracket.ConferenceBrackets == null || bracket.ConferenceBrackets.Count != 2)
            return false;

        return bracket.ConferenceBrackets.All(conferenceBracket =>
            conferenceBracket != null
            && !string.IsNullOrWhiteSpace(conferenceBracket.Conference)
            && conferenceBracket.Seeds != null
            && conferenceBracket.Seeds.Count == 7
            && conferenceBracket.Seeds.Count(seed => seed.IsDivisionWinner) == 4
            && conferenceBracket.Seeds.Count(seed => !seed.IsDivisionWinner) == 3
            && conferenceBracket.Seeds.Select(seed => seed.Seed).OrderBy(seed => seed).SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 7 })
            && conferenceBracket.Rounds != null
            && conferenceBracket.Rounds.Count > 0
            && conferenceBracket.Rounds.Any(round =>
                string.Equals(round.Round, WildCardRound, StringComparison.OrdinalIgnoreCase)
                && round.Games != null
                && round.Games.Count == 3));
    }

    private static PlayoffBracketDto ToDto(PlayoffBracket bracket)
    {
        if (bracket == null)
            return new PlayoffBracketDto();

        return new PlayoffBracketDto
        {
            SeasonYear = bracket.SeasonYear,
            GeneratedFromAbsoluteWeek = bracket.GeneratedFromAbsoluteWeek,
            GeneratedAtPhaseLabel = bracket.GeneratedAtPhaseLabel ?? "",
            LeagueChampionshipRound = new PlayoffRoundDto
            {
                Round = bracket.LeagueChampionshipRound?.Round ?? "",
                Status = bracket.LeagueChampionshipRound?.Status ?? "",
                Games = (bracket.LeagueChampionshipRound?.Games ?? new List<PlayoffGame>())
                    .Select(game => new PlayoffGameDto
                    {
                        GameId = game.GameId ?? "",
                        Round = game.Round ?? "",
                        RoundLabel = game.RoundLabel ?? "",
                        Conference = game.Conference ?? "",
                        AbsoluteWeek = game.AbsoluteWeek,
                        PhaseWeek = game.PhaseWeek,
                        Phase = game.Phase ?? "",
                        GameType = game.GameType ?? "",
                        HomeSeed = game.HomeSeed,
                        AwaySeed = game.AwaySeed,
                        HomeTeamId = game.HomeTeamId ?? "",
                        AwayTeamId = game.AwayTeamId ?? "",
                        HomeTeamName = game.HomeTeamName ?? "",
                        AwayTeamName = game.AwayTeamName ?? "",
                        NeutralSite = game.NeutralSite,
                        HomeScore = game.HomeScore,
                        AwayScore = game.AwayScore,
                        Status = game.Status ?? "",
                        WinnerTeamId = game.WinnerTeamId ?? "",
                        LoserTeamId = game.LoserTeamId ?? "",
                    })
                    .ToList(),
            },
            LeagueChampionRecord = new LeagueChampionRecordDto
            {
                SeasonYear = bracket.LeagueChampionRecord?.SeasonYear ?? 0,
                ChampionTeamId = bracket.LeagueChampionRecord?.ChampionTeamId ?? "",
                ChampionTeamName = bracket.LeagueChampionRecord?.ChampionTeamName ?? "",
                RunnerUpTeamId = bracket.LeagueChampionRecord?.RunnerUpTeamId ?? "",
                RunnerUpTeamName = bracket.LeagueChampionRecord?.RunnerUpTeamName ?? "",
                ChampionshipHomeTeamId = bracket.LeagueChampionRecord?.ChampionshipHomeTeamId ?? "",
                ChampionshipAwayTeamId = bracket.LeagueChampionRecord?.ChampionshipAwayTeamId ?? "",
                ChampionScore = bracket.LeagueChampionRecord?.ChampionScore ?? 0,
                RunnerUpScore = bracket.LeagueChampionRecord?.RunnerUpScore ?? 0,
                CompletedPhaseLabel = bracket.LeagueChampionRecord?.CompletedPhaseLabel ?? "",
            },
            ConferenceBrackets = (bracket.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
                .Select(conferenceBracket => new PlayoffConferenceBracketDto
                {
                    Conference = conferenceBracket.Conference ?? "",
                    Seeds = (conferenceBracket.Seeds ?? new List<PlayoffSeed>())
                        .OrderBy(seed => seed.Seed)
                        .Select(seed => new PlayoffSeedDto
                        {
                            Seed = seed.Seed,
                            TeamId = seed.TeamId ?? "",
                            TeamName = seed.TeamName ?? "",
                            Conference = seed.Conference ?? "",
                            Division = seed.Division ?? "",
                            IsDivisionWinner = seed.IsDivisionWinner,
                            Wins = seed.Wins,
                            Losses = seed.Losses,
                            Ties = seed.Ties,
                            WinPercentage = seed.WinPercentage,
                            PointDifferential = seed.PointDifferential,
                            PointsFor = seed.PointsFor,
                        })
                        .ToList(),
                    Rounds = (conferenceBracket.Rounds ?? new List<PlayoffRound>())
                        .Select(round => new PlayoffRoundDto
                        {
                            Round = round.Round ?? "",
                            Status = round.Status ?? "",
                            Games = (round.Games ?? new List<PlayoffGame>())
                                .Select(game => new PlayoffGameDto
                                {
                                    GameId = game.GameId ?? "",
                                    Round = game.Round ?? "",
                                    RoundLabel = game.RoundLabel ?? "",
                                    Conference = game.Conference ?? "",
                                    AbsoluteWeek = game.AbsoluteWeek,
                                    PhaseWeek = game.PhaseWeek,
                                    Phase = game.Phase ?? "",
                                    GameType = game.GameType ?? "",
                                    HomeSeed = game.HomeSeed,
                                    AwaySeed = game.AwaySeed,
                                    HomeTeamId = game.HomeTeamId ?? "",
                                    AwayTeamId = game.AwayTeamId ?? "",
                                    HomeTeamName = game.HomeTeamName ?? "",
                                    AwayTeamName = game.AwayTeamName ?? "",
                                    NeutralSite = game.NeutralSite,
                                    HomeScore = game.HomeScore,
                                    AwayScore = game.AwayScore,
                                    Status = game.Status ?? "",
                                    WinnerTeamId = game.WinnerTeamId ?? "",
                                    LoserTeamId = game.LoserTeamId ?? "",
                                })
                                .ToList(),
                        })
                        .ToList(),
                })
                .ToList(),
        };
    }

    private static string BuildPlayoffGameId(string conference, string round, int homeSeed, int awaySeed)
        => $"{(conference ?? "").Trim().ToLowerInvariant().Replace(' ', '_')}_{(round ?? "").Trim().ToLowerInvariant().Replace(' ', '_')}_{homeSeed}v{awaySeed}";

    internal static void NormalizePlayoffGame(PlayoffGame game)
    {
        if (game == null)
            return;

        game.GameId = string.IsNullOrWhiteSpace(game.GameId)
            ? BuildPlayoffGameId(game.Conference, game.Round, game.HomeSeed, game.AwaySeed)
            : game.GameId;
        game.Round ??= "";
        var phaseWeek = game.PhaseWeek > 0 ? game.PhaseWeek : GetPlayoffRoundPhaseWeek(game.Round);
        game.RoundLabel = string.IsNullOrWhiteSpace(game.RoundLabel)
            ? BuildRoundLabel(game.Round, phaseWeek)
            : game.RoundLabel;
        game.Conference ??= "";
        game.AbsoluteWeek = game.AbsoluteWeek > 0 ? game.AbsoluteWeek : LeagueBootstrapService.TotalSeasonWeeks + phaseWeek;
        game.PhaseWeek = phaseWeek;
        game.Phase = string.IsNullOrWhiteSpace(game.Phase) ? "Playoffs" : game.Phase;
        game.GameType = string.IsNullOrWhiteSpace(game.GameType) ? "playoffs" : ScheduleService.NormalizeGameType(game.GameType, game.AbsoluteWeek);
        game.HomeTeamId ??= "";
        game.AwayTeamId ??= "";
        game.HomeTeamName ??= "";
        game.AwayTeamName ??= "";
        game.Status = string.IsNullOrWhiteSpace(game.Status) ? "scheduled" : game.Status;
        game.WinnerTeamId ??= "";
        game.LoserTeamId ??= "";
    }

    private static void ApplyResultToPlayoffGame(PlayoffGame game, GameResult result)
    {
        if (game == null || result == null)
            return;

        game.HomeScore = result.HomeScore;
        game.AwayScore = result.AwayScore;
        game.Status = "completed";
        game.WinnerTeamId = ResolveWinnerTeamId(game, result);
        game.LoserTeamId = string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase)
            ? game.AwayTeamId
            : game.HomeTeamId;
    }

    private static string ResolveWinnerTeamId(PlayoffGame game, GameResult result)
    {
        if (game == null || result == null)
            return "";

        return result.HomeScore > result.AwayScore ? game.HomeTeamId : game.AwayTeamId;
    }

    private static string ResolveWinnerLabel(PlayoffGameDto game)
    {
        if (game == null)
            return "Winner TBD";

        if (string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase))
            return $"{game.HomeTeamName} advance";
        if (string.Equals(game.WinnerTeamId, game.AwayTeamId, StringComparison.OrdinalIgnoreCase))
            return $"{game.AwayTeamName} advance";
        return "Winner TBD";
    }

    private static void EnsureLinkedPlayoffResult(LeagueState league, PlayoffGame game)
    {
        if (league == null || game == null || string.IsNullOrWhiteSpace(game.GameId) || !game.HomeScore.HasValue || !game.AwayScore.HasValue)
            return;

        var existing = league.Results.FirstOrDefault(result =>
            string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return;

        var winnerIsHome = string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase);
        var winnerName = winnerIsHome ? game.HomeTeamName : game.AwayTeamName;
        var loserName = winnerIsHome ? game.AwayTeamName : game.HomeTeamName;
        var result = GameDayService.SimulateMatchup(
            league,
            game.GameId,
            game.HomeTeamId,
            game.AwayTeamId,
            game.AbsoluteWeek,
            game.PhaseWeek,
            game.Phase,
            game.GameType,
            game.RoundLabel,
            dayIndex: 6,
            homeFieldBonus: game.NeutralSite ? 0 : 3,
            requireWinner: true);
        result.HomeScore = game.HomeScore.Value;
        result.AwayScore = game.AwayScore.Value;
        result.Winner = winnerName;
        result.Summary = $"{winnerName} defeated {loserName}, {result.HomeScore}-{result.AwayScore}.";
        result.BoxScore = result.BoxScore ?? new BoxScoreState();
        league.Results.Add(result);
    }

    private static void UpdateRoundStatuses(PlayoffConferenceBracket conferenceBracket)
    {
        if (conferenceBracket?.Rounds == null)
            return;

        foreach (var round in conferenceBracket.Rounds)
        {
            if (round == null)
                continue;

            round.Games ??= new List<PlayoffGame>();
            round.Status = round.Games.Count > 0 && round.Games.All(game =>
                    game != null && string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase))
                ? "completed"
                : "scheduled";
        }
    }

    private static int GetRoundOrder(string round)
    {
        return NormalizeRound(round) switch
        {
            "wild card" => 1,
            "divisional" => 2,
            "conference championship" => 3,
            "league championship" => 4,
            _ => 99,
        };
    }

    private static int GetPlayoffRoundPhaseWeek(string round)
    {
        return NormalizeRound(round) switch
        {
            "divisional" => 2,
            "conference championship" => 3,
            "league championship" => 4,
            _ => 1,
        };
    }

    private static string BuildRoundLabel(string round, int phaseWeek)
    {
        return NormalizeRound(round) switch
        {
            "divisional" => "Divisional Round",
            "conference championship" => "Conference Championship",
            "league championship" => LeagueChampionshipRound,
            _ => ScheduleService.BuildPlayoffRoundLabel(phaseWeek),
        };
    }

    private static string BuildRoundHeading(string round, string status)
    {
        var isCompleted = string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase);
        return NormalizeRound(round) switch
        {
            "divisional" => isCompleted ? "Divisional Round Results" : "Divisional Round",
            "conference championship" => isCompleted ? "Conference Championship Results" : "Conference Championship",
            "league championship" => isCompleted ? "League Championship Results" : LeagueChampionshipRound,
            "wild card" => isCompleted ? "Wild Card Results" : "Wild Card",
            _ => isCompleted ? $"{round} Results" : round,
        };
    }

    private static string NormalizeBracketTeamName(string value)
        => string.IsNullOrWhiteSpace(value) ? "TBD" : value.Trim();

    private static void EnsureLeagueChampionRecord(LeagueState league, PlayoffGame game)
    {
        if (league?.PlayoffBracket == null || game == null || !game.HomeScore.HasValue || !game.AwayScore.HasValue || string.IsNullOrWhiteSpace(game.WinnerTeamId))
            return;

        league.PlayoffBracket.LeagueChampionRecord ??= new LeagueChampionRecord();
        var winnerIsHome = string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase);
        league.PlayoffBracket.LeagueChampionRecord.SeasonYear = league.SeasonYear;
        league.PlayoffBracket.LeagueChampionRecord.ChampionTeamId = winnerIsHome ? game.HomeTeamId : game.AwayTeamId;
        league.PlayoffBracket.LeagueChampionRecord.ChampionTeamName = winnerIsHome ? game.HomeTeamName : game.AwayTeamName;
        league.PlayoffBracket.LeagueChampionRecord.RunnerUpTeamId = winnerIsHome ? game.AwayTeamId : game.HomeTeamId;
        league.PlayoffBracket.LeagueChampionRecord.RunnerUpTeamName = winnerIsHome ? game.AwayTeamName : game.HomeTeamName;
        league.PlayoffBracket.LeagueChampionRecord.ChampionshipHomeTeamId = game.HomeTeamId;
        league.PlayoffBracket.LeagueChampionRecord.ChampionshipAwayTeamId = game.AwayTeamId;
        league.PlayoffBracket.LeagueChampionRecord.ChampionScore = winnerIsHome ? game.HomeScore.Value : game.AwayScore.Value;
        league.PlayoffBracket.LeagueChampionRecord.RunnerUpScore = winnerIsHome ? game.AwayScore.Value : game.HomeScore.Value;
        league.PlayoffBracket.LeagueChampionRecord.CompletedPhaseLabel = game.RoundLabel ?? LeagueChampionshipRound;
    }

    private static void MoveLeagueToSeasonComplete(LeagueState league)
    {
        if (league?.Calendar == null)
            return;

        league.Calendar.AbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + 2;
        league.Calendar.Week = league.Calendar.AbsoluteWeek;
        league.Calendar.DayIndex = 0;
        ScheduleService.NormalizeCalendar(league.Calendar);
    }

    private static string NormalizeRound(string round)
        => string.IsNullOrWhiteSpace(round) ? "" : round.Trim().ToLowerInvariant();

    public class PlayoffRoundSimulationResult
    {
        public bool Ok { get; set; }
        public int SimulatedGames { get; set; }
        public bool AlreadyCompleted { get; set; }
        public string Error { get; set; } = "";
    }

    public sealed class WildCardSimulationResult : PlayoffRoundSimulationResult
    {
    }
}
