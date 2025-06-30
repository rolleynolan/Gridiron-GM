"""Utilities for CPU-controlled free agency behavior."""

import random
from typing import Dict, List, Tuple

from gridiron_gm_pkg.engine.free_agency.contract_offer import ContractOffer

# Simple positional roster targets based on typical NFL rosters
POSITION_TARGETS = {
    "QB": 2,
    "RB": 3,
    "WR": 5,
    "TE": 3,
    "LT": 2,
    "LG": 2,
    "C": 2,
    "RG": 2,
    "RT": 2,
    "DL": 7,
    "LB": 6,
    "CB": 5,
    "S": 3,
    "K": 1,
    "P": 1,
}


def evaluate_team_needs(team) -> Dict[str, float]:
    """Return positional needs with a simple weight."""
    counts: Dict[str, int] = {}
    for p in getattr(team, "roster", []):
        counts[p.position] = counts.get(p.position, 0) + 1
    needs: Dict[str, float] = {}
    for pos, target in POSITION_TARGETS.items():
        have = counts.get(pos, 0)
        if have < target:
            needs[pos] = (target - have) + 1
    return needs


def estimate_player_value(player) -> float:
    """Crude estimation of a player's value."""
    age_factor = max(0, 30 - getattr(player, "age", 30)) / 30
    return player.overall + (age_factor * 10)


def generate_contract_offer(player, team, value_score) -> Dict:
    base_salary = max(0.5, round(value_score * 0.1, 2))
    years = random.randint(1, 3)
    return {
        "years": years,
        "salary_per_year": base_salary,
        "total_value": base_salary * years,
        "rookie": False,
    }
