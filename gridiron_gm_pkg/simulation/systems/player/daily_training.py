"""Daily team training system."""

from __future__ import annotations

import random
from datetime import date as date_type
from typing import Dict, Any, Iterable

from gridiron_gm_pkg.config.training_catalog import TRAINING_CATALOG
from gridiron_gm_pkg.simulation.systems.player.injury_manager import InjuryEngine
from gridiron_gm_pkg.simulation.systems.player.weekly_training import _get_attr_container

_DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
_injury_engine = InjuryEngine()


def _extract_week_day(date: Any) -> tuple[str, str]:
    """Return week number string and capitalized day name from various date types."""
    if hasattr(date, "current_week") and hasattr(date, "current_day_index"):
        return str(date.current_week), _DAYS[date.current_day_index]
    if isinstance(date, tuple) and len(date) == 2:
        week, day = date
        day_name = day if isinstance(day, str) else _DAYS[int(day)]
        return str(week), day_name.capitalize()
    if isinstance(date, date_type):
        week = date.isocalendar().week
        return str(week), date.strftime("%A")
    # Fallback: treat as mapping
    week = str(getattr(date, "week", 0))
    day = getattr(date, "day", "Monday")
    return week, str(day).capitalize()


def should_train_today(team: Any, date: Any, schedule_by_team: Dict[str, Iterable[Dict[str, Any]]]) -> bool:
    """Return True if the team can hold training on the given date."""
    week, day_name = _extract_week_day(date)

    games = schedule_by_team.get(getattr(team, "id", None), [])
    for game in games:
        if str(game.get("week")) == week and str(game.get("day", "")).capitalize() == day_name:
            return False

    # Check travel day (day before an away game)
    idx = _DAYS.index(day_name)
    next_day = _DAYS[(idx + 1) % 7]
    next_week = str(int(week) + 1) if idx == 6 else week
    for game in games:
        if (
            str(game.get("day", "")).capitalize() == next_day
            and not game.get("home", True)
            and str(game.get("week")) in {week, next_week}
        ):
            return False
    return True


class CoachTrainingAI:
    """Simple auto-assignment helper for training drills."""

    def choose_drill(self, player: Any) -> str | None:
        position = str(getattr(player, "position", "")).upper()
        matches = [
            name
            for name, drill in TRAINING_CATALOG.items()
            if drill.get("positions") == "ALL" or position in drill.get("positions", [])
        ]
        return random.choice(matches) if matches else None


def assign_training(team: Any, date: Any) -> Dict[str, str]:
    """Return mapping of player_id to drill name for today's training."""
    assignments: Dict[str, str] = {}
    date_key = str(date)
    manual = getattr(team, "training_schedule", {}).get(date_key, {})
    assignments.update(manual)

    available_players = [p for p in getattr(team, "roster", []) if not getattr(p, "is_injured", False)]
    available_players = [p for p in available_players if p.id not in assignments]
    random.shuffle(available_players)
    ai = CoachTrainingAI()
    slots = max(0, 3 - len(assignments))
    for player in available_players[:slots]:
        drill = ai.choose_drill(player)
        if drill:
            assignments[player.id] = drill
    return assignments


def apply_training(team: Any, date: Any, drill_assignments: Dict[str, str]) -> None:
    """Apply drill effects to the assigned players."""
    if not drill_assignments:
        return

    for player in getattr(team, "roster", []):
        drill_name = drill_assignments.get(player.id)
        if not drill_name:
            continue
        drill = TRAINING_CATALOG.get(drill_name, {})
        for attr, weight in drill.get("attribute_weights", {}).items():
            container, _ = _get_attr_container(player, attr)
            if container is None and hasattr(player, attr):
                container = player.__dict__
            if container is None:
                continue
            container[attr] = container.get(attr, 0) + weight
        chance = drill.get("injury_chance", 0.0)
        if chance and random.random() < chance:
            _injury_engine.assign_injury(player)


def run_daily_training(date: Any, league_teams: Iterable[Any], schedule_by_team: Dict[str, Any]) -> None:
    """Run training for all teams for the given date."""
    for team in league_teams:
        if should_train_today(team, date, schedule_by_team):
            assignments = assign_training(team, date)
            apply_training(team, date, assignments)


def log_season_progress_checkpoint(date: Any, all_players: Iterable[Any], checkpoint_label: str) -> None:
    """Record attribute snapshot for each player with provided label."""
    year = getattr(date, "year", None)
    if year is None and isinstance(date, (tuple, list)):
        year = date[0]
    label = f"{year}-{checkpoint_label}"
    for player in all_players:
        hist = getattr(player, "progress_history", {})
        snapshot: Dict[str, Any] = {}
        if hasattr(player, "get_all_attributes"):
            snapshot.update(player.get_all_attributes())
        else:
            attrs = getattr(player, "attributes", None)
            if attrs is not None:
                snapshot.update(getattr(attrs, "core", {}))
                snapshot.update(getattr(attrs, "position_specific", {}))
        for field, val in player.__dict__.items():
            if field.startswith("_"):
                continue
            if isinstance(val, (int, float)) and field not in snapshot:
                snapshot[field] = val
        hist[label] = snapshot
        # Keep only most recent two snapshots
        if len(hist) > 2:
            for key in sorted(hist.keys())[:-2]:
                hist.pop(key, None)
        player.progress_history = hist

