using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Read-only projection over persisted player and season history. It owns no simulation data.
public sealed class RecordBookService
{
    private readonly GameCoreContext _context;
    public RecordBookService(GameCoreContext context) => _context = context;

    public RecordBookResponse GetRecordBook()
    {
        var league = _context?.ActiveLeague;
        if (league == null) return new RecordBookResponse { Error = "Record book is unavailable." };
        var players = BuildPlayerHistories(league);
        return new RecordBookResponse
        {
            Ok = true,
            SeasonRecords = BuildSeasonRecords(players),
            CareerRecords = BuildCareerRecords(players),
            FranchiseRecords = BuildFranchiseRecords(league),
        };
    }

    private static List<PlayerHistory> BuildPlayerHistories(LeagueState league)
    {
        var histories = new Dictionary<string, PlayerHistory>(StringComparer.OrdinalIgnoreCase);
        foreach (var player in league.Teams.SelectMany(team => (team?.Roster ?? new List<PlayerState>()).Concat(team?.InjuredReserve ?? new List<PlayerState>()).Concat(team?.PracticeSquad ?? new List<PlayerState>())).Concat(league.FreeAgents ?? new List<PlayerState>()).Where(player => player != null))
            AddPlayer(histories, player.PlayerId, player.Name, player.Position, player.CareerStats, player.SeasonStats);
        foreach (var retired in (league.RetirementHistory ?? new List<SeasonRetirementRecord>()).SelectMany(record => record?.Players ?? new List<PlayerRetirementRecord>()).Where(player => player != null))
            AddPlayer(histories, retired.PlayerId, retired.PlayerName, retired.Position, retired.CareerStats, retired.CurrentSeasonStats);
        return histories.Values.ToList();
    }

    private static void AddPlayer(Dictionary<string, PlayerHistory> histories, string id, string name, string position, IEnumerable<PlayerSeasonStats> career, PlayerSeasonStats current)
    {
        if (string.IsNullOrWhiteSpace(id)) return;
        if (!histories.TryGetValue(id, out var history)) histories[id] = history = new PlayerHistory { Id = id, Name = name ?? "", Position = position ?? "" };
        foreach (var stats in (career ?? Enumerable.Empty<PlayerSeasonStats>()).Append(current).Where(stats => stats != null && stats.SeasonYear > 0 && stats.GamesPlayed > 0).GroupBy(stats => stats.SeasonYear).Select(group => group.First()))
            if (!history.Seasons.Any(existing => existing.SeasonYear == stats.SeasonYear)) history.Seasons.Add(stats);
    }

    private static List<RecordBookEntryDto> BuildSeasonRecords(IEnumerable<PlayerHistory> players)
        => BuildStatRecords(players.SelectMany(player => player.Seasons.Select(stats => new StatLine(player, stats))), season: true);

    private static List<RecordBookEntryDto> BuildCareerRecords(IEnumerable<PlayerHistory> players)
        => BuildStatRecords(players.Where(player => player.Seasons.Count > 0).Select(player => new StatLine(player, Sum(player.Seasons))), season: false);

    private static List<RecordBookEntryDto> BuildStatRecords(IEnumerable<StatLine> lines, bool season)
    {
        var records = new List<RecordBookEntryDto>();
        AddStat(records, lines, "Passing Yards", line => line.Stats.PassingYards, season);
        AddStat(records, lines, "Rushing Yards", line => line.Stats.RushingYards, season);
        AddStat(records, lines, "Receiving Yards", line => line.Stats.ReceivingYards, season);
        AddStat(records, lines, "Passing Touchdowns", line => line.Stats.PassingTouchdowns, season);
        AddStat(records, lines, "Sacks", line => line.Stats.Sacks, season);
        AddStat(records, lines, "Interceptions", line => line.Stats.Interceptions, season);
        return records;
    }

    private static void AddStat(List<RecordBookEntryDto> records, IEnumerable<StatLine> source, string label, Func<StatLine, int> value, bool season)
    {
        var winner = source.Where(line => value(line) > 0).OrderByDescending(value).ThenBy(line => line.Player.Name, StringComparer.OrdinalIgnoreCase).ThenBy(line => line.Player.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (winner != null) records.Add(new RecordBookEntryDto { Label = label, SubjectName = winner.Player.Name, SubjectId = winner.Player.Id, Value = value(winner), SeasonYear = season ? winner.Stats.SeasonYear : 0 });
    }

    private static List<RecordBookEntryDto> BuildFranchiseRecords(LeagueState league)
    {
        var seasons = (league.HistoricalSeasons ?? new List<SeasonHistoryRecord>()).Where(record => record != null).ToList();
        var teams = seasons.SelectMany(record => (record.TeamRecords ?? new List<SeasonTeamRecord>()).Select(team => new TeamSeason(team, record.SeasonYear))).ToList();
        var records = new List<RecordBookEntryDto>();
        AddTeamRecord(records, teams, "Most Wins", entry => entry.Record.Wins);
        AddTeamRecord(records, teams, "Most Points Scored", entry => entry.Record.PointsFor);
        var champion = seasons.GroupBy(record => record.ChampionTeamId ?? "", StringComparer.OrdinalIgnoreCase).Where(group => !string.IsNullOrWhiteSpace(group.Key)).OrderByDescending(group => group.Count()).ThenBy(group => group.First().ChampionTeamName, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (champion != null) records.Add(new RecordBookEntryDto { Label = "Most Championships", SubjectId = champion.Key, SubjectName = champion.First().ChampionTeamName, Value = champion.Count(), SeasonYear = champion.Max(record => record.SeasonYear) });
        return records;
    }

    private static void AddTeamRecord(List<RecordBookEntryDto> records, IEnumerable<TeamSeason> teams, string label, Func<TeamSeason, int> value)
    {
        var winner = teams.Where(team => value(team) > 0).OrderByDescending(value).ThenBy(team => team.Record.TeamName, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (winner != null) records.Add(new RecordBookEntryDto { Label = label, SubjectId = winner.Record.TeamId, SubjectName = winner.Record.TeamName, TeamName = winner.Record.TeamName, Value = value(winner), SeasonYear = winner.SeasonYear });
    }

    private static PlayerSeasonStats Sum(IEnumerable<PlayerSeasonStats> seasons) => new() { PassingYards = seasons.Sum(stats => stats.PassingYards), PassingTouchdowns = seasons.Sum(stats => stats.PassingTouchdowns), RushingYards = seasons.Sum(stats => stats.RushingYards), RushingTouchdowns = seasons.Sum(stats => stats.RushingTouchdowns), ReceivingYards = seasons.Sum(stats => stats.ReceivingYards), ReceivingTouchdowns = seasons.Sum(stats => stats.ReceivingTouchdowns), Tackles = seasons.Sum(stats => stats.Tackles), Sacks = seasons.Sum(stats => stats.Sacks), Interceptions = seasons.Sum(stats => stats.Interceptions) };
    private sealed class PlayerHistory { public string Id = ""; public string Name = ""; public string Position = ""; public List<PlayerSeasonStats> Seasons = new(); }
    private sealed class StatLine { public StatLine(PlayerHistory player, PlayerSeasonStats stats) { Player = player; Stats = stats; } public PlayerHistory Player; public PlayerSeasonStats Stats; }
    private sealed class TeamSeason { public TeamSeason(SeasonTeamRecord record, int seasonYear) { Record = record; SeasonYear = seasonYear; } public SeasonTeamRecord Record; public int SeasonYear; }
}
