import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_wr_archetype


def test_deep_threat():
    attrs = {
        "speed": 97,
        "acceleration": 94,
        "route_running_deep": 92,
        "catching": 85,
        "release": 80,
    }
    assert evaluate_wr_archetype(attrs) == "Deep Threat"


def test_possession_receiver():
    attrs = {
        "catch_in_traffic": 95,
        "route_running_short": 90,
        "awareness": 88,
        "catching": 92,
    }
    assert evaluate_wr_archetype(attrs) == "Possession Receiver"


def test_return_specialist():
    attrs = {
        "return_skill": 96,
        "speed": 93,
        "acceleration": 92,
        "agility": 90,
        "elusiveness": 88,
        "carry_security": 85,
    }
    assert evaluate_wr_archetype(attrs) == "Return Specialist"
