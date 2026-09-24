using System;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Creates CPU-controlled freshman signings from program context, roster need, and bounded deterministic variation.
public static class CollegeRecruitingService
{
    public static CollegeRecruitingOutcome CreateFreshman(
        LeagueState league,
        CollegeTeamState team,
        CollegeTeamState previousTeam,
        string position,
        int sequence,
        bool willRedshirt,
        GeneratedNamePools names)
    {
        var seasonYear = league?.SeasonYear ?? 0;
        var worldSeed = league?.FranchiseMetadata?.World?.Seed ?? 0UL;
        var identityValue = StableValue($"{worldSeed}-{seasonYear}-{team?.TeamId}-{position}-{sequence}-{willRedshirt}");
        var programBaseline = StableValue($"{team?.TeamId}-recruiting-prestige") % 7 - 3;
        var recentSuccess = previousTeam == null ? 0 : Math.Clamp(previousTeam.Wins - previousTeam.Losses, -6, 6) / 2;
        var coachContext = Math.Clamp(((team?.HeadCoach?.RecruitingRating ?? 70) - 70) / 8, -3, 3);
        var boundedVariation = StableValue($"{identityValue}-talent") % 15 - 7;
        var overall = Math.Clamp(61 + programBaseline + recentSuccess + coachContext + boundedVariation, 52, 79);
        var potential = Math.Clamp(overall + 8 + StableValue($"{identityValue}-upside") % 14, overall, 95);
        var firstName = names?.MaleFirstNames?.Count > 0 ? names.MaleFirstNames[identityValue % names.MaleFirstNames.Count] : "Jordan";
        var lastNameValue = StableValue($"{identityValue}-last");
        var lastName = names?.LastNames?.Count > 0 ? names.LastNames[lastNameValue % names.LastNames.Count] : "Carter";
        var playerId = $"college-{seasonYear}-{team?.TeamId}-recruit-{sequence:D2}";
        var publicTier = overall >= 74 ? "Headline signing" : overall >= 66 ? "Priority signing" : "Developmental signing";
        var summary = willRedshirt
            ? $"{publicTier}; planned redshirt adds depth at {position} without consuming a playable season."
            : $"{publicTier}; signed to address the program's {position} roster need.";
        var player = new CollegePlayerState
        {
            PlayerId = playerId,
            Name = $"{firstName} {lastName}",
            TeamId = team?.TeamId ?? "",
            Position = position ?? "",
            Overall = overall,
            Potential = potential,
            Age = 18,
            ClassYear = 1,
            CollegeYear = 1,
            PlayableSeasonsUsed = willRedshirt ? 0 : 1,
            IsRedshirted = willRedshirt,
            DraftEligible = false,
            RecruitingSummary = summary,
        };
        return new CollegeRecruitingOutcome
        {
            Player = player,
            Record = new CollegeRecruitingRecord
            {
                SeasonYear = seasonYear,
                PlayerId = playerId,
                PlayerName = player.Name,
                Position = player.Position,
                TeamId = player.TeamId,
                PublicTier = publicTier,
                Summary = summary,
                WillRedshirt = willRedshirt,
            },
        };
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

public sealed class CollegeRecruitingOutcome
{
    public CollegePlayerState Player { get; set; } = new();
    public CollegeRecruitingRecord Record { get; set; } = new();
}
