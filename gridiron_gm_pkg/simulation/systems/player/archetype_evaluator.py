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


# -- LB Archetype definitions used by ``evaluate_lb_archetype`` --
LB_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Field General": {
        "awareness": 1.0,
        "play_recognition": 0.95,
        "tackle_lb": 0.9,
        "leadership": 0.85,
        "discipline": 0.8,
    },
    "Coverage Backer": {
        "coverage": 1.0,
        "awareness": 0.95,
        "speed": 0.9,
        "agility": 0.85,
        "tackle_lb": 0.75,
    },
    "Thumper": {
        "tackle_lb": 1.0,
        "hit_power": 0.95,
        "strength": 0.9,
        "toughness": 0.85,
        "awareness": 0.75,
    },
    "Chase & Tackle": {
        "pursuit_lb": 1.0,
        "speed": 0.95,
        "acceleration": 0.9,
        "agility": 0.85,
        "tackle_lb": 0.8,
    },
    "Box General": {
        "block_shedding": 1.0,
        "tackle_lb": 0.95,
        "discipline": 0.9,
        "awareness": 0.85,
        "play_recognition": 0.8,
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


# -- WR Archetype definitions used by ``evaluate_wr_archetype`` --
WR_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Deep Threat": {
        "speed": 1.0,
        "acceleration": 0.9,
        "route_running_deep": 0.9,
        "catching": 0.7,
        "release": 0.6,
    },
    "Possession Receiver": {
        "catch_in_traffic": 1.0,
        "route_running_short": 0.9,
        "awareness": 0.8,
        "catching": 0.8,
    },
    "Red Zone Threat": {
        "spectacular_catch": 1.0,
        "release": 0.9,
        "catch_in_traffic": 0.9,
        "jumping": 0.8,
        "strength": 0.7,
    },
    "YAC Specialist": {
        "elusiveness": 1.0,
        "agility": 0.9,
        "route_running_short": 0.8,
        "acceleration": 0.8,
        "carry_security": 0.6,
    },
    "Route Technician": {
        "route_running_short": 1.0,
        "route_running_mid": 0.95,
        "release": 0.9,
        "awareness": 0.8,
        "iq": 0.7,
    },
    "Return Specialist": {
        "return_skill": 1.0,
        "speed": 0.95,
        "acceleration": 0.9,
        "agility": 0.85,
        "elusiveness": 0.8,
        "carry_security": 0.7,
    },
}


# -- TE Archetype definitions used by ``evaluate_te_archetype`` --
TE_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Receiving TE": {
        "catching": 1.0,
        "catch_in_traffic": 0.9,
        "route_running_short": 0.85,
        "awareness": 0.8,
        "release": 0.75,
    },
    "Blocking TE": {
        "run_block": 1.0,
        "lead_blocking": 0.9,
        "strength": 0.9,
        "impact_blocking": 0.85,
        "awareness": 0.7,
    },
    "Vertical Threat": {
        "speed": 1.0,
        "acceleration": 0.9,
        "catching": 0.85,
        "route_running_deep": 0.85,
        "release": 0.8,
    },
    "H-Back": {
        "lead_blocking": 1.0,
        "awareness": 0.9,
        "route_running_short": 0.8,
        "catching": 0.75,
        "agility": 0.7,
    },
}

# -- OL Archetype definitions used by ``evaluate_ol_archetype`` --
OL_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Pass Protector": {
        "pass_block": 1.0,
        "footwork_ol": 0.95,
        "awareness": 0.9,
        "discipline": 0.85,
        "impact_blocking": 0.75,
    },
    "Run Blocker": {
        "run_block": 1.0,
        "lead_blocking": 0.95,
        "impact_blocking": 0.9,
        "footwork_ol": 0.75,
        "strength": 0.7,
    },
    "Pure Power": {
        "strength": 1.0,
        "impact_blocking": 0.95,
        "block_shed_resistance": 0.9,
        "run_block": 0.75,
        "lead_blocking": 0.7,
    },
    "Technical Blocker": {
        "footwork_ol": 1.0,
        "discipline": 0.95,
        "awareness": 0.9,
        "pass_block": 0.85,
        "run_block": 0.75,
    },
    "Anchor": {
        "block_shed_resistance": 1.0,
        "pass_block": 0.95,
        "strength": 0.9,
        "awareness": 0.8,
        "discipline": 0.75,
    },
}


