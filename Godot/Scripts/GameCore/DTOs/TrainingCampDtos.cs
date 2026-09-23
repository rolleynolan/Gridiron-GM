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
    public bool PlayerFocusApplied { get; set; }
    public bool RosterFinalized { get; set; }
    public string FocusPosition { get; set; } = "";
    public string FocusPlayerId { get; set; } = "";
    public string FocusPlayerName { get; set; } = "";
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

public sealed class TrainingCampCutPreviewDto
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public List<string> PlayerIds { get; set; } = new();
    public List<string> PlayerNames { get; set; } = new();
    public List<string> PositionWarnings { get; set; } = new();
    public int RosterCountBefore { get; set; }
    public int RosterCountAfter { get; set; }
    public int RequiredCutsBefore { get; set; }
    public int RequiredCutsAfter { get; set; }
    public decimal PayrollBefore { get; set; }
    public decimal PayrollAfter { get; set; }
    public decimal CapRoomBefore { get; set; }
    public decimal CapRoomAfter { get; set; }
}

public sealed class ContractPhaseStatusDto
{
    public bool CanSignFreeAgents { get; set; }
    public bool CanOfferExtensions { get; set; }
    public bool CanApplyFranchiseTag { get; set; }
    public bool CanManageRoster { get; set; }
    public string Explanation { get; set; } = "";
}
