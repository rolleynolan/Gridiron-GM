using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

// College simulation is an independent, read-only feeder system. It never owns pro rosters or transactions.
public sealed class CollegeUniverseState
{
    public int SeasonYear { get; set; }
    public int LastAdvancedAbsoluteWeek { get; set; }
    public List<CollegeTeamState> Teams { get; set; } = new();
    public List<CollegePlayerState> Players { get; set; } = new();
    public List<CollegeScheduledGame> Schedule { get; set; } = new();
    public List<CollegeGameResult> Results { get; set; } = new();
    public CollegePostseasonState Postseason { get; set; } = new();
    public List<CollegeSeasonAwardRecord> Awards { get; set; } = new();
    public bool DraftClassFinalized { get; set; }
}

public sealed class CollegePostseasonState
{
    public bool Completed { get; set; }
    public List<CollegePostseasonGame> Games { get; set; } = new();
}

public sealed class CollegePostseasonGame
{
    public string Label { get; set; } = "";
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string WinnerTeamId { get; set; } = "";
}

public sealed class CollegeSeasonArchiveRecord
{
    public int SeasonYear { get; set; }
    public string ChampionTeamId { get; set; } = "";
    public string ChampionTeamName { get; set; } = "";
    public List<CollegeTeamSeasonRecord> TeamRecords { get; set; } = new();
    public List<CollegeSeasonAwardRecord> Awards { get; set; } = new();
    public List<CollegePostseasonGame> PostseasonGames { get; set; } = new();
}

public sealed class CollegeTeamSeasonRecord
{
    public int SeasonYear { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Conference { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int FinalRanking { get; set; }
    public bool WonChampionship { get; set; }
}

// Immutable award snapshots are created only after the completed college schedule supplies authoritative statistics.
public sealed class CollegeSeasonAwardRecord
{
    public string AwardName { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Position { get; set; } = "";
    public int Score { get; set; }
    public string Summary { get; set; } = "";
}

public sealed class CollegeTeamState
{
    public string TeamId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Conference { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ranking { get; set; }
}

public sealed class CollegePlayerState
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string Position { get; set; } = "";
    public int Overall { get; set; }
    public int Potential { get; set; }
    public int Age { get; set; }
    public int ClassYear { get; set; }
    public bool DraftEligible { get; set; }
    public string DraftDecision { get; set; } = "Pending";
    public string DraftDecisionReason { get; set; } = "";
    public string DraftStock { get; set; } = "Undeclared";
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int RushingYards { get; set; }
    public int ReceivingYards { get; set; }
    public int Touchdowns { get; set; }
    public List<CollegePlayerSeasonStats> CareerStats { get; set; } = new();
    public List<CollegePlayerDevelopmentRecord> DevelopmentHistory { get; set; } = new();
    public CollegePlayerInjuryState CurrentInjury { get; set; } = new();
    public List<CollegePlayerInjuryRecord> InjuryHistory { get; set; } = new();
}

public sealed class CollegePlayerSeasonStats
{
    public int SeasonYear { get; set; }
    public string TeamId { get; set; } = "";
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int RushingYards { get; set; }
    public int ReceivingYards { get; set; }
    public int Touchdowns { get; set; }
}

public sealed class CollegePlayerInjuryState
{
    public string Name { get; set; } = "";
    public int WeeksRemaining { get; set; }
    public int OccurredInWeek { get; set; }
    public string GameId { get; set; } = "";
    public bool IsActive => !string.IsNullOrWhiteSpace(Name) && WeeksRemaining > 0;
}

public sealed class CollegePlayerInjuryRecord
{
    public int SeasonYear { get; set; }
    public string Name { get; set; } = "";
    public int WeeksOut { get; set; }
    public int OccurredInWeek { get; set; }
    public int RecoveredInWeek { get; set; }
    public string GameId { get; set; } = "";
}

public sealed class CollegePlayerDevelopmentRecord
{
    public int SeasonYear { get; set; }
    public int OverallBefore { get; set; }
    public int OverallAfter { get; set; }
    public string Reason { get; set; } = "";
}

public sealed class CollegeScheduledGame
{
    public string GameId { get; set; } = "";
    public int ProAbsoluteWeek { get; set; }
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string Status { get; set; } = "upcoming";
}

public sealed class CollegeGameResult
{
    public string GameId { get; set; } = "";
    public int ProAbsoluteWeek { get; set; }
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string WinnerTeamId { get; set; } = "";
}
