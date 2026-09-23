using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class GameDayService
{
    private readonly GameCoreContext _context;
    private readonly ScheduleService _scheduleService;

    public GameDayService(GameCoreContext context)
    {
        _context = context;
        _scheduleService = new ScheduleService(context);
    }

    public GameDayStateResponse GetCurrentGameDayState()
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new GameDayStateResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var game = GetCurrentUserGame();
        if (game == null)
        {
            return new GameDayStateResponse
            {
                Ok = false,
                Error = "No current user game.",
            };
        }

        var userTeam = GameCoreStateHelper.GetUserTeam(league);
        var opponent = GameCoreStateHelper.ResolveOpponent(league, game, league.UserTeamId);

        return new GameDayStateResponse
        {
            Ok = true,
            Game = new GameDayStateDto
            {
                GameId = game.GameId,
                Week = game.PhaseWeek,
                AbsoluteWeek = game.AbsoluteWeek,
                PhaseWeek = game.PhaseWeek,
                Phase = game.Phase,
                GameType = game.GameType,
                WeekLabel = game.WeekLabel,
                HomeTeam = league.Teams.FirstOrDefault(x => string.Equals(x.TeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase))?.Abbreviation ?? game.HomeTeamId,
                AwayTeam = league.Teams.FirstOrDefault(x => string.Equals(x.TeamId, game.AwayTeamId, StringComparison.OrdinalIgnoreCase))?.Abbreviation ?? game.AwayTeamId,
                Opponent = opponent?.Name ?? "TBD",
                OpponentAbbreviation = opponent?.Abbreviation ?? "",
                HomeAway = string.Equals(game.HomeTeamId, userTeam?.TeamId, StringComparison.OrdinalIgnoreCase) ? "home" : "away",
                Status = game.Status,
            },
        };
    }

    public ScheduledGame GetCurrentUserGame()
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return null;

        _scheduleService.RefreshStatuses(league);
        return league.Schedule.FirstOrDefault(game =>
            GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId)
            && game.AbsoluteWeek == league.Calendar.AbsoluteWeek
            && game.DayIndex == league.Calendar.DayIndex
            && !GameCoreStateHelper.IsFinal(game));
    }

    public GameResultResponse SimulateCurrentUserGame(string gameId = null)
        => SimulateScheduledGame(gameId, allowUserTeamGame: true);

    public GameResultResponse SimulateScheduledGame(string gameId, bool allowUserTeamGame)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var game = ResolveGame(league, gameId);
        if (game == null)
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "Game result not found.",
            };
        }

        if (league.ActiveLiveGameSession?.Active == true
            && string.Equals(league.ActiveLiveGameSession.GameId, game.GameId, StringComparison.OrdinalIgnoreCase))
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "A live game session is already in progress. Resume or finish it before using full-game simulation.",
            };
        }

        if (!allowUserTeamGame && GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId))
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "User game cannot be auto-simulated by this path.",
            };
        }

        var existing = league.Results.FirstOrDefault(result =>
            string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            return new GameResultResponse
            {
                Ok = true,
                Result = GameCoreStateHelper.ToGameResultDto(existing),
            };
        }

        var result = SimulateMatchup(
            league,
            game.GameId,
            game.HomeTeamId,
            game.AwayTeamId,
            game.AbsoluteWeek,
            game.PhaseWeek,
            game.Phase,
            game.GameType,
            game.WeekLabel,
            game.DayIndex,
            homeFieldBonus: 3,
            requireWinner: true);

        PlayerInjuryService.ApplyDeterministicGameInjuries(league, result);
        AppendInjuryEvents(league, result);
        PlayerStatisticsService.ApplyGameFatigue(league, result);
        PlayerStatisticsService.ApplyRegularSeasonStats(league, result);
        league.Results.Add(result);
        game.HomeScore = result.HomeScore;
        game.AwayScore = result.AwayScore;
        game.Winner = result.Winner;
        game.Status = "final";
        _scheduleService.RefreshStatuses(league);

        return new GameResultResponse
        {
            Ok = true,
            Result = GameCoreStateHelper.ToGameResultDto(result),
        };
    }

    public GameResultResponse GetGameResult(string gameId)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        if (string.IsNullOrWhiteSpace(gameId))
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "Game result not found.",
            };
        }

        var result = league.Results.FirstOrDefault(entry =>
            string.Equals(entry.GameId, gameId, StringComparison.OrdinalIgnoreCase));
        if (result == null)
        {
            return new GameResultResponse
            {
                Ok = false,
                Error = "Game result not found.",
            };
        }

        return new GameResultResponse
        {
            Ok = true,
            Result = GameCoreStateHelper.ToGameResultDto(result),
        };
    }

    private ScheduledGame ResolveGame(LeagueState league, string gameId)
    {
        if (!string.IsNullOrWhiteSpace(gameId))
        {
            return league.Schedule.FirstOrDefault(game =>
                string.Equals(game.GameId, gameId, StringComparison.OrdinalIgnoreCase));
        }

        return GetCurrentUserGame();
    }

    internal static GameResult SimulateMatchup(
        LeagueState league,
        string gameId,
        string homeTeamId,
        string awayTeamId,
        int absoluteWeek,
        int phaseWeek,
        string phase,
        string gameType,
        string weekLabel,
        int dayIndex,
        int homeFieldBonus,
        bool requireWinner)
    {
        var homeTeam = GameCoreStateHelper.ResolveTeam(league, homeTeamId);
        var awayTeam = GameCoreStateHelper.ResolveTeam(league, awayTeamId);
        var resolvedAbsoluteWeek = absoluteWeek > 0 ? absoluteWeek : 1;
        var resolvedPhaseWeek = phaseWeek > 0 ? phaseWeek : ScheduleService.GetDisplayWeek(gameType, resolvedAbsoluteWeek);
        var resolvedGameType = ScheduleService.NormalizeGameType(gameType, resolvedAbsoluteWeek);
        var homeScore = BuildScore(homeTeam, resolvedAbsoluteWeek, dayIndex, homeFieldBonus);
        var awayScore = BuildScore(awayTeam, resolvedAbsoluteWeek, dayIndex, 0);
        if (requireWinner && homeScore == awayScore)
            homeScore++;

        var winner = homeScore > awayScore ? homeTeam?.Name ?? homeTeamId : awayTeam?.Name ?? awayTeamId;
        var loser = homeScore > awayScore ? awayTeam?.Name ?? awayTeamId : homeTeam?.Name ?? homeTeamId;
        var winnerScore = Math.Max(homeScore, awayScore);
        var loserScore = Math.Min(homeScore, awayScore);

        return new GameResult
        {
            GameId = gameId ?? "",
            Week = resolvedAbsoluteWeek,
            AbsoluteWeek = resolvedAbsoluteWeek,
            PhaseWeek = resolvedPhaseWeek,
            Phase = string.IsNullOrWhiteSpace(phase) ? ScheduleService.GetPhaseForGameType(resolvedGameType) : phase,
            GameType = resolvedGameType,
            WeekLabel = string.IsNullOrWhiteSpace(weekLabel)
                ? ScheduleService.BuildGameWeekLabel(resolvedGameType, resolvedAbsoluteWeek, resolvedPhaseWeek)
                : weekLabel,
            HomeTeamId = homeTeamId ?? "",
            AwayTeamId = awayTeamId ?? "",
            HomeTeam = homeTeam?.Abbreviation ?? homeTeamId ?? "",
            AwayTeam = awayTeam?.Abbreviation ?? awayTeamId ?? "",
            HomeScore = homeScore,
            AwayScore = awayScore,
            Winner = winner,
            Summary = $"{winner} defeated {loser}, {winnerScore}-{loserScore}.",
            BoxScore = BuildBoxScore(homeTeam, awayTeam, homeScore, awayScore),
        };
    }

    private static int BuildScore(TeamState team, int week, int dayIndex, int bonus)
    {
        var starters = ResolveDepthChartStarters(team);
        var averageOverall = starters.Count > 0
            ? (int)Math.Round(starters.DefaultIfEmpty()
                .Average(player => player == null ? 65 : Math.Max(0, player.Overall - (Math.Clamp(player.Fatigue, 0, 100) / 8))))
            : 65;

        var coordinators = team?.Coaches?.Where(coach => coach != null && (string.Equals(coach.Role, "Offensive Coordinator", StringComparison.OrdinalIgnoreCase) || string.Equals(coach.Role, "Defensive Coordinator", StringComparison.OrdinalIgnoreCase))).ToList();
        var strategyModifier = coordinators?.Count == 2 ? Math.Clamp(((coordinators[0].Overall + coordinators[1].Overall) - 150) / 30, -1, 1) : 0;
        var strengthModifier = Math.Clamp((averageOverall - 60) / 2, -5, 15);
        var gameVariation = BuildScoreVariation(team?.TeamId, week, dayIndex);
        return Math.Clamp(17 + bonus + strategyModifier + strengthModifier + gameVariation, 3, 55);
    }

    private static int BuildScoreVariation(string teamId, int week, int dayIndex)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in teamId ?? string.Empty)
                hash = (hash ^ character) * 16777619;
            hash = (hash ^ (uint)Math.Max(0, week)) * 16777619;
            hash = (hash ^ (uint)Math.Max(0, dayIndex)) * 16777619;
            return (int)(hash % 11) - 5;
        }
    }

    private static BoxScoreState BuildBoxScore(TeamState homeTeam, TeamState awayTeam, int homeScore, int awayScore)
    {
        var homeScoringSchedule = BuildScoringSchedule(homeScore, 0);
        var awayScoringSchedule = BuildScoringSchedule(awayScore, 1);
        var homeTouchdowns = homeScoringSchedule.Count(score => score.Points == 7);
        var awayTouchdowns = awayScoringSchedule.Count(score => score.Points == 7);
        var homePlayerStats = BuildPlayerStats(homeTeam, homeScore, homeTouchdowns).ToList();
        var awayPlayerStats = BuildPlayerStats(awayTeam, awayScore, awayTouchdowns).ToList();
        var homeYards = homePlayerStats.Sum(stats => stats.PassingYards + stats.RushingYards);
        var awayYards = awayPlayerStats.Sum(stats => stats.PassingYards + stats.RushingYards);

        return new BoxScoreState
        {
            Final = $"{homeTeam?.Abbreviation ?? "HOME"} {homeScore}, {awayTeam?.Abbreviation ?? "AWAY"} {awayScore}",
            TeamStats = new Dictionary<string, int>
            {
                ["total_yards_home"] = homeYards,
                ["total_yards_away"] = awayYards,
                ["turnovers_home"] = Math.Abs(homeScore - awayScore) % 3,
                ["turnovers_away"] = (Math.Abs(homeScore - awayScore) + 1) % 3,
                ["touchdowns_home"] = homeTouchdowns,
                ["touchdowns_away"] = awayTouchdowns,
            },
            PlayerStats = homePlayerStats
                .Concat(awayPlayerStats)
                .ToList(),
            PlayByPlay = BuildPlayByPlay(homeTeam, awayTeam, homeScore, awayScore, homeScoringSchedule, awayScoringSchedule),
        };
    }

    private static List<GamePlayEventState> BuildPlayByPlay(
        TeamState homeTeam,
        TeamState awayTeam,
        int homeScore,
        int awayScore,
        IReadOnlyList<(int Quarter, int Points, int Order)> homeSchedule,
        IReadOnlyList<(int Quarter, int Points, int Order)> awaySchedule)
    {
        var events = new List<GamePlayEventState>();
        var runningHome = 0;
        var runningAway = 0;

        for (var quarter = 1; quarter <= 4; quarter++)
        {
            events.Add(new GamePlayEventState
            {
                Quarter = quarter,
                ClockSeconds = 14 * 60 + 12,
                PossessionTeamId = quarter % 2 == 1 ? awayTeam?.TeamId ?? "" : homeTeam?.TeamId ?? "",
                Down = 1,
                Distance = 10,
                YardLine = 25,
                YardsGained = 5 + quarter,
                Description = $"{(quarter % 2 == 1 ? awayTeam?.Abbreviation : homeTeam?.Abbreviation) ?? "Team"} opens the quarter with a sustained drive.",
                HomeScore = runningHome,
                AwayScore = runningAway,
            });

            var scoring = homeSchedule.Where(item => item.Quarter == quarter).Select(item => (Home: true, item.Points, item.Order))
                .Concat(awaySchedule.Where(item => item.Quarter == quarter).Select(item => (Home: false, item.Points, item.Order)))
                .OrderBy(item => item.Order)
                .ToList();
            for (var index = 0; index < scoring.Count; index++)
            {
                var score = scoring[index];
                if (score.Home) runningHome += score.Points; else runningAway += score.Points;
                var team = score.Home ? homeTeam : awayTeam;
                events.Add(new GamePlayEventState
                {
                    Quarter = quarter,
                    ClockSeconds = Math.Max(45, 11 * 60 - (index * 165) - (score.Order % 37)),
                    PossessionTeamId = team?.TeamId ?? "",
                    Down = score.Points == 3 ? 4 : 1,
                    Distance = score.Points == 3 ? 7 : 10,
                    YardLine = score.Points == 3 ? 24 : 1,
                    YardsGained = score.Points == 7 ? 8 + ((quarter + index) % 18) : 0,
                    Description = BuildScoringDescription(team?.Abbreviation, score.Points),
                    HomeScore = runningHome,
                    AwayScore = runningAway,
                    IsScoringPlay = true,
                });
            }

            events.Add(new GamePlayEventState
            {
                Quarter = quarter,
                ClockSeconds = 18,
                PossessionTeamId = quarter % 2 == 1 ? homeTeam?.TeamId ?? "" : awayTeam?.TeamId ?? "",
                Down = 4,
                Distance = 6,
                YardLine = 44,
                Description = "The drive ends with a punt as the quarter winds down.",
                HomeScore = runningHome,
                AwayScore = runningAway,
            });
        }

        events.Add(new GamePlayEventState
        {
            Quarter = 4,
            ClockSeconds = 0,
            Description = $"Final: {homeTeam?.Abbreviation ?? "HOME"} {homeScore}, {awayTeam?.Abbreviation ?? "AWAY"} {awayScore}.",
            HomeScore = homeScore,
            AwayScore = awayScore,
        });
        for (var index = 0; index < events.Count; index++)
            events[index].Sequence = index + 1;
        return events;
    }

    private static List<(int Quarter, int Points, int Order)> BuildScoringSchedule(int totalScore, int offset)
    {
        var chunks = new List<int>();
        var remaining = Math.Max(0, totalScore);
        while (remaining > 0)
        {
            var points = remaining switch
            {
                8 => 3,
                6 => 3,
                5 => 3,
                4 => 2,
                3 => 3,
                2 => 2,
                _ when remaining >= 7 => 7,
                _ => remaining,
            };
            chunks.Add(points);
            remaining -= points;
        }

        return chunks.Select((points, index) => (
                Quarter: 1 + ((index + offset) % 4),
                Points: points,
                Order: (index * 2) + offset))
            .ToList();
    }

    private static string BuildScoringDescription(string abbreviation, int points)
    {
        var team = string.IsNullOrWhiteSpace(abbreviation) ? "Team" : abbreviation;
        return points switch
        {
            7 => $"{team} finishes the drive with a touchdown and extra point.",
            3 => $"{team} converts the field goal.",
            2 => $"{team} records a safety.",
            _ => $"{team} adds {points} point{(points == 1 ? "" : "s")}.",
        };
    }

    internal static void AppendInjuryEvents(LeagueState league, GameResult result)
    {
        if (league == null || result?.BoxScore?.PlayByPlay == null)
            return;
        var injuries = league.Teams
            .Where(team => team != null && (string.Equals(team.TeamId, result.HomeTeamId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(team.TeamId, result.AwayTeamId, StringComparison.OrdinalIgnoreCase)))
            .SelectMany(team => team.Roster.Select(player => (Team: team, Player: player)))
            .Where(entry => entry.Player?.CurrentInjury?.IsActive == true
                && string.Equals(entry.Player.CurrentInjury.GameId, result.GameId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Player.PlayerId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (injuries.Count == 0)
            return;

        var finalIndex = result.BoxScore.PlayByPlay.FindIndex(play => play.Quarter >= 4);
        if (finalIndex < 0)
            finalIndex = Math.Max(0, result.BoxScore.PlayByPlay.Count - 1);
        foreach (var injury in injuries)
        {
            var priorPlay = finalIndex > 0 ? result.BoxScore.PlayByPlay[finalIndex - 1] : null;
            result.BoxScore.PlayByPlay.Insert(finalIndex++, new GamePlayEventState
            {
                Quarter = 3,
                ClockSeconds = Math.Max(90, 510 - (finalIndex * 23)),
                PossessionTeamId = injury.Team.TeamId,
                YardLine = 50,
                Description = $"Medical timeout: {injury.Player.Name} leaves with {injury.Player.CurrentInjury.Name}.",
                HomeScore = priorPlay?.HomeScore ?? 0,
                AwayScore = priorPlay?.AwayScore ?? 0,
                IsInjury = true,
            });
        }
        for (var index = 0; index < result.BoxScore.PlayByPlay.Count; index++)
            result.BoxScore.PlayByPlay[index].Sequence = index + 1;
    }

    private static IEnumerable<PlayerGameStats> BuildPlayerStats(TeamState team, int score, int totalTouchdowns)
    {
        var players = (team?.Roster ?? new List<PlayerState>())
            .Where(PlayerInjuryService.IsAvailableForGame)
            .OrderBy(player => GetDepthChartRank(team, player))
            .ThenByDescending(player => player.Overall)
            .ThenBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var quarterback = players.FirstOrDefault(player => string.Equals(player.Position, "QB", StringComparison.OrdinalIgnoreCase));
        var runner = players.FirstOrDefault(player => string.Equals(player.Position, "RB", StringComparison.OrdinalIgnoreCase));
        var receivers = players.Where(player => string.Equals(player.Position, "WR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(player.Position, "TE", StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();
        var defenders = players.Where(player => player.Position.EndsWith("B", StringComparison.OrdinalIgnoreCase)
                || player.Position.EndsWith("E", StringComparison.OrdinalIgnoreCase)
                || string.Equals(player.Position, "DT", StringComparison.OrdinalIgnoreCase)
                || string.Equals(player.Position, "CB", StringComparison.OrdinalIgnoreCase)
                || string.Equals(player.Position, "S", StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();
        var stats = new List<PlayerGameStats>();
        var passingYards = 120 + (score * 5) + (quarterback?.Overall ?? 65);
        var canRecordPassingTouchdowns = quarterback != null && receivers.Count > 0;
        var canRecordRushingTouchdowns = runner != null;
        var rushingTouchdowns = canRecordRushingTouchdowns
            ? canRecordPassingTouchdowns ? totalTouchdowns / 3 : totalTouchdowns
            : 0;
        var passingTouchdowns = canRecordPassingTouchdowns ? totalTouchdowns - rushingTouchdowns : 0;

        AddStats(stats, quarterback, team, stat =>
        {
            stat.PassingYards = passingYards;
            stat.PassingTouchdowns = passingTouchdowns;
        });
        AddStats(stats, runner, team, stat =>
        {
            stat.RushingYards = 35 + (score * 2) + ((runner?.Overall ?? 65) / 3);
            stat.RushingTouchdowns = rushingTouchdowns;
        });
        for (var index = 0; index < receivers.Count; index++)
        {
            var receiver = receivers[index];
            AddStats(stats, receiver, team, stat =>
            {
                stat.ReceivingYards = index == 0 ? (passingYards * 3) / 5 : (passingYards * 2) / 5;
                stat.ReceivingTouchdowns = index == 0 ? (passingTouchdowns + 1) / 2 : passingTouchdowns / 2;
            });
        }
        for (var index = 0; index < defenders.Count; index++)
        {
            var defender = defenders[index];
            AddStats(stats, defender, team, stat =>
            {
                stat.Tackles = 5 + ((defender.Overall + index) % 6);
                stat.Sacks = defender.Position.EndsWith("E", StringComparison.OrdinalIgnoreCase) || string.Equals(defender.Position, "DT", StringComparison.OrdinalIgnoreCase)
                    ? (score + index) % 3
                    : 0;
                stat.Interceptions = string.Equals(defender.Position, "CB", StringComparison.OrdinalIgnoreCase) || string.Equals(defender.Position, "S", StringComparison.OrdinalIgnoreCase)
                    ? (score + index) % 2
                    : 0;
            });
        }
        return stats;
    }

    private static List<PlayerState> ResolveDepthChartStarters(TeamState team)
    {
        if (team?.Roster == null || team.Roster.Count == 0)
            return new List<PlayerState>();
        var rosterById = team.Roster.ToDictionary(player => player.PlayerId, StringComparer.OrdinalIgnoreCase);
        var starters = new List<PlayerState>();
        foreach (var position in team.DepthChart ?? new Dictionary<string, List<string>>())
        {
            var starter = (position.Value ?? new List<string>())
                .Select(playerId => rosterById.TryGetValue(playerId, out var player) ? player : null)
                .FirstOrDefault(player => player != null && PlayerInjuryService.IsAvailableForGame(player));
            if (starter != null && !starters.Contains(starter))
                starters.Add(starter);
        }
        if (starters.Count > 0)
            return starters;
        return team.Roster.Where(PlayerInjuryService.IsAvailableForGame)
            .GroupBy(player => player.Position, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(player => player.Overall).First())
            .ToList();
    }

    private static int GetDepthChartRank(TeamState team, PlayerState player)
    {
        if (team?.DepthChart == null || player == null || !team.DepthChart.TryGetValue(player.Position ?? "", out var order) || order == null)
            return int.MaxValue;
        var rank = order.FindIndex(playerId => string.Equals(playerId, player.PlayerId, StringComparison.OrdinalIgnoreCase));
        return rank >= 0 ? rank : int.MaxValue;
    }

    private static void AddStats(List<PlayerGameStats> stats, PlayerState player, TeamState team, Action<PlayerGameStats> apply)
    {
        if (player == null)
            return;

        var line = new PlayerGameStats
        {
            PlayerId = player.PlayerId,
            PlayerName = player.Name,
            TeamId = team?.TeamId ?? "",
            Position = player.Position,
        };
        apply(line);
        stats.Add(line);
    }
}
