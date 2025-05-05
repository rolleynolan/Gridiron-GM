import random
import os
from datetime import datetime, timedelta
from gridiron_gm.engine.core.player import Player

# ------------------ JERSEY ------------------

def generate_valid_jersey_number(position):
    position_ranges = {
        'QB': range(0, 20),
        'RB': list(range(0, 50)) + list(range(20, 50)),
        'WR': list(range(0, 50)) + list(range(80, 90)),
        'TE': list(range(0, 50)) + list(range(80, 90)),
        'OL': range(50, 80),
        'DL': list(range(50, 80)) + list(range(90, 100)),
        'LB': list(range(0, 60)) + list(range(90, 100)),
        'CB': range(0, 50),
        'S': range(0, 50),
        'K': range(0, 20),
        'P': range(0, 20)
    }
    return random.choice(position_ranges.get(position, range(1, 100)))

# ------------------ PLAYER GEN ------------------

def generate_college_player(year_in_college):
    base_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.abspath(os.path.join(base_dir, "..", "..", ".."))
    data_dir = os.path.join(project_root, "gridiron_gm", "data")

    fallback_first = ["Jalen", "Michael", "Tyrone", "Devon"]
    fallback_last = ["Johnson", "Williams", "Brown", "Taylor"]

    first_path = os.path.join(data_dir, "male_first_names.txt")
    last_path = os.path.join(data_dir, "last_names.txt")

    try:
        with open(first_path, encoding="utf-8") as f:
            first_names = [line.strip() for line in f if line.strip()]
        if not first_names:
            raise ValueError("First name file empty")
    except Exception as e:
        print(f"[ERROR] Failed loading first names: {e}")
        first_names = fallback_first

    try:
        with open(last_path, encoding="utf-8") as f:
            last_names = [line.strip() for line in f if line.strip()]
        if not last_names:
            raise ValueError("Last name file empty")
    except Exception as e:
        print(f"[ERROR] Failed loading last names: {e}")
        last_names = fallback_last

    print(f"[DEBUG] Loaded {len(first_names)} first names from {first_path}")
    print(f"[DEBUG] Loaded {len(last_names)} last names from {last_path}")

    first = random.choice(first_names)
    last = random.choice(last_names)
    name = f"{first} {last}"
    position = random.choice(['QB', 'RB', 'WR', 'TE', 'OL', 'DL', 'LB', 'CB', 'S', 'K', 'P'])
    dob = datetime.now() - timedelta(days=365 * (18 + year_in_college))

    colleges_path = os.path.join(data_dir, 'fbs_colleges.txt')
    cities_path = os.path.join(data_dir, 'cities.txt')

    colleges = ["Alabama", "Ohio State", "Michigan", "Georgia"]
    cities = ["Houston, TX", "Miami, FL", "Chicago, IL", "Phoenix, AZ"]

    try:
        with open(colleges_path, encoding='utf-8') as f:
            loaded = [line.strip() for line in f if line.strip()]
            if loaded:
                colleges = loaded
    except Exception:
        pass

    try:
        with open(cities_path, encoding='utf-8') as f:
            loaded = [line.strip() for line in f if line.strip()]
            if loaded:
                cities = loaded
    except Exception:
        pass

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

    player = Player(name, position, 18 + year_in_college, dob, college, birth_location, jersey_number, overall)
    player.year_in_college = year_in_college
    player.dev_curve = dev_curve
    player.dev_curve_projected = projected
    player.dev_rate = dev_rate
    player.potential = potential
    player.traits = traits
    player.college_stats = college_stats

    print(f"[DEBUG] Generated player: {name}, {position}, {college}")
    return player





# ------------------ COLLEGE DB ------------------

def generate_initial_college_db(num_per_class=450):
    college_db = []
    for year in range(1, 5):
        for _ in range(num_per_class):
            college_db.append(generate_college_player(year))
    return college_db

# ------------------ STATS + DEV ------------------

def generate_college_stats(position, dev_curve, potential, traits=None, team_quality=1.0):
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

        stats = {"games": random.randint(10, 13)}
        variability = 1.2 if "boom_bust" in traits else 1.0

        if position == "QB":
            stats.update({
                "pass_yards": int(random.uniform(2000, 4500) * scale * variability),
                "pass_tds": int(random.uniform(15, 40) * scale * variability),
                "interceptions": int(random.uniform(3, 15) * (1.5 - scale)),
                "completion_pct": round(random.uniform(55, 75) * scale, 1),
                "rush_yards": int(random.uniform(100, 800) * scale),
                "sacks": int(random.uniform(10, 35) * (1.5 - scale)),
            })
        elif position == "RB":
            stats.update({
                "rush_yards": int(random.uniform(600, 2000) * scale * variability),
                "tds": int(random.uniform(4, 20) * scale),
                "receptions": int(random.uniform(10, 50) * scale),
                "rec_yards": int(random.uniform(100, 600) * scale),
                "fumbles": int(random.uniform(0, 5) * (1.5 - scale)),
            })
        elif position == "WR":
            stats.update({
                "receptions": int(random.uniform(30, 110) * scale),
                "rec_yards": int(random.uniform(400, 1600) * scale),
                "tds": int(random.uniform(3, 15) * scale),
                "drops": int(random.uniform(0, 10) * (1.2 - scale)),
            })
        elif position == "LB":
            stats.update({
                "tackles": int(random.uniform(50, 140) * scale),
                "sacks": int(random.uniform(0, 10) * scale),
                "interceptions": int(random.uniform(0, 4) * scale),
                "forced_fumbles": int(random.uniform(0, 4) * scale),
            })
        elif position == "CB":
            stats.update({
                "tackles": int(random.uniform(20, 60) * scale),
                "interceptions": int(random.uniform(0, 5) * scale),
                "pass_deflections": int(random.uniform(5, 20) * scale),
                "tds_allowed": int(random.uniform(1, 10) * (1.5 - scale)),
            })

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

# ------------------ DRAFT DECLARE ------------------

def should_declare_early(player):
    potential = getattr(player, "potential", 70)
    traits = getattr(player, "traits", [])
    stats = player.college_stats.get("Junior", {})

    base = max(0, (potential - 70) / 30)
    stat_score = 0
    if stats.get("games", 0) >= 8:
        stat_sum = sum(
            v for k, v in stats.items()
            if isinstance(v, (int, float)) and k not in ["games", "sacks", "interceptions", "fumbles", "drops", "tds_allowed"]
        )
        stat_score = min(stat_sum / 1500.0, 0.5)

    bonus = 0
    if "boom_bust" in traits:
        bonus += 0.1
    if "injury_prone" in traits:
        bonus += 0.1

    chance = min(base + stat_score + bonus, 0.9)
    return random.random() < chance

def generate_draft_class(college_db):
    draft_class = []
    for player in college_db:
        year = getattr(player, "year_in_college", 0)
        if year == 4:
            player.is_draft_eligible = True
            player.declared_early = False
            draft_class.append(player)
        elif year == 3:
            if should_declare_early(player):
                player.is_draft_eligible = True
                player.declared_early = True
                draft_class.append(player)
            else:
                player.is_draft_eligible = False
                player.declared_early = False
        else:
            player.is_draft_eligible = False
            player.declared_early = False
    return draft_class
