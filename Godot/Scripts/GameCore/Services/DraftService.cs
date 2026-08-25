using System;
using System.Linq;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class DraftService
{
    public const int DraftRounds = 7;
    private readonly GameCoreContext _context;

    public DraftService(GameCoreContext context) => _context = context;

    public string LastMessage { get; private set; } = "";

    public void PrepareDraftBoard()
    {
        var league = _context.ActiveLeague;
        if (league == null)
            return;

        if (league.Draft?.DraftYear != league.SeasonYear)
            league.Draft = new DraftState { DraftYear = league.SeasonYear };

        EnsureDraftRounds(league, BuildDraftOrder(league));
        ProspectEvaluationService.EnsureEvaluations(league, league.CollegeProspects.Where(prospect => prospect != null && prospect.DraftClassYear == league.SeasonYear + 1));
    }

    private System.Collections.Generic.List<TeamStanding> BuildDraftOrder(LeagueState league)
    {
        return new StandingsService(_context).BuildStandings(league)
            .OrderBy(standing => standing.WinPct)
            .ThenBy(standing => standing.PointDifferential)
            .ThenBy(standing => standing.TeamId, StringComparer.Ordinal)
            .ToList();
    }

    private static void EnsureDraftRounds(LeagueState league, System.Collections.Generic.IReadOnlyList<TeamStanding> standings)
    {
        league.Draft.Picks ??= new System.Collections.Generic.List<DraftPickState>();
        for (var round = 1; round <= DraftRounds; round++)
        {
            for (var index = 0; index < standings.Count; index++)
            {
                var overallPick = ((round - 1) * standings.Count) + index + 1;
                if (league.Draft.Picks.Any(pick => pick != null && pick.OverallPick == overallPick))
                    continue;
                league.Draft.Picks.Add(new DraftPickState
                {
                    OverallPick = overallPick,
                    Round = round,
                    PickInRound = index + 1,
                    TeamId = standings[index].TeamId,
                });
            }
        }
    }

    public DraftPickState GetCurrentPick()
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        return league?.Draft?.Picks?
            .Where(pick => pick != null && string.IsNullOrWhiteSpace(pick.ProspectId))
            .OrderBy(pick => pick.OverallPick)
            .FirstOrDefault();
    }

    public bool AdvanceCpuPicksUntilUserTurn()
    {
        var league = _context.ActiveLeague;
        if (league == null || !string.Equals(league.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
            return false;

        PrepareDraftBoard();
        while (true)
        {
            var pick = GetCurrentPick();
            if (pick == null)
            {
                CompleteDraft(league);
                return true;
            }
            if (string.Equals(pick.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase))
            {
                LastMessage = $"Your pick: round {pick.Round}, pick {pick.PickInRound}.";
                return false;
            }

            var prospect = SelectCpuProspect(league, pick);
            var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, pick.TeamId, StringComparison.OrdinalIgnoreCase));
            var error = "";
            if (prospect == null || team == null || !new TransactionService(_context).DraftRookie(pick, prospect, team, out error))
            {
                LastMessage = string.IsNullOrWhiteSpace(error) ? "CPU draft selection could not be completed." : error;
                return false;
            }
        }
    }

    public bool MakePick(string teamId, string prospectId)
    {
        var league = _context.ActiveLeague;
        if (league == null)
        {
            LastMessage = "No active league loaded.";
            return false;
        }
        if (!string.Equals(league.Calendar?.Phase, ScheduleService.DraftPendingPhase, StringComparison.OrdinalIgnoreCase))
        {
            LastMessage = "Draft picks are available during Draft Pending.";
            return false;
        }

        AdvanceCpuPicksUntilUserTurn();
        var pick = GetCurrentPick();
        if (pick == null || !string.Equals(pick.TeamId, teamId ?? league.UserTeamId, StringComparison.OrdinalIgnoreCase))
        {
            LastMessage = "It is not this team's turn to pick.";
            return false;
        }
        var prospect = league.CollegeProspects.FirstOrDefault(candidate => string.Equals(candidate?.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase));
        var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, pick.TeamId, StringComparison.OrdinalIgnoreCase));
        var error = "";
        if (prospect == null || team == null || !new TransactionService(_context).DraftRookie(pick, prospect, team, out error))
        {
            LastMessage = string.IsNullOrWhiteSpace(error) ? "Draft selection could not be completed." : error;
            return false;
        }

        LastMessage = $"Selected {prospect.Name} in round {pick.Round}, pick {pick.PickInRound}.";
        AdvanceCpuPicksUntilUserTurn();
        return true;
    }

    private static CollegeProspectState SelectCpuProspect(LeagueState league, DraftPickState pick)
    {
        var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, pick.TeamId, StringComparison.OrdinalIgnoreCase));
        return league.CollegeProspects
            .Where(prospect => prospect != null && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId))
            .OrderByDescending(prospect => GetCpuValue(prospect, team))
            .ThenByDescending(prospect => prospect.Potential)
            .ThenBy(prospect => prospect.ProspectId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static int GetCpuValue(CollegeProspectState prospect, TeamState team)
    {
        var positionCount = team?.Roster?.Count(player => string.Equals(player?.Position, prospect.Position, StringComparison.OrdinalIgnoreCase)) ?? 0;
        var needBonus = positionCount switch { 0 => 12, 1 => 8, 2 => 4, _ => 0 };
        return prospect.Overall * 3 + prospect.Potential + needBonus;
    }

    private static void CompleteDraft(LeagueState league)
    {
        league.Draft.IsCompleted = true;
        league.Calendar.AbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.RookieSigningPendingPhase);
        league.Calendar.Week = league.Calendar.AbsoluteWeek;
        league.Calendar.DayIndex = 0;
        ScheduleService.NormalizeCalendar(league.Calendar);
    }
}