# -- DB Archetype definitions used by ``evaluate_db_archetype`` --
CB_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Man Cover Corner": {
        "man_coverage": 1.0,
        "speed": 0.95,
        "agility": 0.9,
        "acceleration": 0.85,
        "awareness": 0.75,
    },
    "Zone Corner": {
        "zone_coverage": 1.0,
        "awareness": 0.95,
        "play_recognition": 0.9,
        "catching": 0.75,
        "tackling": 0.7,
    },
    "Ballhawk": {
        "catching": 1.0,
        "zone_coverage": 0.95,
        "awareness": 0.9,
        "discipline": 0.8,
    },
    "Press Technician": {
        "press_coverage": 1.0,
        "strength": 0.9,
        "balance": 0.85,
        "man_coverage": 0.8,
        "awareness": 0.75,
    },
    "Slot Specialist": {
        "agility": 1.0,
        "acceleration": 0.95,
        "man_coverage": 0.9,
        "discipline": 0.85,
        "zone_coverage": 0.7,
    },
}

S_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Center Fielder": {
        "zone_coverage": 1.0,
        "speed": 0.95,
        "awareness": 0.9,
        "catching": 0.85,
    },
    "Box Safety": {
        "tackling": 1.0,
        "block_shedding": 0.95,
        "awareness": 0.9,
        "strength": 0.8,
    },
    "Hybrid Rover": {
        "coverage": 1.0,
        "tackling": 0.95,
        "speed": 0.9,
        "awareness": 0.85,
        "versatility": 0.8,
    },
    "Ballhawk Safety": {
        "catching": 1.0,
        "zone_coverage": 0.95,
        "awareness": 0.9,
        "discipline": 0.85,
    },
    "Match Safety": {
        "man_coverage": 1.0,
        "agility": 0.95,
        "speed": 0.9,
        "awareness": 0.85,
        "discipline": 0.8,
    },
}

# -- Special Teams Archetype definitions used by ``evaluate_special_teams_archetype`` --
KICKER_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Power Kicker": {
        "kick_power": 1.0,
        "strength": 0.8,
        "kick_accuracy": 0.7,
        "discipline": 0.6,
    },
    "Precision Kicker": {
        "kick_accuracy": 1.0,
        "discipline": 0.9,
        "kick_power": 0.7,
        "awareness": 0.6,
    },
}

PUNTER_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Boomer": {
        "punt_power": 1.0,
        "kick_power": 0.9,
        "discipline": 0.6,
        "awareness": 0.6,
    },
    "Coffin Corner Specialist": {
        "punt_accuracy": 1.0,
        "awareness": 0.9,
        "discipline": 0.8,
        "kick_power": 0.6,
    },
}

