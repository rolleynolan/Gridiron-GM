namespace GridironGM.GameCore.Models;

public sealed class WaiverClaimState
{
    public PlayerState Player { get; set; } = new();
    public string WaivedByTeamId { get; set; } = "";
    public int SeasonYear { get; set; }
    public int ExpiresAbsoluteWeek { get; set; }
}
