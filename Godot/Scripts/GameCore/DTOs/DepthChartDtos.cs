using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class TeamDepthChartResponse
{
    public bool Ok { get; set; }
    public TeamIdentityDto Team { get; set; }
    public DepthChartStatusDto DepthChartStatus { get; set; }
    public List<DepthChartPositionDto> Positions { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class DepthChartStatusDto
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new();
}

public sealed class DepthChartPositionDto
{
    public string Position { get; set; } = "";
    public int RequiredStarters { get; set; }
    public bool IsLocked { get; set; }
    public List<DepthChartPlayerDto> Players { get; set; } = new();
}

public sealed class DepthChartPlayerDto
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Overall { get; set; }
    public int EstimatedOverall { get; set; }
    public string EstimatedOverallRange { get; set; } = "";
    public string ScoutingConfidence { get; set; } = "Low";
    public string Status { get; set; } = "";
    public string Injury { get; set; } = "";
    public int InjuryDaysRemaining { get; set; }
    public bool IsAvailable { get; set; }
    public string Role { get; set; } = "";
    public string ContractSummary { get; set; } = "";
    public int Morale { get; set; }
    public string MoraleTrend { get; set; } = "";
    public int Potential { get; set; }
    public int PassingYards { get; set; }
    public int RushingYards { get; set; }
    public int ReceivingYards { get; set; }
    public int Tackles { get; set; }
    public int Sacks { get; set; }
}
