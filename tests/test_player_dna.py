from pathlib import Path
import csv
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA, MutationType
from gridiron_gm_pkg.simulation.systems.player import player_regression
from gridiron_gm_pkg.simulation.systems.player.player_regression import (
    valid_attributes_by_position,
    attribute_decay_type,
    decay_multipliers,
)
from gridiron_gm_pkg.simulation.entities.player import Player


def export_player_log(player_id, dna, position, age, attributes, caps, arc_val):
    """Return a dictionary row for CSV export."""
    return {
        "player": player_id,
        "position": position,
        "age": age,
        "arc": round(arc_val, 3),
        "dev_speed": dna.dev_speed,
        "mutations": ", ".join(m.name for m in dna.mutations),
        **{attr: attributes.get(attr) for attr in caps},
        **{f"{attr}_cap": caps[attr] for attr in caps},
    }


def apply_growth(attr: str, current: float, cap: float, dna: PlayerDNA, age: int, position: str) -> float:
    if attr not in valid_attributes_by_position[position]:
        return 0.0

    growth = 1.0 * dna.dev_speed
    if "FastLearner" in [m.name for m in dna.mutations]:
        growth *= 1.1
    if (
        "TechnicalWizard" in [m.name for m in dna.mutations]
        and attr in ["throw_accuracy", "route_running", "catching"]
    ):
        growth *= 1.15

    return max(0.0, min(growth, cap - current))


def apply_regression_local(attributes: dict, caps: dict, age: int, dna: PlayerDNA, position: str) -> None:
    profile = dna.regression_profile
    if age < profile["start_age"]:
        return

    decay_base = profile["rate"]
    position_mod = profile["position_modifier"].get(position, 1.0)

    for attr in list(attributes.keys()):
        if attr not in valid_attributes_by_position[position]:
            continue

        decay_type = attribute_decay_type.get(attr, "physical")
        decay_mult = decay_multipliers.get(decay_type, 1.0)
        total_decay = decay_base * position_mod * decay_mult

        if "BuiltToLast" in [m.name for m in dna.mutations]:
            total_decay *= 0.5

        attributes[attr] = round(max(0, attributes[attr] * (1 - total_decay)), 2)


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
    player_regression.apply_regression(player)
    assert player.attributes.core["speed"] < 80
    assert player.attributes.core["acceleration"] < 80


def _simulate_full_career(player_id: str, position: str, years: int = 15):
    """Return season-by-season attribute log for a player."""

    dna = PlayerDNA.generate_random_dna(position)
    data = dna.to_dict()
    clone = PlayerDNA.from_dict(data)
    assert [m.name for m in clone.mutations] == [m.name for m in dna.mutations]

    tmp_player = Player("tmp", position, 22, "2000-01-01", "U", "USA", 0, 60)
    attrs = {attr: random.randint(65, 75) for attr in tmp_player.get_relevant_attribute_names()}
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
    player.get_relevant_attribute_names = tmp_player.get_relevant_attribute_names

    base_attrs = attrs.copy()
    arc = dna.career_arc[:years]
    log = []
    for year in range(1, years + 1):
        age = 23 + year - 1
        player.age = age
        arc_val = arc[year - 1] if year - 1 < len(arc) else arc[-1]
        for attr in list(attrs.keys()):
            baseline = base_attrs[attr]
            target = baseline + (caps[attr] - baseline) * arc_val
            attrs[attr] = round(min(caps[attr], max(baseline, target)), 2)
            player.attributes.core[attr] = attrs[attr]
        log.append(
            export_player_log(player_id, dna, position, age, attrs, caps, arc_val)
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
