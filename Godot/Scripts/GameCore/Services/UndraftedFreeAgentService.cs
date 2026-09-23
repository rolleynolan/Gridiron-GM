using System;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class UndraftedFreeAgentService
{
    public static int OpenMarket(LeagueState league)
    {
        if (league?.Draft?.IsCompleted != true) return 0;
        league.FreeAgents ??= new System.Collections.Generic.List<PlayerState>();
        league.CollegeProspects ??= new System.Collections.Generic.List<CollegeProspectState>();
        var undrafted = league.CollegeProspects
            .Where(prospect => prospect != null
                && prospect.DraftClassYear <= league.SeasonYear + 1
                && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId))
            .ToList();
        var added = 0;
        foreach (var prospect in undrafted)
        {
            var playerId = string.IsNullOrWhiteSpace(prospect.CollegePlayerId) ? $"udfa-{league.SeasonYear}-{prospect.ProspectId}" : prospect.CollegePlayerId;
            if (league.FreeAgents.All(player => !string.Equals(player?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase)))
            {
                league.FreeAgents.Add(new PlayerState
                {
                    PlayerId = playerId,
                    Name = prospect.Name,
                    Position = prospect.Position,
                    Trait = string.IsNullOrWhiteSpace(prospect.Trait) ? "Development-minded" : prospect.Trait,
                    College = prospect.College,
                    CollegePlayerId = prospect.CollegePlayerId,
                    CollegeCareerStats = (prospect.CollegeCareerStats ?? new System.Collections.Generic.List<CollegePlayerSeasonStats>()).Where(record => record != null).Select(record => new CollegePlayerSeasonStats { SeasonYear = record.SeasonYear, TeamId = record.TeamId, GamesPlayed = record.GamesPlayed, PassingYards = record.PassingYards, RushingYards = record.RushingYards, ReceivingYards = record.ReceivingYards, Touchdowns = record.Touchdowns }).ToList(),
                    Overall = prospect.Overall,
                    Potential = prospect.Potential,
                    Age = prospect.Age,
                    Status = "Undrafted Free Agent",
                    Morale = 50,
                    MoraleTrend = "Stable",
                    Contract = new PlayerContractState { ContractType = "Undrafted Free Agent" },
                });
                added++;
            }
        }
        league.CollegeProspects = league.CollegeProspects.Except(undrafted).ToList();
        return added;
    }

    public static bool IsUndraftedRookie(PlayerState player)
        => string.Equals(player?.Status, "Undrafted Free Agent", StringComparison.OrdinalIgnoreCase)
            || string.Equals(player?.Contract?.ContractType, "Undrafted Free Agent", StringComparison.OrdinalIgnoreCase);
}
