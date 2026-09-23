using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// A compact completed postseason slate; it does not alter pro scheduling, standings, or player pools.
public static class CollegePostseasonService
{
    public static void EnsureCompleted(CollegeUniverseState universe)
    {
        if (universe?.Postseason?.Completed == true || universe?.Schedule?.Any(game => game == null || !string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)) != false)
            return;
        universe.Postseason ??= new CollegePostseasonState();
        var teams = universe.Teams.Where(team => team != null && team.Ranking > 0).OrderBy(team => team.Ranking).ToList();
        if (teams.Count < 8) return;
        var first = Resolve(universe, "College Playoff Semifinal · 1 vs 4", teams[0], teams[3]);
        var second = Resolve(universe, "College Playoff Semifinal · 2 vs 3", teams[1], teams[2]);
        universe.Postseason.Games.Add(first); universe.Postseason.Games.Add(second);
        universe.Postseason.Games.Add(Resolve(universe, "Coastal Classic · 5 vs 8", teams[4], teams[7]));
        universe.Postseason.Games.Add(Resolve(universe, "Heartland Bowl · 6 vs 7", teams[5], teams[6]));
        var winnerA = teams.First(team => team.TeamId == first.WinnerTeamId);
        var winnerB = teams.First(team => team.TeamId == second.WinnerTeamId);
        universe.Postseason.Games.Add(Resolve(universe, "College National Championship", winnerA, winnerB));
        universe.Postseason.Completed = true;
    }

    private static CollegePostseasonGame Resolve(CollegeUniverseState universe, string label, CollegeTeamState home, CollegeTeamState away)
    {
        var homeScore = Score(universe, home.TeamId, label, 2); var awayScore = Score(universe, away.TeamId, label, 0);
        if (homeScore == awayScore) homeScore++;
        return new CollegePostseasonGame { Label = label, HomeTeamId = home.TeamId, AwayTeamId = away.TeamId, HomeScore = homeScore, AwayScore = awayScore, WinnerTeamId = homeScore > awayScore ? home.TeamId : away.TeamId };
    }
    private static int Score(CollegeUniverseState universe, string teamId, string salt, int bonus) => Math.Clamp((int)Math.Round(universe.Players.Where(player => player.TeamId == teamId && CollegePlayerInjuryService.IsAvailableForGame(player)).DefaultIfEmpty().Average(player => player?.Overall ?? 60) * .35) + StableValue($"{teamId}-{salt}") % 12 + bonus, 10, 50);
    private static int StableValue(string value) { unchecked { uint hash = 2166136261; foreach (var character in value ?? "") hash = (hash ^ character) * 16777619; return (int)(hash & 0x7fffffff); } }
}
