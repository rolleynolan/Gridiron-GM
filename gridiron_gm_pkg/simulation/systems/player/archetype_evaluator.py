"""Utility for evaluating a player's current archetype.

The evaluation is purely descriptive and does not modify any
attribute caps. It considers a subset of core attributes and
recent season statistics to assign a simple label that can be
used by scouting screens or season wrap up reports.
"""
from typing import Any, Dict


# -- QB Archetype definitions used by ``evaluate_qb_archetype`` --
QB_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Pocket Passer": {
        "throw_accuracy_short": 1.0,
        "throw_accuracy_medium": 1.0,
        "awareness": 0.9,
        "iq": 0.8,
        "speed": 0.2,
        "toughness": 0.6,
    },
    "Scrambler": {
        "speed": 1.0,
        "acceleration": 0.9,
        "agility": 0.8,
        "throw_on_run": 0.6,
        "throw_accuracy_short": 0.5,
    },
    "Gunslinger": {
        "throw_power": 1.0,
        "throw_accuracy_deep": 0.9,
        "throw_on_run": 0.6,
        "iq": 0.5,
        "discipline": 0.3,
    },
    "Field General": {
        "iq": 1.0,
        "awareness": 1.0,
        "throw_accuracy_short": 0.8,
        "discipline": 0.7,
        "toughness": 0.6,
    },
    "Dual-Threat": {
        "speed": 0.8,
        "throw_power": 0.8,
        "throw_on_run": 0.8,
        "acceleration": 0.7,
        "throw_accuracy_deep": 0.7,
    },
}


# -- RB Archetype definitions used by ``evaluate_rb_archetype`` --
RB_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Power Back": {
        "trucking": 1.0,
        "strength": 0.9,
        "break_tackle": 0.9,
        "carry_security": 0.8,
        "speed": 0.5,
    },
    "Elusive Back": {
        "elusiveness": 1.0,
        "agility": 0.95,
        "acceleration": 0.9,
        "speed": 0.85,
        "balance": 0.7,
    },
    "Receiving Back": {
        "catching": 1.0,
        "route_running": 0.9,
        "acceleration": 0.8,
        "elusiveness": 0.7,
        "awareness": 0.6,
    },
    "Workhorse": {
        "carry_security": 1.0,
        "stamina": 0.9,
        "toughness": 0.8,
        "speed": 0.7,
        "break_tackle": 0.6,
    },
    "Speed Back": {
        "speed": 1.0,
        "acceleration": 0.95,
        "elusiveness": 0.8,
        "agility": 0.75,
    },
}


def evaluate_archetype(player: Any, stats: Dict[str, int], attributes: Dict[str, int]) -> str:
    """Return a short archetype description based on attributes and stats.

    The function works with either ``Player`` objects or simple dictionaries
    for ``player`` and ``attributes``. Only a few common attribute keys are
    required (e.g. ``speed``, ``strength``, ``awareness``).

    Parameters
    ----------
    player:
        Player instance or dictionary containing at least a ``position`` field.
    stats:
        Dictionary of recent season statistics (rushing yards, receptions, etc.).
    attributes:
        Dictionary of attribute ratings.

    Returns
    -------
    str
        A descriptive archetype string such as ``"Power Back"`` or
        ``"Franchise QB"``.
    """
    position = getattr(player, "position", None)
    if position is None:
        position = player.get("position", "") if isinstance(player, dict) else ""

    # Helpers to read attribute values safely
    def attr(name: str, default: int = 50) -> int:
        if isinstance(attributes, dict):
            return int(attributes.get(name, default))
        return int(getattr(attributes, name, default))

    awareness = attr("awareness")
    speed = attr("speed")
    strength = attr("strength")
    accuracy = attr("throw_accuracy_short")  # used for QB
    throw_power = attr("throw_power")
    catching = attr("catching")
    route_running = attr("route_running_short")
    tackling = attr("tackle_lb")

    # Read some basic stats
    rushing_yards = int(stats.get("rushing_yards", 0))
    receptions = int(stats.get("receptions", 0))
    tackles = int(stats.get("tackles", 0))
    passing_yards = int(stats.get("passing_yards", 0))

    # --- QB Archetypes
    if position == "QB":
        if awareness >= 85 and accuracy >= 85 and passing_yards >= 3500:
            return "Franchise QB"
        if awareness < 70 or accuracy < 70:
            return "Raw Prospect"

    # --- Running Back
    if position == "RB":
        if strength >= 85 and rushing_yards >= 800:
            return "Power Back"
        if speed >= 90 and rushing_yards >= 1000:
            return "Feature RB"
        if awareness < 65 and rushing_yards < 300:
            return "Raw Prospect"

    # --- Wide Receiver
    if position == "WR":
        if receptions >= 80 and speed >= 90:
            return "Feature WR"
        if receptions >= 50 and awareness >= 80:
            return "Slot Technician"
        if awareness < 65 and receptions < 20:
            return "Raw Prospect"

    # --- Linebacker
    if position in ("LB", "ILB", "OLB"):
        if tackles >= 80 and strength >= 80:
            return "Run-Stopping LB"
        if awareness < 65 and tackles < 30:
            return "Raw Prospect"

    # Fallback
    return "Raw Prospect"


def evaluate_qb_archetype(attributes: Dict[str, int]) -> str:
    """Return the most likely QB archetype based on weighted attributes.

    Parameters
    ----------
    attributes:
        Mapping of attribute ratings for a quarterback. Values should range
        from 20 to 99.

    Returns
    -------
    str
        The archetype with the highest weighted score.
    """

    def norm(val: int) -> float:
        # Clamp and normalize the attribute rating to ``0-1``.
        return max(0.0, min((val - 20) / 79, 1.0))

    best_type = ""
    best_score = float("-inf")

    for archetype, profile in QB_ARCHETYPES.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type


def evaluate_rb_archetype(attributes: Dict[str, int]) -> str:
    """Return the most likely RB archetype based on weighted attributes.

    Parameters
    ----------
    attributes:
        Mapping of attribute ratings for a running back. Values should range
        from 20 to 99.

    Returns
    -------
    str
        The archetype with the highest weighted score.
    """

    def norm(val: int) -> float:
        # Clamp and normalize the attribute rating to ``0-1``.
        return max(0.0, min((val - 20) / 79, 1.0))

    best_type = ""
    best_score = float("-inf")

    for archetype, profile in RB_ARCHETYPES.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type
