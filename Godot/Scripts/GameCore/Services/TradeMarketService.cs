using System;
using System.Collections.Generic;
using System.Linq;
using GridironGM.GameCore.DTOs;
using GridironGM.GameCore.Models;

namespace GridironGM.GameCore.Services;

public sealed class TradeMarketService
{
    public const int MaxSubmittedAssets = 8;
    private readonly GameCoreContext _context;

    public TradeMarketService(GameCoreContext context) => _context = context;

    public TradeMarketResponse GetState()
    {
        var league = _context.ActiveLeague;
        if (league == null) return new TradeMarketResponse { Ok = false, Error = "No active league loaded." };
        league.TradeMarket ??= new TradeMarketState();
        return Map(league, league.TradeMarket);
    }

    public TradeMarketResponse Submit(IReadOnlyCollection<string> playerIds, IReadOnlyCollection<int> pickNumbers, string requestedPosition = "")
    {
        var league = _context.ActiveLeague;
        if (league == null) return new TradeMarketResponse { Ok = false, Error = "No active league loaded." };
        if (!ContractPhaseRules.CanProposeTrades(league, out var phaseError)) return new TradeMarketResponse { Ok = false, Error = phaseError };
        var players = (playerIds ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var picks = (pickNumbers ?? Array.Empty<int>()).Where(value => value > 0).Distinct().ToList();
        if (players.Count + picks.Count is < 1 or > MaxSubmittedAssets)
            return new TradeMarketResponse { Ok = false, Error = $"Submit between 1 and {MaxSubmittedAssets} owned players and picks." };

        new DraftService(_context).PrepareDraftBoard();
        var user = league.Teams.First(team => string.Equals(team.TeamId, league.UserTeamId, StringComparison.OrdinalIgnoreCase));
        if (players.Any(id => user.Roster.All(player => !string.Equals(player.PlayerId, id, StringComparison.OrdinalIgnoreCase))))
            return new TradeMarketResponse { Ok = false, Error = "Every submitted player must be on your active roster." };
        if (picks.Any(number => league.Draft.Picks.All(pick => pick.OverallPick != number || !string.Equals(pick.TeamId, user.TeamId, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrWhiteSpace(pick.ProspectId))))
            return new TradeMarketResponse { Ok = false, Error = "Every submitted pick must be unused and owned by your team." };

        var state = new TradeMarketState
        {
            Submitted = true,
            SeasonYear = league.SeasonYear,
            Phase = league.Calendar?.Phase ?? "",
            SubmittedDate = league.Calendar?.CurrentDate ?? "",
            OfferedPlayerIds = players,
            OfferedPickOverallNumbers = picks,
            RequestedPosition = NormalizePosition(requestedPosition),
        };
        var transactions = new TransactionService(_context);
        var contracts = new ContractService(_context);
        var candidates = new List<(TradeMarketOfferState Offer, double Ratio)>();
        foreach (var partner in league.Teams.Where(team => team != null && !string.Equals(team.TeamId, user.TeamId, StringComparison.OrdinalIgnoreCase)))
        {
            var report = new FrontOfficeEvaluationService(_context).EvaluateTeam(partner.TeamId);
            var offeredPositions = players.Select(id => user.Roster.First(player => string.Equals(player.PlayerId, id, StringComparison.OrdinalIgnoreCase)).Position).ToList();
            var interest = picks.Count > 0 || offeredPositions.Any(position => report.PositionNeeds.Any(need => PositionsMatch(position, need)));
            if (!interest) continue;

            TradeProposalPreview best = null;
            string bestPlayerId = "";
            int bestPickNumber = 0;
            if (picks.Count > 0 && string.IsNullOrWhiteSpace(state.RequestedPosition))
            {
                foreach (var candidatePick in league.Draft.Picks
                             .Where(pick => pick != null && string.Equals(pick.TeamId, partner.TeamId, StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(pick.ProspectId))
                             .OrderBy(pick => pick.OverallPick))
                {
                    var preview = transactions.PreviewUserTradeProposal(new TradeProposal
                    {
                        ProposingTeamId = user.TeamId,
                        ReceivingTeamId = partner.TeamId,
                        ProposingPlayerIds = players.ToList(),
                        ProposingPickOverallNumbers = picks.ToList(),
                        ReceivingPickOverallNumbers = new List<int> { candidatePick.OverallPick },
                    }, contracts);
                    if (!IsViableMarketPreview(preview)) continue;
                    if (best == null || preview.RequestedValue > best.RequestedValue)
                    {
                        best = preview;
                        bestPickNumber = candidatePick.OverallPick;
                    }
                }
            }

            if (bestPickNumber == 0)
            {
                foreach (var candidate in partner.Roster
                             .Where(player => string.IsNullOrWhiteSpace(state.RequestedPosition) || PositionsMatch(player.Position, state.RequestedPosition))
                             .OrderByDescending(player => player.Overall)
                             .ThenBy(player => player.PlayerId, StringComparer.OrdinalIgnoreCase))
                {
                    var preview = transactions.PreviewUserTradeProposal(new TradeProposal
                    {
                        ProposingTeamId = user.TeamId,
                        ReceivingTeamId = partner.TeamId,
                        ProposingPlayerIds = players.ToList(),
                        ProposingPickOverallNumbers = picks.ToList(),
                        ReceivingPlayerIds = new List<string> { candidate.PlayerId },
                    }, contracts);
                    if (!IsViableMarketPreview(preview)) continue;
                    if (best == null || preview.RequestedValue > best.RequestedValue)
                    {
                        best = preview;
                        bestPlayerId = candidate.PlayerId;
                    }
                }
            }
            if (best == null) continue;
            var ratio = best.OfferedValue <= 0 ? 0 : (double)best.RequestedValue / best.OfferedValue;
            candidates.Add((new TradeMarketOfferState
            {
                OfferId = $"market-{league.SeasonYear}-{user.TeamId}-{partner.TeamId}",
                PartnerTeamId = partner.TeamId,
                PartnerPlayerIds = string.IsNullOrWhiteSpace(bestPlayerId) ? new List<string>() : new List<string> { bestPlayerId },
                PartnerPickOverallNumbers = bestPickNumber > 0 ? new List<int> { bestPickNumber } : new List<int>(),
                UserPackageValue = best.OfferedValue,
                PartnerPackageValue = best.RequestedValue,
                Rationale = bestPickNumber > 0
                    ? $"{partner.Name} offers its unused pick #{bestPickNumber} in response to your draft-capital package. {best.Rationale}"
                    : $"{partner.Name} is willing to discuss this package. {best.Rationale}",
            }, ratio));
        }
        state.Offers = candidates.OrderByDescending(item => item.Ratio).ThenBy(item => item.Offer.PartnerTeamId, StringComparer.OrdinalIgnoreCase).Take(5).Select(item => item.Offer).ToList();
        league.TradeMarket = state;
        return Map(league, state);
    }

    public TradeProposalResult AcceptOffer(string offerId)
    {
        var league = _context.ActiveLeague;
        if (league == null) return TradeFailure("No active league loaded.");
        var state = league.TradeMarket ??= new TradeMarketState();
        var offer = state.Offers.FirstOrDefault(candidate => string.Equals(candidate?.OfferId, offerId, StringComparison.OrdinalIgnoreCase));
        if (offer == null || !string.Equals(offer.Status, "open", StringComparison.OrdinalIgnoreCase))
            return TradeFailure("Choose an open trade-market offer.");

        return new TransactionService(_context).SubmitUserTradeProposal(new TradeProposal
        {
            ProposingTeamId = league.UserTeamId,
            ReceivingTeamId = offer.PartnerTeamId,
            ProposingPlayerIds = state.OfferedPlayerIds.ToList(),
            ProposingPickOverallNumbers = state.OfferedPickOverallNumbers.ToList(),
            ReceivingPlayerIds = offer.PartnerPlayerIds.ToList(),
            ReceivingPickOverallNumbers = offer.PartnerPickOverallNumbers.ToList(),
        }, new ContractService(_context));
    }

    public TradeMarketResponse RejectOffer(string offerId)
    {
        var league = _context.ActiveLeague;
        if (league == null) return new TradeMarketResponse { Ok = false, Error = "No active league loaded." };
        var state = league.TradeMarket ??= new TradeMarketState();
        var offer = state.Offers.FirstOrDefault(candidate => string.Equals(candidate?.OfferId, offerId, StringComparison.OrdinalIgnoreCase));
        if (offer == null || !string.Equals(offer.Status, "open", StringComparison.OrdinalIgnoreCase))
            return new TradeMarketResponse { Ok = false, Error = "Choose an open trade-market offer." };
        offer.Status = "rejected";
        return Map(league, state);
    }

    public TradeMarketResponse Withdraw()
    {
        var league = _context.ActiveLeague;
        if (league == null) return new TradeMarketResponse { Ok = false, Error = "No active league loaded." };
        league.TradeMarket = new TradeMarketState();
        return Map(league, league.TradeMarket);
    }

    private static TradeMarketResponse Map(LeagueState league, TradeMarketState state)
    {
        var user = league.Teams.FirstOrDefault(team => team.TeamId == league.UserTeamId);
        var response = new TradeMarketResponse { Ok = true, Submitted = state.Submitted, SubmittedDate = state.SubmittedDate, RequestedPosition = state.RequestedPosition };
        response.OfferedAssets.AddRange(state.OfferedPlayerIds.Select(id => user?.Roster.FirstOrDefault(player => player.PlayerId == id) is { } player ? $"{player.Position} {player.Name}" : id));
        response.OfferedAssets.AddRange(state.OfferedPickOverallNumbers.Select(number => $"Pick #{number}"));
        foreach (var offer in state.Offers)
        {
            var partner = league.Teams.FirstOrDefault(team => team.TeamId == offer.PartnerTeamId);
            response.Offers.Add(new TradeMarketOfferDto
            {
                OfferId = offer.OfferId,
                PartnerTeamId = offer.PartnerTeamId,
                PartnerTeamName = partner?.Name ?? offer.PartnerTeamId,
                PartnerAssets = offer.PartnerPlayerIds.Select(id => partner?.Roster.FirstOrDefault(player => player.PlayerId == id) is { } player ? $"{player.Position} {player.Name}" : id)
                    .Concat(offer.PartnerPickOverallNumbers.Select(number => $"Pick #{number}"))
                    .ToList(),
                UserPackageValue = offer.UserPackageValue,
                PartnerPackageValue = offer.PartnerPackageValue,
                Rationale = offer.Rationale,
                Status = offer.Status,
            });
        }
        return response;
    }

    private static string NormalizePosition(string position) => string.IsNullOrWhiteSpace(position) || string.Equals(position, "Any", StringComparison.OrdinalIgnoreCase) ? "" : position.Trim().ToUpperInvariant();

    private static bool IsViableMarketPreview(TradeProposalPreview preview)
        => preview?.Ok == true && preview.CanSubmit && preview.RequestedValue > 0 && preview.OfferedValue * 100 >= preview.RequestedValue * 92;

    private static bool PositionsMatch(string playerPosition, string requestedPosition)
    {
        var player = NormalizePosition(playerPosition);
        var requested = NormalizePosition(requestedPosition);
        if (string.IsNullOrWhiteSpace(requested) || string.Equals(player, requested, StringComparison.OrdinalIgnoreCase)) return true;
        return requested switch
        {
            "OL" => player is "LT" or "LG" or "C" or "RG" or "RT" or "OL",
            "DL" => player is "DE" or "DT" or "NT" or "DL",
            "LB" => player is "ILB" or "OLB" or "MLB" or "LB",
            "S" => player is "FS" or "SS" or "S",
            _ => false,
        };
    }

    private static TradeProposalResult TradeFailure(string message)
        => new() { Ok = false, Accepted = false, Message = message, Rationale = message };
}
