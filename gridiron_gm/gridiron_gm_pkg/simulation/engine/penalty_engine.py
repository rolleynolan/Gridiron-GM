from dataclasses import dataclass
import random
from typing import List
from gridiron_gm import VERBOSE_SIM_OUTPUT

@dataclass
class Player:
    name: str
    position: str
    discipline_rating: int  # 0–100 scale
    traits: List[str]  # e.g., ["Hot-Headed", "Disciplined"]

# Penalty types and base frequencies (tuned for NFL-like rates, slightly increased for tuning)
PENALTIES = {
    "False Start": {
        # OL: much higher chance, others lower
        "base_chance_by_pos": {
            "LT": 0.013, "LG": 0.013, "C": 0.013, "RG": 0.013, "RT": 0.013,  # OL: +30% (was 0.01)
            "TE": 0.0052, "WR": 0.0039, "RB": 0.00325, "FB": 0.00325, "QB": 0.0013  # +30% (was 0.004, 0.003, etc)
        },
        "positions": ["QB", "RB", "WR", "FB", "LT", "LG", "C", "RG", "RT", "TE"],
        "yards": -7  # was -5, increase penalty yards by 2
    },
    "Holding": {
        "base_chance": 0.0052,  # +30% (was 0.004)
        "positions": ["LT", "LG", "C", "RG", "RT", "TE", "FB"],
        "yards": -12  # was -10, increase penalty yards by 2
    },
    "Pass Interference": {
        "base_chance": 0.0033,  # +32% (was 0.0025)
        "positions": ["CB", "S", "LB"],
        "yards": -18  # was -15, increase penalty yards by 3
    },
    "Offside": {
        "base_chance": 0.0023,  # +28% (was 0.0018)
        "positions": ["DE", "DT", "LB"],
        "yards": -7  # was -5, increase penalty yards by 2
    },
}

# Minimum penalty chance floor (to avoid zero-penalty games)
MIN_PENALTY_CHANCE = 0.0013  # +30% (was 0.001)

def simulate_penalty(player: Player, discipline_modifier: float = 0.0) -> str | None:
    for penalty, data in PENALTIES.items():
        if player.position not in data["positions"]:
            continue
        # Special handling for False Start: OL much higher chance
        if penalty == "False Start":
            base_chance = data["base_chance_by_pos"].get(player.position, MIN_PENALTY_CHANCE)
        else:
            base_chance = max(data.get("base_chance", MIN_PENALTY_CHANCE), MIN_PENALTY_CHANCE)
        trait_modifier = -0.01 if "Disciplined" in player.traits else 0.01 if "Hot-Headed" in player.traits else 0
        discipline_penalty = (50 - player.discipline_rating) / 500
        discipline_penalty = max(discipline_penalty, -0.04)
        final_chance = max(base_chance + trait_modifier + discipline_penalty + discipline_modifier, MIN_PENALTY_CHANCE)
        roll = random.random()
        if roll < final_chance:
            if VERBOSE_SIM_OUTPUT:
                print(f"[DEBUG]     PENALTY TRIGGERED: {penalty} for {player.name} (chance={final_chance:.3f}, roll={roll:.3f})")
            return penalty
    
    return None

