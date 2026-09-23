namespace GridironGM.GameCore.Models;

public sealed class GamePlayEventState
{
    public int Sequence { get; set; }
    public int Quarter { get; set; }
    public int ClockSeconds { get; set; }
    public string PossessionTeamId { get; set; } = "";
    public int Down { get; set; }
    public int Distance { get; set; }
    public int YardLine { get; set; }
    public int YardsGained { get; set; }
    public string Description { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public bool IsScoringPlay { get; set; }
    public bool IsTurnover { get; set; }
    public bool IsInjury { get; set; }
}
