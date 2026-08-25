using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class TrainingCampReportState
{
    public string Summary { get; set; } = "";
    public string RecommendedFocusPosition { get; set; } = "";
    public List<TrainingCampPositionReport> Positions { get; set; } = new();
}

public sealed class TrainingCampPositionReport
{
    public string Position { get; set; } = "";
    public int RequiredStarters { get; set; }
    public int AvailablePlayers { get; set; }
    public int UnavailablePlayers { get; set; }
    public int AverageOverall { get; set; }
    public int AveragePotential { get; set; }
    public int AverageFatigue { get; set; }
    public string Recommendation { get; set; } = "";
}
