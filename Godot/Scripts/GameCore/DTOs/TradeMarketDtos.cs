using System.Collections.Generic;

namespace GridironGM.GameCore.DTOs;

public sealed class TradeMarketResponse
{
    public bool Ok { get; set; }
    public string Error { get; set; } = "";
    public bool Submitted { get; set; }
    public string SubmittedDate { get; set; } = "";
    public string RequestedPosition { get; set; } = "";
    public List<string> OfferedAssets { get; set; } = new();
    public List<TradeMarketOfferDto> Offers { get; set; } = new();
}

public sealed class TradeMarketOfferDto
{
    public string OfferId { get; set; } = "";
    public string PartnerTeamId { get; set; } = "";
    public string PartnerTeamName { get; set; } = "";
    public List<string> PartnerAssets { get; set; } = new();
    public int UserPackageValue { get; set; }
    public int PartnerPackageValue { get; set; }
    public string Rationale { get; set; } = "";
    public string Status { get; set; } = "open";
}
