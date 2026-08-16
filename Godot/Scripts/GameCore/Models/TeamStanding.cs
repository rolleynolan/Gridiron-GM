namespace GridironGM.GameCore.Models;

public sealed class TeamStanding
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public double WinPct { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public int PointDifferential { get; set; }
    public string Division { get; set; } = "";
    public string Conference { get; set; } = "";
}
