import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.archetype_evaluator import evaluate_ol_archetype


def test_pass_protector():
    attrs = {
        "pass_block": 95,
        "footwork_ol": 93,
        "awareness": 90,
        "discipline": 88,
        "impact_blocking": 85,
    }
    assert evaluate_ol_archetype(attrs) == "Pass Protector"


def test_run_blocker():
    attrs = {
        "run_block": 96,
        "lead_blocking": 95,
        "impact_blocking": 92,
        "footwork_ol": 85,
        "strength": 88,
    }
    assert evaluate_ol_archetype(attrs) == "Run Blocker"


def test_anchor():
    attrs = {
        "block_shed_resistance": 95,
        "pass_block": 92,
        "strength": 94,
        "awareness": 88,
        "discipline": 86,
    }
    assert evaluate_ol_archetype(attrs) == "Anchor"
