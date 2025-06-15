import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_idl_archetype


def test_run_stuffer():
    attrs = {
        "run_defense": 96,
        "block_shedding": 94,
        "strength": 92,
        "tackle_dl": 90,
        "awareness": 88,
    }
    assert evaluate_idl_archetype(attrs) == "Run Stuffer"


def test_gap_penetrator():
    attrs = {
        "pass_rush_finesse": 95,
        "acceleration": 93,
        "agility": 92,
        "pursuit_dl": 90,
        "awareness": 85,
    }
    assert evaluate_idl_archetype(attrs) == "Gap Penetrator"


def test_power_dt():
    attrs = {
        "pass_rush_power": 96,
        "strength": 94,
        "block_shedding": 92,
        "balance": 90,
        "toughness": 88,
    }
    assert evaluate_idl_archetype(attrs) == "Power DT"
