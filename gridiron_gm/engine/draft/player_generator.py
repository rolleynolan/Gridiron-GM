

import random
import datetime
import json
from gridiron_gm.engine.core.player import Player
# engine/draft/player_generator.py (near top)

CITIES = [
    {"city": "Chicago", "state": "Illinois"},
    {"city": "Dallas", "state": "Texas"},
    {"city": "Miami", "state": "Florida"},
    {"city": "New York", "state": "New York"},
    {"city": "San Francisco", "state": "California"},
    {"city": "Nashville", "state": "Tennessee"},
    {"city": "Las Vegas", "state": "Nevada"},
    {"city": "Atlanta", "state": "Georgia"},
    {"city": "Boston", "state": "Massachusetts"},
    {"city": "Houston", "state": "Texas"},
    {"city": "Los Angeles", "state": "California"},
    {"city": "Philadelphia", "state": "Pennsylvania"},
    {"city": "Phoenix", "state": "Arizona"},
    {"city": "Detroit", "state": "Michigan"},
    {"city": "Seattle", "state": "Washington"},
    {"city": "Denver", "state": "Colorado"},
    {"city": "Cleveland", "state": "Ohio"},
    {"city": "Orlando", "state": "Florida"},
    {"city": "Minneapolis", "state": "Minnesota"},
    {"city": "Baltimore", "state": "Maryland"},
    {"city": "New Orleans", "state": "Louisiana"},
    {"city": "Indianapolis", "state": "Indiana"},
    {"city": "Cincinnati", "state": "Ohio"},
    {"city": "Kansas City", "state": "Missouri"},
    {"city": "Charlotte", "state": "North Carolina"},
    {"city": "Tampa Bay", "state": "Florida"},
    {"city": "Pittsburgh", "state": "Pennsylvania"},
    {"city": "Washington", "state": "D.C."},
    {"city": "Green Bay", "state": "Wisconsin"},
    {"city": "Buffalo", "state": "New York"},
    {"city": "San Diego", "state": "California"},
    {"city": "Portland", "state": "Oregon"}
]

# Load name and location data
with open("gridiron_gm/data/male_first_names.txt", encoding="utf-8") as f:
    PLAYER_FIRST_NAMES = [line.strip() for line in f if line.strip()]
with open("gridiron_gm/data/last_names.txt", encoding="utf-8") as f:
    LAST_NAMES = [line.strip() for line in f if line.strip()]
with open("gridiron_gm/data/enriched_us_cities.json", encoding="utf-8") as f:
    CITIES = json.load(f)
with open("gridiron_gm/data/fbs_colleges.txt", encoding="utf-8") as f:
    COLLEGES = [line.strip() for line in f if line.strip()]

# Trait Pools
TRAINING_TRAITS = ["Hard Worker", "Lazy", "Slacker", "Workaholic", "Quick Learner", "Slow Learner", "Self Motivated", "Loner", "Short Attention Span", "Film Junkie"]
GAMEDAY_TRAITS = ["Big Game Hunter", "Cold Under Pressure", "Momentum Player", "Comeback Artist", "Drive Killer"]
PHYSICAL_TRAITS = ["Iron Man", "Glass Cannon", "Trench Monster", "Flexible", "Explosive First Step", "High Motor", "Top-End Speedster", "Endurance Machine", "Musclebound", "Fragile Bones", "Long Strider", "Compact Build", "Springy Legs", "Slow Twitch", "Undersized", "Thick Frame", "Tendonitis Risk", "Bulldozer", "Quick Recovery", "Clumsy", "Physical Freak"]

# Position-Restricted Traits
POSITION_SPECIFIC_PHYSICAL_TRAITS = {
    "Trench Monster": ["OL", "DL", "LB"],
    "Explosive First Step": ["DL", "EDGE", "RB", "WR"],
    "Top-End Speedster": ["WR", "RB", "CB"],
    "Musclebound": ["OL", "DL", "FB"],
    "Long Strider": ["WR", "TE", "CB"],
    "Compact Build": ["RB", "LB", "FS", "SS"],
    "Springy Legs": ["WR", "TE", "CB", "FS", "SS"],
    "Undersized": ["WR", "CB", "FS", "RB"],
    "Thick Frame": ["OL", "DL", "LB"],
    "Bulldozer": ["RB", "FB", "LB"],
    "Comeback Artist": ["QB"],  # Gameday trait
    "Drive Killer": ["QB", "RB", "WR"]  # Gameday trait
}

