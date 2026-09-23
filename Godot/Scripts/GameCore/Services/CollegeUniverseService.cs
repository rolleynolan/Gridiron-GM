using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class CollegeUniverseService
{
    public const int RegularSeasonWeeks = 12;
    private static readonly string[] DevelopmentRosterPositions =
    {
        "QB", "RB", "WR", "WR", "TE", "OT", "OG", "C",
        "EDGE", "DT", "LB", "CB", "CB", "S", "K", "P",
    };

    private readonly GameCoreContext _context;
    public CollegeUniverseService(GameCoreContext context) => _context = context;

    public static CollegeUniverseState CreateInitial(LeagueState league, CollegeUniverseState previousUniverse = null)
    {
        var universe = new CollegeUniverseState { SeasonYear = league.SeasonYear };
        universe.Teams = CollegeTeamCatalog.CreateTeams();
        var validTeamIds = universe.Teams.Select(team => team.TeamId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var playerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prospects = league.CollegeProspects ?? new List<CollegeProspectState>();
        for (var index = 0; index < prospects.Count; index++)
        {
            var prospect = prospects[index];
            if (prospect == null) continue;
            var team = universe.Teams[index % universe.Teams.Count];
            prospect.College = team.Name;
            prospect.CollegeTeamId = team.TeamId;
            prospect.CollegePlayerId = prospect.ProspectId;
            var player = new CollegePlayerState
            {
                PlayerId = prospect.ProspectId, Name = prospect.Name, TeamId = team.TeamId, Position = prospect.Position,
                Overall = prospect.Overall, Potential = prospect.Potential, Age = prospect.Age, ClassYear = 4,
                CollegeYear = 4, PlayableSeasonsUsed = 4, DraftEligible = true,
            };
            if (playerIds.Add(player.PlayerId)) universe.Players.Add(player);
        }
        foreach (var player in previousUniverse?.Players?.Where(player => player != null).OrderBy(player => player.PlayerId, StringComparer.Ordinal) ?? Enumerable.Empty<CollegePlayerState>())
        {
            if (!validTeamIds.Contains(player.TeamId) || player.CollegeYear >= 5 || player.PlayableSeasonsUsed >= 4 || !playerIds.Add(player.PlayerId))
                continue;
            ArchiveAndResetReturningPlayer(player, previousUniverse.SeasonYear);
            universe.Players.Add(player);
        }
        // Underclassmen make the roster and standings world persist beyond only the current draft pool.
        foreach (var team in universe.Teams)
        for (var slot = 0; slot < DevelopmentRosterPositions.Length; slot++)
        {
            var position = DevelopmentRosterPositions[slot];
            var requiredAtPosition = DevelopmentRosterPositions.Take(slot + 1).Count(candidate => candidate == position);
            if (universe.Players.Count(player => string.Equals(player.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) && string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase)) >= requiredAtPosition)
                continue;
            var value = StableValue($"{league.FranchiseMetadata?.World?.Seed}-{team.TeamId}-{slot}");
            var player = new CollegePlayerState
            {
                PlayerId = $"college-{league.SeasonYear}-{team.TeamId}-{slot + 1}", Name = $"{team.Abbreviation} Prospect {slot + 1}", TeamId = team.TeamId,
                Position = position, Overall = 58 + value % 19,
                Potential = 70 + value % 23,
                Age = previousUniverse == null ? 19 + value % 3 : 18,
                ClassYear = previousUniverse == null ? 1 + value % 3 : 1,
                CollegeYear = previousUniverse == null ? 1 + value % 3 : 1,
                PlayableSeasonsUsed = previousUniverse == null ? 1 + value % 3 : 1,
                DraftEligible = false,
            };
            if (playerIds.Add(player.PlayerId)) universe.Players.Add(player);
        }
        foreach (var team in universe.Teams)
        {
            var value = StableValue($"{league.FranchiseMetadata?.World?.Seed}-{league.SeasonYear}-{team.TeamId}-redshirt");
            var position = DevelopmentRosterPositions[value % DevelopmentRosterPositions.Length];
            var redshirt = new CollegePlayerState
            {
                PlayerId = $"college-{league.SeasonYear}-{team.TeamId}-redshirt",
                Name = $"{team.Abbreviation} Prospect RS",
                TeamId = team.TeamId,
                Position = position,
                Overall = 56 + value % 18,
                Potential = 71 + value % 22,
                Age = 18,
                ClassYear = 1,
                CollegeYear = 1,
                PlayableSeasonsUsed = 0,
                IsRedshirted = true,
                DraftEligible = false,
            };
            if (playerIds.Add(redshirt.PlayerId)) universe.Players.Add(redshirt);
        }
        universe.Schedule = BuildSchedule(universe.Teams);
        RefreshRankings(universe);
        return universe;
    }

    private static void ArchiveAndResetReturningPlayer(CollegePlayerState player, int completedSeasonYear)
    {
        player.CareerStats ??= new List<CollegePlayerSeasonStats>();
        if (player.GamesPlayed > 0 && !player.CareerStats.Any(record => record != null && record.SeasonYear == completedSeasonYear))
            player.CareerStats.Add(new CollegePlayerSeasonStats
            {
                SeasonYear = completedSeasonYear,
                TeamId = player.TeamId,
                GamesPlayed = player.GamesPlayed,
                PassingYards = player.PassingYards,
                RushingYards = player.RushingYards,
                ReceivingYards = player.ReceivingYards,
                Touchdowns = player.Touchdowns,
            });
        player.Age++;
        player.CollegeYear++;
        player.PlayableSeasonsUsed++;
        player.ClassYear = Math.Clamp(player.PlayableSeasonsUsed, 1, 4);
        player.IsRedshirted = false;
        player.DraftEligible = false;
        player.DraftDecision = "Pending";
        player.DraftDecisionReason = "";
        player.DraftStock = "Season outlook pending";
        player.GamesPlayed = 0;
        player.PassingYards = 0;
        player.RushingYards = 0;
        player.ReceivingYards = 0;
        player.Touchdowns = 0;
        player.CurrentInjury = new CollegePlayerInjuryState();
    }

    public void AdvanceToProWeek(int absoluteWeek)
    {
        var league = _context?.ActiveLeague;
        var universe = league?.CollegeUniverse;
        if (universe == null || absoluteWeek <= universe.LastAdvancedAbsoluteWeek)
            return;

        for (var week = universe.LastAdvancedAbsoluteWeek + 1; week <= absoluteWeek; week++)
        {
            if (week > 1)
                CollegePlayerInjuryService.RecoverOneWeek(universe, week);
            foreach (var game in universe.Schedule.Where(game => game.ProAbsoluteWeek == week && !string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)).OrderBy(game => game.GameId, StringComparer.Ordinal))
                ResolveGame(universe, game);
        }
        universe.LastAdvancedAbsoluteWeek = absoluteWeek;
        RefreshRankings(universe);
        CollegePlayerDevelopmentService.ApplyCompletedSeasonDevelopment(league);
        CollegePostseasonService.EnsureCompleted(universe);
        CollegeAwardsService.EnsureAwards(universe);
    }

    public string GetCompactProspectContext(string prospectId)
    {
        var league = _context?.ActiveLeague;
        var prospect = league?.CollegeProspects?.FirstOrDefault(item => string.Equals(item?.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase));
        var universe = league?.CollegeUniverse;
        var player = universe?.Players?.FirstOrDefault(item => string.Equals(item?.PlayerId, prospect?.CollegePlayerId ?? prospectId, StringComparison.OrdinalIgnoreCase));
        var team = universe?.Teams?.FirstOrDefault(item => string.Equals(item?.TeamId, prospect?.CollegeTeamId, StringComparison.OrdinalIgnoreCase));
        if (player == null || team == null)
        {
            if (prospect == null || string.IsNullOrWhiteSpace(prospect.College))
                return "College context is unavailable.";
            var career = prospect.CollegeCareerStats ?? new List<CollegePlayerSeasonStats>();
            var careerText = career.Count == 0 ? "No archived college statistics" : $"{career.Sum(record => record.GamesPlayed)} GP | {career.Sum(record => record.PassingYards + record.RushingYards + record.ReceivingYards):N0} YD | {career.Sum(record => record.Touchdowns)} TD";
            return $"COLLEGE CONTEXT\n{prospect.College} | {prospect.DeclarationStatus} | {prospect.DraftStock}\nCollege career: {careerText}";
        }
        var development = player.DevelopmentHistory?.LastOrDefault(record => record != null && record.SeasonYear == universe.SeasonYear);
        var developmentContext = development == null ? "" : $"\nDevelopment: {development.Reason}";
        var injuryContext = player.CurrentInjury?.IsActive == true ? $"\nInjury: {player.CurrentInjury.Name} · {player.CurrentInjury.WeeksRemaining} week(s) remaining" : "";
        var eligibility = player.IsRedshirted
            ? $"Redshirt · college year {player.CollegeYear} · 4 seasons remaining"
            : $"Year {player.CollegeYear} · class {player.ClassYear} · {Math.Max(0, 4 - player.PlayableSeasonsUsed)} seasons remaining";
        return $"COLLEGE CONTEXT\n#{team.Ranking} {team.Name} ({team.Wins}-{team.Losses}) | {eligibility} | {player.GamesPlayed} GP | {player.Touchdowns} TD{developmentContext}{injuryContext}";
    }

    private static List<CollegeScheduledGame> BuildSchedule(IReadOnlyList<CollegeTeamState> teams)
    {
        var schedule = new List<CollegeScheduledGame>(teams.Count * RegularSeasonWeeks / 2);
        var rotation = teams.ToList();
        for (var week = 1; week <= RegularSeasonWeeks; week++)
        {
            for (var index = 0; index < rotation.Count / 2; index++)
            {
                var left = rotation[index];
                var right = rotation[rotation.Count - 1 - index];
                var home = (week + index) % 2 == 0 ? left : right;
                var away = ReferenceEquals(home, left) ? right : left;
                schedule.Add(new CollegeScheduledGame { GameId = $"college-{week}-{index + 1}", ProAbsoluteWeek = week, HomeTeamId = home.TeamId, AwayTeamId = away.TeamId });
            }

            var last = rotation[^1];
            rotation.RemoveAt(rotation.Count - 1);
            rotation.Insert(1, last);
        }
        return schedule;
    }

    private static void ResolveGame(CollegeUniverseState universe, CollegeScheduledGame game)
    {
        var home = universe.Teams.First(team => team.TeamId == game.HomeTeamId);
        var away = universe.Teams.First(team => team.TeamId == game.AwayTeamId);
        var homeScore = Score(universe, home.TeamId, game.ProAbsoluteWeek, 3);
        var awayScore = Score(universe, away.TeamId, game.ProAbsoluteWeek, 0);
        if (homeScore == awayScore) homeScore++;
        var winnerId = homeScore > awayScore ? home.TeamId : away.TeamId;
        if (winnerId == home.TeamId) { home.Wins++; away.Losses++; } else { away.Wins++; home.Losses++; }
        universe.Results.Add(new CollegeGameResult { GameId = game.GameId, ProAbsoluteWeek = game.ProAbsoluteWeek, HomeTeamId = home.TeamId, AwayTeamId = away.TeamId, HomeScore = homeScore, AwayScore = awayScore, WinnerTeamId = winnerId });
        game.Status = "final";
        ApplyPlayerStats(universe, home.TeamId, homeScore, game.ProAbsoluteWeek);
        ApplyPlayerStats(universe, away.TeamId, awayScore, game.ProAbsoluteWeek);
        CollegePlayerInjuryService.ApplyDeterministicGameInjury(universe, game);
    }

    private static int Score(CollegeUniverseState universe, string teamId, int week, int bonus)
    {
        var strength = universe.Players.Where(player => player.TeamId == teamId && CollegePlayerInjuryService.IsAvailableForGame(player)).OrderByDescending(player => player.Overall).Take(16).DefaultIfEmpty().Average(player => player?.Overall ?? 60);
        return Math.Clamp((int)Math.Round((strength - 54) * .62) + StableValue($"{teamId}-{week}") % 17 + bonus, 10, 55);
    }

    private static void ApplyPlayerStats(CollegeUniverseState universe, string teamId, int score, int week)
    {
        foreach (var player in universe.Players.Where(player => player.TeamId == teamId && CollegePlayerInjuryService.IsAvailableForGame(player)))
        {
            var value = StableValue($"{player.PlayerId}-{week}"); player.GamesPlayed++;
            if (player.Position == "QB") player.PassingYards += 110 + value % 190;
            if (player.Position == "RB") player.RushingYards += 25 + value % 95;
            if (player.Position == "WR" || player.Position == "TE") player.ReceivingYards += 20 + value % 100;
            if (new[] { "QB", "RB", "WR", "TE" }.Contains(player.Position)) player.Touchdowns += (value + score) % 4 == 0 ? 1 : 0;
        }
    }

    private static void RefreshRankings(CollegeUniverseState universe)
    {
        var ordered = universe.Teams.OrderByDescending(team => team.Wins).ThenBy(team => team.Losses).ThenByDescending(team => universe.Players.Where(player => player.TeamId == team.TeamId).Average(player => player.Overall)).ThenBy(team => team.Name, StringComparer.Ordinal).ToList();
        for (var index = 0; index < ordered.Count; index++) ordered[index].Ranking = index + 1;
    }
    private static int StableValue(string value) { unchecked { uint hash = 2166136261; foreach (var character in value) hash = (hash ^ character) * 16777619; return (int)(hash & 0x7fffffff); } }
}