def simulate_play(
    players: List[Player],
    discipline_modifier: float = 0.0,
    offense=None,
    defense=None,
    down=None,
    to_go=None,
    yardline=None,
    context=None,
    **kwargs
) -> list:
    results = []
    offense_players = set()
    if offense and hasattr(offense, "roster"):
        offense_players = set(getattr(p, "name", None) for p in offense.roster)
    elif offense and isinstance(offense, list):
        offense_players = set(getattr(p, "name", None) for p in offense)

    # Penalty checks for every player on the field, every play
    for player in players:
        penalty = simulate_penalty(player, discipline_modifier)
        if penalty:
            penalty_data = PENALTIES[penalty]
            yards = penalty_data["yards"]
            if penalty == "False Start":
                replay_down = True
                auto_first_down = False
            elif penalty == "Holding":
                replay_down = True
                auto_first_down = False
            elif penalty == "Pass Interference":
                replay_down = False
                auto_first_down = True
            elif penalty == "Offside":
                replay_down = True
                auto_first_down = False
            else:
                replay_down = False
                auto_first_down = False

            team = "offense" if getattr(player, "name", None) in offense_players else "defense"
            if VERBOSE_SIM_OUTPUT:
                print(f"[DEBUG] Penalty result: {penalty}, team={team}, yards={yards}, auto_first_down={auto_first_down}, replay_down={replay_down}")

            results.append({
                "type": penalty,
                "player": player,
                "team": team,
                "yards": yards,
                "auto_first_down": auto_first_down,
                "replay_down": replay_down
            })
        else:
            pass
    if VERBOSE_SIM_OUTPUT:
        print(f"[DEBUG] simulate_play: Results: {results}")
    return results

@dataclass
class DriveState:
    down: int = 1
    yards_to_go: int = 10
    yard_line: int = 25  # Start at own 25
    result: str = "In Progress"

def simulate_drive(players: List[Player], discipline_modifier: float = 0.0, max_plays: int = 8) -> dict:
    if VERBOSE_SIM_OUTPUT:
        print(f"[DEBUG] simulate_drive: Starting drive with {len(players)} players")
    state = DriveState()
    penalty_log = []
    total_penalties = 0
    total_penalty_yards = 0

    for play_num in range(1, max_plays + 1):
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Play {play_num} -------------------")
        penalties = simulate_play(players, discipline_modifier)
        for penalty in penalties:
            penalty_log.append(f"Play {play_num}: {penalty}")
            state.yard_line = max(1, state.yard_line + penalty["yards"])
            total_penalties += 1
            total_penalty_yards += abs(penalty["yards"])
            if penalty["replay_down"]:
                state.down = min(4, state.down + 1)
                state.yards_to_go += abs(penalty["yards"])
            if penalty["auto_first_down"]:
                state.down = 1
                state.yards_to_go = 10

        gain = random.randint(5, 15)
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Play {play_num} gain: {gain}")
        state.yard_line += gain
        state.yards_to_go -= gain

        if state.yards_to_go <= 0:
            state.down = 1
            state.yards_to_go = 10
        else:
            state.down += 1

        if state.down > 4:
            state.result = "Punt"
            if VERBOSE_SIM_OUTPUT:
                print(f"[DEBUG] Drive ends: Punt")
            break

        if state.yard_line >= 100:
            state.result = "Touchdown"
            if VERBOSE_SIM_OUTPUT:
                print(f"[DEBUG] Drive ends: Touchdown")
            break

    if state.result == "In Progress":
        state.result = "Field Goal Attempt" if state.yard_line >= 70 else "Punt"
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Drive ends: {state.result}")

    if VERBOSE_SIM_OUTPUT:
        print(f"[DEBUG] Final drive state: {state}")
        print(f"[DEBUG] Penalty log: {penalty_log}")
        print(f"[DEBUG] Total penalties: {total_penalties}, Total penalty yards: {total_penalty_yards}")
    return {
        "Result": state.result,
        "Final Yard Line": state.yard_line,
        "Penalties": penalty_log,
        "Total Penalties": total_penalties,
        "Total Penalty Yards": total_penalty_yards
    }

# --- Test/Validation Utility ---
def simulate_n_games(n_games: int, players: List[Player], discipline_modifier: float = 0.0):
    total_penalties = 0
    total_penalty_yards = 0
    for _ in range(n_games):
        result = simulate_drive(players, discipline_modifier)
        total_penalties += result["Total Penalties"]
        total_penalty_yards += result["Total Penalty Yards"]
    avg_penalties = total_penalties / n_games
    avg_penalty_yards = total_penalty_yards / n_games
    print(f"\n[STATS] Average penalties per team per game: {avg_penalties:.2f}")
    print(f"[STATS] Average penalty yards per team per game: {avg_penalty_yards:.2f}")
    return avg_penalties, avg_penalty_yards
