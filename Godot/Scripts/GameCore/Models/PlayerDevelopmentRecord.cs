namespace GridironGM.GameCore.Models;

public sealed class PlayerDevelopmentRecord
{
    public int SeasonYear { get; set; }
    public int OverallBefore { get; set; }
    public int OverallAfter { get; set; }
    public string Note { get; set; } = "";
}
