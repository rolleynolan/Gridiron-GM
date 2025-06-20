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
        "return_skill": "very_low",
    },
    "RB": {
        "speed": "high",
        "acceleration": "high",
        "agility": "high",
        "ball_carrier_vision": "high",
        "elusiveness": "high",
        "break_tackle": "high",
        "trucking": "medium",
        "carry_security": "high",
        "juke_move": "high",
        "hurdle": "medium",
        "spin_move": "medium",
        "route_running_short": "medium",
        "route_running_mid": "medium",
        "catching": "medium",
        "pass_block": "low",
        "throw_power": "low",
        "iq": "medium",
        "awareness": "medium",
        "tackling": "low",
        "return_skill": "medium",
    },
    "WR": {
        "speed": "high",
        "acceleration": "high",
        "agility": "high",
        "catching": "high",
        "catch_in_traffic": "high",
        "spectacular_catch": "high",
        "release": "high",
        "separation": "high",
        "route_running_short": "high",
        "route_running_mid": "high",
        "route_running_deep": "high",
        "blocking": "low",
        "iq": "medium",
        "awareness": "medium",
        "tackling": "low",
        "return_skill": "medium",
    },
    "TE": {
        "catching": "high",
        "catch_in_traffic": "high",
        "release": "medium",
        "route_running_short": "medium",
        "route_running_mid": "medium",
        "route_running_deep": "medium",
        "separation": "medium",
        "run_block": "high",
        "pass_block": "high",
        "lead_blocking": "high",
        "strength": "high",
        "speed": "medium",
        "iq": "medium",
        "awareness": "medium",
        "tackling": "low",
        "return_skill": "very_low",
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
        "return_skill": "very_low",
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
        "return_skill": "very_low",
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
        "return_skill": "very_low",
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
        "return_skill": "low",
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
        "return_skill": "medium",
    },
    "S": {
        "coverage": "high",
        "speed": "high",
        "tackling": "high",
        "catching": "medium",
        "iq": "medium",
        "awareness": "high",
        "return_skill": "low",
    },
    "K": {
        "kick_power": "high",
        "kick_accuracy": "high",
        "iq": "medium",
        "awareness": "medium",
        "catching": "low",
        "tackling": "low",
        "return_skill": "very_low",
    },
    "P": {
        "punt_power": "high",
        "punt_accuracy": "high",
        "iq": "medium",
        "awareness": "medium",
        "catching": "low",
        "tackling": "low",
        "return_skill": "very_low",
    },
}

# Rating caps based on attribute relevance
RELEVANCE_CAP_RANGES: Dict[str, Tuple[int, int]] = {
    "high": (80, 99),
    "medium": (60, 85),
    "low": (20, 45),
    "very_low": (20, 40),
}

# Starting attribute values based on relevance
RELEVANCE_GEN_RANGES: Dict[str, Tuple[int, int]] = {
    "high": (70, 90),
    "medium": (55, 75),
    "low": (15, 40),
    "very_low": (10, 35),
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
