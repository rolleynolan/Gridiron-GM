from gridiron_gm.engine.core.player import Player

class Team:
    def __init__(self, team_name):
        self.team_name = team_name
        self.players = []  # List of Player objects
        self.depth_chart = {}  # Dict of position -> list of Players
        self.team_record = {"wins": 0, "losses": 0, "ties": 0}
        self.playoff_seed = None

    def add_player(self, player, position_override=None):
        """Adds a player to the team's roster and optionally slots into the depth chart."""
        self.players.append(player)
        position = position_override or player.position

        if position not in self.depth_chart:
            self.depth_chart[position] = []

        self.depth_chart[position].append(player)

    def remove_player(self, player):
        """Removes a player from the roster and depth chart."""
        if player in self.players:
            self.players.remove(player)

        for position, player_list in self.depth_chart.items():
            if player in player_list:
                player_list.remove(player)

    def generate_depth_chart(self):
        """Auto-builds a basic depth chart by grouping players by position."""
        self.depth_chart = {}
        for player in self.players:
            position = player.position
            if position not in self.depth_chart:
                self.depth_chart[position] = []
            self.depth_chart[position].append(player)

    def to_dict(self):
        return {
            "team_name": self.team_name,
            "players": [player.to_dict() for player in self.players],
            "depth_chart": {pos: [p.name for p in players] for pos, players in self.depth_chart.items()},
            "team_record": self.team_record,
            "playoff_seed": self.playoff_seed
        }

    @staticmethod
    def from_dict(data):
        team = Team(team_name=data.get("team_name", "Unnamed Team"))
        # Players must be assigned separately after league/player generation
        team.team_record = data.get("team_record", {"wins": 0, "losses": 0, "ties": 0})
        team.playoff_seed = data.get("playoff_seed", None)
        return team

    def __repr__(self):
        return f"{self.team_name} | Roster Size: {len(self.players)}"
