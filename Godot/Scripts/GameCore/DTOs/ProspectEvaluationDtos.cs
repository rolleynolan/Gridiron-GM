namespace GridironGM.GameCore.DTOs;

public sealed class ProspectEvaluationDto
{
    public string ProspectId { get; set; } = "";
    public string KnownFacts { get; set; } = "";
    public string EstimatedOverall { get; set; } = "";
    public string EstimatedPotential { get; set; } = "";
    public string Confidence { get; set; } = "";
    public string Report { get; set; } = "";
    public string Trait { get; set; } = "";
    public string Interview { get; set; } = "";
}
