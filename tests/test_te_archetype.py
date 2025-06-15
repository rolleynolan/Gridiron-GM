import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_te_archetype


def test_receiving_te():
    attrs = {
        "catching": 95,
        "catch_in_traffic": 92,
        "route_running_short": 88,
        "awareness": 90,
        "release": 85,
    }
    assert evaluate_te_archetype(attrs) == "Receiving TE"


def test_blocking_te():
    attrs = {
        "run_block": 94,
        "lead_blocking": 92,
        "strength": 90,
        "impact_blocking": 88,
        "awareness": 85,
    }
    assert evaluate_te_archetype(attrs) == "Blocking TE"


def test_vertical_threat():
    attrs = {
        "speed": 92,
        "acceleration": 90,
        "catching": 88,
        "route_running_deep": 87,
        "release": 85,
    }
    assert evaluate_te_archetype(attrs) == "Vertical Threat"
