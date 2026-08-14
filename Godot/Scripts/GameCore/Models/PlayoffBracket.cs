using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class PlayoffBracket
{
    public int SeasonYear { get; set; }
    public int GeneratedFromAbsoluteWeek { get; set; }
    public string GeneratedAtPhaseLabel { get; set; } = "";
    public List<PlayoffConferenceBracket> ConferenceBrackets { get; set; } = new();
    public PlayoffRound LeagueChampionshipRound { get; set; } = new();
    public LeagueChampionRecord LeagueChampionRecord { get; set; } = new();
}

public sealed class PlayoffConferenceBracket
{
    public string Conference { get; set; } = "";
    public List<PlayoffSeed> Seeds { get; set; } = new();
    public List<PlayoffRound> Rounds { get; set; } = new();
}

public sealed class PlayoffRound
{
    public string Round { get; set; } = "";
    public string Status { get; set; } = "scheduled";
    public List<PlayoffGame> Games { get; set; } = new();
}

public sealed class PlayoffSeed
{
    public int Seed { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Conference { get; set; } = "";
    public string Division { get; set; } = "";
    public bool IsDivisionWinner { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public double WinPercentage { get; set; }
    public int PointDifferential { get; set; }
    public int PointsFor { get; set; }
}

public sealed class PlayoffGame
{
    public string GameId { get; set; } = "";
    public string Round { get; set; } = "";
    public string RoundLabel { get; set; } = "";
    public string Conference { get; set; } = "";
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "Playoffs";
    public string GameType { get; set; } = "playoffs";
    public int HomeSeed { get; set; }
    public int AwaySeed { get; set; }
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string HomeTeamName { get; set; } = "";
    public string AwayTeamName { get; set; } = "";
    public bool NeutralSite { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = "scheduled";
    public string WinnerTeamId { get; set; } = "";
    public string LoserTeamId { get; set; } = "";
}

public sealed class LeagueChampionRecord
{
    public int SeasonYear { get; set; }
    public string ChampionTeamId { get; set; } = "";
    public string ChampionTeamName { get; set; } = "";
    public string RunnerUpTeamId { get; set; } = "";
    public string RunnerUpTeamName { get; set; } = "";
    public string ChampionshipHomeTeamId { get; set; } = "";
    public string ChampionshipAwayTeamId { get; set; } = "";
    public int ChampionScore { get; set; }
    public int RunnerUpScore { get; set; }
    public string CompletedPhaseLabel { get; set; } = "";
}
