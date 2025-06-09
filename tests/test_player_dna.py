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


def test_growth_curve_has_peak_and_decline():
    dna = PlayerDNA.generate_random_dna("QB")
    curve = dna.growth_curve
    ages = sorted(curve)
    peak = dna.peak_age

    # Ascending portion up to the peak
    ascending = all(
        curve[a] <= curve[min(peak, a + 1)] + 0.05 for a in ages if a < peak
    )
    # Descending portion after the peak
    descending = all(
        curve[a] >= curve[min(ages[-1], a + 1)] - 0.05 for a in ages if a >= peak
    )

    assert ascending and descending


def test_career_progression_simulation(tmp_path):
    """Simulate career progression for environment groups A/B/C."""
    import copy
    import pandas as pd
    import matplotlib.pyplot as plt

    groups = {
        "A": {"xp_factor": 1.5},
        "B": {"xp_factor": 1.0},
        "C": {"xp_factor": 0.5},
    }

    # Create base DNA for each group and shallow clone to players
    for g in groups:
        groups[g]["dna"] = PlayerDNA.generate_random_dna("QB")
        groups[g]["players"] = []
        for i in range(5):
            p = Player(f"{g}_QB_{i}", "QB", 20, "2005-01-01", "U", "USA", i, 50)
            p.dna = copy.copy(groups[g]["dna"])
            p.speed = 50
            p.throw_power = 50
            p.awareness = 50
            groups[g]["players"].append(p)

    log_rows = []
    for age in range(20, 36):
        for g, info in groups.items():
            xp = 100 * info["xp_factor"]
            for p in info["players"]:
                growth = p.dna.growth_curve.get(age, 1.0)
                mult = growth * p.dna.development_speed
                gain = (xp * mult) / 100.0
                if growth < 1.0:
                    gain -= p.dna.regression_rate

                for attr in ["speed", "throw_power", "awareness"]:
                    cap = p.dna.max_attribute_caps.get(attr, 99)
                    val = getattr(p, attr) + gain
                    setattr(p, attr, min(cap, val))

                ovr = round((p.speed + p.throw_power + p.awareness) / 3, 2)
                log_rows.append(
                    {
                        "player_id": p.id,
                        "group": g,
                        "age": age,
                        "ovr": ovr,
                        "speed": p.speed,
                        "throw_power": p.throw_power,
                        "awareness": p.awareness,
                        "growth_multiplier": round(mult, 3),
                        "net_gain": round(gain, 3),
                    }
                )

    df = pd.DataFrame(log_rows)
    df.to_csv(tmp_path / "career_log.csv", index=False)

    # OVR progression per group
    plt.figure()
    for g in groups:
        avg_ovr = df[df["group"] == g].groupby("age")["ovr"].mean()
        plt.plot(avg_ovr.index, avg_ovr.values, label=f"Group {g}")
    plt.xlabel("Age")
    plt.ylabel("OVR")
    plt.legend()
    plt.title("OVR Progression by Group")
    plt.tight_layout()
    plt.savefig(tmp_path / "ovr_by_age.png")
    plt.close()

    # Attribute growth comparison
    plt.figure()
    final_attrs = (
        df[df["age"] == 35]
        .groupby("group")[["speed", "throw_power", "awareness"]]
        .mean()
    )
    final_attrs.plot(kind="bar")
    plt.ylabel("Rating")
    plt.title("Final Attribute Ratings by Group")
    plt.tight_layout()
    plt.savefig(tmp_path / "attr_growth.png")
    plt.close()

    # DNA growth curves
    plt.figure()
    for g, info in groups.items():
        ages = sorted(info["dna"].growth_curve)
        vals = [info["dna"].growth_curve[a] for a in ages]
        plt.plot(ages, vals, label=f"Group {g}")
    plt.xlabel("Age")
    plt.ylabel("Multiplier")
    plt.legend()
    plt.title("DNA Growth Curves")
    plt.tight_layout()
    plt.savefig(tmp_path / "growth_curves_groups.png")
    plt.close()

    # Basic checks
    assert len(df) == 240
    for fn in ["career_log.csv", "ovr_by_age.png", "attr_growth.png", "growth_curves_groups.png"]:
        assert (tmp_path / fn).exists()

