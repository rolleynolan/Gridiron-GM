# engine/game_engine.py
import random

class GameEngine:
    def __init__(self):
        self.log = []

    def simulate_game(self, team1, team2):
        score1 = self.calculate_team_score(team1)
        score2 = self.calculate_team_score(team2)

        result = {
            "home_team": team1.name,
            "away_team": team2.name,
            "home_score": score1,
            "away_score": score2,
            "winner": team1.name if score1 > score2 else team2.name
        }

        self.log.append(result)
        return result

    def calculate_team_score(self, team):
        total = 0
        for player in team.roster:
            if player.fatigue > 60:
                penalty = player.fatigue * 0.1
            else:
                penalty = 0
            contribution = player.overall - penalty
            if "Clutch Performer" in player.traits.get("gameday", []):
                contribution += 3  # Small boost
            total += contribution
        avg_score = total / len(team.roster)
        random_factor = random.randint(-10, 10)
        return max(0, int(avg_score / 2 + random_factor))
