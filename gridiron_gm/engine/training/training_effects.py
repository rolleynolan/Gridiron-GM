# ==============================
# Updated File: training_effects.py
# ==============================

import random

# Base training gain ranges per activity
TRAINING_GAIN_RANGES = {
    "Film Session": (0, 2),
    "Gym Training": (1, 3),
    "Tactical Practice": (0, 2),
    "Special Teams": (0, 1),
    "Conditioning": (1, 2),
    "Rest": (0, 0),
    "Travel": (0, 0)
}

# Universal training trait effects
TRAINING_TRAIT_BOOSTS = {
    "Hard Worker": 0.10,
    "Lazy": -0.10,
    "Slacker": -0.05,
    "Workaholic": 0.15,
    "Quick Learner": 0.10,
    "Slow Learner": -0.10,
    "Self Motivated": 0.05,
    "Loner": -0.05,
    "Short Attention Span": -0.10,
}

# Special activity-specific training boosts
SPECIAL_ACTIVITY_BOOSTS = {
    "Film Junkie": ["Film Session", "Tactical Practice"],
    "Gym Rat": ["Gym Training", "Conditioning"]
}

# Fatigue impact on training
FATIGUE_PENALTY_FACTOR = 0.01  # Each point of fatigue reduces training effectiveness by 1%
WORKAHOLIC_FATIGUE_BONUS = 0.10  # Additional fatigue penalty for Workaholics

def simulate_weekly_training_effects(team):
    for player in team.roster:
        for day, activity in team.training_schedule.schedule.items():
            min_gain, max_gain = TRAINING_GAIN_RANGES.get(activity, (0, 0))
            base_gain = random.uniform(min_gain, max_gain)

            # Fatigue penalty calculation
            fatigue_penalty = player.fatigue * FATIGUE_PENALTY_FACTOR

            # Workaholic extra fatigue
            if "Workaholic" in player.traits.get("training", []):
                fatigue_penalty += WORKAHOLIC_FATIGUE_BONUS

            # Endurance Machine reduces fatigue impact
            if "Endurance Machine" in player.traits.get("physical", []):
                fatigue_penalty *= 0.75  # 25% reduction

            # Iron Man further reduces fatigue impact
            if "Iron Man" in player.traits.get("physical", []):
                fatigue_penalty *= 0.90  # additional 10% reduction

            gain_after_fatigue = max(0, base_gain * (1 - fatigue_penalty))

            # Trait bonuses
            total_trait_bonus = 0.0

            # Universal training trait effects
            for trait in player.traits.get("training", []):
                total_trait_bonus += TRAINING_TRAIT_BOOSTS.get(trait, 0)

            # Special activity-based trait bonuses
            for trait, activities in SPECIAL_ACTIVITY_BOOSTS.items():
                if trait in player.traits.get("training", []) and activity in activities:
                    total_trait_bonus += 0.05  # +5% bonus

            # Physical traits affecting training
            if "High Motor" in player.traits.get("physical", []):
                total_trait_bonus += 0.05  # 5% more effort

            if "Musclebound" in player.traits.get("physical", []):
                if activity in ["Gym Training", "Conditioning"]:
                    total_trait_bonus += 0.05  # better strength training

            if "Slow Twitch" in player.traits.get("physical", []):
                if activity in ["Conditioning", "Special Teams"]:
                    total_trait_bonus -= 0.05  # worse speed/agility training

            # Final adjusted gain
            final_gain = gain_after_fatigue * (1 + total_trait_bonus)

            # Apply gain to random skills
            if player.skills:
                skills_to_train = random.sample(list(player.skills.keys()), k=min(2, len(player.skills)))
                for skill in skills_to_train:
                    player.skills[skill] = min(100, player.skills[skill] + final_gain)

            # Clumsy players rare training loss
            if "Clumsy" in player.traits.get("physical", []):
                if random.random() < 0.01:  # 1% chance daily
                    lost_skill = random.choice(list(player.skills.keys()))
                    player.skills[lost_skill] = max(40, player.skills[lost_skill] - random.uniform(0.5, 1.5))

        # Recalculate overall after week
        if player.skills:
            player.overall = sum(player.skills.values()) // len(player.skills)
