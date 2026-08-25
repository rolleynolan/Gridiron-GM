using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class TrainingCampDecisionResponse
{
    public bool Ok { get; set; }
    public bool Completed { get; set; }
    public string Message { get; set; } = "";
    public TrainingCampStatusDto Status { get; set; } = new();
}

public sealed class TrainingCampStatusDto
{
    public bool IsAvailable { get; set; }
    public bool FocusApplied { get; set; }
    public bool RosterFinalized { get; set; }
    public string FocusPosition { get; set; } = "";
    public string Summary { get; set; } = "";
    public TrainingCampReportDto Report { get; set; } = new();
}

public sealed class TrainingCampReportDto
{
    public string Summary { get; set; } = "";
    public string RecommendedFocusPosition { get; set; } = "";
    public List<TrainingCampPositionReportDto> Positions { get; set; } = new();
}

public sealed class TrainingCampPositionReportDto
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

public sealed class ContractPhaseStatusDto
{
    public bool CanSignFreeAgents { get; set; }
    public bool CanOfferExtensions { get; set; }
    public bool CanApplyFranchiseTag { get; set; }
    public bool CanManageRoster { get; set; }
    public string Explanation { get; set; } = "";
}
