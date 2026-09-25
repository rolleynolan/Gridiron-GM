using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class TrainingCampService
{
    private readonly GameCoreContext _context;

    public TrainingCampService(GameCoreContext context) => _context = context;

    public TrainingCampDecisionResponse GetStatus(string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TrainingCampDecisionResponse { Message = "Team not found." };

        var camp = team.TrainingCamp ??= new TrainingCampState();
        return new TrainingCampDecisionResponse
        {
            Ok = true,
            Status = new TrainingCampStatusDto
            {
                IsAvailable = string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase),
                FocusApplied = camp.FocusApplied,
                PlayerFocusApplied = camp.PlayerFocusApplied,
                RosterFinalized = camp.RosterFinalized,
                FocusPosition = camp.FocusPosition,
                FocusPlayerId = camp.FocusPlayerId,
                FocusPlayerName = camp.FocusPlayerName,
                Summary = camp.Summary,
                Report = ToDto(camp.Report),
            },
        };
    }

    public TrainingCampDecisionResponse GenerateReport(string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TrainingCampDecisionResponse { Message = "Team not found." };
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return new TrainingCampDecisionResponse { Message = "Training-camp reports are only available during training camp." };

        var camp = team.TrainingCamp ??= new TrainingCampState();
        camp.Report = TrainingCampReportService.Build(team);
        return new TrainingCampDecisionResponse { Ok = true, Completed = true, Message = camp.Report.Summary, Status = GetStatus(team.TeamId).Status };
    }

    public TrainingCampDecisionResponse ApplyPositionFocus(string position, string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TrainingCampDecisionResponse { Message = "Team not found." };
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return new TrainingCampDecisionResponse { Message = "Training-camp focus is only available during training camp." };
        if (string.IsNullOrWhiteSpace(position))
            return new TrainingCampDecisionResponse { Message = "Choose a position group for camp focus." };

        var camp = team.TrainingCamp ??= new TrainingCampState();
        if (camp.FocusApplied)
            return new TrainingCampDecisionResponse { Message = "Training-camp focus has already been applied." };
        var players = team.Roster.Where(player => string.Equals(player.Position, position, StringComparison.OrdinalIgnoreCase))
            .Where(PlayerInjuryService.IsAvailableForGame)
            .OrderBy(player => player.Overall)
            .ThenBy(player => player.Age)
            .ThenBy(player => player.PlayerId, StringComparer.Ordinal)
            .Take(3)
            .ToList();
        if (players.Count == 0)
            return new TrainingCampDecisionResponse { Message = "That position group has no available active-roster players." };

        foreach (var player in players)
        {
            player.Fatigue = Math.Max(0, player.Fatigue - 20);
            if (player.Overall < player.Potential)
                player.Overall++;
        }
        camp.FocusPosition = position;
        camp.FocusApplied = true;
        camp.Summary = $"{position} focus improved readiness for {players.Count} player(s).";
        camp.Report = TrainingCampReportService.Build(team);
        new DepthChartService(_context).AutoFillDepthChart(team.TeamId);
        new TransactionService(_context).RecordTrainingCampDecision(team, camp.Summary);
        return new TrainingCampDecisionResponse { Ok = true, Completed = true, Message = camp.Summary, Status = GetStatus(team.TeamId).Status };
    }

    public TrainingCampDecisionResponse ApplyPlayerFocus(string playerId, string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TrainingCampDecisionResponse { Message = "Team not found." };
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return new TrainingCampDecisionResponse { Message = "Training-camp player focus is only available during training camp." };

        var camp = team.TrainingCamp ??= new TrainingCampState();
        if (camp.PlayerFocusApplied)
            return new TrainingCampDecisionResponse { Message = $"Player focus has already been assigned to {camp.FocusPlayerName}." };
        var player = team.Roster.FirstOrDefault(candidate => string.Equals(candidate.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        if (player == null)
            return new TrainingCampDecisionResponse { Message = "Choose an active-roster player for focused camp reps." };
        if (!PlayerInjuryService.IsAvailableForGame(player))
            return new TrainingCampDecisionResponse { Message = $"{player.Name} is unavailable and cannot take focused camp reps." };

        player.Fatigue = Math.Max(0, player.Fatigue - 15);
        if (player.Overall < player.Potential)
            player.Overall++;
        camp.FocusPlayerId = player.PlayerId;
        camp.FocusPlayerName = player.Name;
        camp.PlayerFocusApplied = true;
        var message = $"{player.Name} received focused camp reps; readiness improved and development remained bounded by potential.";
        camp.Summary = string.IsNullOrWhiteSpace(camp.Summary) ? message : $"{camp.Summary} {message}";
        camp.Report = TrainingCampReportService.Build(team);
        new TransactionService(_context).RecordTrainingCampDecision(team, message);
        return new TrainingCampDecisionResponse { Ok = true, Completed = true, Message = message, Status = GetStatus(team.TeamId).Status };
    }

    public TrainingCampCutPreviewDto PreviewRosterCuts(IEnumerable<string> playerIds, string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TrainingCampCutPreviewDto { Error = "Team not found." };
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return new TrainingCampCutPreviewDto { Error = "Final roster cuts are only available during training camp." };

        var ids = (playerIds ?? Enumerable.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ids.Count == 0)
            return new TrainingCampCutPreviewDto { Error = "Select at least one player for the proposed cut list." };
        var players = ids.Select(id => team.Roster.FirstOrDefault(player => string.Equals(player.PlayerId, id, StringComparison.OrdinalIgnoreCase))).ToList();
        if (players.Any(player => player == null))
            return new TrainingCampCutPreviewDto { Error = "The proposed cut list no longer matches the active roster. Review it again." };

        var contracts = new ContractService(_context);
        var payrollBefore = contracts.GetCommittedSalary(team);
        var releasedSalary = players.Sum(player => Math.Max(0m, player.Contract?.AnnualSalary ?? 0m));
        var projected = team.Roster.Where(player => !ids.Contains(player.PlayerId, StringComparer.OrdinalIgnoreCase)).ToList();
        var warnings = DepthChartRules.RequiredStartersByPosition
            .Select(requirement => new { requirement.Key, requirement.Value, Count = projected.Count(player => string.Equals(player.Position, requirement.Key, StringComparison.OrdinalIgnoreCase) && PlayerInjuryService.IsAvailableForGame(player)) })
            .Where(group => group.Count < group.Value)
            .Select(group => $"{group.Key}: {group.Count}/{group.Value} available starter(s)")
            .ToList();
        var capBefore = contracts.GetCapRoom(team);
        return new TrainingCampCutPreviewDto
        {
            Ok = true,
            PlayerIds = ids,
            PlayerNames = players.Select(player => player.Name).ToList(),
            PositionWarnings = warnings,
            RosterCountBefore = team.Roster.Count,
            RosterCountAfter = projected.Count,
            RequiredCutsBefore = Math.Max(0, team.Roster.Count - RosterService.RosterLimit),
            RequiredCutsAfter = Math.Max(0, projected.Count - RosterService.RosterLimit),
            PayrollBefore = payrollBefore,
            PayrollAfter = Math.Max(0m, payrollBefore - releasedSalary),
            CapRoomBefore = capBefore,
            CapRoomAfter = Math.Min(league.SalaryCap, capBefore + releasedSalary),
        };
    }

    public TrainingCampDecisionResponse ConfirmRosterCuts(IEnumerable<string> playerIds, string teamId = null)
    {
        var preview = PreviewRosterCuts(playerIds, teamId);
        if (!preview.Ok)
            return new TrainingCampDecisionResponse { Message = preview.Error };
        var result = new ContractService(_context).ReleasePlayers(preview.PlayerIds, teamId);
        return new TrainingCampDecisionResponse
        {
            Ok = result.Ok && result.Accepted,
            Completed = result.Accepted,
            Message = result.Message,
            Status = GetStatus(teamId).Status,
        };
    }

    public TrainingCampDecisionResponse FinalizeRoster(string teamId = null)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null)
            return new TrainingCampDecisionResponse { Message = "Team not found." };
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
            return new TrainingCampDecisionResponse { Message = "Roster finalization is only available during training camp." };
        var camp = team.TrainingCamp ??= new TrainingCampState();
        if (!camp.FocusApplied)
            return new TrainingCampDecisionResponse { Message = "Choose a training-camp position focus before finalizing the roster." };
        if (team.Roster.Count > RosterService.RosterLimit)
            return new TrainingCampDecisionResponse { Message = $"Reduce the roster to {RosterService.RosterLimit} players before finalizing." };
        var depthChart = new DepthChartService(_context).GetTeamDepthChart(team.TeamId);
        if (!depthChart.DepthChartStatus.IsValid)
            return new TrainingCampDecisionResponse { Message = string.Join(" ", depthChart.DepthChartStatus.Issues) };

        camp.RosterFinalized = true;
        camp.Summary = $"{camp.FocusPosition} focus complete; {team.Roster.Count}-player roster finalized.";
        new TransactionService(_context).RecordTrainingCampDecision(team, camp.Summary);
        return new TrainingCampDecisionResponse { Ok = true, Completed = true, Message = camp.Summary, Status = GetStatus(team.TeamId).Status };
    }

    private static TrainingCampReportDto ToDto(TrainingCampReportState report)
        => new()
        {
            Summary = report?.Summary ?? "",
            RecommendedFocusPosition = report?.RecommendedFocusPosition ?? "",
            Positions = (report?.Positions ?? new System.Collections.Generic.List<TrainingCampPositionReport>())
                .Select(position => new TrainingCampPositionReportDto
                {
                    Position = position.Position,
                    RequiredStarters = position.RequiredStarters,
                    AvailablePlayers = position.AvailablePlayers,
                    UnavailablePlayers = position.UnavailablePlayers,
                    AverageOverall = position.AverageOverall,
                    AveragePotential = position.AveragePotential,
                    AverageFatigue = position.AverageFatigue,
                    Recommendation = position.Recommendation,
                }).ToList(),
        };
}
