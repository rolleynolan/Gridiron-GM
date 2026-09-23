using System;
using System.Collections.Generic;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public static class CollegeTeamCatalog
{
    public const int TeamCount = 128;

    private static readonly string[] Conferences =
    {
        "Northern", "Coastal", "Atlantic", "Frontier",
        "Great Plains", "Mountain", "Gulf", "Pacific",
    };

    private static readonly (string Id, string Name, string Abbreviation)[] FoundingPrograms =
    {
        ("nv", "North Valley", "NVU"), ("ls", "Lakeshore State", "LSS"),
        ("wt", "Western Tech", "WTE"), ("pr", "Pine Ridge", "PRU"),
        ("cu", "Coastal University", "COU"), ("ms", "Metro State", "MST"),
        ("rr", "Red River", "RRU"), ("sc", "Summit College", "SUM"),
        ("as", "Atlantic State", "AST"), ("pa", "Prairie A&M", "PAM"),
        ("ca", "Canyon University", "CAN"), ("gl", "Great Lakes", "GLU"),
        ("es", "Eastern State", "EST"), ("mv", "Mountain Valley", "MTV"),
        ("sv", "Southern Valley", "SOV"), ("ct", "Capital Tech", "CPT"),
    };

    private static readonly string[] Regions =
    {
        "Ashland", "Bayview", "Cedar Grove", "Copper Hills", "Desert Springs", "Elm Coast", "Fairmont",
        "Granite Bay", "Harbor Point", "Ironwood", "Juniper Valley", "Kingsport", "Long Prairie", "Mesa Vista",
        "Northgate", "Oak Ridge", "Pine Coast", "Quartz Valley", "Redstone", "Silver Lake", "Timberland",
        "Union Harbor", "Victory Plains", "Westhaven", "Yellow Pine", "Zephyr Coast", "Briar Ridge", "Crown Valley",
    };

    private static readonly (string NamePattern, string AbbreviationSuffix)[] InstitutionTypes =
    {
        ("{0} State", "ST"),
        ("{0} Tech", "TC"),
        ("University of {0}", "UN"),
        ("{0} A&M", "AM"),
    };

    public static List<CollegeTeamState> CreateTeams()
    {
        var teams = new List<CollegeTeamState>(TeamCount);
        foreach (var program in FoundingPrograms)
            AddTeam(teams, program.Id, program.Name, program.Abbreviation);

        foreach (var region in Regions)
        foreach (var institutionType in InstitutionTypes)
        {
            var regionCode = string.Concat(region[0], region[1]).ToUpperInvariant();
            AddTeam(
                teams,
                $"college-{teams.Count + 1:000}",
                string.Format(institutionType.NamePattern, region),
                $"{regionCode}{institutionType.AbbreviationSuffix}");
        }

        if (teams.Count != TeamCount)
            throw new InvalidOperationException($"College catalog must contain exactly {TeamCount} programs; found {teams.Count}.");
        return teams;
    }

    private static void AddTeam(List<CollegeTeamState> teams, string id, string name, string abbreviation)
    {
        teams.Add(new CollegeTeamState
        {
            TeamId = id,
            Name = name,
            Abbreviation = abbreviation,
            Conference = Conferences[teams.Count % Conferences.Length],
        });
    }
}
