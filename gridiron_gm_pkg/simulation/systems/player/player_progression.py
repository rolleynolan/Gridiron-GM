"""DNA-driven weekly player progression system."""

from __future__ import annotations

import random
from typing import Dict

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.weekly_training import (
    PHYSICAL_ATTRIBUTES,
)

# === Tunable parameters ===
BASE_XP = 0.1  # default weekly XP if none supplied
PHYSICAL_MULTIPLIER = 1.2  # faster change for physical attributes
MENTAL_TECH_MULTIPLIER = 0.5  # slower change for mental/technical attributes
NOISE_RANGE = (-0.02, 0.02)  # random variation added to each gain


def _clamp(value: float, low: float = 40.0, high: float = 99.0) -> float:
    """Clamp ``value`` within ``low``..``high``."""

    return max(low, min(high, value))


def progress_player(player: Player, xp_gains: Dict[str, float] | None = None) -> Player:
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

    dev_speed = getattr(dna, "development_speed", 1.0)
    age = getattr(player, "age", 25)
    age_mult = dna.growth_curve.get(age, 1.0)
    if age_mult < 0:
        age_mult *= getattr(dna, "regression_rate", 1.0)

    def _apply(container: Dict[str, float]) -> None:
        for attr, value in container.items():
            base_gain = xp_gains.get(attr, BASE_XP)
            net = base_gain * age_mult * dev_speed
            if attr in PHYSICAL_ATTRIBUTES:
                net *= PHYSICAL_MULTIPLIER
            else:
                net *= MENTAL_TECH_MULTIPLIER
            net *= random.uniform(1.0 + NOISE_RANGE[0], 1.0 + NOISE_RANGE[1])
            container[attr] = round(_clamp(value + net), 2)

    _apply(getattr(attrs, "core", {}))
    _apply(getattr(attrs, "position_specific", {}))
    return player
