"""Utilities for generating player attributes based on positional relevance."""

from __future__ import annotations

import numpy as np
from typing import Dict, Tuple

# Attribute relevance map per position.  Each position includes universal
# core attributes ``iq``, ``awareness``, ``tackling`` and ``catching`` so that
# every player receives a full baseline attribute profile.
POSITION_RELEVANCE: Dict[str, Dict[str, str]] = {
    "QB": {
        "throw_power": "high",
        "throw_accuracy_short": "high",
        "throw_accuracy_medium": "high",
        "throw_accuracy_deep": "high",
        "speed": "low",
        "agility": "low",
        "iq": "high",
        "awareness": "medium",
        "catching": "low",
        "tackling": "low",
    },
    "RB": {
        "speed": "high",
        "acceleration": "high",
        "agility": "high",
        "elusiveness": "high",
        "carry_security": "high",
        "route_running": "medium",
        "catching": "medium",
        "blocking": "low",
        "throw_power": "low",
        "iq": "medium",
        "awareness": "medium",
        "tackling": "low",
    },
    "WR": {
        "speed": "high",
        "acceleration": "high",
        "catching": "high",
        "route_running_short": "high",
        "route_running_deep": "high",
        "blocking": "low",
        "iq": "medium",
        "awareness": "medium",
        "tackling": "low",
    },
    "TE": {
        "catching": "high",
        "route_running_short": "medium",
        "route_running_deep": "medium",
        "blocking": "high",
        "strength": "high",
        "speed": "medium",
        "iq": "medium",
        "awareness": "medium",
        "tackling": "low",
    },
    "OL": {
        "blocking": "high",
        "strength": "high",
        "agility": "medium",
        "iq": "medium",
        "awareness": "medium",
        "speed": "low",
        "catching": "low",
        "tackling": "low",
    },
    "EDGE": {
        "pass_rush": "high",
        "block_shedding": "high",
        "tackling": "high",
        "speed": "medium",
        "strength": "high",
        "iq": "medium",
        "awareness": "medium",
        "catching": "low",
    },
    "DL": {
        "strength": "high",
        "block_shedding": "high",
        "run_defense": "high",
        "pass_rush": "medium",
        "speed": "low",
        "iq": "medium",
        "awareness": "medium",
        "catching": "low",
        "tackling": "high",
    },
    "LB": {
        "tackling": "high",
        "coverage": "medium",
        "speed": "medium",
        "block_shedding": "medium",
        "pass_rush": "medium",
        "strength": "medium",
        "iq": "high",
        "awareness": "high",
        "catching": "medium",
    },
    "CB": {
        "speed": "high",
        "agility": "high",
        "coverage": "high",
        "catching": "medium",
        "tackling": "medium",
        "pass_rush": "low",
        "iq": "medium",
        "awareness": "high",
    },
    "S": {
        "coverage": "high",
        "speed": "high",
        "tackling": "high",
        "catching": "medium",
        "iq": "medium",
        "awareness": "high",
    },
    "K": {
        "kick_power": "high",
        "kick_accuracy": "high",
        "iq": "medium",
        "awareness": "medium",
        "catching": "low",
        "tackling": "low",
    },
    "P": {
        "punt_power": "high",
        "punt_accuracy": "high",
        "iq": "medium",
        "awareness": "medium",
        "catching": "low",
        "tackling": "low",
    },
}

# Rating caps based on attribute relevance
RELEVANCE_CAP_RANGES: Dict[str, Tuple[int, int]] = {
    "high": (80, 99),
    "medium": (60, 85),
    "low": (20, 45),
}

# Starting attribute values based on relevance
RELEVANCE_GEN_RANGES: Dict[str, Tuple[int, int]] = {
    "high": (70, 90),
    "medium": (55, 75),
    "low": (15, 40),
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
