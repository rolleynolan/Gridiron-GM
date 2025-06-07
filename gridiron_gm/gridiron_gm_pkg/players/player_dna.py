import random
from typing import Dict, Iterable, Optional

# === DNA MUTATIONS ===
DNA_MUTATIONS: Dict[str, Dict] = {
    "Generational Talent": {
        "attribute_cap_boosts": {"physical": 0.1, "mental": 0.1, "technical": 0.1},
        "dev_speed_multiplier": 1.25,
    },
    "Physical Freak": {
        "attribute_cap_boosts": {"physical": 0.2},
        "dev_speed_multiplier": 1.00,
    },
    "Football Savant": {
        "attribute_cap_boosts": {"mental": 0.15},
        "awareness_growth_multiplier": 2.0,
    },
    "Skill Machine": {
        "attribute_cap_boosts": {"technical": 0.15},
        "penalty_reduction": True,
    },
    "Late Unlocker": {
        "delayed_cap_unlock": True,
        "unlock_conditions": ["breakout_season", "coach_quality"],
    },
    "Battle-Hardened": {
        "injury_regression_reduction": True,
        "morale_loss_protection": True,
    },
    "Primetime Performer": {
        "clutch_game_performance_boost": True,
    },
}

# === PLAYER TRAITS ===
PLAYER_TRAITS = [
    "Leader",
    "Spotlight Seeker",
    "Mentor",
    "Hot-Headed",
    "Low Motor",
    "Hard Worker",
    "Team Player",
    "Selfish",
    "Resilient",
    "Ego Driven",
]


class PlayerDNA:
    """Procedural growth and trait profile for a player."""

    def __init__(self) -> None:
        self.growth_type = self._choose_growth_type()
        self.regression_type = self._choose_regression_type()
        self.dev_speed = random.uniform(0.75, 1.25)
        self.dev_focus = self._generate_dev_focus_weights()
        self.attribute_caps = self._generate_attribute_caps()
        self.mutation = self._assign_mutation()
        self.traits = self._assign_traits()

    def _choose_growth_type(self) -> str:
        return random.choice(["early_peak", "late_bloomer", "rollercoaster", "flatline"])

    def _choose_regression_type(self) -> str:
        return random.choice(["early_decline", "late_decline", "injury_decline", "gradual_decline"])

    def _generate_dev_focus_weights(self) -> Dict[str, float]:
        weights = [random.uniform(0.25, 0.45) for _ in range(3)]
        total = sum(weights)
        return {
            "physical": weights[0] / total,
            "mental": weights[1] / total,
            "technical": weights[2] / total,
        }

    def _generate_attribute_caps(self) -> Dict[str, int]:
        return {
            "speed": random.randint(80, 99),
            "strength": random.randint(75, 95),
            "awareness": random.randint(70, 95),
            "agility": random.randint(75, 95),
            "throw_accuracy_short": random.randint(60, 95),
            "tackle_dl": random.randint(60, 95),
            "route_running_short": random.randint(60, 95),
        }

    def _assign_mutation(self) -> Optional[str]:
        roll = random.random()
        if roll <= 0.05:
            return random.choice(list(DNA_MUTATIONS.keys()))
        return None

    def _assign_traits(self) -> Iterable[str]:
        trait_count = random.choices([0, 1, 2, 3], weights=[0.1, 0.4, 0.35, 0.15])[0]
        return random.sample(PLAYER_TRAITS, trait_count)

    def apply_mutation_effects(self, attr_caps: Dict[str, int]) -> Dict[str, int]:
        """Apply mutation bonuses to attribute caps."""
        if not self.mutation:
            return attr_caps

        mutation = DNA_MUTATIONS[self.mutation]
        new_caps = attr_caps.copy()

        if "attribute_cap_boosts" in mutation:
            for group, boost in mutation["attribute_cap_boosts"].items():
                for attr in attr_caps:
                    if group in attr:
                        new_caps[attr] = min(99, int(attr_caps[attr] * (1 + boost)))
        return new_caps

    def to_dict(self) -> Dict:
        return {
            "growth_type": self.growth_type,
            "regression_type": self.regression_type,
            "dev_speed": self.dev_speed,
            "dev_focus": self.dev_focus,
            "attribute_caps": self.attribute_caps,
            "mutation": self.mutation,
            "traits": list(self.traits),
        }

    @classmethod
    def from_dict(cls, data: Dict) -> "PlayerDNA":
        obj = cls.__new__(cls)
        obj.growth_type = data.get("growth_type")
        obj.regression_type = data.get("regression_type")
        obj.dev_speed = data.get("dev_speed", 1.0)
        obj.dev_focus = data.get("dev_focus", {})
        obj.attribute_caps = data.get("attribute_caps", {})
        obj.mutation = data.get("mutation")
        obj.traits = data.get("traits", [])
        return obj
