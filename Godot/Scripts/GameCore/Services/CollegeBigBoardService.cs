using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Public rankings use only public workout, production, team, and declared-outlook context.
public sealed class CollegeBigBoardService
{
    private readonly GameCoreContext _context;
    public CollegeBigBoardService(GameCoreContext context) => _context = context;

    public CollegeBigBoardsResult GetBoards(int limit = 20)
    {
        var league = _context?.ActiveLeague;
        if (league == null)
            return new CollegeBigBoardsResult { Message = "College prospect context is unavailable." };
        limit = Math.Clamp(limit, 1, 50);
        ProspectEvaluationService.EnsureEvaluations(league);
        var prospects = (league.CollegeProspects ?? new List<CollegeProspectState>()).Where(prospect => prospect != null && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId)).ToList();
        return new CollegeBigBoardsResult
        {
            Ok = true,
            Boards = new List<CollegeBigBoard>
            {
                Build("Analyst Board", prospects, prospect => prospect.CombineScore * 2 + prospect.ProDayScore + Outlook(prospect.DraftStock) * 6, limit),
                Build("Media Consensus", prospects, prospect => prospect.ProDayScore * 2 + prospect.CombineScore + Outlook(prospect.DraftStock) * 4 + StableValue($"media-{prospect.ProspectId}") % 12, limit),
            },
        };
    }

    public int GetStableAnalystRank(string prospectId, int limit = 50)
    {
        var league = _context?.ActiveLeague;
        if (league == null || string.IsNullOrWhiteSpace(prospectId))
            return 0;
        limit = Math.Clamp(limit, 1, 50);
        ProspectEvaluationService.EnsureEvaluations(league);
        var board = Build("Analyst Board", (league.CollegeProspects ?? new List<CollegeProspectState>()).Where(prospect => prospect != null), prospect => prospect.CombineScore * 2 + prospect.ProDayScore + Outlook(prospect.DraftStock) * 6, limit);
        return board.Entries.FirstOrDefault(entry => string.Equals(entry.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase))?.Rank ?? 0;
    }

    private static CollegeBigBoard Build(string name, IEnumerable<CollegeProspectState> prospects, Func<CollegeProspectState, int> score, int limit)
    {
        var ordered = prospects.OrderByDescending(score).ThenByDescending(prospect => prospect.CombineScore).ThenBy(prospect => prospect.Name, StringComparer.Ordinal).ThenBy(prospect => prospect.ProspectId, StringComparer.Ordinal).Take(limit).ToList();
        return new CollegeBigBoard { Name = name, Entries = ordered.Select((prospect, index) => new CollegeBigBoardEntry { Rank = index + 1, ProspectId = prospect.ProspectId, Name = prospect.Name, Position = prospect.Position, College = prospect.College, Summary = $"Combine {prospect.CombineScore}/100 · Pro day {prospect.ProDayScore}/100 · {prospect.DraftStock}" }).ToList() };
    }

    private static int Outlook(string stock) => stock switch { "Top prospect" => 4, "First-round range" => 3, "Day-two range" => 2, "Developmental range" => 1, _ => 0 };
    private static int StableValue(string value) { unchecked { uint hash = 2166136261; foreach (var character in value ?? "") hash = (hash ^ character) * 16777619; return (int)(hash & 0x7fffffff); } }
}
