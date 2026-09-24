using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Versioned 12-team postseason rules; it does not alter pro scheduling, standings, or player pools.
public static class CollegePostseasonService
{
    public const string RuleVersion = "college-postseason-12-team-v1";

    public sealed record Selection(CollegeTeamState Team, bool ConferenceChampion);

    public static List<Selection> SelectField(IEnumerable<CollegeTeamState> source)
    {
        var ranked = (source ?? Array.Empty<CollegeTeamState>()).Where(team => team != null && team.Ranking > 0).OrderBy(team => team.Ranking).ThenBy(team => team.Name, StringComparer.Ordinal).ToList();
        var automatic = ranked.GroupBy(team => team.Conference ?? "", StringComparer.OrdinalIgnoreCase).Select(group => group.First()).OrderBy(team => team.Ranking).Take(5).ToList();
        return automatic.Concat(ranked.Where(team => automatic.All(champion => champion.TeamId != team.TeamId)).Take(12 - automatic.Count))
            .OrderBy(team => team.Ranking).Select(team => new Selection(team, automatic.Any(champion => champion.TeamId == team.TeamId))).ToList();
    }

    public static void EnsureCompleted(CollegeUniverseState universe)
    {
        if (universe?.Postseason?.Completed == true || universe?.Schedule?.Any(game => game == null || !string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)) != false)
            return;
        universe.Postseason ??= new CollegePostseasonState();
        universe.Postseason.RuleVersion = RuleVersion;
        universe.Postseason.Games.Clear();
        var field = SelectField(universe.Teams);
        if (field.Count < 12) return;
        var seeds = field.Select(selection => selection.Team).ToList();
        var allTeams = universe.Teams.Where(team => team != null).ToDictionary(team => team.TeamId, StringComparer.OrdinalIgnoreCase);
        CollegeTeamState Winner(CollegePostseasonGame game) => allTeams[game.WinnerTeamId];

        var r1a = Resolve(universe, "College Playoff First Round · 8 vs 9", "First Round", seeds[7], 8, seeds[8], 9);
        var r1b = Resolve(universe, "College Playoff First Round · 5 vs 12", "First Round", seeds[4], 5, seeds[11], 12);
        var r1c = Resolve(universe, "College Playoff First Round · 7 vs 10", "First Round", seeds[6], 7, seeds[9], 10);
        var r1d = Resolve(universe, "College Playoff First Round · 6 vs 11", "First Round", seeds[5], 6, seeds[10], 11);
        universe.Postseason.Games.AddRange(new[] { r1a, r1b, r1c, r1d });

        var q1 = Resolve(universe, "College Playoff Quarterfinal · 1 bracket", "Quarterfinal", seeds[0], 1, Winner(r1a), SeedOf(Winner(r1a), seeds));
        var q2 = Resolve(universe, "College Playoff Quarterfinal · 4 bracket", "Quarterfinal", seeds[3], 4, Winner(r1b), SeedOf(Winner(r1b), seeds));
        var q3 = Resolve(universe, "College Playoff Quarterfinal · 2 bracket", "Quarterfinal", seeds[1], 2, Winner(r1c), SeedOf(Winner(r1c), seeds));
        var q4 = Resolve(universe, "College Playoff Quarterfinal · 3 bracket", "Quarterfinal", seeds[2], 3, Winner(r1d), SeedOf(Winner(r1d), seeds));
        universe.Postseason.Games.AddRange(new[] { q1, q2, q3, q4 });

        var s1 = Resolve(universe, "College Playoff Semifinal · National", "Semifinal", Winner(q1), SeedOf(Winner(q1), seeds), Winner(q2), SeedOf(Winner(q2), seeds));
        var s2 = Resolve(universe, "College Playoff Semifinal · American", "Semifinal", Winner(q3), SeedOf(Winner(q3), seeds), Winner(q4), SeedOf(Winner(q4), seeds));
        universe.Postseason.Games.AddRange(new[] { s1, s2 });
        universe.Postseason.Games.Add(Resolve(universe, "College National Championship", "Championship", Winner(s1), SeedOf(Winner(s1), seeds), Winner(s2), SeedOf(Winner(s2), seeds)));

        var bowlTeams = universe.Teams.Where(team => team != null && field.All(selection => selection.Team.TeamId != team.TeamId)).OrderBy(team => team.Ranking).Take(8).ToList();
        var bowlNames = new[] { "Coastal Classic", "Heartland Bowl", "Lakeside Bowl", "Frontier Bowl" };
        for (var index = 0; index < bowlNames.Length; index++)
            universe.Postseason.Games.Add(Resolve(universe, bowlNames[index], "Bowl", bowlTeams[index], bowlTeams[index].Ranking, bowlTeams[7 - index], bowlTeams[7 - index].Ranking));
        universe.Postseason.Completed = true;
    }

    private static int SeedOf(CollegeTeamState team, IReadOnlyList<CollegeTeamState> seeds) => seeds.ToList().FindIndex(candidate => candidate.TeamId == team.TeamId) + 1;

    private static CollegePostseasonGame Resolve(CollegeUniverseState universe, string label, string stage, CollegeTeamState home, int homeSeed, CollegeTeamState away, int awaySeed)
    {
        var homeScore = Score(universe, home.TeamId, label, 2); var awayScore = Score(universe, away.TeamId, label, 0);
        if (homeScore == awayScore) homeScore++;
        return new CollegePostseasonGame { Label = label, Stage = stage, HomeSeed = homeSeed, AwaySeed = awaySeed, HomeTeamId = home.TeamId, AwayTeamId = away.TeamId, HomeScore = homeScore, AwayScore = awayScore, WinnerTeamId = homeScore > awayScore ? home.TeamId : away.TeamId };
    }
    private static int Score(CollegeUniverseState universe, string teamId, string salt, int bonus) => Math.Clamp((int)Math.Round(universe.Players.Where(player => player.TeamId == teamId && CollegePlayerInjuryService.IsAvailableForGame(player)).DefaultIfEmpty().Average(player => player?.Overall ?? 60) * .35) + StableValue($"{teamId}-{salt}") % 12 + bonus, 10, 50);
    private static int StableValue(string value) { unchecked { uint hash = 2166136261; foreach (var character in value ?? "") hash = (hash ^ character) * 16777619; return (int)(hash & 0x7fffffff); } }
}
