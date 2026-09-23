namespace GridironGM.GameCore.Models;

public sealed class WaiverClaimState
{
    public PlayerState Player { get; set; } = new();
    public string WaivedByTeamId { get; set; } = "";
    public int SeasonYear { get; set; }
    public int ExpiresAbsoluteWeek { get; set; }
    public string PendingClaimTeamId { get; set; } = "";
    public bool PendingConfirmation { get; set; }
    public string ConditionalReleasePlayerId { get; set; } = "";
    public System.Collections.Generic.List<string> DeclinedTeamIds { get; set; } = new();
    public System.Collections.Generic.List<WaiverClaimEntryState> Claims { get; set; } = new();
}

public sealed class WaiverClaimEntryState
{
    public string TeamId { get; set; } = "";
    public string ConditionalReleasePlayerId { get; set; } = "";
}
