using System;
using System.Collections.Generic;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

public partial class DashboardController
{
    private Godot.Collections.Dictionary BuildNativeGameResultDictionary(GameResultDto result)
    {
        var payload = new Godot.Collections.Dictionary
        {
            ["game_id"] = result?.GameId ?? "",
            ["week"] = result?.Week ?? 0,
            ["absolute_week"] = result?.AbsoluteWeek ?? 0,
            ["phase_week"] = result?.PhaseWeek ?? 0,
            ["phase"] = result?.Phase ?? "",
            ["game_type"] = result?.GameType ?? "",
            ["week_label"] = result?.WeekLabel ?? "",
            ["home_team"] = result?.HomeTeam ?? "",
            ["away_team"] = result?.AwayTeam ?? "",
            ["home_score"] = result?.HomeScore ?? 0,
            ["away_score"] = result?.AwayScore ?? 0,
            ["winner"] = result?.Winner ?? "",
            ["summary"] = result?.Summary ?? "",
        };

        var boxScore = new Godot.Collections.Dictionary
        {
            ["final"] = new Godot.Collections.Dictionary
            {
                ["away"] = result?.AwayScore ?? 0,
                ["home"] = result?.HomeScore ?? 0,
            },
        };

        var teamStats = new Godot.Collections.Dictionary();
        var playByPlay = new Godot.Collections.Array();
        var playerStats = new Godot.Collections.Array();
        if (result?.BoxScore != null)
        {
            foreach (var pair in result.BoxScore)
            {
                if (pair.Value is Dictionary<string, int> stats)
                {
                    foreach (var stat in stats)
                        teamStats[stat.Key] = stat.Value;
                }
                else if (pair.Value != null && string.Equals(pair.Key, "final", StringComparison.OrdinalIgnoreCase))
                {
                    boxScore["final_text"] = pair.Value.ToString() ?? "";
                }
                else if (pair.Value is IEnumerable<GamePlayEventState> plays && string.Equals(pair.Key, "play_by_play", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var play in plays)
                    {
                        playByPlay.Add(new Godot.Collections.Dictionary
                        {
                            ["sequence"] = play.Sequence,
                            ["quarter"] = play.Quarter,
                            ["clock_seconds"] = play.ClockSeconds,
                            ["possession_team_id"] = play.PossessionTeamId ?? "",
                            ["down"] = play.Down,
                            ["distance"] = play.Distance,
                            ["yard_line"] = play.YardLine,
                            ["yards_gained"] = play.YardsGained,
                            ["description"] = play.Description ?? "",
                            ["home_score"] = play.HomeScore,
                            ["away_score"] = play.AwayScore,
                            ["is_scoring_play"] = play.IsScoringPlay,
                            ["is_turnover"] = play.IsTurnover,
                            ["is_injury"] = play.IsInjury,
                        });
                    }
                }
                else if (pair.Value is IEnumerable<PlayerGameStats> playerLines && string.Equals(pair.Key, "player_stats", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var line in playerLines)
                    {
                        playerStats.Add(new Godot.Collections.Dictionary
                        {
                            ["player_id"] = line.PlayerId ?? "",
                            ["player_name"] = line.PlayerName ?? "",
                            ["team_id"] = line.TeamId ?? "",
                            ["position"] = line.Position ?? "",
                            ["passing_yards"] = line.PassingYards,
                            ["passing_touchdowns"] = line.PassingTouchdowns,
                            ["rushing_yards"] = line.RushingYards,
                            ["rushing_touchdowns"] = line.RushingTouchdowns,
                            ["receiving_yards"] = line.ReceivingYards,
                            ["receiving_touchdowns"] = line.ReceivingTouchdowns,
                            ["tackles"] = line.Tackles,
                            ["sacks"] = line.Sacks,
                            ["interceptions"] = line.Interceptions,
                            ["snaps"] = line.Snaps,
                            ["detailed_stats_known"] = line.Snaps > 0,
                            ["pass_attempts"] = line.PassAttempts,
                            ["completions"] = line.Completions,
                            ["interceptions_thrown"] = line.InterceptionsThrown,
                            ["rush_attempts"] = line.RushAttempts,
                            ["receptions"] = line.Receptions,
                            ["field_goal_attempts"] = line.FieldGoalAttempts,
                            ["field_goals_made"] = line.FieldGoalsMade,
                            ["extra_points_made"] = line.ExtraPointsMade,
                            ["extra_point_attempts"] = line.ExtraPointAttempts,
                            ["punts"] = line.Punts,
                            ["punt_yards"] = line.PuntYards,
                        });
                    }
                }
            }
        }
        var homeQuarterScores = new Godot.Collections.Array();
        var awayQuarterScores = new Godot.Collections.Array();
        if (result?.BoxScore?.TryGetValue("quarter_scores", out var rawPeriods) == true && rawPeriods is Dictionary<string, int[]> periods)
        {
            foreach (var value in periods["home"]) homeQuarterScores.Add(value);
            foreach (var value in periods["away"]) awayQuarterScores.Add(value);
        }
        boxScore["quarter_scores"] = new Godot.Collections.Dictionary { ["away"] = awayQuarterScores, ["home"] = homeQuarterScores };
        boxScore["quarter_scores_known"] = result?.BoxScore?.TryGetValue("quarter_scores_known", out var known) == true && known is true;
        boxScore["team_stats"] = teamStats;
        boxScore["play_by_play"] = playByPlay;
        boxScore["player_stats"] = playerStats;
        payload["box_score"] = boxScore;
        return payload;
    }

}
