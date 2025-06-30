<<<<<<< HEAD
import random

class Contract:
    def __init__(self, years, total_value, guaranteed):
        self.years = years
        self.total_value = total_value  # in millions
        self.guaranteed = guaranteed    # in millions

    def __repr__(self):
        return f"{self.years} yrs, ${self.total_value}M total, ${self.guaranteed}M guaranteed"

class ContractEngine:
    def __init__(self, inflation_rate=0.05):
        self.inflation_rate = inflation_rate

    def calculate_offer(self, player, team_context):
        """
        player: a Player object with skills, traits, position, etc.
        team_context: dict with team cap space, urgency, etc.
        """
        base = self.get_base_value(player)
        inflation_adjusted = base * (1 + self.inflation_rate)
        guaranteed = round(inflation_adjusted * random.uniform(0.5, 0.9), 2)

        years = self.determine_years(player)
        total = round(inflation_adjusted * years, 2)
        return Contract(years, total, guaranteed)

    def get_base_value(self, player):
        # Base salary from overall and positional value
        base = player.overall * 0.4  # crude multiplier
        age_penalty = max(0, (player.age - 30) * 0.03)
        trait_bonus = 1.0 + (0.05 * len(player.traits.get("training", [])))

        return round(base * (1 - age_penalty) * trait_bonus, 2)

    def determine_years(self, player):
        if player.age < 26:
            return random.choice([4, 5])
        elif player.age < 30:
            return random.choice([3, 4])
        else:
            return random.choice([1, 2])

    def is_offer_acceptable(self, player, offer, market_value):
        """
        Checks if the player would accept the offer vs their perceived market value.
        """
        tolerance = 0.9  # Player will accept ~90% or higher
        return (offer.total_value / offer.years) >= (market_value * tolerance)
=======
import random

class Contract:
    def __init__(self, years, total_value, guaranteed):
        self.years = years
        self.total_value = total_value  # in millions
        self.guaranteed = guaranteed    # in millions

    def __repr__(self):
        return f"{self.years} yrs, ${self.total_value}M total, ${self.guaranteed}M guaranteed"

class ContractEngine:
    def __init__(self, inflation_rate=0.05):
        self.inflation_rate = inflation_rate

    def calculate_offer(self, player, team_context):
        """
        player: a Player object with skills, traits, position, etc.
        team_context: dict with team cap space, urgency, etc.
        """
        base = self.get_base_value(player)
        inflation_adjusted = base * (1 + self.inflation_rate)
        guaranteed = round(inflation_adjusted * random.uniform(0.5, 0.9), 2)

        years = self.determine_years(player)
        total = round(inflation_adjusted * years, 2)
        return Contract(years, total, guaranteed)

    def get_base_value(self, player):
        # Base salary from overall and positional value
        base = player.overall * 0.4  # crude multiplier
        age_penalty = max(0, (player.age - 30) * 0.03)
        trait_bonus = 1.0 + (0.05 * len(player.traits.get("training", [])))

        return round(base * (1 - age_penalty) * trait_bonus, 2)

    def determine_years(self, player):
        if player.age < 26:
            return random.choice([4, 5])
        elif player.age < 30:
            return random.choice([3, 4])
        else:
            return random.choice([1, 2])

    def is_offer_acceptable(self, player, offer, market_value):
        """
        Checks if the player would accept the offer vs their perceived market value.
        """
        tolerance = 0.9  # Player will accept ~90% or higher
        return (offer.total_value / offer.years) >= (market_value * tolerance)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
