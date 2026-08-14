using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class SeasonRetirementRecord
{
    public int SeasonYear { get; set; }
    public string ProcessedPhase { get; set; } = "";
    public bool Completed { get; set; }
    public int RetiredCount { get; set; }
    public List<PlayerRetirementRecord> Players { get; set; } = new();
}

public sealed class PlayerRetirementRecord
{
    public int SeasonYear { get; set; }
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Position { get; set; } = "";
    public int Age { get; set; }
    public int Overall { get; set; }
    public string ReasonLabel { get; set; } = "";
    public string RetiredDuringPhase { get; set; } = "";
}
