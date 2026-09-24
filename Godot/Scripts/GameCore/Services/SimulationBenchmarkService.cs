using System;
using System.Diagnostics;
using System.Text.Json;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class SimulationBenchmarkSample
{
    public string Name { get; init; } = "";
    public int Games { get; init; }
    public double ElapsedMilliseconds { get; init; }
    public long AllocatedBytes { get; init; }
    public long RepresentativeResultBytes { get; init; }
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
/// Measures pro snap resolution without disk/rendering and the separate real college-season lifecycle.
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
            ProjectedCollegeSeason = MeasureCollege(context),
        };
    }

    private static SimulationBenchmarkSample MeasureCollege(GameCoreContext context)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        new CollegeUniverseService(context).AdvanceToProWeek(LeagueBootstrapService.TotalSeasonWeeks);
        stopwatch.Stop();
        return new SimulationBenchmarkSample
        {
            Name = "128-team college season (lightweight, including postseason lifecycle)",
            Games = context.ActiveLeague.CollegeUniverse.Results.Count + context.ActiveLeague.CollegeUniverse.Postseason.Games.Count,
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            AllocatedBytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - before),
        };
    }

    private static SimulationBenchmarkSample Measure(LeagueState league, string name, int gameCount)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var stopwatch = Stopwatch.StartNew();
        GameResult lastResult = null;
        for (var index = 0; index < gameCount; index++)
        {
            var home = league.Teams[(index * 2) % league.Teams.Count];
            var away = league.Teams[(index * 2 + 1) % league.Teams.Count];
            lastResult = GameDayService.SimulateMatchup(
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
                requireWinner: false);
        }
        stopwatch.Stop();
        var allocated = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - before);
        return new SimulationBenchmarkSample
        {
            Name = name,
            Games = gameCount,
            ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            AllocatedBytes = allocated,
            // Measure persisted log size outside the resolution time/allocation sample.
            RepresentativeResultBytes = JsonSerializer.SerializeToUtf8Bytes(lastResult, new JsonSerializerOptions { WriteIndented = true }).LongLength,
        };
    }
}
