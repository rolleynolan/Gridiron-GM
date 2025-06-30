import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player import attribute_generator
from gridiron_gm_pkg.simulation.systems.player.mutation_processor import apply_mutations


def test_apply_mutations_increases_caps_and_values():
    player = Player("Mutant", "RB", 22, "2003-01-01", "U", "USA", 1, 70)
    player.mutations = ["physical_freak", "durable"]
    attrs, caps = attribute_generator.generate_attributes_for_position("RB")
    # Add toughness since attribute generator doesn't provide it
    caps["toughness"] = 70
    attrs["toughness"] = 65

    old_speed_cap = caps["speed"]
    old_speed = attrs["speed"]
    old_tough_cap = caps["toughness"]
    old_tough = attrs["toughness"]

    apply_mutations(player, attrs, caps)

    assert caps["speed"] >= old_speed_cap
    assert attrs["speed"] >= old_speed
    assert caps["speed"] <= 99
    assert attrs["speed"] <= caps["speed"]

    assert caps["toughness"] >= old_tough_cap
    assert attrs["toughness"] >= old_tough
    assert player.dna.injury_multiplier < 1.0


def test_fragile_decreases_caps():
    player = Player("Glass", "RB", 22, "2003-01-01", "U", "USA", 2, 70)
    player.mutations = ["fragile"]
    attrs, caps = attribute_generator.generate_attributes_for_position("RB")
    caps["toughness"] = 70
    attrs["toughness"] = 65

    old_cap = caps["toughness"]
    apply_mutations(player, attrs, caps)

    assert caps["toughness"] <= old_cap
    assert player.dna.injury_multiplier > 1.0
