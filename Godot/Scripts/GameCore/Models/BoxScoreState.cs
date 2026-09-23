using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class BoxScoreState
{
    public string Final { get; set; } = "";
    public Dictionary<string, int> TeamStats { get; set; } = new();
    public List<PlayerGameStats> PlayerStats { get; set; } = new();
    public List<GamePlayEventState> PlayByPlay { get; set; } = new();
}
