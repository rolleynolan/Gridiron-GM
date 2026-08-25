namespace GridironGM.GameCore.Models;

public sealed class PlayerInjuryState
{
    public string Name { get; set; } = "";
    public int DaysRemaining { get; set; }
    public string OccurredOn { get; set; } = "";
    public string GameId { get; set; } = "";

    public bool IsActive => !string.IsNullOrWhiteSpace(Name) && DaysRemaining > 0;
}
