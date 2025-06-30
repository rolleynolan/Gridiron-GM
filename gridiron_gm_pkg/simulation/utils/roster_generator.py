import random
import datetime
from gridiron_gm_pkg.simulation.entities.player import Player
<<<<<<< HEAD
=======
from gridiron_gm_pkg.config.injury_catalog import INJURY_CATALOG
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802


class RosterGenerator:
    POSITIONS = {
        "QB": (2, 3),
        "RB": (3, 4),
        "WR": (4, 6),
        "TE": (2, 3),
        "LT": (2, 3),
        "LG": (2, 3),
        "C": (2, 3),
        "RG": (2, 3),
        "RT": (2, 3),
        "DL": (6, 8),
        "LB": (5, 7),
        "CB": (4, 5),
        "S": (2, 3),
        "K": (1, 1),
        "P": (1, 1)
    }

    def __init__(self, player_generator):
        self.player_generator = player_generator
<<<<<<< HEAD
=======
        self.position_base = {
            "LT": "OL",
            "LG": "OL",
            "C": "OL",
            "RG": "OL",
            "RT": "OL",
            "DE": "DL",
            "DT": "DL",
        }

    def _assign_injury_history(self, player: Player, max_entries: int = 2) -> None:
        """Randomly attach past injuries to a player."""
        history = []
        for _ in range(random.randint(0, max_entries)):
            name, data = random.choice(list(INJURY_CATALOG.items()))
            history.append({
                "name": name,
                "weeks_out": random.randint(*data.get("weeks", (1, 1))),
                "severity": data.get("severity", "Minor"),
            })
        player.injury_history = history
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802

    def generate_team_roster(self):
        roster = []
        for position, (min_count, max_count) in self.POSITIONS.items():
            count = random.randint(min_count, max_count)
            for _ in range(count):
<<<<<<< HEAD
                player = self.player_generator.generate_player(position)
=======
                base_pos = self.position_base.get(position, position)
                player = self.player_generator.generate_player(base_pos)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
                player.age = random.randint(22, 34)  # Veterans
                player.dob = self.player_generator.generate_dob(player.age)
                player.overall = random.randint(65, 85)
                player.projected_potential = random.randint(player.overall, 90)
                player.contract = self.generate_contract(player.overall)
<<<<<<< HEAD
=======
                self._assign_injury_history(player)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
                roster.append(player)
        return roster

    def generate_free_agents(self, count=100):
        free_agents = []
        for _ in range(count):
            position = random.choice(list(self.POSITIONS.keys()))
<<<<<<< HEAD
            player = self.player_generator.generate_player(position)
=======
            base_pos = self.position_base.get(position, position)
            player = self.player_generator.generate_player(base_pos)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
            player.age = random.randint(22, 34)
            player.dob = self.player_generator.generate_dob(player.age)
            player.overall = random.randint(60, 82)
            player.projected_potential = random.randint(player.overall, 88)
            player.contract = self.generate_contract(player.overall)
<<<<<<< HEAD
=======
            self._assign_injury_history(player)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
            free_agents.append(player)
        return free_agents

    def generate_contract(self, overall_rating):
        base_salary = 0.5 + ((overall_rating - 60) * 0.1)  # Very rough salary scale
        years = random.randint(1, 3)
        return {
            "years": years,
            "salary_per_year": round(base_salary, 2),
<<<<<<< HEAD
            "rookie": False
=======
            "rookie": False,
            "years_left": years,
            "expiring": False,
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        }
