using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class SeasonHistoryService
{
    private readonly GameCoreContext _context;
    private readonly StandingsService _standingsService;

    public SeasonHistoryService(GameCoreContext context)
    {
        _context = context;
        _standingsService = new StandingsService(context);
    }

    public bool EnsureSeasonHistorySnapshot(LeagueState league, out string reason)
    {
        reason = "";
        if (league == null)
        {
            reason = "No active league loaded.";
            return false;
        }

        league.HistoricalSeasons ??= new List<SeasonHistoryRecord>();
        if (league.HistoricalSeasons.Any(record => record != null && record.SeasonYear == league.SeasonYear))
        {
            reason = "Season history snapshot already exists.";
            return true;
        }

        ScheduleService.NormalizeCalendar(league.Calendar);
        if (!IsSeasonArchivePhase(league.Calendar?.Phase))
        {
            reason = $"Season history snapshot generation skipped outside {ScheduleService.SeasonCompletePhase} or offseason placeholder phases.";
            return false;
        }

        var championRecord = league.PlayoffBracket?.LeagueChampionRecord;
        if (championRecord == null
            || string.IsNullOrWhiteSpace(championRecord.ChampionTeamId)
            || string.IsNullOrWhiteSpace(championRecord.RunnerUpTeamId)
            || string.IsNullOrWhiteSpace(championRecord.ChampionTeamName)
            || string.IsNullOrWhiteSpace(championRecord.RunnerUpTeamName))
        {
            reason = "Season history snapshot skipped because the league champion record is missing or incomplete.";
            return false;
        }

        var championshipGame = league.PlayoffBracket?.LeagueChampionshipRound?.Games?
            .FirstOrDefault(IsCompletedPlayoffGame);
        if (championshipGame == null)
        {
            reason = "Season history snapshot skipped because the completed League Championship game could not be found.";
            return false;
        }

        var standings = _standingsService.BuildStandings(league);
        var playoffSeeds = BuildPlayoffSeeds(league.PlayoffBracket);
        var playoffResults = BuildPlayoffResults(league.PlayoffBracket);
        var snapshot = new SeasonHistoryRecord
        {
            SeasonYear = league.SeasonYear,
            CompletedPhaseLabel = string.IsNullOrWhiteSpace(championRecord.CompletedPhaseLabel)
                ? ScheduleService.SeasonCompletePhase
                : championRecord.CompletedPhaseLabel,
            ChampionTeamId = championRecord.ChampionTeamId,
            ChampionTeamName = championRecord.ChampionTeamName,
            RunnerUpTeamId = championRecord.RunnerUpTeamId,
            RunnerUpTeamName = championRecord.RunnerUpTeamName,
            ChampionshipWinnerScore = championRecord.ChampionScore,
            ChampionshipRunnerUpScore = championRecord.RunnerUpScore,
            ChampionshipGameLabel = string.IsNullOrWhiteSpace(championshipGame.RoundLabel)
                ? PlayoffService.LeagueChampionshipRound
                : championshipGame.RoundLabel,
            TeamRecords = standings
                .OrderBy(row => row.Conference, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.Division, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(row => row.WinPct)
                .ThenByDescending(row => row.PointDifferential)
                .ThenBy(row => row.TeamName, StringComparer.OrdinalIgnoreCase)
                .Select(row => new SeasonTeamRecord
                {
                    TeamId = row.TeamId,
                    TeamName = row.TeamName,
                    Abbreviation = row.Abbreviation,
                    Conference = row.Conference,
                    Division = row.Division,
                    Wins = row.Wins,
                    Losses = row.Losses,
                    Ties = row.Ties,
                    PointsFor = row.PointsFor,
                    PointsAgainst = row.PointsAgainst,
                    WinPercentage = row.WinPct,
                })
                .ToList(),
            PlayoffSeeds = playoffSeeds,
            PlayoffResults = playoffResults,
            TotalRegularSeasonGames = CountRegularSeasonGames(league.Results),
            TotalPlayoffGames = playoffResults.Count,
            GeneratedAtLabel = BuildGeneratedAtLabel(league),
        };

        league.HistoricalSeasons.Add(snapshot);
        reason = "Season history snapshot generated.";
        return true;
    }

    private static bool IsSeasonArchivePhase(string phase)
        => ScheduleService.IsSeasonArchivePhase(phase);

    public SeasonHistoryRecord GetLatestSeasonRecord(LeagueState league)
    {
        if (league?.HistoricalSeasons == null || league.HistoricalSeasons.Count == 0)
            return null;

        return league.HistoricalSeasons
            .Where(record => record != null)
            .OrderByDescending(record => record.SeasonYear)
            .ThenByDescending(record => record.GeneratedAtLabel, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static List<SeasonPlayoffSeedRecord> BuildPlayoffSeeds(PlayoffBracket bracket)
    {
        return (bracket?.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
            .Where(entry => entry != null)
            .OrderBy(entry => entry.Conference, StringComparer.OrdinalIgnoreCase)
            .SelectMany(entry => (entry.Seeds ?? new List<PlayoffSeed>())
                .Where(seed => seed != null)
                .OrderBy(seed => seed.Seed)
                .Select(seed => new SeasonPlayoffSeedRecord
                {
                    Conference = entry.Conference ?? "",
                    Seed = seed.Seed,
                    TeamId = seed.TeamId ?? "",
                    TeamName = seed.TeamName ?? "",
                    Division = seed.Division ?? "",
                    IsDivisionWinner = seed.IsDivisionWinner,
                }))
            .ToList();
    }

    private static List<SeasonPlayoffResultRecord> BuildPlayoffResults(PlayoffBracket bracket)
    {
        var games = new List<PlayoffGame>();
        foreach (var conferenceBracket in bracket?.ConferenceBrackets ?? new List<PlayoffConferenceBracket>())
        {
            foreach (var round in conferenceBracket?.Rounds ?? new List<PlayoffRound>())
            {
                games.AddRange((round?.Games ?? new List<PlayoffGame>())
                    .Where(IsCompletedPlayoffGame));
            }
        }

        games.AddRange((bracket?.LeagueChampionshipRound?.Games ?? new List<PlayoffGame>())
            .Where(IsCompletedPlayoffGame));

        return games
            .GroupBy(game => game.GameId ?? "", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(game => GetRoundOrder(game.Round))
            .ThenBy(game => game.Conference, StringComparer.OrdinalIgnoreCase)
            .ThenBy(game => game.HomeSeed)
            .ThenBy(game => game.AwaySeed)
            .ThenBy(game => game.HomeTeamName, StringComparer.OrdinalIgnoreCase)
            .Select(game =>
            {
                var winnerIsHome = string.Equals(game.WinnerTeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase);
                return new SeasonPlayoffResultRecord
                {
                    Round = string.IsNullOrWhiteSpace(game.Round) ? game.RoundLabel ?? "" : game.Round,
                    Conference = game.Conference ?? "",
                    HomeTeamId = game.HomeTeamId ?? "",
                    HomeTeamName = game.HomeTeamName ?? "",
                    AwayTeamId = game.AwayTeamId ?? "",
                    AwayTeamName = game.AwayTeamName ?? "",
                    HomeScore = game.HomeScore ?? 0,
                    AwayScore = game.AwayScore ?? 0,
                    WinnerTeamId = game.WinnerTeamId ?? "",
                    WinnerTeamName = winnerIsHome ? game.HomeTeamName ?? "" : game.AwayTeamName ?? "",
                    LoserTeamId = game.LoserTeamId ?? "",
                    LoserTeamName = winnerIsHome ? game.AwayTeamName ?? "" : game.HomeTeamName ?? "",
                };
            })
            .ToList();
    }

    private static int CountRegularSeasonGames(IEnumerable<GameResult> results)
    {
        return (results ?? Array.Empty<GameResult>())
            .Where(ScheduleService.CountsTowardRegularSeasonStandings)
            .GroupBy(result => result.GameId ?? "", StringComparer.OrdinalIgnoreCase)
            .Count();
    }

    private static string BuildGeneratedAtLabel(LeagueState league)
    {
        var currentDate = league?.Calendar?.CurrentDate ?? "";
        var weekLabel = league?.Calendar?.WeekLabel ?? "";
        if (!string.IsNullOrWhiteSpace(currentDate) && !string.IsNullOrWhiteSpace(weekLabel))
            return $"{currentDate} - {weekLabel}";
        if (!string.IsNullOrWhiteSpace(currentDate))
            return currentDate;
        if (!string.IsNullOrWhiteSpace(weekLabel))
            return weekLabel;
        return ScheduleService.SeasonCompleteWeekLabel;
    }

    private static bool IsCompletedPlayoffGame(PlayoffGame game)
    {
        if (game == null)
            return false;

        PlayoffService.NormalizePlayoffGame(game);
        return string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase)
            && game.HomeScore.HasValue
            && game.AwayScore.HasValue
            && !string.IsNullOrWhiteSpace(game.WinnerTeamId)
            && !string.IsNullOrWhiteSpace(game.LoserTeamId);
    }

    private static int GetRoundOrder(string round)
    {
        return (round ?? "").Trim().ToLowerInvariant() switch
        {
            "wild card" => 1,
            "divisional" => 2,
            "conference championship" => 3,
            "league championship" => 4,
            _ => 9,
        };
    }
}
