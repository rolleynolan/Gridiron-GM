using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class CollegePostseasonProjectionResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int SeasonYear { get; set; }
    public string RuleVersion { get; set; } = "";
    public List<CollegeProjectedTeam> FirstRoundByes { get; set; } = new();
    public List<CollegeProjectedMatchup> PlayoffMatchups { get; set; } = new();
    public List<CollegeProjectedMatchup> BowlMatchups { get; set; } = new();
}

public sealed class CollegeProjectedMatchup
{
    public string Label { get; set; } = "";
    public CollegeProjectedTeam Home { get; set; } = new();
    public CollegeProjectedTeam Away { get; set; } = new();
}

public sealed class CollegeProjectedTeam
{
    public int Ranking { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public string SelectionReason { get; set; } = "";
}
