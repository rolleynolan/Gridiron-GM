using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class DashboardStateResponse
{
    public bool Ok { get; set; }
    public DashboardDto Dashboard { get; set; }
    public string Error { get; set; } = "";
}

public sealed class DashboardDto
{
    public TeamSummaryDto Team { get; set; } = new();
    public CalendarSummaryDto Calendar { get; set; } = new();
    public NextGameDto NextGame { get; set; } = new();
    public TeamStatusDto TeamStatus { get; set; } = new();
    public List<ActionItemDto> ActionItems { get; set; } = new();
    public List<RecentResultDto> RecentResults { get; set; } = new();
    public PlayoffBracketDto PlayoffBracket { get; set; } = new();
    public string PlayoffSummaryText { get; set; } = "";
    public SeasonCompletionSummaryDto SeasonCompletionSummary { get; set; } = new();
}

public sealed class SeasonCompletionSummaryDto
{
    public bool IsAvailable { get; set; }
    public string CompletedPhaseLabel { get; set; } = "";
    public string ChampionTeamName { get; set; } = "";
    public string RunnerUpTeamName { get; set; } = "";
    public string ChampionshipResultLine { get; set; } = "";
}

public sealed class TeamSummaryDto
{
    public string Name { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Record { get; set; } = "0-0";
}

public sealed class CalendarSummaryDto
{
    public int Year { get; set; }
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string CurrentDate { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public string WeekLabel { get; set; } = "";
}

public sealed class NextGameDto
{
    public string Opponent { get; set; } = "";
    public string OpponentAbbreviation { get; set; } = "";
    public string HomeAway { get; set; } = "";
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string GameType { get; set; } = "";
    public string GameId { get; set; } = "";
    public string WeekLabel { get; set; } = "";
    public string HeaderOpponentLabel { get; set; } = "";
    public string HeaderNextLabel { get; set; } = "";
}

public sealed class TeamStatusDto
{
    public int RosterSize { get; set; }
    public int Injuries { get; set; }
    public string CapRoom { get; set; } = "";
}

public sealed class ActionItemDto
{
    public string Type { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string PrimaryAction { get; set; } = "";
}

public sealed class RecentResultDto
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
}
