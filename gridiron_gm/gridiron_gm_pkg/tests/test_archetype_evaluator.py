import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_archetype

class DummyPlayer:
    def __init__(self, position):
        self.position = position


def test_franchise_qb():
    player = DummyPlayer("QB")
    stats = {"passing_yards": 4000}
    attrs = {"awareness": 90, "accuracy": 90, "throw_power": 90}
    assert evaluate_archetype(player, stats, attrs) == "Franchise QB"


def test_slot_receiver():
    player = DummyPlayer("WR")
    stats = {"receptions": 60}
    attrs = {"awareness": 82, "speed": 88}
    assert evaluate_archetype(player, stats, attrs) == "Slot Technician"


def test_power_back():
    player = DummyPlayer("RB")
    stats = {"rushing_yards": 900}
    attrs = {"strength": 88, "speed": 80, "awareness": 70}
    assert evaluate_archetype(player, stats, attrs) == "Power Back"


def test_raw_prospect():
    player = DummyPlayer("QB")
    stats = {"passing_yards": 500}
    attrs = {"awareness": 60, "accuracy": 60}
    assert evaluate_archetype(player, stats, attrs) == "Raw Prospect"
