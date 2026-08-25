from __future__ import annotations

import bisect
import random
from typing import Any, Dict, Iterable

from gridiron_gm_pkg.simulation.systems.player.injury_status import iter_league_players
from gridiron_gm_pkg.simulation.systems.player.player_dna import (
    PHYSICAL_ATTRIBUTE_NAMES,
    PHYSICAL_TOKENS,
    MutationType,
)

TOTAL_XP = 1_000_000
MAX_RATING = 99


def _clamp_rating(value: Any) -> int:
    try:
        rating = int(round(value))
    except (TypeError, ValueError):
        rating = 0
    return max(0, min(MAX_RATING, rating))


def _clamp_xp(value: Any) -> int:
    try:
        xp = int(round(value))
    except (TypeError, ValueError):
        xp = 0
    return max(0, min(TOTAL_XP, xp))


_COST_59 = 100 + 92 * 59
_COST_84 = _COST_59 + 287 * 24 + 23 * 24 * 24


def _step_cost(rating: int) -> int:
    if rating <= 59:
        return 100 + 92 * rating
    if rating <= 84:
        t = rating - 60
        return _COST_59 + 287 * t + 23 * t * t
    u = rating - 85
    return _COST_84 + 560 * u + 52 * u * u + 5 * u * u * u


def _spread_diff_across_tail(costs: list[int], diff: int, tail_len: int = 5) -> None:
    if diff == 0:
        return

    tail_len = min(tail_len, len(costs))
    while True:
        start = len(costs) - tail_len
        tail = costs[start:]
        tail_sum = sum(tail)
        if tail_sum == 0:
            costs[-1] = max(costs[-1] + diff, costs[-2])
            return

        adjustments = [int(round(diff * cost / tail_sum)) for cost in tail]
        remainder = diff - sum(adjustments)
        adjustments[-1] += remainder
        new_tail = [cost + adj for cost, adj in zip(tail, adjustments)]

        prev = costs[start - 1] if start > 0 else 0
        for idx in range(len(new_tail)):
            if new_tail[idx] < prev:
                new_tail[idx] = prev
            if idx > 0 and new_tail[idx] < new_tail[idx - 1]:
                new_tail[idx] = new_tail[idx - 1]
            prev = new_tail[idx]

        applied = sum(new_tail) - tail_sum
        remaining = diff - applied
        if remaining < 0:
            remaining = -remaining
            for idx in range(len(new_tail) - 1, -1, -1):
                min_allowed = new_tail[idx - 1] if idx > 0 else (costs[start - 1] if start > 0 else 0)
                max_reduction = new_tail[idx] - min_allowed
                if max_reduction <= 0:
                    continue
                take = min(max_reduction, remaining)
                new_tail[idx] -= take
                remaining -= take
                if remaining == 0:
                    break
            if remaining != 0:
                if tail_len >= len(costs):
                    costs[start:] = new_tail
                    return
                tail_len = min(len(costs), tail_len + 1)
                continue
        elif remaining > 0:
            new_tail[-1] += remaining

        costs[start:] = new_tail
        return


def _build_xp_table() -> list[int]:
    costs = [_step_cost(rating) for rating in range(MAX_RATING)]
    diff = TOTAL_XP - sum(costs)
    if diff != 0:
        last_idx = len(costs) - 1
        proposed = costs[last_idx] + diff
        if proposed >= costs[last_idx - 1] and proposed > 0:
            costs[last_idx] = proposed
        else:
            _spread_diff_across_tail(costs, diff)

    xp_table = [0]
    running = 0
    for cost in costs:
        running += cost
        xp_table.append(running)
    return xp_table


XP_TABLE = _build_xp_table()


def xp_at_value(rating: int) -> int:
    rating = _clamp_rating(rating)
    return XP_TABLE[rating]


def rating_from_xp(xp_total: int) -> int:
    xp_total = _clamp_xp(xp_total)
    idx = bisect.bisect_right(XP_TABLE, xp_total) - 1
    return max(0, min(MAX_RATING, idx))


