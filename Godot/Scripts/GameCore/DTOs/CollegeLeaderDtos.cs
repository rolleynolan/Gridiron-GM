using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class CollegeLeadersResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int SeasonYear { get; set; }
    public List<CollegeLeaderCategory> Categories { get; set; } = new();
}

public sealed class CollegeLeaderCategory
{
    public string Name { get; set; } = "";
    public string StatLabel { get; set; } = "";
    public List<CollegeLeaderEntry> Leaders { get; set; } = new();
}

public sealed class CollegeLeaderEntry
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";
    public string TeamName { get; set; } = "";
    public int Value { get; set; }
}
