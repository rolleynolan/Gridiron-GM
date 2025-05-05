import random
from gridiron_gm.engine.core.team import Team
from gridiron_gm.engine.core.player import Player
from gridiron_gm.engine.core.calendar import Calendar

class LeagueManager:
    def __init__(self):
        self.teams = []
        self.free_agents = []
        self.calendar = Calendar()
        self.standings = {}
        self.week_in_progress = False
        self.schedule = {}

    def add_team(self, team):
        self.teams.append(team)
        self.standings[team.team_name] = {"wins": 0, "losses": 0, "ties": 0}

    def remove_team(self, team):
        if team in self.teams:
            self.teams.remove(team)
            if team.team_name in self.standings:
                del self.standings[team.team_name]

    def advance_week(self):
        self.week_in_progress = True
        self.simulate_weekly_games()
        self.calendar.advance_week()
        self.week_in_progress = False

    def auto_fill_rosters(self, minimum_roster_size=53):
        for team in self.teams:
            while len(team.players) < minimum_roster_size and self.free_agents:
                player = self.free_agents.pop(0)
                team.add_player(player)

    def generate_schedule(self, weeks=14):
        team_names = [team.team_name for team in self.teams]
        num_teams = len(team_names)

        if num_teams % 2 != 0:
            team_names.append("BYE")

        total_weeks = weeks
        self.schedule = {}

        for week in range(1, total_weeks + 1):
            random.shuffle(team_names)
            weekly_matchups = []

            for i in range(0, len(team_names), 2):
                team1 = team_names[i]
                team2 = team_names[i + 1]
                if "BYE" not in (team1, team2):
                    weekly_matchups.append((team1, team2))

            self.schedule[week] = weekly_matchups

    def simulate_weekly_games(self):
        """Simulates all scheduled games for the current week."""
        current_week = self.calendar.current_week

        if current_week not in self.schedule:
            print(f"No games scheduled for Week {current_week}.")
            return

        print(f"📅 Simulating Week {current_week} games...")

        weekly_matchups = self.schedule[current_week]
        for team1_name, team2_name in weekly_matchups:
            team1 = self.get_team_by_name(team1_name)
            team2 = self.get_team_by_name(team2_name)

            if not team1 or not team2:
                continue

            winner = random.choice([team1, team2])
            loser = team2 if winner == team1 else team1

            # Random chance for a tie (~2%)
            is_tie = random.random() < 0.02

            if is_tie:
                self.standings[team1.team_name]["ties"] += 1
                self.standings[team2.team_name]["ties"] += 1
                print(f"🤝 {team1.team_name} and {team2.team_name} tie!")
            else:
                self.standings[winner.team_name]["wins"] += 1
                self.standings[loser.team_name]["losses"] += 1
                winner_score = random.randint(17, 35)
                loser_score = random.randint(10, winner_score - 3)
                print(f"🏆 {winner.team_name} defeats {loser.team_name} ({winner_score}-{loser_score})")

    def get_team_by_name(self, name):
        for team in self.teams:
            if team.team_name == name:
                return team
        return None

    def to_dict(self):
        return {
            "teams": [team.to_dict() for team in self.teams],
            "free_agents": [player.to_dict() for player in self.free_agents],
            "calendar": self.calendar.serialize(),
            "standings": self.standings,
            "schedule": self.schedule
        }

    @staticmethod
    def from_dict(data):
        league = LeagueManager()
        league.standings = data.get("standings", {})
        league.schedule = data.get("schedule", {})
        return league

    def __repr__(self):
        return f"LeagueManager | Teams: {len(self.teams)} | Free Agents: {len(self.free_agents)}"