def _get_attr_container(player: Any, attr: str) -> tuple[Dict[str, int], str] | tuple[None, None]:
    attrs = getattr(player, "attributes", None)
    if attrs is None:
        return None, None
    core = getattr(attrs, "core", {})
    if attr in core:
        return core, "core"
    pos = getattr(attrs, "position_specific", {})
    if attr in pos:
        return pos, "position_specific"
    return None, None


def _get_attr_value(player: Any, attr: str) -> int:
    container, _ = _get_attr_container(player, attr)
    if container is not None:
        return _clamp_rating(container.get(attr, 0))
    if hasattr(player, attr):
        return _clamp_rating(getattr(player, attr, 0))
    return 0


def _set_attr_value(player: Any, attr: str, value: int) -> None:
    container, _ = _get_attr_container(player, attr)
    if container is not None:
        container[attr] = _clamp_rating(value)
        return
    if hasattr(player, attr):
        setattr(player, attr, _clamp_rating(value))


def _iter_attribute_names(player: Any) -> Iterable[str]:
    if hasattr(player, "get_relevant_attribute_names"):
        return player.get_relevant_attribute_names()
    attrs = getattr(player, "attributes", None)
    names: list[str] = []
    if attrs is not None:
        core = getattr(attrs, "core", {})
        if isinstance(core, dict):
            names.extend(core.keys())
        pos = getattr(attrs, "position_specific", {})
        if isinstance(pos, dict):
            names.extend(pos.keys())
    return names


def _resolve_cap_value(player: Any, attr: str, cap_value: int | None) -> int | None:
    if cap_value is not None:
        return _clamp_rating(cap_value)
    dna = getattr(player, "dna", None)
    if dna is not None:
        caps = getattr(dna, "attribute_caps", None)
        if isinstance(caps, dict):
            info = caps.get(attr)
            if isinstance(info, dict) and info.get("hard_cap") is not None:
                return _clamp_rating(info.get("hard_cap"))
    hidden_caps = getattr(player, "hidden_caps", None)
    if isinstance(hidden_caps, dict) and attr in hidden_caps:
        return _clamp_rating(hidden_caps.get(attr))
    return None


def sync_xp_from_rating(player: Any) -> Dict[str, int]:
    xp_map = getattr(player, "attribute_xp", None)
    if not isinstance(xp_map, dict):
        xp_map = {}
        player.attribute_xp = xp_map

    for attr in _iter_attribute_names(player):
        current = xp_map.get(attr)
        if current is None:
            rating = _get_attr_value(player, attr)
            xp_map[attr] = xp_at_value(rating)
            continue
        xp_map[attr] = _clamp_xp(current)
    return xp_map


def apply_xp_to_player(player: Any) -> None:
    xp_map = getattr(player, "attribute_xp", None)
    if not isinstance(xp_map, dict):
        return
    for attr in _iter_attribute_names(player):
        xp_val = xp_map.get(attr)
        if xp_val is None:
            continue
        xp_val = _clamp_xp(xp_val)
        cap_value = _resolve_cap_value(player, attr, None)
        if cap_value is not None:
            xp_val = min(xp_val, xp_at_value(cap_value))
        xp_map[attr] = xp_val
        _set_attr_value(player, attr, rating_from_xp(xp_val))
    if hasattr(player, "normalize_ratings"):
        player.normalize_ratings()


def add_xp(player: Any, attr: str, xp_delta: Any, cap_value: int | None = None) -> int:
    xp_map = getattr(player, "attribute_xp", None)
    if not isinstance(xp_map, dict):
        xp_map = sync_xp_from_rating(player)

    current_xp = xp_map.get(attr)
    current_rating = _get_attr_value(player, attr)
    if current_xp is None:
        current_xp = xp_at_value(current_rating)
    else:
        current_xp = _clamp_xp(current_xp)
        if rating_from_xp(current_xp) != current_rating:
            current_xp = xp_at_value(current_rating)

    delta = 0
    try:
        delta = int(round(xp_delta))
    except (TypeError, ValueError):
        delta = 0

    xp_total = _clamp_xp(current_xp + delta)
    cap_value = _resolve_cap_value(player, attr, cap_value)
    if cap_value is not None:
        xp_total = min(xp_total, xp_at_value(cap_value))

    xp_map[attr] = xp_total
    new_rating = rating_from_xp(xp_total)
    _set_attr_value(player, attr, new_rating)
    if hasattr(player, "normalize_ratings"):
        player.normalize_ratings()
    return new_rating


