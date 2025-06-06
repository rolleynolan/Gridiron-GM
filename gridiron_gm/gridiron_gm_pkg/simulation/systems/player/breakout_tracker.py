"""Dynamic breakout game detection and attribute boost system."""

from __future__ import annotations

import random
from typing import Any, Dict, Iterable

# Basic mapping of position to attributes that can be boosted on a breakout
POSITION_ATTRIBUTES = {
    "WR": [
        "catching",
        "route_running_short",
        "route_running_mid",
        "route_running_deep",
        "separation",
    ],
    "RB": [
        "speed",
        "elusiveness",
        "break_tackle",
        "carry_security",
        "ball_carrier_vision",
    ],
    "QB": [
        "throw_power",
        "throw_accuracy_short",
        "throw_accuracy_mid",
        "throw_accuracy_deep",
        "awareness",
    ],
    "TE": [
        "catching",
        "route_running_short",
        "route_running_mid",
        "route_running_deep",
        "strength",
    ],
    "DL": [
        "pass_rush_power",
        "pass_rush_finesse",
        "strength",
    ],
    "LB": [
        "tackle_lb",
        "play_recognition_lb",
        "block_shedding",
    ],
    "CB": [
        "man_coverage",
        "zone_coverage",
        "awareness",
    ],
}


def _get_attr_containers(player: Any) -> tuple[Dict[str, int], Dict[str, int]]:
    """Return core and position-specific attribute dictionaries."""
    attrs = getattr(player, "attributes", None)
    if attrs is None:
        return {}, {}
    if isinstance(attrs, dict):
        core = attrs.get("core", {})
        pos = attrs.get("position_specific", {})
    else:
        core = getattr(attrs, "core", {})
        pos = getattr(attrs, "position_specific", {})
    return core, pos


def _get_season_data(player: Any, year: str) -> tuple[Dict[str, Any], Dict[str, Any], Dict[int, Dict[str, Any]]]:
    season = getattr(player, "season_stats", {}).get(year, {})
    totals = season.get("season_totals", {})
    logs = season.get("game_logs", {})
    return season, totals, logs


def _prior_games(logs: Dict[int, Dict[str, Any]], week_number: int) -> int:
    return sum(1 for w in logs if int(w) < week_number)


def _value_list(stats_list: Iterable[Dict[str, Any]], stat: str) -> list[int | float]:
    vals = []
    for s in stats_list:
        val = s.get(stat)
        if isinstance(val, (int, float)):
            vals.append(val)
    return vals


def check_breakout_game(player: Any, game_stats: Dict[str, int | float], league_context: Dict[str, Any], week_number: int) -> bool:
    """Detect a breakout game and apply attribute boosts.

    Parameters
    ----------
    player : Any
        Player object with ``overall``, ``dev_arc`` and stat containers.
    game_stats : dict
        Stat line from the game to evaluate.
    league_context : dict
        Context including at minimum ``current_year`` and optional weekly stats.
    week_number : int
        Week number of the game.

    Returns
    -------
    bool
        True if a breakout was detected and applied.
    """
    year = str(league_context.get("current_year", ""))

    # Ensure single breakout per season
    if not hasattr(player, "breakout_log"):
        player.breakout_log = {}
    for info in player.breakout_log.values():
        if info.get("season") == year:
            return False

    # Do not trigger for established stars
    overall = getattr(player, "overall", 0)
    dev_type = getattr(getattr(player, "dev_arc", None), "type", "").lower()
    if overall >= 80 or dev_type in {"star", "x_factor"}:
        return False

    # Basic experience/usage check
    career_snaps = sum(getattr(player, "career_stats", {}).get("snap_counts", {}).values())
    if career_snaps > 500 or getattr(player, "experience", 0) > 2:
        return False

    season, totals, logs = _get_season_data(player, year)

    prior_games = _prior_games(logs, week_number)
    # Determine YTD totals before this game
    prev_totals: Dict[str, float] = {}
    for stat, val in game_stats.items():
        total = totals.get(stat, 0)
        if week_number in logs:
            total -= game_stats.get(stat, 0)
        prev_totals[stat] = max(0.0, float(total))

    def _is_big_jump(stat: str, val: float) -> bool:
        prev = prev_totals.get(stat, 0.0)
        avg = prev / max(1, prior_games)
        return val >= 3 * max(avg, 1)

    breakout = False
    for stat, val in game_stats.items():
        if not isinstance(val, (int, float)):
            continue
        if _is_big_jump(stat, float(val)):
            breakout = True
            break

    # If not a huge jump, see if this stat ranks in top 5% of position this week
    if not breakout:
        wk_stats = (
            league_context.get("weekly_stats", {})
            .get(week_number, {})
            .get(getattr(player, "position", ""), [])
        )
        for stat, val in game_stats.items():
            values = _value_list(wk_stats, stat)
            if not values:
                continue
            values.sort(reverse=True)
            idx = max(0, int(len(values) * 0.05) - 1)
            if idx < len(values) and val >= values[idx] and val > 0:
                breakout = True
                break

    if not breakout:
        return False

    core, pos = _get_attr_containers(player)
    possible_attrs = POSITION_ATTRIBUTES.get(getattr(player, "position", ""), list(pos.keys()) or list(core.keys()))
    if not possible_attrs:
        return False

    num_attrs = random.randint(1, min(3, len(possible_attrs)))
    selected = random.sample(possible_attrs, num_attrs)

    boosts: Dict[str, int] = {}
    for attr in selected:
        inc = random.randint(1, 5)
        if attr in pos:
            pos[attr] = pos.get(attr, 0) + inc
        else:
            core[attr] = core.get(attr, 0) + inc
        boosts[attr] = inc

    player.breakout_log[week_number] = {
        "season": year,
        "stats": dict(game_stats),
        "boosts": boosts,
    }
    return True

