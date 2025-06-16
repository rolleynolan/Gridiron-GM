from __future__ import annotations

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
    def _apply_noise(self, true_value: float) -> float:
        """Return a value with scouting noise applied."""
        noise_range = (1.0 - self.evaluation_skill) * 10.0
        return max(0.0, min(99.0, true_value + random.uniform(-noise_range, noise_range)))

    # ------------------------------------------------------------------
    def _apply_bias(self, attr: str, value: float) -> float:
        """Adjust an attribute based on the scout's bias profile."""
        for bias_key, weight in self.bias_profile.items():
            if attr in ATTRIBUTE_GROUPS.get(bias_key, []):
                value += weight * 10.0
        return max(0.0, min(99.0, value))

    # ------------------------------------------------------------------
    def evaluate_player(self, player: Player) -> Dict[str, object]:
        """Return estimated attribute ranges and commentary for a player."""
        skill_bonus = 0.05 if self.focus_position and player.position == self.focus_position else 0.0
        accuracy = min(1.0, self.evaluation_skill + skill_bonus)
        estimates: Dict[str, Tuple[int, int]] = {}
        commentary: List[str] = []

        for attr, true_val in player.get_all_attributes().items():
            if true_val is None:
                continue
            biased = self._apply_bias(attr, float(true_val))
            est_center = self._apply_noise(biased)
            range_width = 4 + (1.0 - accuracy) * 12
            low = int(max(40.0, est_center - range_width / 2))
            high = int(min(99.0, est_center + range_width / 2))
            estimates[attr] = (low, high)
            if random.random() < 0.25 + accuracy * 0.5:
                commentary.append(_describe_rating(attr, est_center))

        # Exposure increases with each evaluation
        self.add_exposure(player.id)

        return {"estimates": estimates, "commentary": commentary}

    # ------------------------------------------------------------------
    def evaluate_potential(self, player: Player) -> Dict[str, str]:
        """Estimate a player's potential ceiling."""
        base = player.hidden_caps.get("overall", player.overall)
        base = self._apply_bias("overall", float(base))
        est_center = self._apply_noise(base)
        range_width = 10 + (1.0 - self.evaluation_skill) * 20
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
        player_adjust = self._player_modifiers.get(player.id, 0.0)
        eval_result = self.evaluate_player(player)
        pot_result = self.evaluate_potential(player)

        overall_true = player.overall + player_adjust
        overall_est = self._apply_noise(self._apply_bias("overall", overall_true))
        overall_low = int(max(40.0, overall_est - 5))
        overall_high = int(min(99.0, overall_est + 5))

        strengths = [c for c in eval_result["commentary"] if c.startswith("Elite") or c.startswith("Great")]
        weaknesses = [c for c in eval_result["commentary"] if c.startswith("poor") or c.startswith("Adequate")]

        return {
            "position_summary": player.position,
            "strengths": strengths,
            "weaknesses": weaknesses,
            "estimated_overall_range": f"{overall_low}\u2013{overall_high}",
            "estimated_potential_range": pot_result["range"],
            "commentary": eval_result["commentary"],
        }