def _attribute_decay_type(player: Any, attr: str) -> str:
    dna = getattr(player, "dna", None)
    if dna is not None:
        decay_map = getattr(dna, "attribute_decay_type", None)
        if isinstance(decay_map, dict):
            typ = decay_map.get(attr)
            if typ:
                return str(typ)
    lower = str(attr).lower()
    if lower in PHYSICAL_ATTRIBUTE_NAMES or any(token in lower for token in PHYSICAL_TOKENS):
        return "physical"
    if any(token in lower for token in ("awareness", "iq", "recognition", "discipline", "consistency", "vision")):
        return "mental"
    return "skill"


def weekly_decay_xp(
    player: Any,
    attr: str,
    current_date: Any | None = None,
    year: int | None = None,
    week: int | None = None,
    rng: random.Random | None = None,
) -> int:
    _ = current_date
    _ = year
    _ = week
    age = int(getattr(player, "age", 0) or 0)
    if age <= 0:
        return 0

    decay_type = _attribute_decay_type(player, attr)
    dna = getattr(player, "dna", None)
    profile = getattr(dna, "regression_profile", {}) if dna is not None else {}
    if not isinstance(profile, dict):
        profile = {}

    start_age = int(profile.get("start_age", 30) or 30)
    if decay_type == "skill":
        start_age += 2
    elif decay_type == "mental":
        start_age += 4

    if age < start_age:
        return 0

    base_rate = float(profile.get("rate", 0.04) or 0.04)
    position = getattr(player, "position", "")
    position_factor = 1.0
    pos_map = profile.get("position_modifier")
    if isinstance(pos_map, dict):
        position_factor = float(pos_map.get(position, 1.0) or 1.0)

    decay_mult = {"physical": 1.0, "skill": 0.5, "mental": 0.25}.get(decay_type, 0.5)
    age_over = max(0, age - start_age)
    age_factor = 1.0 + age_over * 0.08

    mutations = getattr(dna, "mutations", []) if dna is not None else []
    if MutationType.BuiltToLast in mutations:
        age_factor *= 0.5

    rating = _get_attr_value(player, attr)
    if rating <= 0:
        return 0

    step_xp = max(1, xp_at_value(rating) - xp_at_value(max(0, rating - 1)))
    weekly_rate = base_rate / 52.0
    xp_loss = step_xp * weekly_rate * position_factor * decay_mult * age_factor
    if rng is not None:
        xp_loss *= rng.uniform(0.9, 1.1)
    xp_loss = int(round(xp_loss))
    return max(1, xp_loss)


def _derive_seed(base_seed: int, label: str) -> int:
    seed = int(base_seed) & 0xFFFFFFFF
    for ch in str(label):
        seed = (seed * 31 + ord(ch)) & 0xFFFFFFFF
    return seed


def apply_weekly_decay(
    league: Any,
    year: int | None = None,
    week: int | None = None,
    current_date: Any | None = None,
) -> bool:
    if league is None:
        return False

    cal = getattr(league, "calendar", None)
    if year is None:
        year = getattr(cal, "current_year", None)
    if week is None:
        week = getattr(cal, "current_week", None)
    if current_date is None:
        current_date = getattr(cal, "current_date", None)
    if year is None or week is None:
        return False

    token = f"{year}-W{week}"
    if getattr(league, "last_weekly_decay", None) == token:
        return False

    base_seed = getattr(league, "base_seed", None)
    if base_seed is None:
        base_seed = 0
        league.base_seed = base_seed

    for player in iter_league_players(league):
        if player is None:
            continue
        for attr in _iter_attribute_names(player):
            seed = _derive_seed(base_seed, f"decay|{token}|{player.id}|{attr}")
            rng = random.Random(seed)
            xp_loss = weekly_decay_xp(
                player,
                attr,
                current_date=current_date,
                year=year,
                week=week,
                rng=rng,
            )
            if xp_loss > 0:
                add_xp(player, attr, -xp_loss)

    league.last_weekly_decay = token
    return True
