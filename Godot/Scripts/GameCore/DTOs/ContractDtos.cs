namespace GridironGM.GameCore.DTOs;

public sealed class PlayerReleasePreviewDto
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";
    public string ContractType { get; set; } = "";
    public int YearsRemaining { get; set; }
    public decimal AnnualSalary { get; set; }
    public decimal GuaranteedSalary { get; set; }
    public decimal PayrollBefore { get; set; }
    public decimal PayrollAfter { get; set; }
    public decimal CapRoomBefore { get; set; }
    public decimal CapRoomAfter { get; set; }
    public int RosterCountBefore { get; set; }
    public int RosterCountAfter { get; set; }
}

public sealed class PracticeSquadActiveSigningPreviewDto
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";
    public string CurrentContractType { get; set; } = "";
    public string NewContractType { get; set; } = "Active Roster";
    public decimal CurrentAnnualSalary { get; set; }
    public decimal NewAnnualSalary { get; set; }
    public int RosterCountBefore { get; set; }
    public int RosterCountAfter { get; set; }
    public decimal CapRoomBefore { get; set; }
    public decimal CapRoomAfter { get; set; }
}
