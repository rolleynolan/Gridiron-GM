using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class TrainingCampState
{
    public string FocusPosition { get; set; } = "";
    public bool FocusApplied { get; set; }
    public string FocusPlayerId { get; set; } = "";
    public string FocusPlayerName { get; set; } = "";
    public bool PlayerFocusApplied { get; set; }
    public bool RosterFinalized { get; set; }
    public string Summary { get; set; } = "";
    public TrainingCampReportState Report { get; set; } = new();
    public List<string> UserAdjustedPositions { get; set; } = new();
    public List<PositionBattleOutcome> PositionBattles { get; set; } = new();
}
