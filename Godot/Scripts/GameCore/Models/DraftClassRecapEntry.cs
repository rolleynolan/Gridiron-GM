namespace GridironGM.GameCore.Models;

// Immutable snapshot of the information available when a prospect became a rookie.
public sealed class DraftClassRecapEntry
{
    public int OverallPick { get; set; }
    public int Round { get; set; }
    public int PickInRound { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string ProspectId { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public string College { get; set; } = "";
    public int Age { get; set; }
    public int ScoutedOverall { get; set; }
    public int ScoutedPotential { get; set; }
    public int ScoutingConfidence { get; set; }
    public int CombineScore { get; set; }
    public int ProDayScore { get; set; }
    public string ScoutingReport { get; set; } = "";
    public string Trait { get; set; } = "";
    public string InterviewSummary { get; set; } = "";
    public int PublicBoardRank { get; set; }
    public string PublicReaction { get; set; } = "";
    public string RookiePlacement { get; set; } = "";
    public decimal ContractAnnualSalary { get; set; }
    public decimal ContractGuaranteedSalary { get; set; }
    public int ContractYears { get; set; }
    public string ContractType { get; set; } = "";
}
