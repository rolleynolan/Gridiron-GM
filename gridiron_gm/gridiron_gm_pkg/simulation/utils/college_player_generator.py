import random
import os
from datetime import datetime, timedelta
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
from typing import List, Dict, Optional
import json

DEFAULT_FIRST_NAMES = ["Jalen", "Michael", "Tyrone", "Devon"]
DEFAULT_LAST_NAMES = ["Johnson", "Williams", "Brown", "Taylor"]
DEFAULT_COLLEGES = ["Alabama", "Ohio State", "Michigan", "Georgia"]
DEFAULT_CITIES = ["Houston, TX", "Miami, FL", "Chicago, IL", "Phoenix, AZ"]

def load_data_file(file_path: str, fallback: List[str]) -> List[str]:
    try:
        with open(file_path, encoding="utf-8") as f:
            data = [line.strip() for line in f if line.strip()]
        if not data:
            raise ValueError("File is empty")
        data = list(set(data))
        if not data:
            raise ValueError("File contains only duplicates or empty lines")
        return data
    except Exception as e:
        print(f"[ERROR] Failed loading file {file_path}: {e}")
        print(f"[DEBUG] Falling back to default data for {file_path}")
        return fallback

def load_enriched_cities(file_path: str, fallback: List[str]) -> List[str]:
    try:
        with open(file_path, encoding="utf-8") as f:
            data = json.load(f)
        cities = [f"{city['city']}, {city['state']}" for city in data if 'city' in city and 'state' in city]
        if not cities:
            raise ValueError("File contains no valid city entries")
        return list(set(cities))
    except Exception as e:
        print(f"[ERROR] Failed loading enriched cities file {file_path}: {e}")
        print(f"[DEBUG] Falling back to default cities for {file_path}")
        return fallback

def generate_valid_jersey_number(position):
    position_ranges = {
        'QB': range(1, 20),
        'RB': list(range(20, 50)),
        'WR': list(range(10, 20)) + list(range(80, 90)),
        'TE': list(range(40, 50)) + list(range(80, 90)),
        'OL': range(50, 80),
        'DL': list(range(50, 80)) + list(range(90, 100)),
        'LB': list(range(40, 60)) + list(range(90, 100)),
        'CB': range(20, 40),
        'S': range(20, 40),
        'K': range(1, 20),
        'P': range(1, 20)
    }
    return random.choice(position_ranges.get(position, range(1, 100)))

def generate_position_stats(position: str, scale: float, traits: List[str]) -> Dict[str, int]:
    variability = 1.2 if "boom_bust" in traits else 1.0
    stats = {"games": random.randint(10, 13)}
    if position == "QB":
        stats.update({
            "pass_yards": int(random.uniform(2000, 4500) * scale * variability),
            "pass_tds": int(random.uniform(15, 40) * scale * variability),
            "interceptions": int(random.uniform(3, 15) * (1.5 - scale)),
            "completion_pct": round(random.uniform(55, 75) * scale, 1),
            "rush_yards": int(random.uniform(100, 800) * scale),
            "sacks": int(random.uniform(10, 35) * (1.5 - scale))
        })
    elif position == "RB":
        stats.update({
            "rush_yards": int(random.uniform(600, 2000) * scale * variability),
            "tds": int(random.uniform(4, 20) * scale),
            "receptions": int(random.uniform(10, 50) * scale),
            "rec_yards": int(random.uniform(100, 600) * scale),
            "fumbles": int(random.uniform(0, 5) * (1.5 - scale))
        })
    elif position == "WR":
        stats.update({
            "receptions": int(random.uniform(30, 110) * scale),
            "rec_yards": int(random.uniform(400, 1600) * scale),
            "tds": int(random.uniform(3, 15) * scale),
            "drops": int(random.uniform(0, 10) * (1.2 - scale))
        })
    elif position == "LB":
        stats.update({
            "tackles": int(random.uniform(50, 140) * scale),
            "sacks": int(random.uniform(0, 10) * scale),
            "interceptions": int(random.uniform(0, 4) * scale),
            "forced_fumbles": int(random.uniform(0, 4) * scale)
        })
    elif position == "CB":
        stats.update({
            "tackles": int(random.uniform(20, 60) * scale),
            "interceptions": int(random.uniform(0, 5) * scale),
            "pass_deflections": int(random.uniform(5, 20) * scale),
            "tds_allowed": int(random.uniform(1, 10) * (1.5 - scale))
        })
    return stats

def generate_college_stats(position: str, dev_curve: str, potential: int, traits: Optional[List[str]] = None, team_quality: float = 1.0) -> Dict[str, Dict]:
    if traits is None:
        traits = []

    stats_by_year = {}
    base_years = ['Freshman', 'Sophomore', 'Junior', 'Senior']
    redshirt_used = False
    is_raw = dev_curve in ["late", "flat"]
    redshirt_chance = (0.25 if is_raw else 0.05) * ((100 - potential) / 100)

    if random.random() < redshirt_chance:
        redshirt_used = True
        base_years.insert(1, 'Redshirt Freshman')

    curve_modifiers = {
        'early': [1.2, 1.1, 1.0, 0.9, 0.8],
        'normal': [0.8, 1.0, 1.1, 1.2, 1.3],
        'late': [0.6, 0.8, 1.0, 1.3, 1.4],
        'flat': [1.0] * 5
    }
    modifiers = curve_modifiers.get(dev_curve, [1.0] * len(base_years))

    for i, year in enumerate(base_years):
        scale = (potential / 100) * modifiers[i] * team_quality
        injured = "injury_prone" in traits and random.random() < 0.3
        note = "Injured" if injured else None

        if injured:
            stats_by_year[year] = {"games": random.randint(1, 6), "note": note}
            continue

        starts = random.random() < (0.2 + 0.2 * i)
        if not starts:
            stats_by_year[year] = {"games": 0}
            continue

        stats = generate_position_stats(position, scale, traits)
        if note:
            stats["note"] = note
        stats_by_year[year] = stats

    return stats_by_year

