using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class LeagueState
{
    public const int CurrentSaveVersion = 7;

    public string LeagueId { get; set; } = "test_league";
    public string Name { get; set; } = "Test League";
    public int SaveVersion { get; set; } = CurrentSaveVersion;
    public int SeasonYear { get; set; } = 2026;
    public string UserTeamId { get; set; } = "";
    public CalendarState Calendar { get; set; } = new();
    public List<TeamState> Teams { get; set; } = new();
    public List<ScheduledGame> Schedule { get; set; } = new();
    public List<GameResult> Results { get; set; } = new();
    public PlayoffBracket PlayoffBracket { get; set; } = new();
    public List<SeasonHistoryRecord> HistoricalSeasons { get; set; } = new();
    public List<SeasonRetirementRecord> RetirementHistory { get; set; } = new();
    public ContinueResult LastContinueResult { get; set; } = new();
}
