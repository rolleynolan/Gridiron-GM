using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class DraftService
{
    private readonly GameCoreContext _context;
    public DraftService(GameCoreContext context) => _context = context;

    public void PrepareDraftBoard()
    {
        var league = _context.ActiveLeague;
        if (league == null || league.Draft.DraftYear == league.SeasonYear)
            return;

        var standings = new StandingsService(_context).BuildStandings(league)
            .OrderBy(standing => standing.WinPct).ThenBy(standing => standing.PointDifferential).ThenBy(standing => standing.TeamId, StringComparer.Ordinal).ToList();
        league.Draft = new DraftState { DraftYear = league.SeasonYear };
        for (var round = 1; round <= 3; round++)
            for (var index = 0; index < standings.Count; index++)
                league.Draft.Picks.Add(new DraftPickState { OverallPick = ((round - 1) * standings.Count) + index + 1, Round = round, PickInRound = index + 1, TeamId = standings[index].TeamId });

        var scouting = league.FranchiseMetadata?.GmProfileSnapshot?.Attributes?.ScoutingJudgment ?? 50;
        foreach (var prospect in league.CollegeProspects.Where(prospect => prospect != null && prospect.DraftClassYear == league.SeasonYear + 1))
        {
            var error = Math.Max(1, 12 - ((scouting - 20) / 6));
            var seed = Math.Abs((prospect.ProspectId ?? "").Aggregate(17, (value, character) => unchecked(value * 31 + character)));
            prospect.ScoutedOverall = Math.Clamp(prospect.Overall + ((seed % (error * 2 + 1)) - error), 40, 99);
            prospect.ScoutedPotential = Math.Clamp(prospect.Potential + (((seed / 7) % (error * 2 + 1)) - error), prospect.ScoutedOverall, 99);
        }
    }

    public bool MakePick(string teamId, string prospectId)
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        var pick = league?.Draft.Picks.FirstOrDefault(item => string.Equals(item.TeamId, teamId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(item.ProspectId));
        var prospect = league?.CollegeProspects.FirstOrDefault(item => string.Equals(item.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(item.DraftedByTeamId));
        if (pick == null || prospect == null) return false;
        pick.ProspectId = prospect.ProspectId; prospect.DraftedByTeamId = pick.TeamId; return true;
    }
}
