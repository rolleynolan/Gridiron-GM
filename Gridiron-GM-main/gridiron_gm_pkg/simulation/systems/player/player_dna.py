"""Player DNA system handling growth, traits and attribute caps."""

from __future__ import annotations

import random
import numpy as np
from dataclasses import dataclass, field
from enum import Enum, auto
from typing import Dict, List, Optional

<<<<<<< HEAD
=======
from gridiron_gm_pkg.simulation.systems.player import attribute_generator

# === Position-based peak age ranges ===
# These ranges roughly correspond to when players at each position
# typically reach their athletic prime. They are used to generate
# a growth arc tailored to the player's position.
POSITION_PEAK_RANGES = {
    "QB": (22, 32),
    "RB": (22, 27),
    "WR": (22, 28),
    "TE": (22, 30),
    "OL": (22, 32),
    "DL": (22, 30),
    "LB": (22, 31),
    "DB": (22, 30),
    "K": (22, 35),
    "P": (22, 35),
}


@dataclass
class GrowthArc:
    """Age-based windows for a player's rise, peak and decline."""

    peak_start_age: int
    peak_end_age: int
    decline_start_age: int

>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
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
class MutationType(Enum):
    PhysicalFreak = auto()
    EnduranceEngine = auto()
    MentalProcessor = auto()
    EliteVision = auto()
    BuiltToLast = auto()
    ExplosiveAthlete = auto()
    NaturalLeader = auto()
    TechnicalWizard = auto()
    ClutchPerformer = auto()
    FastLearner = auto()


def assign_mutations() -> List[MutationType]:
    """Randomly assign up to two rare mutations with weighted odds."""
    roll = random.random()
    if roll < 0.975:
        return []
    if roll < 0.995:
        return [random.choice(list(MutationType))]
    return random.sample(list(MutationType), 2)


<<<<<<< HEAD
=======
def generate_growth_arc(position: str | None) -> GrowthArc:
    """Create a positional growth arc describing rise, peak and decline ages."""
    min_age, max_age = POSITION_PEAK_RANGES.get(position, (24, 30))
    center = (min_age + max_age) / 2
    base_peak_start = int(np.clip(random.normalvariate(center, 1.5), min_age, max_age))

    peak_length = random.randint(3, 6)
    peak_end = base_peak_start + peak_length
    decline_start = peak_end + random.randint(1, 4)

    return GrowthArc(
        peak_start_age=base_peak_start,
        peak_end_age=peak_end,
        decline_start_age=decline_start,
    )


>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
def generate_dev_speed() -> float:
    """Generate development speed using a bell curve distribution."""
    value = random.normalvariate(0.65, 0.1)
    return max(0.3, min(1.0, round(value, 3)))

# === Regression defaults ===
DEFAULT_REGRESSION_PROFILE = {
    "start_age": 30,
    "rate": 0.04,  # 4% decline per year after start
    "position_modifier": {
        "RB": 1.3,
        "QB": 0.8,
        "WR": 1.0,
        "LB": 1.2,
        "OL": 1.1,
        "DL": 1.1,
        "DB": 1.2,
        "TE": 1.0,
        "K": 0.5,
        "P": 0.5,
    },
}

ATTRIBUTE_DECAY_TYPE = {
    # Physical
    "speed": "physical",
    "acceleration": "physical",
    "agility": "physical",
    "jumping": "physical",
    "strength": "physical",
    "stamina": "physical",
    "toughness": "physical",
    # Skill
    "catching": "skill",
    "tackling": "skill",
    "blocking": "skill",
    "route_running": "skill",
    "throw_power": "skill",
    "throw_accuracy": "skill",
    "lead_blocking": "skill",
<<<<<<< HEAD
=======
    "return_skill": "skill",
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    # Mental
    "awareness": "mental",
    "iq": "mental",
    "vision": "mental",
    "play_recognition": "mental",
}
# === Mutation utility functions ===
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
<<<<<<< HEAD
=======
        "return_skill",
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
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

    rise_duration: int = field(init=False)
    prime_duration: int = field(init=False)
    fall_duration: int = field(init=False)
    peak_value: float = field(init=False)
    stability: float = field(init=False)
    career_arc: List[float] = field(init=False)
