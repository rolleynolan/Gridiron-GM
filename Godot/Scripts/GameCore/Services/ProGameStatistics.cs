using System;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

/// <summary>The single reducer for snap-derived box scores, also usable to verify or rebuild a projection.</summary>
public static class ProGameStatistics
{
    public static void Initialize(BoxScoreState box)
    {
        foreach (var side in new[] { "home", "away" })
            foreach (var name in new[] { "total_yards", "passing_yards", "rushing_yards", "sack_yards", "turnovers", "touchdowns", "first_downs", "plays", "points", "field_goals", "punts" })
                box.TeamStats.TryAdd($"{name}_{side}", 0);
    }

    public static void Apply(BoxScoreState box, GamePlayEventState play, string homeTeamId)
    {
        foreach (var id in play.ParticipantIds)
            box.PlayerStats.First(s => s.PlayerId == id).Snaps++;
        foreach (var delta in play.StatChanges)
        {
            var total = box.PlayerStats.FirstOrDefault(s => s.PlayerId == delta.PlayerId);
            if (total == null)
            {
                total = new PlayerGameStats { PlayerId = delta.PlayerId, PlayerName = delta.PlayerName, TeamId = delta.TeamId, Position = delta.Position };
                box.PlayerStats.Add(total);
            }
            total.Add(delta);
        }
        var side = play.OffensiveTeamId == homeTeamId ? "home" : "away";
        void Add(string key, int value) => box.TeamStats[$"{key}_{side}"] += value;
        var pass = play.StatChanges.Sum(s => s.PassingYards);
        var rush = play.StatChanges.Sum(s => s.RushingYards);
        var sack = play.StatChanges.Sum(s => s.SackYardsLost);
        Add("passing_yards", pass); Add("rushing_yards", rush); Add("sack_yards", sack);
        Add("total_yards", pass + rush - sack);
        if (play.PlayType is "run" or "pass") Add("plays", 1);
        if (play.IsFirstDown) Add("first_downs", 1);
        if (play.Outcome is "interception" or "fumble") Add("turnovers", 1);
        if (play.Outcome == "touchdown") Add("touchdowns", 1);
        if (play.Outcome == "field_goal_made") Add("field_goals", 1);
        if (play.PlayType == "punt") Add("punts", 1);
        if (play.Points > 0)
            box.TeamStats[$"points_{(play.ScoringTeamId == homeTeamId ? "home" : "away")}"] += play.Points;
    }

    public static BoxScoreState Rebuild(GameResult result)
    {
        var box = new BoxScoreState();
        // Names/positions are event-time identity metadata, never a source of numerical statistics.
        box.PlayerStats = result.BoxScore.PlayerStats.Select(s => new PlayerGameStats
            { PlayerId = s.PlayerId, PlayerName = s.PlayerName, TeamId = s.TeamId, Position = s.Position }).ToList();
        Initialize(box);
        foreach (var play in result.BoxScore.PlayByPlay) Apply(box, play, result.HomeTeamId);
        return box;
    }
}
