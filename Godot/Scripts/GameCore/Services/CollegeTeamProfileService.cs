using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

/// <summary>
/// Builds a read-only college-team encyclopedia view from the persisted universe.
/// It owns no presentation state and never exposes hidden player ratings.
/// </summary>
public sealed class CollegeTeamProfileService
{
    private readonly GameCoreContext _context;

    public CollegeTeamProfileService(GameCoreContext context) => _context = context;

    public CollegeTeamProfileResult GetProfile(string teamId)
    {
        var universe = _context?.ActiveLeague?.CollegeUniverse;
        if (universe == null)
            return new CollegeTeamProfileResult { Message = "College season unavailable." };

        var team = universe.Teams?.FirstOrDefault(candidate =>
            candidate != null && string.Equals(candidate.TeamId, teamId, StringComparison.OrdinalIgnoreCase));
        if (team == null)
            return new CollegeTeamProfileResult { SeasonYear = universe.SeasonYear, Message = "College team unavailable." };

        var teamsById = universe.Teams
            .Where(candidate => candidate != null)
            .ToDictionary(candidate => candidate.TeamId, candidate => candidate, StringComparer.OrdinalIgnoreCase);
        var resultsByGame = (universe.Results ?? new List<CollegeGameResult>())
            .Where(result => result != null)
            .GroupBy(result => result.GameId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var profile = new CollegeTeamProfileResult
        {
            Ok = true,
            SeasonYear = universe.SeasonYear,
            TeamId = team.TeamId,
            TeamName = team.Name,
            Abbreviation = team.Abbreviation,
            Conference = team.Conference,
            Ranking = team.Ranking,
            Wins = team.Wins,
            Losses = team.Losses,
            HeadCoachName = team.HeadCoach?.Name ?? "Vacant",
            HeadCoachTenure = team.HeadCoach?.SeasonsAtProgram ?? 0,
            ProgramLeadership = RatingBand(team.HeadCoach?.ProgramRating ?? 65),
            RecruitingLeadership = RatingBand(team.HeadCoach?.RecruitingRating ?? 65),
        };
        profile.ProgramHistory = (_context.ActiveLeague.CollegeSeasonArchives ?? new List<CollegeSeasonArchiveRecord>())
            .Where(archive => archive != null)
            .SelectMany(archive => archive.TeamRecords ?? new List<CollegeTeamSeasonRecord>())
            .Where(record => record != null && string.Equals(record.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.SeasonYear)
            .Select(record => new CollegeTeamSeasonRecord
            {
                SeasonYear = record.SeasonYear,
                TeamId = record.TeamId,
                TeamName = record.TeamName,
                Conference = record.Conference,
                Wins = record.Wins,
                Losses = record.Losses,
                FinalRanking = record.FinalRanking,
                WonChampionship = record.WonChampionship,
            })
            .ToList();
        profile.RecruitingClass = (universe.RecruitingClass ?? new List<CollegeRecruitingRecord>())
            .Where(record => record != null && string.Equals(record.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(record => record.WillRedshirt)
            .ThenBy(record => PositionOrder(record.Position))
            .ThenBy(record => record.PlayerName, StringComparer.Ordinal)
            .Select(record => new CollegeTeamRecruitLine
            {
                PlayerId = record.PlayerId,
                PlayerName = record.PlayerName,
                Position = record.Position,
                PublicTier = record.PublicTier,
                Summary = record.Summary,
                WillRedshirt = record.WillRedshirt,
            })
            .ToList();

        foreach (var game in (universe.Schedule ?? new List<CollegeScheduledGame>())
                     .Where(game => game != null &&
                         (string.Equals(game.HomeTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) ||
                          string.Equals(game.AwayTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase)))
                     .OrderBy(game => game.ProAbsoluteWeek)
                     .ThenBy(game => game.GameId, StringComparer.Ordinal))
        {
            var isHome = string.Equals(game.HomeTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase);
            var opponentId = isHome ? game.AwayTeamId : game.HomeTeamId;
            resultsByGame.TryGetValue(game.GameId, out var result);
            var teamScore = result == null ? (int?)null : isHome ? result.HomeScore : result.AwayScore;
            var opponentScore = result == null ? (int?)null : isHome ? result.AwayScore : result.HomeScore;
            profile.Schedule.Add(new CollegeTeamScheduleEntry
            {
                Week = game.ProAbsoluteWeek,
                OpponentName = teamsById.GetValueOrDefault(opponentId)?.Name ?? "Unknown opponent",
                IsHome = isHome,
                IsFinal = result != null,
                TeamScore = teamScore,
                OpponentScore = opponentScore,
                Result = result == null ? "" : teamScore > opponentScore ? "W" : "L",
            });
        }

        profile.Roster = (universe.Players ?? new List<CollegePlayerState>())
            .Where(player => player != null && string.Equals(player.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase))
            .OrderBy(player => PositionOrder(player.Position))
            .ThenBy(player => player.Position, StringComparer.Ordinal)
            .ThenBy(player => player.Name, StringComparer.Ordinal)
            .Select(ToPlayerLine)
            .ToList();
        profile.StatLeaders = (universe.Players ?? new List<CollegePlayerState>())
            .Where(player => player != null && string.Equals(player.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(player => player.PassingYards + player.RushingYards + player.ReceivingYards)
            .ThenByDescending(player => player.Touchdowns)
            .ThenBy(player => player.Name, StringComparer.Ordinal)
            .Take(5)
            .Select(ToPlayerLine)
            .ToList();

        return profile;
    }

    private CollegeTeamPlayerLine ToPlayerLine(CollegePlayerState player)
        => new()
        {
            PlayerId = player.PlayerId,
            Name = player.Name,
            Position = player.Position,
            ClassYear = player.ClassYear,
            CollegeYear = player.CollegeYear,
            PlayableSeasonsUsed = player.PlayableSeasonsUsed,
            PlayableSeasonsRemaining = Math.Max(0, 4 - player.PlayableSeasonsUsed),
            IsRedshirted = player.IsRedshirted,
            TransferContext = player.TransferHistory?
                .Where(record => record != null)
                .OrderByDescending(record => record.SeasonYear)
                .Select(record => record.SeasonYear == _context.ActiveLeague.CollegeUniverse.SeasonYear
                    ? $"Transferred in: {record.Reason}"
                    : $"Transferred in {record.SeasonYear}")
                .FirstOrDefault() ?? "",
            GamesPlayed = player.GamesPlayed,
            PassingYards = player.PassingYards,
            RushingYards = player.RushingYards,
            ReceivingYards = player.ReceivingYards,
            Touchdowns = player.Touchdowns,
            CareerSeasons = player.CareerStats?.Count(record => record != null) ?? 0,
            CareerGames = player.GamesPlayed + (player.CareerStats?.Where(record => record != null).Sum(record => record.GamesPlayed) ?? 0),
            CareerYards = player.PassingYards + player.RushingYards + player.ReceivingYards + (player.CareerStats?.Where(record => record != null).Sum(record => record.PassingYards + record.RushingYards + record.ReceivingYards) ?? 0),
            CareerTouchdowns = player.Touchdowns + (player.CareerStats?.Where(record => record != null).Sum(record => record.Touchdowns) ?? 0),
            Availability = player.IsRedshirted
                ? "Redshirt"
                : player.CurrentInjury?.IsActive == true
                ? $"{player.CurrentInjury.Name} ({player.CurrentInjury.WeeksRemaining}w)"
                : "Available",
        };

    private static int PositionOrder(string position)
        => position switch
        {
            "QB" => 0, "RB" => 1, "WR" => 2, "TE" => 3, "OT" => 4, "OG" => 5, "C" => 6,
            "EDGE" => 7, "DT" => 8, "LB" => 9, "CB" => 10, "S" => 11, "K" => 12, "P" => 13,
            _ => 14,
        };

    private static string RatingBand(int rating)
        => rating >= 82 ? "Excellent" : rating >= 72 ? "Strong" : rating >= 62 ? "Stable" : "Developing";
}

public sealed class CollegeTeamProfileResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
    public int SeasonYear { get; set; }
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public string Abbreviation { get; set; } = "";
    public string Conference { get; set; } = "";
    public int Ranking { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public string HeadCoachName { get; set; } = "";
    public int HeadCoachTenure { get; set; }
    public string ProgramLeadership { get; set; } = "";
    public string RecruitingLeadership { get; set; } = "";
    public List<CollegeTeamScheduleEntry> Schedule { get; set; } = new();
    public List<CollegeTeamPlayerLine> StatLeaders { get; set; } = new();
    public List<CollegeTeamPlayerLine> Roster { get; set; } = new();
    public List<CollegeTeamSeasonRecord> ProgramHistory { get; set; } = new();
    public List<CollegeTeamRecruitLine> RecruitingClass { get; set; } = new();
}

public sealed class CollegeTeamRecruitLine
{
    public string PlayerId { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Position { get; set; } = "";
    public string PublicTier { get; set; } = "";
    public string Summary { get; set; } = "";
    public bool WillRedshirt { get; set; }
}

public sealed class CollegeTeamScheduleEntry
{
    public int Week { get; set; }
    public string OpponentName { get; set; } = "";
    public bool IsHome { get; set; }
    public bool IsFinal { get; set; }
    public int? TeamScore { get; set; }
    public int? OpponentScore { get; set; }
    public string Result { get; set; } = "";
}

public sealed class CollegeTeamPlayerLine
{
    public string PlayerId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Position { get; set; } = "";
    public int ClassYear { get; set; }
    public int CollegeYear { get; set; }
    public int PlayableSeasonsUsed { get; set; }
    public int PlayableSeasonsRemaining { get; set; }
    public bool IsRedshirted { get; set; }
    public string TransferContext { get; set; } = "";
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int RushingYards { get; set; }
    public int ReceivingYards { get; set; }
    public int Touchdowns { get; set; }
    public int CareerSeasons { get; set; }
    public int CareerGames { get; set; }
    public int CareerYards { get; set; }
    public int CareerTouchdowns { get; set; }
    public string Availability { get; set; } = "";
}
