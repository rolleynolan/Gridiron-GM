from pathlib import Path
import csv
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA, MutationType
from gridiron_gm_pkg.simulation.systems.player.player_regression import (
    apply_regression,
    valid_attributes_by_position,
)
from gridiron_gm_pkg.simulation.entities.player import Player


def test_player_has_dna_on_creation():
    p = Player("DNA Guy", "QB", 22, "2003-01-01", "U", "USA", 12, 70)
    assert p.dna is not None
    assert p.hidden_caps
    assert p.scouted_potential


def test_mutation_application():
    dna = PlayerDNA()
    dna.dev_speed = 0.5
    dna.attribute_caps = {
        "speed": {
            "current": 80,
            "soft_cap": 85,
            "hard_cap": 95,
            "breakout_history": [],
        }
    }
    # Baseline growth
    original = dna.attribute_caps["speed"]["current"]
    dna.apply_weekly_growth()
    base_growth = dna.attribute_caps["speed"]["current"] - original

    # Reset and apply mutation
    dna.attribute_caps["speed"]["current"] = original
    dna.mutations = [MutationType.PhysicalFreak]
    dna.apply_weekly_growth()
    boosted_growth = dna.attribute_caps["speed"]["current"] - original

    assert boosted_growth > base_growth
    assert 0.3 <= dna.dev_speed <= 1.0


def test_weekly_growth_and_breakout():
    dna = PlayerDNA()
    caps = dna.attribute_caps["speed"].copy()
    dna.apply_weekly_growth()
    assert dna.attribute_caps["speed"]["current"] >= caps["current"]
    dna.check_breakout("speed", production_metric=True, snap_share=0.8, week=1)
    assert dna.attribute_caps["speed"]["current"] >= caps["current"]


def test_position_attribute_storage():
    rb = Player("Back", "RB", 22, "2003-01-01", "U", "USA", 25, 70)
    assert "speed" in rb.attributes.core
    assert "break_tackle" in rb.attributes.position_specific
    assert "throw_power" not in rb.attributes.position_specific


def test_regression_profile_defaults():
    dna = PlayerDNA()
    prof = dna.regression_profile
    assert isinstance(prof, dict)
    assert prof.get("start_age") >= 0
    assert dna.attribute_decay_type.get("speed") == "physical"


def test_apply_regression_decreases_values():
    player = Player("Old", "RB", 31, "1990-01-01", "U", "USA", 22, 70)
    player.attributes.core["speed"] = 80
    player.attributes.core["acceleration"] = 80
    apply_regression(player)
    assert player.attributes.core["speed"] < 80
    assert player.attributes.core["acceleration"] < 80


def _simulate_full_career(player_id: str, position: str, years: int = 15):
    """Return season-by-season attribute log for a player."""

    dna = PlayerDNA.generate_random_dna(position)
    data = dna.to_dict()
    clone = PlayerDNA.from_dict(data)
    assert [m.name for m in clone.mutations] == [m.name for m in dna.mutations]

    attrs = {
        attr: random.randint(65, 75)
        for attr in valid_attributes_by_position[position]
    }
    caps = {
        attr: min(attrs[attr] + random.randint(10, 25), 100)
        for attr in attrs
    }

    class AttrSet:
        def __init__(self, core: dict):
            self.core = core
            self.position_specific = {}

    class DummyPlayer:
        pass

    player = DummyPlayer()
    player.position = position
    player.dna = dna
    player.attributes = AttrSet(attrs)

    log = []
    for year in range(1, years + 1):
        age = 23 + year - 1
        player.age = age
        for attr, val in list(attrs.items()):
            gain = 1.0 * dna.dev_speed
            gain = dna._apply_mutation_boost(attr, gain)
            attrs[attr] = min(caps[attr], round(val + gain, 2))
            player.attributes.core[attr] = attrs[attr]
        apply_regression(player)
        log.append(
            {
                "player": player_id,
                "position": position,
                "year": year,
                "age": age,
                "dev_speed": dna.dev_speed,
                "mutations": ",".join(m.name for m in dna.mutations),
                **attrs,
                **{f"{k}_cap": v for k, v in caps.items()},
            }
        )
    return log


def test_dna_long_term_progression(tmp_path):
    """Simulate full career growth/regression and export to CSV."""
    random.seed(42)

    out_dir = Path("dna_output")
    out_dir.mkdir(exist_ok=True)
    csv_file = out_dir / "dna_long_term_progression.csv"

    logs = []
    for pos in ["QB", "RB", "WR"]:
        for i in range(2):
            logs.extend(_simulate_full_career(f"{pos}_{i+1}", pos))

    fieldnames = []
    for row in logs:
        for key in row:
            if key not in fieldnames:
                fieldnames.append(key)

    with open(csv_file, "w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for row in logs:
            writer.writerow(row)

    assert csv_file.exists() and csv_file.stat().st_size > 0
