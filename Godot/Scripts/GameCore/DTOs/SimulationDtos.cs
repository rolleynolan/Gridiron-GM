using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class GameResultResponse
{
    public bool Ok { get; set; }
    public GameResultDto Result { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class GameDayStateResponse
{
    public bool Ok { get; set; }
    public GameDayStateDto Game { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class GameDayStateDto
{
    public string GameId { get; set; } = "";
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string GameType { get; set; } = "";
    public string WeekLabel { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public string Opponent { get; set; } = "";
    public string OpponentAbbreviation { get; set; } = "";
    public string HomeAway { get; set; } = "";
    public string Status { get; set; } = "";
}

public sealed class GameResultDto
{
    public string GameId { get; set; } = "";
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string GameType { get; set; } = "";
    public string WeekLabel { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string Winner { get; set; } = "";
    public string Summary { get; set; } = "";
    public Dictionary<string, object> BoxScore { get; set; } = new();
}

public sealed class ContinueResponse
{
    public bool Ok { get; set; }
    public ContinueResultDto Result { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class ContinueResultDto
{
    public bool Advanced { get; set; }
    public string StopReason { get; set; } = "";
    public int DaysAdvanced { get; set; }
    public int WeeksAdvanced { get; set; }
    public int GamesSimulated { get; set; }
    public int FinalAbsoluteWeek { get; set; }
    public string FinalWeekLabel { get; set; } = "";
    public string FinalPhase { get; set; } = "";
    public List<ContinueEventDto> EventsProcessed { get; set; } = new();
}

public sealed class ContinueEventDto
{
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
}
