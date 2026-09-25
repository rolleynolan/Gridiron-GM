using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

// Persist continuity only; plans and scores are derived from authoritative state.
public sealed class CpuRosterState
{
    public int SeasonYear { get; set; }
    public List<string> CompletedCheckpoints { get; set; } = new();
}
