namespace GridironGM.GameCore.Models;

public sealed class CollegeProspectState
{
    public string ProspectId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public string College { get; set; } = "";
    public int Overall { get; set; }
    public int Potential { get; set; }
    public int Age { get; set; }
    public int DraftClassYear { get; set; }
    public int ScoutedOverall { get; set; }
    public int ScoutedPotential { get; set; }
    public string DraftedByTeamId { get; set; } = "";
}
