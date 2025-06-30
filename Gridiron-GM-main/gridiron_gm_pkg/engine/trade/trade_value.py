"""Utility functions to estimate player and draft pick trade values."""

from gridiron_gm_pkg.simulation.entities.player import Player


def evaluate_player_value(player: Player, team=None) -> float:
    """Return a simple numeric trade value for a player."""
    age_factor = 1.0
    if hasattr(player, "age"):
        age_factor = max(0.5, 1.0 - (player.age - 25) * 0.03)
    return round(player.overall * 10 * age_factor, 1)


def calculate_pick_value(round_number: int, team=None) -> float:
    """Return a baseline value for a draft pick."""
    values = {1: 1000, 2: 400, 3: 250, 4: 120, 5: 60, 6: 25, 7: 10}
    return float(values.get(round_number, 10))
