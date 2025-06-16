"""Player attribute regression system."""

from __future__ import annotations

import random
from typing import Dict

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.player_dna import (
    ATTRIBUTE_DECAY_TYPE,
    MutationType,
)
from gridiron_gm_pkg.simulation.systems.player.weekly_training import (
    _get_attr_container,
)

# === Decay configuration ===
# ATTRIBUTE_DECAY_TYPE from ``player_dna`` maps attributes to physical, skill
# or mental categories. It is imported rather than duplicated here so tests and
# gameplay systems remain synchronized.

# Multipliers applied to base regression rate
decay_multipliers = {
    "physical": 1.0,
    "skill": 0.6,
    "mental": 0.3,
}

# Attributes considered for each position
valid_attributes_by_position: Dict[str, list[str]] = {
    "QB": ["throw_power", "throw_accuracy", "awareness", "iq", "agility", "acceleration", "vision"],
    "RB": ["speed", "acceleration", "agility", "toughness", "awareness", "carrying", "elusiveness", "catching", "stamina", "return_skill"],
    "WR": ["speed", "acceleration", "agility", "catching", "route_running", "awareness", "jumping", "release", "return_skill"],
    "TE": ["strength", "blocking", "lead_blocking", "catching", "route_running", "awareness"],
    "OL": ["strength", "blocking", "lead_blocking", "awareness", "toughness", "footwork_ol"],
    "DL": ["strength", "tackling", "block_shedding", "awareness", "play_recognition", "pursuit_dl"],
    "LB": ["speed", "tackling", "awareness", "play_recognition", "strength", "coverage", "block_shedding"],
    "DB": ["speed", "acceleration", "agility", "awareness", "catching", "coverage", "play_recognition", "jumping", "return_skill"],
    "K": ["kick_power", "kick_accuracy", "awareness"],
    "P": ["punt_power", "punt_accuracy", "awareness"],
}


def apply_regression(
    player: Player,
    age: int | None = None,
    rng: random.Random | None = None,
) -> Player:
    """Regress player attributes based on age and DNA settings.

    Parameters
    ----------
    player:
        Player object to mutate.
    age:
        Age used for regression; defaults to ``player.age`` if omitted.
    rng:
        Optional random generator for deterministic tests.

    Returns
    -------
    Player
        The mutated ``player`` instance.
    """

    dna = getattr(player, "dna", None)
    attrs = getattr(player, "attributes", None)
    if dna is None or attrs is None:
        return player

    age = age if age is not None else getattr(player, "age", 0)

    if age < dna.growth_arc.decline_start_age:
        return player  # Not regressing yet

    rng = rng or random
    caps = dna.attribute_caps
    profile = dna.regression_profile
    regression_rate = profile.get("rate", 0.04)
    position_modifier = profile.get("position_modifier", {}).get(player.position, 1.0)
    effective_rate = regression_rate * position_modifier

    mutations = set(
        m.name if hasattr(m, "name") else str(m) for m in getattr(dna, "mutations", [])
    )
    if "BuiltToLast" in mutations:
        effective_rate *= 0.5
    if "EnduranceEngine" in mutations:
        effective_rate *= 0.75

    attr_map = ATTRIBUTE_DECAY_TYPE

    for attr in player.get_relevant_attribute_names():
        container, _ = _get_attr_container(player, attr)
        if container is None:
            continue

        current = container.get(attr, 0)
        if current <= 40:
            continue  # Floor prevents unrealistic decline

        decay_type = attr_map.get(attr, "skill")
        type_modifier = {
            "physical": 1.2,
            "skill": 1.0,
            "mental": 0.5,
        }.get(decay_type, 1.0)

        regression_amount = current * effective_rate * type_modifier
        noise = rng.uniform(0.85, 1.15)
        total_loss = int(regression_amount * noise)

        container[attr] = max(40, current - total_loss)

    return player
