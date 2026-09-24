namespace GridironGM.GameCore.Models;

public sealed class PlayerGameStats
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string Position { get; set; } = "";
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PassingYards { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PassingTouchdowns { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int RushingYards { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int RushingTouchdowns { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int ReceivingYards { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int ReceivingTouchdowns { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Tackles { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Sacks { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Interceptions { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Snaps { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PassAttempts { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Completions { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int InterceptionsThrown { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int SacksTaken { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int SackYardsLost { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int RushAttempts { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Receptions { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int FumblesLost { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int FieldGoalAttempts { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int FieldGoalsMade { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int ExtraPointAttempts { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int ExtraPointsMade { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int TwoPointConversions { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int Punts { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int PuntYards { get; set; }

    public PlayerGameStats Copy() => (PlayerGameStats)MemberwiseClone();

    public void Add(PlayerGameStats other)
    {
        PassingYards += other.PassingYards;
        PassingTouchdowns += other.PassingTouchdowns;
        RushingYards += other.RushingYards;
        RushingTouchdowns += other.RushingTouchdowns;
        ReceivingYards += other.ReceivingYards;
        ReceivingTouchdowns += other.ReceivingTouchdowns;
        Tackles += other.Tackles;
        Sacks += other.Sacks;
        Interceptions += other.Interceptions;
        Snaps += other.Snaps;
        PassAttempts += other.PassAttempts;
        Completions += other.Completions;
        InterceptionsThrown += other.InterceptionsThrown;
        SacksTaken += other.SacksTaken;
        SackYardsLost += other.SackYardsLost;
        RushAttempts += other.RushAttempts;
        Receptions += other.Receptions;
        FumblesLost += other.FumblesLost;
        FieldGoalAttempts += other.FieldGoalAttempts;
        FieldGoalsMade += other.FieldGoalsMade;
        ExtraPointAttempts += other.ExtraPointAttempts;
        ExtraPointsMade += other.ExtraPointsMade;
        TwoPointConversions += other.TwoPointConversions;
        Punts += other.Punts;
        PuntYards += other.PuntYards;
    }
}
