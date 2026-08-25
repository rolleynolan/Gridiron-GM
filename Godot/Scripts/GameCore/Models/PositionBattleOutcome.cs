namespace GridironGM.GameCore.Models;

public sealed class PositionBattleOutcome
{
    public string Position { get; set; } = "";
    public string WinnerPlayerId { get; set; } = "";
    public string WinnerName { get; set; } = "";
    public string RunnerUpName { get; set; } = "";
    public string Explanation { get; set; } = "";
}
