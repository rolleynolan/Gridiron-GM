using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class DraftPickState
{
    public int OverallPick { get; set; }
    public int Round { get; set; }
    public int PickInRound { get; set; }
    public string TeamId { get; set; } = "";
    // The original club remains immutable while TeamId tracks the current owner.
    public string OriginalTeamId { get; set; } = "";
    public string ProspectId { get; set; } = "";
    public string PlayerId { get; set; } = "";
}

public sealed class DraftState
{
    public int DraftYear { get; set; }
    public bool IsCompleted { get; set; }
    public List<DraftPickState> Picks { get; set; } = new();
    public List<DraftClassRecapEntry> RecapEntries { get; set; } = new();
    public List<string> UserBoardProspectIds { get; set; } = new();
    public Dictionary<string, string> UserBoardNotes { get; set; } = new();
    public Dictionary<string, string> UserBoardTags { get; set; } = new();
    public Dictionary<string, string> UserBoardTiers { get; set; } = new();
    public bool UseShortDraftAnnouncements { get; set; }
}
