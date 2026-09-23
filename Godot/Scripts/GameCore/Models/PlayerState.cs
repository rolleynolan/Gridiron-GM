using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class PlayerState
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public int Overall { get; set; }
    public int Potential { get; set; }
    public int Age { get; set; }
    public int Fatigue { get; set; }
    public string Status { get; set; } = "Active";
    public string Injury { get; set; } = "";
    public PlayerInjuryState CurrentInjury { get; set; } = new();
    public List<PlayerInjuryRecord> InjuryHistory { get; set; } = new();
    public int Morale { get; set; } = 50;
    public string MoraleTrend { get; set; } = "Stable";
    public string Trait { get; set; } = "";
    public PlayerContractState Contract { get; set; } = new();
    public PlayerSeasonStats SeasonStats { get; set; } = new();
    public List<PlayerSeasonStats> CareerStats { get; set; } = new();
    public List<PlayerDevelopmentRecord> DevelopmentHistory { get; set; } = new();
}
