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
            Summary = $"{winner} defeated {loser}, {homeScore}-{awayScore}.",
            BoxScore = BuildBoxScore(homeTeam, awayTeam, homeScore, awayScore),
        };
    }

    private static int BuildScore(TeamState team, int week, int dayIndex, int bonus)
    {
        var averageOverall = team?.Roster.Count > 0
            ? (int)Math.Round(team.Roster.Average(player => player.Overall))
            : 65;

        return 14 + bonus + (averageOverall % 11) + week + Math.Max(0, dayIndex);
    }

    private static BoxScoreState BuildBoxScore(TeamState homeTeam, TeamState awayTeam, int homeScore, int awayScore)
    {
        var homeYards = (homeTeam?.Roster.Sum(player => player.Overall) ?? 700) + (homeScore * 8);
        var awayYards = (awayTeam?.Roster.Sum(player => player.Overall) ?? 680) + (awayScore * 8);

        return new BoxScoreState
        {
            Final = $"{homeTeam?.Abbreviation ?? "HOME"} {homeScore}, {awayTeam?.Abbreviation ?? "AWAY"} {awayScore}",
            TeamStats = new Dictionary<string, int>
            {
                ["total_yards_home"] = homeYards,
                ["total_yards_away"] = awayYards,
                ["turnovers_home"] = Math.Abs(homeScore - awayScore) % 3,
                ["turnovers_away"] = (Math.Abs(homeScore - awayScore) + 1) % 3,
            },
        };
    }
}
