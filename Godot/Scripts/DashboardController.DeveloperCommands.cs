using Godot;
using System;
using System.Linq;
using System.Threading.Tasks;
using GridironGM.GameCore.Services;

public partial class DashboardController
{
    private async Task<bool> TryRunDeveloperCommand()
    {
        var arguments = OS.GetCmdlineUserArgs();
        if (arguments.Contains("--gamecore-benchmark", StringComparer.Ordinal))
        {
            var report = await Task.Run(() => SimulationBenchmarkService.Run(GetTeamSeedPath()));
            foreach (var sample in new[] { report.SingleGame, report.ProWeek, report.ProSeason, report.ProjectedCollegeSeason })
                GD.Print($"[GameCore benchmark] {sample.Name}: {sample.Games} games, {sample.ElapsedMilliseconds:0.0} ms total, {sample.MillisecondsPerGame:0.000} ms/game, {sample.AllocatedBytes / 1024d / 1024d:0.00} MiB allocated, {sample.BytesPerGame / 1024d:0.0} KiB/game");
            GetTree().Quit(0);
            return true;
        }

        if (!arguments.Contains("--gamecore-smoke-test", StringComparer.Ordinal))
            return false;

        if (!ValidateTeamLogoAssets(out var logoError))
        {
            GD.PushError($"[Asset smoke] {logoError}");
            GetTree().Quit(1);
            return true;
        }
        GD.Print("[Asset smoke] PASS 32 installed team logos load as nearest-filtered textures.");
        var smokeResult = await Task.Run(() => GameCoreSmokeTest.Run(GetTeamSeedPath()));
        foreach (var step in smokeResult.Steps)
            GD.Print($"[GameCore smoke] {step}");
        if (!smokeResult.Ok)
            GD.PushError($"[GameCore smoke] {smokeResult.Message}");
        GetTree().Quit(smokeResult.Ok ? 0 : 1);
        return true;
    }
}
