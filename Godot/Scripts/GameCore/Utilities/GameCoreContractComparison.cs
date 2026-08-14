using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Services;

namespace GridironGM.GameCore.Utilities;

public sealed class ContractComparisonResult
{
    public bool Ok { get; set; }
    public bool PythonLeagueInactive { get; set; }
    public string Summary { get; set; } = "";
    public List<string> Steps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public static class GameCoreContractComparison
{
    public static ContractComparisonResult CompareCompactContracts(
        Godot.Collections.Dictionary dashboardPayload,
        Godot.Collections.Dictionary rosterPayload,
        Godot.Collections.Dictionary depthChartPayload,
        Godot.Collections.Dictionary schedulePayload,
        Godot.Collections.Dictionary standingsPayload)
    {
        var result = new ContractComparisonResult();
        var context = new GameCoreContext();
        var bootstrap = new LeagueBootstrapService(context);
        bootstrap.CreateTestLeague();

        CompareDashboard(result, dashboardPayload, new DashboardService(context).GetDashboardState());
        CompareRoster(result, rosterPayload, new RosterService(context).GetTeamRoster());
        CompareDepthChart(result, depthChartPayload, new DepthChartService(context).GetTeamDepthChart());
        CompareSchedule(result, schedulePayload, new ScheduleService(context).GetTeamSchedule());
        CompareStandings(result, standingsPayload, new StandingsService(context).GetStandings());

        result.Ok = result.Errors.Count == 0;
        result.Summary = result.Ok
            ? "C# / Python compact contracts matched."
            : $"{result.Errors.Count} contract shape issue(s) found.";
        return result;
    }

    private static void CompareDashboard(ContractComparisonResult result, Godot.Collections.Dictionary payload, DashboardStateResponse response)
    {
        const string endpoint = "dashboard_state";
        var errors = new List<string>();

        RequirePythonBool(payload, "ok", endpoint, errors);
        var dashboard = RequirePythonObject(payload, "dashboard", endpoint, errors);
        RequireCSharpProperty(typeof(DashboardStateResponse), nameof(DashboardStateResponse.Ok), endpoint, errors, typeof(bool));
        RequireCSharpProperty(typeof(DashboardStateResponse), nameof(DashboardStateResponse.Dashboard), endpoint, errors, typeof(DashboardDto));

        if (dashboard != null)
        {
            RequirePythonObject(dashboard, "team", endpoint, errors);
            RequirePythonObject(dashboard, "calendar", endpoint, errors);
            RequirePythonObject(dashboard, "next_game", endpoint, errors);
            RequirePythonObject(dashboard, "team_status", endpoint, errors);
            RequirePythonList(dashboard, "action_items", endpoint, errors);
            RequirePythonList(dashboard, "recent_results", endpoint, errors);
        }

        RequireCSharpProperty(typeof(DashboardDto), nameof(DashboardDto.Team), endpoint, errors, typeof(TeamSummaryDto));
        RequireCSharpProperty(typeof(DashboardDto), nameof(DashboardDto.Calendar), endpoint, errors, typeof(CalendarSummaryDto));
        RequireCSharpProperty(typeof(DashboardDto), nameof(DashboardDto.NextGame), endpoint, errors, typeof(NextGameDto));
        RequireCSharpProperty(typeof(DashboardDto), nameof(DashboardDto.TeamStatus), endpoint, errors, typeof(TeamStatusDto));
        RequireCSharpListProperty(typeof(DashboardDto), nameof(DashboardDto.ActionItems), typeof(ActionItemDto), endpoint, errors);
        RequireCSharpListProperty(typeof(DashboardDto), nameof(DashboardDto.RecentResults), typeof(RecentResultDto), endpoint, errors);

        RequireCSharpProperty(typeof(TeamSummaryDto), nameof(TeamSummaryDto.Name), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(TeamSummaryDto), nameof(TeamSummaryDto.Abbreviation), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(TeamSummaryDto), nameof(TeamSummaryDto.Record), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(CalendarSummaryDto), nameof(CalendarSummaryDto.Year), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(CalendarSummaryDto), nameof(CalendarSummaryDto.Week), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(CalendarSummaryDto), nameof(CalendarSummaryDto.Phase), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(NextGameDto), nameof(NextGameDto.Opponent), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(NextGameDto), nameof(NextGameDto.OpponentAbbreviation), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(NextGameDto), nameof(NextGameDto.HomeAway), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(NextGameDto), nameof(NextGameDto.Week), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(NextGameDto), nameof(NextGameDto.GameType), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(NextGameDto), nameof(NextGameDto.GameId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(TeamStatusDto), nameof(TeamStatusDto.RosterSize), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(TeamStatusDto), nameof(TeamStatusDto.Injuries), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(TeamStatusDto), nameof(TeamStatusDto.CapRoom), endpoint, errors, typeof(string));

        if (dashboard != null)
        {
            var actionItems = RequirePythonList(dashboard, "action_items", endpoint, errors);
            RequirePythonRowFields(actionItems, endpoint, "action_items[]", errors, "type", "title", "description", "primary_action");

            var recentResults = RequirePythonList(dashboard, "recent_results", endpoint, errors);
            RequirePythonRowFields(recentResults, endpoint, "recent_results[]", errors, "game_id", "week", "game_type", "home_team", "away_team", "home_score", "away_score", "winner", "summary");
        }

        RequireCSharpProperty(typeof(ActionItemDto), nameof(ActionItemDto.Type), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ActionItemDto), nameof(ActionItemDto.Title), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ActionItemDto), nameof(ActionItemDto.Description), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ActionItemDto), nameof(ActionItemDto.PrimaryAction), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.GameId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.Week), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.GameType), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.HomeTeam), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.AwayTeam), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.HomeScore), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.AwayScore), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.Winner), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(RecentResultDto), nameof(RecentResultDto.Summary), endpoint, errors, typeof(string));

        FinalizeEndpoint(result, endpoint, errors);
    }

    private static void CompareRoster(ContractComparisonResult result, Godot.Collections.Dictionary payload, TeamRosterResponse response)
    {
        const string endpoint = "team_roster";
        var errors = new List<string>();

        RequirePythonBool(payload, "ok", endpoint, errors);
        RequirePythonObject(payload, "team", endpoint, errors);
        RequirePythonObject(payload, "roster_status", endpoint, errors);
        var positionCounts = RequirePythonList(payload, "position_counts", endpoint, errors);
        var players = RequirePythonList(payload, "players", endpoint, errors);

        RequirePythonRowFields(positionCounts, endpoint, "position_counts[]", errors, "position", "count");
        RequirePythonRowFields(players, endpoint, "players[]", errors, "player_id", "name", "position", "overall", "age", "status", "injury", "depth_role");

        RequireCSharpProperty(typeof(TeamRosterResponse), nameof(TeamRosterResponse.Ok), endpoint, errors, typeof(bool));
        RequireCSharpProperty(typeof(TeamRosterResponse), nameof(TeamRosterResponse.Team), endpoint, errors, typeof(TeamIdentityDto));
        RequireCSharpProperty(typeof(TeamRosterResponse), nameof(TeamRosterResponse.RosterStatus), endpoint, errors, typeof(RosterStatusDto));
        RequireCSharpListProperty(typeof(TeamRosterResponse), nameof(TeamRosterResponse.PositionCounts), typeof(PositionCountDto), endpoint, errors);
        RequireCSharpListProperty(typeof(TeamRosterResponse), nameof(TeamRosterResponse.Players), typeof(PlayerRowDto), endpoint, errors);

        RequireCSharpIdentityFields(endpoint, errors);
        RequireCSharpProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.IsValid), endpoint, errors, typeof(bool));
        RequireCSharpProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.RosterSize), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.RosterLimit), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.RequiredCuts), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.OpenSlots), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.InjuredCount), endpoint, errors, typeof(int));
        RequireCSharpListProperty(typeof(RosterStatusDto), nameof(RosterStatusDto.Issues), typeof(string), endpoint, errors);
        RequireCSharpProperty(typeof(PositionCountDto), nameof(PositionCountDto.Position), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(PositionCountDto), nameof(PositionCountDto.Count), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.PlayerId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.Name), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.Position), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.Overall), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.Age), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.Status), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.Injury), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(PlayerRowDto), nameof(PlayerRowDto.DepthRole), endpoint, errors, typeof(string));

        FinalizeEndpoint(result, endpoint, errors);
    }

    private static void CompareDepthChart(ContractComparisonResult result, Godot.Collections.Dictionary payload, TeamDepthChartResponse response)
    {
        const string endpoint = "team_depth_chart";
        var errors = new List<string>();

        RequirePythonBool(payload, "ok", endpoint, errors);
        RequirePythonObject(payload, "team", endpoint, errors);
        RequirePythonObject(payload, "depth_chart_status", endpoint, errors);
        var positions = RequirePythonList(payload, "positions", endpoint, errors);
        var positionRow = RequirePythonFirstObject(positions, endpoint, "positions[]", errors);
        if (positionRow != null)
        {
            RequirePythonKey(positionRow, "position", endpoint, "positions[].position", errors);
            RequirePythonKey(positionRow, "required_starters", endpoint, "positions[].required_starters", errors);
            var players = RequirePythonList(positionRow, "players", endpoint, errors, "positions[].players");
            RequirePythonRowFields(players, endpoint, "positions[].players[]", errors, "player_id", "name", "overall", "status", "injury", "role");
        }

        RequireCSharpProperty(typeof(TeamDepthChartResponse), nameof(TeamDepthChartResponse.Ok), endpoint, errors, typeof(bool));
        RequireCSharpProperty(typeof(TeamDepthChartResponse), nameof(TeamDepthChartResponse.Team), endpoint, errors, typeof(TeamIdentityDto));
        RequireCSharpProperty(typeof(TeamDepthChartResponse), nameof(TeamDepthChartResponse.DepthChartStatus), endpoint, errors, typeof(DepthChartStatusDto));
        RequireCSharpListProperty(typeof(TeamDepthChartResponse), nameof(TeamDepthChartResponse.Positions), typeof(DepthChartPositionDto), endpoint, errors);

        RequireCSharpIdentityFields(endpoint, errors);
        RequireCSharpProperty(typeof(DepthChartStatusDto), nameof(DepthChartStatusDto.IsValid), endpoint, errors, typeof(bool));
        RequireCSharpListProperty(typeof(DepthChartStatusDto), nameof(DepthChartStatusDto.Issues), typeof(string), endpoint, errors);
        RequireCSharpProperty(typeof(DepthChartPositionDto), nameof(DepthChartPositionDto.Position), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(DepthChartPositionDto), nameof(DepthChartPositionDto.RequiredStarters), endpoint, errors, typeof(int));
        RequireCSharpListProperty(typeof(DepthChartPositionDto), nameof(DepthChartPositionDto.Players), typeof(DepthChartPlayerDto), endpoint, errors);
        RequireCSharpProperty(typeof(DepthChartPlayerDto), nameof(DepthChartPlayerDto.PlayerId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(DepthChartPlayerDto), nameof(DepthChartPlayerDto.Name), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(DepthChartPlayerDto), nameof(DepthChartPlayerDto.Overall), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(DepthChartPlayerDto), nameof(DepthChartPlayerDto.Status), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(DepthChartPlayerDto), nameof(DepthChartPlayerDto.Injury), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(DepthChartPlayerDto), nameof(DepthChartPlayerDto.Role), endpoint, errors, typeof(string));

        FinalizeEndpoint(result, endpoint, errors);
    }

    private static void CompareSchedule(ContractComparisonResult result, Godot.Collections.Dictionary payload, TeamScheduleResponse response)
    {
        const string endpoint = "team_schedule";
        var errors = new List<string>();

        RequirePythonBool(payload, "ok", endpoint, errors);
        var schedule = RequirePythonList(payload, "schedule", endpoint, errors);
        RequirePythonRowFields(schedule, endpoint, "schedule[]", errors, "game_id", "week", "game_type", "opponent", "home_away", "status", "home_team", "away_team", "home_score", "away_score", "winner");

        RequireCSharpProperty(typeof(TeamScheduleResponse), nameof(TeamScheduleResponse.Ok), endpoint, errors, typeof(bool));
        RequireCSharpListProperty(typeof(TeamScheduleResponse), nameof(TeamScheduleResponse.Schedule), typeof(ScheduleGameRowDto), endpoint, errors);
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.GameId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.Week), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.GameType), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.Opponent), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.HomeAway), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.Status), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.HomeTeam), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.AwayTeam), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.HomeScore), endpoint, errors, typeof(int?));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.AwayScore), endpoint, errors, typeof(int?));
        RequireCSharpProperty(typeof(ScheduleGameRowDto), nameof(ScheduleGameRowDto.Winner), endpoint, errors, typeof(string));

        FinalizeEndpoint(result, endpoint, errors);
    }

    private static void CompareStandings(ContractComparisonResult result, Godot.Collections.Dictionary payload, StandingsResponse response)
    {
        const string endpoint = "standings";
        var errors = new List<string>();

        RequirePythonBool(payload, "ok", endpoint, errors);
        var standings = RequirePythonList(payload, "standings", endpoint, errors);
        RequirePythonRowFields(standings, endpoint, "standings[]", errors, "team_id", "team_name", "abbreviation", "wins", "losses", "ties", "win_pct", "points_for", "points_against");

        RequireCSharpProperty(typeof(StandingsResponse), nameof(StandingsResponse.Ok), endpoint, errors, typeof(bool));
        RequireCSharpListProperty(typeof(StandingsResponse), nameof(StandingsResponse.Standings), typeof(StandingRowDto), endpoint, errors);
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.TeamId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.TeamName), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.Abbreviation), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.Wins), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.Losses), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.Ties), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.WinPct), endpoint, errors, typeof(double));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.PointsFor), endpoint, errors, typeof(int));
        RequireCSharpProperty(typeof(StandingRowDto), nameof(StandingRowDto.PointsAgainst), endpoint, errors, typeof(int));

        FinalizeEndpoint(result, endpoint, errors);
    }

    private static void FinalizeEndpoint(ContractComparisonResult result, string endpoint, List<string> errors)
    {
        if (errors.Count == 0)
        {
            result.Steps.Add($"PASS {endpoint} shape");
            return;
        }

        foreach (var error in errors)
            result.Errors.Add($"FAIL {endpoint}: {error}");
    }

    private static void RequireCSharpIdentityFields(string endpoint, List<string> errors)
    {
        RequireCSharpProperty(typeof(TeamIdentityDto), nameof(TeamIdentityDto.TeamId), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(TeamIdentityDto), nameof(TeamIdentityDto.Name), endpoint, errors, typeof(string));
        RequireCSharpProperty(typeof(TeamIdentityDto), nameof(TeamIdentityDto.Abbreviation), endpoint, errors, typeof(string));
    }

    private static void RequireCSharpProperty(Type type, string propertyName, string endpoint, List<string> errors, Type expectedType)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            errors.Add($"C# DTO missing {type.Name}.{propertyName}");
            return;
        }

        if (expectedType != null && property.PropertyType != expectedType)
            errors.Add($"C# DTO has {type.Name}.{propertyName} as {property.PropertyType.Name}, expected {expectedType.Name}");
    }

    private static void RequireCSharpListProperty(Type type, string propertyName, Type expectedElementType, string endpoint, List<string> errors)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            errors.Add($"C# DTO missing {type.Name}.{propertyName}");
            return;
        }

        if (!typeof(IEnumerable).IsAssignableFrom(property.PropertyType) || !property.PropertyType.IsGenericType)
        {
            errors.Add($"C# DTO has {type.Name}.{propertyName} but it is not a generic list");
            return;
        }

        var elementType = property.PropertyType.GetGenericArguments()[0];
        if (elementType != expectedElementType)
            errors.Add($"C# DTO has {type.Name}.{propertyName} items as {elementType.Name}, expected {expectedElementType.Name}");
    }

    private static void RequirePythonBool(Godot.Collections.Dictionary dict, string key, string endpoint, List<string> errors)
    {
        if (!dict.ContainsKey(key))
        {
            errors.Add($"missing {key}");
            return;
        }

        var value = (Variant)dict[key];
        if (value.VariantType != Variant.Type.Bool)
            errors.Add($"{key} is not bool");
    }

    private static Godot.Collections.Dictionary RequirePythonObject(Godot.Collections.Dictionary dict, string key, string endpoint, List<string> errors, string pathOverride = null)
    {
        var path = pathOverride ?? key;
        if (!dict.ContainsKey(key))
        {
            errors.Add($"missing {path}");
            return null;
        }

        var value = (Variant)dict[key];
        if (value.VariantType != Variant.Type.Dictionary)
        {
            errors.Add($"{path} is not object");
            return null;
        }

        return value.AsGodotDictionary();
    }

    private static Godot.Collections.Array RequirePythonList(Godot.Collections.Dictionary dict, string key, string endpoint, List<string> errors, string pathOverride = null)
    {
        var path = pathOverride ?? key;
        if (!dict.ContainsKey(key))
        {
            errors.Add($"missing {path}");
            return null;
        }

        var value = (Variant)dict[key];
        if (value.VariantType != Variant.Type.Array)
        {
            errors.Add($"{path} is not list");
            return null;
        }

        return value.AsGodotArray();
    }

    private static Godot.Collections.Dictionary RequirePythonFirstObject(Godot.Collections.Array array, string endpoint, string path, List<string> errors)
    {
        if (array == null || array.Count == 0)
            return null;

        var value = (Variant)array[0];
        if (value.VariantType != Variant.Type.Dictionary)
        {
            errors.Add($"{path} item is not object");
            return null;
        }

        return value.AsGodotDictionary();
    }

    private static void RequirePythonRowFields(Godot.Collections.Array array, string endpoint, string path, List<string> errors, params string[] requiredFields)
    {
        if (array == null || array.Count == 0)
            return;

        var value = (Variant)array[0];
        if (value.VariantType != Variant.Type.Dictionary)
        {
            errors.Add($"{path} item is not object");
            return;
        }

        var row = value.AsGodotDictionary();
        foreach (var field in requiredFields)
            RequirePythonKey(row, field, endpoint, $"{path}.{field}", errors);
    }

    private static void RequirePythonKey(Godot.Collections.Dictionary dict, string key, string endpoint, string path, List<string> errors)
    {
        if (!dict.ContainsKey(key))
            errors.Add($"missing {path}");
    }
}