# Base position groupings for Height/Weight norms
POSITION_HEIGHT_WEIGHT_BASES = {
    "QB": (74, 220),  # 6'2", 220 lbs
    "RB": (71, 210),
    "WR": (73, 205),
    "TE": (76, 250),
    "OL": (76, 315),
    "DL": (75, 290),
    "LB": (74, 245),
    "CB": (71, 195),
    "FS": (72, 200),
    "SS": (72, 210),
    "EDGE": (75, 255)
}

class PlayerGenerator:
    @staticmethod
    def generate_player(position):
        name = random.choice(PLAYER_FIRST_NAMES) + " " + random.choice(LAST_NAMES)
        age = random.randint(20, 24)
        dob = datetime.datetime.now() - datetime.timedelta(days=age * 365 + random.randint(0, 364))
        college = random.choice(COLLEGES)
        birth_location = random.choice(CITIES)["city"]
        jersey_number = random.randint(1, 99)

        # Set base height and weight
        base_height, base_weight = POSITION_HEIGHT_WEIGHT_BASES.get(position, (72, 220))
        height = base_height + random.randint(-2, 2)
        weight = base_weight + random.randint(-15, 15)

        # Set base overall rating
        overall = random.randint(60, 85)

        player = Player(name, position, age, dob, college, birth_location, jersey_number, overall)

        # Assign Skills
        player.skills = {
            "speed": random.randint(60, 90),
            "strength": random.randint(55, 85),
            "agility": random.randint(60, 90),
            "awareness": random.randint(55, 85),
            "jumping": random.randint(60, 90),
            "acceleration": random.randint(60, 90),
        }

        # Assign Traits
        PlayerGenerator.assign_traits(player)

        # Apply Physical Freak adjustments immediately if assigned
        if "Physical Freak" in player.traits.get("physical", []):
            player.skills["speed"] = int(player.skills["speed"] * 1.10)
            player.skills["strength"] = int(player.skills["strength"] * 1.10)
            player.skills["acceleration"] = int(player.skills["acceleration"] * 1.10)
            player.skills["jumping"] = int(player.skills["jumping"] * 1.10)
            height += random.randint(2, 4)  # +2 to +4 inches
            weight = int(weight * 1.10)     # +10% heavier

        # Attach finalized height/weight
        player.height = height
        player.weight = weight

        return player

    @staticmethod
    def assign_traits(player):
        # Training Trait (60% chance)
        if random.random() < 0.6:
            trait = random.choice(TRAINING_TRAITS)
            player.add_trait("training", trait)

        # Gameday Trait (40% chance)
        if random.random() < 0.4:
            trait = random.choice(GAMEDAY_TRAITS)
            # Respect position restrictions
            if trait in POSITION_SPECIFIC_PHYSICAL_TRAITS:
                allowed_positions = POSITION_SPECIFIC_PHYSICAL_TRAITS[trait]
                if player.position in allowed_positions:
                    player.add_trait("gameday", trait)
            else:
                player.add_trait("gameday", trait)

        # Physical Trait (30% chance total; <1% chance for Physical Freak)
        if random.random() < 0.3:
            if random.random() < 0.01:  # Physical Freak chance
                player.add_trait("physical", "Physical Freak")
            else:
                trait = random.choice(PHYSICAL_TRAITS)
                # Position-specific restrictions
                if trait in POSITION_SPECIFIC_PHYSICAL_TRAITS:
                    allowed_positions = POSITION_SPECIFIC_PHYSICAL_TRAITS[trait]
                    if player.position in allowed_positions:
                        player.add_trait("physical", trait)
                else:
                    player.add_trait("physical", trait)
