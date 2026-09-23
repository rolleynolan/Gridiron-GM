using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class LiveGameSessionState
{
    public bool Active { get; set; }
    public bool Completed { get; set; }
    public bool IsPaused { get; set; } = true;
    public string GameId { get; set; } = "";
    public int NextEventIndex { get; set; }
    public GameResult PendingResult { get; set; } = new();
    public List<GamePlayEventState> PlayedEvents { get; set; } = new();
    public List<LiveGameAdjustmentState> Adjustments { get; set; } = new();
}

public sealed class LiveGameAdjustmentState
{
    public int AfterEventSequence { get; set; }
    public string TeamId { get; set; } = "";
    public string Position { get; set; } = "";
    public string PlayerId { get; set; } = "";
    public string TargetPlayerId { get; set; } = "";
    public string Action { get; set; } = "";
}
