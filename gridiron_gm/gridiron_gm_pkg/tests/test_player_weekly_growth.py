import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.player_weekly_growth import apply_weekly_growth

class DummyAttributes:
    def __init__(self):
        self.core = {"awareness": 70}
        self.position_specific = {"route_running_short": 72}

class DummyPlayer:
    def __init__(self):
        self.attributes = DummyAttributes()
        self.fatigue = 0.1
        self.snaps = 60


def test_apply_weekly_growth_small_deltas(monkeypatch):
    # Remove randomness for deterministic test results
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.player.player_weekly_growth.random.uniform",
        lambda a, b: 1.0,
    )

    player = DummyPlayer()
    context = {"snaps": 60}

    deltas = apply_weekly_growth(player, context)

    assert set(deltas.keys()) == {"awareness", "route_running_short"}
    assert all(isinstance(v, float) for v in deltas.values())
    assert all(0 < v <= 0.25 for v in deltas.values())
