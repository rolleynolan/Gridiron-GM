import os
import random
import datetime
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player import player_generation


class PlayerGenerator:
    """Generate professional or college players with basic info."""

    def __init__(self):
        base_dir = os.path.dirname(os.path.abspath(__file__))
        config_dir = os.path.join(base_dir, "..", "..", "config")
        self.first_names = self._load_list(os.path.join(config_dir, "male_first_names.txt"))
        self.last_names = self._load_list(os.path.join(config_dir, "last_names.txt"))
        self.colleges = self._load_list(os.path.join(config_dir, "fbs_colleges.txt"))
        self.cities = self._load_list(os.path.join(config_dir, "cities.txt"))

    def _load_list(self, path):
        try:
            with open(path, encoding="utf-8") as f:
                return [line.strip() for line in f if line.strip()]
        except Exception:
            return []

    def _random_name(self) -> str:
        return f"{random.choice(self.first_names)} {random.choice(self.last_names)}"

    def generate_dob(self, age: int) -> datetime.date:
        today = datetime.date.today()
        offset = random.randint(0, 364)
        return today - datetime.timedelta(days=age * 365 + offset)

    def _jersey_number_for_position(self, position: str) -> int:
        ranges = {
            "QB": range(1, 20),
            "RB": range(20, 50),
            "WR": list(range(10, 20)) + list(range(80, 90)),
            "TE": list(range(40, 50)) + list(range(80, 90)),
            "OL": range(50, 80),
            "DL": list(range(50, 80)) + list(range(90, 100)),
            "LB": list(range(40, 60)) + list(range(90, 100)),
            "CB": range(20, 40),
            "S": range(20, 40),
            "K": range(1, 20),
            "P": range(1, 20),
        }
        choices = ranges.get(position, range(1, 100))
        return random.choice(list(choices))

    # ------------------------------------------------------------------
    # Public generation helpers
    def generate_player(self, position: str) -> Player:
        """Create a professional player for league rosters."""
        return self.generate_pro_player(position)

    def generate_pro_player(self, position: str, age: int | None = None) -> Player:
        if age is None:
            age = random.randint(21, 35)
        base = player_generation.generate_pro_player(position, age)
        dob = self.generate_dob(age)
        name = self._random_name()
        college = random.choice(self.colleges)
        birth_location = random.choice(self.cities)
        jersey = self._jersey_number_for_position(position)
        overall = int(sum(base["attributes"].values()) / len(base["attributes"]))
        data = {
            "name": name,
            "position": position,
            "age": age,
            "dob": dob,
            "college": college,
            "birth_location": birth_location,
            "jersey_number": jersey,
            "overall": overall,
            "attributes": {"core": {}, "position_specific": base["attributes"]},
            "dna": base["dna"].to_dict(),
        }
        player = Player.from_dict(data)
        player.origin = base["origin"]
        player.projected_potential = random.randint(overall, min(99, overall + random.randint(5, 20)))
        return player

    def generate_college_player(self, position: str, year_in_college: int = 1) -> Player:
        age = random.randint(18, 22)
        base = player_generation.generate_college_player(position, age)
        dob = self.generate_dob(age)
        name = self._random_name()
        college = random.choice(self.colleges)
        birth_location = random.choice(self.cities)
        jersey = self._jersey_number_for_position(position)
        overall = int(sum(base["attributes"].values()) / len(base["attributes"]))
        data = {
            "name": name,
            "position": position,
            "age": age,
            "dob": dob,
            "college": college,
            "birth_location": birth_location,
            "jersey_number": jersey,
            "overall": overall,
            "attributes": {"core": {}, "position_specific": base["attributes"]},
            "dna": base["dna"].to_dict(),
        }
        player = Player.from_dict(data)
        player.origin = base["origin"]
        player.year_in_college = year_in_college
        player.projected_potential = random.randint(overall + 5, min(99, overall + random.randint(15, 30)))
        return player
