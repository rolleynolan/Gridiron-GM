import random

from gridiron_gm_pkg.simulation.systems.player.player_progression import progress_player
from gridiron_gm_pkg.simulation.systems.player.player_dna import GrowthArc


class DummyAttrs:
    def __init__(self, core):
        self.core = core
        self.position_specific = {}


class DummyDNA:
    def __init__(self):
        self.dev_speed = 1.0
        self.career_arc = [1.0]
        self.attribute_caps = {"speed": {"current": 50, "soft_cap": 60, "hard_cap": 90}}
        self.growth_arc = GrowthArc(23, 27, 30)


def make_player():
    player = type("P", (), {})()
    player.attributes = DummyAttrs({"speed": 50})
    player.dna = DummyDNA()
    player.hidden_caps = {"speed": 90}
    player.experience = 0
    player.get_relevant_attribute_names = lambda: ["speed"]
    return player


def test_progress_player_deterministic_rng():
    xp = {"speed": 5.0}
    rng1 = random.Random(42)
    p1 = make_player()
    progress_player(p1, xp, rng=rng1)
    result1 = p1.attributes.core["speed"]

    rng2 = random.Random(42)
    p2 = make_player()
    progress_player(p2, xp, rng=rng2)
    result2 = p2.attributes.core["speed"]

    assert result1 == result2
