import random

class PlayerDevelopmentEngine:
    def __init__(self, development_curve):
        self.development_curve = development_curve  # Dict by tier: e.g., {"Blue Chip": 1.5, "High": 1.3, "Mid": 1.1, "Low": 0.8}

    def develop_player(self, player):
        # Fetch the tier-specific multiplier
        multiplier = self.development_curve.get(player.dev_tier, 1.0)

        # Determine skill growth
        skill_growth = {}
        for skill, value in player.skills.items():
            growth = random.randint(0, 3)
            adjusted = min(100, value + int(growth * multiplier))
            skill_growth[skill] = adjusted

        # Update the player
        player.skills.update(skill_growth)
        player.overall = sum(player.skills.values()) // len(player.skills)
        return skill_growth

# Test logic (optional):
if __name__ == "__main__":
    class MockPlayer:
        def __init__(self):
            self.dev_tier = "High"
            self.skills = {"Speed": 75, "Strength": 72, "Agility": 78, "Football IQ": 80}
            self.overall = 76

    dev_engine = PlayerDevelopmentEngine({"Blue Chip": 1.5, "High": 1.3, "Mid": 1.1, "Low": 0.8})
    player = MockPlayer()
    print("Before:", player.skills, player.overall)
    growth = dev_engine.develop_player(player)
    print("Growth:", growth)
    print("After:", player.skills, player.overall)
