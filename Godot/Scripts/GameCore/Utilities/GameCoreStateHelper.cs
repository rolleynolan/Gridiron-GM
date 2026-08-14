using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;

namespace GridironGM.GameCore.Utilities;

public static class GameCoreStateHelper
{
    public static TeamState GetUserTeam(LeagueState league)
        => league?.Teams?.FirstOrDefault(team =>
            string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));

    public static TeamState ResolveTeam(LeagueState league, string teamId = null)
    {
        if (league == null)
            return null;

        var resolvedTeamId = string.IsNullOrWhiteSpace(teamId) ? league.UserTeamId : teamId;
        return league.Teams.FirstOrDefault(team =>
            string.Equals(team.TeamId, resolvedTeamId, StringComparison.OrdinalIgnoreCase));
    }

    public static TeamState ResolveOpponent(LeagueState league, ScheduledGame game, string teamId)
    {
        if (league == null || game == null || string.IsNullOrWhiteSpace(teamId))
            return null;

        var opponentId = string.Equals(game.HomeTeamId, teamId, StringComparison.OrdinalIgnoreCase)
            ? game.AwayTeamId
            : game.HomeTeamId;

        return ResolveTeam(league, opponentId);
    }

    public static bool IsTeamInGame(ScheduledGame game, string teamId)
        => game != null
           && !string.IsNullOrWhiteSpace(teamId)
           && (string.Equals(game.HomeTeamId, teamId, StringComparison.OrdinalIgnoreCase)
               || string.Equals(game.AwayTeamId, teamId, StringComparison.OrdinalIgnoreCase));

    public static bool IsCurrentGameDay(LeagueState league, ScheduledGame game)
        => league != null
           && game != null
           && !IsFinal(game)
           && (
               string.Equals(game.Status, "game_day", StringComparison.OrdinalIgnoreCase)
               || (game.AbsoluteWeek == league.Calendar.AbsoluteWeek
                   && game.DayIndex == league.Calendar.DayIndex)
           );

    public static bool IsFinal(ScheduledGame game)
        => game != null && string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase);

    public static string BuildRecord(TeamStanding standing)
        => standing == null
            ? "0-0"
            : standing.Ties > 0
                ? $"{standing.Wins}-{standing.Losses}-{standing.Ties}"
                : $"{standing.Wins}-{standing.Losses}";

    public static string FormatCapRoom(decimal capRoom)
        => capRoom.ToString("C0", CultureInfo.InvariantCulture);

    public static IEnumerable<GameResult> GetRecentResults(LeagueState league, int count = 5)
        => (league?.Results ?? Enumerable.Empty<GameResult>())
            .OrderByDescending(result => result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week)
            .ThenByDescending(result => result.GameId, StringComparer.OrdinalIgnoreCase)
            .Take(count);

    public static GameResultDto ToGameResultDto(GameResult result)
    {
        var absoluteWeek = result?.AbsoluteWeek ?? result?.Week ?? 0;
        var phaseWeek = result?.PhaseWeek ?? 0;
        if (phaseWeek <= 0)
            phaseWeek = ScheduleService.GetDisplayWeek(result?.GameType ?? "", absoluteWeek);
        var weekLabel = result?.WeekLabel ?? "";
        if (string.IsNullOrWhiteSpace(weekLabel))
            weekLabel = ScheduleService.BuildGameWeekLabel(result?.GameType ?? "", absoluteWeek, phaseWeek);

        var dto = new GameResultDto
        {
            GameId = result?.GameId ?? "",
            Week = phaseWeek,
            AbsoluteWeek = absoluteWeek,
            PhaseWeek = phaseWeek,
            Phase = string.IsNullOrWhiteSpace(result?.Phase) ? ScheduleService.GetPhaseForGameType(result?.GameType ?? "") : result.Phase,
            GameType = result?.GameType ?? "",
            WeekLabel = weekLabel,
            HomeTeam = result?.HomeTeam ?? "",
            AwayTeam = result?.AwayTeam ?? "",
            HomeScore = result?.HomeScore ?? 0,
            AwayScore = result?.AwayScore ?? 0,
            Winner = result?.Winner ?? "",
            Summary = result?.Summary ?? "",
        };

        if (result?.BoxScore != null)
        {
            dto.BoxScore["final"] = result.BoxScore.Final;
            dto.BoxScore["team_stats"] = new Dictionary<string, int>(result.BoxScore.TeamStats);
        }

        return dto;
    }
}
