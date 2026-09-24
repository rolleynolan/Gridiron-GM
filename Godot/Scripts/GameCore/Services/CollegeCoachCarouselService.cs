using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

// Advances a lightweight CPU-only college coaching carousel between seasons.
public static class CollegeCoachCarouselService
{
    public const int MaximumAnnualChanges = 12;

    public static List<CollegeCoachChangeRecord> Apply(
        LeagueState league,
        CollegeUniverseState previousUniverse,
        IReadOnlyList<CollegeTeamState> nextTeams,
        GeneratedNamePools names)
    {
        var changes = new List<CollegeCoachChangeRecord>();
        var previousTeams = (previousUniverse?.Teams ?? new List<CollegeTeamState>())
            .Where(team => team != null)
            .ToDictionary(team => team.TeamId, StringComparer.OrdinalIgnoreCase);
        var changeTeamIds = previousTeams.Values
            .Where(team => team.HeadCoach != null && (team.HeadCoach.Age >= 68 ||
                (team.Losses >= 8 && StableValue($"{league.SeasonYear}-{team.TeamId}-coach-review") % 100 < 55)))
            .OrderByDescending(team => team.HeadCoach.Age >= 68 ? 1000 + team.HeadCoach.Age : team.Losses * 10 - team.Wins * 4)
            .ThenBy(team => team.TeamId, StringComparer.Ordinal)
            .Take(MaximumAnnualChanges)
            .Select(team => team.TeamId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var team in nextTeams.Where(team => team != null).OrderBy(team => team.TeamId, StringComparer.Ordinal))
        {
            previousTeams.TryGetValue(team.TeamId, out var previousTeam);
            var previousCoach = previousTeam?.HeadCoach;
            if (previousCoach != null && !changeTeamIds.Contains(team.TeamId))
            {
                team.HeadCoach = new CollegeCoachState
                {
                    CoachId = previousCoach.CoachId,
                    Name = previousCoach.Name,
                    Age = Math.Clamp(previousCoach.Age + 1, 30, 80),
                    ProgramRating = Math.Clamp(previousCoach.ProgramRating, 40, 99),
                    RecruitingRating = Math.Clamp(previousCoach.RecruitingRating, 40, 99),
                    HiredSeasonYear = previousCoach.HiredSeasonYear,
                    SeasonsAtProgram = Math.Max(1, previousCoach.SeasonsAtProgram + 1),
                };
                continue;
            }

            team.HeadCoach = CreateCoach(league, team, names, previousCoach == null ? 0 : previousCoach.SeasonsAtProgram + 1);
            if (previousCoach != null)
            {
                changes.Add(new CollegeCoachChangeRecord
                {
                    SeasonYear = league.SeasonYear,
                    TeamId = team.TeamId,
                    PreviousCoachName = previousCoach.Name,
                    NewCoachName = team.HeadCoach.Name,
                    Reason = previousCoach.Age >= 68
                        ? "The previous head coach retired; the program completed a background replacement search."
                        : $"The program changed direction after a {previousTeam.Wins}-{previousTeam.Losses} season.",
                });
            }
        }
        return changes;
    }

    public static CollegeCoachState CreateCoach(
        LeagueState league,
        CollegeTeamState team,
        GeneratedNamePools names,
        int cycle)
    {
        var seed = StableValue($"{league?.FranchiseMetadata?.World?.Seed}-{league?.SeasonYear}-{team.TeamId}-coach-{cycle}");
        var firstName = names?.MaleFirstNames?.Count > 0 ? names.MaleFirstNames[seed % names.MaleFirstNames.Count] : "Jordan";
        var lastSeed = StableValue($"{seed}-coach-last");
        var lastName = names?.LastNames?.Count > 0 ? names.LastNames[lastSeed % names.LastNames.Count] : "Hayes";
        return new CollegeCoachState
        {
            CoachId = $"college-coach-{team.TeamId}-{league?.SeasonYear}-{cycle}",
            Name = $"{firstName} {lastName}",
            Age = 36 + seed % 24,
            ProgramRating = 58 + StableValue($"{seed}-program") % 28,
            RecruitingRating = 58 + StableValue($"{seed}-recruiting") % 28,
            HiredSeasonYear = league?.SeasonYear ?? 0,
            SeasonsAtProgram = 1,
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
