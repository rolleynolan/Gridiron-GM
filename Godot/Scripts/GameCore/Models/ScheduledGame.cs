namespace GridironGM.GameCore.Models;

public sealed class ScheduledGame
{
    public string GameId { get; set; } = "";
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public int DayIndex { get; set; } = 2;
    public string GameType { get; set; } = "preseason";
    public string WeekLabel { get; set; } = "";
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string Status { get; set; } = "upcoming";
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Winner { get; set; } = "";
}
