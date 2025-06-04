
# engine/trade/cpu_trade_offers.py

from gridiron_gm.gridiron_gm_pkg.engine.trade.trade_value import evaluate_player_value, calculate_pick_value
from gridiron_gm.gridiron_gm_pkg.engine.trade.trade_decision import compute_total_trade_value, should_accept_trade
import random

def generate_cpu_trade_offers(teams, week_number):
    trade_offers = []

    for seller in teams:
        if getattr(seller, "is_user_controlled", False):
            continue
        if not hasattr(seller, "rebuild_mode") or not seller.rebuild_mode:
            continue

        trade_bait = [
            p for p in seller.players
            if p.age >= 29 and evaluate_player_value(p, seller) > 300
        ]
        if not trade_bait:
            continue

        player_to_offer = random.choice(trade_bait)
        player_value = evaluate_player_value(player_to_offer, seller)

        buyer_candidates = find_best_trade_partners(player_to_offer, seller, teams)
        if not buyer_candidates:
            continue

        buyer = buyer_candidates[0]
        pick_round = pick_round_for_value(player_value)
        pick_label = format_pick_round(pick_round)
        buyer_pick_value = calculate_pick_value(pick_round, buyer, is_future=False)

        offer = {
            "from_team": seller.team_name,
            "to_team": buyer.team_name,
            "players_from_seller": [player_to_offer],
            "players_from_buyer": [],
            "picks_from_seller": [],
            "picks_from_buyer": [pick_label],
        }

        seller_val, buyer_val = compute_total_trade_value(offer, seller, buyer)

        if should_accept_trade(seller_val, buyer_val):
            offer.update({
                "value_from_seller": seller_val,
                "value_from_buyer": buyer_val,
                "value_delta": round(buyer_val - seller_val, 1),
                "status": "ACCEPTED"
            })
            print(f"{seller.team_name} → {buyer.team_name}: Offering {player_to_offer.name} for a {pick_label}. ACCEPTED.")
            trade_offers.append(offer)
        else:
            print(f"{seller.team_name} → {buyer.team_name}: Offering {player_to_offer.name} for a {pick_label}. REJECTED.")

    return trade_offers

def format_pick_round(round_num):
    if round_num == 1:
        return "1st Round Pick"
    elif round_num == 2:
        return "2nd Round Pick"
    elif round_num == 3:
        return "3rd Round Pick"
    else:
        return f"{round_num}th Round Pick"

def find_best_trade_partners(player, seller, all_teams):
    buyers = [
        t for t in all_teams if t != seller and not getattr(t, "rebuild_mode", True)
    ]
    scored_buyers = []

    for team in buyers:
        need_score = evaluate_team_need(team, player.position)
        if need_score > 0:
            scored_buyers.append((team, need_score))

    scored_buyers.sort(key=lambda x: x[1], reverse=True)
    return [entry[0] for entry in scored_buyers]

def evaluate_team_need(team, position):
    position_players = [p for p in team.players if p.position == position]
    if not position_players:
        return 10
    elif all(p.overall < 72 for p in position_players):
        return 7
    elif all(p.overall < 78 for p in position_players):
        return 4
    return 0

def pick_round_for_value(value):
    if value >= 1000:
        return 1
    elif value >= 400:
        return 2
    elif value >= 250:
        return 3
    elif value >= 120:
        return 4
    elif value >= 60:
        return 5
    elif value >= 25:
        return 6
    else:
        return 7
