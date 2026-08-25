using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class SeasonRolloverService
{
    private readonly GameCoreContext _context;

    public SeasonRolloverService(GameCoreContext context)
    {
        _context = context;
    }

    public bool StartNextSeason(out string message)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            message = "No active league loaded.";
            return false;
        }
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.TrainingCampPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            message = "The next season can only begin during training camp.";
            return false;
        }
        var userTeam = league.Teams.FirstOrDefault(team => string.Equals(team?.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (userTeam == null || userTeam.Roster.Count > RosterService.RosterLimit)
        {
            message = $"Reduce the user roster to {RosterService.RosterLimit} players before starting the season.";
            return false;
        }
        if (userTeam.TrainingCamp?.RosterFinalized != true)
        {
            message = "Choose a training-camp focus and finalize the legal roster before starting the season.";
            return false;
        }

        ArchiveDraft(league);
        ConvertUndraftedProspects(league);
        AgePlayers(league);
        league.SeasonYear++;
        foreach (var team in league.Teams.Where(team => team != null))
        {
            team.Wins = 0;
            team.Losses = 0;
            team.Ties = 0;
            team.TrainingCamp = new TrainingCampState();
        }

        league.Draft = new DraftState();
        league.CollegeProspects = LeagueBootstrapService.CreateProspectClass(league, league.SeasonYear + 1);
        league.Schedule = LeagueBootstrapService.BuildDeterministicSchedule(league.Teams);
        league.Results = new List<GameResult>();
        league.PlayoffBracket = new PlayoffBracket();
        league.Calendar = new CalendarState
        {
            Year = league.SeasonYear,
            Week = 1,
            AbsoluteWeek = 1,
            PhaseWeek = 1,
            DayIndex = 0,
            Phase = "Preseason",
            CurrentDate = $"{league.SeasonYear}-08-01",
            WeekLabel = ScheduleService.BuildCalendarWeekLabel(1),
        };
        new ContractService(_context).RefreshCapRoom(league);
        new ScheduleService(_context).RefreshStatuses(league);
        message = $"Season {league.SeasonYear} preseason has begun.";
        return true;
    }

    private static void ArchiveDraft(LeagueState league)
    {
        if (league.Draft?.DraftYear <= 0 || league.Draft.Picks == null || league.Draft.Picks.Count == 0)
            return;

        league.HistoricalDrafts ??= new List<DraftState>();
        if (league.HistoricalDrafts.Any(draft => draft != null && draft.DraftYear == league.Draft.DraftYear))
            return;
        league.HistoricalDrafts.Add(new DraftState
        {
            DraftYear = league.Draft.DraftYear,
            IsCompleted = league.Draft.IsCompleted,
            Picks = league.Draft.Picks.Where(pick => pick != null).Select(pick => new DraftPickState
            {
                OverallPick = pick.OverallPick,
                Round = pick.Round,
                PickInRound = pick.PickInRound,
                TeamId = pick.TeamId,
                ProspectId = pick.ProspectId,
                PlayerId = pick.PlayerId,
            }).ToList(),
            RecapEntries = league.Draft.RecapEntries.Where(entry => entry != null).Select(entry => new DraftClassRecapEntry
            {
                OverallPick = entry.OverallPick, Round = entry.Round, PickInRound = entry.PickInRound, TeamId = entry.TeamId, TeamName = entry.TeamName, ProspectId = entry.ProspectId, PlayerId = entry.PlayerId, Name = entry.Name, Position = entry.Position, College = entry.College, Age = entry.Age,
                ScoutedOverall = entry.ScoutedOverall, ScoutedPotential = entry.ScoutedPotential, ScoutingConfidence = entry.ScoutingConfidence, CombineScore = entry.CombineScore, ProDayScore = entry.ProDayScore, ScoutingReport = entry.ScoutingReport, Trait = entry.Trait, InterviewSummary = entry.InterviewSummary,
                RookiePlacement = entry.RookiePlacement, ContractAnnualSalary = entry.ContractAnnualSalary, ContractGuaranteedSalary = entry.ContractGuaranteedSalary, ContractYears = entry.ContractYears, ContractType = entry.ContractType,
            }).ToList(),
        });
    }

    private static void ConvertUndraftedProspects(LeagueState league)
    {
        var undrafted = league.CollegeProspects
            .Where(prospect => prospect != null
                && prospect.DraftClassYear <= league.SeasonYear + 1
                && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId))
            .ToList();
        foreach (var prospect in undrafted)
        {
            league.FreeAgents.Add(new PlayerState
            {
                PlayerId = $"udfa-{league.SeasonYear}-{prospect.ProspectId}",
                Name = prospect.Name,
                Position = prospect.Position,
                Overall = prospect.Overall,
                Potential = prospect.Potential,
                Age = prospect.Age,
                Status = "Free Agent",
                Morale = 50,
                MoraleTrend = "Stable",
                Contract = new PlayerContractState { ContractType = "Undrafted Free Agent" },
            });
        }
        league.CollegeProspects = league.CollegeProspects.Except(undrafted).ToList();
    }

    private static void AgePlayers(LeagueState league)
    {
        PlayerInjuryService.ClearForNewSeason(league);
        var players = league.Teams.SelectMany(team => team?.Roster ?? Enumerable.Empty<PlayerState>())
            .Concat(league.Teams.SelectMany(team => team?.InjuredReserve ?? Enumerable.Empty<PlayerState>()))
            .Concat(league.Teams.SelectMany(team => team?.PracticeSquad ?? Enumerable.Empty<PlayerState>()))
            .Concat(league.FreeAgents)
            .Where(player => player != null)
            .GroupBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First());
        foreach (var player in players)
        {
            player.SeasonStats ??= new PlayerSeasonStats();
            player.CareerStats ??= new List<PlayerSeasonStats>();
            if (player.SeasonStats.SeasonYear == league.SeasonYear && player.SeasonStats.GamesPlayed > 0)
                player.CareerStats.Add(player.SeasonStats.Copy());
            player.SeasonStats = new PlayerSeasonStats();
            player.Fatigue = 0;
            PlayerDevelopmentService.ApplyAnnualDevelopment(player);
            player.Age++;
        }
    }
}
