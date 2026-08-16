using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class LeagueState
{
    public const int CurrentSaveVersion = 12;
    public const decimal DefaultSalaryCap = 255_000_000m;

    public string LeagueId { get; set; } = "test_league";
    public string Name { get; set; } = "Test League";
    public int SaveVersion { get; set; } = CurrentSaveVersion;
    public int SeasonYear { get; set; } = 2026;
    public decimal SalaryCap { get; set; } = DefaultSalaryCap;
    public int LastContractExpirationSeason { get; set; }
    public string UserTeamId { get; set; } = "";
    public FranchiseMetadata FranchiseMetadata { get; set; } = new();
    public CalendarState Calendar { get; set; } = new();
    public List<TeamState> Teams { get; set; } = new();
    public List<PlayerState> FreeAgents { get; set; } = new();
    public List<CollegeProspectState> CollegeProspects { get; set; } = new();
    public DraftState Draft { get; set; } = new();
    public List<ScheduledGame> Schedule { get; set; } = new();
    public List<GameResult> Results { get; set; } = new();
    public PlayoffBracket PlayoffBracket { get; set; } = new();
    public List<SeasonHistoryRecord> HistoricalSeasons { get; set; } = new();
    public List<SeasonRetirementRecord> RetirementHistory { get; set; } = new();
    public ContinueResult LastContinueResult { get; set; } = new();
}
