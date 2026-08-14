from __future__ import annotations

import datetime
import logging
import os
import shutil
import stat
import uuid
from collections import defaultdict
from pathlib import Path
from typing import Any, Dict

from gridiron_gm_pkg.simulation.career.gm_profile import CareerHistoryEntry, GMProfile
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.api.schemas import STATE_SCHEMA_VERSION
from gridiron_gm_pkg.simulation.persistence.savegame import load_league, save_league
from gridiron_gm_pkg.simulation.systems.core.team_data import (
    fill_team_rosters_with_dummy_players,
    load_teams_from_json,
)
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager
from gridiron_gm_pkg.simulation.systems.time_engine import TimeEngine, make_game_id, parse_clock_hour
from gridiron_gm_pkg.simulation.roster.roster_rules import get_roster_rule_violations
from gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm_pkg.simulation.utils.generate_schedule import (
    ensure_schedule_exists,
    generate_full_schedule_files,
    FULL_SEASON_WEEKS,
    PRESEASON_BYE_WEEK,
    PRESEASON_WEEKS,
    REGULAR_SEASON_WEEKS,
    REGULAR_SEASON_START_WEEK,
)
from gridiron_gm_pkg.simulation.utils.box_score import sanitize_box_score_numbers


class GameFacade:
    _DEPTH_CHART_REQUIREMENTS: tuple[tuple[str, int], ...] = (
        ("QB", 1),
        ("RB", 1),
        ("WR", 2),
        ("TE", 1),
        ("LT", 1),
        ("LG", 1),
        ("C", 1),
        ("RG", 1),
        ("RT", 1),
        ("DE", 2),
        ("DT", 2),
        ("LB", 3),
        ("CB", 2),
        ("S", 2),
        ("K", 1),
        ("P", 1),
    )
    _DEPTH_CHART_FLEX_ALIASES: dict[str, tuple[str, ...]] = {
        "LT": ("LT", "OL"),
        "LG": ("LG", "OL"),
        "C": ("C", "OL"),
        "RG": ("RG", "OL"),
        "RT": ("RT", "OL"),
        "DE": ("DE", "EDGE", "DL"),
        "DT": ("DT", "DL"),
        "CB": ("CB", "DB"),
        "S": ("S", "DB"),
    }

    def __init__(self, save_name: str = "api_save") -> None:
        self.save_name = save_name
        self.league: LeagueManager | None = None
        self.calendar: Calendar | None = None
        self.season_manager: SeasonManager | None = None
        self.debug_pot = False
        self.debug_schedule = False
        self._pot_debugged = False
        self._time_engine: TimeEngine | None = None
        self._continue_in_progress = False
        self._continue_stop_requested = False
        self._last_continue_result: Dict[str, Any] | None = None

    def _inject_dict_access(self, league: LeagueManager) -> None:
        if not hasattr(league, "get"):
            league.get = lambda key, default=None: getattr(league, key, default)
        if not hasattr(league, "__getitem__"):
            league.__getitem__ = lambda key: getattr(league, key)

    def _ensure_game(self) -> None:
        if self.league is None or self.calendar is None or self.season_manager is None:
            self.new_game()

    def new_game(self, gm_name: str | None = None, team_id: str | None = None) -> Dict[str, Any]:
        config_path = (
            Path(__file__).resolve().parents[2] / "config" / "teams.json"
        )
        teams = load_teams_from_json(config_path)
        fill_team_rosters_with_dummy_players(teams)

        league = LeagueManager()
        for team in teams:
            league.add_team(team)
        self._inject_dict_access(league)

        calendar = Calendar()
        season_manager = SeasonManager(calendar, league, self.save_name)
        self.league = league
        self.calendar = calendar
        self.season_manager = season_manager
        self._clear_derived_files(season_manager)
        schedule = ensure_schedule_exists(self.save_name, league, calendar)
        self._sync_schedule(season_manager, schedule)

        selected_team_id = self._resolve_team_id(team_id)
        if selected_team_id is None and self.league.teams:
            selected_team_id = self.league.teams[0].id
        self._set_controlled_team_context(selected_team_id)
        self._ensure_user_gm_profile(gm_name=gm_name, team_id=selected_team_id, persist=True)
        self._time_engine = None
        self._last_continue_result = None
        self._get_time_engine().ensure_agenda_for_today()
        return self.get_state()

    def reset_save(self, save_path: str | Path | None = None) -> Dict[str, Any]:
        warnings: list[str] = []

        def on_remove_error(func, path, exc_info) -> None:
            try:
                os.chmod(path, stat.S_IWRITE)
                func(path)
            except Exception as exc:
                warnings.append(f"Failed to remove '{path}': {exc}")

        def is_probably_invalid_path(value: str) -> bool:
            if not value:
                return True
            if "\0" in value:
                return True
            if "://" in value:
                if len(value) >= 3 and value[1] == ":" and value[2] == "/":
                    return False
                return True
            return False

        try:
            cleaned_save_path = ""
            if save_path is not None:
                try:
                    cleaned_save_path = str(save_path).strip().strip("'\"").strip()
                except Exception as exc:
                    warnings.append(f"Failed to parse save path: {exc}")

            if cleaned_save_path:
                if is_probably_invalid_path(cleaned_save_path):
                    warnings.append(f"Skipping invalid save path '{cleaned_save_path}'.")
                else:
                    try:
                        path = Path(cleaned_save_path)
                    except Exception as exc:
                        warnings.append(f"Invalid save path '{cleaned_save_path}': {exc}")
                    else:
                        try:
                            is_file = path.is_file()
                        except Exception as exc:
                            warnings.append(f"Failed to stat save file '{cleaned_save_path}': {exc}")
                            is_file = False
                        if is_file:
                            try:
                                path.unlink()
                            except Exception as exc:
                                warnings.append(f"Failed to delete save file '{cleaned_save_path}': {exc}")

            save_name = str(self.save_name or "").strip()
            if save_name:
                save_dir = Path(__file__).resolve().parents[2] / "data" / "saves" / save_name
                try:
                    save_dir_exists = save_dir.exists()
                except Exception as exc:
                    warnings.append(f"Failed to stat save dir '{save_dir}': {exc}")
                    save_dir_exists = False
                if save_dir_exists:
                    try:
                        shutil.rmtree(save_dir, onerror=on_remove_error)
                    except Exception as exc:
                        warnings.append(f"Failed to delete save dir '{save_dir}': {exc}")
        except Exception as exc:
            warnings.append(f"Unexpected reset error: {exc}")

        try:
            new_game_state = self.new_game()
        except Exception as exc:
            warnings.append(f"Failed to start new game: {exc}")
            return {"ok": False, "reset_warnings": warnings, "error": str(exc)}

        return {"ok": True, "reset_warnings": warnings, **new_game_state}

    def advance_day(self) -> Dict[str, Any]:
        self._ensure_game()
        result = self._get_time_engine().advance_day()
        return self._compact_advance_response(result)

    def request_continue_stop(self) -> Dict[str, Any]:
        was_active = bool(self._continue_in_progress)
        self._continue_stop_requested = True
        message = (
            "Continue stop requested. Simulation will pause at the next safe point."
            if was_active
            else "No continue operation is active."
        )
        return {
            "stop_requested": was_active,
            "message": message,
        }

    def save(self, path: str | Path) -> Dict[str, Any]:
        self._ensure_game()
        save_league(path, self.league)
        return {"ok": True, "path": str(path)}

    def load(self, path: str | Path) -> Dict[str, Any]:
        league = load_league(path)
        self._inject_dict_access(league)
        calendar = getattr(league, "calendar", Calendar())
        self.league = league
        self.calendar = calendar
        self.season_manager = SeasonManager(calendar, league, self.save_name)
        schedule = ensure_schedule_exists(self.save_name, league, calendar)
        self._sync_schedule(self.season_manager, schedule)
        self._normalize_loaded_user_context()
        self._time_engine = None
        self._last_continue_result = None
        self._get_time_engine().ensure_agenda_for_today()
        return self.get_state()

    def set_user_team(self, team_id: str) -> Dict[str, Any]:
        self._ensure_game()
        if not team_id:
            return {"ok": False, "error": "missing_team_id"}
        team = self.league.id_to_team.get(team_id) if hasattr(self.league, "id_to_team") else None
        if team is None:
            return {"ok": False, "error": "team_not_found"}
        old_team_id = getattr(self.league, "user_team_id", None)
        self._set_controlled_team_context(team_id)
        self._ensure_user_gm_profile(team_id=team_id, persist=True)
        engine = self._get_time_engine()
        today = engine.clock.current_date
        team_ids = {tid for tid in (old_team_id, team_id) if tid}
        agenda_types = {"InboxCheck", "TrainingSlot", "Travel", "GameKickoff", "GameWrap"}
        if team_ids:
            def _payload_matches_team(payload: Any) -> bool:
                if not isinstance(payload, dict):
                    return False
                if payload.get("team_id") in team_ids:
                    return True
                if payload.get("home_id") in team_ids or payload.get("away_id") in team_ids:
                    return True
                game_id = payload.get("game_id")
                if game_id:
                    parts = str(game_id).split("|")
                    if len(parts) >= 3 and (parts[1] in team_ids or parts[2] in team_ids):
                        return True
                return False

            engine.queue.remove_matching(
                lambda event: event.type in agenda_types
                and event.date == today
                and _payload_matches_team(event.payload)
            )
            for inbox_team_id in list(team_ids):
                messages = list(engine.inboxes.get(inbox_team_id, []))
                if not messages:
                    continue
                filtered_messages = []
                for msg in messages:
                    subject = str(getattr(msg, "subject", "") or "")
                    if not subject.lower().startswith("kickoff:"):
                        filtered_messages.append(msg)
                        continue
                    payload = getattr(msg, "payload", {}) or {}
                    game_id = ""
                    if isinstance(payload, dict):
                        game_id = str(payload.get("game_id") or "")
                    if not game_id:
                        for action in getattr(msg, "actions", []) or []:
                            if not isinstance(action, dict):
                                continue
                            game_id = str(
                                action.get("game_id")
                                or ((action.get("payload") or {}).get("game_id") if isinstance(action.get("payload"), dict) else "")
                                or ""
                            )
                            if game_id:
                                break
                    if not game_id:
                        filtered_messages.append(msg)
                        continue
                    parts = game_id.split("|")
                    if len(parts) >= 3 and (parts[1] in team_ids or parts[2] in team_ids):
                        continue
                    filtered_messages.append(msg)
                engine.inboxes[inbox_team_id] = filtered_messages
            self.league.inboxes = engine.inboxes
        self.league.last_agenda_date = None
        self._time_engine = None
        self._get_time_engine().ensure_agenda_for_today()
        payload: Dict[str, Any] = {"ok": True, "user_team_id": team_id}
        payload["user_team_abbr"] = getattr(team, "abbreviation", None)
        payload["user_team_name"] = getattr(team, "team_name", None) or getattr(team, "name", None)
        return payload

    def _resolve_team_id(self, team_id: str | None) -> str | None:
        requested = str(team_id or "").strip()
        if requested:
            if hasattr(self.league, "id_to_team") and requested in self.league.id_to_team:
                return requested
            if hasattr(self.league, "abbr_to_id") and requested in self.league.abbr_to_id:
                return self.league.abbr_to_id.get(requested)
        for attr_name in ("controlled_team_id", "user_team_id", "selected_team_id"):
            candidate = str(getattr(self.league, attr_name, None) or "").strip()
            if candidate and hasattr(self.league, "id_to_team") and candidate in self.league.id_to_team:
                return candidate
        return None

    def _set_controlled_team_context(self, team_id: str | None) -> None:
        self.league.controlled_team_id = team_id
        self.league.user_team_id = team_id

    def _normalize_loaded_user_context(self) -> None:
        controlled_team_id = self._resolve_team_id(getattr(self.league, "controlled_team_id", None))
        if controlled_team_id is None and self.league.teams:
            controlled_team_id = self.league.teams[0].id
        self._set_controlled_team_context(controlled_team_id)
        self._ensure_user_gm_profile(team_id=controlled_team_id, persist=True)

    def _default_gm_profile(self, gm_name: str | None = None, team_id: str | None = None) -> GMProfile:
        current_year = int(getattr(self.calendar, "current_year", 0) or 0)
        team = self.league.id_to_team.get(team_id) if team_id and hasattr(self.league, "id_to_team") else None
        history: list[CareerHistoryEntry] = []
        if team is not None:
            team_name = getattr(team, "team_name", None) or getattr(team, "name", None) or ""
            history = [
                CareerHistoryEntry(
                    team_id=team.id,
                    team_name=str(team_name),
                    role="General Manager",
                    start_year=current_year,
                    end_year=None,
                    wins=0,
                    losses=0,
                    playoff_appearances=0,
                    championships=0,
                    status="active",
                )
            ]
        return GMProfile(
            gm_id=uuid.uuid4().hex,
            name=str(gm_name or "User GM"),
            current_team_id=team_id,
            reputation=50,
            job_security=50,
            career_start_year=current_year,
            current_role="General Manager",
            career_history=history,
            traits=[],
            created_at_date=getattr(self.calendar, "current_date", None).isoformat()
            if getattr(self.calendar, "current_date", None) is not None
            else None,
        )

    def _sync_gm_history(self, gm_profile: GMProfile, team_id: str | None) -> None:
        team = self.league.id_to_team.get(team_id) if team_id and hasattr(self.league, "id_to_team") else None
        team_name = ""
        if team is not None:
            team_name = str(getattr(team, "team_name", None) or getattr(team, "name", None) or "")
        active_entry = next(
            (entry for entry in gm_profile.career_history if str(getattr(entry, "status", "")) == "active"),
            None,
        )
        if team_id is None:
            if active_entry is not None:
                active_entry.team_id = ""
                active_entry.team_name = ""
            return
        if active_entry is None:
            gm_profile.career_history.append(
                CareerHistoryEntry(
                    team_id=team_id,
                    team_name=team_name,
                    role=gm_profile.current_role or "General Manager",
                    start_year=int(gm_profile.career_start_year or getattr(self.calendar, "current_year", 0) or 0),
                    end_year=None,
                    wins=0,
                    losses=0,
                    playoff_appearances=0,
                    championships=0,
                    status="active",
                )
            )
            return
        active_entry.team_id = team_id
        active_entry.team_name = team_name
        active_entry.role = gm_profile.current_role or "General Manager"
        active_entry.status = "active"

    def _ensure_user_gm_profile(
        self,
        *,
        gm_name: str | None = None,
        team_id: str | None = None,
        persist: bool = False,
    ) -> GMProfile:
        raw_profile = getattr(self.league, "user_gm", None)
        gm_profile = raw_profile if isinstance(raw_profile, GMProfile) else GMProfile.from_dict(raw_profile)
        if not getattr(gm_profile, "name", None):
            gm_profile.name = str(gm_name or "User GM")
        elif gm_name:
            gm_profile.name = str(gm_name)
        if not getattr(gm_profile, "career_start_year", 0):
            gm_profile.career_start_year = int(getattr(self.calendar, "current_year", 0) or 0)
        if not getattr(gm_profile, "created_at_date", None):
            current_date = getattr(self.calendar, "current_date", None)
            gm_profile.created_at_date = current_date.isoformat() if current_date is not None else None
        gm_profile.current_role = str(getattr(gm_profile, "current_role", None) or "General Manager")
        if gm_profile.reputation is None:
            gm_profile.reputation = 50
        if gm_profile.job_security is None:
            gm_profile.job_security = 50
        resolved_team_id = team_id if team_id is not None else getattr(self.league, "controlled_team_id", None)
        gm_profile.current_team_id = resolved_team_id
        self._sync_gm_history(gm_profile, resolved_team_id)
        if persist:
            self.league.user_gm = gm_profile
        return gm_profile

    def get_state(self) -> Dict[str, Any]:
        self._ensure_game()
        payload = {
            "schema_version": STATE_SCHEMA_VERSION,
            "calendar": self.calendar.serialize(),
            "league": self.league.to_dict() if hasattr(self.league, "to_dict") else {},
            "save_name": self.save_name,
        }
        payload["time_engine"] = self._get_time_payload()
        payload.update(self.get_schedule_context())
        return payload

    def get_state_summary(self) -> Dict[str, Any]:
        self._ensure_game()
        gm_profile = self._ensure_user_gm_profile(
            team_id=getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
        )
        teams = []
        for team in self.league.teams:
            record = getattr(team, "team_record", {})
            teams.append(
                {
                    "id": team.id,
                    "team_name": team.team_name,
                    "city": team.city,
                    "abbreviation": team.abbreviation,
                    "conference": team.conference,
                    "division": team.division,
                    "wins": record.get("wins", 0),
                    "losses": record.get("losses", 0),
                }
            )
        payload = {
            "schema_version": STATE_SCHEMA_VERSION,
            "calendar": self.calendar.serialize(),
            "league": {"teams": teams},
            "save_name": self.save_name,
            "user_team_id": getattr(self.league, "user_team_id", None),
            "user": {
                "gm_id": gm_profile.gm_id,
                "gm_name": gm_profile.name,
                "team_id": gm_profile.current_team_id,
                "current_role": gm_profile.current_role,
            },
        }
        team = None
        if getattr(self.league, "user_team_id", None) and hasattr(self.league, "id_to_team"):
            team = self.league.id_to_team.get(self.league.user_team_id)
        payload["user_team_abbr"] = getattr(team, "abbreviation", None) if team else None
        user_team_name = None
        if team is not None:
            user_team_name = getattr(team, "team_name", None) or getattr(team, "name", None)
        payload["user_team_name"] = user_team_name
        payload["time_engine"] = self._get_time_payload()
        payload.update(self.get_schedule_context())
        return payload

    def get_dashboard_state(self) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        team_id = (
            getattr(self.league, "controlled_team_id", None)
            or getattr(self.league, "user_team_id", None)
        )
        team = self._dashboard_team(team_id)
        schedule_context = self.get_schedule_context()
        next_game = (
            schedule_context.get("user_team_game_today")
            or schedule_context.get("user_team_next_game")
            or schedule_context.get("next_game")
        )
        calendar_payload = self.calendar.serialize() if hasattr(self.calendar, "serialize") else {}

        return {
            "ok": True,
            "dashboard": {
                "team": {
                    "name": self._dashboard_team_name(team),
                    "abbreviation": self._dashboard_team_abbreviation(team, team_id),
                    "record": self._dashboard_team_record(team, team_id),
                },
                "calendar": {
                    "year": self._safe_int(
                        calendar_payload.get("season_year", calendar_payload.get("current_year")),
                        0,
                    ),
                    "week": self._safe_int(
                        calendar_payload.get("football_week", calendar_payload.get("current_week")),
                        0,
                    ),
                    "phase": str(
                        calendar_payload.get("season_phase")
                        or calendar_payload.get("phase_label")
                        or ""
                    ),
                },
                "next_game": self._dashboard_next_game_payload(next_game, team_id),
                "team_status": {
                    "roster_size": self._dashboard_roster_size(team),
                    "injuries": self._dashboard_injury_count(team_id),
                    "cap_room": self._dashboard_cap_room(team),
                },
                "action_items": self._build_dashboard_action_items(
                    team=team,
                    team_id=team_id,
                    calendar_payload=calendar_payload,
                    next_game=next_game,
                ),
                "recent_results": self._build_dashboard_recent_results(team_id),
            },
        }

    def get_api_state(self) -> Dict[str, Any]:
        return self.get_state_snapshot()

    def has_active_game(self) -> bool:
        return self.league is not None and self.calendar is not None and self.season_manager is not None

    def get_state_snapshot(
        self,
        save_path: str | None = None,
        *,
        inbox_limit: int = 10,
        today_games_limit: int = 10,
    ) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "no_active_game"}

        engine = self._get_time_engine()
        calendar_payload = self.calendar.serialize() if hasattr(self.calendar, "serialize") else {}
        schedule_context = self.get_schedule_context()
        gm_profile = self._ensure_user_gm_profile(
            team_id=getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
        )
        user_team_id = gm_profile.current_team_id
        resolved_save_path = str(save_path or "").strip() or self.save_name
        resolved_save_name = os.path.basename(resolved_save_path) if resolved_save_path else self.save_name

        today_games = schedule_context.get("games_today", [])
        if not isinstance(today_games, list):
            today_games = []
        compact_today_games = [
            self._compact_scheduled_game_payload(game)
            for game in today_games[: max(0, int(today_games_limit))]
            if isinstance(game, dict)
        ]

        return {
            "ok": True,
            "schema_version": STATE_SCHEMA_VERSION,
            "save_name": resolved_save_name,
            "save_path": resolved_save_path,
            "calendar": self._compact_calendar_snapshot(calendar_payload),
            "user": {
                "gm_id": gm_profile.gm_id,
                "gm_name": gm_profile.name,
                "team_id": user_team_id,
                "current_role": gm_profile.current_role,
            },
            "today": {
                "has_user_game": bool(schedule_context.get("user_team_game_today")),
                "user_game": self._compact_scheduled_game_payload(schedule_context.get("user_team_game_today")),
                "league_games_count": int(schedule_context.get("league_games_today_count", 0) or 0),
                "games": compact_today_games,
            },
            "next_user_game": self._compact_scheduled_game_payload(schedule_context.get("user_team_next_game")),
            "standings_summary": self._compact_standings_summary(),
            "inbox_summary": self._compact_inbox_summary(engine, limit=inbox_limit),
            "available_actions": ["continue", "advance_day", "sim_until"],
            "games_today": compact_today_games,
            "next_game": self._compact_scheduled_game_payload(schedule_context.get("next_game")),
            "next_game_date": schedule_context.get("next_game_date"),
            "next_game_label": schedule_context.get("next_game_label"),
            "can_simulate_today": bool(schedule_context.get("can_simulate_today")),
            "user_team_game_today": self._compact_scheduled_game_payload(schedule_context.get("user_team_game_today")),
            "user_team_next_game": self._compact_scheduled_game_payload(schedule_context.get("user_team_next_game")),
            "user_team_can_simulate": bool(schedule_context.get("user_team_can_simulate")),
            "league_games_today_count": int(schedule_context.get("league_games_today_count", 0) or 0),
        }

    def _compact_calendar_snapshot(self, calendar_payload: Dict[str, Any]) -> Dict[str, Any]:
        payload = calendar_payload if isinstance(calendar_payload, dict) else {}
        return {
            "current_date": str(payload.get("current_date") or ""),
            "current_time": str(payload.get("current_time") or payload.get("current_time_str") or "00:00"),
            "day_of_week": str(payload.get("day_of_week") or ""),
            "season_year": self._safe_int(payload.get("season_year", payload.get("current_year")), 0),
            "season_phase": str(payload.get("season_phase") or ""),
            "football_week": self._safe_int(payload.get("football_week", payload.get("current_week")), 0),
            "current_year": self._safe_int(payload.get("current_year", payload.get("season_year")), 0),
            "current_week": self._safe_int(payload.get("current_week", payload.get("football_week")), 0),
            "phase_label": str(payload.get("phase_label") or payload.get("season_phase_label") or ""),
            "week_label": str(payload.get("week_label") or ""),
        }

    def _compact_scheduled_game_payload(self, game: Any) -> Dict[str, Any] | None:
        if not isinstance(game, dict):
            return None
        return {
            "game_id": str(game.get("game_id") or ""),
            "date": str(game.get("date") or ""),
            "time": str(game.get("kickoff") or ""),
            "week": self._safe_int(game.get("week"), 0),
            "home": str(game.get("home") or ""),
            "away": str(game.get("away") or ""),
            "label": str(game.get("label") or ""),
            "status": str(game.get("status") or "scheduled"),
        }

    def _dashboard_team(self, team_id: Any) -> Any:
        if not team_id or self.league is None or not hasattr(self.league, "id_to_team"):
            return None
        return self.league.id_to_team.get(team_id)

    def _dashboard_team_name(self, team: Any) -> str:
        if team is None:
            return ""
        city = str(getattr(team, "city", "") or "").strip()
        team_name = str(getattr(team, "team_name", None) or getattr(team, "name", None) or "").strip()
        if city and team_name:
            return f"{city} {team_name}"
        return team_name or city

    def _dashboard_team_abbreviation(self, team: Any, team_id: Any) -> str:
        if team is not None:
            return str(getattr(team, "abbreviation", "") or "")
        return str(team_id or "")

    def _dashboard_team_record(self, team: Any, team_id: Any) -> str:
        record = {}
        standings_payload = self.get_standings()
        teams = standings_payload.get("standings", []) if isinstance(standings_payload, dict) else []
        for row in teams:
            if isinstance(row, dict) and str(row.get("team_id") or "") == str(team_id or ""):
                record = row
                break
        if not record and team is not None:
            record = getattr(team, "team_record", {}) or {}
        wins = self._safe_int(record.get("wins", record.get("W")), 0)
        losses = self._safe_int(record.get("losses", record.get("L")), 0)
        ties = self._safe_int(record.get("ties", record.get("T")), 0)
        return f"{wins}-{losses}-{ties}" if ties > 0 else f"{wins}-{losses}"

    def _dashboard_next_game_payload(self, game: Any, team_id: Any) -> Dict[str, Any]:
        if not isinstance(game, dict):
            return {
                "opponent": None,
                "opponent_abbreviation": None,
                "home_away": None,
                "week": None,
                "game_type": None,
            }

        home_id = game.get("home_id")
        away_id = game.get("away_id")
        is_home = str(team_id or "") == str(home_id or "")
        opponent_id = away_id if is_home else home_id
        opponent_team = self._dashboard_team(opponent_id)
        season_type = str(game.get("season_type") or "").strip().lower()
        if not season_type:
            season_info = self._season_info_for_calendar_week(game.get("week"))
            if season_info is not None:
                season_type = season_info[0]
        game_id = str(game.get("game_id") or "").strip()
        if not game_id and home_id and away_id:
            game_id = make_game_id(str(game.get("week") or ""), home_id, away_id)

        return {
            "opponent": self._dashboard_team_name(opponent_team) or None,
            "opponent_abbreviation": self._dashboard_team_abbreviation(opponent_team, opponent_id) or None,
            "home_away": "home" if is_home else "away",
            "week": self._safe_optional_int(game.get("week")),
            "game_type": season_type or None,
            "game_id": game_id or None,
        }

    def _build_dashboard_recent_results(self, team_id: Any, limit: int = 5) -> list[Dict[str, Any]]:
        resolved_team_id = self._resolve_team_id(team_id) or str(team_id or "").strip()
        if not resolved_team_id:
            return []

        results_by_week = self._get_results_by_week()
        recent_results: list[tuple[tuple[int, int, str], Dict[str, Any]]] = []
        for week_key, result in self._iter_results_entries(results_by_week):
            if not isinstance(result, dict) or not self._result_is_user_facing(result):
                continue

            home_id = self._result_team_value(result, "home")
            away_id = self._result_team_value(result, "away")
            if not self._teams_match(home_id, resolved_team_id) and not self._teams_match(away_id, resolved_team_id):
                continue

            compact_result = self._compact_dashboard_recent_result(result, week_key)
            if compact_result is None:
                continue

            recent_results.append(
                (
                    (
                        self._safe_int(week_key, 0),
                        self._date_ordinal_for_sort(result.get("date")),
                        str(compact_result.get("game_id") or ""),
                    ),
                    compact_result,
                )
            )

        recent_results.sort(key=lambda item: item[0], reverse=True)
        return [item[1] for item in recent_results[: max(0, int(limit))]]

    def _compact_dashboard_recent_result(self, result: Dict[str, Any], week_key: Any) -> Dict[str, Any] | None:
        home_id = self._result_team_value(result, "home")
        away_id = self._result_team_value(result, "away")
        if home_id is None or away_id is None:
            return None

        resolved_home = self._resolve_team_id(home_id) or str(home_id)
        resolved_away = self._resolve_team_id(away_id) or str(away_id)
        home_team = self._team_abbr(resolved_home) or str(home_id)
        away_team = self._team_abbr(resolved_away) or str(away_id)
        home_score = self._safe_optional_int(result.get("home_score"))
        away_score = self._safe_optional_int(result.get("away_score"))
        winner = self._dashboard_recent_result_winner(result, resolved_home, resolved_away, home_score, away_score)
        summary = self._dashboard_recent_result_summary(
            result=result,
            home_team=home_team,
            away_team=away_team,
            home_score=home_score,
            away_score=away_score,
            winner=winner,
        )

        season_type = str(result.get("season_type") or result.get("season_phase") or "").strip().lower() or None
        week_value = self._safe_optional_int(result.get("season_week"))
        if season_type is None or week_value is None:
            season_info = self._season_info_for_calendar_week(week_key)
            if season_info is not None:
                season_type = season_type or season_info[0]
                if week_value is None:
                    week_value = season_info[1]

        game_id = str(result.get("game_id") or make_game_id(str(week_key), resolved_home, resolved_away)).strip()
        return {
            "game_id": game_id or None,
            "week": week_value,
            "game_type": season_type,
            "home_team": home_team,
            "away_team": away_team,
            "home_score": home_score,
            "away_score": away_score,
            "winner": winner,
            "summary": summary,
        }

    def _dashboard_recent_result_winner(
        self,
        result: Dict[str, Any],
        home_id: Any,
        away_id: Any,
        home_score: int | None,
        away_score: int | None,
    ) -> str | None:
        winner_id = result.get("winner_id") or result.get("winner")
        if winner_id and self._teams_match(winner_id, home_id):
            return self._team_abbr(home_id) or str(home_id)
        if winner_id and self._teams_match(winner_id, away_id):
            return self._team_abbr(away_id) or str(away_id)
        if home_score is not None and away_score is not None:
            if home_score > away_score:
                return self._team_abbr(home_id) or str(home_id)
            if away_score > home_score:
                return self._team_abbr(away_id) or str(away_id)
        return None

    def _dashboard_recent_result_summary(
        self,
        *,
        result: Dict[str, Any],
        home_team: str,
        away_team: str,
        home_score: int | None,
        away_score: int | None,
        winner: str | None,
    ) -> str:
        summary = str(result.get("summary") or result.get("summary_text") or "").strip()
        if summary:
            return summary
        if winner and home_score is not None and away_score is not None:
            loser = away_team if winner == home_team else home_team
            return f"{winner} defeated {loser}, {home_score}-{away_score}."
        if home_score is not None and away_score is not None:
            return f"Final: {away_team} {away_score} - {home_team} {home_score}"
        return "Game complete."

    def _date_ordinal_for_sort(self, value: Any) -> int:
        try:
            if isinstance(value, datetime.date):
                return value.toordinal()
            text = str(value or "").strip()
            if not text:
                return 0
            return datetime.date.fromisoformat(text).toordinal()
        except (TypeError, ValueError):
            return 0

    def _dashboard_roster_size(self, team: Any) -> int:
        if team is None:
            return 0
        return sum(
            len(group)
            for group in (
                getattr(team, "roster", []) or [],
                getattr(team, "ir_list", []) or [],
                getattr(team, "practice_squad", []) or [],
            )
            if isinstance(group, list)
        )

    def _dashboard_injury_count(self, team_id: Any) -> int:
        if not team_id:
            return 0
        payload = self.get_injury_report(str(team_id))
        entries = payload.get("entries", []) if isinstance(payload, dict) else []
        return len(entries) if isinstance(entries, list) else 0

    def _safe_optional_number(self, value: Any) -> float | int | None:
        if value is None:
            return None
        try:
            number = float(value)
        except (TypeError, ValueError):
            return None
        return int(number) if number.is_integer() else round(number, 2)

    def _dashboard_cap_room(self, team: Any) -> float | int | None:
        if team is None:
            return None
        salary_cap = self._safe_optional_number(getattr(team, "salary_cap", None))
        payroll = self._safe_optional_number(getattr(team, "payroll", None))
        if salary_cap is None or payroll is None:
            return None
        cap_room = float(salary_cap) - float(payroll)
        return int(cap_room) if cap_room.is_integer() else round(cap_room, 2)

    def _build_dashboard_action_items(
        self,
        *,
        team: Any,
        team_id: Any,
        calendar_payload: Dict[str, Any] | None,
        next_game: Any,
    ) -> list[Dict[str, Any]]:
        action_items: list[Dict[str, Any]] = []
        seen_types: set[str] = set()

        def add_action_item(item: Dict[str, Any] | None) -> None:
            if not isinstance(item, dict):
                return
            item_type = str(item.get("type") or "").strip()
            if not item_type or item_type in seen_types:
                return
            seen_types.add(item_type)
            action_items.append(item)

        add_action_item(self._build_roster_action_item(team, next_game))
        add_action_item(self._build_depth_chart_action_item(team))
        add_action_item(self._build_game_day_action_item(team_id, next_game))
        add_action_item(self._build_last_continue_action_item(calendar_payload))
        return action_items

    def _dashboard_action_item(
        self,
        *,
        item_type: str,
        title: str,
        description: str,
        severity: str,
        requires_user_action: bool,
        primary_action: str | None,
    ) -> Dict[str, Any]:
        return {
            "type": str(item_type or ""),
            "title": str(title or ""),
            "description": str(description or ""),
            "severity": str(severity or "info"),
            "requires_user_action": bool(requires_user_action),
            "primary_action": str(primary_action) if primary_action else None,
        }

    def _build_game_day_action_item(self, team_id: Any, next_game: Any) -> Dict[str, Any] | None:
        user_game = self._compact_continue_current_user_game()
        if not isinstance(user_game, dict):
            return None

        next_game_payload = self._dashboard_next_game_payload(next_game, team_id)
        opponent = str(
            user_game.get("opponent_abbreviation")
            or user_game.get("opponent")
            or (next_game_payload.get("opponent_abbreviation") if isinstance(next_game_payload, dict) else "")
            or (next_game_payload.get("opponent") if isinstance(next_game_payload, dict) else "")
            or ""
        ).strip()
        game_type = str(
            (next_game_payload.get("game_type") if isinstance(next_game_payload, dict) else "")
            or ""
        ).strip()
        display_game_type = game_type.replace("_", " ") if game_type else "game"
        title = f"Game Day: vs {opponent}" if opponent else "Game Day"
        return self._dashboard_action_item(
            item_type="game_day",
            title=title,
            description=f"Your {display_game_type} matchup is ready.",
            severity="info",
            requires_user_action=True,
            primary_action="View Matchup",
        )

    def _build_roster_action_item(self, team: Any, next_game: Any) -> Dict[str, Any] | None:
        if team is None:
            return None

        roster_status = self._team_roster_status(team)
        if not roster_status.get("is_valid", True):
            issues = roster_status.get("issues", [])
            description = issues[0] if isinstance(issues, list) and issues else "Your roster needs attention before you can continue."
            return self._dashboard_action_item(
                item_type="roster_invalid",
                title="Roster Issue",
                description=description,
                severity="danger",
                requires_user_action=True,
                primary_action="View Roster",
            )

        user_team_id = getattr(team, "id", None)
        if not user_team_id or self.league is None or not isinstance(next_game, dict):
            return None

        try:
            roster_issues = get_roster_rule_violations(
                self.league,
                str(user_team_id),
                context={"check_type": "game_day", "game_day_check": True},
            )
        except Exception:
            roster_issues = []

        if not roster_issues:
            return None

        issue = roster_issues[0] if isinstance(roster_issues[0], dict) else {}
        description = str(issue.get("message") or issue.get("title") or "").strip()
        if not description:
            description = "Your roster needs attention before you can continue."
        return self._dashboard_action_item(
            item_type="roster_invalid",
            title="Roster Issue",
            description=description,
            severity="danger",
            requires_user_action=True,
            primary_action="View Roster",
        )

    def _build_depth_chart_action_item(self, team: Any) -> Dict[str, Any] | None:
        if team is None:
            return None

        depth_chart_status = self._team_depth_chart_status(team)
        if not depth_chart_status.get("is_valid", True):
            issues = depth_chart_status.get("issues", [])
            description = "Your depth chart needs attention before you can continue."
            if isinstance(issues, list):
                issue_text = [str(issue).strip() for issue in issues if str(issue).strip()]
                if issue_text:
                    description = " ".join(issue_text)
            return self._dashboard_action_item(
                item_type="depth_chart_invalid",
                title="Depth Chart Issue",
                description=description,
                severity="warning",
                requires_user_action=True,
                primary_action="View Depth Chart",
            )
        return None

    def _build_last_continue_action_item(self, calendar_payload: Dict[str, Any] | None) -> Dict[str, Any] | None:
        last_result = self._last_continue_result if isinstance(self._last_continue_result, dict) else {}
        stop_reason = str(last_result.get("stop_reason") or "").strip()
        if not stop_reason:
            return None

        if stop_reason == "season_phase_changed":
            phase = ""
            if isinstance(calendar_payload, dict):
                phase = str(
                    calendar_payload.get("season_phase")
                    or calendar_payload.get("phase_label")
                    or ""
                ).strip()
            phase_text = phase.replace("_", " ") if phase else "the next phase"
            return self._dashboard_action_item(
                item_type="season_phase_changed",
                title="Season Phase Changed",
                description=f"The league calendar has moved into {phase_text}.",
                severity="info",
                requires_user_action=False,
                primary_action=None,
            )

        if stop_reason == "max_days_reached":
            return self._dashboard_action_item(
                item_type="max_days_reached",
                title="Continue Paused",
                description="The sim paused after reaching the safe continue limit.",
                severity="info",
                requires_user_action=False,
                primary_action=None,
            )

        if stop_reason == "roster_invalid":
            team_id = getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
            team = self._team_for_key(team_id)
            roster_status = self._team_roster_status(team) if team is not None else {}
            issues = roster_status.get("issues", [])
            description = issues[0] if isinstance(issues, list) and issues else "Your roster needs attention before you can continue."
            return self._dashboard_action_item(
                item_type="roster_invalid",
                title="Roster Issue",
                description=description,
                severity="danger",
                requires_user_action=True,
                primary_action="View Roster",
            )

        if stop_reason == "depth_chart_invalid":
            team_id = getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
            team = self._team_for_key(team_id)
            depth_chart_status = self._team_depth_chart_status(team) if team is not None else {}
            issues = depth_chart_status.get("issues", [])
            description = "Your depth chart needs attention before you can continue."
            if isinstance(issues, list):
                issue_text = [str(issue).strip() for issue in issues if str(issue).strip()]
                if issue_text:
                    description = " ".join(issue_text)
            return self._dashboard_action_item(
                item_type="depth_chart_invalid",
                title="Depth Chart Issue",
                description=description,
                severity="warning",
                requires_user_action=True,
                primary_action="View Depth Chart",
            )

        return None

    def _compact_standings_summary(self) -> list[Dict[str, Any]]:
        raw = self.get_standings()
        teams = raw.get("standings", []) if isinstance(raw, dict) else []
        summary: list[Dict[str, Any]] = []
        for team in teams:
            if not isinstance(team, dict):
                continue
            wins = self._safe_int(team.get("wins"), 0)
            losses = self._safe_int(team.get("losses"), 0)
            ties = self._safe_int(team.get("ties"), 0)
            games_played = wins + losses + ties
            pct = None
            if games_played > 0:
                pct = round((wins + (0.5 * ties)) / games_played, 3)
            summary.append(
                {
                    "team_id": str(team.get("team_id") or ""),
                    "team_name": str(team.get("team_name") or team.get("name") or ""),
                    "wins": wins,
                    "losses": losses,
                    "ties": ties,
                    "pct": pct,
                }
            )
        return summary

    def _compact_inbox_summary(self, engine: TimeEngine, limit: int = 10) -> Dict[str, Any]:
        unread_count = engine.unread_inbox_count()
        latest = []
        for message in self._calendar_dashboard_notifications(engine, limit=limit):
            latest.append(
                {
                    "id": message.get("notification_id"),
                    "created_at": self._join_created_at(
                        message.get("created_at_date"),
                        message.get("created_at_time"),
                    ),
                    "title": message.get("title"),
                    "message": message.get("message"),
                    "category": message.get("category"),
                    "requires_ack": bool(message.get("requires_user_attention")),
                    "requires_user_attention": bool(message.get("requires_user_attention")),
                    "read": bool(message.get("read")),
                }
            )
        return {
            "unread_count": unread_count,
            "blocking_decision_count": len(engine.get_blocking_decisions()),
            "latest": latest,
        }

    def _join_created_at(self, created_at_date: Any, created_at_time: Any) -> str:
        date_text = str(created_at_date or "").strip()
        time_text = str(created_at_time or "").strip()
        if date_text and time_text:
            return f"{date_text}T{time_text}:00"
        return date_text

    def get_schedule_context(self) -> Dict[str, Any]:
        self._ensure_game()
        current_date = getattr(self.calendar, "current_date", None)
        if not isinstance(current_date, datetime.date):
            return {
                "games_today": [],
                "next_game": None,
                "next_game_date": None,
                "next_game_label": None,
                "can_simulate_today": False,
                "user_team_game_today": None,
                "user_team_next_game": None,
                "user_team_can_simulate": False,
                "league_games_today_count": 0,
            }

        current_week = str(getattr(self.calendar, "current_week", ""))
        current_day = current_date.strftime("%A")
        user_team_id = str(getattr(self.league, "user_team_id", "") or "")
        schedule_by_week = self._get_schedule_by_week()
        results_by_week = self._get_results_by_week()
        candidates: list[tuple[int, int, str, Dict[str, Any]]] = []
        user_team_candidates: list[tuple[int, int, str, Dict[str, Any]]] = []
        games_today: list[Dict[str, Any]] = []
        user_team_games_today: list[Dict[str, Any]] = []

        for week_key, games in schedule_by_week.items():
            if not isinstance(games, list):
                continue
            week_str = str(week_key)
            for game in games:
                if not isinstance(game, dict):
                    continue
                home_id = game.get("home_id")
                away_id = game.get("away_id")
                if not home_id or not away_id:
                    continue
                if self._is_placeholder_team_ref(home_id) or self._is_placeholder_team_ref(away_id):
                    continue

                day = self._normalize_day_name(game.get("day"))
                date_obj = self._week_day_to_date(week_str, day)
                if date_obj is None:
                    continue
                game_id = make_game_id(week_str, home_id, away_id)
                if self._find_result_for_game(week_str, home_id, away_id, game_id, results_by_week):
                    continue

                kickoff_minutes = self._parse_kickoff_minutes(game.get("kickoff"))
                entry = self._schedule_context_game(game, week_str, date_obj, day, game_id)
                is_user_team_game = bool(user_team_id) and user_team_id in {str(home_id), str(away_id)}
                if week_str == current_week and date_obj == current_date and day == current_day:
                    games_today.append(entry)
                    if is_user_team_game:
                        user_team_games_today.append(entry)
                if date_obj >= current_date:
                    candidates.append((date_obj.toordinal(), kickoff_minutes, game_id, entry))
                    if is_user_team_game:
                        user_team_candidates.append((date_obj.toordinal(), kickoff_minutes, game_id, entry))

        games_today.sort(
            key=lambda game: (
                self._parse_kickoff_minutes(game.get("kickoff")),
                str(game.get("game_id")),
            )
        )
        candidates.sort(key=lambda item: (item[0], item[1], item[2]))
        user_team_candidates.sort(key=lambda item: (item[0], item[1], item[2]))
        next_game = candidates[0][3] if candidates else None
        user_team_game_today = user_team_games_today[0] if user_team_games_today else None
        user_team_next_game = user_team_candidates[0][3] if user_team_candidates else None
        return {
            "games_today": games_today,
            "next_game": next_game,
            "next_game_date": next_game.get("date") if next_game else None,
            "next_game_label": next_game.get("label") if next_game else None,
            "can_simulate_today": bool(games_today),
            "user_team_game_today": user_team_game_today,
            "user_team_next_game": user_team_next_game,
            "user_team_can_simulate": bool(user_team_game_today),
            "league_games_today_count": len(games_today),
        }

    def get_team(self, team_id: str) -> Dict[str, Any]:
        self._ensure_game()
        team = self.league.id_to_team.get(team_id)
        if team is None:
            raise KeyError("team_not_found")
        record = getattr(team, "team_record", {})
        return {
            "id": team.id,
            "team_name": team.team_name,
            "city": team.city,
            "abbreviation": team.abbreviation,
            "conference": team.conference,
            "division": team.division,
            "scouting_accuracy": team.scouting_accuracy,
            "team_record": {
                "wins": record.get("wins", 0),
                "losses": record.get("losses", 0),
                "ties": record.get("ties", 0),
            },
            "rebuild_mode": getattr(team, "rebuild_mode", False),
        }

    def _player_minimal(self, player: Any) -> Dict[str, Any]:
        if isinstance(player, dict):
            return {
                "id": player.get("id"),
                "name": player.get("name", ""),
                "position": player.get("position", ""),
                "overall": player.get("overall", 0),
                "pot": self._resolve_pot(player),
                "age": player.get("age", 0),
                "jersey_number": player.get("jersey_number", 0),
                "on_injured_reserve": player.get("on_injured_reserve", False),
            }
        if isinstance(player, (str, int, float, bool)):
            label = self._safe_stringify(player)
            return {
                "id": None,
                "name": label,
                "position": "",
                "overall": 0,
                "pot": None,
                "age": 0,
                "jersey_number": 0,
                "on_injured_reserve": False,
            }
        return {
            "id": getattr(player, "id", None),
            "name": getattr(player, "name", ""),
            "position": getattr(player, "position", ""),
            "overall": getattr(player, "overall", 0),
            "pot": self._resolve_pot(player),
            "age": getattr(player, "age", 0),
            "jersey_number": getattr(player, "jersey_number", 0),
            "on_injured_reserve": getattr(player, "on_injured_reserve", False),
        }

    def _safe_stringify(self, value: Any) -> str:
        try:
            return str(value)
        except Exception:
            return "<unprintable>"

    def _player_value(self, player: Any, key: str, default: Any = None) -> Any:
        if isinstance(player, dict):
            return player.get(key, default)
        return getattr(player, key, default)

    def _payload_from_roster_entry(self, entry: Any) -> Dict[str, Any]:
        if hasattr(entry, "to_dict"):
            try:
                payload = entry.to_dict()
            except Exception:
                payload = None
            if isinstance(payload, dict):
                return payload
        if isinstance(entry, dict):
            return dict(entry)
        return self._player_minimal(entry)

    def _resolve_pot(self, player: Any) -> int | None:
        value = None
        if isinstance(player, dict):
            for key in ("pot", "potential", "pot_rating"):
                if key in player:
                    value = player.get(key)
                    break
            if value is None:
                for key in ("dna", "player_dna"):
                    nested = player.get(key)
                    if isinstance(nested, dict):
                        if "potential" in nested:
                            value = nested.get("potential")
                            break
                        if "pot" in nested:
                            value = nested.get("pot")
                            break
        else:
            for attr in ("pot", "potential", "pot_rating"):
                if hasattr(player, attr):
                    value = getattr(player, attr)
                    if value is not None:
                        break
            if value is None:
                for attr in ("dna", "player_dna"):
                    nested = getattr(player, attr, None)
                    if nested is None:
                        continue
                    if hasattr(nested, "potential"):
                        value = getattr(nested, "potential")
                        if value is not None:
                            break
                    if hasattr(nested, "pot"):
                        value = getattr(nested, "pot")
                        if value is not None:
                            break
        if value is None:
            return None
        try:
            return int(round(float(value)))
        except (TypeError, ValueError):
            return None

    def _get_legacy_team_roster(self, team_id: str, include_details: bool = False) -> Dict[str, Any]:
        self._ensure_game()
        team = self.league.id_to_team.get(team_id)
        if team is None:
            raise KeyError("team_not_found")
        if include_details:
            roster = [self._with_pot(self._payload_from_roster_entry(p), p) for p in team.roster]
            ir_list = [self._with_pot(self._payload_from_roster_entry(p), p) for p in team.ir_list]
            practice_squad = [
                self._with_pot(self._payload_from_roster_entry(p), p)
                for p in team.practice_squad
            ]
        else:
            roster = [self._player_minimal(p) for p in team.roster]
            ir_list = [self._player_minimal(p) for p in team.ir_list]
            practice_squad = [self._player_minimal(p) for p in team.practice_squad]
        if self.debug_pot and not self._pot_debugged and roster:
            print(f"[API] pot resolved for first roster player: {roster[0].get('pot')}")
            self._pot_debugged = True
        return {
            "team_id": team.id,
            "roster": roster,
            "ir_list": ir_list,
            "practice_squad": practice_squad,
        }

    def _format_roster_issue_message(self, roster_size: int, roster_limit: int) -> str:
        required_cuts = max(0, roster_size - roster_limit)
        return (
            f"Roster has {roster_size} players. Limit is {roster_limit}. "
            f"Cut {required_cuts} players."
        )

    def _team_roster_status(self, team: Any) -> Dict[str, Any]:
        active_roster = list(getattr(team, "roster", []) or [])
        ir_list = list(getattr(team, "ir_list", []) or [])
        practice_squad = list(getattr(team, "practice_squad", []) or [])
        roster_size = len(active_roster)
        roster_limit = self._safe_int(getattr(team, "MAX_ROSTER_SIZE", 53), 53)
        if roster_limit <= 0:
            roster_limit = 53
        required_cuts = max(0, roster_size - roster_limit)
        open_slots = max(0, roster_limit - roster_size)

        issues: list[str] = []
        if required_cuts > 0:
            issues.append(self._format_roster_issue_message(roster_size, roster_limit))
        if roster_size == 0:
            issues.append("Roster is empty.")

        injured_count = 0
        for player in active_roster + ir_list + practice_squad:
            injury_status = self._normalize_injury_status_value(player)
            if injury_status != "healthy" or self._safe_bool(self._player_value(player, "on_injured_reserve", False)):
                injured_count += 1

        return {
            "is_valid": required_cuts == 0,
            "roster_size": roster_size,
            "roster_limit": roster_limit,
            "required_cuts": required_cuts,
            "open_slots": open_slots,
            "injured_count": injured_count,
            "issues": issues,
        }

    def _compact_roster_injury(self, player: Any) -> str | None:
        injury_status = self._normalize_injury_status_value(player)
        if injury_status == "healthy" and not self._safe_bool(self._player_value(player, "on_injured_reserve", False)):
            return None

        injury_name = self._safe_stringify(self._player_value(player, "injury_name", "")).strip()
        if injury_name:
            return injury_name
        return injury_status.replace("_", " ").title() if injury_status else "Injured"

    def _compact_depth_role_map(self, team: Any) -> Dict[str, str]:
        depth_chart = getattr(team, "depth_chart", None)
        if not isinstance(depth_chart, dict):
            return {}

        depth_roles: Dict[str, str] = {}
        for position, players in depth_chart.items():
            if not isinstance(players, list):
                continue
            normalized_position = str(position or "").strip().upper() or "UNK"
            for index, player in enumerate(players):
                player_id = self._safe_stringify(self._player_value(player, "id", "")).strip()
                if not player_id or player_id in depth_roles:
                    continue
                if index == 0:
                    depth_roles[player_id] = "Starter"
                else:
                    depth_roles[player_id] = f"{normalized_position}{index + 1}"
        return depth_roles

    def _compact_team_roster_players(self, team: Any) -> list[Dict[str, Any]]:
        buckets = (
            ("active", list(getattr(team, "roster", []) or [])),
            ("ir", list(getattr(team, "ir_list", []) or [])),
            ("practice_squad", list(getattr(team, "practice_squad", []) or [])),
        )
        depth_roles = self._compact_depth_role_map(team)
        players: list[Dict[str, Any]] = []
        seen_player_ids: set[str] = set()

        for bucket_name, bucket_players in buckets:
            for player in bucket_players:
                player_id = self._safe_stringify(self._player_value(player, "id", "")).strip()
                name = self._safe_stringify(self._player_value(player, "name", "")).strip()
                position = self._safe_stringify(self._player_value(player, "position", "")).strip().upper() or "UNK"
                dedupe_key = player_id or f"{bucket_name}:{name}:{position}"
                if dedupe_key in seen_player_ids:
                    continue
                seen_player_ids.add(dedupe_key)
                players.append(
                    {
                        "player_id": player_id or None,
                        "name": name or "Unknown",
                        "position": position,
                        "overall": self._safe_optional_int(
                            self._player_value(player, "overall", self._player_value(player, "ovr"))
                        ),
                        "age": self._safe_optional_int(self._player_value(player, "age")),
                        "status": self._roster_bucket(player, bucket_name),
                        "injury": self._compact_roster_injury(player),
                        "depth_role": depth_roles.get(player_id),
                    }
                )

        players.sort(
            key=lambda row: (
                self._position_group_sort_key(str(row.get("position") or "")),
                -(row.get("overall") if isinstance(row.get("overall"), int) else -1),
                str(row.get("name") or ""),
            )
        )
        return players

    def _compact_position_counts(self, team: Any) -> list[Dict[str, Any]]:
        counts: Dict[str, int] = {}
        for player in list(getattr(team, "roster", []) or []):
            position = self._safe_stringify(self._player_value(player, "position", "")).strip().upper() or "UNK"
            counts[position] = counts.get(position, 0) + 1

        position_counts = [
            {"position": position, "count": count}
            for position, count in counts.items()
        ]
        position_counts.sort(key=lambda entry: self._position_group_sort_key(str(entry.get("position") or "")))
        return position_counts

    def get_team_roster(self, team_id: str | None = None, include_details: bool = False) -> Dict[str, Any]:
        _ = include_details
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        resolved_team_id = (
            team_id
            or getattr(self.league, "controlled_team_id", None)
            or getattr(self.league, "user_team_id", None)
        )
        if not resolved_team_id:
            return {"ok": False, "error": "missing_team_id"}

        team = self._team_for_key(resolved_team_id)
        if team is None:
            return {"ok": False, "error": "team_not_found"}

        team_name = getattr(team, "team_name", None) or getattr(team, "name", None) or ""
        team_abbreviation = getattr(team, "abbreviation", None) or str(resolved_team_id)
        return {
            "ok": True,
            "team": {
                "team_id": getattr(team, "id", resolved_team_id),
                "name": self._safe_stringify(team_name),
                "abbreviation": self._safe_stringify(team_abbreviation),
            },
            "roster_status": self._team_roster_status(team),
            "position_counts": self._compact_position_counts(team),
            "players": self._compact_team_roster_players(team),
        }

    def _normalize_injury_status_value(self, player: Any) -> str:
        from gridiron_gm_pkg.simulation.systems.player.injury_status import normalize_injury_status

        injury_status = normalize_injury_status(self._player_value(player, "injury_status", "healthy"))
        if self._safe_bool(self._player_value(player, "on_injured_reserve", False)) and injury_status == "healthy":
            return "ir"
        return injury_status

    def _roster_bucket(self, player: Any, default_bucket: str) -> str:
        if self._safe_bool(self._player_value(player, "on_injured_reserve", False)):
            return "ir"
        return default_bucket

    def _roster_player_card(self, player: Any, roster_bucket: str) -> Dict[str, Any]:
        position = self._safe_stringify(self._player_value(player, "position", "")).strip().upper()
        return {
            "player_id": self._player_value(player, "id"),
            "name": self._safe_stringify(self._player_value(player, "name", "")),
            "position": position,
            "age": self._safe_optional_int(self._player_value(player, "age")),
            "jersey_number": self._safe_optional_int(self._player_value(player, "jersey_number")),
            "overall": self._safe_optional_int(
                self._player_value(player, "overall", self._player_value(player, "ovr"))
            ),
            "pot": self._resolve_pot(player),
            "injury_status": self._normalize_injury_status_value(player),
            "on_injured_reserve": self._safe_bool(
                self._player_value(player, "on_injured_reserve", roster_bucket == "ir")
            ),
            "roster_bucket": self._roster_bucket(player, roster_bucket),
        }

    def _position_group_sort_key(self, position: str) -> tuple[int, str]:
        order = {
            "QB": 0,
            "RB": 1,
            "FB": 2,
            "WR": 3,
            "TE": 4,
            "LT": 5,
            "LG": 6,
            "C": 7,
            "RG": 8,
            "RT": 9,
            "OL": 10,
            "EDGE": 11,
            "DE": 12,
            "DT": 13,
            "MLB": 14,
            "OLB": 15,
            "LB": 16,
            "CB": 17,
            "FS": 18,
            "SS": 19,
            "S": 20,
            "K": 21,
            "P": 22,
        }
        normalized = str(position or "").strip().upper() or "UNK"
        return (order.get(normalized, 999), normalized)

    def get_team_roster_snapshot(self, team_id: str | None = None) -> Dict[str, Any]:
        return self.get_team_roster(team_id)

    def _depth_chart_position_sources(self, depth_chart: Any) -> Dict[str, list[Any]]:
        sources: Dict[str, list[Any]] = defaultdict(list)
        if not isinstance(depth_chart, dict):
            return {}

        for raw_position, raw_players in depth_chart.items():
            position = self._safe_stringify(raw_position).strip().upper()
            if not position or not isinstance(raw_players, list):
                continue
            for player in raw_players:
                if player is None:
                    continue
                sources[position].append(player)
        return dict(sources)

    def _build_fallback_depth_chart(self, team: Any) -> Dict[str, list[Any]]:
        grouped_players: Dict[str, list[Any]] = defaultdict(list)
        for player in list(getattr(team, "roster", []) or []):
            position = self._safe_stringify(self._player_value(player, "position", "")).strip().upper()
            if not position:
                continue
            grouped_players[position].append(player)

        for players in grouped_players.values():
            players.sort(
                key=lambda player: (
                    0 if self._is_player_available_for_depth_chart(player) else 1,
                    -self._safe_int(self._player_value(player, "overall", self._player_value(player, "ovr")), 0),
                    self._safe_stringify(self._player_value(player, "name", "")),
                )
            )

        return dict(grouped_players)

    def _effective_depth_chart(self, team: Any) -> Dict[str, list[Any]]:
        depth_chart = self._depth_chart_position_sources(getattr(team, "depth_chart", None))
        if depth_chart:
            return depth_chart
        return self._build_fallback_depth_chart(team)

    def _ensure_persisted_depth_chart(self, team: Any) -> Dict[str, list[Any]]:
        depth_chart = self._depth_chart_position_sources(getattr(team, "depth_chart", None))
        if depth_chart:
            return depth_chart
        generated_depth_chart = self._build_fallback_depth_chart(team)
        self._persist_depth_chart(team, generated_depth_chart)
        return generated_depth_chart

    def _persist_depth_chart(self, team: Any, depth_chart: Dict[str, list[Any]]) -> None:
        if team is None:
            return
        try:
            team.depth_chart = depth_chart if isinstance(depth_chart, dict) else {}
        except Exception:
            setattr(team, "depth_chart", depth_chart if isinstance(depth_chart, dict) else {})

    def _depth_chart_positions_for_requirement(self, requirement_position: str, available_positions: set[str]) -> list[str]:
        aliases = self._DEPTH_CHART_FLEX_ALIASES.get(requirement_position, (requirement_position,))
        positions: list[str] = []
        seen: set[str] = set()
        for alias in aliases:
            normalized = self._safe_stringify(alias).strip().upper()
            if not normalized or normalized in seen:
                continue
            seen.add(normalized)
            if normalized in available_positions:
                positions.append(normalized)
        return positions

    def _depth_chart_required_starters(self, position: str) -> int:
        normalized_position = self._safe_stringify(position).strip().upper()
        for requirement_position, starter_count in self._DEPTH_CHART_REQUIREMENTS:
            if requirement_position == normalized_position:
                return starter_count
        return 1

    def _depth_chart_group_positions(self, requested_position: str, available_positions: set[str]) -> list[str]:
        normalized_position = self._safe_stringify(requested_position).strip().upper()
        if not normalized_position:
            return []

        candidate_positions = self._depth_chart_positions_for_requirement(normalized_position, available_positions)
        if candidate_positions:
            return candidate_positions
        if normalized_position in available_positions:
            return [normalized_position]
        return []

    def _reassign_depth_chart_group_positions(
        self,
        depth_chart: Dict[str, list[Any]],
        group_positions: list[str],
        group_players: list[Any],
        original_counts: list[int],
    ) -> None:
        cursor = 0
        total_players = len(group_players)
        for index, position in enumerate(group_positions):
            count = original_counts[index] if index < len(original_counts) else 0
            next_cursor = min(total_players, cursor + max(0, count))
            depth_chart[position] = list(group_players[cursor:next_cursor])
            cursor = next_cursor
        if cursor < total_players and group_positions:
            last_position = group_positions[-1]
            depth_chart[last_position] = list(depth_chart.get(last_position, [])) + list(group_players[cursor:])

    def _is_player_available_for_depth_chart(self, player: Any) -> bool:
        try:
            from gridiron_gm_pkg.simulation.systems.player.injury_status import is_available_for_game

            return bool(is_available_for_game(player))
        except Exception:
            status = self._normalize_injury_status_value(player)
            if status in {"out", "ir"}:
                return False
            return not self._safe_bool(self._player_value(player, "on_injured_reserve", False))

    def _team_depth_chart_status(self, team: Any) -> Dict[str, Any]:
        if team is None:
            return {"is_valid": False, "issues": ["Your depth chart needs attention before you can continue."]}

        effective_depth_chart = self._effective_depth_chart(team)
        available_positions = set(effective_depth_chart.keys())
        issues: list[str] = []
        for requirement_position, starter_count in self._DEPTH_CHART_REQUIREMENTS:
            candidate_positions = self._depth_chart_positions_for_requirement(requirement_position, available_positions)
            if not candidate_positions:
                issues.append(f"Missing starting {requirement_position}.")
                continue

            active_players = 0
            for position in candidate_positions:
                for player in effective_depth_chart.get(position, []):
                    if self._is_player_available_for_depth_chart(player):
                        active_players += 1

            if active_players < starter_count:
                if active_players <= 0:
                    issues.append(f"Missing starting {requirement_position}.")
                else:
                    issues.append(
                        f"Only {active_players} active {requirement_position} available. Need {starter_count}."
                    )

        return {
            "is_valid": len(issues) == 0,
            "issues": issues,
        }

    def _compact_depth_chart_positions(self, team: Any) -> list[Dict[str, Any]]:
        effective_depth_chart = self._effective_depth_chart(team)
        available_positions = set(effective_depth_chart.keys())
        positions_payload: list[Dict[str, Any]] = []
        seen_positions: set[str] = set()

        for requirement_position, starter_count in self._DEPTH_CHART_REQUIREMENTS:
            candidate_positions = self._depth_chart_positions_for_requirement(requirement_position, available_positions)
            players_payload: list[Dict[str, Any]] = []

            for position in candidate_positions:
                seen_positions.add(position)
                for player in effective_depth_chart.get(position, []):
                    is_available = self._is_player_available_for_depth_chart(player)
                    role = "Starter" if len(players_payload) < starter_count else "Backup"
                    players_payload.append(
                        {
                            "player_id": self._safe_stringify(self._player_value(player, "id", "")).strip() or None,
                            "name": self._safe_stringify(self._player_value(player, "name", "")).strip() or "Unknown Player",
                            "overall": self._safe_optional_int(
                                self._player_value(player, "overall", self._player_value(player, "ovr"))
                            ),
                            "status": "active" if is_available else "inactive",
                            "injury": self._compact_roster_injury(player),
                            "role": role,
                        }
                    )

            positions_payload.append(
                {
                    "position": requirement_position,
                    "required_starters": starter_count,
                    "players": players_payload,
                }
            )

        extra_positions = sorted(position for position in available_positions if position not in seen_positions)
        for position in extra_positions:
            players_payload: list[Dict[str, Any]] = []
            for player in effective_depth_chart.get(position, []):
                is_available = self._is_player_available_for_depth_chart(player)
                role = "Starter" if len(players_payload) == 0 else "Backup"
                players_payload.append(
                    {
                        "player_id": self._safe_stringify(self._player_value(player, "id", "")).strip() or None,
                        "name": self._safe_stringify(self._player_value(player, "name", "")).strip() or "Unknown Player",
                        "overall": self._safe_optional_int(
                            self._player_value(player, "overall", self._player_value(player, "ovr"))
                        ),
                        "status": "active" if is_available else "inactive",
                        "injury": self._compact_roster_injury(player),
                        "role": role,
                    }
                )

            positions_payload.append(
                {
                    "position": position,
                    "required_starters": 0,
                    "players": players_payload,
                }
            )

        positions_payload.sort(key=lambda entry: self._position_group_sort_key(str(entry.get("position") or "")))
        return positions_payload

    def _compact_depth_chart_payload(self, team: Any, resolved_team_id: Any) -> Dict[str, Any]:
        team_name = getattr(team, "team_name", None) or getattr(team, "name", None) or ""
        team_abbreviation = getattr(team, "abbreviation", None) or str(resolved_team_id)
        return {
            "team": {
                "team_id": getattr(team, "id", resolved_team_id),
                "name": self._safe_stringify(team_name),
                "abbreviation": self._safe_stringify(team_abbreviation),
            },
            "depth_chart_status": self._team_depth_chart_status(team),
            "positions": self._compact_depth_chart_positions(team),
        }

    def get_team_depth_chart(self, team_id: str | None = None) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        resolved_team_id = (
            team_id
            or getattr(self.league, "controlled_team_id", None)
            or getattr(self.league, "user_team_id", None)
        )
        if not resolved_team_id:
            return {"ok": False, "error": "No team selected."}

        team = self._team_for_key(resolved_team_id)
        if team is None:
            return {"ok": False, "error": "team_not_found"}

        return {"ok": True, **self._compact_depth_chart_payload(team, resolved_team_id)}

    def auto_fill_depth_chart(self, team_id: str | None = None) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        resolved_team_id = (
            team_id
            or getattr(self.league, "controlled_team_id", None)
            or getattr(self.league, "user_team_id", None)
        )
        if not resolved_team_id:
            return {"ok": False, "error": "No team selected."}

        team = self._team_for_key(resolved_team_id)
        if team is None:
            return {"ok": False, "error": "team_not_found"}

        generated_depth_chart = self._build_fallback_depth_chart(team)
        self._persist_depth_chart(team, generated_depth_chart)

        return {
            "ok": True,
            "message": "Depth chart auto-filled.",
            "depth_chart": self._compact_depth_chart_payload(team, resolved_team_id),
        }

    def update_depth_chart(
        self,
        position: str,
        player_id: str,
        action: str,
        team_id: str | None = None,
    ) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        normalized_position = self._safe_stringify(position).strip().upper()
        normalized_player_id = self._safe_stringify(player_id).strip()
        normalized_action = self._safe_stringify(action).strip().lower()
        if not normalized_position or not normalized_player_id or not normalized_action:
            return {"ok": False, "error": "Missing position, player_id, or action."}
        if normalized_action not in {"move_up", "move_down", "set_starter"}:
            return {"ok": False, "error": "Invalid depth chart action."}

        resolved_team_id = (
            team_id
            or getattr(self.league, "controlled_team_id", None)
            or getattr(self.league, "user_team_id", None)
        )
        if not resolved_team_id:
            return {"ok": False, "error": "No team selected."}

        team = self._team_for_key(resolved_team_id)
        if team is None:
            return {"ok": False, "error": "team_not_found"}

        depth_chart = self._ensure_persisted_depth_chart(team)
        available_positions = set(depth_chart.keys())
        group_positions = self._depth_chart_group_positions(normalized_position, available_positions)
        original_counts = [len(depth_chart.get(group_position, [])) for group_position in group_positions]
        group_players: list[Any] = []
        for group_position in group_positions:
            group_players.extend(list(depth_chart.get(group_position, [])))

        player_index = -1
        for index, player in enumerate(group_players):
            current_player_id = self._safe_stringify(self._player_value(player, "id", "")).strip()
            if current_player_id == normalized_player_id:
                player_index = index
                break

        if player_index < 0:
            return {"ok": False, "error": "Player not found in depth chart position group."}

        if normalized_action == "move_up":
            if player_index > 0:
                group_players[player_index - 1], group_players[player_index] = (
                    group_players[player_index],
                    group_players[player_index - 1],
                )
        elif normalized_action == "move_down":
            if player_index < len(group_players) - 1:
                group_players[player_index + 1], group_players[player_index] = (
                    group_players[player_index],
                    group_players[player_index + 1],
                )
        elif player_index > 0:
            selected_player = group_players.pop(player_index)
            group_players.insert(0, selected_player)

        self._reassign_depth_chart_group_positions(depth_chart, group_positions, group_players, original_counts)
        self._persist_depth_chart(team, depth_chart)

        return {
            "ok": True,
            "message": "Depth chart updated.",
            "depth_chart": self._compact_depth_chart_payload(team, resolved_team_id),
        }

    def get_injury_report(self, team_id: str | None = None) -> Dict[str, Any]:
        self._ensure_game()
        resolved_team_id = team_id or getattr(self.league, "user_team_id", None)
        if not resolved_team_id:
            return {"ok": False, "error": "missing_team_id"}
        team = self._team_for_key(resolved_team_id)
        if team is None:
            return {"ok": False, "error": "team_not_found"}

        current_date = getattr(self.calendar, "current_date", None)
        if not isinstance(current_date, datetime.date):
            engine = self._get_time_engine()
            current_date = getattr(engine.clock, "current_date", None)

        from gridiron_gm_pkg.simulation.systems.player.injury_status import normalize_injury_status

        entries: list[dict] = []
        seen_ids: set[str] = set()

        def _get_value(player: Any, key: str, default: Any = None) -> Any:
            if isinstance(player, dict):
                return player.get(key, default)
            return getattr(player, key, default)

        for group in ("roster", "ir_list", "practice_squad"):
            for player in getattr(team, group, []) or []:
                player_id = _get_value(player, "id")
                if player_id and player_id in seen_ids:
                    continue

                injury_status_raw = _get_value(player, "injury_status", "healthy")
                on_ir = bool(_get_value(player, "on_injured_reserve", False))
                injury_status = normalize_injury_status(injury_status_raw)
                if on_ir and injury_status == "healthy":
                    injury_status = "ir"

                if injury_status == "healthy" and not on_ir:
                    continue

                injury_name = _get_value(player, "injury_name")
                start_raw = _get_value(player, "injury_start_date")
                end_raw = _get_value(player, "injury_end_date")
                end_date = self._coerce_date(end_raw)
                days_remaining = None
                if isinstance(current_date, datetime.date) and end_date is not None:
                    days_remaining = max(0, (end_date - current_date).days)

                entries.append(
                    {
                        "player_id": player_id,
                        "name": self._safe_stringify(_get_value(player, "name", "")),
                        "position": self._safe_stringify(_get_value(player, "position", "")),
                        "overall": self._safe_int(
                            _get_value(player, "overall", _get_value(player, "ovr", 0)), 0
                        ),
                        "injury_status": injury_status,
                        "injury_name": self._safe_stringify(injury_name) if injury_name else None,
                        "injury_start_date": self._format_date(start_raw),
                        "injury_end_date": self._format_date(end_raw),
                        "days_remaining": days_remaining,
                        "on_injured_reserve": on_ir,
                    }
                )

                if player_id:
                    seen_ids.add(player_id)

        team_id_value = getattr(team, "id", None) or str(resolved_team_id)
        return {"ok": True, "team_id": team_id_value, "entries": entries}

    def _with_pot(self, payload: Dict[str, Any], player: Any) -> Dict[str, Any]:
        payload["pot"] = self._resolve_pot(player)
        return payload

    def _safe_int(self, value: Any, default: int = 0) -> int:
        try:
            return int(value)
        except (TypeError, ValueError):
            return default

    def _safe_bool(self, value: Any, default: bool = False) -> bool:
        if isinstance(value, bool):
            return value
        if isinstance(value, str):
            lowered = value.strip().lower()
            if lowered in {"1", "true", "yes", "y", "on"}:
                return True
            if lowered in {"0", "false", "no", "n", "off"}:
                return False
        if value is None:
            return default
        return bool(value)

    def _safe_optional_int(self, value: Any) -> int | None:
        if value is None:
            return None
        try:
            return int(value)
        except (TypeError, ValueError):
            return None

    def _coerce_date(self, value: Any) -> datetime.date | None:
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

    def _format_date(self, value: Any) -> str | None:
        date_value = self._coerce_date(value)
        return date_value.isoformat() if date_value else None

    def _format_week_value(self, week: Any) -> Any:
        if week is None:
            return ""
        text = str(week)
        if text.isdigit():
            return int(text)
        return text

    def _normalize_day_name(self, day: Any) -> str:
        return str(day or "").strip().capitalize()

    def _normalize_week_key_map(self, data: Any) -> Dict[str, Any]:
        if not isinstance(data, dict):
            return {}
        normalized: Dict[str, Any] = {}
        for key, value in data.items():
            key_str = str(key)
            if key_str not in normalized:
                normalized[key_str] = value
                continue
            existing = normalized[key_str]
            if isinstance(existing, list) and isinstance(value, list):
                normalized[key_str] = existing + value
            elif isinstance(existing, list):
                normalized[key_str] = existing + [value]
            elif isinstance(value, list):
                normalized[key_str] = [existing] + value
            else:
                normalized[key_str] = value
        return normalized

    def _augment_schedule_games(self, week_key: Any, games: Any) -> None:
        if not isinstance(games, list):
            return
        calendar_week = self._calendar_week_value(week_key)
        season_type = None
        season_week = None
        if calendar_week is not None:
            season_info = self._season_info_for_calendar_week(calendar_week)
            if season_info is not None:
                season_type, season_week = season_info
        week_key_value = None
        if season_type is not None and season_week is not None:
            week_key_value = self._week_key_for_info(season_type, season_week)
        for game in games:
            if not isinstance(game, dict):
                continue
            if calendar_week is not None:
                game.setdefault("calendar_week", calendar_week)
                game.setdefault("week", calendar_week)
            if season_type is not None:
                game.setdefault("season_type", season_type)
            if season_week is not None:
                game.setdefault("season_week", season_week)
            if week_key_value is not None:
                game.setdefault("week_key", week_key_value)

    def _normalize_schedule_by_week(self, schedule: Any) -> Dict[str, Any]:
        normalized = self._normalize_week_key_map(schedule)
        cleaned: Dict[str, Any] = {}
        for week_key, games in normalized.items():
            if isinstance(games, list):
                self._augment_schedule_games(week_key, games)
                cleaned[week_key] = games
            elif isinstance(games, dict):
                wrapped = [games]
                self._augment_schedule_games(week_key, wrapped)
                cleaned[week_key] = wrapped
            else:
                cleaned[week_key] = []
        return cleaned

    def _normalize_results_by_week(self, results: Any) -> Dict[str, Any]:
        normalized = self._normalize_week_key_map(results)
        cleaned: Dict[str, Any] = {}
        for week_key, games in normalized.items():
            if isinstance(games, list):
                cleaned[week_key] = games
            elif isinstance(games, dict):
                cleaned[week_key] = [games]
            else:
                cleaned[week_key] = []
        return cleaned

    def _attach_season_fields(self, payload: Dict[str, Any], week_value: Any) -> None:
        if not isinstance(payload, dict):
            return
        calendar_week = payload.get("calendar_week")
        if calendar_week is None:
            calendar_week = payload.get("week", week_value)
        season_type = payload.get("season_type")
        season_week = payload.get("season_week")
        if season_type is None or season_week is None:
            season_info = self._season_info_for_calendar_week(calendar_week)
            if season_info is not None:
                season_type = season_type or season_info[0]
                if season_week is None:
                    season_week = season_info[1]
        if season_week is not None:
            season_week = self._safe_int(season_week, 0)
        if calendar_week is not None:
            payload.setdefault("calendar_week", self._format_week_value(calendar_week))
        if season_type is not None:
            payload.setdefault("season_type", season_type)
        if season_week is not None:
            payload.setdefault("season_week", season_week)
        if "week_key" not in payload and season_type is not None and season_week is not None:
            payload["week_key"] = self._week_key_for_info(season_type, int(season_week))

    def _expected_games_per_week(self, league: Any) -> int:
        teams = getattr(league, "teams", []) if league is not None else []
        team_count = sum(1 for team in teams if getattr(team, "id", None))
        if team_count < 2:
            return 0
        return team_count // 2

    def _schedule_has_full_week(self, schedule_by_week: Dict[str, Any], expected_games: int) -> bool:
        if expected_games <= 0:
            return False
        for games in schedule_by_week.values():
            if not isinstance(games, list):
                continue
            count = 0
            for game in games:
                if isinstance(game, dict) and game.get("home_id") and game.get("away_id"):
                    count += 1
            if count >= expected_games:
                return True
        return False

    def _schedule_is_valid(
        self,
        schedule_by_week: Dict[str, Any],
        expected_games: int,
        start_week: int = 1,
        end_week: int = FULL_SEASON_WEEKS,
        bye_weeks: set[int] | None = None,
    ) -> bool:
        if expected_games <= 0:
            return True
        if not isinstance(schedule_by_week, dict):
            return False
        bye_weeks = set(bye_weeks or [])
        for week in range(start_week, end_week + 1):
            games = schedule_by_week.get(str(week))
            if not isinstance(games, list):
                return False
            if week in bye_weeks:
                if games:
                    return False
                continue
            if len(games) != expected_games:
                return False
            seen = set()
            for game in games:
                if not isinstance(game, dict):
                    return False
                home_id = game.get("home_id")
                away_id = game.get("away_id")
                if not home_id or not away_id:
                    return False
                if home_id == away_id:
                    return False
                if home_id in seen or away_id in seen:
                    return False
                seen.add(home_id)
                seen.add(away_id)
        return True

    def _reset_team_records(self, league: LeagueManager | None) -> None:
        if league is None:
            return
        for team in getattr(league, "teams", []):
            team.team_record = {
                "wins": 0,
                "losses": 0,
                "ties": 0,
                "PF": 0,
                "PA": 0,
                "points_for": 0,
                "points_against": 0,
            }

    def _clear_derived_files(self, season_manager: SeasonManager) -> None:
        save_dir = Path(__file__).resolve().parents[2] / "data" / "saves" / self.save_name
        for filename in ("schedule_by_week.json", "schedule_by_team.json", "results_by_week.json"):
            path = save_dir / filename
            if path.exists():
                path.unlink()
        for standings_path in save_dir.glob("standings_*.json"):
            if standings_path.exists():
                standings_path.unlink()
        for box_dir in (save_dir / "boxscores", save_dir / "box_scores"):
            if box_dir.exists() and box_dir.is_dir():
                shutil.rmtree(box_dir)

        season_manager.results_by_week = {}
        if self.league is not None:
            self.league.results_by_week = {}
        if hasattr(season_manager, "standings_manager"):
            standings_manager = season_manager.standings_manager
            standings_manager.results_by_week = {}
            standings_manager.standings = standings_manager.load_standings()
            if self.league is not None:
                self.league.standings = standings_manager.standings
        self._reset_team_records(getattr(season_manager, "league", None))

    def _clear_results_and_standings(self, season_manager: SeasonManager) -> None:
        self._clear_derived_files(season_manager)

    def _get_schedule_by_week(self) -> Dict[str, Any]:
        if self.season_manager is not None and hasattr(self.season_manager, "schedule_by_week"):
            schedule = getattr(self.season_manager, "schedule_by_week", {}) or {}
        elif self.league is not None:
            schedule = getattr(self.league, "schedule_by_week", {}) or {}
        else:
            schedule = {}
        schedule = schedule if isinstance(schedule, dict) else {}
        return self._normalize_schedule_by_week(schedule)

    def _get_results_by_week(self) -> Dict[str, Any]:
        if self.season_manager is not None and hasattr(self.season_manager, "results_by_week"):
            results = getattr(self.season_manager, "results_by_week", {}) or {}
        elif self.league is not None:
            results = getattr(self.league, "results_by_week", {}) or {}
        else:
            results = {}
        results = results if isinstance(results, dict) else {}
        return self._normalize_results_by_week(results)

    def _resolve_team_id(self, key: Any) -> str | None:
        if key is None or self.league is None:
            return None
        key_str = str(key)
        if hasattr(self.league, "id_to_team") and key_str in self.league.id_to_team:
            return key_str
        if hasattr(self.league, "abbr_to_id") and key_str in self.league.abbr_to_id:
            return self.league.abbr_to_id[key_str]
        for team in getattr(self.league, "teams", []):
            if key_str == str(getattr(team, "team_name", "")):
                return team.id
            if key_str == str(getattr(team, "abbreviation", "")):
                return team.id
        return None

    def _team_for_key(self, key: Any) -> Any | None:
        if key is None or self.league is None:
            return None
        team_id = self._resolve_team_id(key) or str(key)
        if hasattr(self.league, "id_to_team"):
            team = self.league.id_to_team.get(team_id)
            if team is not None:
                return team
        for team in getattr(self.league, "teams", []):
            if str(getattr(team, "abbreviation", "")) == str(key):
                return team
            if str(getattr(team, "team_name", "")) == str(key):
                return team
        return None

    def _team_abbr(self, key: Any) -> str:
        if key is None:
            return ""
        team = self._team_for_key(key)
        if team is not None:
            return str(getattr(team, "abbreviation", "") or "")
        return str(key)

    def _display_date(self, value: datetime.date) -> str:
        return f"{value.strftime('%A, %b')} {value.day}, {value.year}"

    def _week_label_for_calendar_week(self, week: Any) -> str:
        season_info = self._season_info_for_calendar_week(week)
        if season_info is not None:
            return self._week_key_label(self._week_key_for_info(season_info[0], season_info[1]))
        if self.calendar is not None:
            try:
                if int(str(week)) == int(getattr(self.calendar, "current_week", 0)):
                    return self.calendar.get_week_label()
            except (TypeError, ValueError):
                pass
        return f"Week {week}"

    def _schedule_context_game(
        self,
        game: Dict[str, Any],
        week: Any,
        date_obj: datetime.date,
        day: str,
        game_id: str,
    ) -> Dict[str, Any]:
        home_id = game.get("home_id")
        away_id = game.get("away_id")
        home = self._team_abbr(home_id)
        away = self._team_abbr(away_id)
        week_label = game.get("week_label") or self._week_label_for_calendar_week(week)
        label = f"{self._display_date(date_obj)} — {away} at {home}"
        return {
            "game_id": game_id,
            "date": date_obj.isoformat(),
            "day_of_week": day,
            "label": label,
            "home": home,
            "away": away,
            "home_id": home_id,
            "away_id": away_id,
            "week": self._format_week_value(week),
            "calendar_week": game.get("calendar_week", self._format_week_value(week)),
            "week_key": game.get("week_key"),
            "week_label": week_label,
            "season_type": game.get("season_type") or "",
            "season_week": self._safe_int(game.get("season_week"), 0),
            "kickoff": str(game.get("kickoff") or ""),
            "status": "scheduled",
        }

    def _week_day_to_date(self, week: Any, day: Any) -> datetime.date | None:
        if self.calendar is None:
            return None
        current_date = getattr(self.calendar, "current_date", None)
        if not isinstance(current_date, datetime.date):
            return None
        try:
            week_int = int(str(week))
        except (TypeError, ValueError):
            return None
        day_name = self._normalize_day_name(day)
        days = getattr(self.calendar, "DAYS_OF_WEEK", [])
        if day_name not in days:
            return None
        try:
            current_week_int = int(getattr(self.calendar, "current_week", 0))
        except (TypeError, ValueError):
            return None
        week_start = current_date - datetime.timedelta(days=current_date.weekday())
        delta_weeks = week_int - current_week_int
        return week_start + datetime.timedelta(days=days.index(day_name) + delta_weeks * 7)

    def _parse_kickoff_minutes(self, kickoff: Any) -> int:
        if kickoff is None:
            return 0
        if isinstance(kickoff, (int, float)):
            return int(kickoff) * 60
        text = str(kickoff).strip()
        if not text:
            return 0
        upper = text.upper().replace(" ", "")
        if "AM" in upper or "PM" in upper:
            is_pm = "PM" in upper
            upper = upper.replace("AM", "").replace("PM", "")
            if ":" in upper:
                hour_str, minute_str = upper.split(":", 1)
            else:
                hour_str, minute_str = upper, "0"
            try:
                hour = int(hour_str)
                minute = int(minute_str[:2])
            except ValueError:
                return 0
            if hour == 12:
                hour = 0
            if is_pm:
                hour += 12
            return hour * 60 + minute
        if ":" in text:
            parts = text.split(":")
            try:
                hour = int(parts[0])
                minute = int(parts[1][:2])
            except ValueError:
                return 0
            return hour * 60 + minute
        try:
            return int(text) * 60
        except ValueError:
            return 0

    def _find_result_for_game(
        self,
        week: str,
        home_id: Any,
        away_id: Any,
        game_id: str,
        results_by_week: Dict[str, Any],
    ) -> Dict[str, Any] | None:
        results = results_by_week.get(str(week), [])
        if not isinstance(results, list):
            return None
        for result in results:
            if result.get("game_id") == game_id:
                return result
        resolved_home = self._resolve_team_id(home_id) or str(home_id)
        resolved_away = self._resolve_team_id(away_id) or str(away_id)
        for result in results:
            r_home = result.get("home_id") or result.get("home")
            r_away = result.get("away_id") or result.get("away")
            r_home_id = self._resolve_team_id(r_home) or str(r_home)
            r_away_id = self._resolve_team_id(r_away) or str(r_away)
            if r_home_id == resolved_home and r_away_id == resolved_away:
                return result
        return None

    def _iter_results_entries(self, results_by_week: Dict[str, Any]):
        for week_key, games in results_by_week.items():
            if isinstance(games, list):
                for result in games:
                    if isinstance(result, dict):
                        yield week_key, result
                continue
            if isinstance(games, dict):
                if any(key in games for key in ("home", "home_id", "away", "away_id", "game_id")):
                    yield week_key, games
                for list_key in ("games", "results", "items"):
                    items = games.get(list_key)
                    if isinstance(items, list):
                        for result in items:
                            if isinstance(result, dict):
                                yield week_key, result
                        break

    def _result_has_game_id(self, result: Dict[str, Any], game_id: str) -> bool:
        for key in ("game_id", "gameId", "id"):
            if str(result.get(key, "")) == game_id:
                return True
        return False

    def _is_placeholder_team_ref(self, value: Any) -> bool:
        text = str(value or "").strip()
        return not text or text.upper().startswith("TBD")

    def _result_is_user_facing(self, result: Dict[str, Any]) -> bool:
        home_value = self._result_team_value(result, "home")
        away_value = self._result_team_value(result, "away")
        if self._is_placeholder_team_ref(home_value) or self._is_placeholder_team_ref(away_value):
            return False
        return True

    def _extract_team_key(self, value: Any) -> Any:
        if isinstance(value, dict):
            for key in ("id", "team_id", "teamId", "abbr", "abbreviation", "short_name", "name", "team_name"):
                if value.get(key):
                    return value.get(key)
        return value

    def _result_team_value(self, result: Dict[str, Any], side: str) -> Any | None:
        keys = (
            f"{side}_id",
            side,
            f"{side}_team",
            f"{side}_abbr",
            f"{side}TeamId",
            f"{side}_team_id",
            f"{side}Id",
        )
        for key in keys:
            if key in result and result[key] not in (None, ""):
                return self._extract_team_key(result[key])
        teams = result.get("teams")
        if isinstance(teams, dict) and side in teams:
            return self._extract_team_key(teams.get(side))
        return None

    def _teams_match(self, left: Any, right: Any) -> bool:
        if left is None or right is None:
            return False
        left_resolved = self._resolve_team_id(left) or str(left)
        right_resolved = self._resolve_team_id(right) or str(right)
        if str(left_resolved).casefold() == str(right_resolved).casefold():
            return True
        return str(left).casefold() == str(right).casefold()

    def _build_minimal_box_score(self, payload: Dict[str, Any]) -> Dict[str, Any]:
        home_score = self._safe_optional_int(payload.get("home_score"))
        away_score = self._safe_optional_int(payload.get("away_score"))
        return {
            "final": {"home": home_score, "away": away_score},
            "scoring_by_quarter": {"home": [], "away": []},
            "team_stats": {"home": {}, "away": {}},
            "player_stats": [],
        }

    def _game_sort_key(self, week: Any, day: Any, kickoff: Any, game_id: Any) -> tuple:
        week_val = 0
        text = str(week) if week is not None else ""
        if text.isdigit():
            week_val = int(text)
        day_name = self._normalize_day_name(day)
        days = getattr(self.calendar, "DAYS_OF_WEEK", [])
        day_index = days.index(day_name) if day_name in days else 0
        kickoff_minutes = self._parse_kickoff_minutes(kickoff)
        return (week_val, day_index, kickoff_minutes, str(game_id))

    def get_standings(self) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        teams = list(getattr(self.league, "teams", []) or [])
        fallback_records = self._build_fallback_standings_records()
        league_standings = getattr(self.league, "standings", {}) if self.league is not None else {}
        standings_payload = []

        for team in teams:
            team_id = str(getattr(team, "id", "") or "")
            record = getattr(team, "team_record", None) or {}
            if not isinstance(record, dict):
                record = {}
            league_record = league_standings.get(team_id, {}) if isinstance(league_standings, dict) else {}
            if not isinstance(league_record, dict):
                league_record = {}
            fallback_record = fallback_records.get(team_id, {})

            record_wins = self._safe_int(record.get("wins", record.get("W")), 0)
            record_losses = self._safe_int(record.get("losses", record.get("L")), 0)
            record_ties = self._safe_int(record.get("ties", record.get("T")), 0)
            league_wins = self._safe_int(league_record.get("wins", league_record.get("W")), 0)
            league_losses = self._safe_int(league_record.get("losses", league_record.get("L")), 0)
            league_ties = self._safe_int(league_record.get("ties", league_record.get("T")), 0)
            fallback_wins = self._safe_int(fallback_record.get("wins"), 0)
            fallback_losses = self._safe_int(fallback_record.get("losses"), 0)
            fallback_ties = self._safe_int(fallback_record.get("ties"), 0)

            source_record = fallback_record
            if record_wins + record_losses + record_ties > 0:
                source_record = record
            elif league_wins + league_losses + league_ties > 0:
                source_record = league_record

            wins = self._safe_int(source_record.get("wins", source_record.get("W", fallback_wins)), fallback_wins)
            losses = self._safe_int(source_record.get("losses", source_record.get("L", fallback_losses)), fallback_losses)
            ties = self._safe_int(source_record.get("ties", source_record.get("T", fallback_ties)), fallback_ties)

            points_for = source_record.get("points_for", source_record.get("PF"))
            if points_for is None:
                points_for = fallback_record.get("points_for", 0)

            points_against = source_record.get("points_against", source_record.get("PA"))
            if points_against is None:
                points_against = fallback_record.get("points_against", 0)

            games_played = wins + losses + ties
            win_pct = round((wins + (0.5 * ties)) / games_played, 3) if games_played > 0 else 0.0
            team_name = self._dashboard_team_name(team) or str(getattr(team, "team_name", "") or "")
            abbreviation = str(getattr(team, "abbreviation", "") or team_id)

            standings_payload.append(
                {
                    "team_id": team_id,
                    "team_name": team_name,
                    "abbreviation": abbreviation,
                    "wins": wins,
                    "losses": losses,
                    "ties": ties,
                    "win_pct": win_pct,
                    "points_for": self._safe_int(points_for, 0),
                    "points_against": self._safe_int(points_against, 0),
                    "division": str(getattr(team, "division", "") or ""),
                    "conference": str(getattr(team, "conference", "") or ""),
                }
            )

        standings_payload.sort(
            key=lambda item: (
                -item["wins"],
                item["losses"],
                -float(item["win_pct"]),
                -(item["points_for"] - item["points_against"]),
                str(item["abbreviation"] or item["team_name"] or item["team_id"]),
            )
        )
        return {"ok": True, "standings": standings_payload}

    def _build_fallback_standings_records(self) -> Dict[str, Dict[str, int]]:
        records: Dict[str, Dict[str, int]] = {}
        for team in getattr(self.league, "teams", []) or []:
            team_id = str(getattr(team, "id", "") or "")
            if not team_id:
                continue
            records[team_id] = {
                "wins": 0,
                "losses": 0,
                "ties": 0,
                "points_for": 0,
                "points_against": 0,
            }

        results_by_week = self._get_results_by_week()
        for _, result in self._iter_results_entries(results_by_week):
            if not isinstance(result, dict):
                continue
            home_id = self._resolve_team_id(self._result_team_value(result, "home")) or str(self._result_team_value(result, "home") or "")
            away_id = self._resolve_team_id(self._result_team_value(result, "away")) or str(self._result_team_value(result, "away") or "")
            if not home_id or not away_id:
                continue

            home_score = self._safe_optional_int(result.get("home_score"))
            away_score = self._safe_optional_int(result.get("away_score"))
            if home_score is None or away_score is None:
                continue

            records.setdefault(home_id, {"wins": 0, "losses": 0, "ties": 0, "points_for": 0, "points_against": 0})
            records.setdefault(away_id, {"wins": 0, "losses": 0, "ties": 0, "points_for": 0, "points_against": 0})
            records[home_id]["points_for"] += home_score
            records[home_id]["points_against"] += away_score
            records[away_id]["points_for"] += away_score
            records[away_id]["points_against"] += home_score

            if home_score > away_score:
                records[home_id]["wins"] += 1
                records[away_id]["losses"] += 1
            elif away_score > home_score:
                records[away_id]["wins"] += 1
                records[home_id]["losses"] += 1
            else:
                records[home_id]["ties"] += 1
                records[away_id]["ties"] += 1

        return records

    def _week_sort_key(self, value: Any) -> tuple[int, Any]:
        text = str(value)
        if text.isdigit():
            return (0, int(text))
        return (1, text)

    def _unique_week_keys(self, keys: list[Any]) -> list[str]:
        seen: set[str] = set()
        ordered: list[str] = []
        for key in keys:
            key_str = str(key)
            if key_str in seen:
                continue
            seen.add(key_str)
            ordered.append(key_str)
        return ordered

    def _calendar_week_value(self, value: Any) -> int | None:
        text = str(value).strip()
        if text.isdigit():
            return int(text)
        return None

    def _season_info_for_calendar_week(self, week: Any) -> tuple[str, int] | None:
        week_int = self._calendar_week_value(week)
        if week_int is None or week_int <= 0:
            return None
        if week_int <= PRESEASON_WEEKS:
            return ("preseason", week_int)
        if week_int == PRESEASON_BYE_WEEK:
            return ("bye", 0)
        regular_end = REGULAR_SEASON_START_WEEK + REGULAR_SEASON_WEEKS - 1
        if week_int <= regular_end:
            return ("regular", week_int - REGULAR_SEASON_START_WEEK + 1)
        calendar = self.calendar or getattr(self.season_manager, "calendar", None)
        if calendar is None:
            return ("postseason", week_int - regular_end)
        playoff_start = calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][0]
        playoff_end = calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][1]
        postseason_start = calendar.phase_boundaries[calendar.PHASE_POSTSEASON][0]
        if playoff_start <= week_int <= playoff_end:
            return ("playoffs", week_int - playoff_start + 1)
        if week_int >= postseason_start:
            return ("postseason", week_int - postseason_start + 1)
        return None

    def _week_key_for_info(self, season_type: str, season_week: int) -> str:
        return f"{season_type}:{season_week}"

    def _parse_week_key(self, value: Any) -> tuple[str | None, int | None]:
        if value is None:
            return (None, None)
        text = str(value).strip()
        if not text or ":" not in text:
            return (None, None)
        season_text, week_text = text.split(":", 1)
        season_type = season_text.strip().lower()
        if not season_type:
            return (None, None)
        week_text = week_text.strip()
        if not week_text.isdigit():
            return (None, None)
        return (season_type, int(week_text))

    def _calendar_week_from_week_key(self, value: Any) -> int | None:
        season_type, season_week = self._parse_week_key(value)
        if season_type is None or season_week is None or season_week <= 0:
            return None
        if season_type == "preseason":
            return season_week
        if season_type == "bye":
            return PRESEASON_BYE_WEEK
        if season_type == "regular":
            return REGULAR_SEASON_START_WEEK + season_week - 1
        if season_type == "playoffs":
            calendar = self.calendar or getattr(self.season_manager, "calendar", None)
            if calendar is None:
                return FULL_SEASON_WEEKS + season_week
            return calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][0] + season_week - 1
        if season_type == "postseason":
            calendar = self.calendar or getattr(self.season_manager, "calendar", None)
            if calendar is None:
                return FULL_SEASON_WEEKS + season_week
            return calendar.phase_boundaries[calendar.PHASE_POSTSEASON][0] + season_week - 1
        return None

    def _week_key_for_calendar_week(self, week: Any) -> str | None:
        info = self._season_info_for_calendar_week(week)
        if info is None:
            return None
        season_type, season_week = info
        return self._week_key_for_info(season_type, season_week)

    def _week_key_label(self, week_key: Any) -> str:
        season_type, season_week = self._parse_week_key(week_key)
        if season_type is None or season_week is None:
            return str(week_key)
        if season_type == "preseason":
            return f"Preseason Week {season_week}"
        if season_type == "regular":
            return f"Regular Season Week {season_week}"
        if season_type == "playoffs":
            calendar = self.calendar or getattr(self.season_manager, "calendar", None)
            if calendar is not None:
                playoff_week = calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][0] + season_week - 1
                subphase = calendar.playoff_subphases.get(playoff_week)
                if subphase:
                    return f"Playoffs - {subphase}"
            return f"Playoffs Week {season_week}"
        if season_type == "bye":
            return "Preseason Bye / Final Cutdown"
        if season_type == "postseason":
            return f"Postseason Week {season_week}"
        return f"{season_type.title()} W{season_week}"

    def _has_scheduled_games(self, games: Any) -> bool:
        if not isinstance(games, list):
            return False
        for game in games:
            if not isinstance(game, dict):
                continue
            home_id = game.get("home_id") or game.get("home")
            away_id = game.get("away_id") or game.get("away")
            if home_id and away_id:
                return True
        return False

    def _available_schedule_calendar_weeks(self, schedule_by_week: Dict[str, Any]) -> list[str]:
        weeks = [
            week_key
            for week_key, games in schedule_by_week.items()
            if self._has_scheduled_games(games)
        ]
        return sorted(self._unique_week_keys(weeks), key=self._week_sort_key)

    def _completed_results_calendar_weeks(self, results_by_week: Dict[str, Any]) -> list[str]:
        weeks = [
            week_key
            for week_key, games in results_by_week.items()
            if isinstance(games, list) and any(
                isinstance(game, dict) and self._result_is_user_facing(game)
                for game in games
            )
        ]
        return sorted(self._unique_week_keys(weeks), key=self._week_sort_key)

    def _resolve_results_week_key(
        self,
        *,
        week: Any,
        season_type: Any,
        season_week: Any,
        week_key: Any,
        available_keys: list[str],
        completed_keys: list[str],
    ) -> str | None:
        if week_key is None and isinstance(week, str) and ":" in week:
            week_key = week
            week = None
        if week_key is not None:
            parsed_type, parsed_week = self._parse_week_key(week_key)
            if parsed_type is not None and parsed_week is not None:
                return self._week_key_for_info(parsed_type, parsed_week)
        if season_type is not None and season_week is not None:
            type_text = str(season_type).strip().lower()
            week_text = str(season_week).strip()
            if type_text and week_text.isdigit():
                return self._week_key_for_info(type_text, int(week_text))
        if week is not None:
            week_text = str(week).strip()
            if week_text and week_text.lower() not in ("null", "none"):
                calendar_week = self._calendar_week_value(week_text)
                if calendar_week is not None:
                    derived_key = self._week_key_for_calendar_week(calendar_week)
                    if derived_key:
                        return derived_key
        if completed_keys:
            return completed_keys[-1]
        if available_keys:
            return available_keys[0]
        return None

    def get_results(
        self,
        week: Any = None,
        *,
        season_type: Any = None,
        season_week: Any = None,
        week_key: Any = None,
    ) -> Dict[str, Any]:
        self._ensure_game()
        schedule_by_week = self._get_schedule_by_week()
        results_by_week = self._get_results_by_week()
        available_calendar_weeks = self._available_schedule_calendar_weeks(schedule_by_week)
        completed_calendar_weeks = self._completed_results_calendar_weeks(results_by_week)
        available_week_keys = [
            key
            for week_value in available_calendar_weeks
            for key in [self._week_key_for_calendar_week(week_value)]
            if key is not None
        ]
        completed_week_keys = [
            key
            for week_value in completed_calendar_weeks
            for key in [self._week_key_for_calendar_week(week_value)]
            if key is not None
        ]
        selected_week_key = self._resolve_results_week_key(
            week=week,
            season_type=season_type,
            season_week=season_week,
            week_key=week_key,
            available_keys=available_week_keys,
            completed_keys=completed_week_keys,
        )
        selected_calendar_week = self._calendar_week_from_week_key(selected_week_key)
        if selected_calendar_week is None:
            selected_calendar_week = self._calendar_week_value(week)
        if selected_calendar_week is None and available_calendar_weeks:
            selected_calendar_week = self._calendar_week_value(available_calendar_weeks[0])
        if selected_calendar_week is None:
            selected_calendar_week = 1
        week_str = str(selected_calendar_week)
        season_info = self._season_info_for_calendar_week(selected_calendar_week)
        selected_season_type = season_info[0] if season_info else None
        selected_season_week = season_info[1] if season_info else None
        if selected_week_key is None and selected_season_type is not None and selected_season_week is not None:
            selected_week_key = self._week_key_for_info(selected_season_type, selected_season_week)
        schedule_games = schedule_by_week.get(week_str, []) or []
        results_games = results_by_week.get(week_str, []) or []
        default_week_key = selected_week_key or self._week_key_for_calendar_week(selected_calendar_week) or str(selected_calendar_week)
        default_week_label = self._week_key_label(default_week_key)
        games_payload = []
        matched_game_ids = set()
        matched_pairs = set()
        for game in schedule_games:
            if not isinstance(game, dict):
                continue
            home_id = game.get("home_id")
            away_id = game.get("away_id")
            if not home_id or not away_id:
                continue
            game_id = make_game_id(week_str, home_id, away_id)
            result = self._find_result_for_game(week_str, home_id, away_id, game_id, results_by_week)
            if result is not None and not self._result_is_user_facing(result):
                result = None
            status = "final" if result else "scheduled"
            home_score = self._safe_optional_int(result.get("home_score")) if result else None
            away_score = self._safe_optional_int(result.get("away_score")) if result else None
            box_score = result.get("box_score") if result else None
            if box_score is not None:
                box_score = sanitize_box_score_numbers(box_score)
            day = self._normalize_day_name(game.get("day"))
            kickoff = game.get("kickoff") or (result.get("kickoff_time") if result else None) or ""
            date = game.get("date")
            if not date and result:
                date = result.get("date")
            if not date:
                date_obj = self._week_day_to_date(week_str, day)
                date = date_obj.isoformat() if date_obj else None
            game_season_type = game.get("season_type") or selected_season_type or ""
            game_season_week = game.get("season_week")
            if game_season_week is None:
                game_season_week = selected_season_week or 0
            else:
                game_season_week = self._safe_int(game_season_week, 0)
            game_calendar_week = game.get("calendar_week")
            if game_calendar_week is None:
                game_calendar_week = selected_calendar_week
            games_payload.append(
                {
                    "game_id": game_id,
                    "calendar_week": game_calendar_week,
                    "season_type": game_season_type,
                    "season_week": game_season_week,
                    "day": day,
                    "kickoff": str(kickoff) if kickoff is not None else "",
                    "date": date,
                    "home_abbr": game.get("home_abbr") or self._team_abbr(home_id),
                    "away_abbr": game.get("away_abbr") or self._team_abbr(away_id),
                    "home_score": home_score,
                    "away_score": away_score,
                    "status": status,
                    "box_score": box_score,
                }
            )
            matched_game_ids.add(game_id)
            resolved_home = self._resolve_team_id(home_id) or str(home_id)
            resolved_away = self._resolve_team_id(away_id) or str(away_id)
            matched_pairs.add((resolved_home, resolved_away))
        for result in results_games:
            if not isinstance(result, dict):
                continue
            if not self._result_is_user_facing(result):
                continue
            result_game_id = result.get("game_id")
            home_key = result.get("home_id") or result.get("home")
            away_key = result.get("away_id") or result.get("away")
            resolved_home = self._resolve_team_id(home_key) or str(home_key)
            resolved_away = self._resolve_team_id(away_key) or str(away_key)
            if result_game_id and result_game_id in matched_game_ids:
                continue
            if (resolved_home, resolved_away) in matched_pairs:
                continue
            game_id = result_game_id or make_game_id(week_str, resolved_home, resolved_away)
            day = self._normalize_day_name(result.get("day"))
            kickoff = result.get("kickoff_time") or result.get("kickoff") or ""
            date = result.get("date")
            if not date:
                date_obj = self._week_day_to_date(week_str, day)
                date = date_obj.isoformat() if date_obj else None
            result_season_type = result.get("season_type") or selected_season_type or ""
            result_season_week = result.get("season_week")
            if result_season_week is None:
                result_season_week = selected_season_week or 0
            else:
                result_season_week = self._safe_int(result_season_week, 0)
            result_calendar_week = result.get("calendar_week")
            if result_calendar_week is None:
                result_calendar_week = selected_calendar_week
            games_payload.append(
                {
                    "game_id": game_id,
                    "calendar_week": result_calendar_week,
                    "season_type": result_season_type,
                    "season_week": result_season_week,
                    "day": day,
                    "kickoff": str(kickoff) if kickoff is not None else "",
                    "date": date,
                    "home_abbr": self._team_abbr(home_key),
                    "away_abbr": self._team_abbr(away_key),
                    "home_score": self._safe_optional_int(result.get("home_score")),
                    "away_score": self._safe_optional_int(result.get("away_score")),
                    "status": "final",
                    "box_score": sanitize_box_score_numbers(result.get("box_score"))
                    if result.get("box_score") is not None
                    else None,
                }
            )
        games_payload.sort(
            key=lambda game: self._game_sort_key(week_str, game.get("day"), game.get("kickoff"), game.get("game_id"))
        )
        if self.debug_schedule:
            schedule_count = len(schedule_games) if isinstance(schedule_games, list) else 0
            print(
                f"[API] schedule week {week_str}: {schedule_count} games; results payload: {len(games_payload)}"
            )
        available_week_labels = [self._week_key_label(key) for key in available_week_keys]
        completed_week_labels = [self._week_key_label(key) for key in completed_week_keys]
        return {
            "ok": True,
            "week": self._format_week_value(week_str),
            "week_key": default_week_key,
            "week_label": default_week_label,
            "games": games_payload,
            "available_week_keys": available_week_keys,
            "available_week_labels": available_week_labels,
            "completed_week_keys": completed_week_keys,
            "completed_week_labels": completed_week_labels,
            "available_weeks": [self._format_week_value(w) for w in available_calendar_weeks],
            "completed_weeks": [self._format_week_value(w) for w in completed_calendar_weeks],
        }

    def get_game(self, game_id: str) -> Dict[str, Any]:
        self._ensure_game()
        if not game_id:
            return {"ok": False, "error": "not found"}
        results_by_week = self._get_results_by_week()
        game_id_str = str(game_id)
        result = None
        week_hint = None

        direct = results_by_week.get(game_id_str)
        if isinstance(direct, dict):
            result = direct
            week_hint = direct.get("week")
        elif isinstance(direct, list):
            for entry in direct:
                if isinstance(entry, dict) and self._result_has_game_id(entry, game_id_str):
                    result = entry
                    week_hint = entry.get("week")
                    break
            if result is None and len(direct) == 1 and isinstance(direct[0], dict):
                result = direct[0]
                week_hint = direct[0].get("week")

        if result is None:
            for week_key, entry in self._iter_results_entries(results_by_week):
                if self._result_has_game_id(entry, game_id_str):
                    result = entry
                    week_hint = week_key
                    break

        parts = game_id_str.split("|")
        home_key = None
        away_key = None
        week_key = None
        if len(parts) >= 3:
            week_key, home_key, away_key = parts[0], parts[1], parts[2]

        if result is None and home_key and away_key:
            for week, entry in self._iter_results_entries(results_by_week):
                if week_key and str(week) != str(week_key):
                    continue
                r_home = self._result_team_value(entry, "home")
                r_away = self._result_team_value(entry, "away")
                if not r_home or not r_away:
                    continue
                if self._teams_match(r_home, home_key) and self._teams_match(r_away, away_key):
                    result = entry
                    week_hint = week
                    break
                if self._teams_match(r_home, away_key) and self._teams_match(r_away, home_key):
                    result = entry
                    week_hint = week
                    break

        if result is None and home_key and away_key:
            for week, entry in self._iter_results_entries(results_by_week):
                r_home = self._result_team_value(entry, "home")
                r_away = self._result_team_value(entry, "away")
                if not r_home or not r_away:
                    continue
                if self._teams_match(r_home, home_key) and self._teams_match(r_away, away_key):
                    result = entry
                    week_hint = week
                    break
                if self._teams_match(r_home, away_key) and self._teams_match(r_away, home_key):
                    result = entry
                    week_hint = week
                    break

        if result is not None:
            payload = dict(result)
            payload.setdefault("game_id", game_id_str)
            if week_hint:
                payload.setdefault("week", week_hint)
            if payload.get("box_score") is not None:
                payload["box_score"] = sanitize_box_score_numbers(payload["box_score"])
            else:
                logging.warning("TODO: missing box score generation for game_id=%s", game_id_str)
                payload["box_score"] = self._build_minimal_box_score(payload)
            self._attach_season_fields(payload, week_hint)
            return {"ok": True, "game": payload}

        if home_key and away_key:
            payload = {
                "game_id": game_id_str,
                "week": week_key,
                "home": home_key,
                "away": away_key,
            }
            logging.warning("TODO: missing game result for game_id=%s", game_id_str)
            payload["box_score"] = self._build_minimal_box_score(payload)
            self._attach_season_fields(payload, week_key)
            return {"ok": True, "game": payload}

        return {"ok": False, "error": "not found"}

    def get_game_result(self, game_id: str) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}
        if not str(game_id or "").strip():
            return {"ok": False, "error": "Missing game_id."}

        result, week_hint = self._find_completed_result_entry(str(game_id).strip())
        if result is None:
            return {"ok": False, "error": "Game result not found."}

        payload = dict(result)
        payload.setdefault("game_id", str(game_id).strip())
        self._attach_season_fields(payload, week_hint)
        compact = self._compact_sim_result(payload)
        return {"ok": True, "result": compact}

    def _find_completed_result_entry(self, game_id: str) -> tuple[Dict[str, Any] | None, Any]:
        results_by_week = self._get_results_by_week()
        game_id_str = str(game_id or "").strip()
        if not game_id_str:
            return None, None

        direct = results_by_week.get(game_id_str)
        if isinstance(direct, dict):
            return direct, direct.get("week")
        if isinstance(direct, list):
            for entry in direct:
                if isinstance(entry, dict) and self._result_has_game_id(entry, game_id_str):
                    return entry, entry.get("week")
            if len(direct) == 1 and isinstance(direct[0], dict):
                return direct[0], direct[0].get("week")

        for week_key, entry in self._iter_results_entries(results_by_week):
            if self._result_has_game_id(entry, game_id_str):
                return entry, week_key

        parts = game_id_str.split("|")
        home_key = None
        away_key = None
        week_key = None
        if len(parts) >= 3:
            week_key, home_key, away_key = parts[0], parts[1], parts[2]

        if home_key and away_key:
            for week, entry in self._iter_results_entries(results_by_week):
                if week_key and str(week) != str(week_key):
                    continue
                r_home = self._result_team_value(entry, "home")
                r_away = self._result_team_value(entry, "away")
                if not r_home or not r_away:
                    continue
                if self._teams_match(r_home, home_key) and self._teams_match(r_away, away_key):
                    return entry, week
                if self._teams_match(r_home, away_key) and self._teams_match(r_away, home_key):
                    return entry, week

        return None, None

    def get_team_schedule(self, team_id: str | None = None, limit: int | None = None) -> Dict[str, Any]:
        if not self.has_active_game():
            return {"ok": False, "error": "No active league loaded."}

        requested_team_id = str(team_id or "").strip()
        resolved_id = self._resolve_team_id(requested_team_id) if requested_team_id else ""
        if not resolved_id:
            resolved_id = str(getattr(self.league, "user_team_id", "") or "").strip()
        if not resolved_id:
            return {"ok": False, "error": "No active league loaded."}
        if self._team_for_key(resolved_id) is None and self._resolve_team_id(resolved_id) is None:
            return {"ok": False, "error": "team_not_found"}

        current_user_game = self._compact_continue_current_user_game() or {}
        current_game_id = str(current_user_game.get("game_id") or "").strip()
        user_team_id = str(getattr(self.league, "user_team_id", "") or "").strip()
        schedule_by_week = self._get_schedule_by_week()
        results_by_week = self._get_results_by_week()
        schedule_payload = []

        for week_key, games in schedule_by_week.items():
            if not isinstance(games, list):
                continue
            week_str = str(week_key)
            for game in games:
                if not isinstance(game, dict):
                    continue
                home_id = str(game.get("home_id") or "")
                away_id = str(game.get("away_id") or "")
                if resolved_id not in {home_id, away_id}:
                    continue
                if not home_id or not away_id:
                    continue

                opponent_id = away_id if resolved_id == home_id else home_id
                game_id = str(game.get("game_id") or make_game_id(week_str, home_id, away_id))
                result = self._find_result_for_game(week_str, home_id, away_id, game_id, results_by_week)

                status = "upcoming"
                if isinstance(result, dict):
                    status = "final"
                elif resolved_id == user_team_id and current_game_id and game_id == current_game_id:
                    status = "game_day"

                season_type = str(game.get("season_type") or "").strip()
                season_week = game.get("season_week")
                if not season_type or season_week is None:
                    season_info = self._season_info_for_calendar_week(week_str)
                    if season_info is not None:
                        if not season_type:
                            season_type = str(season_info[0] or "").strip()
                        if season_week is None:
                            season_week = season_info[1]
                week_value = self._safe_int(season_week, self._format_week_value(week_str))

                home_score = None
                away_score = None
                winner = None
                if isinstance(result, dict):
                    home_score = self._safe_optional_int(result.get("home_score"))
                    away_score = self._safe_optional_int(result.get("away_score"))
                    winner = self._team_abbr(result.get("winner")) or str(result.get("winner") or "").strip() or None

                schedule_payload.append(
                    {
                        "game_id": game_id or None,
                        "week": week_value,
                        "game_type": season_type or "",
                        "opponent": self._team_abbr(opponent_id),
                        "home_away": "home" if resolved_id == home_id else "away",
                        "status": status,
                        "home_team": self._team_abbr(home_id),
                        "away_team": self._team_abbr(away_id),
                        "home_score": home_score,
                        "away_score": away_score,
                        "winner": winner,
                        "day": self._normalize_day_name(game.get("day")),
                        "kickoff": str(game.get("kickoff") or ""),
                    }
                )

        schedule_payload.sort(
            key=lambda game: self._game_sort_key(game.get("week"), game.get("day"), game.get("kickoff"), game.get("game_id"))
        )
        if limit is not None and limit > 0 and len(schedule_payload) > limit:
            schedule_payload = schedule_payload[:limit]
        for game in schedule_payload:
            game.pop("day", None)
            game.pop("kickoff", None)
        return {"ok": True, "team_id": resolved_id, "schedule": schedule_payload}

    def get_next_user_game(self) -> Dict[str, Any]:
        self._ensure_game()
        engine = self._get_time_engine()
        user_team_id = engine.user_team_id or getattr(self.league, "user_team_id", None)
        if not user_team_id:
            return {"ok": True, "game": None}
        schedule_by_week = self._get_schedule_by_week()
        results_by_week = self._get_results_by_week()
        current_date = engine.clock.current_date
        current_minutes = engine.clock.hour * 60
        candidates = []
        for week_key, games in schedule_by_week.items():
            if not isinstance(games, list):
                continue
            week_str = str(week_key)
            for game in games:
                if not isinstance(game, dict):
                    continue
                home_id = game.get("home_id")
                away_id = game.get("away_id")
                if user_team_id not in (home_id, away_id):
                    continue
                if self._is_placeholder_team_ref(home_id) or self._is_placeholder_team_ref(away_id):
                    continue
                game_id = make_game_id(week_str, home_id, away_id)
                if self._find_result_for_game(week_str, home_id, away_id, game_id, results_by_week):
                    continue
                day = self._normalize_day_name(game.get("day"))
                date_obj = self._week_day_to_date(week_str, day)
                if date_obj is None:
                    continue
                kickoff_minutes = self._parse_kickoff_minutes(game.get("kickoff"))
                if date_obj < current_date:
                    continue
                if date_obj == current_date and kickoff_minutes <= current_minutes:
                    continue
                opponent_id = away_id if user_team_id == home_id else home_id
                entry = {
                    "week": self._format_week_value(week_str),
                    "date": date_obj.isoformat(),
                    "day": day,
                    "kickoff": str(game.get("kickoff") or ""),
                    "opponent_abbr": self._team_abbr(opponent_id),
                    "home": user_team_id == home_id,
                    "game_id": game_id,
                    "status": "scheduled",
                    "team_score": None,
                    "opp_score": None,
                }
                candidates.append((date_obj.toordinal(), kickoff_minutes, entry))
        if not candidates:
            return {"ok": True, "game": None}
        candidates.sort(key=lambda item: (item[0], item[1], str(item[2].get("game_id"))))
        return {"ok": True, "game": candidates[0][2]}

    def _get_time_engine(self) -> TimeEngine:
        self._ensure_game()
        if self._time_engine is None or self._time_engine.league is not self.league:
            self._time_engine = TimeEngine(self.league, self.calendar, self.season_manager)
        if self.season_manager is not None:
            self._time_engine.schedule_by_week = getattr(
                self.season_manager, "schedule_by_week", self._time_engine.schedule_by_week
            )
        self._time_engine.user_team_id = getattr(self.league, "user_team_id", self._time_engine.user_team_id)
        if self.calendar is not None and self._time_engine.clock.current_date != self.calendar.current_date:
            self._time_engine.clock.current_date = self.calendar.current_date
            self._time_engine.clock.hour = parse_clock_hour(
                getattr(self.calendar, "current_time_str", self._time_engine.clock.current_time_str)
            )
        return self._time_engine

    def _sync_schedule(self, season_manager: SeasonManager, schedule: Dict[str, Any]) -> None:
        if not schedule:
            return
        calendar = self.calendar or getattr(season_manager, "calendar", None)
        if calendar is None:
            return
        normalized = self._normalize_schedule_by_week(schedule)
        if not normalized:
            return
        expected_games = self._expected_games_per_week(getattr(season_manager, "league", None))
        if expected_games > 0 and not self._schedule_has_full_week(normalized, expected_games):
            existing = self._normalize_schedule_by_week(getattr(season_manager, "schedule_by_week", {}) or {})
            if self._schedule_has_full_week(existing, expected_games):
                normalized = existing
        schedule_by_team = None
        if expected_games > 0 and not self._schedule_is_valid(
            normalized,
            expected_games,
            1,
            FULL_SEASON_WEEKS,
            bye_weeks={PRESEASON_BYE_WEEK},
        ):
            league = getattr(season_manager, "league", None) or self.league
            self._clear_derived_files(season_manager)
            regenerated, regenerated_by_team = generate_full_schedule_files(
                league,
                save_name=self.save_name,
                weeks=FULL_SEASON_WEEKS,
            )
            regenerated = self._normalize_schedule_by_week(regenerated)
            if regenerated:
                normalized = regenerated
                schedule_by_team = regenerated_by_team if isinstance(regenerated_by_team, dict) else {}
        season_manager.schedule_by_week = normalized
        if schedule_by_team is not None:
            season_manager.schedule_by_team = schedule_by_team
        last_days = {}
        for week, games in normalized.items():
            if not games:
                last_days[str(week)] = 6
                continue
            indices = [
                calendar.DAYS_OF_WEEK.index(str(game.get("day", "")).strip().capitalize())
                for game in games
                if str(game.get("day", "")).strip().capitalize() in calendar.DAYS_OF_WEEK
            ]
            last_days[str(week)] = max(indices) if indices else 6
        season_manager.last_scheduled_day_for_week = last_days

    def _get_time_payload(self) -> Dict[str, Any]:
        engine = self._get_time_engine()
        clock = engine.clock
        return {
            "current_date": clock.current_date.isoformat(),
            "current_time": clock.current_time_str,
            "current_week": getattr(self.calendar, "current_week", None),
            "current_phase": getattr(self.calendar, "season_phase", None),
            "current_year": getattr(self.calendar, "current_year", None),
            "date": clock.current_date.isoformat(),
            "hour": clock.hour,
            "unread_inbox": engine.unread_inbox_count(),
            "blocking_decision_count": len(engine.get_blocking_decisions()),
            "next_user_event": engine.next_user_event_time(),
            "latest_inbox": engine.latest_inbox_preview(),
        }

    def get_calendar_dashboard(self) -> Dict[str, Any]:
        self._ensure_game()
        engine = self._get_time_engine()
        engine.ensure_agenda_for_today()
        clock = engine.clock
        current_date = clock.current_date
        current_hour = int(getattr(clock, "hour", 0))
        queue_events = list(engine.queue.events())
        next_event = None
        today_events = []

        for event in queue_events:
            if event.date == current_date and event.hour >= current_hour:
                today_events.append(self._calendar_dashboard_event_dict(event, engine))
            if next_event is None and (
                event.date > current_date
                or (event.date == current_date and event.hour > current_hour)
            ):
                next_event = self._calendar_dashboard_event_dict(event, engine)

        notifications = self._calendar_dashboard_notifications(engine, limit=10)
        blocking_decisions = [self._compact_decision_payload(item) for item in engine.get_blocking_decisions()]
        return {
            "current_date": clock.current_date.isoformat(),
            "current_time": clock.current_time_str,
            "current_week": getattr(self.calendar, "current_week", None),
            "current_phase": getattr(self.calendar, "season_phase", None),
            "current_year": getattr(self.calendar, "current_year", None),
            "gm": self._calendar_dashboard_gm_payload(),
            "team": self._calendar_dashboard_team_payload(engine.user_team_id),
            "next_event": next_event,
            "today_events": today_events,
            "notifications": notifications,
            "blocking_decision_count": len(blocking_decisions),
            "blocking_decisions": blocking_decisions,
            "available_actions": self._calendar_dashboard_actions(),
        }

    def _calendar_dashboard_gm_payload(self) -> Dict[str, Any]:
        gm_profile = self._ensure_user_gm_profile(
            team_id=getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
        )
        return {
            "gm_id": gm_profile.gm_id,
            "name": gm_profile.name,
            "current_team_id": gm_profile.current_team_id,
            "current_role": gm_profile.current_role,
            "reputation": int(gm_profile.reputation),
            "job_security": int(gm_profile.job_security),
        }

    def get_gm_profile(self) -> Dict[str, Any]:
        self._ensure_game()
        gm_profile = self._ensure_user_gm_profile(
            team_id=getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
        )
        return {
            "ok": True,
            "gm": {
                "gm_id": gm_profile.gm_id,
                "name": gm_profile.name,
                "current_team_id": gm_profile.current_team_id,
                "current_role": gm_profile.current_role,
                "reputation": int(gm_profile.reputation),
                "job_security": int(gm_profile.job_security),
                "career_start_year": int(gm_profile.career_start_year),
                "traits": list(gm_profile.traits),
                "career_history": [entry.to_dict() for entry in gm_profile.career_history],
            },
        }

    def _calendar_dashboard_team_payload(self, team_id: str | None) -> Dict[str, Any]:
        team = None
        if team_id and hasattr(self.league, "id_to_team"):
            team = self.league.id_to_team.get(team_id)
        if team is None:
            return {"team_id": team_id, "name": "", "record": "0-0"}
        record = getattr(team, "team_record", {}) or {}
        wins = int(record.get("wins", 0) or 0)
        losses = int(record.get("losses", 0) or 0)
        ties = int(record.get("ties", 0) or 0)
        record_text = f"{wins}-{losses}" if ties <= 0 else f"{wins}-{losses}-{ties}"
        team_name = getattr(team, "team_name", None) or getattr(team, "name", None) or ""
        return {
            "team_id": getattr(team, "id", team_id),
            "name": str(team_name),
            "record": record_text,
        }

    def _calendar_dashboard_actions(self) -> list[str]:
        return [
            "advance_to_next_event",
            "advance_to_end_of_day",
            "advance_one_week",
            "advance_to_milestone",
        ]

    def _calendar_dashboard_notifications(
        self,
        engine: TimeEngine,
        limit: int = 10,
    ) -> list[Dict[str, Any]]:
        messages = engine.get_inbox()
        notifications = []
        for msg in messages[: max(0, int(limit))]:
            notifications.append(engine._notification_summary(msg))
        return notifications

    def _compact_decision_payload(self, decision: Any) -> Dict[str, Any]:
        if hasattr(decision, "to_dict"):
            payload = decision.to_dict()
        elif isinstance(decision, dict):
            payload = dict(decision)
        else:
            payload = {}
        return {
            "decision_id": str(payload.get("decision_id") or ""),
            "created_at_date": str(payload.get("created_at_date") or ""),
            "created_at_time": str(payload.get("created_at_time") or ""),
            "category": str(payload.get("category") or ""),
            "decision_type": str(payload.get("decision_type") or ""),
            "title": str(payload.get("title") or ""),
            "message": str(payload.get("message") or ""),
            "priority": self._safe_int(payload.get("priority"), 0),
            "status": str(payload.get("status") or ""),
            "blocks_advancement": bool(payload.get("blocks_advancement", False)),
            "options": [dict(option) for option in payload.get("options", []) if isinstance(option, dict)],
            "selected_option": payload.get("selected_option"),
        }

    def _calendar_dashboard_event_dict(
        self,
        event: Any,
        engine: TimeEngine,
    ) -> Dict[str, Any]:
        return {
            "event_id": str(getattr(event, "id", "")),
            "date": event.date.isoformat(),
            "time": f"{int(getattr(event, 'hour', 0)):02d}:00",
            "event_type": self._calendar_dashboard_event_type(getattr(event, "type", "")),
            "title": self._calendar_dashboard_event_title(event, engine),
            "processed": False,
            "requires_user_attention": self._calendar_dashboard_event_requires_attention(event, engine),
        }

    def _calendar_dashboard_event_type(self, event_type: Any) -> str:
        mapping = {
            "InboxCheck": "inbox_check",
            "TrainingSlot": "training_session",
            "Travel": "travel",
            "GameKickoff": "game_kickoff",
            "GameWrap": "game_wrap",
            "PhaseChange": "phase_change",
            "TradeDeadline": "trade_deadline",
        }
        text = str(event_type or "")
        return mapping.get(text, text.strip().lower().replace(" ", "_"))

    def _calendar_dashboard_event_title(self, event: Any, engine: TimeEngine) -> str:
        event_type = str(getattr(event, "type", "") or "")
        payload = getattr(event, "payload", {}) or {}
        if event_type == "TrainingSlot":
            return "Team Training"
        if event_type == "InboxCheck":
            return "Inbox Review"
        if event_type == "Travel":
            return "Team Travel"
        if event_type == "TradeDeadline":
            return "Trade Deadline"
        if event_type == "PhaseChange":
            return "Phase Change"
        if event_type == "GameWrap":
            return "Game Finalization"
        if event_type == "GameKickoff":
            home_id = payload.get("home_id")
            away_id = payload.get("away_id")
            user_team_id = engine.user_team_id or getattr(self.league, "user_team_id", None)
            opponent_id = away_id if user_team_id == home_id else home_id
            opponent_abbr = self._team_abbr(opponent_id) if opponent_id else ""
            return f"Kickoff vs {opponent_abbr}" if opponent_abbr else "Game Kickoff"
        return str(event_type or "League Event")

    def _calendar_dashboard_event_requires_attention(self, event: Any, engine: TimeEngine) -> bool:
        event_type = str(getattr(event, "type", "") or "")
        if event_type == "InboxCheck":
            team_id = None
            payload = getattr(event, "payload", {}) or {}
            if isinstance(payload, dict):
                team_id = payload.get("team_id")
            return engine.pending_ack_count(team_id) > 0
        return event_type in {"GameKickoff", "TradeDeadline", "PhaseChange"}

    def advance_hour(self) -> Dict[str, Any]:
        result = self._get_time_engine().advance_hour()
        payload = self.get_state()
        payload["time_engine_result"] = result
        return payload

    def _compact_advance_response(self, result: Dict[str, Any]) -> Dict[str, Any]:
        engine = self._get_time_engine()
        queue_events = engine.queue.events()
        next_event = None
        if queue_events:
            next_event = self._calendar_dashboard_event_dict(queue_events[0], engine)
        paused = bool(result.get("paused"))
        payload = {
            "status": "stopped_for_user_attention" if paused else "advanced",
            "paused": paused,
            "stop_reason": result.get("stop_reason"),
            "advanced_hours": result.get("advanced_hours", 0),
            "current_date": engine.clock.current_date.isoformat(),
            "current_time": engine.clock.current_time_str,
            "current_week": getattr(self.calendar, "current_week", None),
            "current_phase": getattr(self.calendar, "season_phase", None),
            "clock": result.get(
                "clock",
                {
                    "date": engine.clock.current_date.isoformat(),
                    "current_time": engine.clock.current_time_str,
                    "hour": engine.clock.hour,
                },
            ),
            "processed_events": result.get("processed_events", []),
            "new_notifications": result.get("new_notifications", []),
            "stopped_for_user_attention": paused,
            "next_event": next_event,
        }
        if result.get("debug_game_events") is not None:
            payload["debug_game_events"] = result.get("debug_game_events")
        if result.get("blocking_decisions") is not None:
            payload["blocking_decisions"] = result.get("blocking_decisions")
            payload["blocking_decision_count"] = len(result.get("blocking_decisions") or [])
        return payload

    def _compact_continue_once_response(self, result: Dict[str, Any]) -> Dict[str, Any]:
        payload = self._compact_advance_response(result)
        stop_reason = str(result.get("stop_reason") or "")
        if stop_reason == "max_reached":
            payload["status"] = "advanced"
            payload["paused"] = True
            payload["stopped_for_user_attention"] = False
            payload["stop_reason"] = "no_events_available"
            return payload
        if stop_reason == "blocking_decision":
            payload["status"] = "stopped_for_user_attention"
            payload["paused"] = True
            payload["stopped_for_user_attention"] = True
            return payload
        if stop_reason != "user_attention_required":
            payload["status"] = "advanced"
            payload["paused"] = True
            payload["stopped_for_user_attention"] = False
        return payload

    def continue_until_pause(
        self,
        max_hours: int = 168,
        *,
        max_days: int | None = None,
        compact: bool = False,
    ) -> Dict[str, Any]:
        if compact or max_days is not None:
            resolved_max_days = max_days if max_days is not None else max_hours
            return self._continue_until_pause_compact(resolved_max_days)
        result = self._get_time_engine().continue_until_pause(max_hours=max_hours)
        return self._compact_advance_response(result)

    def continue_once(self, max_hours: int = 336) -> Dict[str, Any]:
        result = self._get_time_engine().continue_once(max_hours=max_hours)
        return self._compact_continue_once_response(result)

    def _continue_until_pause_compact(self, max_days: int = 14) -> Dict[str, Any]:
        if not self.has_active_game():
            return self._compact_continue_result(
                advanced=False,
                stop_reason="no_active_league",
                days_advanced=0,
                events_processed=[],
            )

        try:
            max_days = int(max_days)
        except (TypeError, ValueError):
            max_days = 14
        max_days = max(1, max_days)

        self._continue_in_progress = True
        self._continue_stop_requested = False
        try:
            current_pause = self._compact_continue_pause_event_for_current_state()
            if current_pause is not None:
                return self._compact_continue_result(
                    advanced=False,
                    stop_reason=str(current_pause.get("type") or "game_day"),
                    days_advanced=0,
                    events_processed=[current_pause],
                )

            events_processed: list[Dict[str, Any]] = []
            days_advanced = 0

            for _ in range(max_days):
                if self._continue_stop_requested:
                    stop_event = self._compact_continue_event(
                        "user_stop_requested",
                        "Stop requested",
                        True,
                    )
                    events_processed.append(stop_event)
                    return self._compact_continue_result(
                        advanced=days_advanced > 0,
                        stop_reason="user_stop_requested",
                        days_advanced=days_advanced,
                        events_processed=events_processed,
                    )

                previous_week = self._safe_int(getattr(self.calendar, "current_week", 0), 0)
                previous_phase = str(getattr(self.calendar, "season_phase", "") or "")

                if hasattr(self.calendar, "advance_day"):
                    self.calendar.advance_day()
                if hasattr(self.calendar, "current_time_str"):
                    self.calendar.current_time_str = "00:00"
                self._sync_time_engine_clock_from_calendar()

                days_advanced += 1
                day_name = str(getattr(self.calendar, "day_of_week", "") or "")
                events_processed.append(
                    self._compact_continue_event(
                        "calendar_day",
                        f"Advanced to {day_name}" if day_name else "Advanced one day",
                        False,
                    )
                )

                current_week = self._safe_int(getattr(self.calendar, "current_week", 0), 0)
                current_phase = str(getattr(self.calendar, "season_phase", "") or "")
                if current_week != previous_week or current_phase != previous_phase:
                    phase_title = self._compact_continue_phase_change_title(previous_phase, current_phase)
                    events_processed.append(
                        self._compact_continue_event(
                            "season_phase_changed",
                            phase_title,
                            True,
                        )
                    )
                    return self._compact_continue_result(
                        advanced=True,
                        stop_reason="season_phase_changed",
                        days_advanced=days_advanced,
                        events_processed=events_processed,
                    )

                roster_issue = self._compact_continue_roster_pause_event()
                if roster_issue is not None:
                    events_processed.append(roster_issue)
                    return self._compact_continue_result(
                        advanced=True,
                        stop_reason=str(roster_issue.get("type") or "roster_invalid"),
                        days_advanced=days_advanced,
                        events_processed=events_processed,
                    )

                game_day_event = self._compact_continue_game_day_event()
                if game_day_event is not None:
                    events_processed.append(game_day_event)
                    return self._compact_continue_result(
                        advanced=True,
                        stop_reason="game_day",
                        days_advanced=days_advanced,
                        events_processed=events_processed,
                    )

            events_processed.append(
                self._compact_continue_event(
                    "max_days_reached",
                    "Max days reached",
                    True,
                )
            )
            return self._compact_continue_result(
                advanced=days_advanced > 0,
                stop_reason="max_days_reached",
                days_advanced=days_advanced,
                events_processed=events_processed,
            )
        finally:
            self._continue_in_progress = False
            self._continue_stop_requested = False

    def _compact_continue_result(
        self,
        *,
        advanced: bool,
        stop_reason: str,
        days_advanced: int,
        events_processed: list[Dict[str, Any]],
    ) -> Dict[str, Any]:
        payload = {
            "advanced": bool(advanced),
            "stop_reason": str(stop_reason or ""),
            "days_advanced": self._safe_int(days_advanced, 0),
            "events_processed": list(events_processed or []),
        }
        self._last_continue_result = self._snapshot_continue_result(payload)
        return payload

    def _snapshot_continue_result(self, result: Dict[str, Any]) -> Dict[str, Any]:
        payload = result if isinstance(result, dict) else {}
        calendar_payload = self.calendar.serialize() if hasattr(self.calendar, "serialize") else {}
        compact_events = []
        for event in payload.get("events_processed", []):
            if not isinstance(event, dict):
                continue
            compact_events.append(
                {
                    "type": str(event.get("type") or ""),
                    "title": str(event.get("title") or ""),
                    "requires_user_action": bool(event.get("requires_user_action")),
                }
            )
        stopped_at = {
            "current_date": str(calendar_payload.get("current_date") or ""),
            "year": self._safe_int(calendar_payload.get("season_year", calendar_payload.get("current_year")), 0),
            "week": self._safe_int(calendar_payload.get("football_week", calendar_payload.get("current_week")), 0),
            "phase": str(calendar_payload.get("season_phase") or calendar_payload.get("phase_label") or ""),
        }
        return {
            "stop_reason": str(payload.get("stop_reason") or ""),
            "days_advanced": self._safe_int(payload.get("days_advanced"), 0),
            "events_processed": compact_events,
            "stopped_at": stopped_at,
        }

    def _compact_continue_event(
        self,
        event_type: str,
        title: str,
        requires_user_action: bool,
    ) -> Dict[str, Any]:
        return {
            "type": str(event_type or ""),
            "title": str(title or ""),
            "requires_user_action": bool(requires_user_action),
        }

    def _compact_continue_pause_event_for_current_state(self) -> Dict[str, Any] | None:
        roster_issue = self._compact_continue_roster_pause_event()
        if roster_issue is not None:
            return roster_issue
        depth_chart_issue = self._compact_continue_depth_chart_pause_event()
        if depth_chart_issue is not None:
            return depth_chart_issue
        return self._compact_continue_game_day_event()

    def _compact_continue_roster_pause_event(self) -> Dict[str, Any] | None:
        team = self._compact_continue_user_team()
        if team is None:
            return None

        active_roster = list(getattr(team, "roster", []) or [])
        roster_limit = getattr(team, "MAX_ROSTER_SIZE", None)
        if isinstance(roster_limit, int) and roster_limit > 0 and len(active_roster) > roster_limit:
            return self._compact_continue_event(
                "roster_invalid",
                f"Roster invalid for {self._team_abbr(getattr(team, 'id', None)) or 'user team'}",
                True,
            )

        user_team_id = getattr(team, "id", None)
        if not user_team_id or self.league is None or self._compact_continue_current_user_game() is None:
            return None
        roster_issues = get_roster_rule_violations(
            self.league,
            str(user_team_id),
            context={"check_type": "game_day", "game_day_check": True},
        )
        if roster_issues:
            return self._compact_continue_event(
                "roster_invalid",
                str(roster_issues[0].get("title") or "Roster invalid"),
                True,
            )
        return None

    def _compact_continue_depth_chart_pause_event(self) -> Dict[str, Any] | None:
        team = self._compact_continue_user_team()
        if team is None:
            return None

        depth_chart_status = self._team_depth_chart_status(team)
        if depth_chart_status.get("is_valid", True):
            return None

        return self._compact_continue_event(
            "depth_chart_invalid",
            f"Depth chart invalid for {self._team_abbr(getattr(team, 'id', None)) or 'user team'}",
            True,
        )

    def _compact_continue_user_team(self) -> Any:
        if self.league is None:
            return None
        team_id = getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
        if team_id and hasattr(self.league, "id_to_team"):
            return self.league.id_to_team.get(team_id)
        teams = getattr(self.league, "teams", []) or []
        return teams[0] if teams else None

    def _compact_continue_game_day_event(self) -> Dict[str, Any] | None:
        user_game = self._compact_continue_current_user_game()
        if not isinstance(user_game, dict):
            return None
        opponent = str(user_game.get("opponent_abbreviation") or user_game.get("opponent") or "").strip()
        title = "Game day reached"
        if opponent:
            title = f"Game day reached vs {opponent}"
        return self._compact_continue_event("game_day", title, True)

    def _compact_continue_current_user_game(self) -> Dict[str, Any] | None:
        schedule_context = self.get_schedule_context()
        user_game_today = schedule_context.get("user_team_game_today")
        if isinstance(user_game_today, dict):
            return self._compact_continue_user_game_payload(user_game_today)

        current_week = str(getattr(self.calendar, "current_week", "") or "")
        team = self._compact_continue_user_team()
        user_team_id = str(getattr(team, "id", "") or "")
        if not current_week or not user_team_id:
            return None

        schedule_by_week = self._get_schedule_by_week()
        week_games = schedule_by_week.get(current_week, [])
        if not isinstance(week_games, list):
            return None

        results_by_week = self._get_results_by_week()
        current_date = getattr(self.calendar, "current_date", None)
        current_day = self._normalize_day_name(
            current_date.strftime("%A") if isinstance(current_date, datetime.date) else None
        )
        for game in week_games:
            if not isinstance(game, dict):
                continue
            home_id = str(game.get("home_id") or "")
            away_id = str(game.get("away_id") or "")
            if user_team_id not in {home_id, away_id}:
                continue
            game_id = make_game_id(current_week, home_id, away_id)
            if self._find_result_for_game(current_week, home_id, away_id, game_id, results_by_week):
                continue
            explicit_date = str(game.get("date") or "").strip()
            if explicit_date:
                if isinstance(current_date, datetime.date) and explicit_date != current_date.isoformat():
                    continue
            else:
                game_day = self._normalize_day_name(game.get("day"))
                if game_day and current_day and game_day != current_day:
                    continue
            return self._compact_continue_user_game_payload(game)
        return None

    def _compact_continue_user_game_payload(self, game: Dict[str, Any]) -> Dict[str, Any]:
        team = self._compact_continue_user_team()
        user_team_id = str(getattr(team, "id", "") or "")
        home_id = str(game.get("home_id") or "")
        away_id = str(game.get("away_id") or "")
        opponent_id = away_id if user_team_id == home_id else home_id
        game_id = str(game.get("game_id") or "")
        if not game_id and home_id and away_id:
            game_id = make_game_id(str(getattr(self.calendar, "current_week", "") or ""), home_id, away_id)
        return {
            "game_id": game_id,
            "opponent": self._dashboard_team_name(self._dashboard_team(opponent_id)),
            "opponent_abbreviation": self._team_abbr(opponent_id),
            "home_away": "vs" if user_team_id == home_id else "@",
        }

    def _compact_continue_phase_change_title(self, previous_phase: str, current_phase: str) -> str:
        previous_label = Calendar.phase_label(previous_phase) if previous_phase else ""
        current_label = Calendar.phase_label(current_phase) if current_phase else ""
        if previous_label and current_label and previous_label != current_label:
            return f"Season phase changed to {current_label}"
        if current_label:
            return f"Season phase changed to {current_label}"
        return "Season phase changed"

    def _sync_time_engine_clock_from_calendar(self) -> None:
        if self._time_engine is None or self.calendar is None:
            return
        current_date = getattr(self.calendar, "current_date", None)
        if isinstance(current_date, datetime.date):
            self._time_engine.clock.current_date = current_date
        self._time_engine.clock.hour = parse_clock_hour(
            getattr(self.calendar, "current_time_str", self._time_engine.clock.current_time_str)
        )

    def advance_to_next_event(self, max_hours: int = 336) -> Dict[str, Any]:
        result = self._get_time_engine().advance_to_next_event(max_hours=max_hours)
        return self._compact_advance_response(result)

    def advance_to_end_of_day(self, max_hours: int = 48) -> Dict[str, Any]:
        result = self._get_time_engine().advance_to_end_of_day(max_hours=max_hours)
        return self._compact_advance_response(result)

    def advance_one_week(self, max_hours: int = 24 * 8) -> Dict[str, Any]:
        result = self._get_time_engine().advance_one_week(max_hours=max_hours)
        return self._compact_advance_response(result)

    def advance_to_milestone(
        self,
        target_type: str | None = None,
        target_week: int | str | None = None,
        target_value: Any | None = None,
        max_hours: int = 24 * 365,
    ) -> Dict[str, Any]:
        self._ensure_game()
        target = self._resolve_sim_until_target(target_type, target_week, target_value)
        if not target.get("ok"):
            return target
        try:
            max_hours = int(max_hours)
        except (TypeError, ValueError):
            return {"ok": False, "error": "invalid_max_hours"}
        if max_hours <= 0:
            return {"ok": False, "error": "invalid_max_hours"}
        target_week_abs = int(target["target_week"])
        if self._sim_until_target_is_behind(target_week_abs):
            return {"ok": False, "error": "target_behind_current_state"}
        week_start = self._week_start_date(target_week_abs)
        if week_start is None:
            return {"ok": False, "error": "invalid_target_week"}
        target_datetime = datetime.datetime.combine(week_start, datetime.time(0, 0))
        result = self._get_time_engine().advance_to_timestamp(
            target_datetime,
            stop_reason_on_target=str(target.get("target_label") or "milestone_reached").lower().replace(" ", "_"),
            max_hours=max_hours,
        )
        payload = self.get_state()
        payload["time_engine_result"] = result
        payload["milestone"] = {
            "target_type": target_type,
            "target_week": target_week_abs,
            "target_label": target.get("target_label"),
            "target_date": week_start.isoformat(),
        }
        return payload

    def sim_until(
        self,
        target_type: str | None = None,
        target_week: int | str | None = None,
        target_value: Any | None = None,
        max_iterations: int = 10000,
    ) -> Dict[str, Any]:
        self._ensure_game()
        target = self._resolve_sim_until_target(target_type, target_week, target_value)
        if not target.get("ok"):
            return target
        try:
            max_iterations = int(max_iterations)
        except (TypeError, ValueError):
            return {"ok": False, "error": "invalid_max_iterations"}
        if max_iterations <= 0:
            return {"ok": False, "error": "invalid_max_iterations"}

        target_week_abs = int(target["target_week"])
        if self._sim_until_target_is_behind(target_week_abs):
            return {"ok": False, "error": "target_behind_current_state"}

        before_results = self._count_results()
        if self._is_at_sim_until_target(target_week_abs):
            return {
                "ok": True,
                "target_reached": True,
                "stopped_at": self._calendar_stop_payload(),
                "games_simulated": 0,
                "results_added": 0,
            }

        engine = self._get_time_engine()
        iterations = 0
        simulated_game_ids: set[str] = set()
        while not self._is_at_sim_until_target(target_week_abs):
            if engine.has_blocking_decisions():
                return {
                    "ok": True,
                    "target_reached": False,
                    "status": "stopped_for_user_attention",
                    "stop_reason": "blocking_decision",
                    "blocking_decision_count": len(engine.get_blocking_decisions()),
                    "blocking_decisions": [self._compact_decision_payload(item) for item in engine.get_blocking_decisions()],
                    "stopped_at": self._calendar_stop_payload(),
                    "games_simulated": len(simulated_game_ids),
                    "results_added": max(0, self._count_results() - before_results),
                }
            if iterations >= max_iterations:
                return {
                    "ok": False,
                    "error": "max_iterations_reached",
                    "target_reached": False,
                    "stopped_at": self._calendar_stop_payload(),
                    "games_simulated": len(simulated_game_ids),
                    "results_added": max(0, self._count_results() - before_results),
                }

            result = engine.advance_hour()
            iterations += 1
            for event in result.get("processed", []):
                if not isinstance(event, dict) or event.get("type") != "GameKickoff":
                    continue
                payload = event.get("payload") or {}
                game_id = payload.get("game_id")
                if not game_id:
                    continue
                game = engine._resolve_game_by_id(str(game_id))
                if game and engine._is_user_game(game):
                    game_result = engine.simulate_scheduled_game(str(game_id))
                    if game_result.get("already_simmed") is not True and not game_result.get("error"):
                        simulated_game_ids.add(str(game_id))
            self._ensure_playoffs_generated_if_ready()

        after_results = self._count_results()
        return {
            "ok": True,
            "target_reached": True,
            "stopped_at": self._calendar_stop_payload(),
            "games_simulated": after_results - before_results,
            "results_added": after_results - before_results,
        }

    def simulate_user_game(self, game_id: str) -> Dict[str, Any]:
        result = self._get_time_engine().simulate_user_game(game_id)
        if isinstance(result, dict) and result.get("error"):
            return {
                "ok": False,
                "error": str(result.get("error")),
                "result": self._compact_sim_result(result),
            }
        return {
            "ok": True,
            "result": self._compact_sim_result(result),
        }

    def _compact_sim_result(self, result: Dict[str, Any] | None) -> Dict[str, Any]:
        payload = result if isinstance(result, dict) else {}
        home_id = payload.get("home_id") or payload.get("home")
        away_id = payload.get("away_id") or payload.get("away")
        home_team = self._team_abbr(home_id) if home_id else "Unknown home team"
        away_team = self._team_abbr(away_id) if away_id else "Unknown away team"
        winner_id = payload.get("winner_id")
        if winner_id:
            winner = self._team_abbr(winner_id)
        else:
            winner = payload.get("winner")
        summary = str(payload.get("summary") or payload.get("summary_text") or "").strip()
        if not summary:
            if winner and winner not in {"None", ""}:
                loser = away_team if winner == home_team else home_team
                home_score = payload.get("home_score")
                away_score = payload.get("away_score")
                if home_score is not None and away_score is not None:
                    summary = f"{winner} defeated {loser}, {home_score}-{away_score}."
            if not summary:
                summary = "Game complete."

        compact = {
            "game_id": str(payload.get("game_id") or ""),
            "week": self._safe_optional_int(
                payload.get("season_week", payload.get("calendar_week", payload.get("week")))
            ),
            "game_type": str(
                payload.get("season_type")
                or payload.get("season_phase")
                or ""
            ).strip().lower(),
            "home_team": home_team,
            "away_team": away_team,
            "home_score": payload.get("home_score"),
            "away_score": payload.get("away_score"),
            "winner": str(winner or ""),
            "summary": summary,
        }
        raw_box_score = payload.get("box_score")
        if raw_box_score is not None:
            compact["box_score"] = sanitize_box_score_numbers(raw_box_score)
        else:
            compact["box_score"] = self._build_minimal_box_score(payload)
        if payload.get("already_simmed") is True:
            compact["already_simmed"] = True
        return compact

    def _resolve_sim_until_target(
        self,
        target_type: str | None,
        target_week: int | str | None,
        target_value: Any | None,
    ) -> Dict[str, Any]:
        target_type_norm = str(target_type or "").strip().lower()
        if target_type_norm == "regular_season_week":
            week_value = target_week if target_week is not None else target_value
            try:
                regular_week = int(week_value)
            except (TypeError, ValueError):
                return {"ok": False, "error": "invalid_target_week"}
            if regular_week < 1 or regular_week > REGULAR_SEASON_WEEKS:
                return {"ok": False, "error": "invalid_target_week"}
            return {
                "ok": True,
                "target_week": REGULAR_SEASON_START_WEEK + regular_week - 1,
                "target_label": f"Regular Season Week {regular_week}",
            }
        if target_type_norm == "playoffs_start":
            return {
                "ok": True,
                "target_week": self.calendar.phase_boundaries[self.calendar.PHASE_PLAYOFFS][0],
                "target_label": "Playoffs",
            }
        if target_type_norm == "offseason_start":
            return {
                "ok": True,
                "target_week": self.calendar.phase_boundaries[self.calendar.PHASE_OFFSEASON][0],
                "target_label": "Offseason",
            }
        return {"ok": False, "error": "invalid_target_type"}

    def _is_at_sim_until_target(self, target_week: int) -> bool:
        engine = self._get_time_engine()
        return (
            int(getattr(self.calendar, "current_week", 0)) == int(target_week)
            and int(getattr(self.calendar, "current_day_index", -1)) == 0
            and int(getattr(engine.clock, "hour", -1)) == 0
        )

    def _sim_until_target_is_behind(self, target_week: int) -> bool:
        current_week = int(getattr(self.calendar, "current_week", 0))
        if current_week > int(target_week):
            return True
        if current_week < int(target_week):
            return False
        return not self._is_at_sim_until_target(target_week)

    def _ensure_playoffs_generated_if_ready(self) -> None:
        if self.calendar is None or self.season_manager is None:
            return
        if not self.calendar.is_regular_season_over():
            return
        if getattr(self.season_manager, "playoffs_generated", False):
            return
        self.season_manager.generate_playoff_bracket_if_ready()
        if self._time_engine is not None:
            self._time_engine.schedule_by_week = self.season_manager.schedule_by_week
            self._time_engine.last_agenda_date = None
            self.league.last_agenda_date = None

    def _calendar_stop_payload(self) -> Dict[str, Any]:
        calendar_payload = self.calendar.serialize()
        return {
            "current_date": calendar_payload.get("current_date"),
            "current_time": calendar_payload.get("current_time"),
            "season_phase": calendar_payload.get("season_phase"),
            "football_week": calendar_payload.get("football_week"),
            "week_label": calendar_payload.get("week_label"),
        }

    def _count_results(self) -> int:
        results_by_week = self._get_results_by_week()
        count = 0
        for games in results_by_week.values():
            if isinstance(games, list):
                count += sum(1 for game in games if isinstance(game, dict))
            elif isinstance(games, dict):
                count += 1
        return count

    def _week_start_date(self, week: int | str) -> datetime.date | None:
        if self.calendar is None:
            return None
        try:
            week_int = int(week)
        except (TypeError, ValueError):
            return None
        base_date = getattr(self.calendar, "nfl_week1_start_date", None)
        if not isinstance(base_date, datetime.date):
            return None
        return base_date + datetime.timedelta(days=(week_int - 1) * 7)

    def get_inbox(self, team_id: str | None = None) -> Dict[str, Any]:
        engine = self._get_time_engine()
        messages = engine.get_inbox(team_id)[:20]
        notifications = [engine._notification_summary(msg) for msg in messages]
        open_decisions = [self._compact_decision_payload(item) for item in engine.get_open_decisions()]
        return {
            "ok": True,
            "unread_count": engine.unread_inbox_count(team_id),
            "blocking_decision_count": len(engine.get_blocking_decisions()),
            "notifications": notifications,
            "messages": notifications,
            "open_decisions": open_decisions,
        }

    def review_user_roster(self) -> Dict[str, Any]:
        self._ensure_game()
        team_id = getattr(self.league, "controlled_team_id", None) or getattr(self.league, "user_team_id", None)
        if not team_id:
            return {"ok": False, "error": "missing_team_id"}
        return self._get_time_engine().review_team_roster(
            team_id,
            include_advisories=True,
            game_day_check=False,
        )

    def mark_inbox_read(
        self,
        message_id: str,
        team_id: str | None = None,
        include_messages: bool = False,
    ) -> Dict[str, Any]:
        engine = self._get_time_engine()
        ok = engine.mark_read(message_id, team_id)
        unread_count = engine.unread_inbox_count(team_id)
        payload: Dict[str, Any] = {"ok": ok, "unread_count": unread_count, "unread": unread_count}
        if include_messages:
            messages = engine.get_inbox(team_id)
            payload["messages"] = [engine._notification_summary(msg) for msg in messages]
        return payload

    def acknowledge_inbox_notification(
        self,
        notification_id: str,
        team_id: str | None = None,
        include_messages: bool = False,
    ) -> Dict[str, Any]:
        engine = self._get_time_engine()
        ok = engine.acknowledge_notification(notification_id, team_id)
        unread_count = engine.unread_inbox_count(team_id)
        payload: Dict[str, Any] = {
            "ok": ok,
            "unread_count": unread_count,
            "blocking_decision_count": len(engine.get_blocking_decisions()),
        }
        if include_messages:
            messages = engine.get_inbox(team_id)
            payload["messages"] = [engine._notification_summary(msg) for msg in messages]
        return payload

    def mark_all_inbox_read(self, team_id: str | None = None) -> Dict[str, Any]:
        engine = self._get_time_engine()
        count = engine.mark_all_read(team_id)
        return {"ok": True, "marked": count, "unread": engine.unread_inbox_count(team_id)}

    def get_decisions(self, *, open_only: bool = True) -> Dict[str, Any]:
        engine = self._get_time_engine()
        decisions = engine.get_open_decisions() if open_only else list(engine.decisions)
        return {
            "ok": True,
            "blocking_decision_count": len(engine.get_blocking_decisions()),
            "decisions": [item.to_dict() for item in decisions],
        }

    def resolve_decision(self, decision_id: str, option_id: str) -> Dict[str, Any]:
        engine = self._get_time_engine()
        result = engine.resolve_decision(decision_id, option_id)
        if not result.get("ok"):
            return result
        result["open_decision_count"] = len(engine.get_open_decisions())
        return result

    def create_decision(
        self,
        *,
        category: str,
        decision_type: str,
        title: str,
        message: str,
        priority: int = 50,
        blocks_advancement: bool = False,
        options: list[Dict[str, Any]] | None = None,
        payload: Dict[str, Any] | None = None,
        linked_notification_id: str | None = None,
    ) -> Dict[str, Any]:
        engine = self._get_time_engine()
        decision = engine.create_decision(
            category=category,
            decision_type=decision_type,
            title=title,
            message=message,
            priority=priority,
            blocks_advancement=blocks_advancement,
            options=options,
            payload=payload,
            linked_notification_id=linked_notification_id,
        )
        return {"ok": True, "decision": decision.to_dict()}
