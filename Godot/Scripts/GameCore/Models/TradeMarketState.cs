using System.Collections.Generic;

namespace GridironGM.GameCore.Models;

public sealed class TradeMarketState
{
    public bool Submitted { get; set; }
    public int SeasonYear { get; set; }
    public string Phase { get; set; } = "";
    public string SubmittedDate { get; set; } = "";
    public List<string> OfferedPlayerIds { get; set; } = new();
    public List<int> OfferedPickOverallNumbers { get; set; } = new();
    public string RequestedPosition { get; set; } = "";
    public List<TradeMarketOfferState> Offers { get; set; } = new();
}

public sealed class TradeMarketOfferState
{
    public string OfferId { get; set; } = "";
    public string PartnerTeamId { get; set; } = "";
    public List<string> PartnerPlayerIds { get; set; } = new();
    public List<int> PartnerPickOverallNumbers { get; set; } = new();
    public int UserPackageValue { get; set; }
    public int PartnerPackageValue { get; set; }
    public string Rationale { get; set; } = "";
    public string Status { get; set; } = "open";
}
