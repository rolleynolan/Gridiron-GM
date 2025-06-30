"""Apply DNA mutation effects to player attributes at creation time."""
from __future__ import annotations

import random
from typing import Dict, List

from gridiron_gm_pkg.simulation.entities.player import Player

# Mapping of mutation names to their effects
MUTATION_EFFECTS = {
    "physical_freak": {
        "attributes": ["speed", "acceleration", "agility", "strength"],
        "cap_boost_range": (3, 10),
        "start_boost_range": (2, 8),
    },
    "durable": {
        "attributes": ["toughness"],
        "cap_boost_range": (2, 5),
        "start_boost_range": (2, 5),
        "injury_multiplier": 0.9,
        "recovery_speed_bonus": 0.15,
    },
    "fragile": {
        "attributes": ["toughness"],
        "cap_boost_range": (-5, -2),
        "start_boost_range": (-10, -5),
        "injury_multiplier": 1.25,
    },
}


def apply_mutations(player: Player, attributes: Dict[str, int], caps: Dict[str, int]) -> None:
    """Apply mutation boosts to ``attributes`` and ``caps`` in-place."""
    if not getattr(player, "mutations", None):
        return

    for mutation in player.mutations:
        effects = MUTATION_EFFECTS.get(mutation)
        if not effects:
            continue

        cap_low, cap_high = effects.get("cap_boost_range", (0, 0))
        start_low, start_high = effects.get("start_boost_range", (0, 0))
        for attr in effects.get("attributes", []):
            if attr not in caps:
                continue
            cap_boost = random.randint(cap_low, cap_high)
            caps[attr] = max(0, min(99, caps[attr] + cap_boost))
            start_boost = random.randint(start_low, start_high)
            new_val = attributes.get(attr, 0) + start_boost
            attributes[attr] = max(0, min(caps[attr], new_val))

        # Store any system-level effects on the player's DNA profile
        if hasattr(player, "dna"):
            if "injury_multiplier" in effects:
                base = getattr(player.dna, "injury_multiplier", 1.0)
                player.dna.injury_multiplier = base * effects["injury_multiplier"]
            if "recovery_speed_bonus" in effects:
                base = getattr(player.dna, "recovery_speed_bonus", 0.0)
                player.dna.recovery_speed_bonus = base + effects["recovery_speed_bonus"]

