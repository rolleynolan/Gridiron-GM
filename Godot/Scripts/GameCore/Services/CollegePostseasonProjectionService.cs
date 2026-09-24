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
        var field = CollegePostseasonService.SelectField(teams);
        if (field.Count < 12)
            return new CollegePostseasonProjectionResult { Message = "Postseason projections need current college rankings.", SeasonYear = universe.SeasonYear };

        var result = new CollegePostseasonProjectionResult { Ok = true, SeasonYear = universe.SeasonYear, RuleVersion = CollegePostseasonService.RuleVersion };
        result.FirstRoundByes.AddRange(field.Take(4).Select(Team));
        result.PlayoffMatchups.Add(Matchup("Projected First Round · 8 vs 9", field[7], field[8]));
        result.PlayoffMatchups.Add(Matchup("Projected First Round · 5 vs 12", field[4], field[11]));
        result.PlayoffMatchups.Add(Matchup("Projected First Round · 7 vs 10", field[6], field[9]));
        result.PlayoffMatchups.Add(Matchup("Projected First Round · 6 vs 11", field[5], field[10]));
        var bowlTeams = teams.Where(team => field.All(selection => selection.Team.TeamId != team.TeamId)).Take(8).ToList();
        var bowlNames = new[] { "Projected Coastal Classic", "Projected Heartland Bowl", "Projected Lakeside Bowl", "Projected Frontier Bowl" };
        for (var index = 0; index < bowlNames.Length; index++) result.BowlMatchups.Add(Matchup(bowlNames[index], bowlTeams[index], bowlTeams[7 - index]));
        return result;
    }

    private static CollegeProjectedMatchup Matchup(string label, CollegePostseasonService.Selection home, CollegePostseasonService.Selection away)
        => new() { Label = label, Home = Team(home), Away = Team(away) };

    private static CollegeProjectedMatchup Matchup(string label, CollegeTeamState home, CollegeTeamState away)
        => new() { Label = label, Home = Team(home), Away = Team(away) };

    private static CollegeProjectedTeam Team(CollegeTeamState team)
        => new() { Ranking = team.Ranking, TeamId = team.TeamId, TeamName = team.Name, Wins = team.Wins, Losses = team.Losses };

    private static CollegeProjectedTeam Team(CollegePostseasonService.Selection selection)
    {
        var team = Team(selection.Team);
        team.SelectionReason = selection.ConferenceChampion ? "Conference champion auto-bid" : "At-large";
        return team;
    }
}
