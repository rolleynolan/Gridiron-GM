using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class TeamState
{
    public string TeamId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Division { get; set; } = "";
    public string Conference { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public decimal CapRoom { get; set; }
    public List<PlayerState> Roster { get; set; } = new();
    public List<CoachState> Coaches { get; set; } = new();
    public Dictionary<string, List<string>> DepthChart { get; set; } = new();
}
