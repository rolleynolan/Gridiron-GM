using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class CollegeSeasonArchiveService
{
    public static void EnsureArchived(LeagueState league)
    {
        var universe = league?.CollegeUniverse;
        if (universe?.Postseason?.Completed != true || universe.SeasonYear <= 0)
            return;
        league.CollegeSeasonArchives ??= new List<CollegeSeasonArchiveRecord>();
        if (league.CollegeSeasonArchives.Any(record => record != null && record.SeasonYear == universe.SeasonYear))
            return;
        var championship = universe.Postseason.Games.FirstOrDefault(game => string.Equals(game.Label, "College National Championship", StringComparison.Ordinal));
        var champion = universe.Teams.FirstOrDefault(team => string.Equals(team?.TeamId, championship?.WinnerTeamId, StringComparison.OrdinalIgnoreCase));
        league.CollegeSeasonArchives.Add(new CollegeSeasonArchiveRecord
        {
            SeasonYear = universe.SeasonYear, ChampionTeamId = championship?.WinnerTeamId ?? "", ChampionTeamName = champion?.Name ?? "",
            TeamRecords = universe.Teams.Where(team => team != null).Select(team => new CollegeTeamSeasonRecord
            {
                SeasonYear = universe.SeasonYear,
                TeamId = team.TeamId,
                TeamName = team.Name,
                Conference = team.Conference,
                Wins = team.Wins,
                Losses = team.Losses,
                FinalRanking = team.Ranking,
                WonChampionship = string.Equals(team.TeamId, championship?.WinnerTeamId, StringComparison.OrdinalIgnoreCase),
            }).ToList(),
            Awards = (universe.Awards ?? new List<CollegeSeasonAwardRecord>()).Where(award => award != null).Select(award => new CollegeSeasonAwardRecord { AwardName = award.AwardName, PlayerId = award.PlayerId, PlayerName = award.PlayerName, TeamId = award.TeamId, TeamName = award.TeamName, Position = award.Position, Score = award.Score, Summary = award.Summary }).ToList(),
            PostseasonGames = universe.Postseason.Games.Where(game => game != null).Select(game => new CollegePostseasonGame { Label = game.Label, HomeTeamId = game.HomeTeamId, AwayTeamId = game.AwayTeamId, HomeScore = game.HomeScore, AwayScore = game.AwayScore, WinnerTeamId = game.WinnerTeamId }).ToList(),
        });
    }
}
