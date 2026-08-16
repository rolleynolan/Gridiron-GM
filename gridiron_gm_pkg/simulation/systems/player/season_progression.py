"""Deterministic, season-end player progression.

This module owns the once-per-season development update.  It deliberately does
not advance the calendar, construct rosters, or execute transactions; those
responsibilities remain with the season and franchise systems.
"""

from __future__ import annotations

import random
from typing import Any, Dict

from gridiron_gm_pkg.simulation.systems.player.attribute_xp import (
    _attribute_decay_type,
    _get_attr_value,
    add_xp,
    sync_xp_from_rating,
    xp_at_value,
)
from gridiron_gm_pkg.simulation.systems.player.injury_status import iter_league_players


def _seed_for(base_seed: int, *parts: object) -> int:
    value = int(base_seed) & 0xFFFFFFFF
    for part in parts:
        for char in str(part):
            value = (value * 31 + ord(char)) & 0xFFFFFFFF
    return value


def _attribute_names(player: Any) -> list[str]:
    getter = getattr(player, "get_relevant_attribute_names", None)
    return list(getter()) if callable(getter) else []


def _growth_chance(player: Any, attr: str) -> float:
    age = int(getattr(player, "age", 0) or 0)
    speed = float(getattr(getattr(player, "dna", None), "dev_speed", 0.65) or 0.65)
    if age <= 23:
        chance = 0.55
    elif age <= 25:
        chance = 0.35
    elif age <= 27:
        chance = 0.14
    else:
        return 0.0
    if _attribute_decay_type(player, attr) == "mental":
        chance += 0.05
    return max(0.0, min(0.85, chance * speed / 0.65))


def _retirement_reason(player: Any, rng: random.Random) -> str | None:
    age = int(getattr(player, "age", 0) or 0)
    overall = int(getattr(player, "overall", 0) or 0)
    if age >= 41:
        return "age_limit"
    if age < 35:
        return None
    chance = 0.0
    if age >= 39:
        chance = 0.65
    elif age == 38:
        chance = 0.30
    elif age == 37:
        chance = 0.12
    elif age == 36:
        chance = 0.04
    if overall < 60:
        chance += 0.15
    elif overall >= 80:
        chance -= 0.10
    return "age_decline" if rng.random() < max(0.0, min(0.95, chance)) else None


def apply_season_progression(league: Any, year: int | None = None) -> Dict[str, Any]:
    """Apply exactly one development update for ``year`` and return its summary."""
    if league is None:
        return {"applied": False, "reason": "missing_league", "players": []}
    if year is None:
        year = getattr(getattr(league, "calendar", None), "current_year", None)
    if year is None:
        return {"applied": False, "reason": "missing_year", "players": []}

    token = str(int(year))
    if getattr(league, "last_season_progression", None) == token:
        return {"applied": False, "reason": "already_applied", "year": int(year), "players": []}

    base_seed = int(getattr(league, "base_seed", 0) or 0)
    seen_ids: set[str] = set()
    summaries: list[Dict[str, Any]] = []
    for player in iter_league_players(league):
        player_id = str(getattr(player, "id", id(player)))
        if player_id in seen_ids or getattr(player, "retired", False):
            continue
        seen_ids.add(player_id)

        age_before = int(getattr(player, "age", 0) or 0)
        player.age = age_before + 1
        sync_xp_from_rating(player)
        changes: Dict[str, int] = {}
        for attr in _attribute_names(player):
            rating_before = _get_attr_value(player, attr)
            rng = random.Random(_seed_for(base_seed, "season_progression", token, player_id, attr))
            if rng.random() < _growth_chance(player, attr):
                add_xp(player, attr, xp_at_value(min(99, rating_before + 1)) - xp_at_value(rating_before))
            rating_after = _get_attr_value(player, attr)
            if rating_after != rating_before:
                changes[attr] = rating_after - rating_before

        current = {attr: _get_attr_value(player, attr) for attr in _attribute_names(player)}
        player.last_attribute_values = current
        player.no_growth_years = {
            attr: (0 if changes.get(attr, 0) > 0 else int(getattr(player, "no_growth_years", {}).get(attr, 0)) + 1)
            for attr in current
        }
        history = getattr(player, "progress_history", None)
        if not isinstance(history, dict):
            history = {}
            player.progress_history = history
        history[token] = {"age": player.age, "changes": changes, "overall": int(getattr(player, "overall", 0) or 0)}

        retirement_rng = random.Random(_seed_for(base_seed, "retirement", token, player_id))
        reason = _retirement_reason(player, retirement_rng)
        if reason:
            player.retired = True
            player.retirement_reason = reason
            player.retirement_year = int(year)
        summaries.append({"player_id": player_id, "changes": changes, "retired": bool(reason)})

    league.last_season_progression = token
    return {"applied": True, "year": int(year), "players": summaries}
