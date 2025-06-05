"""Utility for evaluating a player's current archetype.

The evaluation is purely descriptive and does not modify any
attribute caps. It considers a subset of core attributes and
recent season statistics to assign a simple label that can be
used by scouting screens or season wrap up reports.
"""
from typing import Any, Dict


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
    accuracy = attr("accuracy")  # used for QB
    throw_power = attr("throw_power")
    catching = attr("catching")
    route_running = attr("route_running")
    tackling = attr("tackling")

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
