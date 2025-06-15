import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_rb_archetype


def test_speed_back():
    attrs = {
        "speed": 98,
        "acceleration": 95,
    }
    assert evaluate_rb_archetype(attrs) == "Speed Back"


def test_power_back():
    attrs = {
        "trucking": 92,
        "strength": 90,
        "break_tackle": 89,
        "carry_security": 85,
        "speed": 72,
    }
    assert evaluate_rb_archetype(attrs) == "Power Back"


def test_receiving_back():
    attrs = {
        "catching": 90,
        "route_running": 88,
        "acceleration": 86,
        "elusiveness": 80,
        "awareness": 82,
    }
    assert evaluate_rb_archetype(attrs) == "Receiving Back"
