"""Player DNA system handling growth, traits and attribute caps."""

from __future__ import annotations

import random
from dataclasses import dataclass, field
from typing import Dict, List, Optional

# === TRAITS (Behavioral Modifiers) ===
TRAIT_POOL = [
    "Leader",
    "Mentor",
    "Hard Worker",
    "Low Motor",
    "Resilient",
    "Ego Driven",
    "Team Player",
    "Hot-Headed",
    "System Dependent",
    "Spotlight Seeker",
]

# === MUTATIONS (Rare DNA Modifiers) ===
DNA_MUTATIONS = {
    "Generational Talent": {
        "attribute_cap_boosts": {"physical": 0.1, "mental": 0.1, "technical": 0.1},
        "dev_speed_multiplier": 1.25,
    },
    "Physical Freak": {
        "attribute_cap_boosts": {"physical": 0.20},
        "dev_speed_multiplier": 0.85,
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


# === ATTRIBUTE CAPS STRUCTURE ===
def generate_attribute_caps(dev_focus: Dict[str, float]) -> Dict[str, Dict]:
    caps: Dict[str, Dict] = {}
    for attr in [
        "speed",
        "strength",
        "awareness",
        "agility",
        "tackle",
        "catching",
        "route_running_short",
    ]:
        base = random.randint(70, 90)
        soft_cap = int(base + random.randint(2, 5))
        hard_cap = int(soft_cap + random.randint(2, 5))
        caps[attr] = {
            "current": base,
            "soft_cap": min(soft_cap, 98),
            "hard_cap": min(hard_cap, 99),
            "breakout_history": [],
        }
    return caps


@dataclass
class PlayerDNA:
    """Container for a player's long-term development profile."""

    growth_arc: str = field(init=False)
    regression_profile: str = field(init=False)
    dev_speed: float = field(init=False)
    dev_focus: Dict[str, float] = field(init=False)
    traits: List[str] = field(init=False)
    mutation: Optional[str] = field(init=False)
    attribute_caps: Dict[str, Dict] = field(init=False)
    scouted_caps: Dict[str, int] = field(init=False)

    def __post_init__(self) -> None:
        self.growth_arc = random.choice(["early", "late", "balanced", "rollercoaster"])
        self.regression_profile = random.choice(
            ["early_decline", "late_decline", "injury_decline", "gradual"]
        )
        self.dev_speed = round(random.uniform(0.75, 1.25), 2)
        self.dev_focus = self._generate_dev_focus_weights()
        self.traits = self._assign_traits()
        self.mutation = self._assign_mutation()
        self.attribute_caps = generate_attribute_caps(self.dev_focus)
        if self.mutation:
            self.apply_mutation_effects()
        self.scouted_caps = self._generate_scouted_caps()

    # --- Helper generators -------------------------------------------------
    def _generate_dev_focus_weights(self) -> Dict[str, float]:
        weights = [random.uniform(0.25, 0.45) for _ in range(3)]
        total = sum(weights)
        return {
            "physical": weights[0] / total,
            "mental": weights[1] / total,
            "technical": weights[2] / total,
        }

    def _assign_traits(self) -> List[str]:
        count = random.choices([0, 1, 2, 3], weights=[0.1, 0.4, 0.35, 0.15])[0]
        return random.sample(TRAIT_POOL, count)

    def _assign_mutation(self) -> Optional[str]:
        return (
            random.choice(list(DNA_MUTATIONS.keys()))
            if random.random() < 0.05
            else None
        )

    def _generate_scouted_caps(self) -> Dict[str, int]:
        offset = lambda cap: cap + random.randint(-5, 10)
        return {
            attr: offset(caps["hard_cap"]) for attr, caps in self.attribute_caps.items()
        }

    # --- Mutation effects ---------------------------------------------------
    def apply_mutation_effects(self) -> None:
        if not self.mutation:
            return
        details = DNA_MUTATIONS[self.mutation]
        boosts = details.get("attribute_cap_boosts", {})
        for group, boost in boosts.items():
            for attr, caps in self.attribute_caps.items():
                if group in attr:
                    caps["soft_cap"] = min(98, int(caps["soft_cap"] * (1 + boost)))
                    caps["hard_cap"] = min(99, int(caps["hard_cap"] * (1 + boost)))

        mult = details.get("dev_speed_multiplier")
        if mult:
            self.dev_speed = round(self.dev_speed * mult, 2)

    # --- Weekly progression -------------------------------------------------
    def apply_weekly_growth(
        self, usage_factor: float = 1.0, coaching_quality: float = 1.0
    ) -> None:
        for attr, caps in self.attribute_caps.items():
            cur = caps["current"]
            soft_cap = caps["soft_cap"]
            hard_cap = caps["hard_cap"]
            if cur >= hard_cap:
                continue
            base_gain = 0.1 * self.dev_speed * usage_factor * coaching_quality
            if cur < soft_cap:
                growth = base_gain
            else:
                growth = base_gain * 0.25
            caps["current"] = min(hard_cap, cur + growth)

    def check_breakout(
        self,
        attr: str,
        production_metric: bool,
        snap_share: float,
        week: int,
        traits: Optional[List[str]] = None,
    ) -> None:
        traits = traits or []
        caps = self.attribute_caps[attr]
        if (
            caps["current"] >= caps["soft_cap"] - 1
            and production_metric
            and snap_share > 0.7
        ):
            if "Hard Worker" in self.traits or "Resilient" in traits:
                caps["current"] = min(
                    caps["hard_cap"], caps["current"] + random.randint(2, 4)
                )
                caps["soft_cap"] = min(
                    caps["hard_cap"], caps["soft_cap"] + random.randint(1, 3)
                )
                caps["breakout_history"].append(week)

    # --- Regression ---------------------------------------------------------
    def regress_player(self, age: int, is_injured: bool = False) -> None:
        age_trigger = {"early": 27, "balanced": 30, "late": 32, "rollercoaster": 29}
        drop = 0.5 if self.regression_profile == "gradual" else 1.0
        trigger = age_trigger.get(self.growth_arc, 29)
        if age >= trigger:
            for attr, caps in self.attribute_caps.items():
                if is_injured and DNA_MUTATIONS.get(self.mutation, {}).get(
                    "injury_regression_reduction"
                ):
                    continue
                drop_mod = 1.5 if attr in ["speed", "agility"] else 1.0
                caps["current"] = max(0, caps["current"] - drop * drop_mod)

    # --- Serialization ------------------------------------------------------
    def to_dict(self) -> Dict:
        return {
            "growth_arc": self.growth_arc,
            "regression_profile": self.regression_profile,
            "dev_speed": self.dev_speed,
            "dev_focus": self.dev_focus,
            "traits": self.traits,
            "mutation": self.mutation,
            "attribute_caps": self.attribute_caps,
            "scouted_caps": self.scouted_caps,
        }

    @classmethod
    def from_dict(cls, data: Dict) -> "PlayerDNA":
        obj = cls.__new__(cls)
        for field_name in [
            "growth_arc",
            "regression_profile",
            "dev_speed",
            "dev_focus",
            "traits",
            "mutation",
            "attribute_caps",
            "scouted_caps",
        ]:
            setattr(obj, field_name, data.get(field_name))
        return obj

    # Convenience factory used by Player for compatibility
    @staticmethod
    def generate_random_dna(position: str | None = None) -> "PlayerDNA":
        _ = position  # position is unused but kept for API compatibility
        return PlayerDNA()
