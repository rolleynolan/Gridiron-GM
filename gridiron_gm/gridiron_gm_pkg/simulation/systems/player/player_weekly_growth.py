"""Weekly incremental training growth system.

This module provides ``apply_weekly_growth`` which calculates subtle
attribute improvements for a player each week based on usage
and fatigue. The function does **not** mutate the player. It simply
returns a dictionary of attribute deltas for the calling system to apply.
"""

from __future__ import annotations

from typing import Dict, Iterable
import random


def _get_training_attributes(player) -> Dict[str, int]:
    """Return combined mapping of attribute names to values for the player."""

    attrs = {}
    attr_container = getattr(player, "attributes", None)
    if attr_container is None:
        return attrs

    core = getattr(attr_container, "core", {})
    if isinstance(core, dict):
        attrs.update(core)
    pos = getattr(attr_container, "position_specific", {})
    if isinstance(pos, dict):
        attrs.update(pos)
    return attrs


def _focused(attr: str, focus: Iterable[str] | str | None) -> bool:
    if focus is None:
        return False
    if isinstance(focus, str):
        return attr == focus
    return attr in focus


def apply_weekly_growth(player, context: Dict | None = None) -> Dict[str, float]:
    """Calculate weekly attribute growth.

    Parameters
    ----------
    player:
        Player object or dict-like with ``attributes`` and ``fatigue`` fields.
    context:
        Optional context providing ``snaps`` (int) and ``training_focus`` (iterable
        or string) to influence growth.

    Returns
    -------
    dict
        Mapping of attribute names to small positive deltas that should be
        applied elsewhere.
    """

    context = context or {}
    snaps = context.get("snaps", getattr(player, "snaps", 0))
    fatigue = float(getattr(player, "fatigue", 0.0))
    focus = context.get("training_focus")

    # Normalize usage and fatigue into 0..1 multipliers
    usage_factor = min(1.0, snaps / 50.0)
    fatigue_factor = max(0.0, 1.0 - min(fatigue, 1.0))

    deltas: Dict[str, float] = {}
    attrs = _get_training_attributes(player)
    if not attrs:
        return deltas

    for attr in attrs:
        base_gain = 0.1 + 0.15 * usage_factor * fatigue_factor
        if _focused(attr, focus):
            base_gain *= 1.5
        gain = min(base_gain, 0.25)
        # random variation within small range
        gain *= random.uniform(0.9, 1.1)
        gain = round(gain, 3)
        if gain > 0:
            deltas[attr] = gain

    return deltas

