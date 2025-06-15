from pathlib import Path
import csv
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA, MutationType
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


def test_dna_long_term_progression(tmp_path):
    """Simulate long term DNA growth and write results to csv."""
    random.seed(42)

    out_dir = Path("dna_output")
    out_dir.mkdir(exist_ok=True)
    csv_file = out_dir / "dna_long_term_progression.csv"

    positions = ["QB", "RB", "WR"]
    players = []
    for pos in positions:
        for i in range(2):
            players.append(
                Player(f"{pos}_{i+1}", pos, 22, "2000-01-01", "U", "USA", 10 + i, 70)
            )

    coaching = {p.name: round(random.uniform(0.8, 1.2), 2) for p in players}

    records = []
    week_counter = 0
    for year in range(15):
        for p in players:
            for _ in range(17):
                p.dna.apply_weekly_growth(coaching_quality=coaching[p.name])
                for attr in p.dna.attribute_caps:
                    p.dna.check_breakout(
                        attr, production_metric=True, snap_share=0.8, week=week_counter
                    )
                week_counter += 1
            p.age += 1
            row = {
                "player": p.name,
                "position": p.position,
                "year": year + 1,
                "age": p.age,
                "coaching_quality": coaching[p.name],
                "dev_speed": p.dna.dev_speed,
                "mutations": ",".join(m.name for m in p.dna.mutations),
            }
            for attr, caps in p.dna.attribute_caps.items():
                row[attr] = round(caps["current"], 2)
                row[f"{attr}_cap"] = caps["hard_cap"]
            records.append(row)

    fieldnames = list(records[0].keys())
    with open(csv_file, "w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        writer.writerows(records)

    assert csv_file.exists()
