using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class SeasonHistoryRecord
{
    public int SeasonYear { get; set; }
    public string CompletedPhaseLabel { get; set; } = "";
    public string ChampionTeamId { get; set; } = "";
    public string ChampionTeamName { get; set; } = "";
    public string RunnerUpTeamId { get; set; } = "";
    public string RunnerUpTeamName { get; set; } = "";
    public int ChampionshipWinnerScore { get; set; }
    public int ChampionshipRunnerUpScore { get; set; }
    public string ChampionshipGameLabel { get; set; } = "";
    public List<SeasonTeamRecord> TeamRecords { get; set; } = new();
    public List<SeasonPlayoffSeedRecord> PlayoffSeeds { get; set; } = new();
    public List<SeasonPlayoffResultRecord> PlayoffResults { get; set; } = new();
    public int TotalRegularSeasonGames { get; set; }
    public int TotalPlayoffGames { get; set; }
    public string GeneratedAtLabel { get; set; } = "";
}

public sealed class SeasonTeamRecord
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Conference { get; set; } = "";
    public string Division { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public double WinPercentage { get; set; }
}

public sealed class SeasonPlayoffSeedRecord
{
    public string Conference { get; set; } = "";
    public int Seed { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Division { get; set; } = "";
    public bool IsDivisionWinner { get; set; }
}

public sealed class SeasonPlayoffResultRecord
{
    public string Round { get; set; } = "";
    public string Conference { get; set; } = "";
    public string HomeTeamId { get; set; } = "";
    public string HomeTeamName { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string AwayTeamName { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string WinnerTeamId { get; set; } = "";
    public string WinnerTeamName { get; set; } = "";
    public string LoserTeamId { get; set; } = "";
    public string LoserTeamName { get; set; } = "";
}
