<<<<<<< HEAD

import random
import math

# Define detailed football positions
DETAILED_POSITIONS = [
    "QB", "RB", "WR", "TE",
    "LT", "LG", "C", "RG", "RT",
    "DE", "DT", "OLB", "MLB",
    "CB", "FS", "SS",
    "K", "P"
]

# Map clusters to group-level skills for generalization
POSITION_CLUSTERS = {
    "OL": ["LT", "LG", "C", "RG", "RT"],
    "DL": ["DE", "DT"],
    "LB": ["OLB", "MLB"],
    "DB": ["CB", "FS", "SS"],
    "K/P": ["K", "P"],
    "RB": ["RB"],
    "WR": ["WR"],
    "TE": ["TE"],
    "QB": ["QB"]
}

class Scout:
    def __init__(self, name, role, region=None, scouting_accuracy=None):
        self.name = name
        self.role = role
        self.region = region
        self.age = random.randint(30, 65)
        self.accuracy = random.randint(40, 100)
        self.scouting_accuracy = (
            scouting_accuracy if scouting_accuracy is not None else self.accuracy / 100.0
        )
        self.speed = random.randint(5, 20)
        self.position_skills = self.generate_position_skills()
        self.personality = random.choice(["Risk Averse", "Upside Seeker", "Old School", "Analytics Oriented"])
        self.bias_region = random.choice([None, self.region])
        self.auto_scouting_enabled = False if role != 'Head' else True
        self.active_assignments = []
        self.assignments = []
        self.region_focus = False
        self.task_type = "assigned_players"
        self.task_param = None
        self.history = []

    def generate_position_skills(self):
        # Skill by cluster
        return {cluster: random.randint(40, 100) for cluster in POSITION_CLUSTERS.keys()}

    def get_position_skill(self, position):
        # Translate detailed position into its cluster and return skill
        for cluster, detailed in POSITION_CLUSTERS.items():
            if position in detailed:
                return self.position_skills.get(cluster, 70)
        return 70  # default

    def evaluate_player(self, player, team_name):
        if self.role == "College" and getattr(player, "experience", 0) > 0:
            return None
        if self.role == "Pro" and getattr(player, "experience", 0) == 0:
            return None

        base = getattr(player, "true_overall", getattr(player, "overall", 60))
        pos_bonus = self.get_position_skill(player.position)
        region_bonus = 5 if self.bias_region and self.bias_region == getattr(player, "region", None) else 0

        accuracy_influence = math.log(101 - self.accuracy + 1) * 5
        base_deviation = 1.25 if self.role == "College" else 1.0 if getattr(player, "experience", 0) < 2 else 0.75
        deviation = int(accuracy_influence * base_deviation + (100 - pos_bonus) // 10)
        scouted = base + random.randint(-deviation, deviation) + region_bonus
        scouted = max(40, min(99, scouted))

        scouted_percent = min(100, max(10, self.speed * 5))
        projected_future = min(100, max(scouted + random.randint(0, 10), scouted))

        skill_ratings = {
            skill: max(40, min(100, rating + random.randint(-5, 5)))
            for skill, rating in player.true_attributes.items()
        }

        player.scouted_rating[team_name] = scouted
        player.scouted_skills[team_name] = skill_ratings

        report = self.generate_scout_report(player, scouted, scouted_percent, projected_future, skill_ratings)
        player.scout_reports[self.name] = report
        self.history.append((player.name, player.true_overall, scouted, report))
        return scouted
    
    def weekly_scout(self, team_name, prospects):
        """Evaluate a batch of players based on task type and return (name, rating) tuples."""
        results = []
        if self.task_type == "assigned_players":
            targets = self.task_param or []
        elif self.task_type == "position":
            targets = [p for p in prospects if p.position == self.task_param]
        elif self.task_type == "region":
            targets = [p for p in prospects if getattr(p, "region", None) == self.task_param]
        else:
            targets = prospects

        for player in targets[:self.speed]:  # Limit based on scout speed
            rating = self.evaluate_player(player, team_name)
            if rating is not None:
                results.append((player.name, rating))

        return results


    def generate_scout_report(self, player, scouted_rating, scouted_percent, future_rating, skill_ratings):
        traits = [
            "Elite physical tools", "Strong fundamentals", "Questionable decision making",
            "Raw but talented", "High football IQ", "Great motor", "Inconsistent technique",
            "Needs time to develop", "NFL ready", "Boom-or-bust prospect"
        ]
        skills_summary = "\n".join([f"- {skill}: {value}/100" for skill, value in skill_ratings.items()])
        summary = [
            f"Scouted {scouted_percent}%",
            f"Current Ability: {scouted_rating}/100",
            f"Projected Potential: {future_rating}/100",
            f"Evaluation: {random.choice(traits)}",
            f"- Positional fit appears appropriate for current scheme.",
            f"- Shows flashes of development in key situations.",
            f"- Expected role: {self.projected_role(future_rating)}",
            "Skill Ratings:",
            skills_summary
        ]
        return "\n" + "\n".join(summary)

    def projected_role(self, rating):
        if rating >= 90:
            return "Franchise Player"
        elif rating >= 80:
            return "Future Star"
        elif rating >= 70:
            return "Solid Starter"
        elif rating >= 60:
            return "Rotational Player"
        elif rating >= 50:
            return "Career Backup"
        else:
            return "Camp Body"
=======
from __future__ import annotations

"""
TASK: Expand the Scout class to support fog-of-war evaluations based on scout skill and bias.

FEATURES TO ADD:
1. Evaluate any player (college or pro) and generate a 'scouted profile'.
2. Output should include:
    - Estimated OVR and POT as RANGES, not single values (e.g. 67–74)
    - Text blurbs based on standout traits (e.g. "Explosive runner", "Elite agility")
    - Specific attributes (e.g. Speed, Awareness) shown as ranges if relevant to scout role
    - Tag known traits like 'High Work Ethic' or 'Injury Prone' with chance for accuracy

3. Inaccuracy Rules:
    - Scouts use Gaussian noise scaled to their skill level (higher skill = tighter ranges)
    - Bias can influence reporting (e.g. overrate athletes, underrate low-IQ players)
    - Potentials are harder to scout accurately than current ability

4. Evaluation evolves over time:
    - Add `refine_evaluation(player)` method that reduces error margins if scout watches more film or sees combine
    - Scout accuracy should improve over multiple evaluations

5. Add methods:
    - `evaluate_player(player, context="season") -> dict`  # full scouting report
    - `refine_evaluation(player)`                         # narrows ranges, updates report

6. Output format:
{
  "summary": "Big-play receiver with elite top speed. Needs polish as a route runner.",
  "ovr_range": [67, 74],
  "pot_range": [83, 94],
  "attribute_ranges": {
    "speed": [91, 97],
    "awareness": [60, 72],
    "catching": [78, 85]
  },
  "known_traits": ["Explosive Athlete", "Raw Route Tree"],
  "scout_accuracy": 0.78,  # reflects confidence level (0–1)
}
"""

import random
from typing import Dict, List, Optional, Tuple

from gridiron_gm_pkg.simulation.entities.player import Player

# Attribute groupings for bias handling
ATTRIBUTE_GROUPS: Dict[str, List[str]] = {
    "athleticism": [
        "speed",
        "acceleration",
        "agility",
        "strength",
        "toughness",
        "balance",
    ],
    "iq": ["awareness", "iq"],
}


def _describe_rating(attr: str, rating: float) -> str:
    """Return a short textual description for an attribute rating."""
    name = attr.replace("_", " ")
    tier = (
        "elite"
        if rating >= 90
        else "great"
        if rating >= 80
        else "solid"
        if rating >= 70
        else "adequate"
        if rating >= 60
        else "poor"
    )
    return f"{tier.title()} {name}"


class Scout:
    """Represents a scout evaluating football players."""

    def __init__(
        self,
        name: str,
        evaluation_skill: float = 0.5,
        bias_profile: Optional[Dict[str, float]] = None,
        focus_position: Optional[str] = None,
    ) -> None:
        self.name = name
        self.evaluation_skill = max(0.0, min(1.0, evaluation_skill))
        self.scouting_accuracy = self.evaluation_skill  # compatibility for fog-of-war
        self.bias_profile: Dict[str, float] = bias_profile or {}
        self.focus_position = focus_position

        # Track exposure for fog-of-war reports
        self.exposure_map: Dict[str, float] = {}
        # Per-player opinion modifiers from events
        self._player_modifiers: Dict[str, float] = {}
        # Cached scouting reports
        self._reports: Dict[str, Dict[str, object]] = {}

    # ------------------------------------------------------------------
    def get_accuracy_for(self, attribute: str) -> float:
        """Return accuracy modifier for a specific attribute."""
        return self.scouting_accuracy

    # ------------------------------------------------------------------
    def add_exposure(self, player_id: str, amount: float = 0.1) -> None:
        """Increase exposure value for a player."""
        current = self.exposure_map.get(player_id, 0.0)
        self.exposure_map[player_id] = min(1.0, current + amount)

    # ------------------------------------------------------------------
    def _apply_noise(self, true_value: float, exposure: float = 0.0) -> float:
        """Return a value with scouting noise applied."""
        std_dev = (1.0 - self.evaluation_skill) * 5.0 * max(0.25, 1.0 - exposure)
        noisy = random.gauss(true_value, std_dev)
        return max(0.0, min(99.0, noisy))

    # ------------------------------------------------------------------
    def _apply_bias(self, attr: str, value: float) -> float:
        """Adjust an attribute based on the scout's bias profile."""
        for bias_key, weight in self.bias_profile.items():
            if attr in ATTRIBUTE_GROUPS.get(bias_key, []):
                value += weight * 10.0
        return max(0.0, min(99.0, value))

    # ------------------------------------------------------------------
    def evaluate_player(self, player: Player, context: str = "season") -> Dict[str, object]:
        """Return a fog-of-war scouting profile for a player."""
        exposure = self.exposure_map.get(player.id, 0.0)
        skill_bonus = 0.1 if self.focus_position and player.position == self.focus_position else 0.0
        accuracy = min(1.0, self.evaluation_skill + skill_bonus + exposure * 0.3)

        attribute_ranges: Dict[str, Tuple[int, int]] = {}
        blurbs: List[str] = []

        for attr, true_val in player.get_all_attributes().items():
            if true_val is None:
                continue
            biased = self._apply_bias(attr, float(true_val))
            est_center = self._apply_noise(biased, exposure)
            range_width = 8 + (1.0 - accuracy) * 20
            low = int(max(40.0, est_center - range_width / 2))
            high = int(min(99.0, est_center + range_width / 2))
            attribute_ranges[attr] = (low, high)
            if random.random() < 0.3 + accuracy * 0.5:
                blurbs.append(_describe_rating(attr, est_center))

        # Estimated overall and potential
        true_ovr = player.overall + self._player_modifiers.get(player.id, 0.0)
        true_pot = player.potential or player.hidden_caps.get("overall", player.overall)
        ovr_center = self._apply_noise(self._apply_bias("overall", float(true_ovr)), exposure)
        pot_center = self._apply_noise(self._apply_bias("potential", float(true_pot)), exposure)
        ovr_width = 6 + (1.0 - accuracy) * 14
        pot_width = ovr_width + 4
        ovr_range = [int(max(40.0, ovr_center - ovr_width / 2)), int(min(99.0, ovr_center + ovr_width / 2))]
        pot_range = [int(max(60.0, pot_center - pot_width / 2)), int(min(99.0, pot_center + pot_width / 2))]

        known_traits: List[str] = []
        for category, traits in player.traits.items():
            for trait in traits:
                if random.random() < accuracy * 0.7 + exposure * 0.3:
                    known_traits.append(trait.replace("_", " ").title())

        summary = "; ".join(blurbs[:2]) if blurbs else f"{player.position} prospect"

        report = {
            "summary": summary,
            "ovr_range": ovr_range,
            "pot_range": pot_range,
            "attribute_ranges": attribute_ranges,
            "known_traits": known_traits,
            "scout_accuracy": round(accuracy, 2),
        }

        self.add_exposure(player.id)
        self._reports[player.id] = report
        return report

    # ------------------------------------------------------------------
    def refine_evaluation(self, player: Player) -> Dict[str, object]:
        """Improve an existing scouting report by increasing exposure."""
        self.add_exposure(player.id, 0.2)
        return self.evaluate_player(player)

    # ------------------------------------------------------------------
    def evaluate_potential(self, player: Player) -> Dict[str, str]:
        """Estimate a player's potential ceiling."""
        exposure = self.exposure_map.get(player.id, 0.0)
        base = player.hidden_caps.get("overall", player.overall)
        base = self._apply_bias("overall", float(base))
        est_center = self._apply_noise(base, exposure)
        range_width = 10 + (1.0 - self.evaluation_skill) * 20 * max(0.5, 1.0 - exposure)
        low = int(max(60.0, est_center - range_width / 2))
        high = int(min(99.0, est_center + range_width / 2))
        confidence = (
            "High" if self.evaluation_skill >= 0.8 else "Medium" if self.evaluation_skill >= 0.5 else "Low"
        )
        description = (
            "Could develop into a top-tier {pos}".format(pos=player.position)
            if high >= 90
            else "Upside appears limited"
        )
        return {
            "range": f"{low}\u2013{high}",
            "confidence": confidence,
            "description": description,
        }

    # ------------------------------------------------------------------
    def update_opinion_from_event(self, player: Player, event: Dict[str, object]) -> None:
        """Update stored opinion of a player based on new information."""
        if player.id not in self._player_modifiers:
            self._player_modifiers[player.id] = 0.0
        change = float(event.get("change", 0.0))
        self._player_modifiers[player.id] += change

    # ------------------------------------------------------------------
    def generate_report(self, player: Player) -> Dict[str, object]:
        """Compile a full scouting report for a player."""
        return self.evaluate_player(player)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
