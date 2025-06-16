import random
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.offseason_updates import (
    apply_conditioning_regression,
    scout_reevaluation,
)


def make_player(position="WR", age=30):
    p = Player("Test", position, age, "1990-01-01", "U", "USA", 1, 70)
    p.motivation = 30
    p.durability = 50
    p.resilience = 50
    return p


def test_conditioning_regression_applies():
    rng = random.Random(1)
    player = make_player()
    current = player.speed
    changed = apply_conditioning_regression(player, rng)
    assert changed
    assert player.speed in {current - 1, current - 2}


def test_scout_reevaluation_changes_potential():
    player = make_player()
    player.season_stats = {"2025": {"season_totals": {"receiving_yards": 1300}}}
    before = dict(player.scouted_potential)
    changed = scout_reevaluation(player, scout_quality=1.0, rng=random.Random(2))
    assert changed
    assert player.scouted_potential != before
