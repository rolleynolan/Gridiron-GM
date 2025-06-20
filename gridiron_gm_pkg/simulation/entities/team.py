import uuid
from typing import List, Dict, Optional
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.roster.depth_chart import generate_depth_chart

class Team:
    """
    Represents a football team in the simulation engine.

    Attributes:
        team_name (str): The official team name.
        city (str): The city the team is based in.
        abbreviation (str): Team abbreviation (e.g., "DAL").
        players (List[Player]): All players on the team.
        depth_chart (Dict[str, List[Player]]): Players grouped by position, sorted by overall.
        team_record (Dict[str, int]): Team's win/loss/tie record.
        playoff_seed (Optional[int]): Playoff seed if qualified.
        rebuild_mode (bool): Whether the team is in rebuild mode.
    """

    def __init__(
        self,
        team_name: str,
        city: str,
        abbreviation: str,
        conference: str = "Unknown",
        division: str = "Unknown",
        id: str | None = None,
        scouting_accuracy: float = 1.0,
    ) -> None:
        self.id: str = id if id is not None else str(uuid.uuid4())
        self.team_name: str = team_name
        self.city: str = city
        self.abbreviation: str = abbreviation
        self.conference: str = conference
        self.division: str = division
        self.scouting_accuracy: float = scouting_accuracy

        self.players: List[Player] = []
        self.depth_chart: Dict[str, List[Player]] = {}
        self.team_record: Dict[str, int] = {"wins": 0, "losses": 0, "ties": 0}
        self.playoff_seed: Optional[int] = None
        self.rebuild_mode: bool = False

        # Placeholders for future extensibility
        self.staff: Dict[str, object] = {}
        self.salary_cap: Optional[int] = None
        self.payroll: Optional[int] = None

        # Debug print to trace conference assignment
        print(f"[Team __init__] Created team: {self.abbreviation} | Conference: {self.conference} | ID: {self.id}")

    def add_player(self, player: Player, position_override: Optional[str] = None) -> None:
        """
        Adds a player to the team's roster and depth chart.

        Args:
            player (Player): The player to add.
            position_override (Optional[str]): If provided, overrides the player's position in the depth chart.
        """
        if player in self.players:
            return  # Prevent duplicates

        self.players.append(player)
        position = position_override or player.position

        if position not in self.depth_chart:
            self.depth_chart[position] = []
        self.depth_chart[position].append(player)
        self.depth_chart[position].sort(key=lambda p: getattr(p, "overall", 0), reverse=True)

    def remove_player(self, player: Player) -> None:
        """
        Removes a player from the roster and depth chart.

        Args:
            player (Player): The player to remove.
        """
        if player in self.players:
            self.players.remove(player)
        for position, player_list in self.depth_chart.items():
            if player in player_list:
                player_list.remove(player)

    def generate_depth_chart(self) -> None:
        """
        Rebuilds the depth chart by grouping and sorting players by position and overall.
        """
        self.depth_chart = generate_depth_chart(self)

    def get_starters(self) -> Dict[str, Player]:
        """
        Returns the top player at each position.

        Returns:
            Dict[str, Player]: Mapping of position to starting player.
        """
        return {pos: players[0] for pos, players in self.depth_chart.items() if players}

    def find_player_by_name(self, name: str) -> Optional[Player]:
        """
        Finds a player by name.

        Args:
            name (str): The player's name.

        Returns:
            Optional[Player]: The player if found, else None.
        """
        for player in self.players:
            if player.name == name:
                return player
        return None

    def to_dict(self) -> dict:
        """
        Serializes the team to a dictionary.

        Returns:
            dict: Serialized team data.
        """
        return {
            "id": self.id,
            "team_name": self.team_name,
            "city": self.city,
            "abbreviation": self.abbreviation,
            "conference": self.conference,
            "division": self.division,
            "scouting_accuracy": self.scouting_accuracy,
            "players": [player.to_dict() for player in self.players],
            "depth_chart": {pos: [p.name for p in players] for pos, players in self.depth_chart.items()},
            "team_record": self.team_record,
            "playoff_seed": self.playoff_seed,
            "rebuild_mode": self.rebuild_mode
        }

    @classmethod
    def from_dict(cls, data: dict) -> "Team":
        # Debug print: show the raw input dict
        # print(f"[DEBUG] Team.from_dict input: {data}")  # <-- COMMENT OUT THIS LINE
        # Only use "Nova" if the field is truly missing from the data (not just None or empty)
        if "conference" not in data:
            print(f"WARNING: Team '{data.get('team_name', 'Unnamed Team')}' ({data.get('abbreviation', 'UNK')}) missing conference in data source. Defaulting to 'Nova'.")
            conference = "Nova"
        else:
            conference = data.get("conference")
        team = cls(
            team_name=data.get("team_name", "Unnamed Team"),
            city=data.get("city", "Unknown City"),
            abbreviation=data.get("abbreviation", "UNK"),
            conference=conference,
            division=data.get("division", "Unknown"),
            id=data.get("id"),
        )
        team.players = [Player.from_dict(p) for p in data.get("players", [])]
        team.generate_depth_chart()
        team.team_record = data.get("team_record", {"wins": 0, "losses": 0, "ties": 0})
        team.playoff_seed = data.get("playoff_seed", None)
        team.rebuild_mode = data.get("rebuild_mode", False)
        return team

    def __repr__(self) -> str:
        return f"{self.team_name} | Roster Size: {len(self.players)}"
