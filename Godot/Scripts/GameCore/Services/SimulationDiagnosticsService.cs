using System;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class SimulationBalanceDiagnostics
{
    public int CompletedRegularSeasonGames { get; init; }
    public double PointsPerTeamGame { get; init; }
    public double HomeWinRate { get; init; }
    public int LargestScoreMargin { get; init; }
}

public static class SimulationDiagnosticsService
{
    public static SimulationBalanceDiagnostics AnalyzeRegularSeason(LeagueState league)
    {
        var games = (league?.Results ?? new())
            .Where(ScheduleService.CountsTowardRegularSeasonStandings)
            .ToList();
        if (games.Count == 0)
            return new SimulationBalanceDiagnostics();

        return new SimulationBalanceDiagnostics
        {
            CompletedRegularSeasonGames = games.Count,
            PointsPerTeamGame = games.Sum(game => game.HomeScore + game.AwayScore) / (double)(games.Count * 2),
            HomeWinRate = games.Count(game => game.HomeScore > game.AwayScore) / (double)games.Count,
            LargestScoreMargin = games.Max(game => Math.Abs(game.HomeScore - game.AwayScore)),
        };
    }
}
