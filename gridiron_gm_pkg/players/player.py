"""Helpers for presenting player data with scouting fog-of-war."""

from __future__ import annotations

from typing import Dict, Optional

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.scout import Scout
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.systems.scouting.scout_engine import ScoutEngine

_engine = ScoutEngine()


def get_rookie_view(player: Player, scout: Optional[Scout] = None, team: Optional[Team] = None) -> Dict[str, int]:
    """Return masked OVR and POT for display based on scouting info."""
    accuracy = getattr(team, "scouting_accuracy", 0.6)
    bias = getattr(team, "scouting_bias", 0.0)
    if scout is not None:
        accuracy = (accuracy + getattr(scout, "evaluation_skill", 0.5)) / 2
        bias += getattr(scout, "bias_profile", {}).get("overall", 0.0)
    return _engine.mask_player_ratings(player, accuracy, bias)
