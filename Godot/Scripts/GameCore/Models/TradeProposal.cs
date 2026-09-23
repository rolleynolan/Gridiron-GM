using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class TradeProposal
{
    public string ProposingTeamId { get; set; } = "";
    public string ReceivingTeamId { get; set; } = "";
    public List<string> ProposingPlayerIds { get; set; } = new();
    public List<int> ProposingPickOverallNumbers { get; set; } = new();
    public List<string> ReceivingPlayerIds { get; set; } = new();
    public List<int> ReceivingPickOverallNumbers { get; set; } = new();
}

public sealed class TradeProposalResult
{
    public bool Ok { get; set; }
    public bool Accepted { get; set; }
    public string Message { get; set; } = "";
    public string Rationale { get; set; } = "";
    public int OfferedValue { get; set; }
    public int RequestedValue { get; set; }
}

public sealed class TradeProposalPreview
{
    public bool Ok { get; set; }
    public bool CanSubmit { get; set; }
    public string Message { get; set; } = "";
    public string Rationale { get; set; } = "";
    public int OfferedValue { get; set; }
    public int RequestedValue { get; set; }
    public int ProposerRosterAfter { get; set; }
    public int ReceiverRosterAfter { get; set; }
    public decimal ProposerCapRoomAfter { get; set; }
    public decimal ReceiverCapRoomAfter { get; set; }
}
