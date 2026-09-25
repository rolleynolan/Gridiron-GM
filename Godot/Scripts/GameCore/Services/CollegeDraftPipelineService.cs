using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Resolves the college season's read-only declaration outcomes before the pro draft opens.
public sealed class CollegeDraftPipelineService
{
    private readonly GameCoreContext _context;

    public CollegeDraftPipelineService(GameCoreContext context) => _context = context;

    public CollegeDraftClassResult FinalizeCurrentDraftClass()
    {
        var result = new CollegeDraftClassResult();
        var league = _context?.ActiveLeague;
        var universe = league?.CollegeUniverse;
        if (league == null || universe == null || universe.DraftClassFinalized)
        {
            result.AlreadyFinalized = universe?.DraftClassFinalized == true;
            return result;
        }
        if (universe.Schedule.Any(game => game != null && !string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase)))
            return result;

        league.CollegeProspects ??= new List<CollegeProspectState>();
        var prospectsByPlayerId = league.CollegeProspects
            .Where(prospect => prospect != null && !string.IsNullOrWhiteSpace(prospect.CollegePlayerId))
            .GroupBy(prospect => prospect.CollegePlayerId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var teamsById = universe.Teams.Where(team => team != null).ToDictionary(team => team.TeamId, StringComparer.OrdinalIgnoreCase);
        var declaredPlayerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var player in universe.Players.Where(player => player != null).OrderBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase))
        {
            var declares = ShouldDeclare(player, league.SeasonYear);
            player.DraftStock = GetDraftStock(player);
            player.DraftEligible = declares;
            player.DraftDecision = declares ? "Declared" : "Returned";
            player.DraftDecisionReason = BuildDecisionReason(player, declares);
            if (!declares)
            {
                result.ReturnedCount++;
                continue;
            }

            result.DeclaredCount++;
            declaredPlayerIds.Add(player.PlayerId);
            if (!prospectsByPlayerId.TryGetValue(player.PlayerId, out var prospect))
            {
                teamsById.TryGetValue(player.TeamId, out var team);
                prospect = new CollegeProspectState
                {
                    ProspectId = $"draft-{league.SeasonYear + 1}-{player.PlayerId}",
                    CollegePlayerId = player.PlayerId,
                    CollegeTeamId = player.TeamId,
                    College = team?.Name ?? "",
                    Name = player.Name,
                    Position = Utilities.DepthChartRules.ProEntryPosition(player.Position, player.PlayerId),
                    Overall = player.Overall,
                    Potential = player.Potential,
                    Age = player.Age,
                    DraftClassYear = league.SeasonYear + 1,
                };
                league.CollegeProspects.Add(prospect);
                prospectsByPlayerId[player.PlayerId] = prospect;
                result.AddedToDraftPool++;
            }

            prospect.DeclarationStatus = "Declared";
            prospect.DeclarationRationale = player.DraftDecisionReason;
            prospect.DraftStock = player.DraftStock;
            prospect.CollegePlayerId = player.PlayerId;
            prospect.CollegeTeamId = player.TeamId;
            prospect.CollegeCareerStats = CopyCollegeCareer(player, league.SeasonYear);
        }

        // A declared player leaves the college-owned player pool; the linked draft record is now authoritative.
        universe.Players = universe.Players.Where(player => player == null || !declaredPlayerIds.Contains(player.PlayerId)).ToList();
        universe.DraftClassFinalized = true;
        ProspectEvaluationService.EnsureEvaluations(league);
        return result;
    }

    private static List<CollegePlayerSeasonStats> CopyCollegeCareer(CollegePlayerState player, int currentSeasonYear)
    {
        var seasons = (player.CareerStats ?? new List<CollegePlayerSeasonStats>())
            .Where(record => record != null)
            .Select(CopyStats)
            .ToList();
        if (player.GamesPlayed > 0 && seasons.All(record => record.SeasonYear != currentSeasonYear))
            seasons.Add(new CollegePlayerSeasonStats
            {
                SeasonYear = currentSeasonYear,
                TeamId = player.TeamId,
                GamesPlayed = player.GamesPlayed,
                PassingYards = player.PassingYards,
                RushingYards = player.RushingYards,
                ReceivingYards = player.ReceivingYards,
                Touchdowns = player.Touchdowns,
            });
        return seasons.OrderBy(record => record.SeasonYear).ToList();
    }

    private static CollegePlayerSeasonStats CopyStats(CollegePlayerSeasonStats record)
        => new()
        {
            SeasonYear = record.SeasonYear,
            TeamId = record.TeamId,
            GamesPlayed = record.GamesPlayed,
            PassingYards = record.PassingYards,
            RushingYards = record.RushingYards,
            ReceivingYards = record.ReceivingYards,
            Touchdowns = record.Touchdowns,
        };

    private static bool ShouldDeclare(CollegePlayerState player, int seasonYear)
    {
        if (player.CollegeYear >= 5 || player.PlayableSeasonsUsed >= 4)
            return true;
        if (player.CollegeYear < 3)
            return false;

        var projection = player.Overall + ((player.Potential - player.Overall) / 2) + Math.Min(6, player.Touchdowns / 2);
        if (projection >= 78)
            return true;
        return StableValue($"{seasonYear}-{player.PlayerId}") % 100 < Math.Clamp(projection - 58, 8, 28);
    }

    private static string GetDraftStock(CollegePlayerState player)
    {
        var score = player.Overall + ((player.Potential - player.Overall) / 2) + Math.Min(5, player.Touchdowns / 2);
        return score >= 84 ? "Top prospect" : score >= 77 ? "First-round range" : score >= 70 ? "Day-two range" : "Developmental range";
    }

    private static string BuildDecisionReason(CollegePlayerState player, bool declares)
    {
        if (player.CollegeYear >= 5 || player.PlayableSeasonsUsed >= 4)
            return $"Exhausted college eligibility with a {player.DraftStock.ToLowerInvariant()} outlook.";
        return declares
            ? $"Eligible after three college years and declared with a {player.DraftStock.ToLowerInvariant()} outlook."
            : $"Returning to school to continue development after a {player.DraftStock.ToLowerInvariant()} outlook.";
    }

    private static int StableValue(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in value ?? "") hash = (hash ^ character) * 16777619;
            return (int)(hash & 0x7fffffff);
        }
    }
}

public sealed class CollegeDraftClassResult
{
    public bool AlreadyFinalized { get; set; }
    public int DeclaredCount { get; set; }
    public int ReturnedCount { get; set; }
    public int AddedToDraftPool { get; set; }
}
