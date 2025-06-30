"""Simple helpers for evaluating and accepting trades."""

from typing import Any, Iterable

from .trade_value import evaluate_player_value, calculate_pick_value


def _asset_value(asset: Any, team: Any) -> float:
    if isinstance(asset, str) and "Pick" in asset:
        try:
            round_number = int(asset.split()[0][0])
        except (ValueError, IndexError):
            return 0.0
        return calculate_pick_value(round_number, team)
    if hasattr(asset, "overall"):
        return evaluate_player_value(asset, team)
    return 0.0


def compute_total_trade_value(offer: dict, from_team: Any, to_team: Any) -> tuple[float, float]:
    seller_assets: Iterable[Any] = offer.get("players_from_seller", []) + offer.get("picks_from_seller", [])
    buyer_assets: Iterable[Any] = offer.get("players_from_buyer", []) + offer.get("picks_from_buyer", [])
    seller_value = sum(_asset_value(a, from_team) for a in seller_assets)
    buyer_value = sum(_asset_value(a, to_team) for a in buyer_assets)
    return round(seller_value, 1), round(buyer_value, 1)


def should_accept_trade(seller_value: float, buyer_value: float, max_delta: float = 100.0) -> bool:
    return abs(buyer_value - seller_value) <= max_delta
