using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class ContinueResult
{
    public bool Advanced { get; set; }
    public string StopReason { get; set; } = "";
    public int DaysAdvanced { get; set; }
    public int WeeksAdvanced { get; set; }
    public int GamesSimulated { get; set; }
    public int FinalAbsoluteWeek { get; set; }
    public string FinalWeekLabel { get; set; } = "";
    public string FinalPhase { get; set; } = "";
    public List<ContinueEvent> EventsProcessed { get; set; } = new();
}
