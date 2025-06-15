import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_qb_archetype


def test_pocket_passer():
    attrs = {
        "throw_accuracy_short": 90,
        "throw_accuracy_medium": 88,
        "awareness": 85,
        "iq": 90,
        "speed": 60,
        "toughness": 70,
    }
    assert evaluate_qb_archetype(attrs) == "Pocket Passer"


def test_dual_threat():
    attrs = {
        "speed": 92,
        "acceleration": 88,
        "agility": 80,
        "throw_on_run": 85,
        "throw_power": 90,
        "throw_accuracy_deep": 85,
    }
    assert evaluate_qb_archetype(attrs) == "Dual-Threat"


def test_gunslinger():
    attrs = {
        "throw_power": 95,
        "throw_accuracy_deep": 90,
        "throw_on_run": 80,
        "iq": 75,
        "discipline": 60,
    }
    assert evaluate_qb_archetype(attrs) == "Gunslinger"

