namespace GridironGM.GameCore.Models;

public sealed class PlayerInjuryRecord
{
    public int SeasonYear { get; set; }
    public string Name { get; set; } = "";
    public int DaysOut { get; set; }
    public string OccurredOn { get; set; } = "";
    public string GameId { get; set; } = "";
    public string RecoveredOn { get; set; } = "";
}
