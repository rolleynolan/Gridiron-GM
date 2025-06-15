"""DNA-driven weekly player progression system."""

from __future__ import annotations

import random
from typing import Dict

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.weekly_training import (
    PHYSICAL_ATTRIBUTES,
    _get_attr_container,
)

# === Tunable parameters ===
BASE_XP = 0.1  # default weekly XP if none supplied
PHYSICAL_MULTIPLIER = 1.2  # faster change for physical attributes
MENTAL_TECH_MULTIPLIER = 0.5  # slower change for mental/technical attributes
NOISE_RANGE = (-0.02, 0.02)  # random variation added to each gain


def _clamp(value: float, low: float = 40.0, high: float = 99.0) -> float:
    """Clamp ``value`` within ``low``..``high``."""

    return max(low, min(high, value))


def progress_player(
    player: Player,
    xp_gains: Dict[str, float] | None = None,
    coach_quality: float = 1.0,
) -> Player:
    """Update a player's attributes using their DNA growth curve.

    Parameters
    ----------
    player:
        Player instance with ``dna`` and ``attributes`` fields.
    xp_gains:
        Optional mapping of attribute names to weekly XP earned from
        training or in-game performance.

    Returns
    -------
    Player
        The mutated player object.
    """

    xp_gains = xp_gains or {}
    attrs = getattr(player, "attributes", None)
    dna = getattr(player, "dna", None)
    if attrs is None or dna is None:
        return player

    try:
        quality_mod = min(1.2, max(0.8, float(coach_quality)))
    except (TypeError, ValueError):
        quality_mod = 1.0

    dev_speed = getattr(dna, "dev_speed", 1.0)
    year = getattr(player, "experience", 0)
    arc = dna.career_arc
    arc_mult = arc[year] if year < len(arc) else arc[-1]

    hidden_caps = getattr(player, "hidden_caps", {})
    attr_caps = dna.attribute_caps

    for attr in player.get_relevant_attribute_names():
        container, _ = _get_attr_container(player, attr)
        if container is None:
            continue
        current = container.get(attr, 0)
        hard_cap = hidden_caps.get(attr, attr_caps.get(attr, {}).get("hard_cap", 99))
        soft_cap = attr_caps.get(attr, {}).get("soft_cap", hard_cap)
        if current >= hard_cap:
            continue
        gain = xp_gains.get(attr, BASE_XP)
        growth = gain * arc_mult * dev_speed * quality_mod
        if current >= soft_cap:
            growth *= 0.25
        growth *= random.uniform(1.0 + NOISE_RANGE[0], 1.0 + NOISE_RANGE[1])
        container[attr] = round(_clamp(current + growth), 2)

    return player
