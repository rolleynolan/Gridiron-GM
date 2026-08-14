using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class BoxScoreState
{
    public string Final { get; set; } = "";
    public Dictionary<string, int> TeamStats { get; set; } = new();
}
