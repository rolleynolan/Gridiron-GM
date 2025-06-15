import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_lb_archetype


def test_field_general():
    attrs = {
        "awareness": 92,
        "play_recognition": 90,
        "tackle_lb": 88,
        "leadership": 85,
        "discipline": 84,
    }
    assert evaluate_lb_archetype(attrs) == "Field General"


def test_coverage_backer():
    attrs = {
        "coverage": 95,
        "awareness": 90,
        "speed": 88,
        "agility": 87,
        "tackle_lb": 80,
    }
    assert evaluate_lb_archetype(attrs) == "Coverage Backer"


def test_thumper():
    attrs = {
        "tackle_lb": 95,
        "hit_power": 94,
        "strength": 92,
        "toughness": 90,
        "awareness": 80,
    }
    assert evaluate_lb_archetype(attrs) == "Thumper"
