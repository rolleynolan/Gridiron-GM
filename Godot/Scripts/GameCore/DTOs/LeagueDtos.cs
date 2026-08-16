using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class StandingsResponse
{
    public bool Ok { get; set; }
    public List<StandingRowDto> Standings { get; set; } = new();
    public PlayoffBracketDto PlayoffBracket { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class StandingRowDto
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public double WinPct { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public string Division { get; set; } = "";
    public string Conference { get; set; } = "";
}

public sealed class PlayoffBracketDto
{
    public int SeasonYear { get; set; }
    public int GeneratedFromAbsoluteWeek { get; set; }
    public string GeneratedAtPhaseLabel { get; set; } = "";
    public List<PlayoffConferenceBracketDto> ConferenceBrackets { get; set; } = new();
    public PlayoffRoundDto LeagueChampionshipRound { get; set; } = new();
    public LeagueChampionRecordDto LeagueChampionRecord { get; set; } = new();
}

public sealed class PlayoffConferenceBracketDto
{
    public string Conference { get; set; } = "";
    public List<PlayoffSeedDto> Seeds { get; set; } = new();
    public List<PlayoffRoundDto> Rounds { get; set; } = new();
}

public sealed class PlayoffRoundDto
{
    public string Round { get; set; } = "";
    public string Status { get; set; } = "";
    public List<PlayoffGameDto> Games { get; set; } = new();
}

public sealed class PlayoffSeedDto
{
    public int Seed { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Conference { get; set; } = "";
    public string Division { get; set; } = "";
    public bool IsDivisionWinner { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public double WinPercentage { get; set; }
    public int PointDifferential { get; set; }
    public int PointsFor { get; set; }
}

public sealed class PlayoffGameDto
{
    public string GameId { get; set; } = "";
    public string Round { get; set; } = "";
    public string RoundLabel { get; set; } = "";
    public string Conference { get; set; } = "";
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string GameType { get; set; } = "";
    public int HomeSeed { get; set; }
    public int AwaySeed { get; set; }
    public string HomeTeamId { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string HomeTeamName { get; set; } = "";
    public string AwayTeamName { get; set; } = "";
    public bool NeutralSite { get; set; }
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Status { get; set; } = "";
    public string WinnerTeamId { get; set; } = "";
    public string LoserTeamId { get; set; } = "";
}

public sealed class LeagueChampionRecordDto
{
    public int SeasonYear { get; set; }
    public string ChampionTeamId { get; set; } = "";
    public string ChampionTeamName { get; set; } = "";
    public string RunnerUpTeamId { get; set; } = "";
    public string RunnerUpTeamName { get; set; } = "";
    public string ChampionshipHomeTeamId { get; set; } = "";
    public string ChampionshipAwayTeamId { get; set; } = "";
    public int ChampionScore { get; set; }
    public int RunnerUpScore { get; set; }
    public string CompletedPhaseLabel { get; set; } = "";
}

public sealed class TeamScheduleResponse
{
    public bool Ok { get; set; }
    public List<ScheduleGameRowDto> Schedule { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class ScheduleGameRowDto
{
    public string GameId { get; set; } = "";
    public int Week { get; set; }
    public int AbsoluteWeek { get; set; }
    public int PhaseWeek { get; set; }
    public string Phase { get; set; } = "";
    public string DisplayWeek { get; set; } = "";
    public string GameType { get; set; } = "";
    public string WeekLabel { get; set; } = "";
    public string Opponent { get; set; } = "";
    public string HomeAway { get; set; } = "";
    public string Status { get; set; } = "";
    public string HomeTeam { get; set; } = "";
    public string AwayTeam { get; set; } = "";
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
    public string Winner { get; set; } = "";
}

public sealed class TeamIdentityDto
{
    public string TeamId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Abbreviation { get; set; } = "";
}

public sealed class LeagueHistoryResponse
{
    public bool Ok { get; set; }
    public List<LeagueHistorySeasonDto> Seasons { get; set; } = new();
    public string Error { get; set; } = "";
}

public sealed class LeagueHistorySeasonDto
{
    public int SeasonYear { get; set; }
    public string CompletedPhaseLabel { get; set; } = "";
    public string ChampionTeamId { get; set; } = "";
    public string ChampionTeamName { get; set; } = "";
    public string RunnerUpTeamId { get; set; } = "";
    public string RunnerUpTeamName { get; set; } = "";
    public string ChampionshipGameLabel { get; set; } = "";
    public int ChampionshipWinnerScore { get; set; }
    public int ChampionshipRunnerUpScore { get; set; }
    public int TotalRegularSeasonGames { get; set; }
    public int TotalPlayoffGames { get; set; }
    public string GeneratedAtLabel { get; set; } = "";
    public List<LeagueHistoryTeamRecordDto> TeamRecords { get; set; } = new();
    public List<LeagueHistoryPlayoffSeedDto> PlayoffSeeds { get; set; } = new();
    public List<LeagueHistoryPlayoffResultDto> PlayoffResults { get; set; } = new();
}

public sealed class LeagueHistoryTeamRecordDto
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Conference { get; set; } = "";
    public string Division { get; set; } = "";
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Ties { get; set; }
    public int PointsFor { get; set; }
    public int PointsAgainst { get; set; }
    public double WinPercentage { get; set; }
}

public sealed class LeagueHistoryPlayoffSeedDto
{
    public string Conference { get; set; } = "";
    public int Seed { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Division { get; set; } = "";
    public bool IsDivisionWinner { get; set; }
}

public sealed class LeagueHistoryPlayoffResultDto
{
    public string Round { get; set; } = "";
    public string Conference { get; set; } = "";
    public string HomeTeamId { get; set; } = "";
    public string HomeTeamName { get; set; } = "";
    public string AwayTeamId { get; set; } = "";
    public string AwayTeamName { get; set; } = "";
    public int HomeScore { get; set; }
    public int AwayScore { get; set; }
    public string WinnerTeamId { get; set; } = "";
    public string WinnerTeamName { get; set; } = "";
    public string LoserTeamId { get; set; } = "";
    public string LoserTeamName { get; set; } = "";
}
