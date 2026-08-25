using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class RosterEvaluationResponse
{
    public bool Ok { get; set; }
    public List<PlayerRoleFeedbackDto> Players { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class PlayerRoleFeedbackDto
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public string Role { get; set; } = "";
    public string Readiness { get; set; } = "";
    public string Explanation { get; set; } = "";
}
