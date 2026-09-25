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
    {
        var league = _context.ActiveLeague;
        var game = league == null ? null : ResolveGame(league, gameId);
        if (game == null || !GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId))
            return new GameResultResponse { Ok = false, Error = "No playable user game was found." };
        if (!league.Results.Any(r => r.GameId == game.GameId)
            && (game.AbsoluteWeek != league.Calendar.AbsoluteWeek || game.DayIndex != league.Calendar.DayIndex))
            return new GameResultResponse { Ok = false, Error = "Advance to the scheduled game day before simulating." };
        return SimulateScheduledGame(game.GameId, allowUserTeamGame: true);
    }

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

        if (league.ActiveLiveGameSession?.Active == true)
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

        if (!new CpuRosterManagementService(_context).PrepareForGame(game.GameId, game.HomeTeamId, game.AwayTeamId, out var rosterError))
            return new GameResultResponse { Ok = false, Error = rosterError };
        foreach (var team in league.Teams.Where(t => t.TeamId == game.HomeTeamId || t.TeamId == game.AwayTeamId))
            if (!team.Coaches.Any(c => c.Role == "Head Coach"))
                return new GameResultResponse { Ok = false, Error = $"{team.Name} must appoint a Head Coach before playing." };
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
            requireWinner: game.GameType != "regular_season" && game.GameType != "preseason");

        CommitResult(league, result);
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
        LeagueState league, string gameId, string homeTeamId, string awayTeamId,
        int absoluteWeek, int phaseWeek, string phase, string gameType, string weekLabel,
        int dayIndex, int homeFieldBonus, bool requireWinner)
    {
        var type = ScheduleService.NormalizeGameType(gameType, absoluteWeek);
        var result = ProSnapEngine.Create(league, gameId, homeTeamId, awayTeamId, absoluteWeek, phaseWeek,
            phase, type, weekLabel, homeFieldBonus, requireWinner);
        new ProSnapEngine(league, result).Finish();
        return result;
    }

    internal static GameResult CommitResult(LeagueState league, GameResult result)
    {
        var existing = league.Results.FirstOrDefault(r => string.Equals(r.GameId, result.GameId, StringComparison.OrdinalIgnoreCase));
        if (existing != null) return existing;
        if (result.ProGame != null && !result.ProGame.Completed)
            throw new InvalidOperationException("An unfinished game cannot be committed.");
        if (result.ProGame != null)
        {
            foreach (var play in result.BoxScore.PlayByPlay.Where(p => p.IsInjury && p.Injury != null))
            {
                var player = league.Teams.SelectMany(t => t.Roster).FirstOrDefault(p => p.PlayerId == play.InjuredPlayerId);
                PlayerInjuryService.InjurePlayer(league, player, play.Injury.Name, play.Injury.DaysRemaining, result.GameId);
            }
        }
        PlayerStatisticsService.ApplyGameFatigue(league, result);
        PlayerStatisticsService.ApplyRegularSeasonStats(league, result);
        league.Results.Add(result);
        var game = league.Schedule.FirstOrDefault(g => string.Equals(g.GameId, result.GameId, StringComparison.OrdinalIgnoreCase));
        if (game != null)
        {
            game.HomeScore = result.HomeScore; game.AwayScore = result.AwayScore;
            game.Winner = result.Winner; game.Status = "final";
        }
        return result;
    }
}
