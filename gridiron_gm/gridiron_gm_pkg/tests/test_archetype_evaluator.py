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
    attrs = {"awareness": 90, "throw_accuracy_short": 90, "throw_power": 90}
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
    attrs = {"awareness": 60, "throw_accuracy_short": 60}
    assert evaluate_archetype(player, stats, attrs) == "Raw Prospect"


def test_wr_archetype_variation():
    player = DummyPlayer("WR")
    stats = {"receiving_yards": 1200, "touchdowns": 9, "receptions": 85}
    attrs = {"route_running_short": 88, "catching": 85, "speed": 92}
    arch = evaluate_archetype(player, stats, attrs)
    assert arch in ("Feature WR", "Slot Technician")

    stats_alt = {"receiving_yards": 200, "touchdowns": 1, "receptions": 10}
    attrs_alt = {"route_running_short": 40, "catching": 50, "speed": 70, "awareness": 40}
    arch_alt = evaluate_archetype(player, stats_alt, attrs_alt)
    assert arch != arch_alt
