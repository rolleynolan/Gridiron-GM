namespace GridironGM.GameCore.Models;

public sealed class PlayerMedicalReport
{
    public string Diagnosis { get; init; } = "";
    public string EvaluationStatus { get; init; } = "";
    public string Treatment { get; init; } = "";
    public string ClearanceStatus { get; init; } = "";
    public string EstimatedClearanceWindow { get; init; } = "";
    public string EstimateContext { get; init; } = "";
    public string Restrictions { get; init; } = "";
    public string ReportedOn { get; init; } = "";
    public bool HasActiveInjury { get; init; }
}
