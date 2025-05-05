import random
import json
import os
from gridiron_gm.engine.scouting.rookie_player import RookiePlayer

POSITIONS = ["QB", "RB", "WR", "TE", "OL", "DL", "LB", "CB", "S", "K", "P"]
COLLEGES = [
    "Alabama", "Georgia", "Ohio State", "Michigan", "USC",
    "Oklahoma", "Texas", "LSU", "Florida", "Oregon"
]

# Load real names from /data/ folder
DATA_DIR = os.path.join(os.path.dirname(__file__), "..", "..", "data")

def load_names(filename):
    path = os.path.join(DATA_DIR, filename)
    with open(path, "r", encoding="utf-8") as f:
        return [line.strip() for line in f if line.strip()]

MALE_FIRST_NAMES = load_names("male_first_names.txt")
LAST_NAMES = load_names("last_names.txt")

def generate_rookie_class(num_players=150):
    rookie_class = []

    for _ in range(num_players):
        first_name = random.choice(MALE_FIRST_NAMES)
        last_name = random.choice(LAST_NAMES)
        position = random.choice(POSITIONS)
        college = random.choice(COLLEGES)
        height = random.randint(68, 80)  # Inches
        weight = random.randint(180, 330)  # Pounds
        overall = random.randint(50, 95)
        potential = random.randint(60, 99)
        projected_pick = random.randint(1, 224)
        dev_tier = random.choice(["Low", "Mid", "High", "Blue Chip"])

        player = RookiePlayer(
            name=f"{first_name} {last_name}",
            position=position,
            college=college,
            height=height,
            weight=weight,
            overall=overall,
            potential=potential,
            dev_tier=dev_tier,
            projected_pick=projected_pick
        )

        rookie_class.append(player)

    return rookie_class

def save_rookie_class(rookie_class, filename="rookie_class.json"):
    simple_list = [player.to_dict() for player in rookie_class]
    with open(filename, "w", encoding="utf-8") as f:
        json.dump(simple_list, f, indent=4)

def load_rookie_class(filename="rookie_class.json"):
    try:
        with open(filename, "r", encoding="utf-8") as f:
            data = json.load(f)
        rookie_class = [RookiePlayer.from_dict(player_data) for player_data in data]
        return rookie_class
    except FileNotFoundError:
        return []
