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
    public int InjuredReserveCount { get; set; }
    public int PracticeSquadCount { get; set; }
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
    public int Potential { get; set; }
    public int EstimatedOverall { get; set; }
    public int EstimatedPotential { get; set; }
    public string EstimatedOverallRange { get; set; } = "";
    public string EstimatedPotentialRange { get; set; } = "";
    public string ScoutingConfidence { get; set; } = "Low";
    public int Age { get; set; }
    public int Fatigue { get; set; }
    public string Status { get; set; } = "";
    public string Injury { get; set; } = "";
    public int InjuryDaysRemaining { get; set; }
    public bool IsAvailable { get; set; }
    public string DepthRole { get; set; } = "";
    public string ContractSummary { get; set; } = "";
    public decimal AnnualSalary { get; set; }
    public int ContractYearsRemaining { get; set; }
    public string MoraleTrend { get; set; } = "";
    public int Morale { get; set; }
    public string Trait { get; set; } = "";
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int RushingYards { get; set; }
    public int ReceivingYards { get; set; }
    public int Tackles { get; set; }
    public int Sacks { get; set; }
    public int Interceptions { get; set; }
}
