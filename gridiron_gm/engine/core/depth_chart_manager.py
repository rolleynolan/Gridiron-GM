class DepthChartManager:
    def __init__(self, team):
        self.team = team
        self.depth_chart = {
            "QB": [],
            "RB": [],
            "WR": [],
            "TE": [],
            "OL": [],
            "DL": [],
            "LB": [],
            "CB": [],
            "S": [],
            "K": [],
            "P": []
        }

    def auto_assign_depth_chart(self):
        """
        Automatically assign players to depth chart based on overall rating.
        """
        for position in self.depth_chart:
            players_at_position = [p for p in self.team.roster if p.position == position]
            sorted_players = sorted(players_at_position, key=lambda p: p.overall, reverse=True)
            self.depth_chart[position] = sorted_players

    def get_starters(self):
        """
        Return a dictionary of starting players by position (just the top ranked).
        """
        starters = {}
        for pos, players in self.depth_chart.items():
            if players:
                starters[pos] = players[0]
        return starters

    def print_depth_chart(self):
        print(f"Depth Chart for {self.team.name}")
        for position, players in self.depth_chart.items():
            print(f"{position}:")
            for i, player in enumerate(players):
                print(f"  {i+1}. {player.name} (OVR: {player.overall})")
