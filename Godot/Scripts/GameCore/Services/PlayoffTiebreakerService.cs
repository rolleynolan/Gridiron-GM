using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

/// <summary>
/// Applies the NFL postseason tiebreak sequence to persisted regular-season results.
/// A stable season/team draw replaces the real league's final coin toss so save/reload is deterministic.
/// </summary>
public sealed class PlayoffTiebreakerService
{
    private const double Epsilon = 0.0000001d;
    private readonly LeagueState _league;
    private readonly List<GameResult> _results;
    private readonly Dictionary<string, TeamStanding> _standings;

    public PlayoffTiebreakerService(LeagueState league, IEnumerable<TeamStanding> standings)
    {
        _league = league ?? throw new ArgumentNullException(nameof(league));
        _results = (league.Results ?? new List<GameResult>())
            .Where(ScheduleService.CountsTowardRegularSeasonStandings)
            .GroupBy(result => result.GameId ?? "", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        _standings = (standings ?? Enumerable.Empty<TeamStanding>())
            .Where(standing => standing != null && !string.IsNullOrWhiteSpace(standing.TeamId))
            .ToDictionary(standing => standing.TeamId, StringComparer.OrdinalIgnoreCase);
    }

    public List<TeamStanding> RankDivision(IEnumerable<TeamStanding> standings)
        => RankByRecord(standings, divisionTie: true, reduceDivisions: false);

    public List<TeamStanding> RankConference(IEnumerable<TeamStanding> standings)
        => RankByRecord(standings, divisionTie: false, reduceDivisions: false);

    public List<TeamStanding> RankWildCards(IEnumerable<TeamStanding> standings)
        => RankByRecord(standings, divisionTie: false, reduceDivisions: true);

    private List<TeamStanding> RankByRecord(IEnumerable<TeamStanding> source, bool divisionTie, bool reduceDivisions)
    {
        var remaining = (source ?? Enumerable.Empty<TeamStanding>())
            .Where(standing => standing != null)
            .ToList();
        var ranked = new List<TeamStanding>(remaining.Count);

        while (remaining.Count > 0)
        {
            var bestRecord = remaining.Max(standing => standing.WinPct);
            var recordGroup = remaining.Where(standing => Math.Abs(standing.WinPct - bestRecord) < Epsilon).ToList();
            TeamStanding winner;

            if (recordGroup.Count == 1)
            {
                winner = recordGroup[0];
            }
            else if (reduceDivisions)
            {
                var finalists = recordGroup
                    .GroupBy(standing => standing.Division ?? "", StringComparer.OrdinalIgnoreCase)
                    .Select(group => RankDivision(group).First())
                    .ToList();
                winner = finalists.Count == 1 ? finalists[0] : ResolveConferenceWinner(finalists);
            }
            else
            {
                winner = divisionTie ? ResolveDivisionWinner(recordGroup) : ResolveConferenceWinner(recordGroup);
            }

            ranked.Add(winner);
            remaining.Remove(winner);
        }

        return ranked;
    }

    private TeamStanding ResolveDivisionWinner(List<TeamStanding> tied)
    {
        var criteria = new List<Func<List<TeamStanding>, Dictionary<string, double?>>>
        {
            contenders => HeadToHead(contenders, requireSweepForMultiTeam: false),
            contenders => RecordAgainst(contenders, result => IsDivisionGame(result, contenders[0].Division)),
            CommonGames,
            contenders => RecordAgainst(contenders, result => IsConferenceGame(result, contenders[0].Conference)),
            StrengthOfVictory,
            StrengthOfSchedule,
            contenders => CombinedScoringRank(contenders, conferenceOnly: true),
            contenders => CombinedScoringRank(contenders, conferenceOnly: false),
            CommonGameNetPoints,
            AllGameNetPoints,
            NetTouchdowns,
        };
        return Resolve(tied, criteria, divisionTie: true);
    }

    private TeamStanding ResolveConferenceWinner(List<TeamStanding> tied)
    {
        if (tied.Select(team => team.Division).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            return ResolveDivisionWinner(tied);

        var criteria = new List<Func<List<TeamStanding>, Dictionary<string, double?>>>
        {
            contenders => HeadToHead(contenders, requireSweepForMultiTeam: true),
            contenders => RecordAgainst(contenders, result => IsConferenceGame(result, contenders[0].Conference)),
            CommonGamesWithFourGameMinimum,
            StrengthOfVictory,
            StrengthOfSchedule,
            contenders => CombinedScoringRank(contenders, conferenceOnly: true),
            contenders => CombinedScoringRank(contenders, conferenceOnly: false),
            ConferenceGameNetPoints,
            AllGameNetPoints,
            NetTouchdowns,
        };
        return Resolve(tied, criteria, divisionTie: false);
    }

    private TeamStanding Resolve(
        List<TeamStanding> original,
        IReadOnlyList<Func<List<TeamStanding>, Dictionary<string, double?>>> criteria,
        bool divisionTie)
    {
        var contenders = original.ToList();
        foreach (var criterion in criteria)
        {
            var narrowed = Best(contenders, criterion(contenders));
            if (narrowed.Count == 0 || narrowed.Count == contenders.Count)
                continue;
            contenders = narrowed;
            if (contenders.Count == 1)
                return contenders[0];
            if (contenders.Count == 2 && original.Count > 2)
                return divisionTie ? ResolveDivisionWinner(contenders) : ResolveConferenceWinner(contenders);
        }

        return contenders
            .OrderBy(team => StableDraw(team.TeamId))
            .ThenBy(team => team.TeamId, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static List<TeamStanding> Best(List<TeamStanding> contenders, IReadOnlyDictionary<string, double?> values)
    {
        if (contenders.Any(team => !values.TryGetValue(team.TeamId, out var value) || !value.HasValue))
            return contenders;

        var best = contenders.Max(team => values[team.TeamId]!.Value);
        return contenders.Where(team => Math.Abs(values[team.TeamId]!.Value - best) < Epsilon).ToList();
    }

    private Dictionary<string, double?> HeadToHead(List<TeamStanding> contenders, bool requireSweepForMultiTeam)
    {
        var ids = contenders.Select(team => team.TeamId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var values = contenders.ToDictionary(team => team.TeamId, _ => (double?)null, StringComparer.OrdinalIgnoreCase);
        foreach (var team in contenders)
        {
            var games = GamesFor(team.TeamId).Where(result => ids.Contains(OpponentId(result, team.TeamId))).ToList();
            if (games.Count == 0)
                continue;

            if (requireSweepForMultiTeam && contenders.Count > 2)
            {
                var opponentsPlayed = games.Select(result => OpponentId(result, team.TeamId)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                if (opponentsPlayed != contenders.Count - 1)
                    continue;
                var record = Record(team.TeamId, games);
                if (record.Wins != games.Count && record.Losses != games.Count)
                    continue;
            }

            values[team.TeamId] = Percentage(Record(team.TeamId, games));
        }
        return values;
    }

    private Dictionary<string, double?> RecordAgainst(List<TeamStanding> contenders, Func<GameResult, bool> predicate)
        => contenders.ToDictionary(
            team => team.TeamId,
            team => PercentageOrNull(Record(team.TeamId, GamesFor(team.TeamId).Where(predicate))),
            StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, double?> CommonGames(List<TeamStanding> contenders)
        => CommonGamesCore(contenders, minimumGames: 1);

    private Dictionary<string, double?> CommonGamesWithFourGameMinimum(List<TeamStanding> contenders)
        => CommonGamesCore(contenders, minimumGames: 4);

    private Dictionary<string, double?> CommonGamesCore(List<TeamStanding> contenders, int minimumGames)
    {
        HashSet<string> commonOpponents = null;
        foreach (var team in contenders)
        {
            var opponents = GamesFor(team.TeamId)
                .Select(result => OpponentId(result, team.TeamId))
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            commonOpponents = commonOpponents == null ? opponents : new HashSet<string>(commonOpponents.Intersect(opponents, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }

        commonOpponents ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return contenders.ToDictionary(
            team => team.TeamId,
            team =>
            {
                var games = GamesFor(team.TeamId).Where(result => commonOpponents.Contains(OpponentId(result, team.TeamId))).ToList();
                return games.Count >= minimumGames ? Percentage(Record(team.TeamId, games)) : (double?)null;
            },
            StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, double?> StrengthOfVictory(List<TeamStanding> contenders)
        => contenders.ToDictionary(
            team => team.TeamId,
            team => OpponentStrength(team.TeamId, GamesFor(team.TeamId).Where(result => Won(team.TeamId, result))),
            StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, double?> StrengthOfSchedule(List<TeamStanding> contenders)
        => contenders.ToDictionary(
            team => team.TeamId,
            team => OpponentStrength(team.TeamId, GamesFor(team.TeamId)),
            StringComparer.OrdinalIgnoreCase);

    private double? OpponentStrength(string teamId, IEnumerable<GameResult> games)
    {
        var wins = 0;
        var losses = 0;
        var ties = 0;
        var found = false;
        foreach (var game in games)
        {
            if (!_standings.TryGetValue(OpponentId(game, teamId), out var opponent))
                continue;
            found = true;
            wins += opponent.Wins;
            losses += opponent.Losses;
            ties += opponent.Ties;
        }
        return found ? Percentage((wins, losses, ties)) : null;
    }

    private Dictionary<string, double?> CombinedScoringRank(List<TeamStanding> contenders, bool conferenceOnly)
    {
        var scope = _standings.Values
            .Where(team => !conferenceOnly || string.Equals(team.Conference, contenders[0].Conference, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var pointsForRanks = CompetitionRanks(scope, team => team.PointsFor, descending: true);
        var pointsAllowedRanks = CompetitionRanks(scope, team => team.PointsAgainst, descending: false);
        return contenders.ToDictionary(
            team => team.TeamId,
            team => (double?)-(pointsForRanks[team.TeamId] + pointsAllowedRanks[team.TeamId]),
            StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, int> CompetitionRanks(List<TeamStanding> teams, Func<TeamStanding, int> selector, bool descending)
    {
        var ordered = descending
            ? teams.OrderByDescending(selector).ToList()
            : teams.OrderBy(selector).ToList();
        var ranks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < ordered.Count; index++)
            ranks[ordered[index].TeamId] = index == 0 || selector(ordered[index]) != selector(ordered[index - 1]) ? index + 1 : ranks[ordered[index - 1].TeamId];
        return ranks;
    }

    private Dictionary<string, double?> CommonGameNetPoints(List<TeamStanding> contenders)
    {
        HashSet<string> commonOpponents = null;
        foreach (var team in contenders)
        {
            var opponents = GamesFor(team.TeamId).Select(result => OpponentId(result, team.TeamId)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            commonOpponents = commonOpponents == null ? opponents : new HashSet<string>(commonOpponents.Intersect(opponents, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
        }
        commonOpponents ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return contenders.ToDictionary(
            team => team.TeamId,
            team => (double?)NetPoints(team.TeamId, GamesFor(team.TeamId).Where(result => commonOpponents.Contains(OpponentId(result, team.TeamId)))),
            StringComparer.OrdinalIgnoreCase);
    }

    private Dictionary<string, double?> ConferenceGameNetPoints(List<TeamStanding> contenders)
        => contenders.ToDictionary(
            team => team.TeamId,
            team => (double?)NetPoints(team.TeamId, GamesFor(team.TeamId).Where(result => IsConferenceGame(result, team.Conference))),
            StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, double?> AllGameNetPoints(List<TeamStanding> contenders)
        => contenders.ToDictionary(team => team.TeamId, team => (double?)team.PointDifferential, StringComparer.OrdinalIgnoreCase);

    private Dictionary<string, double?> NetTouchdowns(List<TeamStanding> contenders)
        => contenders.ToDictionary(
            team => team.TeamId,
            team => TouchdownDifferential(team.TeamId),
            StringComparer.OrdinalIgnoreCase);

    private double? TouchdownDifferential(string teamId)
    {
        var total = 0;
        var found = false;
        foreach (var result in GamesFor(teamId))
        {
            var stats = result.BoxScore?.TeamStats;
            if (stats == null)
                return null;
            var home = Same(result.HomeTeamId, teamId);
            var ownKey = home ? "touchdowns_home" : "touchdowns_away";
            var opponentKey = home ? "touchdowns_away" : "touchdowns_home";
            if (!stats.TryGetValue(ownKey, out var own) || !stats.TryGetValue(opponentKey, out var opponent))
                return null;
            found = true;
            total += own - opponent;
        }
        return found ? total : null;
    }

    private IEnumerable<GameResult> GamesFor(string teamId)
        => _results.Where(result => Same(result.HomeTeamId, teamId) || Same(result.AwayTeamId, teamId));

    private bool IsDivisionGame(GameResult result, string division)
        => string.Equals(Team(result.HomeTeamId)?.Division, division, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Team(result.AwayTeamId)?.Division, division, StringComparison.OrdinalIgnoreCase);

    private bool IsConferenceGame(GameResult result, string conference)
        => string.Equals(Team(result.HomeTeamId)?.Conference, conference, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Team(result.AwayTeamId)?.Conference, conference, StringComparison.OrdinalIgnoreCase);

    private TeamState Team(string id)
        => _league.Teams?.FirstOrDefault(team => Same(team?.TeamId, id));

    private static (int Wins, int Losses, int Ties) Record(string teamId, IEnumerable<GameResult> games)
    {
        var wins = 0;
        var losses = 0;
        var ties = 0;
        foreach (var result in games)
        {
            var own = Same(result.HomeTeamId, teamId) ? result.HomeScore : result.AwayScore;
            var opponent = Same(result.HomeTeamId, teamId) ? result.AwayScore : result.HomeScore;
            if (own > opponent) wins++; else if (own < opponent) losses++; else ties++;
        }
        return (wins, losses, ties);
    }

    private static double? PercentageOrNull((int Wins, int Losses, int Ties) record)
        => record.Wins + record.Losses + record.Ties == 0 ? null : Percentage(record);

    private static double Percentage((int Wins, int Losses, int Ties) record)
    {
        var games = record.Wins + record.Losses + record.Ties;
        return games == 0 ? 0d : (record.Wins + record.Ties * 0.5d) / games;
    }

    private static bool Won(string teamId, GameResult result)
        => Same(result.HomeTeamId, teamId) ? result.HomeScore > result.AwayScore : result.AwayScore > result.HomeScore;

    private static string OpponentId(GameResult result, string teamId)
        => Same(result.HomeTeamId, teamId) ? result.AwayTeamId : result.HomeTeamId;

    private static int NetPoints(string teamId, IEnumerable<GameResult> games)
        => games.Sum(result => Same(result.HomeTeamId, teamId)
            ? result.HomeScore - result.AwayScore
            : result.AwayScore - result.HomeScore);

    private ulong StableDraw(string teamId)
    {
        var hash = 1469598103934665603UL ^ (uint)_league.SeasonYear;
        foreach (var character in teamId ?? "")
        {
            hash ^= character;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    private static bool Same(string left, string right)
        => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
