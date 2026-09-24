using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GridironGM.GameCore.Models;

public sealed class LiveGameSessionState
{
    public bool Active { get; set; }
    public bool Completed { get; set; }
    public bool IsPaused { get; set; } = true;
    public string GameId { get; set; } = "";
    public int NextEventIndex { get; set; }
    public GameResult PendingResult { get; set; } = new();
    private List<GamePlayEventState> _legacyPlayedEvents = new();
    [JsonIgnore]
    public List<GamePlayEventState> PlayedEvents
    {
        get => PendingResult?.ProGame != null ? PendingResult.BoxScore?.PlayByPlay ?? _legacyPlayedEvents : _legacyPlayedEvents;
        set => _legacyPlayedEvents = value ?? new();
    }
    // Read old playback saves, but never serialize a second copy of the new authoritative log.
    [JsonPropertyName("PlayedEvents")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GamePlayEventState> LegacyPlayedEvents
    {
        get => PendingResult?.ProGame == null ? _legacyPlayedEvents : null;
        set => _legacyPlayedEvents = value ?? new();
    }
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
