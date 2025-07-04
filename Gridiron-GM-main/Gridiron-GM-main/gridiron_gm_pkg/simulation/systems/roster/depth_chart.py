from typing import List, Dict, Any, Tuple

def get_healthy_players(players: List[Any]) -> List[Any]:
    """Return only players who are not marked as injured."""
    return [p for p in players if not getattr(p, "is_injured", False)]

class DepthChartManager:
    """
    Handles a team's depth chart (player ordering by position),
    starter assignment, and retrieval for simulation or UI.
    """
    def __init__(self, team: Dict[str, Any]):
        self.team = team
        self.depth_chart = self._initialize_chart()

    def _initialize_chart(self) -> Dict[str, List[Any]]:
        base_chart = {
            "QB": [], "RB": [], "WR": [], "TE": [],
            "LT": [], "LG": [], "C": [], "RG": [], "RT": [],
            "DE": [], "DT": [], "LB": [], "CB": [],
            "S": [], "K": [], "P": []
        }

        generated = generate_depth_chart(self.team)

        # Merge the generated chart into the base so predefined positions always exist
        for pos in base_chart:
            if pos in generated:
                base_chart[pos] = generated[pos]

        # Include any additional positions that may exist on the roster
        for pos, players in generated.items():
            if pos not in base_chart:
                base_chart[pos] = players

        return base_chart

    def get_starters_by_scheme(self, scheme: Dict[str, int]) -> Dict[str, List[Any]]:
        """
        Returns dictionary of starters for each position by scheme (e.g., {'RB':2, 'WR':3}).
        """
        starters = {}
        for pos, count in scheme.items():
            starters[pos] = self.depth_chart.get(pos, [])[:count]
        return starters

    def auto_assign_depth_chart(self) -> None:
        self.depth_chart = self._initialize_chart()

    def print_depth_chart(self) -> None:
        print(f"Depth Chart for {getattr(self.team, 'team_name', 'Unknown Team')}")
        for position, players in self.depth_chart.items():
            print(f"{position}:")
            for i, player in enumerate(players):
                name = getattr(player, "name", "Unknown")
                overall = getattr(player, "overall", 0)
                print(f"  {i+1}. {name} (OVR: {overall})")


def generate_depth_chart(team: Any) -> Dict[str, List[Any]]:
    """Generate a depth chart from a team or list of players.

    This utility accepts either a Team object/dict with ``roster`` or
    ``players`` attribute or a direct list of players. Injured players are
    filtered out and each position list is sorted by ``overall`` rating in
    descending order.
    """
    # Determine the list of players from the provided object
    if isinstance(team, list):
        players = team
    else:
        players = getattr(team, "roster", None)
        if players is None:
            players = getattr(team, "players", [])

    # Only include healthy players
    players = get_healthy_players(players)

    depth_chart: Dict[str, List[Any]] = {}
    for player in players:
        pos = getattr(player, "position", None)
        if not pos:
            continue
        depth_chart.setdefault(pos, []).append(player)

    # Sort players at each position by overall rating
    for pos in depth_chart:
        depth_chart[pos].sort(key=lambda p: getattr(p, "overall", 0), reverse=True)

    return depth_chart

