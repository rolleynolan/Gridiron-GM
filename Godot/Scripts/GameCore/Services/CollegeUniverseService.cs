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
        var generatedNames = NamePoolService.Load();
        universe.CoachingChanges = CollegeCoachCarouselService.Apply(league, previousUniverse, universe.Teams, generatedNames);
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
        var returningPlayers = previousUniverse?.Players?
            .Where(player => player != null && validTeamIds.Contains(player.TeamId) && player.CollegeYear < 5 && player.PlayableSeasonsUsed < 4)
            .OrderBy(player => player.PlayerId, StringComparer.Ordinal)
            .ToList() ?? new List<CollegePlayerState>();
        var transferPlan = CollegeTransferPortalService.BuildPlan(league.SeasonYear, previousUniverse, returningPlayers, universe.Teams);
        var transfersByPlayer = transferPlan.ToDictionary(record => record.PlayerId, StringComparer.OrdinalIgnoreCase);
        foreach (var player in returningPlayers)
        {
            if (!playerIds.Add(player.PlayerId))
                continue;
            ArchiveAndResetReturningPlayer(player, previousUniverse.SeasonYear);
            if (transfersByPlayer.TryGetValue(player.PlayerId, out var transfer))
            {
                player.TeamId = transfer.ToTeamId;
                player.TransferHistory ??= new List<CollegeTransferRecord>();
                player.TransferHistory.Add(CopyTransfer(transfer));
                universe.Transfers.Add(CopyTransfer(transfer));
            }
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
            var previousTeam = previousUniverse?.Teams?.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase));
            var recruit = CollegeRecruitingService.CreateFreshman(league, team, previousTeam, position, slot + 1, false, generatedNames);
            var initialClassYear = previousUniverse == null
                ? 1 + StableValue($"{league.FranchiseMetadata?.World?.Seed}-{team.TeamId}-{slot}-initial-class") % 3
                : 1;
            recruit.Player.ClassYear = initialClassYear;
            recruit.Player.CollegeYear = initialClassYear;
            recruit.Player.PlayableSeasonsUsed = initialClassYear;
            recruit.Player.Age = 17 + initialClassYear;
            if (initialClassYear > 1)
                recruit.Player.RecruitingSummary = "";
            if (playerIds.Add(recruit.Player.PlayerId))
            {
                universe.Players.Add(recruit.Player);
                if (initialClassYear == 1)
                    universe.RecruitingClass.Add(recruit.Record);
            }
        }
        foreach (var team in universe.Teams)
        {
            var value = StableValue($"{league.FranchiseMetadata?.World?.Seed}-{league.SeasonYear}-{team.TeamId}-redshirt-position");
            var position = DevelopmentRosterPositions[value % DevelopmentRosterPositions.Length];
            var previousTeam = previousUniverse?.Teams?.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase));
            var recruit = CollegeRecruitingService.CreateFreshman(league, team, previousTeam, position, DevelopmentRosterPositions.Length + 1, true, generatedNames);
            if (playerIds.Add(recruit.Player.PlayerId))
            {
                universe.Players.Add(recruit.Player);
                universe.RecruitingClass.Add(recruit.Record);
            }
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

    private static CollegeTransferRecord CopyTransfer(CollegeTransferRecord record)
        => new()
        {
            SeasonYear = record.SeasonYear,
            PlayerId = record.PlayerId,
            PlayerName = record.PlayerName,
            Position = record.Position,
            FromTeamId = record.FromTeamId,
            ToTeamId = record.ToTeamId,
            Reason = record.Reason,
        };

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
        var playerStats = ApplyPlayerStats(universe, home.TeamId, homeScore, game.ProAbsoluteWeek)
            .Concat(ApplyPlayerStats(universe, away.TeamId, awayScore, game.ProAbsoluteWeek))
            .ToList();
        universe.Results.Add(new CollegeGameResult { GameId = game.GameId, ProAbsoluteWeek = game.ProAbsoluteWeek, HomeTeamId = home.TeamId, AwayTeamId = away.TeamId, HomeScore = homeScore, AwayScore = awayScore, WinnerTeamId = winnerId, PlayerStats = playerStats });
        game.Status = "final";
        CollegePlayerInjuryService.ApplyDeterministicGameInjury(universe, game);
    }

    private static int Score(CollegeUniverseState universe, string teamId, int week, int bonus)
    {
        var strength = universe.Players.Where(player => player.TeamId == teamId && CollegePlayerInjuryService.IsAvailableForGame(player)).OrderByDescending(player => player.Overall).Take(16).DefaultIfEmpty().Average(player => player?.Overall ?? 60);
        var coach = universe.Teams.FirstOrDefault(team => string.Equals(team.TeamId, teamId, StringComparison.OrdinalIgnoreCase))?.HeadCoach;
        var coachBonus = Math.Clamp(((coach?.ProgramRating ?? 65) - 65) / 10, -2, 2);
        return Math.Clamp((int)Math.Round((strength - 54) * .62) + StableValue($"{teamId}-{week}") % 17 + bonus + coachBonus, 10, 55);
    }

    private static List<CollegeGamePlayerStatLine> ApplyPlayerStats(CollegeUniverseState universe, string teamId, int score, int week)
    {
        var lines = new List<CollegeGamePlayerStatLine>();
        foreach (var player in universe.Players.Where(player => player.TeamId == teamId && CollegePlayerInjuryService.IsAvailableForGame(player)))
        {
            var value = StableValue($"{player.PlayerId}-{week}");
            var line = new CollegeGamePlayerStatLine
            {
                PlayerId = player.PlayerId,
                PlayerName = player.Name,
                TeamId = teamId,
                Position = player.Position,
                PassingYards = player.Position == "QB" ? 110 + value % 190 : 0,
                RushingYards = player.Position == "RB" ? 25 + value % 95 : 0,
                ReceivingYards = player.Position == "WR" || player.Position == "TE" ? 20 + value % 100 : 0,
                Touchdowns = new[] { "QB", "RB", "WR", "TE" }.Contains(player.Position) && (value + score) % 4 == 0 ? 1 : 0,
            };
            player.GamesPlayed++;
            player.PassingYards += line.PassingYards;
            player.RushingYards += line.RushingYards;
            player.ReceivingYards += line.ReceivingYards;
            player.Touchdowns += line.Touchdowns;
            lines.Add(line);
        }
        return lines;
    }

    private static void RefreshRankings(CollegeUniverseState universe)
    {
        var ordered = universe.Teams.OrderByDescending(team => team.Wins).ThenBy(team => team.Losses).ThenByDescending(team => universe.Players.Where(player => player.TeamId == team.TeamId).Average(player => player.Overall)).ThenBy(team => team.Name, StringComparer.Ordinal).ToList();
        for (var index = 0; index < ordered.Count; index++) ordered[index].Ranking = index + 1;
    }
    private static int StableValue(string value) { unchecked { uint hash = 2166136261; foreach (var character in value) hash = (hash ^ character) * 16777619; return (int)(hash & 0x7fffffff); } }
}
