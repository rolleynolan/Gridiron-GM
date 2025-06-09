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
    dna = PlayerDNA.generate_random_dna("QB")
    dna.mutations = ["Physical Freak"]
    dna.max_attribute_caps = {"speed": 90, "strength": 80}
    dna.apply_mutation_effects()
    assert dna.max_attribute_caps["speed"] >= 90
    assert dna.max_attribute_caps["strength"] >= 80




def test_generate_player_growth_tables(tmp_path):
    """Generate sample players and output growth and attribute tables."""
    import pandas as pd
    import matplotlib.pyplot as plt

    players = [Player(f"Player {i}", "QB", 22, "2003-01-01", "U", "USA", i, 70) for i in range(5)]

    table_rows = []
    for p in players:
        df = pd.DataFrame(sorted(p.dna.growth_curve.items()), columns=["Age", "Multiplier"])
        df.to_csv(tmp_path / f"{p.id}_growth.csv", index=False)
        plt.plot(df["Age"], df["Multiplier"], label=p.name)
        table_rows.append({
            "Name": p.name,
            "Mutation": ",".join(p.dna.mutations) if p.dna.mutations else "None",
            "DevSpeed": p.dna.development_speed,
            "Regression": p.dna.regression_rate,
            "PeakAge": p.dna.peak_age,
        })

    plt.legend()
    plt.xlabel("Age")
    plt.ylabel("Growth Multiplier")
    plt.title("Player DNA Curves")
    plt.tight_layout()
    plt.savefig(tmp_path / "growth_curves.png")

    summary = pd.DataFrame(table_rows)
    summary.to_csv(tmp_path / "player_summary.csv", index=False)

    # Ensure files were written
    assert (tmp_path / "growth_curves.png").exists()
    for p in players:
        assert (tmp_path / f"{p.id}_growth.csv").exists()

