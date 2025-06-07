import random
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.players.player_dna import PlayerDNA
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player


def test_player_has_dna_on_creation():
    p = Player("DNA Guy", "QB", 22, "2003-01-01", "U", "USA", 12, 70)
    assert p.dna is not None
    assert p.hidden_caps


def test_mutation_application():
    dna = PlayerDNA()
    dna.mutation = "Physical Freak"
    caps = {"speed": 90, "strength": 80}
    boosted = dna.apply_mutation_effects(caps)
    assert boosted["speed"] >= 90
    assert boosted["strength"] >= 80
