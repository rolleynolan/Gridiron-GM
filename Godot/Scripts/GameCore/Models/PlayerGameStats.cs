namespace GridironGM.GameCore.Models;

public sealed class PlayerGameStats
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string Position { get; set; } = "";
    public int PassingYards { get; set; }
    public int PassingTouchdowns { get; set; }
    public int RushingYards { get; set; }
    public int RushingTouchdowns { get; set; }
    public int ReceivingYards { get; set; }
    public int ReceivingTouchdowns { get; set; }
    public int Tackles { get; set; }
    public int Sacks { get; set; }
    public int Interceptions { get; set; }
}
