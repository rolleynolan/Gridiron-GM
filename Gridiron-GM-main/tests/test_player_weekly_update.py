import random

from gridiron_gm_pkg.simulation.systems.player.player_weekly_update import advance_player_week
from gridiron_gm_pkg.simulation.systems.player.player_progression import progress_player
from gridiron_gm_pkg.simulation.systems.player.player_regression import apply_regression
from gridiron_gm_pkg.simulation.systems.player.player_dna import (
    GrowthArc,
    DEFAULT_REGRESSION_PROFILE,
)


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
        self.regression_profile = DEFAULT_REGRESSION_PROFILE


def make_player(age=25):
    player = type("P", (), {})()
    player.attributes = DummyAttrs({"speed": 50})
    player.dna = DummyDNA()
    player.hidden_caps = {"speed": 90}
    player.position = "RB"
    player.age = age
    player.get_relevant_attribute_names = lambda: ["speed"]
    return player


def test_advance_player_week_consistency():
    xp = {"speed": 5.0}
    rng1 = random.Random(99)
    p1 = make_player(31)
    advance_player_week(p1, xp, 1.0, rng1)
    result1 = p1.attributes.core["speed"]

    rng2 = random.Random(99)
    p2 = make_player(31)
    progress_player(p2, xp, 1.0, rng2)
    apply_regression(p2, p2.age, rng2)
    result2 = p2.attributes.core["speed"]

    assert result1 == result2
