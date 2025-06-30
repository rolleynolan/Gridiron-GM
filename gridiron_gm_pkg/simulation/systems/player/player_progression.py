"""DNA-driven weekly player progression system."""

from __future__ import annotations

import random
from typing import Dict

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.weekly_training import (
    PHYSICAL_ATTRIBUTES,
<<<<<<< HEAD
=======
    _get_attr_container,
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
)

# === Tunable parameters ===
BASE_XP = 0.1  # default weekly XP if none supplied
PHYSICAL_MULTIPLIER = 1.2  # faster change for physical attributes
MENTAL_TECH_MULTIPLIER = 0.5  # slower change for mental/technical attributes
NOISE_RANGE = (-0.02, 0.02)  # random variation added to each gain


def _clamp(value: float, low: float = 40.0, high: float = 99.0) -> float:
    """Clamp ``value`` within ``low``..``high``."""

    return max(low, min(high, value))


<<<<<<< HEAD
def progress_player(player: Player, xp_gains: Dict[str, float] | None = None) -> Player:
=======
def progress_player(
    player: Player,
    xp_gains: Dict[str, float] | None = None,
    coach_quality: float = 1.0,
    rng: random.Random | None = None,
) -> Player:
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    """Update a player's attributes using their DNA growth curve.

    Parameters
    ----------
    player:
        Player instance with ``dna`` and ``attributes`` fields.
    xp_gains:
        Optional mapping of attribute names to weekly XP earned from
        training or in-game performance.

<<<<<<< HEAD
=======
    rng:
        Optional ``random.Random`` instance used to introduce noise. Providing a
        seeded generator allows deterministic results for testing.

>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    Returns
    -------
    Player
        The mutated player object.
    """

    xp_gains = xp_gains or {}
<<<<<<< HEAD
=======
    rng = rng or random
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    attrs = getattr(player, "attributes", None)
    dna = getattr(player, "dna", None)
    if attrs is None or dna is None:
        return player

<<<<<<< HEAD
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
=======
    try:
        quality_mod = min(1.2, max(0.8, float(coach_quality)))
    except (TypeError, ValueError):
        quality_mod = 1.0

    dev_speed = getattr(dna, "dev_speed", 1.0)

    # Determine where the player is on their growth arc using age rather than
    # years of experience. This allows for more nuanced progression that accounts
    # for early bloomers and late starters.
    age = getattr(player, "age", 0)
    growth_arc = dna.growth_arc

    if age < growth_arc.peak_start_age:
        arc_mult = 0.6  # Early development phase
    elif growth_arc.peak_start_age <= age <= growth_arc.peak_end_age:
        arc_mult = 1.0  # Prime growth window
    elif age < growth_arc.decline_start_age:
        arc_mult = 0.4  # Slowing post-peak
    else:
        arc_mult = 0.1  # Decline phase, growth mostly stalls

    # Clamp multiplier to safe bounds just in case
    arc_mult = max(0.0, min(arc_mult, 1.0))

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
        growth *= rng.uniform(1.0 + NOISE_RANGE[0], 1.0 + NOISE_RANGE[1])
        container[attr] = round(_clamp(current + growth), 2)

>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    return player
