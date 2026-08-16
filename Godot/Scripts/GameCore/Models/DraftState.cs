using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class DraftPickState
{
    public int OverallPick { get; set; }
    public int Round { get; set; }
    public int PickInRound { get; set; }
    public string TeamId { get; set; } = "";
    public string ProspectId { get; set; } = "";
}

public sealed class DraftState
{
    public int DraftYear { get; set; }
    public List<DraftPickState> Picks { get; set; } = new();
}
