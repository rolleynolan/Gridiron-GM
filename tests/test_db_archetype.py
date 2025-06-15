import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_db_archetype


def test_man_cover_corner():
    attrs = {
        "man_coverage": 95,
        "speed": 94,
        "agility": 92,
        "acceleration": 90,
        "awareness": 85,
    }
    assert evaluate_db_archetype(attrs, "CB") == "Man Cover Corner"


def test_zone_corner():
    attrs = {
        "zone_coverage": 96,
        "awareness": 94,
        "play_recognition": 92,
        "catching": 85,
        "tackling": 80,
    }
    assert evaluate_db_archetype(attrs, "CB") == "Zone Corner"


def test_center_fielder():
    attrs = {
        "zone_coverage": 95,
        "speed": 92,
        "awareness": 90,
        "catching": 88,
    }
    assert evaluate_db_archetype(attrs, "S") == "Center Fielder"
