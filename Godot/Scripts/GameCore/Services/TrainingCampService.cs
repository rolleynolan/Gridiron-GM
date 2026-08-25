using System;
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
                RosterFinalized = camp.RosterFinalized,
                FocusPosition = camp.FocusPosition,
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
