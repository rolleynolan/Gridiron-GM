using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class FrontOfficeEvaluationService
{
    private readonly GameCoreContext _context;

    public FrontOfficeEvaluationService(GameCoreContext context) => _context = context;

    public FrontOfficeEvaluationResponse EvaluateTeam(string teamId)
    {
        var league = _context?.ActiveLeague;
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, teamId, StringComparison.OrdinalIgnoreCase));
        if (league == null || team == null)
            return new FrontOfficeEvaluationResponse { Error = "Team evaluation is unavailable." };

        var roster = team.Roster ?? new();
        var contracts = new ContractService(_context);
        var needs = AssessPositions(team)
            .Where(need => need.Priority > 0)
            .OrderByDescending(need => need.Priority)
            .ThenBy(need => need.Position, StringComparer.Ordinal)
            .Select(need => $"{need.Position} ({need.Reason})")
            .ToList();
        if (roster.Count < RosterService.RosterLimit)
            needs.Add($"Active roster ({RosterService.RosterLimit - roster.Count} open)");

        var averageAge = roster.Count == 0 ? 0 : (int)Math.Round(roster.Average(player => player.Age), MidpointRounding.AwayFromZero);
        var expiring = roster.Count(player => player.Contract?.YearsRemaining == 1);
        var picks = league.Draft?.Picks?.Count(pick => pick != null && string.Equals(pick.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(pick.ProspectId)) ?? 0;
        var capRoom = contracts.GetCapRoom(team);
        var pressure = capRoom < 0m ? "over the cap" : capRoom < 5_000_000m ? "limited cap room" : "cap flexibility";
        var rationale = $"{team.Name} has {pressure}, {expiring} expiring contract(s), an average roster age of {averageAge}, and {picks} available draft pick(s). " +
            (needs.Count == 0 ? "No starter-depth shortages are currently identified." : $"Priority needs: {string.Join(", ", needs)}.");

        return new FrontOfficeEvaluationResponse
        {
            Ok = true,
            TeamId = team.TeamId,
            TeamName = team.Name,
            RosterSize = roster.Count,
            CapRoom = capRoom,
            AverageAge = averageAge,
            ExpiringContracts = expiring,
            DraftPicksAvailable = picks,
            PositionNeeds = needs,
            Rationale = rationale,
        };
    }

    // CPU-only scores stay internal; public rationale describes roster facts without rating values.
    public static List<CpuPositionNeed> AssessPositions(TeamState team)
        => DepthChartRules.RequiredStartersByPosition.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
            {
                var players = team.Roster.Where(player => SamePosition(player.Position, pair.Key)).ToList();
                var healthy = players.Count(PlayerInjuryService.IsAvailableForGame);
                var target = DesiredDepth(pair.Key);
                var aging = players.Count(player => player.Age >= 30 || player.Contract?.YearsRemaining == 1);
                var priority = healthy < pair.Value ? 100 : healthy < target ? 60 : aging >= pair.Value ? 20 : 0;
                return new CpuPositionNeed
                {
                    Position = pair.Key, RequiredStarters = pair.Value, DesiredDepth = target,
                    Rostered = players.Count, Available = healthy, AgingOrExpiring = aging,
                    Priority = priority,
                    Reason = healthy < pair.Value ? "available starter emergency"
                        : healthy < target ? "missing required depth"
                        : aging >= pair.Value ? "aging or expiring group"
                        : players.Count > target ? "excess positional depth" : "developmental depth opportunity",
                };
            }).ToList();

    public static int DesiredDepth(string position) => position?.ToUpperInvariant() switch
    {
        "K" or "P" => 1,
        "WR" => 5,
        "EDGE" or "DT" or "LB" or "CB" => 4,
        "RB" or "S" => 3,
        _ => 2,
    };

    public static int PlayerValue(PlayerState player)
        => player.Overall * 4 + (player.Age <= 25 ? Math.Clamp(player.Potential - player.Overall, 0, 15) : 0)
            - Math.Max(0, player.Age - 30) * 2;

    public static int DraftValue(CollegeProspectState prospect, CpuPositionNeed need)
        => prospect.Overall * 4 + prospect.Potential
            + (need.Available < need.RequiredStarters ? 48 : need.Rostered < need.DesiredDepth ? 20 : need.AgingOrExpiring >= need.RequiredStarters ? 8 : 0)
            - Math.Max(0, need.Rostered - need.DesiredDepth) * 12;

    public static bool CanRemove(TeamState team, PlayerState player, PlayerState incoming = null)
    {
        var remaining = team.Roster.Where(member => member.PlayerId != player.PlayerId).ToList();
        // An existing shortage cannot justify creating another one. Preserve saved healthy starters
        // for discretionary moves; only a better same-position acquisition can replace one.
        return DepthChartRules.RequiredStartersByPosition.All(pair =>
            remaining.Count(member => SamePosition(member.Position, pair.Key) && PlayerInjuryService.IsAvailableForGame(member))
                + (incoming != null && SamePosition(incoming.Position, pair.Key) && string.IsNullOrWhiteSpace(incoming.Injury) && incoming.CurrentInjury?.IsActive != true ? 1 : 0)
            >= Math.Min(pair.Value, team.Roster.Count(member => SamePosition(member.Position, pair.Key) && PlayerInjuryService.IsAvailableForGame(member))));
    }

    public static bool IsProtectedStarter(TeamState team, PlayerState player)
    {
        var order = team.DepthChart.TryGetValue(player.Position, out var ids) ? ids : new List<string>();
        var available = team.Roster.Where(p => SamePosition(p.Position, player.Position) && PlayerInjuryService.IsAvailableForGame(p))
            .OrderBy(p => order.IndexOf(p.PlayerId) < 0 ? int.MaxValue : order.IndexOf(p.PlayerId))
            .ThenByDescending(PlayerValue).ThenBy(p => p.PlayerId, StringComparer.Ordinal);
        return available.Take(DepthChartRules.GetRequiredStarters(player.Position)).Any(p => p.PlayerId == player.PlayerId);
    }

    public static IEnumerable<PlayerState> CutCandidates(TeamState team, PlayerState incoming = null)
        => team.Roster.Where(player => CanRemove(team, player, incoming)
                && (!IsProtectedStarter(team, player) || incoming != null && SamePosition(incoming.Position, player.Position) && PlayerValue(incoming) >= PlayerValue(player) + 16))
            .OrderByDescending(player => team.Roster.Count(p => SamePosition(p.Position, player.Position)) - DesiredDepth(player.Position))
            .ThenBy(PlayerValue).ThenByDescending(player => player.Age).ThenBy(player => player.PlayerId, StringComparer.Ordinal);

    public static bool SamePosition(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    public static bool IsUsefulAcquisition(TeamState team, PlayerState player, CpuPositionNeed need = null)
    {
        if (!string.IsNullOrWhiteSpace(player.Injury) || player.CurrentInjury?.IsActive == true) return false;
        need ??= AssessPositions(team).FirstOrDefault(n => SamePosition(n.Position, player.Position));
        if (need == null) return false;
        if (need.Available < need.DesiredDepth) return true;
        var weakest = team.Roster.Where(p => SamePosition(p.Position, player.Position) && PlayerInjuryService.IsAvailableForGame(p))
            .OrderBy(PlayerValue).ThenBy(p => p.PlayerId, StringComparer.Ordinal).FirstOrDefault();
        return weakest != null && PlayerValue(player) >= PlayerValue(weakest) + 24;
    }

    public static bool IsRecentDeparture(LeagueState league, TransactionRecord transaction)
        => transaction.Type is "player_released" or "player_waived"
            && (transaction.SeasonYear == league.SeasonYear && league.Calendar.AbsoluteWeek - transaction.AbsoluteWeek <= 4
                || transaction.SeasonYear == league.SeasonYear - 1 && transaction.Phase == ScheduleService.TrainingCampPendingPhase && league.Calendar.AbsoluteWeek <= 3);

    public static PlayerState ConditionalRelease(TeamState team, PlayerState incoming, decimal cost, int limit, decimal capRoom)
    {
        var need = AssessPositions(team).First(n => SamePosition(n.Position, incoming.Position));
        var replacing = need.Rostered >= need.DesiredDepth && need.Available >= need.DesiredDepth;
        if (team.Roster.Count < limit && cost <= capRoom && !replacing) return null;
        return CutCandidates(team, incoming).Where(p => !replacing || SamePosition(p.Position, incoming.Position))
            .FirstOrDefault(p => cost <= capRoom + (p.Contract?.AnnualSalary ?? 0m));
    }

    public static bool ConfirmWaiver(LeagueState league, TeamState team, PlayerState player, PlayerState release)
        => !SamePosition(team.TeamId, league.UserTeamId) && IsUsefulAcquisition(team, player)
            && !league.Transactions.Any(t => SamePosition(t.TeamId, team.TeamId) && SamePosition(t.PlayerId, player.PlayerId) && IsRecentDeparture(league, t))
            && (release == null || CanRemove(team, release, player)
                && (!IsProtectedStarter(team, release) || SamePosition(player.Position, release.Position) && PlayerValue(player) >= PlayerValue(release) + 16));

    // Pure proposals: TransactionService still validates each one against current ownership,
    // budget, capacity, phase, and the queue before it records anything.
    public static IEnumerable<(TeamState Team, PlayerState Release)> ProposeWaiverClaims(LeagueState league, WaiverClaimState waiver, ContractService contracts)
    {
        if (waiver?.Player == null || waiver.PendingConfirmation) yield break;
        var slots = Math.Max(0, 4 - waiver.Claims.Count);
        var candidates = league.Teams.Where(team => !SamePosition(team.TeamId, league.UserTeamId) && !SamePosition(team.TeamId, waiver.WaivedByTeamId)
            && !waiver.Claims.Any(claim => SamePosition(claim.TeamId, team.TeamId)) && ConfirmWaiver(league, team, waiver.Player, null))
            .OrderByDescending(team => AssessPositions(team).First(n => SamePosition(n.Position, waiver.Player.Position)).Priority)
            .ThenBy(team => team.TeamId, StringComparer.Ordinal);
        foreach (var team in candidates)
        {
            if (slots == 0) yield break;
            if (league.Waivers.Sum(w => w.Claims.Count(c => SamePosition(c.TeamId, team.TeamId))) >= 2) continue;
            var cost = waiver.Player.Contract?.AnnualSalary ?? 0m;
            var room = contracts.GetCapRoom(team);
            var release = ConditionalRelease(team, waiver.Player, cost, RosterService.RosterLimit, room);
            if (team.Roster.Count - (release == null ? 0 : 1) + 1 > RosterService.RosterLimit || cost > room + (release?.Contract?.AnnualSalary ?? 0m)) continue;
            yield return (team, release);
            slots--;
        }
    }
}

public sealed class CpuPositionNeed
{
    public string Position { get; init; } = "";
    public int RequiredStarters { get; init; }
    public int DesiredDepth { get; init; }
    public int Rostered { get; init; }
    public int Available { get; init; }
    public int AgingOrExpiring { get; init; }
    public int Priority { get; init; }
    public string Reason { get; init; } = "";
}
