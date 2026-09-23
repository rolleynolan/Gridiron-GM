using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class StaffChangeResult
{
    public bool Ok { get; set; }
    public string Message { get; set; } = "";
}

// Owns the narrow staff-carousel workflow. Implemented staff effects remain bounded and role-specific.
public sealed class StaffService
{
    public static readonly IReadOnlyList<string> SupportedRoles = new[]
    {
        "Head Coach",
        "Offensive Coordinator",
        "Defensive Coordinator",
        "Special Teams Coordinator",
        "Director of Player Personnel",
        "Medical Director",
        "Strength & Conditioning Coach",
    };

    private readonly GameCoreContext _context;

    public StaffService(GameCoreContext context) => _context = context;

    public StaffChangeResult ReleaseCoach(string teamId, string coachId)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        var coach = team?.Coaches?.FirstOrDefault(item => SameId(item?.CoachId, coachId));
        if (team == null || coach == null)
            return Failure("Team or assigned staff member was not found.");
        if (!CanChangeStaff(league, out var error))
            return Failure(error);

        var priorRole = coach.Role;
        team.Coaches.Remove(coach);
        coach.Role = "Available Staff";
        HeadCoachAuthorityService.Normalize(coach);
        league.AvailableCoaches ??= new List<CoachState>();
        if (!league.AvailableCoaches.Any(item => SameId(item?.CoachId, coach.CoachId)))
            league.AvailableCoaches.Add(coach);
        Record(league, "staff_released", team, coach, $"Released from {priorRole}.");
        return new StaffChangeResult { Ok = true, Message = $"{coach.Name} was released to the staff market." };
    }

    public StaffChangeResult HireCoach(string teamId, string role, string coachId)
    {
        var league = _context.ActiveLeague;
        var team = GameCoreStateHelper.ResolveTeam(league, teamId);
        if (team == null || string.IsNullOrWhiteSpace(role))
            return Failure("Team or staff role was not found.");
        var supportedRole = SupportedRoles.FirstOrDefault(candidate => string.Equals(candidate, role.Trim(), StringComparison.OrdinalIgnoreCase));
        if (supportedRole == null)
            return Failure("That staff role is not supported.");
        if (!CanChangeStaff(league, out var error))
            return Failure(error);
        if (team.Coaches?.Any(item => string.Equals(item?.Role, supportedRole, StringComparison.OrdinalIgnoreCase)) == true)
            return Failure($"{supportedRole} is already filled. Release the current staff member before hiring.");

        var coach = league.AvailableCoaches?.FirstOrDefault(item => SameId(item?.CoachId, coachId));
        if (coach == null)
            return Failure("Selected staff-market candidate was not found.");

        league.AvailableCoaches.Remove(coach);
        coach.Role = supportedRole;
        HeadCoachAuthorityService.Normalize(coach);
        coach.TenureStartSeason = league.SeasonYear;
        team.Coaches ??= new List<CoachState>();
        team.Coaches.Add(coach);
        Record(league, "staff_hired", team, coach, $"Hired as {coach.Role} (overall {coach.Overall}).");
        return new StaffChangeResult { Ok = true, Message = $"{coach.Name} was hired as {coach.Role}." };
    }

    public static bool CanChangeStaff(LeagueState league, out string error)
    {
        if (league == null) { error = "No active league loaded."; return false; }
        if (!string.Equals(ScheduleService.GetOffseasonPhaseKey(league.Calendar?.Phase), ScheduleService.StaffCarouselPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
        {
            error = "Staff changes are available only during the Staff Carousel offseason phase.";
            return false;
        }
        error = "";
        return true;
    }

    public int ProcessOffseasonRetirementsAndCpuReplacements()
    {
        var league = _context.ActiveLeague;
        if (league == null || !string.Equals(ScheduleService.GetOffseasonPhaseKey(league.Calendar?.Phase), ScheduleService.StaffCarouselPendingPhaseKey, StringComparison.OrdinalIgnoreCase))
            return 0;
        var changed = 0;
        foreach (var team in league.Teams.Where(team => team != null))
        {
            foreach (var coach in (team.Coaches ?? new List<CoachState>()).Where(coach => coach != null && coach.Age >= 70).ToList())
            {
                team.Coaches.Remove(coach);
                Record(league, "staff_retired", team, coach, $"Retired after {Math.Max(1, league.SeasonYear - coach.TenureStartSeason + 1)} season(s) in role as {coach.Role}.");
                changed++;
            }
            if (string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var role in RequiredRoles.Where(role => !(team.Coaches ?? new List<CoachState>()).Any(coach => string.Equals(coach.Role, role, StringComparison.OrdinalIgnoreCase))))
            {
                var candidate = (league.AvailableCoaches ?? new List<CoachState>()).OrderByDescending(coach => coach.Overall).ThenBy(coach => coach.CoachId, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (candidate == null) break;
                league.AvailableCoaches.Remove(candidate); candidate.Role = role; HeadCoachAuthorityService.Normalize(candidate); candidate.TenureStartSeason = league.SeasonYear; team.Coaches.Add(candidate);
                Record(league, "staff_hired", team, candidate, $"CPU replacement hired as {role} after a staff retirement."); changed++;
            }
        }
        return changed;
    }

    private static readonly IReadOnlyList<string> RequiredRoles = SupportedRoles;

    private static void Record(LeagueState league, string type, TeamState team, CoachState coach, string details)
    {
        league.Transactions ??= new List<TransactionRecord>();
        league.Transactions.Add(new TransactionRecord
        {
            TransactionId = $"{league.SeasonYear}-{league.Transactions.Count + 1:00000}", SeasonYear = league.SeasonYear,
            DateLabel = league.Calendar?.CurrentDate ?? "", Phase = league.Calendar?.Phase ?? "", Type = type,
            TeamId = team.TeamId, TeamName = team.Name, StaffId = coach.CoachId, StaffName = coach.Name, Details = details,
        });
    }

    private static bool SameId(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static StaffChangeResult Failure(string message) => new() { Ok = false, Message = message };
}
