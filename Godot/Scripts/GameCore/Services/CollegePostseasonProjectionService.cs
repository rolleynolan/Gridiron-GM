using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Read-only projections remain separate from the college schedule, results, and standings.
public sealed class CollegePostseasonProjectionService
{
    private readonly GameCoreContext _context;
    public CollegePostseasonProjectionService(GameCoreContext context) => _context = context;

    public CollegePostseasonProjectionResult GetProjections()
    {
        var universe = _context?.ActiveLeague?.CollegeUniverse;
        if (universe == null)
            return new CollegePostseasonProjectionResult { Message = "College season unavailable." };

        var teams = (universe.Teams ?? new List<CollegeTeamState>()).Where(team => team != null && team.Ranking > 0).OrderBy(team => team.Ranking).ThenBy(team => team.Name, StringComparer.Ordinal).ToList();
        if (teams.Count < 4)
            return new CollegePostseasonProjectionResult { Message = "Postseason projections need current college rankings.", SeasonYear = universe.SeasonYear };

        var result = new CollegePostseasonProjectionResult { Ok = true, SeasonYear = universe.SeasonYear };
        result.PlayoffMatchups.Add(Matchup("Projected Playoff Semifinal · 1 vs 4", teams[0], teams[3]));
        result.PlayoffMatchups.Add(Matchup("Projected Playoff Semifinal · 2 vs 3", teams[1], teams[2]));
        if (teams.Count >= 8)
        {
            result.BowlMatchups.Add(Matchup("Projected Coastal Classic · 5 vs 8", teams[4], teams[7]));
            result.BowlMatchups.Add(Matchup("Projected Heartland Bowl · 6 vs 7", teams[5], teams[6]));
        }
        return result;
    }

    private static CollegeProjectedMatchup Matchup(string label, CollegeTeamState home, CollegeTeamState away)
        => new() { Label = label, Home = Team(home), Away = Team(away) };

    private static CollegeProjectedTeam Team(CollegeTeamState team)
        => new() { Ranking = team.Ranking, TeamId = team.TeamId, TeamName = team.Name, Wins = team.Wins, Losses = team.Losses };
}
