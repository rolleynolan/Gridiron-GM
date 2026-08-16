namespace GridironGM.GameCore.Models;

public sealed class GameResult
{
    public string GameId { get; set; } = "";
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string GameType { get; set; } = "regular_season";
    public string WeekLabel { get; set; } = "";
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Winner { get; set; } = "";
    public string Summary { get; set; } = "";
    public BoxScoreState BoxScore { get; set; } = new();
}
