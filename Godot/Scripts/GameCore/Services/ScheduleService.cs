using System;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class ScheduleService
{
    private readonly GameCoreContext _context;
    public const string PostseasonPendingPhase = "Postseason Pending";
    public const string PostseasonPendingWeekLabel = "Postseason Pending";
    public const string SeasonCompletePhase = "Season Complete";
    public const string SeasonCompleteWeekLabel = "Season Complete";
    public const string OffseasonPendingPhaseKey = "offseason_pending";
    public const string OffseasonPendingPhase = "Offseason Pending";
    public const string OffseasonPendingWeekLabel = "Offseason Pending";
    public const string StaffCarouselPendingPhaseKey = "staff_carousel_pending";
    public const string StaffCarouselPendingPhase = "Staff Carousel Pending";
    public const string StaffCarouselPendingWeekLabel = "Staff Carousel Pending";
    public const string RetirementPendingPhaseKey = "retirement_pending";
    public const string RetirementPendingPhase = "Retirement Pending";
    public const string RetirementPendingWeekLabel = "Retirement Pending";
    public const string ExclusiveNegotiationPendingPhaseKey = "exclusive_negotiation_pending";
    public const string ExclusiveNegotiationPendingPhase = "Exclusive Negotiation Pending";
    public const string ExclusiveNegotiationPendingWeekLabel = "Exclusive Negotiation Pending";
    public const string FranchiseTagPendingPhaseKey = "franchise_tag_pending";
    public const string FranchiseTagPendingPhase = "Franchise Tag Pending";
    public const string FranchiseTagPendingWeekLabel = "Franchise Tag Pending";
    public const string LeagueYearPendingPhaseKey = "league_year_pending";
    public const string LeagueYearPendingPhase = "League Year Pending";
    public const string LeagueYearPendingWeekLabel = "League Year Pending";
    public const string FreeAgencyPendingPhaseKey = "free_agency_pending";
    public const string FreeAgencyPendingPhase = "Free Agency Pending";
    public const string FreeAgencyPendingWeekLabel = "Free Agency Pending";
    public const string DraftPrepPendingPhaseKey = "draft_prep_pending";
    public const string DraftPrepPendingPhase = "Draft Prep Pending";
    public const string DraftPrepPendingWeekLabel = "Draft Prep Pending";
    public const string DraftPendingPhaseKey = "draft_pending";
    public const string DraftPendingPhase = "Draft Pending";
    public const string DraftPendingWeekLabel = "Draft Pending";
    public const string RookieSigningPendingPhaseKey = "rookie_signing_pending";
    public const string RookieSigningPendingPhase = "Rookie Signing Pending";
    public const string RookieSigningPendingWeekLabel = "Rookie Signing Pending";
    public const string TrainingCampPendingPhaseKey = "training_camp_pending";
    public const string TrainingCampPendingPhase = "Training Camp Pending";
    public const string TrainingCampPendingWeekLabel = "Training Camp Pending";

    private static readonly OffseasonPhaseDefinition[] OffseasonPlaceholderPhases =
    {
        new(OffseasonPendingPhaseKey, OffseasonPendingPhase, OffseasonPendingWeekLabel),
        new(StaffCarouselPendingPhaseKey, StaffCarouselPendingPhase, StaffCarouselPendingWeekLabel),
        new(RetirementPendingPhaseKey, RetirementPendingPhase, RetirementPendingWeekLabel),
        new(ExclusiveNegotiationPendingPhaseKey, ExclusiveNegotiationPendingPhase, ExclusiveNegotiationPendingWeekLabel),
        new(FranchiseTagPendingPhaseKey, FranchiseTagPendingPhase, FranchiseTagPendingWeekLabel),
        new(LeagueYearPendingPhaseKey, LeagueYearPendingPhase, LeagueYearPendingWeekLabel),
        new(FreeAgencyPendingPhaseKey, FreeAgencyPendingPhase, FreeAgencyPendingWeekLabel),
        new(DraftPrepPendingPhaseKey, DraftPrepPendingPhase, DraftPrepPendingWeekLabel),
        new(DraftPendingPhaseKey, DraftPendingPhase, DraftPendingWeekLabel),
        new(RookieSigningPendingPhaseKey, RookieSigningPendingPhase, RookieSigningPendingWeekLabel),
        new(TrainingCampPendingPhaseKey, TrainingCampPendingPhase, TrainingCampPendingWeekLabel),
    };

    public ScheduleService(GameCoreContext context)
    {
        _context = context;
    }

    public TeamScheduleResponse GetTeamSchedule(string teamId = null)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new TeamScheduleResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
        {
            return new TeamScheduleResponse
            {
                Ok = false,
                Error = "Team not found.",
            };
        }

        RefreshStatuses(league);

        return new TeamScheduleResponse
        {
            Ok = true,
            Schedule = league.Schedule
                .Where(game => GameCoreStateHelper.IsTeamInGame(game, team.TeamId))
                .OrderBy(game => game.Week)
                .ThenBy(game => game.DayIndex)
                .ThenBy(game => game.GameId, StringComparer.OrdinalIgnoreCase)
                .Select(game =>
                {
                    var opponent = GameCoreStateHelper.ResolveOpponent(league, game, team.TeamId);
                    return new ScheduleGameRowDto
                    {
                        GameId = game.GameId,
                        Week = game.PhaseWeek,
                        AbsoluteWeek = game.AbsoluteWeek,
                        PhaseWeek = game.PhaseWeek,
                        Phase = game.Phase,
                        DisplayWeek = game.PhaseWeek.ToString(),
                        GameType = game.GameType,
                        WeekLabel = game.WeekLabel,
                        Opponent = opponent?.Name ?? "TBD",
                        HomeAway = string.Equals(game.HomeTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) ? "home" : "away",
                        Status = game.Status,
                        HomeTeam = league.Teams.FirstOrDefault(x => string.Equals(x.TeamId, game.HomeTeamId, StringComparison.OrdinalIgnoreCase))?.Abbreviation ?? game.HomeTeamId,
                        AwayTeam = league.Teams.FirstOrDefault(x => string.Equals(x.TeamId, game.AwayTeamId, StringComparison.OrdinalIgnoreCase))?.Abbreviation ?? game.AwayTeamId,
                        HomeScore = game.HomeScore,
                        AwayScore = game.AwayScore,
                        Winner = game.Winner,
                    };
                })
                .ToList(),
        };
    }

    public void RefreshStatuses(LeagueState league)
    {
        if (league == null)
            return;

        league.Calendar ??= new CalendarState();
        NormalizeCalendar(league.Calendar);

        foreach (var game in league.Schedule)
        {
            NormalizeScheduledGame(game);
            if (HasCompletedResult(league, game))
            {
                game.Status = "final";
                continue;
            }

            if (IsGameDue(league.Calendar, game)
                && GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId))
            {
                game.Status = "game_day";
                continue;
            }

            if (game.Week == league.Calendar.Week && game.DayIndex == league.Calendar.DayIndex)
            {
                game.Status = "game_day";
                continue;
            }

            game.Status = "upcoming";
        }
    }

    public static bool IsGameDue(CalendarState calendar, ScheduledGame game)
    {
        if (calendar == null || game == null)
            return false;

        NormalizeCalendar(calendar);
        NormalizeScheduledGame(game);

        return game.AbsoluteWeek < calendar.AbsoluteWeek
               || (game.AbsoluteWeek == calendar.AbsoluteWeek && game.DayIndex <= calendar.DayIndex);
    }

    public static string GetPhaseForWeek(int week)
    {
        if (week <= LeagueBootstrapService.PreseasonWeeks)
            return "Preseason";
        if (week < LeagueBootstrapService.RegularSeasonStartWeek)
            return "Preseason Bye";
        if (week <= LeagueBootstrapService.TotalSeasonWeeks)
            return "Regular Season";
        if (week == LeagueBootstrapService.TotalSeasonWeeks + 1)
            return PostseasonPendingPhase;
        if (week == LeagueBootstrapService.TotalSeasonWeeks + 2)
            return SeasonCompletePhase;
        if (TryGetOffseasonPlaceholderByAbsoluteWeek(week, out var offseasonPhase))
            return offseasonPhase.Label;
        return "Offseason";
    }

    public static int GetPhaseWeek(int week)
    {
        if (week <= LeagueBootstrapService.PreseasonWeeks)
            return Math.Max(1, week);
        if (week < LeagueBootstrapService.RegularSeasonStartWeek)
            return 0;
        if (week <= LeagueBootstrapService.TotalSeasonWeeks)
            return Math.Max(1, week - LeagueBootstrapService.PreseasonWeeks - LeagueBootstrapService.PreseasonByeWeeks);
        if (week == LeagueBootstrapService.TotalSeasonWeeks + 1
            || week == LeagueBootstrapService.TotalSeasonWeeks + 2
            || TryGetOffseasonPlaceholderByAbsoluteWeek(week, out _))
            return 1;
        return Math.Max(1, week - LeagueBootstrapService.TotalSeasonWeeks);
    }

    public static string BuildCalendarWeekLabel(int absoluteWeek)
    {
        var phase = GetPhaseForWeek(absoluteWeek);
        if (string.Equals(phase, "Preseason Bye", StringComparison.OrdinalIgnoreCase))
            return $"Week {absoluteWeek} - Preseason Bye";
        if (string.Equals(phase, PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
            return PostseasonPendingWeekLabel;
        if (string.Equals(phase, SeasonCompletePhase, StringComparison.OrdinalIgnoreCase))
            return SeasonCompleteWeekLabel;
        if (TryGetOffseasonPlaceholderDefinition(phase, out var offseasonPhase))
            return offseasonPhase.WeekLabel;
        return $"Week {GetPhaseWeek(absoluteWeek)} - {phase}";
    }

    public static string BuildGameWeekLabel(string gameType, int absoluteWeek, int phaseWeek = 0)
    {
        var resolvedPhaseWeek = phaseWeek > 0 ? phaseWeek : GetDisplayWeek(gameType, absoluteWeek);
        var phaseName = GetPhaseForGameType(gameType);
        if (string.Equals(NormalizeGameType(gameType), "playoffs", StringComparison.OrdinalIgnoreCase))
            return BuildPlayoffRoundLabel(resolvedPhaseWeek);
        return string.IsNullOrWhiteSpace(phaseName)
            ? $"Week {resolvedPhaseWeek}"
            : $"{phaseName} Week {resolvedPhaseWeek}";
    }

    public static string BuildPlayoffRoundLabel(int phaseWeek)
        => phaseWeek switch
        {
            1 => "Playoffs - Wild Card",
            2 => "Playoffs - Divisional",
            3 => "Playoffs - Conference Championship",
            4 => "League Championship",
            _ => $"Playoffs - Round {Math.Max(1, phaseWeek)}",
        };

    public static int GetDisplayWeek(string gameType, int absoluteWeek)
    {
        if (absoluteWeek <= 0)
            return 1;

        var normalized = NormalizeGameType(gameType);
        return normalized switch
        {
            "preseason" => Math.Max(1, absoluteWeek),
            "regular_season" => Math.Max(1, absoluteWeek - LeagueBootstrapService.PreseasonWeeks - LeagueBootstrapService.PreseasonByeWeeks),
            _ => GetPhaseWeek(absoluteWeek),
        };
    }

    public static void NormalizeCalendar(CalendarState calendar)
    {
        if (calendar == null)
            return;

        var absoluteWeek = calendar.AbsoluteWeek > 0
            ? calendar.AbsoluteWeek
            : calendar.Week > 0 ? calendar.Week : 1;

        calendar.AbsoluteWeek = absoluteWeek;
        calendar.Week = absoluteWeek;
        calendar.Phase = GetPhaseForWeek(absoluteWeek);
        calendar.PhaseWeek = GetPhaseWeek(absoluteWeek);
        calendar.WeekLabel = BuildCalendarWeekLabel(absoluteWeek);
    }

    public static void NormalizeScheduledGame(ScheduledGame game)
    {
        if (game == null)
            return;

        var absoluteWeek = game.AbsoluteWeek > 0
            ? game.AbsoluteWeek
            : game.Week > 0 ? game.Week : 1;

        game.AbsoluteWeek = absoluteWeek;
        game.Week = absoluteWeek;
        game.GameType = NormalizeGameType(game.GameType, absoluteWeek);
        game.Phase = string.IsNullOrWhiteSpace(game.Phase) ? GetPhaseForGameType(game.GameType) : game.Phase;
        game.PhaseWeek = game.PhaseWeek > 0 ? game.PhaseWeek : GetDisplayWeek(game.GameType, absoluteWeek);
        game.WeekLabel = string.IsNullOrWhiteSpace(game.WeekLabel)
            ? BuildGameWeekLabel(game.GameType, absoluteWeek, game.PhaseWeek)
            : game.WeekLabel;
    }

    public static void NormalizeResult(GameResult result)
    {
        if (result == null)
            return;

        var absoluteWeek = result.AbsoluteWeek > 0
            ? result.AbsoluteWeek
            : result.Week > 0 ? result.Week : 1;

        result.AbsoluteWeek = absoluteWeek;
        result.Week = absoluteWeek;
        result.GameType = NormalizeGameType(result.GameType, absoluteWeek);
        result.Phase = string.IsNullOrWhiteSpace(result.Phase) ? GetPhaseForGameType(result.GameType) : result.Phase;
        result.PhaseWeek = result.PhaseWeek > 0 ? result.PhaseWeek : GetDisplayWeek(result.GameType, absoluteWeek);
        result.WeekLabel = string.IsNullOrWhiteSpace(result.WeekLabel)
            ? BuildGameWeekLabel(result.GameType, absoluteWeek, result.PhaseWeek)
            : result.WeekLabel;
    }

    public static string NormalizeGameType(string gameType, int absoluteWeek = 0)
    {
        var normalized = (gameType ?? "").Trim().ToLowerInvariant();
        return normalized switch
        {
            "" => InferGameTypeFromAbsoluteWeek(absoluteWeek),
            "regular" => "regular_season",
            "regular season" => "regular_season",
            "pre" => "preseason",
            "postseason" => "playoffs",
            _ => normalized,
        };
    }

    public static string GetPhaseForGameType(string gameType)
    {
        return NormalizeGameType(gameType) switch
        {
            "preseason" => "Preseason",
            "regular_season" => "Regular Season",
            "playoffs" => "Playoffs",
            "bye" => "Bye Week",
            _ => string.IsNullOrWhiteSpace(gameType) ? "" : gameType,
        };
    }

    public static bool CountsTowardRegularSeasonStandings(GameResult result)
    {
        if (result == null)
            return false;

        NormalizeResult(result);
        return string.Equals(result.GameType, "regular_season", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsInCurrentAbsoluteWeek(CalendarState calendar, ScheduledGame game)
    {
        if (calendar == null || game == null)
            return false;

        NormalizeCalendar(calendar);
        NormalizeScheduledGame(game);
        return game.AbsoluteWeek == calendar.AbsoluteWeek;
    }

    public static string HumanizeGameType(string gameType)
        => GetPhaseForGameType(gameType);

    public static string InferGameTypeFromAbsoluteWeek(int absoluteWeek)
    {
        if (absoluteWeek <= 0)
            return "regular_season";
        if (absoluteWeek <= LeagueBootstrapService.PreseasonWeeks)
            return "preseason";
        if (absoluteWeek < LeagueBootstrapService.RegularSeasonStartWeek)
            return "bye";
        if (absoluteWeek <= LeagueBootstrapService.TotalSeasonWeeks)
            return "regular_season";
        return "playoffs";
    }

    public static bool IsOffseasonPlaceholderPhase(string phase)
        => TryGetOffseasonPlaceholderDefinition(phase, out _);

    public static bool IsTerminalOffseasonPlaceholderPhase(string phase)
        => string.Equals(GetOffseasonPhaseKey(phase), TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsSeasonArchivePhase(string phase)
        => string.Equals(phase, SeasonCompletePhase, StringComparison.OrdinalIgnoreCase)
            || IsOffseasonPlaceholderPhase(phase);

    public static string GetOffseasonPhaseKey(string phase)
        => TryGetOffseasonPlaceholderDefinition(phase, out var definition) ? definition.Key : "";

    public static string GetOffseasonPhaseLabel(string phaseOrKey)
        => TryGetOffseasonPlaceholderDefinition(phaseOrKey, out var definition) ? definition.Label : phaseOrKey ?? "";

    public static string GetNextOffseasonPlaceholderPhase(string phase)
    {
        if (!TryGetOffseasonPlaceholderDefinition(phase, out var definition))
            return "";

        var currentIndex = Array.FindIndex(OffseasonPlaceholderPhases, item =>
            string.Equals(item.Key, definition.Key, StringComparison.OrdinalIgnoreCase));
        if (currentIndex < 0)
            return "";
        if (currentIndex >= OffseasonPlaceholderPhases.Length - 1)
            return definition.Label;

        return OffseasonPlaceholderPhases[currentIndex + 1].Label;
    }

    public static int GetOffseasonPlaceholderAbsoluteWeek(string phaseOrKey)
    {
        if (!TryGetOffseasonPlaceholderDefinition(phaseOrKey, out var definition))
            return 0;

        var index = Array.FindIndex(OffseasonPlaceholderPhases, item =>
            string.Equals(item.Key, definition.Key, StringComparison.OrdinalIgnoreCase));
        return index < 0
            ? 0
            : LeagueBootstrapService.TotalSeasonWeeks + 3 + index;
    }

    public static bool TryGetOffseasonTargetPhase(string targetType, out string phaseLabel)
    {
        phaseLabel = NormalizeOffseasonTargetType(targetType) switch
        {
            "offseason_start" => OffseasonPendingPhase,
            "free_agency" => FreeAgencyPendingPhase,
            "draft" => DraftPendingPhase,
            "training_camp" => TrainingCampPendingPhase,
            _ => "",
        };

        return !string.IsNullOrWhiteSpace(phaseLabel);
    }

    private static bool TryGetOffseasonPlaceholderByAbsoluteWeek(int absoluteWeek, out OffseasonPhaseDefinition definition)
    {
        var index = absoluteWeek - (LeagueBootstrapService.TotalSeasonWeeks + 3);
        if (index >= 0 && index < OffseasonPlaceholderPhases.Length)
        {
            definition = OffseasonPlaceholderPhases[index];
            return true;
        }

        definition = null;
        return false;
    }

    private static bool TryGetOffseasonPlaceholderDefinition(string phaseOrKey, out OffseasonPhaseDefinition definition)
    {
        definition = OffseasonPlaceholderPhases.FirstOrDefault(item =>
            string.Equals(item.Key, phaseOrKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Label, phaseOrKey, StringComparison.OrdinalIgnoreCase));
        return definition != null;
    }

    private static string NormalizeOffseasonTargetType(string targetType)
        => (targetType ?? "").Trim().ToLowerInvariant();

    private static bool HasCompletedResult(LeagueState league, ScheduledGame game)
    {
        if (league == null || game == null)
            return false;

        if (league.Results.Any(result => string.Equals(result.GameId, game.GameId, StringComparison.OrdinalIgnoreCase)))
            return true;

        return game.HomeScore.HasValue
               && game.AwayScore.HasValue
               && (!string.IsNullOrWhiteSpace(game.Winner)
                   || string.Equals(game.Status, "final", StringComparison.OrdinalIgnoreCase));
    }

    public ScheduledGame GetNextUserGame(LeagueState league)
    {
        if (league == null)
            return null;

        RefreshStatuses(league);
        return league.Schedule
            .Where(game => GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId) && !GameCoreStateHelper.IsFinal(game))
            .OrderBy(game => game.AbsoluteWeek > 0 ? game.AbsoluteWeek : game.Week)
            .ThenBy(game => game.DayIndex)
            .ThenBy(game => game.GameId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private sealed class OffseasonPhaseDefinition
    {
        public OffseasonPhaseDefinition(string key, string label, string weekLabel)
        {
            Key = key;
            Label = label;
            WeekLabel = weekLabel;
        }

        public string Key { get; }
        public string Label { get; }
        public string WeekLabel { get; }
    }
}
