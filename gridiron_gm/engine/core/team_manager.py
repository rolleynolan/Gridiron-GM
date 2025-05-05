from gridiron_gm.engine.core.player import Player
from gridiron_gm.engine.core.coach import Coach

class Team:
    def __init__(self, name, city, abbreviation):
        self.name = name
        self.city = city
        self.abbreviation = abbreviation
        self.roster = []  # List of Player objects
        self.ir_list = []  # List of Players on Injured Reserve
        self.head_coach = None  # Coach object
        self.record = {"wins": 0, "losses": 0, "ties": 0}
        self.user_controlled = False  # Is this the user's team

    def add_player(self, player):
        self.roster.append(player)

    def remove_player(self, player):
        if player in self.roster:
            self.roster.remove(player)

    def place_player_on_ir(self, player):
        """Move an injured player to the IR list if eligible (8+ weeks out)."""
        if player in self.roster and player.weeks_out >= 8:
            self.roster.remove(player)
            self.ir_list.append(player)
            player.on_injured_reserve = True
            player.is_injured = True  # Player is now injured
            print(f"{player.name} has been placed on Injured Reserve.")

    def remove_player_from_ir(self, player):
        """Return a player from IR once recovered."""
        if player in self.ir_list:
            self.ir_list.remove(player)
            self.roster.append(player)
            player.on_injured_reserve = False
            player.is_injured = False  # Player is no longer injured
            print(f"{player.name} has been removed from Injured Reserve and returned to the roster.")

    def set_head_coach(self, coach):
        self.head_coach = coach

    def to_dict(self):
        return {
            "name": self.name,
            "city": self.city,
            "abbreviation": self.abbreviation,
            "roster": [player.to_dict() for player in self.roster],
            "ir_list": [player.to_dict() for player in self.ir_list],
            "head_coach": self.head_coach.to_dict() if self.head_coach else None,
            "record": self.record,
            "user_controlled": self.user_controlled
        }

    @staticmethod
    def from_dict(data):
        team = Team(
            name=data["name"],
            city=data["city"],
            abbreviation=data["abbreviation"]
        )

        # Load Players
        team.roster = [Player.from_dict(p) for p in data.get("roster", [])]
        team.ir_list = [Player.from_dict(p) for p in data.get("ir_list", [])]

        # Load Coach
        coach_data = data.get("head_coach")
        if coach_data:
            team.head_coach = Coach.from_dict(coach_data)

        # Load other fields
        team.record = data.get("record", {"wins": 0, "losses": 0, "ties": 0})
        team.user_controlled = data.get("user_controlled", False)

        return team

    def __repr__(self):
        return f"{self.city} {self.name} ({self.abbreviation}) - Record: {self.record['wins']}-{self.record['losses']}-{self.record['ties']}"
