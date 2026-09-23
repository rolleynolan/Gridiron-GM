using System;
using System.Linq;
using System.Globalization;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class DashboardService
{
    private readonly GameCoreContext _context;
    private readonly RosterService _rosterService;
    private readonly DepthChartService _depthChartService;
    private readonly ScheduleService _scheduleService;
    private readonly GameDayService _gameDayService;
    private readonly StandingsService _standingsService;
    private readonly PlayoffService _playoffService;
    private readonly SeasonHistoryService _seasonHistoryService;

    public DashboardService(GameCoreContext context)
    {
        _context = context;
        _rosterService = new RosterService(context);
        _depthChartService = new DepthChartService(context);
        _scheduleService = new ScheduleService(context);
        _gameDayService = new GameDayService(context);
        _standingsService = new StandingsService(context);
        _playoffService = new PlayoffService(context);
        _seasonHistoryService = new SeasonHistoryService(context);
    }

    public DashboardStateResponse GetDashboardState()
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new DashboardStateResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        _scheduleService.RefreshStatuses(league);
        var team = GameCoreStateHelper.GetUserTeam(league);
        if (team == null)
        {
            return new DashboardStateResponse
            {
                Ok = false,
                Error = "User team not found.",
            };
        }

        var standings = _standingsService.BuildStandings(league);
        var standing = standings.FirstOrDefault(x => string.Equals(x.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase));
        var nextGame = _scheduleService.GetNextUserGame(league);
        var opponent = GameCoreStateHelper.ResolveOpponent(league, nextGame, team.TeamId);
        var roster = _rosterService.GetTeamRoster(team.TeamId);
        var depthChart = _depthChartService.GetTeamDepthChart(team.TeamId);
        var playoffBracket = _playoffService.GetPlayoffBracketDto(league);
        var playoffSummaryText = PlayoffService.FormatBracketSummary(playoffBracket);
        _seasonHistoryService.EnsureSeasonHistorySnapshot(league, out _);
        var seasonCompletionSummary = BuildSeasonCompletionSummary(league);
        var nextGameDto = BuildNextGameDto(league, team, nextGame, opponent, playoffBracket);

        return new DashboardStateResponse
        {
            Ok = true,
            Dashboard = new DashboardDto
            {
                Team = new TeamSummaryDto
                {
                    Name = team.Name,
                    Abbreviation = team.Abbreviation,
                    Record = GameCoreStateHelper.BuildRecord(standing),
                },
                Calendar = new CalendarSummaryDto
                {
                    Year = league.Calendar.Year,
                    Week = league.Calendar.PhaseWeek,
                    AbsoluteWeek = league.Calendar.AbsoluteWeek,
                    PhaseWeek = league.Calendar.PhaseWeek,
                    Phase = league.Calendar.Phase,
                    CurrentDate = league.Calendar.CurrentDate ?? "",
                    DayOfWeek = ResolveDayOfWeek(league.Calendar.CurrentDate),
                    WeekLabel = league.Calendar.WeekLabel,
                },
                NextGame = nextGameDto,
                TeamStatus = new TeamStatusDto
                {
                    RosterSize = team.Roster.Count,
                    Injuries = team.Roster.Count(x => !string.IsNullOrWhiteSpace(x.Injury)),
                    CapRoom = GameCoreStateHelper.FormatCapRoom(team.CapRoom),
                },
                ActionItems = BuildActionItems(nextGame, opponent, roster, depthChart, playoffBracket),
                PlayoffBracket = playoffBracket,
                PlayoffSummaryText = playoffSummaryText,
                SeasonCompletionSummary = seasonCompletionSummary,
                RecentResults = league.Results
                    .Where(result => string.Equals(result.HomeTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(result.AwayTeamId, team.TeamId, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(result => result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week)
                    .ThenByDescending(result => result.GameId, StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .Select(result => new RecentResultDto
                {
                    GameId = result.GameId,
                    Week = result.PhaseWeek > 0 ? result.PhaseWeek : ScheduleService.GetDisplayWeek(result.GameType, result.Week),
                    AbsoluteWeek = result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week,
                    PhaseWeek = result.PhaseWeek > 0 ? result.PhaseWeek : ScheduleService.GetDisplayWeek(result.GameType, result.Week),
                    Phase = string.IsNullOrWhiteSpace(result.Phase) ? ScheduleService.GetPhaseForGameType(result.GameType) : result.Phase,
                    GameType = result.GameType,
                    WeekLabel = string.IsNullOrWhiteSpace(result.WeekLabel)
                        ? ScheduleService.BuildGameWeekLabel(result.GameType, result.AbsoluteWeek > 0 ? result.AbsoluteWeek : result.Week, result.PhaseWeek)
                        : result.WeekLabel,
                    HomeTeam = result.HomeTeam,
                    AwayTeam = result.AwayTeam,
                    HomeScore = result.HomeScore,
                    AwayScore = result.AwayScore,
                    Winner = result.Winner,
                    Summary = result.Summary,
                }).ToList(),
            },
        };
    }

    public TransactionHistoryResponse GetTransactionHistory(int limit = 100)
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return new TransactionHistoryResponse { Error = "No active league loaded." };

        return new TransactionHistoryResponse
        {
            Ok = true,
            Transactions = (league.Transactions ?? new System.Collections.Generic.List<TransactionRecord>())
                .Where(transaction => transaction != null)
                .OrderByDescending(transaction => transaction.SeasonYear)
                .ThenByDescending(transaction => transaction.TransactionId, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(limit, 1, 500))
                .Select(transaction => new TransactionRecordDto
                {
                    DateLabel = transaction.DateLabel,
                    Phase = transaction.Phase,
                    Type = transaction.Type,
                    TeamName = transaction.TeamName,
                    PlayerName = string.IsNullOrWhiteSpace(transaction.PlayerName) ? transaction.StaffName : transaction.PlayerName,
                    Details = transaction.Details,
                })
                .ToList(),
        };
    }

    public LeagueHistoryResponse GetLeagueHistory()
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            return new LeagueHistoryResponse
            {
                Ok = false,
                Error = "No active league loaded.",
            };
        }

        _seasonHistoryService.EnsureSeasonHistorySnapshot(league, out _);
        var seasons = (league.HistoricalSeasons ?? new System.Collections.Generic.List<SeasonHistoryRecord>())
            .Where(record => record != null)
            .OrderByDescending(record => record.SeasonYear)
            .ThenByDescending(record => record.GeneratedAtLabel, StringComparer.OrdinalIgnoreCase)
            .Select(record => MapLeagueHistorySeason(record, (league.HistoricalDrafts ?? new System.Collections.Generic.List<DraftState>()).FirstOrDefault(draft => draft?.DraftYear == record.SeasonYear)))
            .ToList();

        return new LeagueHistoryResponse
        {
            Ok = true,
            Seasons = seasons,
        };
    }

    public RecordBookResponse GetRecordBook()
        => new RecordBookService(_context).GetRecordBook();

    public HistoricalArchiveResponse GetHistoricalArchive()
    {
        var league = _context?.ActiveLeague;
        if (league == null) return new HistoricalArchiveResponse { Error = "Historical archive is unavailable." };
        return new HistoricalArchiveResponse
        {
            Ok = true,
            RecordBook = GetRecordBook(),
            Championships = (league.HistoricalSeasons ?? new System.Collections.Generic.List<SeasonHistoryRecord>())
                .Where(season => season != null)
                .OrderByDescending(season => season.SeasonYear)
                .Select(season => new HistoricalChampionshipDto { SeasonYear = season.SeasonYear, ChampionTeamName = season.ChampionTeamName ?? "", RunnerUpTeamName = season.RunnerUpTeamName ?? "", ChampionScore = season.ChampionshipWinnerScore, RunnerUpScore = season.ChampionshipRunnerUpScore })
                .ToList(),
            Retirements = (league.RetirementHistory ?? new System.Collections.Generic.List<SeasonRetirementRecord>())
                .SelectMany(season => (season?.Players ?? new System.Collections.Generic.List<PlayerRetirementRecord>()).Where(player => player != null))
                .OrderByDescending(player => player.SeasonYear)
                .ThenBy(player => player.PlayerName, StringComparer.OrdinalIgnoreCase)
                .Select(player => new HistoricalRetirementDto { SeasonYear = player.SeasonYear, PlayerName = player.PlayerName ?? "", TeamName = player.TeamName ?? "", Position = player.Position ?? "", Age = player.Age, Reason = player.ReasonLabel ?? "" })
                .ToList(),
        };
    }

    private SeasonCompletionSummaryDto BuildSeasonCompletionSummary(LeagueState league)
    {
        var record = _seasonHistoryService.GetLatestSeasonRecord(league);
        if (record == null || record.SeasonYear != league?.SeasonYear)
            return new SeasonCompletionSummaryDto();

        return new SeasonCompletionSummaryDto
        {
            IsAvailable = true,
            CompletedPhaseLabel = string.IsNullOrWhiteSpace(record.CompletedPhaseLabel)
                ? ScheduleService.SeasonCompletePhase
                : record.CompletedPhaseLabel,
            ChampionTeamName = record.ChampionTeamName ?? "",
            RunnerUpTeamName = record.RunnerUpTeamName ?? "",
            ChampionshipResultLine = $"{record.ChampionTeamName} {record.ChampionshipWinnerScore} def. {record.RunnerUpTeamName} {record.ChampionshipRunnerUpScore}",
        };
    }

    private static string ResolveDayOfWeek(string currentDate)
    {
        if (DateTime.TryParse(currentDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.ToString("dddd", CultureInfo.InvariantCulture);
        return "";
    }

    private System.Collections.Generic.List<ActionItemDto> BuildActionItems(
        ScheduledGame nextGame,
        TeamState opponent,
        TeamRosterResponse roster,
        TeamDepthChartResponse depthChart,
        PlayoffBracketDto playoffBracket)
    {
        var items = new System.Collections.Generic.List<ActionItemDto>();

        var pendingWaiver = (_context.ActiveLeague?.Waivers ?? new System.Collections.Generic.List<WaiverClaimState>())
            .FirstOrDefault(waiver => waiver?.PendingConfirmation == true && string.Equals(waiver.PendingClaimTeamId, _context.ActiveLeague.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (pendingWaiver?.Player != null)
        {
            var pendingTeam = _context.ActiveLeague.Teams.FirstOrDefault(team => string.Equals(team.TeamId, pendingWaiver.PendingClaimTeamId, StringComparison.OrdinalIgnoreCase));
            var conditionalRelease = pendingTeam?.Roster?.FirstOrDefault(player => string.Equals(player.PlayerId, pendingWaiver.ConditionalReleasePlayerId, StringComparison.OrdinalIgnoreCase));
            var releaseSummary = conditionalRelease == null ? "No conditional release is attached." : $"Finalizing will release {conditionalRelease.Name} ({conditionalRelease.Position}); cancelling leaves that player on the roster.";
            items.Add(new ActionItemDto
            {
                Type = "waiver_claim_confirmation",
                Title = "Action Required: Winning Waiver Claim",
                Description = $"The League Office awarded the pending claim for {pendingWaiver.Player.Name} ({pendingWaiver.Player.Position}). The inherited contract is {GameCoreStateHelper.FormatCapRoom(pendingWaiver.Player.Contract?.AnnualSalary ?? 0m)} annually. {releaseSummary} Finalize or cancel the opportunity before time can advance; no transfer or release has occurred yet.",
                PrimaryAction = "Review Waiver Decision",
            });
        }

        if (roster.Ok && roster.RosterStatus != null && !roster.RosterStatus.IsValid)
        {
            items.Add(new ActionItemDto
            {
                Type = "roster_invalid",
                Title = "Roster Issue",
                Description = $"Roster has {roster.RosterStatus.RosterSize} players. Limit is {roster.RosterStatus.RosterLimit}. Cut {roster.RosterStatus.RequiredCuts} players.",
                PrimaryAction = "View Roster",
            });
        }

        if (depthChart.Ok && depthChart.DepthChartStatus != null && !depthChart.DepthChartStatus.IsValid)
        {
            items.Add(new ActionItemDto
            {
                Type = "depth_chart_invalid",
                Title = "Depth Chart Issue",
                Description = string.Join(" ", depthChart.DepthChartStatus.Issues),
                PrimaryAction = "View Depth Chart",
            });
        }

        if (depthChart.Ok && depthChart.DepthChartStatus?.IsValid == true)
        {
            var thinInjuryGroups = depthChart.Positions
                .Select(position => new
                {
                    position.Position,
                    position.RequiredStarters,
                    Available = position.Players.Count(player => player.IsAvailable),
                    Unavailable = position.Players.Count(player => !player.IsAvailable),
                })
                .Where(group => group.RequiredStarters > 0 && group.Available == group.RequiredStarters && group.Unavailable > 0)
                .OrderBy(group => FootballPositionOrder.GetSortOrder(group.Position))
                .ThenBy(group => group.Position, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (thinInjuryGroups.Count > 0)
            {
                items.Add(new ActionItemDto
                {
                    Type = "injury_depth_advisory",
                    Title = "Injury Depth Advisory",
                    Description = $"The roster remains legal, but injuries leave no available reserve at {string.Join(", ", thinInjuryGroups.Select(group => group.Position))}. Review availability and the saved emergency order; this advisory does not authorize a signing or depth change.",
                    PrimaryAction = "Review Depth Chart",
                });
            }
        }

        var calendar = _context.ActiveLeague?.Calendar;
        if (string.Equals(calendar?.Phase, "Regular Season", StringComparison.OrdinalIgnoreCase)
            && calendar.PhaseWeek == 1
            && nextGame != null
            && string.Equals(nextGame.GameType, "regular_season", StringComparison.OrdinalIgnoreCase)
            && nextGame.PhaseWeek == 1)
        {
            items.Add(new ActionItemDto
            {
                Type = "opening_week_readiness",
                Title = "Opening Week Readiness",
                Description = $"Week 1 against {opponent?.Name ?? "TBD"} is approaching. Review the active roster, player availability, and saved depth order before game day. This reminder does not change personnel or assignments.",
                PrimaryAction = "Review Roster",
            });
        }

        if (_gameDayService.GetCurrentUserGame() != null)
        {
            items.Add(new ActionItemDto
            {
                Type = "game_day",
                Title = "Game Day",
                Description = nextGame == null
                    ? "User team has a game today."
                    : $"Prepare for {nextGame.WeekLabel} against {opponent?.Name ?? "TBD"}.",
                PrimaryAction = "View Matchup",
            });
        }

        if (string.Equals(_context.ActiveLeague?.Calendar?.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            var bracketAvailable = playoffBracket?.ConferenceBrackets != null
                && playoffBracket.ConferenceBrackets.Count > 0;
            var wildCardCompleted = IsWildCardRoundCompleted(playoffBracket);
            var divisionalCompleted = IsDivisionalRoundCompleted(playoffBracket);
            var conferenceChampionshipCompleted = IsConferenceChampionshipCompleted(playoffBracket);
            var leagueChampionshipCompleted = IsLeagueChampionshipCompleted(playoffBracket);
            items.Add(new ActionItemDto
            {
                Type = "postseason_pending",
                Title = !bracketAvailable
                    ? "Action Required: Playoff bracket could not be generated."
                    : leagueChampionshipCompleted
                        ? "Season complete."
                        : conferenceChampionshipCompleted
                            ? "Action Required: Simulate the League Championship."
                            : divisionalCompleted
                                ? "Action Required: Simulate the Conference Championship."
                                : wildCardCompleted
                                ? "Action Required: Simulate the Divisional round."
                                : "Action Required: Simulate the Wild Card round.",
                Description = bracketAvailable
                    ? leagueChampionshipCompleted
                        ? "Season complete. Continue to begin the offseason."
                        : conferenceChampionshipCompleted
                            ? "Conference Championship results are final. The League Championship is ready for native simulation."
                            : divisionalCompleted
                                ? "Divisional results are final. The Conference Championship is ready for native simulation."
                                : wildCardCompleted
                                ? "Wild Card results are final. The Divisional Round is ready for native simulation."
                                : "Regular season complete. The playoff bracket is ready for native Wild Card simulation."
                    : "Regular season complete, but the playoff bracket is missing.",
                PrimaryAction = "View Playoffs",
            });
        }

        if (string.Equals(_context.ActiveLeague?.Calendar?.Phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase))
        {
            items.Add(new ActionItemDto
            {
                Type = "season_complete",
                Title = "Season complete.",
                Description = "Season complete. Continue to begin the offseason.",
                PrimaryAction = "Continue",
            });
        }

        var currentPhase = _context.ActiveLeague?.Calendar?.Phase ?? "";
        if (ScheduleService.IsOffseasonPlaceholderPhase(currentPhase))
        {
            items.Add(BuildOffseasonActionItem(_context.ActiveLeague, currentPhase));

            var retirementSummary = BuildRetirementSummaryActionItem(_context.ActiveLeague, currentPhase);
            if (retirementSummary != null)
                items.Add(retirementSummary);
        }

        return items;
    }

    private static NextGameDto BuildNextGameDto(
        LeagueState league,
        TeamState userTeam,
        ScheduledGame nextGame,
        TeamState opponent,
        PlayoffBracketDto playoffBracket)
    {
        var dto = new NextGameDto
        {
            Opponent = opponent?.Name ?? "TBD",
            OpponentAbbreviation = opponent?.Abbreviation ?? "",
            HomeAway = nextGame == null
                ? ""
                : string.Equals(nextGame.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase) ? "home" : "away",
            Week = nextGame?.PhaseWeek ?? 0,
            AbsoluteWeek = nextGame?.AbsoluteWeek ?? 0,
            PhaseWeek = nextGame?.PhaseWeek ?? 0,
            Phase = nextGame?.Phase ?? "",
            GameType = nextGame?.GameType ?? "",
            GameId = nextGame?.GameId ?? "",
            WeekLabel = nextGame?.WeekLabel ?? "",
        };

        ApplyDefaultNextGameLabels(dto);

        if (string.Equals(league?.Calendar?.Phase, ScheduleService.PostseasonPendingPhase, StringComparison.OrdinalIgnoreCase))
            ApplyPostseasonPendingLabels(userTeam, playoffBracket, dto);
        else if (string.Equals(league?.Calendar?.Phase, ScheduleService.SeasonCompletePhase, StringComparison.OrdinalIgnoreCase))
        {
            dto.HeaderNextLabel = "Next: Season Complete";
            dto.HeaderOpponentLabel = "Next opponent: TBD";
        }
        else if (ScheduleService.IsOffseasonPlaceholderPhase(league?.Calendar?.Phase))
        {
            var phaseLabel = ScheduleService.GetOffseasonPhaseLabel(league?.Calendar?.Phase);
            dto.HeaderNextLabel = $"Next: {phaseLabel}";
            dto.HeaderOpponentLabel = "Next opponent: TBD";
            dto.Opponent = "TBD";
            dto.OpponentAbbreviation = "";
            dto.HomeAway = "";
            dto.GameId = "";
            dto.GameType = "";
            dto.Phase = phaseLabel;
            dto.PhaseWeek = 0;
            dto.Week = 0;
            dto.AbsoluteWeek = 0;
            dto.WeekLabel = phaseLabel;
        }

        return dto;
    }

    private static ActionItemDto BuildOffseasonActionItem(LeagueState league, string phase)
    {
        var phaseLabel = ScheduleService.GetOffseasonPhaseLabel(phase);
        var phaseKey = ScheduleService.GetOffseasonPhaseKey(phase);
        var isRetirement = string.Equals(phaseKey, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
        var isDraftPrep = string.Equals(phaseKey, ScheduleService.DraftPrepPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
        var seasonRetirements = RetirementService.GetSeasonRetirementRecord(league, league?.SeasonYear ?? 0);
        return new ActionItemDto
        {
            Type = phaseKey,
            Title = isDraftPrep ? "Draft Board Review Reminder" : phaseLabel,
            Description = isDraftPrep
                ? "The draft begins after this preparation day. Review your private Team Draft Board, including its manual order, tiers, notes, tags, and current scouting confidence. No approval or board change is required."
                : isRetirement
                ? seasonRetirements?.Completed == true
                    ? $"{seasonRetirements.RetiredCount} players retired."
                    : "Retirement decisions pending."
                : BuildOffseasonActionDescription(phaseKey),
            PrimaryAction = isDraftPrep
                ? "Review Team Draft Board"
                : isRetirement
                ? seasonRetirements?.Completed == true
                    ? "Continue to next offseason phase"
                    : "Continue to process retirements"
                : string.Equals(phaseKey, ScheduleService.FreeAgencyPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
                    ? "Open Free Agency"
                    : string.Equals(phaseKey, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
                        ? "Review Roster"
                    : "Continue to next offseason phase",
        };
    }

    private static string BuildOffseasonActionDescription(string phaseKey)
    {
        return phaseKey switch
        {
            ScheduleService.OffseasonPendingPhaseKey => "Process expiring contracts and prepare the offseason market.",
            ScheduleService.StaffCarouselPendingPhaseKey => "Staff changes are not available in this build. Continue to retirement processing.",
            ScheduleService.ExclusiveNegotiationPendingPhaseKey => "Contract extensions and releases are available before free agency.",
            ScheduleService.FranchiseTagPendingPhaseKey => "Apply one franchise tag to an eligible final-year player or continue to process expirations.",
            ScheduleService.LeagueYearPendingPhaseKey => "The new league year is ready to open free agency.",
            ScheduleService.FreeAgencyPendingPhaseKey => "Free agency is open. Review the market and submit offers.",
            ScheduleService.DraftPrepPendingPhaseKey => "Draft preparation is active.",
            ScheduleService.DraftPendingPhaseKey => "The draft is ready for selections.",
            ScheduleService.RookieSigningPendingPhaseKey => "Rookie signing is pending.",
            ScheduleService.TrainingCampPendingPhaseKey => "Finalize your 53-player roster to begin the next preseason.",
            _ => "Continue to the next offseason phase.",
        };
    }

    private static ActionItemDto BuildRetirementSummaryActionItem(LeagueState league, string currentPhase)
    {
        var currentPhaseKey = ScheduleService.GetOffseasonPhaseKey(currentPhase);
        if (string.IsNullOrWhiteSpace(currentPhaseKey)
            || string.Equals(currentPhaseKey, ScheduleService.OffseasonPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentPhaseKey, ScheduleService.StaffCarouselPendingPhaseKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(currentPhaseKey, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var seasonRetirements = RetirementService.GetSeasonRetirementRecord(league, league?.SeasonYear ?? 0);
        if (seasonRetirements?.Completed != true)
            return null;

        return new ActionItemDto
        {
            Type = "retirement_summary",
            Title = "Retirements",
            Description = $"{seasonRetirements.RetiredCount} players retired.",
            PrimaryAction = "Retirement history will be expanded later",
        };
    }

    private static void ApplyDefaultNextGameLabels(NextGameDto dto)
    {
        var nextOpponent = !string.IsNullOrWhiteSpace(dto.OpponentAbbreviation) ? dto.OpponentAbbreviation : dto.Opponent;
        if (string.IsNullOrWhiteSpace(nextOpponent))
        {
            dto.HeaderOpponentLabel = "No upcoming game";
            dto.HeaderNextLabel = "Next: unavailable";
            return;
        }

        dto.HeaderOpponentLabel = string.Equals(dto.HomeAway, "home", StringComparison.OrdinalIgnoreCase)
            ? $"Next opponent: {nextOpponent} (home)"
            : $"Next opponent: {nextOpponent} (away)";

        var typeText = string.IsNullOrWhiteSpace(dto.GameType) ? "" : $"{ScheduleService.HumanizeGameType(dto.GameType)} ";
        var weekText = dto.Week > 0 ? $"Week {dto.Week}" : "";
        var details = $"{typeText}{weekText}".Trim();
        dto.HeaderNextLabel = string.IsNullOrWhiteSpace(details)
            ? $"Next: {nextOpponent}"
            : $"Next: {details} vs {nextOpponent}";
    }

    private static void ApplyPostseasonPendingLabels(TeamState userTeam, PlayoffBracketDto playoffBracket, NextGameDto dto)
    {
        dto.HeaderNextLabel = "Next: Playoffs Pending";
        dto.HeaderOpponentLabel = "Next opponent: TBD";

        if (userTeam == null || playoffBracket?.ConferenceBrackets == null || playoffBracket.ConferenceBrackets.Count == 0)
            return;

        var conferenceBracket = playoffBracket.ConferenceBrackets.FirstOrDefault(entry =>
            entry?.Seeds != null && entry.Seeds.Any(seed => string.Equals(seed.TeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)));
        if (conferenceBracket == null)
            return;

        var wildCardRound = conferenceBracket.Rounds?.FirstOrDefault(round =>
            string.Equals(round?.Round, "Wild Card", StringComparison.OrdinalIgnoreCase));
        var wildCardGame = wildCardRound?.Games?.FirstOrDefault(game =>
            string.Equals(game?.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(game?.AwayTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase));

        if (wildCardGame != null)
        {
            if (string.Equals(wildCardGame.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                ApplyDivisionalLabels(conferenceBracket, userTeam, playoffBracket, dto);
                return;
            }

            var opponentName = string.Equals(wildCardGame.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)
                ? wildCardGame.AwayTeamName
                : wildCardGame.HomeTeamName;
            dto.HeaderNextLabel = "Next: Wild Card Round";
            dto.HeaderOpponentLabel = $"Next opponent: {NormalizeBracketTeamName(opponentName)}";
            return;
        }

        var userSeed = conferenceBracket.Seeds?.FirstOrDefault(seed => string.Equals(seed.TeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase));
        if (userSeed?.Seed == 1)
        {
            if (IsWildCardRoundCompleted(playoffBracket))
            {
                ApplyDivisionalLabels(conferenceBracket, userTeam, playoffBracket, dto);
                return;
            }

            dto.HeaderNextLabel = "Next: Wild Card Bye";
        }
    }

    private static void ApplyDivisionalLabels(
        PlayoffConferenceBracketDto conferenceBracket,
        TeamState userTeam,
        PlayoffBracketDto playoffBracket,
        NextGameDto dto)
    {
        if (IsDivisionalRoundCompleted(playoffBracket))
        {
            ApplyConferenceChampionshipLabels(conferenceBracket, userTeam, playoffBracket, dto);
            return;
        }

        var divisionalRound = conferenceBracket?.Rounds?.FirstOrDefault(round =>
            string.Equals(round?.Round, PlayoffService.DivisionalRound, StringComparison.OrdinalIgnoreCase));
        var divisionalGame = divisionalRound?.Games?.FirstOrDefault(game =>
            string.Equals(game?.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(game?.AwayTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase));
        if (divisionalGame == null)
        {
            dto.HeaderNextLabel = "Next: Divisional Round Pending";
            dto.HeaderOpponentLabel = "Next opponent: TBD";
            return;
        }

        if (string.Equals(divisionalGame.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            ApplyConferenceChampionshipLabels(conferenceBracket, userTeam, playoffBracket, dto);
            return;
        }

        var opponentName = string.Equals(divisionalGame.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)
            ? divisionalGame.AwayTeamName
            : divisionalGame.HomeTeamName;
        dto.HeaderNextLabel = "Next: Divisional Round";
        dto.HeaderOpponentLabel = $"Next opponent: {NormalizeBracketTeamName(opponentName)}";
    }

    private static void ApplyConferenceChampionshipLabels(
        PlayoffConferenceBracketDto conferenceBracket,
        TeamState userTeam,
        PlayoffBracketDto playoffBracket,
        NextGameDto dto)
    {
        if (IsConferenceChampionshipCompleted(playoffBracket))
        {
            ApplyLeagueChampionshipLabels(playoffBracket, dto);
            return;
        }

        var conferenceRound = conferenceBracket?.Rounds?.FirstOrDefault(round =>
            string.Equals(round?.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase));
        var conferenceGame = conferenceRound?.Games?.FirstOrDefault(game =>
            string.Equals(game?.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)
            || string.Equals(game?.AwayTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase));
        if (conferenceGame == null)
        {
            dto.HeaderNextLabel = "Next: Conference Championship Pending";
            dto.HeaderOpponentLabel = "Next opponent: TBD";
            return;
        }

        if (string.Equals(conferenceGame.Status, "completed", StringComparison.OrdinalIgnoreCase))
        {
            ApplyLeagueChampionshipLabels(playoffBracket, dto);
            return;
        }

        var opponentName = string.Equals(conferenceGame.HomeTeamId, userTeam.TeamId, StringComparison.OrdinalIgnoreCase)
            ? conferenceGame.AwayTeamName
            : conferenceGame.HomeTeamName;
        dto.HeaderNextLabel = "Next: Conference Championship";
        dto.HeaderOpponentLabel = $"Next opponent: {NormalizeBracketTeamName(opponentName)}";
    }

    private static void ApplyLeagueChampionshipLabels(
        PlayoffBracketDto playoffBracket,
        NextGameDto dto)
    {
        if (IsLeagueChampionshipCompleted(playoffBracket))
        {
            dto.HeaderNextLabel = "Next: Season Complete";
            dto.HeaderOpponentLabel = "Next opponent: TBD";
            return;
        }

        var game = playoffBracket?.LeagueChampionshipRound?.Games?.FirstOrDefault(entry => entry != null);
        if (game == null)
        {
            dto.HeaderNextLabel = "Next: League Championship Pending";
            dto.HeaderOpponentLabel = "Next opponent: TBD";
            return;
        }

        dto.HeaderNextLabel = "Next: League Championship";
        dto.HeaderOpponentLabel = "Next opponent: TBD";
    }

    private static string NormalizeBracketTeamName(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "TBD" : value.Trim();
    }

    private static bool IsWildCardRoundCompleted(PlayoffBracketDto playoffBracket)
    {
        var wildCardGames = (playoffBracket?.ConferenceBrackets ?? new System.Collections.Generic.List<PlayoffConferenceBracketDto>())
            .SelectMany(entry => entry?.Rounds ?? new System.Collections.Generic.List<PlayoffRoundDto>())
            .Where(round => string.Equals(round?.Round, PlayoffService.WildCardRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new System.Collections.Generic.List<PlayoffGameDto>())
            .Where(game => game != null)
            .ToList();

        return wildCardGames.Count == 6
            && wildCardGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDivisionalRoundCompleted(PlayoffBracketDto playoffBracket)
    {
        var divisionalGames = (playoffBracket?.ConferenceBrackets ?? new System.Collections.Generic.List<PlayoffConferenceBracketDto>())
            .SelectMany(entry => entry?.Rounds ?? new System.Collections.Generic.List<PlayoffRoundDto>())
            .Where(round => string.Equals(round?.Round, PlayoffService.DivisionalRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new System.Collections.Generic.List<PlayoffGameDto>())
            .Where(game => game != null)
            .ToList();

        return divisionalGames.Count == 4
            && divisionalGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsConferenceChampionshipCompleted(PlayoffBracketDto playoffBracket)
    {
        var conferenceGames = (playoffBracket?.ConferenceBrackets ?? new System.Collections.Generic.List<PlayoffConferenceBracketDto>())
            .SelectMany(entry => entry?.Rounds ?? new System.Collections.Generic.List<PlayoffRoundDto>())
            .Where(round => string.Equals(round?.Round, PlayoffService.ConferenceChampionshipRound, StringComparison.OrdinalIgnoreCase))
            .SelectMany(round => round.Games ?? new System.Collections.Generic.List<PlayoffGameDto>())
            .Where(game => game != null)
            .ToList();

        return conferenceGames.Count == 2
            && conferenceGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLeagueChampionshipCompleted(PlayoffBracketDto playoffBracket)
    {
        var leagueGames = playoffBracket?.LeagueChampionshipRound?.Games?
            .Where(game => game != null)
            .ToList()
            ?? new System.Collections.Generic.List<PlayoffGameDto>();

        return leagueGames.Count == 1
            && leagueGames.All(game => string.Equals(game.Status, "completed", StringComparison.OrdinalIgnoreCase));
    }

    private static LeagueHistorySeasonDto MapLeagueHistorySeason(SeasonHistoryRecord record, DraftState draft)
    {
        return new LeagueHistorySeasonDto
        {
            SeasonYear = record?.SeasonYear ?? 0,
            CompletedPhaseLabel = record?.CompletedPhaseLabel ?? "",
            ChampionTeamId = record?.ChampionTeamId ?? "",
            ChampionTeamName = record?.ChampionTeamName ?? "",
            RunnerUpTeamId = record?.RunnerUpTeamId ?? "",
            RunnerUpTeamName = record?.RunnerUpTeamName ?? "",
            ChampionshipGameLabel = record?.ChampionshipGameLabel ?? "",
            ChampionshipWinnerScore = record?.ChampionshipWinnerScore ?? 0,
            ChampionshipRunnerUpScore = record?.ChampionshipRunnerUpScore ?? 0,
            TotalRegularSeasonGames = record?.TotalRegularSeasonGames ?? 0,
            TotalPlayoffGames = record?.TotalPlayoffGames ?? 0,
            GeneratedAtLabel = record?.GeneratedAtLabel ?? "",
            TeamRecords = (record?.TeamRecords ?? new System.Collections.Generic.List<SeasonTeamRecord>())
                .Where(team => team != null)
                .Select(team => new LeagueHistoryTeamRecordDto
                {
                    TeamId = team.TeamId ?? "",
                    TeamName = team.TeamName ?? "",
                    Abbreviation = team.Abbreviation ?? "",
                    Conference = team.Conference ?? "",
                    Division = team.Division ?? "",
                    Wins = team.Wins,
                    Losses = team.Losses,
                    Ties = team.Ties,
                    PointsFor = team.PointsFor,
                    PointsAgainst = team.PointsAgainst,
                    WinPercentage = team.WinPercentage,
                })
                .ToList(),
            PlayoffSeeds = (record?.PlayoffSeeds ?? new System.Collections.Generic.List<SeasonPlayoffSeedRecord>())
                .Where(seed => seed != null)
                .Select(seed => new LeagueHistoryPlayoffSeedDto
                {
                    Conference = seed.Conference ?? "",
                    Seed = seed.Seed,
                    TeamId = seed.TeamId ?? "",
                    TeamName = seed.TeamName ?? "",
                    Division = seed.Division ?? "",
                    IsDivisionWinner = seed.IsDivisionWinner,
                })
                .ToList(),
            PlayoffResults = (record?.PlayoffResults ?? new System.Collections.Generic.List<SeasonPlayoffResultRecord>())
                .Where(result => result != null)
                .Select(result => new LeagueHistoryPlayoffResultDto
                {
                    Round = result.Round ?? "",
                    Conference = result.Conference ?? "",
                    HomeTeamId = result.HomeTeamId ?? "",
                    HomeTeamName = result.HomeTeamName ?? "",
                    AwayTeamId = result.AwayTeamId ?? "",
                    AwayTeamName = result.AwayTeamName ?? "",
                    HomeScore = result.HomeScore,
                    AwayScore = result.AwayScore,
                    WinnerTeamId = result.WinnerTeamId ?? "",
                    WinnerTeamName = result.WinnerTeamName ?? "",
                    LoserTeamId = result.LoserTeamId ?? "",
                    LoserTeamName = result.LoserTeamName ?? "",
                })
                .ToList(),
            Awards = (record?.Awards ?? new System.Collections.Generic.List<SeasonAwardRecord>())
                .Where(award => award != null)
                .Select(award => new SeasonAwardDto { AwardName = award.AwardName ?? "", PlayerName = award.PlayerName ?? "", TeamName = award.TeamName ?? "", Position = award.Position ?? "", Summary = award.Summary ?? "" })
                .ToList(),
            DraftClass = (draft?.RecapEntries ?? new System.Collections.Generic.List<DraftClassRecapEntry>())
                .Where(entry => entry != null)
                .OrderBy(entry => entry.OverallPick)
                .Select(entry => new DraftClassRecapDto
                {
                    OverallPick = entry.OverallPick, Round = entry.Round, PickInRound = entry.PickInRound, TeamName = entry.TeamName ?? "", Name = entry.Name ?? "", Position = entry.Position ?? "", College = entry.College ?? "", Age = entry.Age,
                    EstimatedOverall = FormatEstimateRange(entry.ScoutedOverall, entry.ScoutingConfidence), EstimatedPotential = FormatEstimateRange(entry.ScoutedPotential, entry.ScoutingConfidence, 1), Confidence = GetConfidenceLabel(entry.ScoutingConfidence),
                    CombineScore = entry.CombineScore, ProDayScore = entry.ProDayScore, Report = entry.ScoutingReport ?? "", Trait = entry.Trait ?? "", Interview = entry.InterviewSummary ?? "", RookiePlacement = entry.RookiePlacement ?? "", ContractSummary = FormatContractSummary(entry),
                })
                .ToList(),
        };
    }

    private static string FormatEstimateRange(int estimate, int confidence, int extraSpread = 0)
    {
        var spread = (confidence >= 80 ? 3 : confidence >= 60 ? 5 : 8) + extraSpread;
        return $"{Math.Clamp(estimate - spread, 40, 99)}-{Math.Clamp(estimate + spread, 40, 99)}";
    }

    private static string GetConfidenceLabel(int confidence)
        => confidence >= 80 ? "High" : confidence >= 60 ? "Medium" : "Low";

    private static string FormatContractSummary(DraftClassRecapEntry entry)
        => string.IsNullOrWhiteSpace(entry.ContractType)
            ? "Contract unavailable"
            : $"{entry.ContractType}: ${entry.ContractAnnualSalary / 1_000_000m:0.00}M annual, ${entry.ContractGuaranteedSalary / 1_000_000m:0.00}M guaranteed, {entry.ContractYears} year(s)";
}
