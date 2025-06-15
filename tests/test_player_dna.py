from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import csv
import random
from gridiron_gm.gridiron_gm_pkg.players.player_dna import PlayerDNA, MutationType
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player


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


def test_long_term_dna_output():
    random.seed(42)
    positions = ["QB", "RB", "WR"]
    players = []
    for pos in positions:
        for i in range(2):
            players.append(
                Player(f"Sim {pos}{i}", pos, 20, "2005-01-01", "U", "USA", i + 1, 60)
            )

    rows = []
    for player in players:
        coach = random.uniform(0.9, 1.1)
        for year in range(15):
            for _ in range(17):
                player.dna.apply_weekly_growth(coaching_quality=coach)
            age = player.age + year
            log = {
                "player_id": player.id,
                "position": player.position,
                "age": age,
                "coach_quality": round(coach, 2),
                "mutations": "|".join(m.name for m in player.dna.mutations),
            }
            for attr, caps in player.dna.attribute_caps.items():
                log[f"{attr}_cur"] = round(caps["current"], 2)
                log[f"{attr}_soft"] = caps["soft_cap"]
                log[f"{attr}_hard"] = caps["hard_cap"]
            rows.append(log)
            player.dna.regress_player(age)

    output_path = Path("dna_output/long_term_dna.csv")
    output_path.parent.mkdir(exist_ok=True)
    with output_path.open("w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=rows[0].keys())
        writer.writeheader()
        writer.writerows(rows)

    assert output_path.exists()
    assert len(rows) == 15 * len(players)
