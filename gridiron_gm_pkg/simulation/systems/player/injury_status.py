from __future__ import annotations

import datetime
import random
from typing import Any, Iterator

_VALID_STATUSES = {"healthy", "questionable", "out", "ir"}
_SEVERITY_LEVELS = {"minor": 1, "moderate": 2, "severe": 3}


def normalize_injury_status(value: Any) -> str:
    value = str(value or "").strip().lower()
    return value if value in _VALID_STATUSES else "healthy"


def training_multiplier(player: Any) -> float:
    status = normalize_injury_status(getattr(player, "injury_status", "healthy"))
    if status != "healthy":
        return 0.0
    if getattr(player, "on_injured_reserve", False):
        return 0.0
    return 1.0


def is_available_for_game(player: Any) -> bool:
    status = normalize_injury_status(getattr(player, "injury_status", "healthy"))
    if status in {"out", "ir"}:
        return False
    if getattr(player, "on_injured_reserve", False):
        return False
    return True


def _coerce_date(value: Any) -> datetime.date | None:
    if value is None:
        return None
    if isinstance(value, datetime.datetime):
        return value.date()
    if isinstance(value, datetime.date):
        return value
    if isinstance(value, str):
        try:
            return datetime.date.fromisoformat(value)
        except ValueError:
            return None
    return None


def _severity_to_level(severity: Any) -> int | None:
    if severity is None:
        return None
    if isinstance(severity, (int, float)):
        level = int(severity)
        return max(0, min(3, level))
    label = str(severity).strip().lower()
    return _SEVERITY_LEVELS.get(label)


def clear_injury_fields(player: Any) -> None:
    player.injury_status = "healthy"
    player.injury_name = None
    player.injury_start_date = None
    player.injury_end_date = None
    player.injury_severity = None


def heal_if_due(player: Any, current_date: Any) -> bool:
    current = _coerce_date(current_date)
    if current is None:
        return False
    end_date = _coerce_date(getattr(player, "injury_end_date", None))
    if end_date is None:
        return False
    if current >= end_date:
        clear_injury_fields(player)
        return True
    return False


def heal_player_if_due(player: Any, current_date: Any) -> bool:
    return heal_if_due(player, current_date)


def iter_league_players(league: Any) -> Iterator[Any]:
    for team in getattr(league, "teams", []) or []:
        for group in ("roster", "ir_list", "practice_squad"):
            for player in getattr(team, group, []) or []:
                yield player
    for group in ("free_agents", "draft_prospects", "college_db"):
        for player in getattr(league, group, []) or []:
            yield player


def heal_league_players(league: Any, current_date: datetime.date) -> int:
    healed = 0
    for player in iter_league_players(league):
        if heal_if_due(player, current_date):
            healed += 1
    return healed


def apply_simple_injury(
    player: Any,
    start_date: Any,
    duration_days: int,
    injury_name: str,
    severity: Any = None,
    status: str | None = None,
) -> None:
    start = _coerce_date(start_date) or datetime.date.today()
    duration = max(1, int(duration_days))
    if status is None:
        status = "questionable" if duration <= 7 else "out"
    player.injury_status = normalize_injury_status(status)
    player.injury_name = injury_name
    player.injury_start_date = start
    player.injury_end_date = start + datetime.timedelta(days=duration)
    player.injury_severity = _severity_to_level(severity)


def convert_legacy_injury_fields(player: Any, current_date: Any) -> bool:
    """Convert legacy weeks_out/is_injured fields into status/end date.

    Returns True if a conversion occurred.
    """
    status = normalize_injury_status(getattr(player, "injury_status", "healthy"))
    weeks_out = getattr(player, "weeks_out", 0) or 0
    is_injured = bool(getattr(player, "is_injured", False))
    if status == "healthy" and is_injured and weeks_out > 0:
        start = _coerce_date(current_date) or datetime.date.today()
        player.injury_status = "out"
        if not getattr(player, "injury_name", None):
            player.injury_name = "Legacy Injury"
        player.injury_start_date = start
        player.injury_end_date = start + datetime.timedelta(days=int(weeks_out) * 7)
        player.injury_severity = _severity_to_level(getattr(player, "injury_severity", None))
        player.weeks_out = 0
        player.is_injured = False
        return True

    # Ensure legacy flags are neutralized going forward
    player.weeks_out = 0
    player.is_injured = False
    return False


def assign_catalog_injury(
    player: Any,
    current_date: Any,
    context: str = "game",
    rng: Any = None,
) -> dict | None:
    status = normalize_injury_status(getattr(player, "injury_status", "healthy"))
    if status != "healthy" or getattr(player, "on_injured_reserve", False):
        return None

    from gridiron_gm_pkg.config.injury_catalog import INJURY_CATALOG

    rng = rng or random
    allowed_contexts = {"either"}
    if str(context).lower() == "game":
        allowed_contexts.add("on_field")
    else:
        allowed_contexts.add("off_field")

    candidates = [
        (name, data)
        for name, data in (INJURY_CATALOG or {}).items()
        if data.get("injury_context", "on_field") in allowed_contexts
    ]
    if not candidates:
        candidates = list((INJURY_CATALOG or {}).items())

    if not candidates:
        injury_name = "Injury"
        injury_data = {"severity": "minor", "weeks": (1, 1)}
    else:
        injury_name, injury_data = rng.choice(candidates)

    weeks_range = injury_data.get("weeks", (1, 1))
    try:
        weeks_out = rng.randint(int(weeks_range[0]), int(weeks_range[1]))
    except (TypeError, ValueError, IndexError):
        weeks_out = 1
    duration_days = max(1, int(weeks_out) * 7)
    severity_label = injury_data.get("severity", "")
    severity_level = _severity_to_level(severity_label)

    apply_simple_injury(
        player,
        current_date,
        duration_days,
        injury_name,
        severity=severity_level,
    )
    return {
        "injury_name": injury_name,
        "severity": severity_label,
        "severity_level": severity_level,
        "status": getattr(player, "injury_status", "out"),
        "start_date": getattr(player, "injury_start_date", None),
        "end_date": getattr(player, "injury_end_date", None),
        "duration_days": duration_days,
    }


def assign_game_injuries(home_team: Any, away_team: Any, current_date: Any, rng: Any = None) -> list[dict]:
    rng = rng or random
    roll = rng.random()
    if roll < 0.02:
        count = 2
    elif roll < 0.08:
        count = 1
    else:
        return []

    candidates = []
    for team in (home_team, away_team):
        roster = getattr(team, "roster", None) or getattr(team, "players", [])
        for player in roster:
            status = normalize_injury_status(getattr(player, "injury_status", "healthy"))
            if status != "healthy":
                continue
            candidates.append(player)

    if not candidates:
        return []

    rng.shuffle(candidates)
    injuries: list[dict] = []
    for player in candidates[: min(count, len(candidates))]:
        injury_payload = assign_catalog_injury(player, current_date, context="game", rng=rng)
        if not injury_payload:
            continue
        injuries.append(
            {
                "player_id": getattr(player, "id", None),
                "player_name": getattr(player, "name", None),
                "injury_name": injury_payload.get("injury_name"),
                "severity": injury_payload.get("severity"),
                "status": injury_payload.get("status"),
                "start_date": injury_payload.get("start_date"),
                "end_date": injury_payload.get("end_date"),
                "duration_days": injury_payload.get("duration_days"),
            }
        )
    return injuries
