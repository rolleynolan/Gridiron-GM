"""Utility functions for player retirement decisions."""
from __future__ import annotations

import random
from typing import Any, Dict

from gridiron_gm_pkg.simulation.entities.player import Player


def evaluate_player_retirement(player: Player, rng: random.Random | None = None) -> bool:
    """Return True if the player decides to retire.

    Parameters
    ----------
    player:
        Player object to evaluate.
    rng:
        Optional random number generator for deterministic testing.
    """
    rng = rng or random

    age = getattr(player, "age", 0)
    overall = getattr(player, "overall", 0)
    position = getattr(player, "position", "").upper()

    years_played = getattr(player, "experience", 0)
    if getattr(player, "rookie_year", None) is not None:
        current_year = getattr(player, "rookie_year", 0) + years_played
        years_played = max(years_played, current_year - player.rookie_year)

    # --- Base chance based on age ---
    chance = 0.0
    if age >= 37:
        chance += 0.6
    elif age >= 34:
        chance += 0.3

    # Position modifiers
    if position in {"RB"}:
        chance += 0.1
    elif position in {"QB"}:
        chance -= 0.1
    elif position in {"K", "P"}:
        chance -= 0.2

    # Career length modifier
    if years_played >= 10:
        chance += (years_played - 9) * 0.05

    # Injury history modifier
    serious_injuries = [inj for inj in getattr(player, "injury_history", []) if getattr(inj, "weeks_out", 0) >= 8]
    if len(serious_injuries) >= 2:
        chance += 0.2
    if player.injuries:
        last_injury = player.injuries[-1]
        if getattr(last_injury, "weeks_out", 0) >= 8:
            chance += 0.15

    # Declining performance
    if overall <= 65 and age >= 32:
        chance += 0.2
    if overall <= 60:
        chance += 0.3

    chance = max(0.0, min(chance, 0.95))
    return rng.random() < chance


def retirement_log_entry(player: Player, team_abbr: str) -> Dict[str, Any]:
    """Return a summary dictionary for archival purposes."""
    years = getattr(player, "experience", 0)
    return {
        "name": player.name,
        "position": player.position,
        "age": player.age,
        "overall": player.overall,
        "team": team_abbr,
        "years": years,
        "career_stats": getattr(player, "career_stats", {}),
    }
