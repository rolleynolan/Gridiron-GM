"""Offseason conditioning regression and scout reevaluation utilities."""

from __future__ import annotations

import random
from typing import Any

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.scouted_potential_reducer import (
    reevaluate_scouted_potential,
)

PHYSICAL_ATTRS = ["speed", "acceleration", "stamina", "strength"]
LINEMAN_POS = {"LT", "LG", "C", "RG", "RT", "OL", "DL", "DE", "EDGE"}


def apply_conditioning_regression(player: Player, rng: random.Random | None = None) -> bool:
    """Apply minor offseason physical regression when warranted.

    Returns True if any attribute was reduced.
    """
    rng = rng or random
    attrs = getattr(player, "attributes", None)
    if attrs is None:
        return False

    age = getattr(player, "age", 0)
    position = getattr(player, "position", "").upper()
    arc = getattr(getattr(player, "dna", None), "growth_arc", None)
    decline_age = getattr(arc, "decline_start_age", 30)

    # Determine baseline age threshold by position
    if position in LINEMAN_POS:
        age_threshold = max(decline_age, 32)
    else:
        age_threshold = max(decline_age, 29)

    motivation = getattr(player, "motivation", 50) or 50
    durability = getattr(player, "durability", 70) or 70
    resilience = getattr(player, "resilience", 70) or 70

    recent_injury = False
    history = getattr(player, "injury_history", [])
    if history:
        last = history[-1]
        sev = str(getattr(last, "severity", "")).lower()
        if sev in {"major", "severe", "moderate"}:
            recent_injury = True

    candidate = False
    if age >= age_threshold:
        candidate = True
    if motivation < 40 or durability < 60 or resilience < 60:
        candidate = True
    if recent_injury:
        candidate = True

    if not candidate:
        return False

    # Probability scaled by how far past decline age
    base_chance = 0.3 + max(0, age - age_threshold) * 0.05
    if rng.random() > min(base_chance, 0.8):
        return False

    attr_name = rng.choice(PHYSICAL_ATTRS)
    container = attrs.core
    cur = container.get(attr_name, 0)
    loss = rng.randint(1, 2)
    new_val = max(40, cur - loss)
    container[attr_name] = new_val
    return new_val != cur


def scout_reevaluation(player: Player, scout_quality: float = 0.6, bias: float = 0.0,
                       rng: random.Random | None = None) -> bool:
    """Reevaluate a player's scouted potential based on the recent season.

    Scout quality controls randomness (0-1). Bias shifts evaluations up or down.
    Returns True if any perceived values changed.
    """
    rng = rng or random
    old = dict(getattr(player, "scouted_potential", {}))

    # Determine if player had a strong season using simple heuristics
    totals = {}
    stats = getattr(player, "season_stats", {})
    if isinstance(stats, dict) and stats:
        last_year = sorted(stats.keys())[-1]
        totals = stats[last_year].get("season_totals", {})
    position = getattr(player, "position", "").upper()
    strong = False
    if position == "WR":
        strong = totals.get("receiving_yards", 0) >= 1200
    elif position == "RB":
        strong = totals.get("rushing_yards", 0) >= 1200
    elif position == "QB":
        strong = totals.get("passing_yards", 0) >= 4000
    elif position in {"DL", "DE", "EDGE"}:
        strong = totals.get("sacks", 0) >= 10
    elif position in {"LB"}:
        strong = totals.get("total_tackles", 0) >= 120

    player.strong_season = strong
    all_attrs = player.get_all_attributes()
    current_attrs = {k: v for k, v in all_attrs.items() if v is not None}
    reevaluate_scouted_potential(player, current_attrs)

    # Apply scout accuracy noise and bias
    for attr, val in player.scouted_potential.items():
        cap = player.hidden_caps.get(attr, 99)
        noise_range = int(round((1.0 - scout_quality) * 2))
        noise = rng.randint(-noise_range, noise_range) if noise_range > 0 else 0
        val = max(current_attrs.get(attr, val), min(cap, val + noise + int(bias)))
        player.scouted_potential[attr] = val

    changed = player.scouted_potential != old
    player.strong_season = False
    return changed
