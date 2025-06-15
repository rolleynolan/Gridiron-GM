import re
from gridiron_gm.gridiron_gm_pkg.engine.trade.trade_value import evaluate_player_value, calculate_pick_value

def calculate_asset_value(asset, team):
    if isinstance(asset, str) and "Pick" in asset:
        # Extract the round number using regex
        match = re.match(r"(\d+)(st|nd|rd|th) Round Pick", asset)
        if match:
            round_number = int(match.group(1))
            return calculate_pick_value(round_number, team)
        else:
            return 0
    elif hasattr(asset, "overall"):
        return evaluate_player_value(asset, team)
    return 0

def compute_total_trade_value(offer, from_team, to_team):
    seller_assets = offer.get("players_from_seller", []) + offer.get("picks_from_seller", [])
    buyer_assets = offer.get("players_from_buyer", []) + offer.get("picks_from_buyer", [])

    seller_value = sum([calculate_asset_value(asset, from_team) for asset in seller_assets])
    buyer_value = sum([calculate_asset_value(asset, to_team) for asset in buyer_assets])

    return round(seller_value, 1), round(buyer_value, 1)

def should_accept_trade(seller_value, buyer_value, max_delta=100):
    delta = buyer_value - seller_value
    return abs(delta) <= max_delta
