using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Awards are immutable season-history snapshots derived from already-authoritative player statistics.
public static class SeasonAwardsService
{
    public static void EnsureAwards(LeagueState league, SeasonHistoryRecord season)
    {
        if (league == null || season == null) return;
        season.Awards ??= new List<SeasonAwardRecord>();
        if (season.Awards.Count > 0) return;
        var candidates = BuildCandidates(league, season.SeasonYear);
        AddAward(season.Awards, "Most Valuable Player", candidates, candidate => candidate.Stats.PassingYards + candidate.Stats.RushingYards + candidate.Stats.ReceivingYards + (candidate.Stats.PassingTouchdowns + candidate.Stats.RushingTouchdowns + candidate.Stats.ReceivingTouchdowns) * 45, candidate => $"{candidate.Stats.PassingYards:N0} pass yds | {candidate.Stats.RushingYards + candidate.Stats.ReceivingYards:N0} rush/rec yds | {candidate.TotalTouchdowns} TD");
        AddAward(season.Awards, "Offensive Player of the Year", candidates.Where(candidate => candidate.Stats.PassingYards + candidate.Stats.RushingYards + candidate.Stats.ReceivingYards > 0), candidate => candidate.Stats.PassingYards + candidate.Stats.RushingYards + candidate.Stats.ReceivingYards + candidate.TotalTouchdowns * 35, candidate => $"{candidate.Stats.PassingYards + candidate.Stats.RushingYards + candidate.Stats.ReceivingYards:N0} total yards | {candidate.TotalTouchdowns} TD");
        AddAward(season.Awards, "Defensive Player of the Year", candidates.Where(candidate => candidate.Stats.Tackles + candidate.Stats.Sacks + candidate.Stats.Interceptions > 0), candidate => candidate.Stats.Tackles + candidate.Stats.Sacks * 9 + candidate.Stats.Interceptions * 12, candidate => $"{candidate.Stats.Tackles} tackles | {candidate.Stats.Sacks} sacks | {candidate.Stats.Interceptions} INT");
    }

    private static List<Candidate> BuildCandidates(LeagueState league, int seasonYear)
    {
        var candidates = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in league.Teams.Where(team => team != null))
        foreach (var player in (team.Roster ?? new List<PlayerState>()).Concat(team.InjuredReserve ?? new List<PlayerState>()).Concat(team.PracticeSquad ?? new List<PlayerState>()).Where(player => player != null))
            AddCandidate(candidates, player.PlayerId, player.Name, player.Position, team.TeamId, team.Name, FindStats(player.CareerStats, player.SeasonStats, seasonYear));
        foreach (var player in (league.FreeAgents ?? new List<PlayerState>()).Where(player => player != null))
            AddCandidate(candidates, player.PlayerId, player.Name, player.Position, "", "Free Agent", FindStats(player.CareerStats, player.SeasonStats, seasonYear));
        foreach (var retired in (league.RetirementHistory ?? new List<SeasonRetirementRecord>()).SelectMany(record => record?.Players ?? new List<PlayerRetirementRecord>()).Where(player => player != null))
            AddCandidate(candidates, retired.PlayerId, retired.PlayerName, retired.Position, retired.TeamId, retired.TeamName, FindStats(retired.CareerStats, retired.CurrentSeasonStats, seasonYear));
        return candidates.Values.Where(candidate => candidate.Stats != null && candidate.Stats.GamesPlayed > 0).ToList();
    }

    private static void AddCandidate(Dictionary<string, Candidate> candidates, string id, string name, string position, string teamId, string teamName, PlayerSeasonStats stats)
    {
        if (string.IsNullOrWhiteSpace(id) || stats == null || stats.GamesPlayed <= 0 || candidates.ContainsKey(id)) return;
        candidates[id] = new Candidate { PlayerId = id, PlayerName = name ?? "", Position = position ?? "", TeamId = teamId ?? "", TeamName = teamName ?? "", Stats = stats };
    }

    private static PlayerSeasonStats FindStats(IEnumerable<PlayerSeasonStats> career, PlayerSeasonStats current, int seasonYear)
        => (career ?? Enumerable.Empty<PlayerSeasonStats>()).Append(current).FirstOrDefault(stats => stats != null && stats.SeasonYear == seasonYear && stats.GamesPlayed > 0);

    private static void AddAward(List<SeasonAwardRecord> awards, string name, IEnumerable<Candidate> candidates, Func<Candidate, int> score, Func<Candidate, string> summary)
    {
        var winner = candidates.OrderByDescending(score).ThenBy(candidate => candidate.PlayerName, StringComparer.OrdinalIgnoreCase).ThenBy(candidate => candidate.PlayerId, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        if (winner == null || score(winner) <= 0) return;
        awards.Add(new SeasonAwardRecord { AwardName = name, PlayerId = winner.PlayerId, PlayerName = winner.PlayerName, TeamId = winner.TeamId, TeamName = winner.TeamName, Position = winner.Position, Score = score(winner), Summary = summary(winner) });
    }

    private sealed class Candidate
    {
        public string PlayerId = ""; public string PlayerName = ""; public string Position = ""; public string TeamId = ""; public string TeamName = ""; public PlayerSeasonStats Stats = new();
        public int TotalTouchdowns => Stats.PassingTouchdowns + Stats.RushingTouchdowns + Stats.ReceivingTouchdowns;
    }
}
