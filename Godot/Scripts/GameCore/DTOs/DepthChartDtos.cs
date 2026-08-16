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
    public List<DepthChartPlayerDto> Players { get; set; } = new();
}

public sealed class DepthChartPlayerDto
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Overall { get; set; }
    public string Status { get; set; } = "";
    public string Injury { get; set; } = "";
    public string Role { get; set; } = "";
}
