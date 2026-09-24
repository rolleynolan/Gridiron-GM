using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// A compact read-only feed derived from the college state; it never creates news state or simulation events.
public sealed class CollegeNewsService
{
    private readonly GameCoreContext _context;
    public CollegeNewsService(GameCoreContext context) => _context = context;

    public CollegeNewsResult GetNews(int limit = 12)
    {
        var universe = _context?.ActiveLeague?.CollegeUniverse;
        if (universe == null)
            return new CollegeNewsResult { Message = "College season unavailable." };

        limit = Math.Clamp(limit, 1, 30);
        var teams = (universe.Teams ?? new List<CollegeTeamState>()).Where(team => team != null).ToDictionary(team => team.TeamId, StringComparer.OrdinalIgnoreCase);
        var items = new List<CollegeNewsItem>();
        foreach (var recruit in (universe.RecruitingClass ?? new List<CollegeRecruitingRecord>())
                     .Where(record => record != null)
                     .OrderBy(record => record.PublicTier == "Headline signing" ? 0 : record.PublicTier == "Priority signing" ? 1 : 2)
                     .ThenBy(record => record.PlayerName, StringComparer.Ordinal)
                     .Take(4))
        {
            var team = teams.GetValueOrDefault(recruit.TeamId)?.Name ?? "A college program";
            items.Add(new CollegeNewsItem
            {
                Category = "RECRUITING",
                ProAbsoluteWeek = 0,
                Headline = $"{team} adds {recruit.PlayerName}",
                Detail = $"{recruit.Position} · {recruit.Summary}",
            });
        }
        foreach (var transfer in (universe.Transfers ?? new List<CollegeTransferRecord>())
                     .Where(record => record != null)
                     .OrderBy(record => record.PlayerName, StringComparer.Ordinal)
                     .Take(4))
        {
            var fromTeam = teams.GetValueOrDefault(transfer.FromTeamId)?.Name ?? "the previous program";
            var toTeam = teams.GetValueOrDefault(transfer.ToTeamId)?.Name ?? "a new program";
            items.Add(new CollegeNewsItem
            {
                Category = "TRANSFER",
                ProAbsoluteWeek = 0,
                Headline = $"{transfer.PlayerName} transfers to {toTeam}",
                Detail = $"{transfer.Position} from {fromTeam}. {transfer.Reason}",
            });
        }
        foreach (var result in (universe.Results ?? new List<CollegeGameResult>()).Where(result => result != null).OrderByDescending(result => result.ProAbsoluteWeek).ThenBy(result => result.GameId, StringComparer.Ordinal))
        {
            var home = teams.TryGetValue(result.HomeTeamId, out var homeTeam) ? homeTeam.Name : "Home";
            var away = teams.TryGetValue(result.AwayTeamId, out var awayTeam) ? awayTeam.Name : "Away";
            var winner = string.Equals(result.WinnerTeamId, result.HomeTeamId, StringComparison.OrdinalIgnoreCase) ? home : away;
            items.Add(new CollegeNewsItem { Category = "RESULT", ProAbsoluteWeek = result.ProAbsoluteWeek, Headline = $"{winner} earns a Week {result.ProAbsoluteWeek} win", Detail = $"Final: {away} {result.AwayScore}, {home} {result.HomeScore}." });
        }

        var topTeam = teams.Values.Where(team => team.Ranking > 0).OrderBy(team => team.Ranking).ThenBy(team => team.Name, StringComparer.Ordinal).FirstOrDefault();
        if (topTeam != null)
            items.Add(new CollegeNewsItem { Category = "RANKING", ProAbsoluteWeek = universe.LastAdvancedAbsoluteWeek, Headline = $"{topTeam.Name} holds the No. 1 ranking", Detail = $"The current leader is {topTeam.Wins}-{topTeam.Losses}." });

        var leader = (universe.Players ?? new List<CollegePlayerState>()).Where(player => player != null && player.GamesPlayed > 0).OrderByDescending(TotalYards).ThenByDescending(player => player.Touchdowns).ThenBy(player => player.Name, StringComparer.Ordinal).FirstOrDefault();
        if (leader != null)
            items.Add(new CollegeNewsItem { Category = "PERFORMANCE", ProAbsoluteWeek = universe.LastAdvancedAbsoluteWeek, Headline = $"{leader.Name} leads the college season in total yards", Detail = $"{leader.Position} · {TotalYards(leader):N0} total yards · {leader.Touchdowns} touchdowns." });

        return new CollegeNewsResult { Ok = true, SeasonYear = universe.SeasonYear, Items = items.OrderByDescending(item => item.ProAbsoluteWeek).ThenBy(item => item.Category, StringComparer.Ordinal).ThenBy(item => item.Headline, StringComparer.Ordinal).Take(limit).ToList() };
    }

    private static int TotalYards(CollegePlayerState player) => player.PassingYards + player.RushingYards + player.ReceivingYards;
}
