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
    public int Snaps { get; set; }
    public int PassAttempts { get; set; }
    public int Completions { get; set; }
    public int InterceptionsThrown { get; set; }
    public int SacksTaken { get; set; }
    public int SackYardsLost { get; set; }
    public int RushAttempts { get; set; }
    public int Receptions { get; set; }
    public int FumblesLost { get; set; }
    public int FieldGoalAttempts { get; set; }
    public int FieldGoalsMade { get; set; }
    public int ExtraPointAttempts { get; set; }
    public int ExtraPointsMade { get; set; }
    public int TwoPointConversions { get; set; }
    public int Punts { get; set; }
    public int PuntYards { get; set; }

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
        Snaps += gameStats.Snaps;
        PassAttempts += gameStats.PassAttempts;
        Completions += gameStats.Completions;
        InterceptionsThrown += gameStats.InterceptionsThrown;
        SacksTaken += gameStats.SacksTaken;
        SackYardsLost += gameStats.SackYardsLost;
        RushAttempts += gameStats.RushAttempts;
        Receptions += gameStats.Receptions;
        FumblesLost += gameStats.FumblesLost;
        FieldGoalAttempts += gameStats.FieldGoalAttempts;
        FieldGoalsMade += gameStats.FieldGoalsMade;
        ExtraPointAttempts += gameStats.ExtraPointAttempts;
        ExtraPointsMade += gameStats.ExtraPointsMade;
        TwoPointConversions += gameStats.TwoPointConversions;
        Punts += gameStats.Punts;
        PuntYards += gameStats.PuntYards;
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
        Snaps = Snaps,
        PassAttempts = PassAttempts,
        Completions = Completions,
        InterceptionsThrown = InterceptionsThrown,
        SacksTaken = SacksTaken,
        SackYardsLost = SackYardsLost,
        RushAttempts = RushAttempts,
        Receptions = Receptions,
        FumblesLost = FumblesLost,
        FieldGoalAttempts = FieldGoalAttempts,
        FieldGoalsMade = FieldGoalsMade,
        ExtraPointAttempts = ExtraPointAttempts,
        ExtraPointsMade = ExtraPointsMade,
        TwoPointConversions = TwoPointConversions,
        Punts = Punts,
        PuntYards = PuntYards,
    };
}
