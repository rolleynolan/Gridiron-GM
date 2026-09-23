using System;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;
using GridironGM.GameCore.Utilities;

namespace GridironGM.GameCore.Services;

public sealed class DraftService
{
    public const int DraftRounds = 7;
    public const int UserBoardLimit = 100;
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
                    OriginalTeamId = standings[index].TeamId,
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
        if (prospect == null || !IsDraftEligible(league, prospect) || team == null || !new TransactionService(_context).DraftRookie(pick, prospect, team, out error))
        {
            LastMessage = string.IsNullOrWhiteSpace(error) ? "Draft selection could not be completed." : error;
            return false;
        }

        LastMessage = $"Selected {prospect.Name} in round {pick.Round}, pick {pick.PickInRound}.";
        AdvanceCpuPicksUntilUserTurn();
        return true;
    }

    public bool AddToUserBoard(string prospectId)
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        var prospect = league?.CollegeProspects?.FirstOrDefault(candidate => string.Equals(candidate?.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase));
        if (prospect == null || !string.IsNullOrWhiteSpace(prospect.DraftedByTeamId)) { LastMessage = "Select an available prospect first."; return false; }
        league.Draft.UserBoardProspectIds ??= new System.Collections.Generic.List<string>();
        if (league.Draft.UserBoardProspectIds.Contains(prospectId, StringComparer.OrdinalIgnoreCase)) { LastMessage = "Prospect is already on the team board."; return false; }
        if (league.Draft.UserBoardProspectIds.Count >= UserBoardLimit) { LastMessage = $"Team draft board is limited to {UserBoardLimit} players."; return false; }
        league.Draft.UserBoardProspectIds.Add(prospect.ProspectId);
        LastMessage = $"Added {prospect.Name} to the team draft board.";
        return true;
    }

    public bool RemoveFromUserBoard(string prospectId)
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        var removed = league?.Draft?.UserBoardProspectIds?.RemoveAll(id => string.Equals(id, prospectId, StringComparison.OrdinalIgnoreCase)) ?? 0;
        LastMessage = removed > 0 ? "Prospect removed from the team draft board." : "Prospect is not on the team draft board.";
        return removed > 0;
    }

    public bool MoveOnUserBoard(string prospectId, int direction)
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        var board = league?.Draft?.UserBoardProspectIds;
        var index = board?.FindIndex(id => string.Equals(id, prospectId, StringComparison.OrdinalIgnoreCase)) ?? -1;
        if (index < 0) { LastMessage = "Prospect is not on the team draft board."; return false; }
        var target = Math.Clamp(index + Math.Sign(direction), 0, board.Count - 1);
        if (target == index) { LastMessage = "Prospect is already at that end of the board."; return false; }
        (board[index], board[target]) = (board[target], board[index]);
        LastMessage = "Team draft board order updated.";
        return true;
    }

    public bool MoveOnUserBoard(string prospectId, string targetProspectId, bool insertAfter)
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        var board = league?.Draft?.UserBoardProspectIds;
        var sourceIndex = board?.FindIndex(id => string.Equals(id, prospectId, StringComparison.OrdinalIgnoreCase)) ?? -1;
        var targetIndex = board?.FindIndex(id => string.Equals(id, targetProspectId, StringComparison.OrdinalIgnoreCase)) ?? -1;
        if (sourceIndex < 0 || targetIndex < 0) { LastMessage = "Both prospects must be on the team draft board."; return false; }
        if (sourceIndex == targetIndex) { LastMessage = "Choose a different destination on the team draft board."; return false; }

        var movedId = board[sourceIndex];
        board.RemoveAt(sourceIndex);
        targetIndex = board.FindIndex(id => string.Equals(id, targetProspectId, StringComparison.OrdinalIgnoreCase));
        var insertionIndex = Math.Clamp(targetIndex + (insertAfter ? 1 : 0), 0, board.Count);
        board.Insert(insertionIndex, movedId);
        LastMessage = "Team draft board order updated.";
        return true;
    }

    public bool SetUserBoardContext(string prospectId, string tag, string note, string tier = null)
    {
        var league = _context.ActiveLeague;
        PrepareDraftBoard();
        if (league?.Draft?.UserBoardProspectIds?.Contains(prospectId, StringComparer.OrdinalIgnoreCase) != true)
        {
            LastMessage = "Add the prospect to the team draft board before saving private context.";
            return false;
        }
        league.Draft.UserBoardTags ??= new System.Collections.Generic.Dictionary<string, string>();
        league.Draft.UserBoardNotes ??= new System.Collections.Generic.Dictionary<string, string>();
        league.Draft.UserBoardTiers ??= new System.Collections.Generic.Dictionary<string, string>();
        var normalizedTag = (tag ?? "").Trim().ToLowerInvariant();
        if (normalizedTag is not ("target" or "avoid")) normalizedTag = "";
        if (string.IsNullOrWhiteSpace(normalizedTag)) league.Draft.UserBoardTags.Remove(prospectId); else league.Draft.UserBoardTags[prospectId] = normalizedTag;
        var normalizedNote = (note ?? "").Trim();
        if (normalizedNote.Length > 240) normalizedNote = normalizedNote[..240];
        if (string.IsNullOrWhiteSpace(normalizedNote)) league.Draft.UserBoardNotes.Remove(prospectId); else league.Draft.UserBoardNotes[prospectId] = normalizedNote;
        var normalizedTier = (tier ?? "").Trim();
        if (normalizedTier.Length > 32) normalizedTier = normalizedTier[..32];
        if (string.IsNullOrWhiteSpace(normalizedTier)) league.Draft.UserBoardTiers.Remove(prospectId); else league.Draft.UserBoardTiers[prospectId] = normalizedTier;
        LastMessage = "Private team-board context saved.";
        return true;
    }

    public TeamDraftBoardContextDto GetUserBoardNeedContext(string prospectId)
    {
        var league = _context.ActiveLeague;
        var prospect = league?.CollegeProspects?.FirstOrDefault(candidate => string.Equals(candidate?.ProspectId, prospectId, StringComparison.OrdinalIgnoreCase));
        var team = league?.Teams?.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (prospect == null || team == null)
            return null;

        var required = DepthChartRules.GetRequiredStarters(prospect.Position);
        var rostered = team.Roster.Count(player => player != null && string.Equals(player.Position, prospect.Position, StringComparison.OrdinalIgnoreCase));
        var need = rostered <= required ? "High" : rostered == required + 1 ? "Medium" : "Low";
        var rolePath = need == "High" ? "Immediate depth need" : need == "Medium" ? "Thin rotation" : "Established room";
        return new TeamDraftBoardContextDto
        {
            ProspectId = prospect.ProspectId,
            Position = prospect.Position,
            NeedLevel = need,
            RolePath = rolePath,
            RosteredAtPosition = rostered,
            RequiredStarters = required,
            Explanation = $"{prospect.Position}: {rostered} rostered for {required} required starter slot{(required == 1 ? "" : "s")}. {rolePath}; this is roster context, not a hidden-rating or scheme-fit claim.",
        };
    }

    private static CollegeProspectState SelectCpuProspect(LeagueState league, DraftPickState pick)
    {
        var team = league.Teams.FirstOrDefault(candidate => string.Equals(candidate?.TeamId, pick.TeamId, StringComparison.OrdinalIgnoreCase));
        return league.CollegeProspects
            .Where(prospect => prospect != null && string.IsNullOrWhiteSpace(prospect.DraftedByTeamId) && IsDraftEligible(league, prospect))
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

    private static bool IsDraftEligible(LeagueState league, CollegeProspectState prospect)
    {
        if (prospect == null)
            return false;
        var playerId = string.IsNullOrWhiteSpace(prospect.CollegePlayerId) ? prospect.ProspectId : prospect.CollegePlayerId;
        var collegePlayer = league?.CollegeUniverse?.Players?.FirstOrDefault(player => string.Equals(player?.PlayerId, playerId, StringComparison.OrdinalIgnoreCase));
        return collegePlayer?.DraftEligible ?? true; // Retains compatibility for intentionally supported pre-universe saves.
    }

    private static void CompleteDraft(LeagueState league)
    {
        league.Draft.IsCompleted = true;
        UndraftedFreeAgentService.OpenMarket(league);
        league.Calendar.AbsoluteWeek = ScheduleService.GetOffseasonPlaceholderAbsoluteWeek(ScheduleService.RookieSigningPendingPhase);
        league.Calendar.Week = league.Calendar.AbsoluteWeek;
        league.Calendar.DayIndex = 0;
        ScheduleService.NormalizeCalendar(league.Calendar);
    }
}
