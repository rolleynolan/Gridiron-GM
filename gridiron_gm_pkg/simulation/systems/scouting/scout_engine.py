import random
from typing import Dict

from gridiron_gm_pkg.simulation.entities.player import Player

class ScoutEngine:
    """Utility class for masking ratings based on scouting accuracy and bias."""

    def __init__(self, rng: random.Random | None = None) -> None:
        self.rng = rng or random.Random()

    def apply_rating_mask(self, rating: float, accuracy: float, bias: float = 0.0) -> int:
        """Return a masked rating value.

        Parameters
        ----------
        rating : float
            True rating value.
        accuracy : float
            Scouting accuracy in range 0-1.
        bias : float
            Team-specific bias modifier (positive = optimistic).
        """
        biased = rating + bias * 10.0
        noise = self.rng.gauss(0.0, max(0.0, 1.0 - accuracy) * 5.0)
        masked = biased + noise
        return int(max(40, min(99, round(masked))))

    def mask_player_ratings(self, player: Player, accuracy: float, bias: float = 0.0) -> Dict[str, int]:
        """Return masked overall and potential ratings for a player."""
        overall = self.apply_rating_mask(player.overall, accuracy, bias)
        true_pot = player.potential if player.potential is not None else player.hidden_caps.get("overall", player.overall)
        potential = self.apply_rating_mask(true_pot, accuracy, bias)
        return {"overall": overall, "potential": potential}