RETURNER_ARCHETYPES: Dict[str, Dict[str, float]] = {
    "Explosive Returner": {
        "return_skill": 1.0,
        "speed": 0.95,
        "acceleration": 0.9,
        "elusiveness": 0.85,
        "agility": 0.8,
    },
    "Reliable Returner": {
        "return_skill": 1.0,
        "catching": 0.9,
        "discipline": 0.85,
        "awareness": 0.8,
    },
    "Versatile Returner": {
        "return_skill": 1.0,
        "elusiveness": 0.9,
        "vision": 0.85,
        "agility": 0.8,
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


def evaluate_wr_archetype(attributes: Dict[str, int]) -> str:
    """Return the most likely WR archetype based on weighted attributes.

    Parameters
    ----------
    attributes:
        Mapping of attribute ratings for a wide receiver. Values should range
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

    for archetype, profile in WR_ARCHETYPES.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type


def evaluate_te_archetype(attributes: Dict[str, int]) -> str:
    """Return the most likely TE archetype based on weighted attributes.

    Parameters
    ----------
    attributes:
        Mapping of attribute ratings for a tight end. Values should range
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

    for archetype, profile in TE_ARCHETYPES.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type


def evaluate_ol_archetype(attributes: Dict[str, int]) -> str:
    """Return the most likely OL archetype based on weighted attributes.

    Parameters
    ----------
    attributes:
        Mapping of attribute ratings for an offensive lineman. Values should
        range from 20 to 99.

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

    for archetype, profile in OL_ARCHETYPES.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type


def evaluate_edge_archetype(attributes: dict) -> str:
    """Determine EDGE archetype (DE/OLB) based on key attributes."""
    weights = {
        "Speed Rusher": (
            attributes.get("pass_rush_finesse", 0) * 1.0
            + attributes.get("speed", 0) * 0.95
            + attributes.get("acceleration", 0) * 0.9
            + attributes.get("agility", 0) * 0.85
            + attributes.get("pursuit_dl", 0) * 0.75
        ),
        "Power Rusher": (
            attributes.get("pass_rush_power", 0) * 1.0
            + attributes.get("strength", 0) * 0.95
            + attributes.get("block_shedding", 0) * 0.9
            + attributes.get("balance", 0) * 0.8
            + attributes.get("toughness", 0) * 0.75
        ),
        "Hybrid Rusher": (
            attributes.get("pass_rush_power", 0) * 0.85
            + attributes.get("pass_rush_finesse", 0) * 0.85
            + attributes.get("block_shedding", 0) * 0.85
            + attributes.get("awareness", 0) * 0.75
            + attributes.get("pursuit_dl", 0) * 0.7
        ),
        "Edge Setter": (
            attributes.get("run_defense", 0) * 1.0
            + attributes.get("block_shedding", 0) * 0.95
            + attributes.get("strength", 0) * 0.9
            + attributes.get("tackle_dl", 0) * 0.85
            + attributes.get("awareness", 0) * 0.75
        ),
        "Pure Athlete": (
            attributes.get("speed", 0) * 1.0
            + attributes.get("acceleration", 0) * 0.95
            + attributes.get("agility", 0) * 0.9
            + attributes.get("pass_rush_power", 0) * 0.5
            + attributes.get("awareness", 0) * 0.4
        ),
        "Technical Edge": (
            attributes.get("hands", 0) * 1.0
            + attributes.get("pass_rush_finesse", 0) * 0.95
            + attributes.get("awareness", 0) * 0.9
            + attributes.get("block_shedding", 0) * 0.85
            + attributes.get("pursuit_dl", 0) * 0.75
        ),
    }
    return max(weights, key=weights.get)


def evaluate_idl_archetype(attributes: dict) -> str:
    """Determine DT/IDL archetype based on key attributes."""
    weights = {
        "Run Stuffer": (
            attributes.get("run_defense", 0) * 1.0
            + attributes.get("block_shedding", 0) * 0.95
            + attributes.get("strength", 0) * 0.9
            + attributes.get("tackle_dl", 0) * 0.85
            + attributes.get("awareness", 0) * 0.8
        ),
        "Gap Penetrator": (
            attributes.get("pass_rush_finesse", 0) * 1.0
            + attributes.get("acceleration", 0) * 0.95
            + attributes.get("agility", 0) * 0.9
            + attributes.get("pursuit_dl", 0) * 0.85
            + attributes.get("awareness", 0) * 0.75
        ),
        "Nose Tackle": (
            attributes.get("block_shedding", 0) * 1.0
            + attributes.get("strength", 0) * 0.95
            + attributes.get("toughness", 0) * 0.9
            + attributes.get("balance", 0) * 0.85
            + attributes.get("run_defense", 0) * 0.8
        ),
        "Power DT": (
            attributes.get("pass_rush_power", 0) * 1.0
            + attributes.get("strength", 0) * 0.95
            + attributes.get("block_shedding", 0) * 0.9
            + attributes.get("balance", 0) * 0.8
            + attributes.get("toughness", 0) * 0.75
        ),
        "Technical DT": (
            attributes.get("hands", 0) * 1.0
            + attributes.get("pass_rush_finesse", 0) * 0.95
            + attributes.get("awareness", 0) * 0.9
            + attributes.get("block_shedding", 0) * 0.85
            + attributes.get("pursuit_dl", 0) * 0.75
        ),
    }
    return max(weights, key=weights.get)


def evaluate_lb_archetype(attributes: dict) -> str:
    """Return the most likely linebacker archetype based on weighted attributes."""

    def norm(val: int) -> float:
        return max(0.0, min((val - 20) / 79, 1.0))

    best_type = ""
    best_score = float("-inf")

    for archetype, profile in LB_ARCHETYPES.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type


def evaluate_db_archetype(attributes: Dict[str, int], position: str) -> str:
    """Return the best defensive back archetype for the given position."""

    def norm(val: int) -> float:
        return max(0.0, min((val - 20) / 79, 1.0))

    pos_key = position.upper()
    if pos_key == "CB":
        archetypes = CB_ARCHETYPES
    else:
        archetypes = S_ARCHETYPES

    best_type = ""
    best_score = float("-inf")

    for archetype, profile in archetypes.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type


def evaluate_special_teams_archetype(attributes: Dict[str, int], position: str) -> str:
    """Return the best special teams archetype based on the player's position."""

    def norm(val: int) -> float:
        return max(0.0, min((val - 20) / 79, 1.0))

    pos_key = position.upper()
    if pos_key == "K":
        archetypes = KICKER_ARCHETYPES
    elif pos_key == "P":
        archetypes = PUNTER_ARCHETYPES
    else:
        archetypes = RETURNER_ARCHETYPES

    best_type = ""
    best_score = float("-inf")

    for archetype, profile in archetypes.items():
        score = 0.0
        for attr, weight in profile.items():
            if attr in attributes:
                score += norm(int(attributes[attr])) * weight
        if score > best_score:
            best_score = score
            best_type = archetype

    return best_type

