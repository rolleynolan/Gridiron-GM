"""Player attribute regression system."""

from __future__ import annotations

<<<<<<< HEAD
from typing import Dict

from .player_dna import MutationType

# === Decay configuration ===
attribute_decay_type = {
    # Physical
    "speed": "physical",
    "acceleration": "physical",
    "agility": "physical",
    "jumping": "physical",
    "strength": "physical",
    "stamina": "physical",
    "toughness": "physical",
    # Skill
    "catching": "skill",
    "tackling": "skill",
    "blocking": "skill",
    "route_running": "skill",
    "throw_power": "skill",
    "throw_accuracy": "skill",
    "lead_blocking": "skill",
    # Mental
    "awareness": "mental",
    "iq": "mental",
    "vision": "mental",
    "play_recognition": "mental",
}
=======
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
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802

# Multipliers applied to base regression rate
decay_multipliers = {
    "physical": 1.0,
    "skill": 0.6,
    "mental": 0.3,
}

# Attributes considered for each position
valid_attributes_by_position: Dict[str, list[str]] = {
    "QB": ["throw_power", "throw_accuracy", "awareness", "iq", "agility", "acceleration", "vision"],
<<<<<<< HEAD
    "RB": ["speed", "acceleration", "agility", "toughness", "awareness", "carrying", "elusiveness", "catching", "stamina"],
    "WR": ["speed", "acceleration", "agility", "catching", "route_running", "awareness", "jumping", "release"],
    "TE": ["strength", "blocking", "lead_blocking", "catching", "route_running", "awareness"],
    "OL": ["strength", "blocking", "lead_blocking", "awareness", "toughness", "footwork_ol"],
    "DL": ["strength", "tackling", "block_shedding", "awareness", "play_recognition", "pursuit_dl"],
    "LB": ["speed", "tackling", "awareness", "play_recognition", "strength", "coverage", "block_shedding"],
    "DB": ["speed", "acceleration", "agility", "awareness", "catching", "coverage", "play_recognition", "jumping"],
=======
    "RB": ["speed", "acceleration", "agility", "toughness", "awareness", "carrying", "elusiveness", "catching", "stamina", "return_skill"],
    "WR": ["speed", "acceleration", "agility", "catching", "route_running", "awareness", "jumping", "release", "return_skill"],
    "TE": ["strength", "blocking", "lead_blocking", "catching", "route_running", "awareness"],
    "OL": ["strength", "blocking", "lead_blocking", "awareness", "toughness", "block_footwork"],
    "DL": ["strength", "tackling", "block_shedding", "awareness", "play_recognition", "pursuit_dl"],
    "LB": ["speed", "tackling", "awareness", "play_recognition", "strength", "coverage", "block_shedding"],
    "DB": ["speed", "acceleration", "agility", "awareness", "catching", "coverage", "play_recognition", "jumping", "return_skill"],
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    "K": ["kick_power", "kick_accuracy", "awareness"],
    "P": ["punt_power", "punt_accuracy", "awareness"],
}


<<<<<<< HEAD
def apply_regression(player) -> None:
    """Apply end-of-season attribute regression to a player."""
    age = getattr(player, "age", 0)
    position = getattr(player, "position", "")
    dna = getattr(player, "dna", None)
    if dna is None:
        return
    profile = getattr(dna, "regression_profile", {})
    mutations = getattr(dna, "mutations", [])

    if age < profile.get("start_age", 30):
        return

    base_rate = profile.get("rate", 0.03)
    position_factor = profile.get("position_modifier", {}).get(position, 1.0)

    attr_container = getattr(player, "attributes", None)
    if attr_container is None:
        return
    core = getattr(attr_container, "core", {})
    pos_attrs = getattr(attr_container, "position_specific", {})

    valid = valid_attributes_by_position.get(position, [])
    containers = [core, pos_attrs]

    for container in containers:
        for attr, value in container.items():
            if attr not in valid or value is None:
                continue
            decay_type = attribute_decay_type.get(attr, "physical")
            decay_mult = decay_multipliers.get(decay_type, 1.0)
            rate = base_rate * position_factor * decay_mult
            if MutationType.BuiltToLast in mutations:
                rate *= 0.5
            new_val = max(0, round(value * (1 - rate), 2))
            container[attr] = new_val
=======
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

    if not hasattr(player, "_regression_buffer"):
        from collections import defaultdict
        player._regression_buffer = defaultdict(float)

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

        weekly_regression = (current * effective_rate * type_modifier) / 52
        noise = rng.uniform(0.85, 1.15)
        regression_value = weekly_regression * noise

        player._regression_buffer[attr] += regression_value
        if player._regression_buffer[attr] >= 1.0:
            loss = int(player._regression_buffer[attr])
            container[attr] = max(40, container[attr] - loss)
            player._regression_buffer[attr] %= 1.0

    return player
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
