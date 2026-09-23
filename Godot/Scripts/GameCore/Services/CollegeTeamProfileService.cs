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
        };

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

        profile.StatLeaders = (universe.Players ?? new List<CollegePlayerState>())
            .Where(player => player != null && string.Equals(player.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(player => player.PassingYards + player.RushingYards + player.ReceivingYards)
            .ThenByDescending(player => player.Touchdowns)
            .ThenBy(player => player.Name, StringComparer.Ordinal)
            .Take(5)
            .Select(player => new CollegeTeamPlayerLine
            {
                PlayerId = player.PlayerId,
                Name = player.Name,
                Position = player.Position,
                ClassYear = player.ClassYear,
                GamesPlayed = player.GamesPlayed,
                PassingYards = player.PassingYards,
                RushingYards = player.RushingYards,
                ReceivingYards = player.ReceivingYards,
                Touchdowns = player.Touchdowns,
                Availability = player.CurrentInjury?.IsActive == true
                    ? $"{player.CurrentInjury.Name} ({player.CurrentInjury.WeeksRemaining}w)"
                    : "Available",
            })
            .ToList();

        return profile;
    }
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
    public List<CollegeTeamScheduleEntry> Schedule { get; set; } = new();
    public List<CollegeTeamPlayerLine> StatLeaders { get; set; } = new();
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
    public int GamesPlayed { get; set; }
    public int PassingYards { get; set; }
    public int RushingYards { get; set; }
    public int ReceivingYards { get; set; }
    public int Touchdowns { get; set; }
    public string Availability { get; set; } = "";
}
