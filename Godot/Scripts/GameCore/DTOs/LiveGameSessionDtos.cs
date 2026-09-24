using GridironGM.GameCore.Models;
using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class LiveGameSessionResponse
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public LiveGameSessionDto Session { get; set; } = new();
}

public sealed class LiveGameSessionDto
{
    public string GameId { get; set; } = "";
    public bool Active { get; set; }
    public bool Completed { get; set; }
    public bool IsPaused { get; set; }
    public int NextEventIndex { get; set; }
    public int TotalEvents { get; set; }
    public GamePlayEventState CurrentEvent { get; set; }
    public GameResultDto Result { get; set; }
    public List<GamePlayEventState> PlayedEvents { get; set; } = new();
    public List<GridironGM.GameCore.Services.GameDecisionOption> Decisions { get; set; } = new();
    public string Situation { get; set; } = "";
    public int Quarter { get; set; }
    public int ClockSeconds { get; set; }
    public int YardLine { get; set; }
    public string PossessionTeamId { get; set; } = "";
}
