using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class ContinueService
{
    private const int DefaultSimUntilIterationLimit = 256;
    private readonly GameCoreContext _context;
    private readonly RosterService _rosterService;
    private readonly DepthChartService _depthChartService;
    private readonly ScheduleService _scheduleService;
    private readonly GameDayService _gameDayService;
    private readonly PlayoffService _playoffService;
    private readonly SeasonHistoryService _seasonHistoryService;
    private readonly RetirementService _retirementService;

    public ContinueService(GameCoreContext context)
    {
        _context = context;
        _rosterService = new RosterService(context);
        _depthChartService = new DepthChartService(context);
        _scheduleService = new ScheduleService(context);
        _gameDayService = new GameDayService(context);
        _playoffService = new PlayoffService(context);
        _seasonHistoryService = new SeasonHistoryService(context);
        _retirementService = new RetirementService();
    }

    public ContinueResponse Continue(int maxDays = 1)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new ContinueResponse
            {
                Ok = false,
                Error = "No active league loaded.",
                Result = new ContinueResultDto { StopReason = "no_active_league" },
            };
        }

        if (league.ActiveLiveGameSession?.Active == true)
            return BuildStop(league, false, "game_day", 0, 0, 0, new ContinueEvent { Type = "game_day", Description = "Resume the live game before advancing the calendar." });
        var pendingWaiver = (league.Waivers ?? new System.Collections.Generic.List<WaiverClaimState>())
            .FirstOrDefault(waiver => waiver?.PendingConfirmation == true && string.Equals(waiver.PendingClaimTeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (pendingWaiver?.Player != null)
            return BuildStop(league, false, "waiver_claim_confirmation", 0, 0, 0, new ContinueEvent { Type = "waiver_claim_confirmation", Description = $"Finalize or cancel the winning waiver opportunity for {pendingWaiver.Player.Name}." });

        if (maxDays <= 0)
            maxDays = 1;

        _scheduleService.RefreshStatuses(league);
        if (string.Equals(league.Calendar?.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
            return ContinuePlayoffsOneRound();
        if (string.Equals(league.Calendar?.Phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase))
        {
            MoveLeagueToOffseasonPending(league);
            return BuildStop(
                league,
                true,
                "offseason_pending",
                1,
                Math.Max(0, league.Calendar.AbsoluteWeek - (LeagueBootstrapService.TotalSeasonWeeks + 2)),
                0,
                new ContinueEvent
                {
                    Type = ScheduleService.OffseasonPendingPhaseKey,
                    Description = BuildOffseasonDescription(league, ScheduleService.OffseasonPendingPhase),
                });
        }
        if (ScheduleService.IsOffseasonPlaceholderPhase(league.Calendar?.Phase))
            return ContinueOffseasonPhase(league);

        var roster = _rosterService.GetTeamRoster();
        if (!roster.Ok)
            return BuildFailure("no_active_league");
        if (!roster.RosterStatus.IsValid)
            return BuildStop(league, false, "roster_invalid", 0, 0, 0, new ContinueEvent { Type = "roster_invalid", Description = "Roster must be fixed before continuing." });

        var depthChart = _depthChartService.GetTeamDepthChart();
        if (!depthChart.Ok)
            return BuildFailure("no_active_league");
        if (!depthChart.DepthChartStatus.IsValid)
            return BuildStop(league, false, "depth_chart_invalid", 0, 0, 0, new ContinueEvent { Type = "depth_chart_invalid", Description = string.Join(" ", depthChart.DepthChartStatus.Issues) });

        if (_gameDayService.GetCurrentUserGame() != null)
            return BuildStop(league, false, "game_day", 0, 0, 0, new ContinueEvent { Type = "game_day", Description = "User team has a game today." });

        var events = new List<ContinueEvent>();
        var daysAdvanced = 0;
        var gamesSimulated = 0;
        var startAbsoluteWeek = league.Calendar.AbsoluteWeek;

        while (daysAdvanced < maxDays)
        {
            var simulation = SimulateDueGames(league, stopForUserGame: true);
            if (!simulation.Ok)
                return BuildFailure("simulation_failed", simulation.ErrorMessage);

            events.AddRange(simulation.Events);
            gamesSimulated += simulation.GamesSimulated;
            if (simulation.StopForUserGame)
            {
                events.Add(new ContinueEvent
                {
                    Type = "game_day",
                    Description = "User team reached game day.",
                });
                return BuildStop(
                    league,
                    daysAdvanced > 0 || gamesSimulated > 0,
                    "game_day",
                    daysAdvanced,
                    Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek),
                    gamesSimulated,
                    events.ToArray());
            }

            var priorAbsoluteWeek = league.Calendar.AbsoluteWeek;
            var priorPhase = league.Calendar.Phase ?? "";
            AdvanceOneDay(league.Calendar);
            PlayerStatisticsService.RecoverOneDay(league);
            PlayerInjuryService.RecoverOneDay(league);
            daysAdvanced++;
            events.Add(new ContinueEvent
            {
                Type = "day_advanced",
                Description = $"Advanced to {league.Calendar.WeekLabel}, day {league.Calendar.DayIndex}.",
            });

            _scheduleService.RefreshStatuses(league);
            var expiredWaivers = new TransactionService(_context).ExpireWaivers();
            if (expiredWaivers > 0)
            {
                events.Add(new ContinueEvent
                {
                    Type = "waivers_resolved",
                    Description = $"{expiredWaivers} waiver case(s) resolved through a claim or free agency.",
                });
            }
            var resolvedUserWaiver = (league.Waivers ?? new System.Collections.Generic.List<WaiverClaimState>())
                .FirstOrDefault(waiver => waiver?.PendingConfirmation == true && string.Equals(waiver.PendingClaimTeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
            if (resolvedUserWaiver?.Player != null)
            {
                events.Add(new ContinueEvent { Type = "waiver_claim_confirmation", Description = $"The winning waiver opportunity for {resolvedUserWaiver.Player.Name} requires a decision." });
                return BuildStop(league, true, "waiver_claim_confirmation", daysAdvanced, Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek), gamesSimulated, events.ToArray());
            }

            if (league.Calendar.AbsoluteWeek != priorAbsoluteWeek)
            {
                // Resolve the college week that closed, not the newly opened pro week.
                new CollegeUniverseService(_context).AdvanceToProWeek(priorAbsoluteWeek);
                var stopReason = string.Equals(league.Calendar.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase)
                    ? "postseason_pending"
                    : string.Equals(league.Calendar.Phase, priorPhase, StringComparison.OrdinalIgnoreCase)
                        ? "week_advanced"
                        : "season_phase_changed";
                events.Add(new ContinueEvent
                {
                    Type = stopReason,
                    Description = string.Equals(stopReason, "postseason_pending", StringComparison.OrdinalIgnoreCase)
                        ? "Reached postseason pending."
                        : string.Equals(stopReason, "season_phase_changed", StringComparison.OrdinalIgnoreCase)
                        ? $"Season phase is now {league.Calendar.Phase}."
                        : $"Advanced to {league.Calendar.WeekLabel}.",
                });
                return BuildStop(
                    league,
                    true,
                    stopReason,
                    daysAdvanced,
                    Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek),
                    gamesSimulated,
                    events.ToArray());
            }

            simulation = SimulateDueGames(league, stopForUserGame: true);
            if (!simulation.Ok)
                return BuildFailure("simulation_failed", simulation.ErrorMessage);

            events.AddRange(simulation.Events);
            gamesSimulated += simulation.GamesSimulated;
            if (simulation.StopForUserGame)
            {
                events.Add(new ContinueEvent
                {
                    Type = "game_day",
                    Description = "User team reached game day.",
                });
                return BuildStop(
                    league,
                    true,
                    "game_day",
                    daysAdvanced,
                    Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek),
                    gamesSimulated,
                    events.ToArray());
            }

        }

        events.Add(new ContinueEvent
        {
            Type = "max_days_reached",
            Description = $"Reached max_days limit of {maxDays}.",
        });
        return BuildStop(
            league,
            daysAdvanced > 0 || gamesSimulated > 0,
            "max_days_reached",
            daysAdvanced,
            Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek),
            gamesSimulated,
            events.ToArray());
    }

    public ContinueResponse ContinueUntil(string targetType, int targetWeek = 0, int maxDaysPerStep = 14, int maxIterations = DefaultSimUntilIterationLimit)
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return BuildFailure("no_active_league");

        if (maxIterations <= 0)
            maxIterations = DefaultSimUntilIterationLimit;
        if (maxDaysPerStep <= 0)
            maxDaysPerStep = 1;

        _scheduleService.RefreshStatuses(league);

        var target = ResolveSimUntilTarget(targetType, targetWeek);
        if (!target.Ok)
            return BuildFailure(target.Error, target.Description);

        var events = new List<ContinueEvent>();
        var totalDaysAdvanced = 0;
        var totalWeeksAdvanced = 0;
        var totalGamesSimulated = 0;
        var advanced = false;
        var startAbsoluteWeek = league.Calendar.AbsoluteWeek;

        if (HasReachedSimUntilTarget(league, target))
        {
            events.Add(new ContinueEvent
            {
                Type = target.ReachedStopReason,
                Description = $"{target.Label} reached.",
            });
            return BuildStop(league, false, target.ReachedStopReason, 0, 0, 0, events.ToArray());
        }

        for (var iteration = 0; iteration < maxIterations; iteration++)
        {
            var stepStartWeek = league.Calendar.AbsoluteWeek;
            var response = Continue(maxDaysPerStep);
            if (response == null)
                return BuildFailure("simulation_failed", "Continue returned no response.");
            if (!response.Ok)
                return response;

            var step = response.Result ?? new ContinueResultDto();
            totalDaysAdvanced += Math.Max(0, step.DaysAdvanced);
            totalGamesSimulated += Math.Max(0, step.GamesSimulated);
            totalWeeksAdvanced += Math.Max(0, league.Calendar.AbsoluteWeek - stepStartWeek);
            advanced |= step.Advanced || step.GamesSimulated > 0;
            events.AddRange(ToContinueEvents(step.EventsProcessed));

            var autoSimulatedUserGame = false;
            if (string.Equals(step.StopReason, "game_day", StringComparison.OrdinalIgnoreCase))
            {
                var userGame = _gameDayService.GetCurrentUserGame();
                if (userGame == null)
                    return BuildStop(league, advanced, "required_user_action", totalDaysAdvanced, totalWeeksAdvanced, totalGamesSimulated, events.ToArray());

                var userGameResult = _gameDayService.SimulateCurrentUserGame(userGame.GameId);
                if (userGameResult == null || !userGameResult.Ok)
                {
                    var error = string.IsNullOrWhiteSpace(userGameResult?.Error)
                        ? "Unable to simulate current user game."
                        : userGameResult.Error;
                    events.Add(new ContinueEvent
                    {
                        Type = "simulation_failed",
                        Description = error,
                    });
                    return BuildFailure("simulation_failed", error);
                }

                totalGamesSimulated++;
                advanced = true;
                autoSimulatedUserGame = true;
                events.Add(new ContinueEvent
                {
                    Type = "game_simulated",
                    Description = $"{userGameResult.Result.HomeTeam} vs {userGameResult.Result.AwayTeam} finished {userGameResult.Result.HomeScore}-{userGameResult.Result.AwayScore}.",
                });
                _scheduleService.RefreshStatuses(league);
            }

            if (HasReachedSimUntilTarget(league, target))
            {
                if (string.Equals(step.StopReason, PlayoffService.LeagueChampionshipCompletedStopReason, StringComparison.OrdinalIgnoreCase))
                {
                    return BuildStop(
                        league,
                        advanced,
                        step.StopReason,
                        totalDaysAdvanced,
                        Math.Max(totalWeeksAdvanced, Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek)),
                        totalGamesSimulated,
                        events.ToArray());
                }

                events.Add(new ContinueEvent
                {
                    Type = target.ReachedStopReason,
                    Description = $"{target.Label} reached.",
                });
                return BuildStop(
                    league,
                    advanced,
                    target.ReachedStopReason,
                    totalDaysAdvanced,
                    Math.Max(totalWeeksAdvanced, Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek)),
                    totalGamesSimulated,
                    events.ToArray());
            }

            if (string.Equals(target.Type, "offseason_start", StringComparison.OrdinalIgnoreCase)
                && string.Equals(league.Calendar?.Phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase)
                && string.Equals(step.StopReason, PlayoffService.LeagueChampionshipCompletedStopReason, StringComparison.OrdinalIgnoreCase))
                continue;

            if (autoSimulatedUserGame)
                continue;

            var resolvedStopReason = ResolveSimUntilStopReason(step.StopReason, target, league);
            var stopSeverity = ClassifyStopReason(resolvedStopReason);
            if (stopSeverity == ContinueStopSeverity.HardStop || stopSeverity == ContinueStopSeverity.Target)
            {
                if (!string.IsNullOrWhiteSpace(resolvedStopReason) && !string.Equals(resolvedStopReason, step.StopReason, StringComparison.OrdinalIgnoreCase))
                {
                    events.Add(new ContinueEvent
                    {
                        Type = resolvedStopReason,
                        Description = BuildStopDescription(resolvedStopReason, target),
                    });
                }

                return BuildStop(
                    league,
                    advanced,
                    resolvedStopReason,
                    totalDaysAdvanced,
                    Math.Max(totalWeeksAdvanced, Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek)),
                    totalGamesSimulated,
                    events.ToArray());
            }

            if (!step.Advanced && !string.Equals(step.StopReason, "game_day", StringComparison.OrdinalIgnoreCase))
            {
                return BuildStop(
                    league,
                    advanced,
                    string.IsNullOrWhiteSpace(step.StopReason) ? "required_user_action" : step.StopReason,
                    totalDaysAdvanced,
                    Math.Max(totalWeeksAdvanced, Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek)),
                    totalGamesSimulated,
                    events.ToArray());
            }
        }

        events.Add(new ContinueEvent
        {
            Type = "max_iterations_reached",
            Description = $"Reached sim-until iteration limit of {maxIterations}.",
        });
        return BuildStop(
            league,
            advanced,
            "max_iterations_reached",
            totalDaysAdvanced,
            Math.Max(totalWeeksAdvanced, Math.Max(0, league.Calendar.AbsoluteWeek - startAbsoluteWeek)),
            totalGamesSimulated,
            events.ToArray());
    }

    private ContinueResponse BuildFailure(string stopReason, string error = "No active league loaded.")
    {
        return new ContinueResponse
        {
            Ok = false,
            Error = error,
            Result = new ContinueResultDto
            {
                StopReason = stopReason,
            },
        };
    }

    private ContinueResponse BuildStop(
        LeagueState league,
        bool advanced,
        string stopReason,
        int daysAdvanced,
        int weeksAdvanced,
        int gamesSimulated,
        params ContinueEvent[] eventsProcessed)
    {
        league.LastContinueResult = new ContinueResult
        {
            Advanced = advanced,
            StopReason = stopReason,
            DaysAdvanced = daysAdvanced,
            WeeksAdvanced = weeksAdvanced,
            GamesSimulated = gamesSimulated,
            FinalAbsoluteWeek = league.Calendar?.AbsoluteWeek ?? 0,
            FinalWeekLabel = league.Calendar?.WeekLabel ?? "",
            FinalPhase = league.Calendar?.Phase ?? "",
            EventsProcessed = new List<ContinueEvent>(eventsProcessed),
        };

        if (string.Equals(league.Calendar?.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            if (_playoffService.EnsureBracketGenerated(league, out var playoffReason))
            {
                if (!string.Equals(playoffReason, "Playoff bracket already exists.", StringComparison.Ordinal)
                    && !league.LastContinueResult.EventsProcessed.Any(@event => string.Equals(@event.Type, "playoff_bracket_generated", StringComparison.OrdinalIgnoreCase)))
                {
                    league.LastContinueResult.EventsProcessed.Add(new ContinueEvent
                    {
                        Type = "playoff_bracket_generated",
                        Description = "Generated playoff bracket from final regular-season standings.",
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(playoffReason)
                && !league.LastContinueResult.EventsProcessed.Any(@event => string.Equals(@event.Type, "playoff_bracket_skipped", StringComparison.OrdinalIgnoreCase)))
            {
                league.LastContinueResult.EventsProcessed.Add(new ContinueEvent
                {
                    Type = "playoff_bracket_skipped",
                    Description = playoffReason,
                });
            }
        }

        if (ScheduleService.IsSeasonArchivePhase(league.Calendar?.Phase))
        {
            if (_seasonHistoryService.EnsureSeasonHistorySnapshot(league, out var seasonHistoryReason))
            {
                if (!string.Equals(seasonHistoryReason, "Season history snapshot already exists.", StringComparison.Ordinal)
                    && !league.LastContinueResult.EventsProcessed.Any(@event => string.Equals(@event.Type, "season_history_generated", StringComparison.OrdinalIgnoreCase)))
                {
                    league.LastContinueResult.EventsProcessed.Add(new ContinueEvent
                    {
                        Type = "season_history_generated",
                        Description = "Generated season history snapshot for the completed season.",
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(seasonHistoryReason)
                && !league.LastContinueResult.EventsProcessed.Any(@event => string.Equals(@event.Type, "season_history_skipped", StringComparison.OrdinalIgnoreCase)))
            {
                league.LastContinueResult.EventsProcessed.Add(new ContinueEvent
                {
                    Type = "season_history_skipped",
                    Description = seasonHistoryReason,
                });
            }
        }

        return new ContinueResponse
        {
            Ok = true,
            Result = new ContinueResultDto
            {
                Advanced = advanced,
                StopReason = stopReason,
                DaysAdvanced = daysAdvanced,
                WeeksAdvanced = weeksAdvanced,
                GamesSimulated = gamesSimulated,
                FinalAbsoluteWeek = league.LastContinueResult.FinalAbsoluteWeek,
                FinalWeekLabel = league.LastContinueResult.FinalWeekLabel,
                FinalPhase = league.LastContinueResult.FinalPhase,
                EventsProcessed = new List<ContinueEventDto>(league.LastContinueResult.EventsProcessed.ConvertAll(@event => new ContinueEventDto
                {
                    Type = @event.Type,
                    Description = @event.Description,
                })),
            },
        };
    }

    private static List<ContinueEvent> ToContinueEvents(IEnumerable<ContinueEventDto> eventsProcessed)
    {
        if (eventsProcessed == null)
            return new List<ContinueEvent>();

        return eventsProcessed
            .Where(@event => @event != null)
            .Select(@event => new ContinueEvent
            {
                Type = @event.Type ?? "",
                Description = @event.Description ?? "",
            })
            .ToList();
    }

    private static ContinueStopSeverity ClassifyStopReason(string stopReason)
    {
        return NormalizeStopReason(stopReason) switch
        {
            "" => ContinueStopSeverity.Informational,
            "week_advanced" => ContinueStopSeverity.Informational,
            "season_phase_changed" => ContinueStopSeverity.Informational,
            "no_games_this_week" => ContinueStopSeverity.Informational,
            "max_days_reached" => ContinueStopSeverity.Informational,
            "reached_requested_week" => ContinueStopSeverity.Target,
            "reached_regular_season" => ContinueStopSeverity.Target,
            "reached_playoffs" => ContinueStopSeverity.Target,
            "reached_offseason" => ContinueStopSeverity.Target,
            "reached_free_agency" => ContinueStopSeverity.Target,
            "reached_draft" => ContinueStopSeverity.Target,
            "reached_training_camp" => ContinueStopSeverity.Target,
            "postseason_pending" => ContinueStopSeverity.Target,
            "offseason_pending" => ContinueStopSeverity.HardStop,
            "staff_carousel_pending" => ContinueStopSeverity.HardStop,
            "retirement_pending" => ContinueStopSeverity.HardStop,
            "exclusive_negotiation_pending" => ContinueStopSeverity.HardStop,
            "franchise_tag_pending" => ContinueStopSeverity.HardStop,
            "league_year_pending" => ContinueStopSeverity.HardStop,
            "free_agency_pending" => ContinueStopSeverity.HardStop,
            "draft_prep_pending" => ContinueStopSeverity.HardStop,
            "draft_pending" => ContinueStopSeverity.HardStop,
            "rookie_signing_pending" => ContinueStopSeverity.HardStop,
            "training_camp_pending" => ContinueStopSeverity.HardStop,
            "divisional_round_pending" => ContinueStopSeverity.HardStop,
            "conference_championship_pending" => ContinueStopSeverity.HardStop,
            "league_championship_pending" => ContinueStopSeverity.HardStop,
            "season_complete" => ContinueStopSeverity.HardStop,
            _ => ContinueStopSeverity.HardStop,
        };
    }

    private static string ResolveSimUntilStopReason(string stopReason, SimUntilTarget target, LeagueState league)
    {
        if (league?.Calendar == null)
            return NormalizeStopReason(stopReason);

        var normalized = NormalizeStopReason(stopReason);
        var phase = league.Calendar.Phase ?? "";
        if (string.Equals(phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(target.Type, "playoffs_start", StringComparison.OrdinalIgnoreCase))
                return "reached_playoffs";
            if (target.RequiresPostseasonSupport)
                return "week_advanced";
        }

        if (ScheduleService.IsOffseasonPlaceholderPhase(phase))
        {
            if (string.Equals(target.Type, "offseason_start", StringComparison.OrdinalIgnoreCase))
                return "reached_offseason";

            if (ScheduleService.TryGetOffseasonTargetPhase(target.Type, out var targetPhase))
            {
                if (string.Equals(phase, targetPhase, StringComparison.OrdinalIgnoreCase))
                    return target.ReachedStopReason;
                return "week_advanced";
            }
        }

        return normalized;
    }

    private static string BuildStopDescription(string stopReason, SimUntilTarget target)
    {
        return NormalizeStopReason(stopReason) switch
        {
            "postseason_pending" => "Reached postseason pending before native playoff simulation could continue.",
            "divisional_round_pending" => "Wild Card round completed. Divisional Round is pending.",
            "conference_championship_pending" => "Divisional Round completed. Conference Championship is pending.",
            "league_championship_pending" => "Conference Championship completed. League Championship is pending.",
            "season_complete" => "Season complete. Continue to begin the offseason.",
            "offseason_pending" => "Offseason Pending reached.",
            "staff_carousel_pending" => "Staff Carousel Pending reached.",
            "retirement_pending" => "Retirement Pending reached.",
            "exclusive_negotiation_pending" => "Exclusive Negotiation Pending reached.",
            "franchise_tag_pending" => "Franchise Tag Pending reached.",
            "league_year_pending" => "League Year Pending reached.",
            "free_agency_pending" => "Free Agency Pending reached.",
            "draft_prep_pending" => "Draft Prep Pending reached.",
            "draft_pending" => "Draft Pending reached.",
            "rookie_signing_pending" => "Rookie Signing Pending reached.",
            "training_camp_pending" => "Training Camp Pending reached.",
            "reached_playoffs" => "Playoffs reached.",
            "reached_offseason" => "Offseason Pending reached.",
            "reached_free_agency" => "Free Agency Pending reached.",
            "reached_draft" => "Draft Pending reached.",
            "reached_training_camp" => "Training Camp Pending reached.",
            "reached_requested_week" => $"{target.Label} reached.",
            _ => stopReason.Replace('_', ' '),
        };
    }

    private static string NormalizeStopReason(string stopReason)
        => string.IsNullOrWhiteSpace(stopReason) ? "" : stopReason.Trim().ToLowerInvariant();

    private static bool HasReachedSimUntilTarget(LeagueState league, SimUntilTarget target)
    {
        if (league?.Calendar == null || target == null)
            return false;

        ScheduleService.NormalizeCalendar(league.Calendar);
        return string.Equals(target.Type, "regular_season_week", StringComparison.OrdinalIgnoreCase)
            ? league.Calendar.AbsoluteWeek >= target.TargetAbsoluteWeek
            : league.Calendar.AbsoluteWeek >= target.TargetAbsoluteWeek;
    }

    private SimUntilTarget ResolveSimUntilTarget(string targetType, int targetWeek)
    {
        var normalizedTargetType = NormalizeStopReason(targetType);
        if (normalizedTargetType == "regular_season_week")
        {
            if (targetWeek < 1 || targetWeek > LeagueBootstrapService.RegularSeasonWeeks)
            {
                return new SimUntilTarget
                {
                    Ok = false,
                    Error = "invalid_target_week",
                    Description = "Regular season week target is invalid.",
                };
            }

            return new SimUntilTarget
            {
                Ok = true,
                Type = normalizedTargetType,
                TargetAbsoluteWeek = LeagueBootstrapService.RegularSeasonStartWeek + targetWeek - 1,
                Label = $"Regular Season Week {targetWeek}",
                ReachedStopReason = "reached_requested_week",
            };
        }

        if (normalizedTargetType == "playoffs_start")
        {
            return new SimUntilTarget
            {
                Ok = true,
                Type = normalizedTargetType,
                TargetAbsoluteWeek = LeagueBootstrapService.TotalSeasonWeeks + 1,
                Label = "Playoffs",
                ReachedStopReason = "reached_playoffs",
                RequiresPostseasonSupport = true,
            };
        }

        if (normalizedTargetType == "offseason_start")
        {
            return new SimUntilTarget
            {
                Ok = true,
                Type = normalizedTargetType,
                TargetAbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.OffseasonPendingPhase),
                Label = ScheduleService.OffseasonPendingPhase,
                ReachedStopReason = "reached_offseason",
                RequiresPostseasonSupport = true,
            };
        }

        if (normalizedTargetType == "free_agency")
        {
            return new SimUntilTarget
            {
                Ok = true,
                Type = normalizedTargetType,
                TargetAbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.FreeAgencyPendingPhase),
                Label = ScheduleService.FreeAgencyPendingPhase,
                ReachedStopReason = "reached_free_agency",
                RequiresPostseasonSupport = true,
            };
        }

        if (normalizedTargetType == "draft")
        {
            return new SimUntilTarget
            {
                Ok = true,
                Type = normalizedTargetType,
                TargetAbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.DraftPendingPhase),
                Label = ScheduleService.DraftPendingPhase,
                ReachedStopReason = "reached_draft",
                RequiresPostseasonSupport = true,
            };
        }

        if (normalizedTargetType == "training_camp")
        {
            return new SimUntilTarget
            {
                Ok = true,
                Type = normalizedTargetType,
                TargetAbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.TrainingCampPendingPhase),
                Label = ScheduleService.TrainingCampPendingPhase,
                ReachedStopReason = "reached_training_camp",
                RequiresPostseasonSupport = true,
            };
        }

        return new SimUntilTarget
        {
            Ok = false,
            Error = "invalid_target_type",
            Description = "Sim Until target type is invalid.",
        };
    }

    private static void AdvanceOneDay(CalendarState calendar)
    {
        ScheduleService.NormalizeCalendar(calendar);
        calendar.DayIndex++;
        if (calendar.DayIndex > 6)
        {
            calendar.DayIndex = 0;
            calendar.AbsoluteWeek++;
            calendar.Week = calendar.AbsoluteWeek;
        }

        if (DateTime.TryParse(calendar.CurrentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            calendar.CurrentDate = date.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        ScheduleService.NormalizeCalendar(calendar);
    }

    private static void MoveLeagueToOffseasonPending(LeagueState league)
    {
        MoveLeagueToOffseasonPhase(league, ScheduleService.OffseasonPendingPhase);
    }

    private ContinueResponse ContinueOffseasonPhase(LeagueState league)
    {
        ScheduleService.NormalizeCalendar(league.Calendar);
        var currentPhase = league.Calendar?.Phase ?? "";
        var currentPhaseKey = ScheduleService.GetOffseasonPhaseKey(currentPhase);
        if (string.IsNullOrWhiteSpace(currentPhaseKey))
            return BuildFailure("invalid_phase", "Offseason continuation is only available during offseason phases.");

        if (ScheduleService.IsTerminalOffseasonPlaceholderPhase(currentPhase))
        {
            var trainingCampCpuRepairs = new CpuRosterManagementService(_context).RepairCpuStarterShortages();
            var cpuCuts = new RosterConstructionService(_context).ProcessCpuTrainingCampCuts();
            var userRoster = _rosterService.GetTeamRoster();
            if (!userRoster.Ok || !userRoster.RosterStatus.IsValid)
            {
                return BuildStop(
                    league,
                    cpuCuts > 0 || trainingCampCpuRepairs.Signings > 0,
                    "roster_invalid",
                    0,
                    0,
                    0,
                    new ContinueEvent
                    {
                        Type = "roster_invalid",
                        Description = $"Training camp roster must be reduced to {RosterService.RosterLimit} players before starting the next season.",
                    });
            }

            var rollover = new SeasonRolloverService(_context);
            if (rollover.StartNextSeason(out var rolloverMessage))
            {
                return BuildStop(
                    league,
                    true,
                    "new_season_started",
                    0,
                    0,
                    0,
                    new ContinueEvent { Type = "new_season_started", Description = rolloverMessage });
            }

            return BuildStop(
                league,
                cpuCuts > 0 || trainingCampCpuRepairs.Signings > 0,
                currentPhaseKey,
                0,
                0,
                0,
                new ContinueEvent
                {
                    Type = currentPhaseKey,
                    Description = string.IsNullOrWhiteSpace(rolloverMessage) ? BuildOffseasonDescription(league, currentPhase) : rolloverMessage,
                });
        }

        var priorAbsoluteWeek = league.Calendar.AbsoluteWeek;
        var events = new List<ContinueEvent>();
        if (string.Equals(currentPhaseKey, ScheduleService.DraftPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            var draft = new DraftService(_context);
            var completed = draft.AdvanceCpuPicksUntilUserTurn();
            if (!completed)
            {
                return BuildStop(
                    league,
                    false,
                    ScheduleService.DraftPendingPhaseKey,
                    0,
                    0,
                    0,
                    new ContinueEvent { Type = ScheduleService.DraftPendingPhaseKey, Description = string.IsNullOrWhiteSpace(draft.LastMessage) ? "Make your current draft selection before continuing." : draft.LastMessage });
            }

            return BuildStop(
                league,
                true,
                ScheduleService.RookieSigningPendingPhaseKey,
                0,
                Math.Max(0, league.Calendar.AbsoluteWeek - priorAbsoluteWeek),
                0,
                new ContinueEvent { Type = ScheduleService.RookieSigningPendingPhaseKey, Description = "All draft selections are complete. Rookie contracts have been created." });
        }
        if (string.Equals(currentPhaseKey, ScheduleService.FreeAgencyPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            var transitionPhase = ScheduleService.GetNextOffseasonPlaceholderPhase(currentPhase);
            MoveLeagueToOffseasonPhase(league, transitionPhase);
            return BuildStop(
                league,
                true,
                ScheduleService.GetOffseasonPhaseKey(transitionPhase),
                0,
                Math.Max(0, league.Calendar.AbsoluteWeek - priorAbsoluteWeek),
                0,
                new ContinueEvent
                {
                    Type = ScheduleService.GetOffseasonPhaseKey(transitionPhase),
                    Description = "Free agency closed. Draft preparation is now active.",
                });
        }
        if (string.Equals(currentPhaseKey, ScheduleService.FranchiseTagPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            var expiredContracts = new ContractService(_context).ProcessContractExpirations();
            events.Add(new ContinueEvent
            {
                Type = "contracts_processed",
                Description = expiredContracts > 0
                    ? $"{expiredContracts} untagged contracts expired and players entered free agency."
                    : "Franchise-tag decisions closed; no contracts expired.",
            });
        }
        if (string.Equals(currentPhaseKey, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            var retirementResult = _retirementService.GenerateRetirementsForCurrentSeason(league);
            events.Add(new ContinueEvent
            {
                Type = retirementResult.Generated ? "retirements_generated" : "retirements_skipped",
                Description = retirementResult.Generated
                    ? $"{retirementResult.RetiredCount} players retired."
                    : string.IsNullOrWhiteSpace(retirementResult.Reason)
                        ? $"{retirementResult.RetiredCount} players already retired this offseason."
                        : retirementResult.Reason,
            });
        }

        var nextPhase = ScheduleService.GetNextOffseasonPlaceholderPhase(currentPhase);
        MoveLeagueToOffseasonPhase(league, nextPhase);
        var nextPhaseKey = ScheduleService.GetOffseasonPhaseKey(nextPhase);
        var staffChanges = string.Equals(nextPhaseKey, ScheduleService.StaffCarouselPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            ? new StaffService(_context).ProcessOffseasonRetirementsAndCpuReplacements()
            : 0;
        var openingFreeAgency = string.Equals(nextPhaseKey, ScheduleService.FreeAgencyPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
        var openingDraft = string.Equals(nextPhaseKey, ScheduleService.DraftPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
        var cpuRepairs = openingFreeAgency ? new CpuRosterManagementService(_context).RepairCpuStarterShortages() : new CpuRosterRepairResult();
        if (openingDraft)
            new CollegeDraftPipelineService(_context).FinalizeCurrentDraftClass();
        if (openingDraft)
            new DraftService(_context).AdvanceCpuPicksUntilUserTurn();
        events.Add(new ContinueEvent
        {
            Type = openingFreeAgency ? "free_agency_open" : nextPhaseKey,
            Description = openingFreeAgency
                ? $"Free agency is open. CPU teams completed {cpuRepairs.Signings} rule-approved starter repair signing(s); review the market and submit offers before continuing to draft preparation."
                : openingDraft ? "The draft is underway. Make your current selection before continuing." : staffChanges > 0 ? $"Staff Carousel is open. {staffChanges} retirement or CPU replacement change(s) were recorded; review any franchise vacancies." : BuildOffseasonDescription(league, nextPhase),
        });
        return BuildStop(
            league,
            true,
            openingFreeAgency ? "free_agency_open" : nextPhaseKey,
            0,
            Math.Max(0, league.Calendar.AbsoluteWeek - priorAbsoluteWeek),
            0,
            events.ToArray());
    }

    private static void MoveLeagueToOffseasonPhase(LeagueState league, string offseasonPhase)
    {
        if (league?.Calendar == null)
            return;

        var absoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(offseasonPhase);
        if (absoluteWeek <= 0)
            absoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.OffseasonPendingPhase);

        league.Calendar.AbsoluteWeek = absoluteWeek;
        league.Calendar.Week = league.Calendar.AbsoluteWeek;
        league.Calendar.DayIndex = 0;

        if (DateTime.TryParse(league.Calendar.CurrentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            league.Calendar.CurrentDate = date.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        ScheduleService.NormalizeCalendar(league.Calendar);
    }

    private static string BuildOffseasonDescription(LeagueState league, string phase)
    {
        var phaseLabel = ScheduleService.GetOffseasonPhaseLabel(phase);
        if (string.Equals(ScheduleService.GetOffseasonPhaseKey(phaseLabel), ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            var seasonRetirements = RetirementService.GetSeasonRetirementRecord(league, league?.SeasonYear ?? 0);
            if (seasonRetirements?.Completed == true)
                return $"{seasonRetirements.RetiredCount} players retired.";

            return "Retirement decisions pending.";
        }

        return ScheduleService.GetOffseasonPhaseKey(phaseLabel) switch
        {
            ScheduleService.OffseasonPendingPhaseKey => "Process expiring contracts and prepare the offseason market.",
            ScheduleService.StaffCarouselPendingPhaseKey => "Staff changes are open. Review vacancies and the staff market, or continue to retirement processing.",
            ScheduleService.ExclusiveNegotiationPendingPhaseKey => "Contract extensions and releases are available before free agency.",
            ScheduleService.FranchiseTagPendingPhaseKey => "Apply a franchise tag to one eligible final-year player or continue to process expiring contracts.",
            ScheduleService.LeagueYearPendingPhaseKey => "The new league year is ready to open free agency.",
            ScheduleService.FreeAgencyPendingPhaseKey => "Free agency is open. Review the market and submit offers.",
            ScheduleService.DraftPrepPendingPhaseKey => "Draft preparation is active.",
            ScheduleService.DraftPendingPhaseKey => "The draft is ready for selections.",
            ScheduleService.RookieSigningPendingPhaseKey => "Rookie signing is pending.",
            ScheduleService.TrainingCampPendingPhaseKey => "Finalize your 53-player roster to begin the next preseason.",
            _ => phaseLabel,
        };
    }

    public ContinueResponse ContinuePlayoffsOneRound()
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return BuildFailure("no_active_league");

        _scheduleService.RefreshStatuses(league);
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
            return BuildFailure("invalid_phase", "Playoff continuation is only available at postseason pending.");

        var wildCardSimulation = _playoffService.SimulateWildCardRound(league);
        if (!wildCardSimulation.Ok)
            return BuildFailure("simulation_failed", wildCardSimulation.Error);
        if (!wildCardSimulation.AlreadyCompleted)
        {
            return BuildStop(
                league,
                true,
                PlayoffService.WildCardCompletedStopReason,
                0,
                0,
                wildCardSimulation.SimulatedGames,
                new ContinueEvent
                {
                    Type = "wild_card_completed",
                    Description = "Simulated the Wild Card round. Divisional Round is pending.",
                });
        }

        var divisionalSimulation = _playoffService.SimulateDivisionalRound(league);
        if (!divisionalSimulation.Ok)
            return BuildFailure("simulation_failed", divisionalSimulation.Error);
        if (!divisionalSimulation.AlreadyCompleted)
        {
            return BuildStop(
                league,
                true,
                PlayoffService.DivisionalCompletedStopReason,
                0,
                0,
                divisionalSimulation.SimulatedGames,
                new ContinueEvent
                {
                    Type = "divisional_completed",
                    Description = "Simulated the Divisional Round. Conference Championship is pending.",
                });
        }

        var conferenceSimulation = _playoffService.SimulateConferenceChampionshipRound(league);
        if (!conferenceSimulation.Ok)
            return BuildFailure("simulation_failed", conferenceSimulation.Error);
        if (!conferenceSimulation.AlreadyCompleted)
        {
            return BuildStop(
                league,
                true,
                PlayoffService.ConferenceChampionshipCompletedStopReason,
                0,
                0,
                conferenceSimulation.SimulatedGames,
                new ContinueEvent
                {
                    Type = "conference_championship_completed",
                    Description = "Simulated the Conference Championship. League Championship is pending.",
                });
        }

        var leagueChampionshipSimulation = _playoffService.SimulateLeagueChampionshipRound(league);
        if (!leagueChampionshipSimulation.Ok)
            return BuildFailure("simulation_failed", leagueChampionshipSimulation.Error);

        return BuildStop(
            league,
            !leagueChampionshipSimulation.AlreadyCompleted,
            PlayoffService.LeagueChampionshipCompletedStopReason,
            0,
            0,
            leagueChampionshipSimulation.SimulatedGames,
            new ContinueEvent
            {
                Type = leagueChampionshipSimulation.AlreadyCompleted ? "league_championship_already_completed" : "league_championship_completed",
                Description = leagueChampionshipSimulation.AlreadyCompleted
                    ? "League Championship already completed. Season complete. Continue to begin the offseason."
                    : "Simulated the League Championship. Season complete. Continue to begin the offseason.",
            });
    }

    private DueGameSimulationResult SimulateDueGames(LeagueState league, bool stopForUserGame)
    {
        var events = new List<ContinueEvent>();
        var dueGames = league.Schedule
            .Where(game => game != null
                && !GameCoreStateHelper.IsFinal(game)
                && ScheduleService.IsInCurrentAbsoluteWeek(league.Calendar, game)
                && ScheduleService.IsGameDue(league.Calendar, game))
            .OrderBy(game => game.AbsoluteWeek > 0 ? game.AbsoluteWeek : game.Week)
            .ThenBy(game => game.DayIndex)
            .ThenBy(game => game.GameId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var gamesSimulated = 0;
        foreach (var game in dueGames)
        {
            var isUserGame = GameCoreStateHelper.IsTeamInGame(game, league.UserTeamId);
            if (isUserGame && stopForUserGame)
            {
                _scheduleService.RefreshStatuses(league);
                return new DueGameSimulationResult
                {
                    Ok = true,
                    StopForUserGame = true,
                    GamesSimulated = gamesSimulated,
                    Events = events,
                };
            }

            var result = _gameDayService.SimulateScheduledGame(game.GameId, allowUserTeamGame: !stopForUserGame);
            if (result == null || !result.Ok)
            {
                events.Add(new ContinueEvent
                {
                    Type = "simulation_failed",
                    Description = string.IsNullOrWhiteSpace(result?.Error)
                        ? $"Unable to simulate scheduled game {game.GameId}."
                        : result.Error,
                });
                return new DueGameSimulationResult
                {
                    Ok = false,
                    ErrorMessage = string.IsNullOrWhiteSpace(result?.Error)
                        ? $"Unable to simulate scheduled game {game.GameId}."
                        : result.Error,
                    Events = events,
                };
            }

            gamesSimulated++;
            events.Add(new ContinueEvent
            {
                Type = "game_simulated",
                Description = $"{result.Result.HomeTeam} vs {result.Result.AwayTeam} finished {result.Result.HomeScore}-{result.Result.AwayScore}.",
            });
        }

        _scheduleService.RefreshStatuses(league);
        return new DueGameSimulationResult
        {
            Ok = true,
            GamesSimulated = gamesSimulated,
            Events = events,
        };
    }

    private sealed class DueGameSimulationResult
    {
        public bool Ok { get; set; }
        public bool StopForUserGame { get; set; }
        public int GamesSimulated { get; set; }
        public string ErrorMessage { get; set; } = "";
        public List<ContinueEvent> Events { get; set; } = new();
    }

    private sealed class SimUntilTarget
    {
        public bool Ok { get; set; }
        public string Error { get; set; } = "";
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Label { get; set; } = "";
        public string ReachedStopReason { get; set; } = "";
        public int TargetAbsoluteWeek { get; set; }
        public bool RequiresPostseasonSupport { get; set; }
    }

    private enum ContinueStopSeverity
    {
        Informational,
        Target,
        HardStop,
    }
}
