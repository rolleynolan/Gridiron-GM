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
        chart = {
            "QB": [], "RB": [], "WR": [], "TE": [],
            "LT": [], "LG": [], "C": [], "RG": [], "RT": [],
            "DE": [], "DT": [], "LB": [], "CB": [],
            "S": [], "K": [], "P": []
        }
        healthy_players = get_healthy_players(self.team.players)
        for player in healthy_players:
            pos = getattr(player, "position", None)
            if pos in chart:
                chart[pos].append(player)
        for pos in chart:
            chart[pos] = sorted(chart[pos], key=lambda p: getattr(p, "overall", 0), reverse=True)
        return chart

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

class SubstitutionManagerV2:
    """
    Handles substitutions for a team, returning a lineup by scheme and managing fatigue/bench logic.
    """
    def __init__(self, depth_chart: Dict[str, List[Any]]):
        self.depth_chart = depth_chart

    def get_active_lineup_with_bench_log(
        self,
        formation: Dict[str, int],
        offense: Dict[str, Any],
        fatigue_log: List[str],
        scheme: Dict[str, int]
    ) -> Tuple[Dict[str, Any], List[str]]:
        """
        Returns the lineup for the given formation, making fatigue-aware substitutions as needed.
        Injured players are never included.
        """
        lineup = {}
        bench_log = []
        for pos, count in scheme.items():
            depth_list = [p for p in self.depth_chart.get(pos, []) if not getattr(p, "is_injured", False)]
            chosen = []
            for i in range(count):
                if i < len(depth_list):
                    player = depth_list[i]
                    if getattr(player, "fatigue", 0.0) >= 0.9 and len(depth_list) > i + 1:
                        backup = depth_list[i + 1]
                        if getattr(backup, "fatigue", 0.0) < 0.9:
                            chosen.append(backup)
                            bench_log.append(f"{pos}: {getattr(player, 'name', 'Unknown')} → {getattr(backup, 'name', 'Unknown')}")
                        else:
                            chosen.append(player)
                    else:
                        chosen.append(player)
            for idx, player in enumerate(chosen, 1):
                key = f"{pos}{idx}" if count > 1 else pos
                lineup[key] = player
        return lineup, bench_log

def generate_depth_chart(team):
    """
    Stub: Generates a depth chart for the given team.
    """
    # TODO: Implement real logic
    return {}

# When generating a depth chart or active lineup:
# Example team object for demonstration; replace with your actual team object as needed.
team = {"roster": []}  # Replace with actual team data
healthy_roster = get_healthy_players(team.get("roster", []))
depth_chart = generate_depth_chart(healthy_roster)
# Or, if your sub_manager uses the full roster, pass only healthy players
