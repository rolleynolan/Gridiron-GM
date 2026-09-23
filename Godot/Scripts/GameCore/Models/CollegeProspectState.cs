namespace GridironGM.GameCore.Models;

public sealed class CollegeProspectState
{
    public string ProspectId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public string College { get; set; } = "";
    public string CollegeTeamId { get; set; } = "";
    public string CollegePlayerId { get; set; } = "";
    public int Overall { get; set; }
    public int Potential { get; set; }
    public int Age { get; set; }
    public int DraftClassYear { get; set; }
    public string DeclarationStatus { get; set; } = "Declared";
    public string DeclarationRationale { get; set; } = "";
    public string DraftStock { get; set; } = "Season outlook pending";
    public int ScoutedOverall { get; set; }
    public int ScoutedPotential { get; set; }
    public int ScoutingConfidence { get; set; }
    public int CombineScore { get; set; }
    public int ProDayScore { get; set; }
    public string ScoutingReport { get; set; } = "";
    public string Trait { get; set; } = "";
    public string InterviewSummary { get; set; } = "";
    public string DraftedByTeamId { get; set; } = "";
}
