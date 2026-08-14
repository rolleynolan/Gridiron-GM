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
            .Select(MapLeagueHistorySeason)
            .ToList();

        return new LeagueHistoryResponse
        {
            Ok = true,
            Seasons = seasons,
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
                        ? "Season complete. Offseason systems are not implemented yet."
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
                Description = "Season complete. Offseason systems are not implemented yet.",
                PrimaryAction = "Continue",
            });
        }

        var currentPhase = _context.ActiveLeague?.Calendar?.Phase ?? "";
        if (ScheduleService.IsOffseasonPlaceholderPhase(currentPhase))
        {
            items.Add(BuildOffseasonPlaceholderActionItem(_context.ActiveLeague, currentPhase));

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

    private static ActionItemDto BuildOffseasonPlaceholderActionItem(LeagueState league, string phase)
    {
        var phaseLabel = ScheduleService.GetOffseasonPhaseLabel(phase);
        var phaseKey = ScheduleService.GetOffseasonPhaseKey(phase);
        var isTrainingCamp = string.Equals(phaseKey, ScheduleService.TrainingCampPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
        var isRetirement = string.Equals(phaseKey, ScheduleService.RetirementPendingPhaseKey, StringComparison.OrdinalIgnoreCase);
        var seasonRetirements = RetirementService.GetSeasonRetirementRecord(league, league?.SeasonYear ?? 0);
        return new ActionItemDto
        {
            Type = phaseKey,
            Title = phaseLabel,
            Description = isRetirement
                ? seasonRetirements?.Completed == true
                    ? $"{seasonRetirements.RetiredCount} players retired."
                    : "Retirement decisions pending."
                : isTrainingCamp
                ? "Training camp systems are not implemented yet."
                : $"{phaseLabel} is not implemented yet. Continue to move through the placeholder offseason flow.",
            PrimaryAction = isRetirement
                ? seasonRetirements?.Completed == true
                    ? "Continue to next offseason phase"
                    : "Continue to process retirements"
                : isTrainingCamp
                ? "Training camp systems are not implemented yet."
                : "Continue to next offseason phase",
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

    private static LeagueHistorySeasonDto MapLeagueHistorySeason(SeasonHistoryRecord record)
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
        };
    }
}
