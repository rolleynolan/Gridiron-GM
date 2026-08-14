using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class StandingsService
{
    private readonly GameCoreContext _context;

    public StandingsService(GameCoreContext context)
    {
        _context = context;
    }

    public StandingsResponse GetStandings()
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new StandingsResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        return new StandingsResponse
        {
            Ok = true,
            Standings = BuildStandings(league).Select(standing => new StandingRowDto
            {
                TeamId = standing.TeamId,
                TeamName = standing.TeamName,
                Abbreviation = standing.Abbreviation,
                Wins = standing.Wins,
                Losses = standing.Losses,
                Ties = standing.Ties,
                WinPct = standing.WinPct,
                PointsFor = standing.PointsFor,
                PointsAgainst = standing.PointsAgainst,
                Division = standing.Division,
                Conference = standing.Conference,
            }).ToList(),
            PlayoffBracket = new PlayoffService(_context).GetPlayoffBracketDto(league),
        };
    }

    public List<TeamStanding> BuildStandings(LeagueState league)
    {
        var standings = league.Teams.ToDictionary(
            team => team.TeamId,
            team => new TeamStanding
            {
                TeamId = team.TeamId,
                TeamName = team.Name,
                Abbreviation = team.Abbreviation,
                Division = team.Division,
                Conference = team.Conference,
            },
            StringComparer.OrdinalIgnoreCase);

        foreach (var result in league.Results)
        {
            if (!ScheduleService.CountsTowardRegularSeasonStandings(result))
                continue;

            if (!standings.TryGetValue(result.HomeTeamId, out var home)
                || !standings.TryGetValue(result.AwayTeamId, out var away))
            {
                continue;
            }

            home.PointsFor += result.HomeScore;
            home.PointsAgainst += result.AwayScore;
            away.PointsFor += result.AwayScore;
            away.PointsAgainst += result.HomeScore;

            if (result.HomeScore > result.AwayScore)
            {
                home.Wins++;
                away.Losses++;
            }
            else if (result.AwayScore > result.HomeScore)
            {
                away.Wins++;
                home.Losses++;
            }
            else
            {
                home.Ties++;
                away.Ties++;
            }
        }

        foreach (var standing in standings.Values)
        {
            var games = standing.Wins + standing.Losses + standing.Ties;
            standing.WinPct = games == 0
                ? 0
                : Math.Round((standing.Wins + (standing.Ties * 0.5)) / games, 3);
            standing.PointDifferential = standing.PointsFor - standing.PointsAgainst;
        }

        return PlayoffService.RankStandings(standings.Values);
    }
}
