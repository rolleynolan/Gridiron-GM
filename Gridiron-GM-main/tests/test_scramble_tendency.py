import random
from gridiron_gm_pkg.simulation.systems.player import player_generation


def test_calculate_scramble_tendency_ranges():
    random.seed(0)
    high = player_generation.calculate_scramble_tendency(
        {"speed": 95, "acceleration": 90, "agility": 90}
    )
    assert 80 <= high <= 99

    random.seed(1)
    mid_high = player_generation.calculate_scramble_tendency(
        {"speed": 85, "acceleration": 83, "agility": 80}
    )
    assert 65 <= mid_high <= 85

    random.seed(2)
    mid = player_generation.calculate_scramble_tendency(
        {"speed": 75, "acceleration": 72, "agility": 70}
    )
    assert 45 <= mid <= 65

    random.seed(3)
    low = player_generation.calculate_scramble_tendency(
        {"speed": 60, "acceleration": 60, "agility": 60}
    )
    assert 20 <= low <= 45