def project_dev_curve(college_stats, traits=None, scout_accuracy=0.8):
    if traits is None:
        traits = []
    scores = []
    for year, stats in college_stats.items():
        if stats.get("games", 0) == 0:
            scores.append(0)
        else:
            total = sum(
                v for k, v in stats.items()
                if isinstance(v, (int, float)) and k not in ["games", "sacks", "interceptions", "fumbles", "drops", "tds_allowed"]
            )
            scores.append(total)

    max_val = max(scores) if scores else 1
    norm = [s / max_val if max_val else 0 for s in scores]

    if len(norm) < 2:
        guess = "flat"
    elif norm[0] > 0.8 and all(n < norm[0] for n in norm[1:]):
        guess = "early"
    elif norm[-1] > 0.8 and all(n < norm[-1] for n in norm[:-1]):
        guess = "late"
    elif all(earlier <= later for earlier, later in zip(norm, norm[1:])):
        guess = "normal"
    else:
        guess = "flat"

    if random.random() > scout_accuracy:
        choices = ["early", "normal", "late", "flat"]
        choices.remove(guess)
        guess = random.choice(choices)

    return guess

def generate_college_player(year_in_college: int) -> Player:
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.abspath(os.path.join(base_dir, "..", ".."))
    data_dir = os.path.join(project_root, "data")

    first_names = load_data_file(os.path.join(data_dir, "names", "male_first_names.txt"), DEFAULT_FIRST_NAMES)
    last_names = load_data_file(os.path.join(data_dir, "names", "last_names.txt"), DEFAULT_LAST_NAMES)
    colleges = load_data_file(os.path.join(data_dir, "locations", "fbs_colleges.txt"), DEFAULT_COLLEGES)
    cities = load_enriched_cities(os.path.join(data_dir, "locations", "enriched_us_cities.json"), DEFAULT_CITIES)

    name = f"{random.choice(first_names)} {random.choice(last_names)}"
    position = random.choice(['QB', 'RB', 'WR', 'TE', 'OL', 'DL', 'LB', 'CB', 'S', 'K', 'P'])
    dob = datetime.now() - timedelta(days=random.randint(18*365, 23*365))
    age = (datetime.now() - dob).days // 365

    college = random.choice(colleges)
    birth_location = random.choice(cities)
    jersey_number = generate_valid_jersey_number(position)
    overall = max(40, min(int(random.gauss(70, 7)), 85))
    dev_curve = random.choices(['early', 'normal', 'late', 'flat'], weights=[0.2, 0.55, 0.2, 0.05])[0]
    potential = max(50, min(int(random.gauss(75, 7)), 99))
    dev_rate = round(random.uniform(0.9, 1.1), 2)

    traits = []
    if random.random() < 0.15:
        traits.append("boom_bust")
    if random.random() < 0.10:
        traits.append("injury_prone")

    college_stats = generate_college_stats(position, dev_curve, potential, traits)
    projected = project_dev_curve(college_stats, traits)

    player = Player(name, position, age, dob, college, birth_location, jersey_number, overall)
    player.school = college
    player.true_attributes = {k: random.randint(60, 99) for k in ["Speed", "Strength", "Awareness"]}
    player.estimated_attributes = {k: (v - random.randint(5, 15), v + random.randint(5, 15)) for k, v in player.true_attributes.items()}
    player.scouted_rating = {}
    player.scouted_skills = {}
    player.scout_reports = {}

    player.year_in_college = year_in_college
    player.dev_curve = dev_curve
    player.dev_curve_projected = projected
    player.dev_rate = dev_rate
    player.potential = potential
    player.traits = traits
    player.college_stats = college_stats
    player.true_overall = overall
    player.region = random.choice(["Southeast", "Midwest", "West", "Northeast"])
    player.redshirt_used = any("Redshirt" in y for y in college_stats.keys())
    player.college_year_labels = list(college_stats.keys())
    return player

def generate_initial_college_db(num_per_class=2825):
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.abspath(os.path.join(base_dir, "..", ".."))
    data_dir = os.path.join(project_root, "data")

    cities = load_enriched_cities(os.path.join(data_dir, "locations", "enriched_us_cities.json"), DEFAULT_CITIES)

    college_db = []
    for year in range(1, 5):
        for _ in range(num_per_class):
            player = generate_college_player(year)
            player.birth_location = random.choice(cities)
            college_db.append(player)
    return college_db

def generate_freshman_class(num_players=2825):
    """Generate a new freshman class to append each season."""
    return [generate_college_player(1) for _ in range(num_players)]