<<<<<<< HEAD
=======
    growth_arc: GrowthArc = field(init=False)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802

    regression_profile: Dict[str, any] = field(init=False)
    attribute_decay_type: Dict[str, str] = field(init=False)
    dev_speed: float = field(init=False)
    dev_focus: Dict[str, float] = field(init=False)
    traits: List[str] = field(init=False)
    mutations: List[MutationType] = field(init=False)
    attribute_caps: Dict[str, Dict] = field(init=False)
    scouted_caps: Dict[str, int] = field(init=False)
<<<<<<< HEAD
=======
    injury_multiplier: float = field(init=False)
    recovery_speed_bonus: float = field(init=False)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802

    def __post_init__(self) -> None:
        self.rise_duration = random.randint(1, 6)
        self.prime_duration = random.randint(1, 8)
        self.fall_duration = max(2, 20 - (self.rise_duration + self.prime_duration))
        self.peak_value = round(random.uniform(0.85, 1.0), 2)
        self.stability = round(random.uniform(0.01, 0.05), 3)
        self.career_arc = self.generate_procedural_arc()
<<<<<<< HEAD
=======
        self.growth_arc = generate_growth_arc(None)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802

        self.regression_profile = DEFAULT_REGRESSION_PROFILE.copy()
        self.attribute_decay_type = ATTRIBUTE_DECAY_TYPE
        self.dev_speed = generate_dev_speed()
        self.dev_focus = self._generate_dev_focus_weights()
        self.traits = self._assign_traits()
        self.mutations = assign_mutations()
        self.attribute_caps = generate_attribute_caps(self.dev_focus)
        self.scouted_caps = self._generate_scouted_caps()
<<<<<<< HEAD
=======
        self.injury_multiplier = 1.0
        self.recovery_speed_bonus = 0.0
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802

    def generate_procedural_arc(self, total_years: int = 25) -> List[float]:
        """Return an annual multiplier curve representing the player's career trajectory."""
        rise = np.power(np.linspace(0, 1, self.rise_duration), 1.5) * self.peak_value
        prime_noise = np.random.normal(0, self.stability, self.prime_duration)
        prime = np.clip(np.full(self.prime_duration, self.peak_value) + prime_noise, 0, 1.05)
        fall_x = np.linspace(1, 0, self.fall_duration)
        fall = np.power(fall_x, 0.7) * self.peak_value
        fall_noise = np.random.normal(0, self.stability, self.fall_duration)
        fall = np.clip(fall + fall_noise, 0, 1.05)
        arc = np.concatenate((rise, prime, fall))
        return arc[:total_years].tolist()

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

    def _generate_scouted_caps(self) -> Dict[str, int]:
        offset = lambda cap: cap + random.randint(-5, 10)
        return {
            attr: offset(caps["hard_cap"]) for attr, caps in self.attribute_caps.items()
        }

    # --- Mutation effects ---------------------------------------------------
    def _apply_mutation_boost(self, attribute_name: str, base_growth: float) -> float:
        """Apply growth bonuses based on DNA mutations."""
        boost = 1.0
        muts = self.mutations
        if MutationType.FastLearner in muts:
            boost *= 1.1
        if MutationType.PhysicalFreak in muts and attribute_name in ["speed", "acceleration", "strength"]:
            boost *= 1.1
        if MutationType.ExplosiveAthlete in muts and attribute_name in ["agility", "acceleration", "jumping"]:
            boost *= 1.1
        if MutationType.TechnicalWizard in muts and attribute_name in ["tackle", "throw_accuracy", "route_running"]:
            boost *= 1.15
        if MutationType.MentalProcessor in muts and attribute_name in ["awareness", "iq", "play_recognition"]:
            boost *= 1.1
        if MutationType.EliteVision in muts and attribute_name in ["catching", "reaction_time", "ball_tracking"]:
            boost *= 1.1
        return base_growth * boost

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
            growth = self._apply_mutation_boost(attr, growth)
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
        age_trigger = self.regression_profile.get("start_age", 30)
        drop = 0.5 if self.regression_profile == "gradual" else 1.0
        if age >= age_trigger:
            for attr, caps in self.attribute_caps.items():
                drop_mod = 1.5 if attr in ["speed", "agility"] else 1.0
                caps["current"] = max(0, caps["current"] - drop * drop_mod)

    # --- Serialization ------------------------------------------------------
    def to_dict(self) -> Dict:
        return {
            "rise_duration": self.rise_duration,
            "prime_duration": self.prime_duration,
            "fall_duration": self.fall_duration,
            "peak_value": self.peak_value,
            "stability": self.stability,
            "career_arc": self.career_arc,
<<<<<<< HEAD
=======
            "growth_arc": {
                "peak_start_age": self.growth_arc.peak_start_age,
                "peak_end_age": self.growth_arc.peak_end_age,
                "decline_start_age": self.growth_arc.decline_start_age,
            },
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
            "regression_profile": self.regression_profile,
            "attribute_decay_type": self.attribute_decay_type,
            "dev_speed": self.dev_speed,
            "dev_focus": self.dev_focus,
            "traits": self.traits,
            "mutations": [m.name for m in self.mutations],
            "attribute_caps": self.attribute_caps,
            "scouted_caps": self.scouted_caps,
<<<<<<< HEAD
=======
            "injury_multiplier": self.injury_multiplier,
            "recovery_speed_bonus": self.recovery_speed_bonus,
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        }

    @classmethod
    def from_dict(cls, data: Dict) -> "PlayerDNA":
        obj = cls.__new__(cls)
        for field_name in [
            "rise_duration",
            "prime_duration",
            "fall_duration",
            "peak_value",
            "stability",
            "career_arc",
<<<<<<< HEAD
=======
            "growth_arc",
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
            "regression_profile",
            "attribute_decay_type",
            "dev_speed",
            "dev_focus",
            "traits",
            "attribute_caps",
            "scouted_caps",
<<<<<<< HEAD
        ]:
            setattr(obj, field_name, data.get(field_name))
        obj.mutations = [MutationType[m] for m in data.get("mutations", [])]
