using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class TeamRosterResponse
{
    public bool Ok { get; set; }
    public TeamIdentityDto Team { get; set; }
    public RosterStatusDto RosterStatus { get; set; }
    public List<PositionCountDto> PositionCounts { get; set; } = new();
    public List<PlayerRowDto> Players { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class RosterStatusDto
{
    public bool IsValid { get; set; }
    public int RosterSize { get; set; }
    public int RosterLimit { get; set; }
    public int RequiredCuts { get; set; }
    public int OpenSlots { get; set; }
    public int InjuredCount { get; set; }
    public List<string> Issues { get; set; } = new();
}

public sealed class PositionCountDto
{
    public string Position { get; set; } = "";
    public int Count { get; set; }
}

public sealed class PlayerRowDto
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public int Overall { get; set; }
    public int Age { get; set; }
    public string Status { get; set; } = "";
    public string Injury { get; set; } = "";
    public string DepthRole { get; set; } = "";
}
