"""Player attribute regression system."""

from __future__ import annotations

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

# Multipliers applied to base regression rate
decay_multipliers = {
    "physical": 1.0,
    "skill": 0.6,
    "mental": 0.3,
}

# Attributes considered for each position
valid_attributes_by_position: Dict[str, list[str]] = {
    "QB": ["throw_power", "throw_accuracy", "awareness", "iq", "agility", "acceleration", "vision"],
    "RB": ["speed", "acceleration", "agility", "toughness", "awareness", "carrying", "elusiveness", "catching", "stamina"],
    "WR": ["speed", "acceleration", "agility", "catching", "route_running", "awareness", "jumping", "release"],
    "TE": ["strength", "blocking", "lead_blocking", "catching", "route_running", "awareness"],
    "OL": ["strength", "blocking", "lead_blocking", "awareness", "toughness", "footwork_ol"],
    "DL": ["strength", "tackling", "block_shedding", "awareness", "play_recognition", "pursuit_dl"],
    "LB": ["speed", "tackling", "awareness", "play_recognition", "strength", "coverage", "block_shedding"],
    "DB": ["speed", "acceleration", "agility", "awareness", "catching", "coverage", "play_recognition", "jumping"],
    "K": ["kick_power", "kick_accuracy", "awareness"],
    "P": ["punt_power", "punt_accuracy", "awareness"],
}


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
