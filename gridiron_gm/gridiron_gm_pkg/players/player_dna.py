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


# === Growth Curve Generation ===
def generate_growth_curve(
    min_age: int = 20,
    max_age: int = 40,
    peak_age: int | None = None,
    peak_duration: int | None = None,
) -> Dict[int, float]:
    """Return a per-age growth multiplier curve."""
    if max_age <= min_age:
        raise ValueError("max_age must be greater than min_age")

    peak_age = peak_age or int(max(min(random.gauss(27, 2), 32), 22))
    plateau_years = peak_duration or random.randint(1, 4)

    growth_speed = random.choice([0.8, 1.0, 1.2])
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
