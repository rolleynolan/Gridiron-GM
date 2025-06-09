"""Procedural DNA system for players."""
from __future__ import annotations

import random
from dataclasses import dataclass, field
from typing import Dict, List

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

from typing import Iterable, Optional, Dict
# === Growth Curve Generation ===
def generate_growth_curve(
    min_age: int = 20,
    max_age: int = 40,
    peak_age: Optional[int] = None,
    peak_duration: Optional[int] = None,
) -> Dict[int, float]:
    """Return a per-age growth multiplier curve."""
    if max_age <= min_age:
        raise ValueError("max_age must be greater than min_age")

    peak_age = peak_age or int(max(min(random.gauss(27, 2), 32), 22))
    plateau_years = peak_duration or random.randint(1, 4)

    growth_speed = random.choice([0.8, 1.0, 1.2])
    
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


def generate_growth_curve(min_age: int = 20, max_age: int = 34) -> Dict[int, float]:
    """Return a growth multiplier curve keyed by age.

    The curve has an ascension phase leading into a short prime plateau followed
    by decline. Each player receives a unique curve based on randomised peak
    age, plateau length and slope parameters. Values are clamped between 0.6 and
    1.05 and include a small amount of per-age noise.
    """

    if max_age <= min_age:
        raise ValueError("max_age must be greater than min_age")

    # --- Key parameters
    peak_age = int(max(min(random.gauss(27, 2), 32), 22))
    plateau_years = random.randint(1, 4)

    growth_speed = random.choice([0.8, 1.0, 1.2])  # slow/med/fast
    decline_speed = random.choice([0.8, 1.0, 1.2, 1.4])

    start_value = random.uniform(0.72, 0.82)
    years_to_peak = max(1, peak_age - min_age)
    base_slope = (1.0 - start_value) / years_to_peak
    growth_slope = base_slope * growth_speed
    decline_slope = base_slope * decline_speed

    curve: Dict[int, float] = {}
    value = start_value
    for age in range(min_age, max_age + 1):
        if age < peak_age:
            value += growth_slope
        elif age < peak_age + plateau_years:
            value = value
        else:
            value -= decline_slope
        noise = random.uniform(-0.02, 0.02)
        final = max(0.6, min(1.1, value + noise))
        curve[age] = round(final, 3)
    return curve


POSITION_ATTRS: Dict[str, List[str]] = {
    "QB": [
        "throw_power",
        "throw_accuracy_short",
        "throw_accuracy_mid",
        "throw_accuracy_deep",
        "throw_on_run",
        "pocket_presence",
        "release_time",
        "read_progression",
        "throw_under_pressure",
    ]
}

CORE_ATTRS = ["speed", "strength", "agility", "awareness"]


def _generate_attribute_caps(position: str) -> Dict[str, int]:
    attrs = CORE_ATTRS + POSITION_ATTRS.get(position.upper(), [])
    caps = {}
    for attr in attrs:
        base_low = 70 if attr in CORE_ATTRS else 60
        base_high = 99 if attr in CORE_ATTRS else 95
        caps[attr] = random.randint(base_low, base_high)
    return caps


@dataclass
class PlayerDNA:
    max_attribute_caps: Dict[str, int]
    development_speed: float
    regression_rate: float
    peak_age: int
    peak_duration: int
    growth_curve: Dict[int, float]
    mutations: List[str] = field(default_factory=list)

    def apply_mutation_effects(self) -> None:
        """Modify attribute caps in-place based on mutations."""
        for m in self.mutations:
            details = DNA_MUTATIONS.get(m, {})
            boosts = details.get("attribute_cap_boosts", {})
            for group, amt in boosts.items():
                for attr in list(self.max_attribute_caps.keys()):
                    if group in attr:
                        new_val = int(self.max_attribute_caps[attr] * (1 + amt))
                        self.max_attribute_caps[attr] = min(99, new_val)
            mult = details.get("dev_speed_multiplier")
            if mult:
                self.development_speed = round(self.development_speed * mult, 2)

    @staticmethod
    def generate_random_dna(position: str) -> "PlayerDNA":
        dev_speed = round(random.uniform(0.85, 1.15), 2)
        regression = round(random.uniform(0.85, 1.25), 2)
        peak_age = int(max(min(random.gauss(27, 2), 32), 22))
        peak_duration = random.randint(1, 6)
        growth_curve = generate_growth_curve(
            min_age=20,
            max_age=40,
            peak_age=peak_age,
            peak_duration=peak_duration,
        )
        mutation = []
        if random.random() <= 0.05:
            mutation.append(random.choice(list(DNA_MUTATIONS.keys())))
        caps = _generate_attribute_caps(position)
        dna = PlayerDNA(
            max_attribute_caps=caps,
            development_speed=dev_speed,
            regression_rate=regression,
            peak_age=peak_age,
            peak_duration=peak_duration,
            growth_curve=growth_curve,
            mutations=mutation,
        )
        if mutation:
            dna.apply_mutation_effects()
        return dna

    @classmethod
    def from_dict(cls, data: Dict) -> "PlayerDNA":
        return cls(
            max_attribute_caps=data.get("max_attribute_caps", {}),
            development_speed=data.get("development_speed", 1.0),
            regression_rate=data.get("regression_rate", 1.0),
            peak_age=data.get("peak_age", 26),
            peak_duration=data.get("peak_duration", 3),
            growth_curve=data.get("growth_curve", {}),
            mutations=data.get("mutations", []),
        )

    def to_dict(self) -> Dict:
        return {
            "max_attribute_caps": self.max_attribute_caps,
            "development_speed": self.development_speed,
            "regression_rate": self.regression_rate,
            "peak_age": self.peak_age,
            "peak_duration": self.peak_duration,
            "growth_curve": self.growth_curve,
            "mutations": self.mutations,
        }
