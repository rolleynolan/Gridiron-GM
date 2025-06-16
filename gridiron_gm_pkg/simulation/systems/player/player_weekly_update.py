from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.player_progression import progress_player
from gridiron_gm_pkg.simulation.systems.player.player_regression import apply_regression


def advance_player_week(player: Player, xp_gains: dict, coach_quality: float, rng=None):
    """Apply weekly progression and regression to a player."""
    player = progress_player(player, xp_gains, coach_quality, rng)
    player = apply_regression(player, player.age, rng)
    return player