=======
            "injury_multiplier",
            "recovery_speed_bonus",
        ]:
            setattr(obj, field_name, data.get(field_name))
        obj.mutations = [MutationType[m] for m in data.get("mutations", [])]
        arc_data = data.get("growth_arc") or {}
        obj.growth_arc = GrowthArc(
            arc_data.get("peak_start_age", 0),
            arc_data.get("peak_end_age", 0),
            arc_data.get("decline_start_age", 0),
        )
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        return obj

    # Convenience factory used by Player for compatibility
    @staticmethod
<<<<<<< HEAD
    def generate_random_dna(position: str | None = None) -> "PlayerDNA":
        _ = position  # position is unused but kept for API compatibility
        return PlayerDNA()
=======
    def generate_random_dna(position: str | None = None, level: str = "pro") -> "PlayerDNA":
        dna = PlayerDNA()
        dna.growth_arc = generate_growth_arc(position)
        if position:
            attrs, caps = attribute_generator.generate_attributes_for_position(position)
            attr_caps: Dict[str, Dict] = {}
            for attr, val in attrs.items():
                hard_cap = caps.get(attr, val)
                attr_type = ATTRIBUTE_DECAY_TYPE.get(attr, "skill")

                if level == "pro":
                    if random.random() < 0.02:
                        hard_cap = 99
                    else:
                        hard_cap = min(99, max(hard_cap, caps.get(attr, hard_cap)))
                elif level == "college":
                    if attr_type == "physical":
                        hard_cap = min(99, max(hard_cap, random.randint(85, 99)))
                    else:
                        hard_cap = min(90, hard_cap)

                soft_cap = min(hard_cap, max(val + random.randint(2, 5), val))
                attr_caps[attr] = {
                    "current": val,
                    "soft_cap": soft_cap,
                    "hard_cap": hard_cap,
                    "breakout_history": [],
                }
            dna.attribute_caps = attr_caps
            dna.scouted_caps = dna._generate_scouted_caps()
        return dna
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
