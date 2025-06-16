import csv
import random
from pathlib import Path

import matplotlib
matplotlib.use("Agg")

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.player_weekly_update import advance_player_week


def _simulate_development(player_id: str, position: str, seasons: int = 5):
    """Return detailed season logs for a player's development."""

    player = Player(player_id, position, 22, "2000-01-01", "U", "USA", 1, 70)
    attr_names = list(player.dna.attribute_caps.keys())
    logs = []
    rng = random.Random(123)

    for season in range(seasons):
        # Progress/regress each week
        for _ in range(17):
            advance_player_week(player, {}, 1.0, rng)

        arc_val = (
            player.dna.career_arc[season]
            if season < len(player.dna.career_arc)
            else player.dna.career_arc[-1]
        )
        row = {
            "player": player_id,
            "age": player.age,
            "arc": round(arc_val, 3),
            "dev_speed": player.dna.dev_speed,
            "traits": ", ".join(player.dna.traits),
            "mutations": ", ".join(m.name for m in player.dna.mutations),
        }
        for attr in attr_names:
            val = player.attributes.core.get(attr, player.attributes.position_specific.get(attr))
            row[attr] = val
            cap = player.hidden_caps.get(attr, player.dna.attribute_caps.get(attr, {}).get("hard_cap"))
            row[f"{attr}_cap"] = cap
        logs.append(row)

        player.age += 1

    return logs


def test_player_growth_regression_output(tmp_path):
    random.seed(99)
    out_dir = Path("dna_output")
    out_dir.mkdir(exist_ok=True)
    csv_file = out_dir / "player_growth_regression.csv"

    all_logs = []
    for pos in ["QB", "RB", "WR"]:
        log = _simulate_development(f"{pos}_dev", pos)
        all_logs.extend(log)

    if all_logs:
        fieldnames = []
        for row in all_logs:
            for key in row:
                if key not in fieldnames:
                    fieldnames.append(key)
        with open(csv_file, "w", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=fieldnames)
            writer.writeheader()
            for row in all_logs:
                writer.writerow(row)

    assert csv_file.exists() and csv_file.stat().st_size > 0
