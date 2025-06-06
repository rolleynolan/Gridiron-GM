"""Weekly practice attribute growth system."""

from __future__ import annotations

import random
from typing import Any, Dict, Iterable

from gridiron_gm.gridiron_gm_pkg.config.training_catalog import TRAINING_CATALOG

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
    "throwing": [
        "throw_power",
        "throw_accuracy_short",
        "throw_accuracy_mid",
        "throw_accuracy_deep",
    ],
    "route running": [
        "route_running_short",
        "route_running_mid",
        "route_running_deep",
        "awareness",
    ],
    "awareness": ["awareness"],
    "strength": ["strength"],
    "speed": ["speed"],
}

# Base increase applied before weighting and multipliers
BASE_GROWTH = 0.2


def apply_training_plan(team: Any, plan: Dict[str, Any], week: int) -> None:
    """Apply a weighted training drill to appropriate players."""

    if not plan:
        return

    drill_name = plan.get("drill")
    drill = TRAINING_CATALOG.get(drill_name)
    if not drill:
        return

    # Determine coach multiplier
    quality = getattr(team, "coach_quality", getattr(team, "training_quality", 1.0))
    try:
        mult = float(quality)
    except (TypeError, ValueError):
        mult = 1.0
    mult = min(1.2, max(0.8, mult))

    target_type = plan.get("type", "team")
    players: Iterable[Any]
    if target_type == "player":
        player = plan.get("player")
        players = [player] if player is not None else []
    elif target_type == "position":
        pos = str(plan.get("position", "")).upper()
        players = [p for p in getattr(team, "roster", []) if getattr(p, "position", "").upper() == pos]
    else:
        players = [p for p in getattr(team, "roster", [])]

    for player in players:
        if player is None or getattr(player, "is_injured", False):
            continue

        # Ensure tracking containers exist
        if not hasattr(player, "training_log"):
            player.training_log = {}
        if not hasattr(player, "_season_physical_growth"):
            player._season_physical_growth = {}

        for attr, weight in drill.get("attribute_weights", {}).items():
            container, _ = _get_attr_container(player, attr)
            if container is None:
                continue

            gain = BASE_GROWTH * weight * mult
            if target_type == "team":
                gain *= 0.5
            elif target_type == "player":
                gain *= 1.2

            if attr in PHYSICAL_ATTRIBUTES:
                total = player._season_physical_growth.get(attr, 0.0)
                if total >= 2.0:
                    continue
                gain = min(gain, 2.0 - total)
                player._season_physical_growth[attr] = total + gain

            container[attr] = round(container.get(attr, 0) + gain, 2)
            player.training_log.setdefault(week, {})[attr] = round(gain, 3)


def assign_training(team: Any, week: int) -> None:
    """Assign a simple CPU training plan if none provided."""

    if getattr(team, "user_controlled", False):
        return

    if not hasattr(team, "training_plan"):
        team.training_plan = {}

    if week in team.training_plan:
        return

    # Determine weakest position by average overall
    pos_scores: Dict[str, float] = {}
    for p in getattr(team, "roster", []):
        if getattr(p, "is_injured", False):
            continue
        pos_scores.setdefault(p.position, []).append(getattr(p, "overall", 0))
    if not pos_scores:
        return
    avg_scores = {pos: sum(vals) / len(vals) for pos, vals in pos_scores.items()}
    weakest = min(avg_scores, key=avg_scores.get)

    drill_name = None
    for name, drill in TRAINING_CATALOG.items():
        if drill["positions"] == "ALL" or weakest in drill["positions"]:
            drill_name = name
            break
    if drill_name is None:
        drill_name = next(iter(TRAINING_CATALOG))

    team.training_plan[week] = {"type": "position", "position": weakest, "drill": drill_name}


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
