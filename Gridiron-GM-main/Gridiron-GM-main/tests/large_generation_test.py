import random
import sys
import os
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..")))
import csv
import json
import sys
from pathlib import Path

import seaborn as sns
import matplotlib.pyplot as plt

sys.path.append(str(Path(__file__).resolve().parents[2]))
from gridiron_gm_pkg.config.attribute_profiles import ATTRIBUTE_RANGES

# Derive positions from attribute ranges
POSITIONS = list(ATTRIBUTE_RANGES.keys())


def generate_attribute(position: str, name: str, level: str = "college"):
    """Generate a single attribute value with soft and hard caps."""
    config = ATTRIBUTE_RANGES[position][level][name]
    value = random.gauss(config["mean"], config["stddev"])
    value = max(config["min"], min(config["max"], round(value, 2)))
    soft_cap = min(config["max"], round(value + random.uniform(3, 10), 2))
    hard_cap = min(99.0, round(soft_cap + random.uniform(2, 8), 2))
    return {"value": value, "soft_cap": soft_cap, "hard_cap": hard_cap}


def generate_player(position: str, level: str = "college"):
    attrs = {}
    for attr in ATTRIBUTE_RANGES[position][level].keys():
        attrs[attr] = generate_attribute(position, attr, level)
    age = random.randint(20, 23) if level == "college" else random.randint(22, 35)
    return {"position": position, "level": level, "age": age, "attributes": attrs}


def main():
    output_dir = Path("dna_output")
    output_dir.mkdir(exist_ok=True)
    players = []
    for _ in range(500):
        players.append(generate_player(random.choice(POSITIONS), "college"))
    for _ in range(500):
        players.append(generate_player(random.choice(POSITIONS), "pro"))

    # Determine all attribute names for CSV header
    attr_names = sorted({a for p in players for a in p["attributes"]})
    fieldnames = ["position", "level", "age"]
    for attr in attr_names:
        fieldnames.extend([attr, f"{attr}_soft_cap", f"{attr}_hard_cap"])

    csv_path = output_dir / "player_generation_output.csv"
    with open(csv_path, "w", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=fieldnames)
        writer.writeheader()
        for p in players:
            row = {"position": p["position"], "level": p["level"], "age": p["age"]}
            for attr in attr_names:
                data = p["attributes"].get(attr)
                if data:
                    row[attr] = data["value"]
                    row[f"{attr}_soft_cap"] = data["soft_cap"]
                    row[f"{attr}_hard_cap"] = data["hard_cap"]
                else:
                    row[attr] = row[f"{attr}_soft_cap"] = row[f"{attr}_hard_cap"] = ""
            writer.writerow(row)

    # Create histogram of speed values across all players
    speed_values = [p["attributes"].get("speed", {}).get("value") for p in players if "speed" in p["attributes"]]
    plt.figure(figsize=(8, 6))
    sns.histplot(speed_values, kde=True, stat="density", bins=20, color="skyblue")
    plt.title("Speed Attribute Distribution")
    plt.xlabel("Speed Rating")
    plt.ylabel("Density")
    img_path = output_dir / "speed_distribution.png"
    plt.tight_layout()
    plt.savefig(img_path)
    plt.close()

    json_path = output_dir / "player_generation_output.json"
    with open(json_path, "w") as f:
        json.dump(players, f, indent=2)


if __name__ == "__main__":
    main()
