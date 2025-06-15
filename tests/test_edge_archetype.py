import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_edge_archetype


def test_speed_rusher():
    attrs = {
        "pass_rush_finesse": 95,
        "speed": 94,
        "acceleration": 92,
        "agility": 90,
        "pursuit_dl": 88,
    }
    assert evaluate_edge_archetype(attrs) == "Speed Rusher"


def test_power_rusher():
    attrs = {
        "pass_rush_power": 95,
        "strength": 94,
        "block_shedding": 92,
        "balance": 90,
        "toughness": 88,
    }
    assert evaluate_edge_archetype(attrs) == "Power Rusher"


def test_edge_setter():
    attrs = {
        "run_defense": 96,
        "block_shedding": 93,
        "strength": 90,
        "tackle_dl": 88,
        "awareness": 85,
    }
    assert evaluate_edge_archetype(attrs) == "Edge Setter"
