namespace GridironGM.GameCore.Models;

public sealed class PlayerSeasonStats
{
    public int SeasonYear { get; set; }
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int PassingTouchdowns { get; set; }
    public int RushingYards { get; set; }
    public int RushingTouchdowns { get; set; }
    public int ReceivingYards { get; set; }
    public int ReceivingTouchdowns { get; set; }
    public int Tackles { get; set; }
    public int Sacks { get; set; }
    public int Interceptions { get; set; }

    public void Add(PlayerGameStats gameStats)
    {
        GamesPlayed++;
        PassingYards += gameStats.PassingYards;
        PassingTouchdowns += gameStats.PassingTouchdowns;
        RushingYards += gameStats.RushingYards;
        RushingTouchdowns += gameStats.RushingTouchdowns;
        ReceivingYards += gameStats.ReceivingYards;
        ReceivingTouchdowns += gameStats.ReceivingTouchdowns;
        Tackles += gameStats.Tackles;
        Sacks += gameStats.Sacks;
        Interceptions += gameStats.Interceptions;
    }

    public PlayerSeasonStats Copy() => new()
    {
        SeasonYear = SeasonYear,
        GamesPlayed = GamesPlayed,
        PassingYards = PassingYards,
        PassingTouchdowns = PassingTouchdowns,
        RushingYards = RushingYards,
        RushingTouchdowns = RushingTouchdowns,
        ReceivingYards = ReceivingYards,
        ReceivingTouchdowns = ReceivingTouchdowns,
        Tackles = Tackles,
        Sacks = Sacks,
        Interceptions = Interceptions,
    };
}
