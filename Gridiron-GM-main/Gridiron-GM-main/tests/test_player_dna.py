from pathlib import Path
import csv
import random
import sys

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA, MutationType
from gridiron_gm_pkg.simulation.systems.player import player_regression
from gridiron_gm_pkg.simulation.systems.player.player_regression import (
    valid_attributes_by_position,
    decay_multipliers,
)
from gridiron_gm_pkg.simulation.systems.player.player_dna import ATTRIBUTE_DECAY_TYPE
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.player_weekly_update import advance_player_week


def plot_career_arcs(
    arc_data: dict,
    decline_ages: dict,
    start_ages: dict,
    highlight_player: str = "QB_4",
    output_file: Path | None = None,
):
    """Plot career arcs with optional highlight and decline marker."""

    plt.figure(figsize=(12, 6))

    for player_label, arc in arc_data.items():
        years = list(range(len(arc)))
        if player_label == highlight_player:
            plt.plot(
                years,
                arc,
                label=player_label,
                linewidth=3.5,
                color="black",
                linestyle="--",
            )
            decline_year = decline_ages[player_label] - start_ages[player_label]
            plt.axvline(x=decline_year, color="red", linestyle=":", linewidth=2)
        else:
            plt.plot(years, arc, label=player_label, alpha=0.4)

    plt.title("Career Arcs")
    plt.xlabel("Year")
    plt.ylabel("Arc Value")
    plt.legend()
    plt.grid(True)

    if output_file:
        plt.tight_layout()
        plt.savefig(output_file)
    else:
        plt.show()
    plt.close()


def export_player_log(player_id, dna, position, age, attributes, caps, arc_val):
    """Return a dictionary row for CSV export."""
    return {
        "player": player_id,
        "position": position,
        "age": age,
        "arc": round(arc_val, 3),
        "dev_speed": dna.dev_speed,
        "traits": ", ".join(dna.traits),
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

        decay_type = ATTRIBUTE_DECAY_TYPE.get(attr, "physical")
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
    assert "throw_power" in rb.attributes.position_specific


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
    # Force decline to begin at or before the player's current age
    player.dna.growth_arc.decline_start_age = 30
    player_regression.apply_regression(player, age=31)
    assert player.attributes.core["speed"] <= 80
    assert player.attributes.core["acceleration"] <= 80


def test_regression_accumulates_over_weeks():
    player = Player("Old", "RB", 31, "1990-01-01", "U", "USA", 22, 70)
    player.attributes.core["speed"] = 80
    player.dna.growth_arc.decline_start_age = 30
    rng = random.Random(42)
    for _ in range(20):
        player_regression.apply_regression(player, age=31, rng=rng)
    assert player.attributes.core["speed"] < 80


def _simulate_full_career(player_id: str, position: str, years: int = 15):
    """Simulate weekly development/regression for a player's career."""

    player = Player(player_id, position, 22, "2000-01-01", "U", "USA", 1, 70)

    start_age = player.age
    decline_age = player.dna.growth_arc.decline_start_age

    data = player.dna.to_dict()
    clone = PlayerDNA.from_dict(data)
    assert [m.name for m in clone.mutations] == [m.name for m in player.dna.mutations]

    attr_names = list(player.dna.attribute_caps.keys())
    logs = []
    arc_vals = []
    rng = random.Random(123)

    for season in range(years):
        for _ in range(17):
            advance_player_week(player, {}, 1.0, rng)

        arc_val = (
            player.dna.career_arc[season]
            if season < len(player.dna.career_arc)
            else player.dna.career_arc[-1]
        )

        attributes = {
            attr: player.attributes.core.get(attr, player.attributes.position_specific.get(attr))
            for attr in attr_names
        }
        caps = {
            attr: player.hidden_caps.get(attr, player.dna.attribute_caps.get(attr, {}).get("hard_cap"))
            for attr in attr_names
        }

        logs.append(
            export_player_log(player_id, player.dna, position, player.age, attributes, caps, arc_val)
        )
        arc_vals.append(arc_val)
        player.age += 1

    return logs, arc_vals, decline_age, start_age


def test_dna_long_term_progression(tmp_path):
    """Simulate full career growth/regression and export to CSV."""
    random.seed(42)

    out_dir = Path("dna_output")
    out_dir.mkdir(exist_ok=True)
    csv_file = out_dir / "dna_long_term_progression.csv"

    logs = []
    arcs = []
    decline_ages = {}
    start_ages = {}
    for pos in ["QB", "RB", "WR"]:
        for i in range(5):
            player_label = f"{pos}_{i+1}"
            log, arc, decline_age, start_age = _simulate_full_career(player_label, pos)
            logs.extend(log)
            arcs.append((player_label, arc))
            decline_ages[player_label] = decline_age
            start_ages[player_label] = start_age

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

    graph_file = out_dir / "career_arcs.png"
    arc_dict = {label: arc for label, arc in arcs}
    plot_career_arcs(
        arc_dict,
        decline_ages,
        start_ages,
        highlight_player="QB_4",
        output_file=graph_file,
    )

    assert csv_file.exists() and csv_file.stat().st_size > 0
    assert graph_file.exists() and graph_file.stat().st_size > 0
