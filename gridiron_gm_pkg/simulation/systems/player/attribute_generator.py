"""Utilities for generating player attributes based on positional relevance."""

from __future__ import annotations

import numpy as np
from typing import Dict, Tuple

# Attribute relevance map per position
POSITION_RELEVANCE: Dict[str, Dict[str, str]] = {
    "QB": {
        "throw_power": "high",
        "throw_accuracy_short": "high",
        "throw_accuracy_medium": "high",
        "throw_accuracy_deep": "high",
        "iq": "high",
        "awareness": "medium",
        "speed": "low",
        "agility": "low",
        "catching": "low",
        "tackling": "low",
    },
    "RB": {
        "speed": "high",
        "acceleration": "high",
        "agility": "high",
        "elusiveness": "high",
        "carry_security": "high",
        "catching": "medium",
        "route_running": "medium",
        "blocking": "low",
        "throw_power": "low",
        "tackling": "low",
    },
    "WR": {
        "speed": "high",
        "acceleration": "high",
        "catching": "high",
        "route_running_short": "high",
        "route_running_deep": "high",
        "awareness": "medium",
        "blocking": "low",
        "tackling": "low",
    },
    "CB": {
        "speed": "high",
        "agility": "high",
        "coverage": "high",
        "catching": "medium",
        "tackling": "medium",
        "pass_rush": "low",
    },
    # Additional positions can be added here.
}

# Rating caps based on attribute relevance
RELEVANCE_CAP_RANGES: Dict[str, Tuple[int, int]] = {
    "high": (75, 99),
    "medium": (60, 85),
    "low": (25, 50),
}

# Starting attribute values based on relevance
RELEVANCE_GEN_RANGES: Dict[str, Tuple[int, int]] = {
    "high": (65, 88),
    "medium": (50, 70),
    "low": (20, 40),
}

def bell_curve_sample(mean: float, std_dev: float, min_val: int, max_val: int) -> int:
    """Return a value drawn from a bell curve and clamped within a range."""
    value = int(np.clip(np.random.normal(mean, std_dev), min_val, max_val))
    return value

def generate_attributes_for_position(position: str) -> Tuple[Dict[str, int], Dict[str, int]]:
    """Generate attribute values and hard caps for the given ``position``.

    Parameters
    ----------
    position:
        Standardized position code such as ``"QB"`` or ``"RB"``.

    Returns
    -------
    tuple of dict
        ``(attributes, caps)`` where ``attributes`` is the starting rating
        mapping and ``caps`` contains the corresponding hard caps for each
        attribute.
    """
    attr_profile = POSITION_RELEVANCE.get(position.upper(), {})
    attributes: Dict[str, int] = {}
    caps: Dict[str, int] = {}

    for attr, relevance in attr_profile.items():
        cap_low, cap_high = RELEVANCE_CAP_RANGES[relevance]
        cap_mean = (cap_low + cap_high) / 2
        cap = bell_curve_sample(cap_mean, 7, cap_low, cap_high)
        caps[attr] = cap

        gen_low, gen_high = RELEVANCE_GEN_RANGES[relevance]
        gen_mean = (gen_low + gen_high) / 2
        start_mean = min(gen_mean, cap - 5)
        start_val = bell_curve_sample(start_mean, 5, gen_low, cap)
        attributes[attr] = start_val

    return attributes, caps
