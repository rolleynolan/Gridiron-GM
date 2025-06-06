"""Weekly practice attribute growth system."""

from __future__ import annotations

import random
from typing import Any, Dict, Iterable

# Attributes considered physical for slower capped growth
PHYSICAL_ATTRIBUTES = {
    "speed",
    "strength",
    "acceleration",
    "agility",
    "jumping",
    "throw_power",
}

# Map of valid training focus keywords to attributes that should grow
FOCUS_MAP = {
    "throwing": ["throw_power", "accuracy"],
    "route running": ["route_running", "awareness"],
    "awareness": ["awareness"],
    "strength": ["strength"],
    "speed": ["speed"],
}


def _get_attr_container(player: Any, attr: str) -> tuple[Dict[str, float], str] | tuple[None, None]:
    """Return attribute dict and section name for the given attribute."""
    attrs = getattr(player, "attributes", None)
    if attrs is None:
        return None, None

    if hasattr(attrs, "core") and attr in getattr(attrs, "core", {}):
        return attrs.core, "core"
    if hasattr(attrs, "position_specific") and attr in getattr(attrs, "position_specific", {}):
        return attrs.position_specific, "position_specific"
    return None, None


def apply_weekly_training(player: Any, team_context: Any) -> None:
    """Apply weekly training gains directly to the player's attributes."""

    # Check active/practice squad membership
    roster = None
    practice = None
    if isinstance(team_context, dict):
        roster = team_context.get("roster") or getattr(team_context.get("team"), "roster", None)
        practice = team_context.get("practice_squad")
        quality = team_context.get("coach_quality", team_context.get("training_quality", 1.0))
        week = team_context.get("week_number")
    else:
        roster = getattr(team_context, "roster", getattr(team_context, "players", None))
        practice = getattr(team_context, "practice_squad", None)
        quality = getattr(team_context, "coach_quality", getattr(team_context, "training_quality", 1.0))
        week = getattr(team_context, "current_week", None)

    if roster is not None and player not in roster and (practice is None or player not in practice):
        return

    if getattr(player, "is_injured", False):
        return

    focus = getattr(player, "training_focus", None)
    if not focus:
        return

    attr_names = FOCUS_MAP.get(str(focus).lower())
    if not attr_names:
        return

    # Determine multiplier from coaching quality
    try:
        mult = float(quality)
    except (TypeError, ValueError):
        mult = 1.0
    mult = min(1.2, max(0.8, mult))

    # Ensure tracking containers exist
    if not hasattr(player, "training_log"):
        player.training_log = {}
    if not hasattr(player, "_season_physical_growth"):
        player._season_physical_growth = {}

    for attr in attr_names:
        container, _ = _get_attr_container(player, attr)
        if container is None:
            continue

        if attr in PHYSICAL_ATTRIBUTES:
            gain = random.uniform(0.1, 0.3) * mult
            total = player._season_physical_growth.get(attr, 0.0)
            if total >= 2.0:
                continue
            gain = min(gain, 2.0 - total)
            player._season_physical_growth[attr] = total + gain
        else:
            gain = random.uniform(0.1, 0.5) * mult

        container[attr] = round(container.get(attr, 0) + gain, 2)

        if week is not None:
            player.training_log.setdefault(week, {})[attr] = round(gain, 3)
