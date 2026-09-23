using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class FrontOfficeEvaluationResponse
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public int RosterSize { get; set; }
    public decimal CapRoom { get; set; }
    public int AverageAge { get; set; }
    public int AveragePotential { get; set; }
    public int ExpiringContracts { get; set; }
    public int DraftPicksAvailable { get; set; }
    public List<string> PositionNeeds { get; set; } = new();
    public string Rationale { get; set; } = "";
}
