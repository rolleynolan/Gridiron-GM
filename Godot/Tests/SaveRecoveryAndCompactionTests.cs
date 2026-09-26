using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Services;
using Xunit;

namespace GridironGM.Tests;

public sealed class SaveRecoveryAndCompactionTests
{
    [Fact]
    public void SuccessfulSavesRetainThreeOrderedRecoveryBackups()
    {
        var name = $"rolling-backup-{Guid.NewGuid():N}.json"; var saves = new GameCoreSaveService(); var context = ContextWithEvents();
        try
        {
            for (var year = 2026; year <= 2030; year++)
            {
                context.ActiveLeague.SeasonYear = year;
                Assert.True(saves.Save(context, name).Ok);
            }

            var info = saves.GetStorageInfo(name);
            Assert.True(info.PrimaryExists); Assert.Equal(3, info.BackupCount); Assert.True(info.BackupBytes > 0);
            Assert.Equal(2030, saves.Load(name).League.SeasonYear);
            Assert.Equal(2029, saves.LoadBackup(name, 1).League.SeasonYear);
            Assert.Equal(2028, saves.LoadBackup(name, 2).League.SeasonYear);
            Assert.Equal(2027, saves.LoadBackup(name, 3).League.SeasonYear);
        }
        finally { saves.Delete(name); }
    }

    [Fact]
    public void CorruptPrimaryUsesLatestValidBackupAndPreservesDamagedFiles()
    {
        var name = $"recovery-{Guid.NewGuid():N}.json"; var saves = new GameCoreSaveService(); var context = ContextWithEvents();
        var path = AbsolutePath(name);
        try
        {
            context.ActiveLeague.SeasonYear = 2026; Assert.True(saves.Save(context, name).Ok);
            context.ActiveLeague.SeasonYear = 2027; Assert.True(saves.Save(context, name).Ok);
            context.ActiveLeague.SeasonYear = 2028; Assert.True(saves.Save(context, name).Ok);
            File.WriteAllText(path, "{broken", Encoding.UTF8);
            File.WriteAllText(path + ".backup1", "{also-broken", Encoding.UTF8);

            var failed = saves.Load(name);
            Assert.False(failed.Ok); Assert.True(failed.SaveCorrupt); Assert.True(failed.RecoveryAvailable); Assert.Equal(2, failed.AvailableBackupCount);
            Assert.False(saves.LoadBackup(name).Ok);
            Assert.Equal(2026, saves.LoadBackup(name, 2).League.SeasonYear);
            Assert.True(saves.RecoverFromLatestValidBackup(name).Ok);
            Assert.Equal(2026, saves.Load(name).League.SeasonYear);
            Assert.NotEmpty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".corrupt-*"));
        }
        finally
        {
            saves.Delete(name);
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".corrupt-*"));
        }
    }

    [Fact]
    public void MissingPrimaryCanBeExplicitlyRestoredFromRetainedBackup()
    {
        var name = $"missing-primary-{Guid.NewGuid():N}.json"; var saves = new GameCoreSaveService(); var context = ContextWithEvents();
        var path = AbsolutePath(name);
        try
        {
            context.ActiveLeague.SeasonYear = 2026; Assert.True(saves.Save(context, name).Ok);
            context.ActiveLeague.SeasonYear = 2027; Assert.True(saves.Save(context, name).Ok);
            File.Delete(path);

            var missing = saves.Load(name);
            Assert.False(missing.Ok); Assert.True(missing.SaveMissing); Assert.True(missing.RecoveryAvailable);
            Assert.True(saves.RecoverFromBackup(name).Ok);
            Assert.Equal(2026, saves.Load(name).League.SeasonYear);
        }
        finally { saves.Delete(name); }
    }

    [Fact]
    public void CompactEventSchemaRoundTripsAndMateriallyReducesPlayLogStorage()
    {
        var name = $"compact-events-{Guid.NewGuid():N}.json"; var saves = new GameCoreSaveService(); var context = ContextWithEvents();
        try
        {
            var verboseBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(context.ActiveLeague));
            Assert.True(saves.Save(context, name).Ok);
            var compactBytes = saves.GetStorageInfo(name).PrimaryBytes;
            var loaded = saves.Load(name); Assert.True(loaded.Ok, loaded.Message);
            var events = loaded.League.Results[0].BoxScore.PlayByPlay;

            Assert.Equal(200, events.Count);
            Assert.Equal("Pass complete", events[99].Description);
            Assert.Equal(17, events[99].YardsGained);
            Assert.True(events[99].IsFirstDown);
            Assert.Equal(17, events[99].StatChanges[0].PassingYards);
            Console.WriteLine($"Synthetic play-log save: compact={compactBytes} bytes, verbose={verboseBytes} bytes, ratio={(double)compactBytes / verboseBytes:P1}.");
            Assert.True(compactBytes < verboseBytes * 0.70, $"Expected compact save below 70% of verbose size; compact={compactBytes}, verbose={verboseBytes}.");
        }
        finally { saves.Delete(name); }
    }

    [Fact]
    public void VerboseLegacyPlayEventsRemainLoadable()
    {
        var name = $"verbose-events-{Guid.NewGuid():N}.json"; var saves = new GameCoreSaveService(); var context = ContextWithEvents();
        try
        {
            var path = AbsolutePath(name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(context.ActiveLeague), Encoding.UTF8);
            var loaded = saves.Load(name);
            Assert.True(loaded.Ok, loaded.Message);
            Assert.Equal(200, loaded.League.Results[0].BoxScore.PlayByPlay.Count);
            Assert.Equal("qb", loaded.League.Results[0].BoxScore.PlayByPlay[0].ParticipantIds[0]);
            Assert.Equal(1, loaded.League.Results[0].BoxScore.PlayByPlay[0].StatChanges[0].PassAttempts);
        }
        finally { saves.Delete(name); }
    }

    private static GameCoreContext ContextWithEvents()
    {
        var events = new List<GamePlayEventState>();
        for (var index = 0; index < 200; index++)
            events.Add(new GamePlayEventState
            {
                Sequence = index + 1, Quarter = (index / 50) + 1, ClockSeconds = 900 - (index % 50) * 12,
                PossessionTeamId = "home", OffensiveTeamId = "home", Down = 1, Distance = 10, YardLine = 40,
                StartYardLine = 40, StartDown = 1, StartDistance = 10, StartClockSeconds = 900,
                YardsGained = 17, ElapsedSeconds = 6, Description = "Pass complete", PlayType = "pass", Outcome = "complete",
                HomeScore = 7, IsFirstDown = true, OffensiveCall = "Pass", DefensiveCall = "Balanced",
                ParticipantIds = new() { "qb", "wr" },
                StatChanges = new() { new() { PlayerId = "qb", PlayerName = "Quarterback", TeamId = "home", Position = "QB", PassingYards = 17, PassAttempts = 1, Completions = 1, Snaps = 1 } },
            });
        return new GameCoreContext
        {
            ActiveLeague = new LeagueState
            {
                SaveVersion = LeagueState.CurrentSaveVersion, SeasonYear = 2026, UserTeamId = "home",
                Teams = new() { new() { TeamId = "home", Name = "Home" }, new() { TeamId = "away", Name = "Away" } },
                Results = new() { new() { GameId = "game", HomeTeamId = "home", AwayTeamId = "away", BoxScore = new BoxScoreState { PlayByPlay = events } } },
            },
        };
    }

    private static string AbsolutePath(string name)
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GridironGM", "saves", Path.GetFileName(name));
}
