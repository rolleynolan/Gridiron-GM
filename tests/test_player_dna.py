import random
from pathlib import Path
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.players.player_dna import PlayerDNA
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player


def test_player_has_dna_on_creation():
    p = Player("DNA Guy", "QB", 22, "2003-01-01", "U", "USA", 12, 70)
    assert p.dna is not None
    assert p.hidden_caps


def test_mutation_application():
    dna = PlayerDNA()
    dna.mutation = "Physical Freak"
    caps = {"speed": 90, "strength": 80}
    boosted = dna.apply_mutation_effects(caps)
    assert boosted["speed"] >= 90
    assert boosted["strength"] >= 80


def _generate_growth_curve(dna: PlayerDNA, years: int = 15) -> list[tuple[int, float]]:
    """Return age-rating pairs for a simple growth curve based on dna.growth_type."""
    rating = 50.0
    curve = []
    for year in range(years):
        age = 20 + year
        if dna.growth_type == "early_peak":
            if year < 4:
                rating += dna.dev_speed * 3
            elif year < 8:
                rating += dna.dev_speed
            else:
                rating -= dna.dev_speed * 1.5
        elif dna.growth_type == "late_bloomer":
            if year < 6:
                rating += dna.dev_speed
            elif year < 10:
                rating += dna.dev_speed * 3
            else:
                rating -= dna.dev_speed * 1.5
        elif dna.growth_type == "rollercoaster":
            rating += random.choice([-2, 2]) * dna.dev_speed
        else:  # flatline
            rating += dna.dev_speed
        rating = max(40.0, min(99.0, rating))
        curve.append((age, rating))
    return curve


def test_generate_player_growth_tables(tmp_path):
    """Generate sample players and output growth and attribute tables."""
    import pandas as pd
    import matplotlib.pyplot as plt

    players = [Player(f"Player {i}", "QB", 22, "2003-01-01", "U", "USA", i, 70) for i in range(5)]

    table_rows = []
    for p in players:
        curve = _generate_growth_curve(p.dna)
        df = pd.DataFrame(curve, columns=["Age", "Rating"])
        df.to_csv(tmp_path / f"{p.id}_growth.csv", index=False)
        plt.plot(df["Age"], df["Rating"], label=p.name)
        table_rows.append({
            "Name": p.name,
            "Mutation": p.dna.mutation or "None",
            "Growth Type": p.dna.growth_type,
            "Caps": p.dna.attribute_caps,
        })

    plt.legend()
    plt.xlabel("Age")
    plt.ylabel("Rating")
    plt.title("Player Growth Curves")
    plt.tight_layout()
    plt.savefig(tmp_path / "growth_curves.png")

    summary = pd.DataFrame(table_rows)
    summary.to_csv(tmp_path / "player_summary.csv", index=False)

    # Ensure files were written
    assert (tmp_path / "growth_curves.png").exists()
    for p in players:
        assert (tmp_path / f"{p.id}_growth.csv").exists()

