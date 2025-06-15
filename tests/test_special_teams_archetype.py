import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_special_teams_archetype


def test_power_kicker():
    attrs = {
        "kick_power": 96,
        "strength": 88,
        "kick_accuracy": 85,
        "discipline": 80,
    }
    assert evaluate_special_teams_archetype(attrs, "K") == "Power Kicker"


def test_coffin_corner_specialist():
    attrs = {
        "punt_accuracy": 95,
        "awareness": 92,
        "discipline": 88,
        "kick_power": 80,
    }
    assert evaluate_special_teams_archetype(attrs, "P") == "Coffin Corner Specialist"


def test_explosive_returner():
    attrs = {
        "return_skill": 96,
        "speed": 94,
        "acceleration": 92,
        "elusiveness": 90,
        "agility": 88,
    }
    assert evaluate_special_teams_archetype(attrs, "RS") == "Explosive Returner"
