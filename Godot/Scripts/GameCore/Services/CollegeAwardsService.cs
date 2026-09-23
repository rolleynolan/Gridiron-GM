using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Awards are immutable season snapshots derived exclusively from completed college-game statistics.
public static class CollegeAwardsService
{
    public static void EnsureAwards(CollegeUniverseState universe)
    {
        if (universe == null)
            return;

        universe.Awards ??= new List<CollegeSeasonAwardRecord>();
        if (universe.Awards.Count > 0 || !IsComplete(universe))
            return;

        var candidates = (universe.Players ?? new List<CollegePlayerState>())
            .Where(player => player != null && player.GamesPlayed > 0)
            .Select(player => new Candidate
            {
                Player = player,
                Team = universe.Teams?.FirstOrDefault(team => string.Equals(team?.TeamId, player.TeamId, StringComparison.OrdinalIgnoreCase)),
            })
            .ToList();

        AddAward(universe.Awards, candidates, "College Player of the Year",
            candidate => TotalYards(candidate.Player) + candidate.Player.Touchdowns * 35,
            candidate => $"{TotalYards(candidate.Player):N0} total yds | {candidate.Player.Touchdowns} TD");
        AddAward(universe.Awards, candidates.Where(candidate => string.Equals(candidate.Player.Position, "QB", StringComparison.OrdinalIgnoreCase)), "College Quarterback of the Year",
            candidate => candidate.Player.PassingYards + candidate.Player.Touchdowns * 35,
            candidate => $"{candidate.Player.PassingYards:N0} pass yds | {candidate.Player.Touchdowns} TD");
        AddAward(universe.Awards, candidates.Where(candidate => string.Equals(candidate.Player.Position, "RB", StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Player.Position, "WR", StringComparison.OrdinalIgnoreCase) || string.Equals(candidate.Player.Position, "TE", StringComparison.OrdinalIgnoreCase)), "College Skill Player of the Year",
            candidate => candidate.Player.RushingYards + candidate.Player.ReceivingYards + candidate.Player.Touchdowns * 35,
            candidate => $"{candidate.Player.RushingYards + candidate.Player.ReceivingYards:N0} rush/rec yds | {candidate.Player.Touchdowns} TD");
    }

    public static bool IsComplete(CollegeUniverseState universe)
        => universe?.Schedule?.Count > 0 && universe.Schedule.All(game => game != null && string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase));

    private static void AddAward(List<CollegeSeasonAwardRecord> awards, IEnumerable<Candidate> candidates, string name, Func<Candidate, int> score, Func<Candidate, string> summary)
    {
        var winner = candidates
            .OrderByDescending(score)
            .ThenByDescending(candidate => candidate.Player.Touchdowns)
            .ThenByDescending(candidate => TotalYards(candidate.Player))
            .ThenBy(candidate => candidate.Player.Name, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Player.PlayerId, StringComparer.Ordinal)
            .FirstOrDefault();
        if (winner == null)
            return;

        awards.Add(new CollegeSeasonAwardRecord
        {
            AwardName = name,
            PlayerId = winner.Player.PlayerId,
            PlayerName = winner.Player.Name,
            TeamId = winner.Player.TeamId,
            TeamName = winner.Team?.Name ?? "",
            Position = winner.Player.Position,
            Score = score(winner),
            Summary = summary(winner),
        });
    }

    private static int TotalYards(CollegePlayerState player)
        => player.PassingYards + player.RushingYards + player.ReceivingYards;

    private sealed class Candidate
    {
        public CollegePlayerState Player { get; init; }
        public CollegeTeamState Team { get; init; }
    }
}
