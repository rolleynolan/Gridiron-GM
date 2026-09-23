using System;
using System.Diagnostics;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class SimulationBenchmarkSample
{
    public string Name { get; init; } = "";
    public int Games { get; init; }
    public double ElapsedMilliseconds { get; init; }
    public long AllocatedBytes { get; init; }
    public double MillisecondsPerGame => Games == 0 ? 0d : ElapsedMilliseconds / Games;
    public long BytesPerGame => Games == 0 ? 0L : AllocatedBytes / Games;
}

public sealed class SimulationBenchmarkReport
{
    public SimulationBenchmarkSample SingleGame { get; init; } = new();
    public SimulationBenchmarkSample ProWeek { get; init; } = new();
    public SimulationBenchmarkSample ProSeason { get; init; } = new();
    public SimulationBenchmarkSample ProjectedCollegeSeason { get; init; } = new();
}

/// <summary>
/// Measures the detailed matchup simulator without disk or rendering work. The college sample
/// uses the same detailed engine at a 128-team, 12-game schedule workload (768 games).
/// </summary>
public static class SimulationBenchmarkService
{
    public const int ProWeekGames = 16;
    public const int ProSeasonGames = LeagueBootstrapService.RegularSeasonGameCount;
    public const int ProjectedCollegeSeasonGames = 128 * 12 / 2;

    public static SimulationBenchmarkReport Run(string teamSeedPath = null)
    {
        var context = new GameCoreContext();
        var league = new LeagueBootstrapService(context).CreateTestLeague(teamSeedPath);
        Measure(league, "Warmup", 2);
        return new SimulationBenchmarkReport
        {
            SingleGame = Measure(league, "Single detailed game", 1),
            ProWeek = Measure(league, "16-game pro week", ProWeekGames),
            ProSeason = Measure(league, "272-game pro regular season", ProSeasonGames),
            ProjectedCollegeSeason = Measure(league, "128-team college regular season", ProjectedCollegeSeasonGames),
        };
    }

    private static SimulationBenchmarkSample Measure(LeagueState league, string name, int gameCount)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < gameCount; index++)
        {
            var home = league.Teams[(index * 2) % league.Teams.Count];
            var away = league.Teams[(index * 2 + 1) % league.Teams.Count];
            _ = GameDayService.SimulateMatchup(
                league,
                $"benchmark-{gameCount}-{index}",
                home.TeamId,
                away.TeamId,
                absoluteWeek: index / ProWeekGames + 1,
                phaseWeek: index / ProWeekGames + 1,
                phase: "Regular Season",
                gameType: "regular_season",
                weekLabel: $"Week {index / ProWeekGames + 1}",
                dayIndex: 3,
                homeFieldBonus: 3,
                requireWinner: true);
        }
        stopwatch.Stop();
        return new SimulationBenchmarkSample
        {
            Name = name,
            Games = gameCount,
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            AllocatedBytes = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - before),
        };
    }
}
