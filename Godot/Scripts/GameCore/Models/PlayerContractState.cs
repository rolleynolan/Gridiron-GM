namespace GridironGM.GameCore.Models;

public sealed class PlayerContractState
{
    public decimal AnnualSalary { get; set; }
    public decimal GuaranteedSalary { get; set; }
    public int YearsRemaining { get; set; }
    public int SignedSeason { get; set; }
    public string ContractType { get; set; } = "Standard";
}

public sealed class ContractOffer
{
    public decimal AnnualSalary { get; set; }
    public decimal GuaranteedSalary { get; set; }
    public int Years { get; set; }
}

public sealed class ContractTransactionResult
{
    public bool Ok { get; set; }
    public bool Accepted { get; set; }
    public string Message { get; set; } = "";
    public decimal RequiredAnnualSalary { get; set; }
    public decimal CapRoomAfterSigning { get; set; }
}
