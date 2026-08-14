import argparse
import ctypes
from ctypes import wintypes
import errno
import json
import os
import threading
import time
import urllib.parse
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from typing import Any, Dict
import logging
import traceback

from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.persistence.savegame import SCHEMA_VERSION
from gridiron_gm_pkg.api import schemas  # new: strict schemas

# configure basic structured logging if not already configured
logging.basicConfig(
    filename=os.environ.get("GRIDIRON_BACKEND_LOG", "backend.log"),
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)


class _Handler(BaseHTTPRequestHandler):
    facade: GameFacade = GameFacade()
    lock = threading.Lock()
    save_path: str = "./savegame.json"

    def _shutdown_server(self) -> None:
        print("[API] Shutdown requested.")

        def shutdown() -> None:
            try:
                self.server.shutdown()
            except Exception as exc:
                logging.exception("Shutdown error")

        threading.Thread(target=shutdown, daemon=True).start()

    def _send_json(self, status: int, payload: Dict[str, Any]) -> None:
        try:
            data = json.dumps(payload).encode("utf-8")
        except Exception as exc:
            error_payload = {"ok": False, "error": f"{type(exc).__name__}: {exc}"}
            data = json.dumps(error_payload).encode("utf-8")
            status = 500
        try:
            self.close_connection = True
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Connection", "close")
            self.send_header("Content-Length", str(len(data)))
            self.end_headers()
            self.wfile.write(data)
            self.wfile.flush()
        except Exception:
            logging.exception("Failed to send JSON response with status=%s", status)
            raise

    def _read_json(self) -> Dict[str, Any]:
        length = int(self.headers.get("Content-Length", "0"))
        if length <= 0:
            return {}
        raw = self.rfile.read(length)
        try:
            return json.loads(raw.decode("utf-8"))
        except json.JSONDecodeError:
            return {"__invalid_json__": True}

    def _read_optional_json_body(self) -> Dict[str, Any]:
        content_length = int(self.headers.get("Content-Length", "0"))
        if content_length <= 0:
            return {}
        return self._read_json()

    def _handle_exception(self, exc: Exception) -> None:
        logging.exception("Unhandled exception during request")
        error = f"{type(exc).__name__}: {exc}" if str(exc) else type(exc).__name__
        self._send_json(500, {"ok": False, "error": error})

    def _new_game_response(self, options: Dict[str, Any] | None = None) -> tuple[int, Dict[str, Any]]:
        try:
            options = options if isinstance(options, dict) else {}
            gm_name = options.get("gm_name")
            team_id = options.get("team_id")
            if gm_name is not None or team_id is not None:
                self.facade.new_game(gm_name=gm_name, team_id=team_id)
            else:
                self.facade.new_game()
            gm_payload = self.facade.get_gm_profile().get("gm", {})
            return 200, {
                "ok": True,
                "save_name": "savegame.json",
                "user": {
                    "gm_id": gm_payload.get("gm_id"),
                    "gm_name": gm_payload.get("name"),
                    "team_id": gm_payload.get("current_team_id"),
                },
                "state_summary": self.facade.get_state_summary(),
            }
        except Exception as exc:
            logging.exception("new_game failed")
            error = f"{type(exc).__name__}: {exc}" if str(exc) else type(exc).__name__
            return 500, {"ok": False, "error": error}

    def log_message(self, format: str, *args) -> None:
        return

    def do_GET(self) -> None:
        start = time.time()
        try:
            parsed = urllib.parse.urlparse(self.path)
            path = parsed.path
            query = urllib.parse.parse_qs(parsed.query)
            if path == "/state":
                self._send_json(200, self.facade.get_state_snapshot(self.save_path))
                return
            if path == "/state_summary":
                # Get the full state_summary from facade
                raw = self.facade.get_state_summary()
                # Ensure it has the required fields for StateSummary schema
                calendar_obj = raw.get("calendar", {})
                current_week = calendar_obj.get("football_week", calendar_obj.get("current_week", 1))
                current_year = calendar_obj.get("season_year", calendar_obj.get("current_year", 2025))
                sim_seed = getattr(self.facade.league, "sim_seed", 42) if self.facade.league else 42
                
                # Validate the core fields (but return the full payload for compatibility)
                state_summary_dict = {
                    "current_week": current_week,
                    "season": current_year,
                    "sim_seed": sim_seed,
                }
                try:
                    # Validate the schema
                    schemas.StateSummary.model_validate(state_summary_dict)
                except Exception:
                    logging.exception("state_summary schema validation failed")
                    self._send_json(500, {"ok": False, "error": "schema_validation_error"})
                    return
                # Return the full payload (for test compatibility and dashboard needs)
                self._send_json(200, raw)
                return
            if path == "/standings":
                raw = self.facade.get_standings()
                if isinstance(raw, dict) and raw.get("ok") is False:
                    self._send_json(200, raw)
                    return
                standings_rows = raw.get("standings", []) if isinstance(raw, dict) else []
                try:
                    resp = schemas.StandingsResponse.model_validate({"ok": True, "standings": standings_rows})
                except Exception:
                    logging.exception("standings schema validation failed")
                    self._send_json(500, {"ok": False, "error": "schema_validation_error"})
                    return
                self._send_json(200, resp.model_dump())
                return
            if path == "/results":
                week = query.get("week", [None])[0]
                season_type = query.get("season_type", [None])[0]
                season_week = query.get("season_week", [None])[0]
                week_key = query.get("week_key", [None])[0]
                raw = self.facade.get_results(
                    week,
                    season_type=season_type,
                    season_week=season_week,
                    week_key=week_key,
                )
                # Extract week and games from facade response
                week_value = 1
                if isinstance(raw, dict):
                    week_value = raw.get("week", week)
                elif week is not None:
                    week_value = week
                week_text = str(week_value).strip() if week_value is not None else ""
                week_value = int(week_text) if week_text.isdigit() else 1
                games = raw.get("games", []) if isinstance(raw, dict) else raw if isinstance(raw, list) else []
                
                results_games = []
                for game in games:
                    if isinstance(game, dict):
                        calendar_week = game.get("calendar_week")
                        if calendar_week is None:
                            calendar_week = week_value
                        season_type = game.get("season_type") or ""
                        season_week = game.get("season_week")
                        if season_week is None:
                            season_week = 0
                        results_games.append({
                            "game_id": game.get("game_id", ""),
                            "week": week_value,
                            "calendar_week": calendar_week,
                            "season_type": season_type,
                            "season_week": season_week,
                            "home_team": game.get("home_abbr", ""),
                            "away_team": game.get("away_abbr", ""),
                            "home_score": game.get("home_score"),
                            "away_score": game.get("away_score"),
                            "status": game.get("status", "scheduled"),
                        })
                def normalize_week_list(value):
                    if not isinstance(value, list):
                        return []
                    normalized = []
                    for item in value:
                        item_text = str(item).strip() if item is not None else ""
                        if item_text.isdigit():
                            normalized.append(int(item_text))
                    return normalized
                def normalize_key_list(value):
                    if not isinstance(value, list):
                        return []
                    normalized = []
                    for item in value:
                        item_text = str(item).strip() if item is not None else ""
                        if item_text:
                            normalized.append(item_text)
                    return normalized
                available_weeks = normalize_week_list(
                    raw.get("available_weeks", []) if isinstance(raw, dict) else []
                )
                completed_weeks = normalize_week_list(
                    raw.get("completed_weeks", []) if isinstance(raw, dict) else []
                )
                available_week_keys = normalize_key_list(
                    raw.get("available_week_keys", []) if isinstance(raw, dict) else []
                )
                completed_week_keys = normalize_key_list(
                    raw.get("completed_week_keys", []) if isinstance(raw, dict) else []
                )
                available_week_labels = normalize_key_list(
                    raw.get("available_week_labels", []) if isinstance(raw, dict) else []
                )
                completed_week_labels = normalize_key_list(
                    raw.get("completed_week_labels", []) if isinstance(raw, dict) else []
                )
                week_key_value = ""
                week_label = ""
                if isinstance(raw, dict):
                    week_key_value = str(raw.get("week_key") or "")
                    week_label = str(raw.get("week_label") or "")
                try:
                    resp = schemas.WeekResultsResponse.model_validate({
                        "week": week_value,
                        "week_key": week_key_value,
                        "week_label": week_label,
                        "games": results_games,
                        "available_week_keys": available_week_keys,
                        "available_week_labels": available_week_labels,
                        "completed_week_keys": completed_week_keys,
                        "completed_week_labels": completed_week_labels,
                        "available_weeks": available_weeks,
                        "completed_weeks": completed_weeks,
                    })
                except Exception:
                    logging.exception("results schema validation failed")
                    self._send_json(500, {"ok": False, "error": "schema_validation_error"})
                    return
                self._send_json(200, resp.model_dump())
                return
            if path.startswith("/game/"):
                parts = [p for p in path.split("/") if p]
                if len(parts) == 2:
                    game_id = urllib.parse.unquote(parts[1])
                    self._send_json(200, self.facade.get_game(game_id))
                    return
            if path == "/team_schedule":
                team_id = query.get("team_id", [None])[0]
                raw = self.facade.get_team_schedule(team_id)
                if isinstance(raw, dict) and raw.get("ok") is False:
                    self._send_json(200, raw)
                    return
                schedule_games = raw.get("schedule", []) if isinstance(raw, dict) else []
                try:
                    resp = schemas.TeamScheduleResponse.model_validate({
                        "ok": True,
                        "schedule": schedule_games,
                    })
                except Exception:
                    logging.exception("team_schedule schema validation failed")
                    self._send_json(500, {"ok": False, "error": "schema_validation_error"})
                    return
                self._send_json(200, resp.model_dump())
                return
            if path == "/injury_report":
                team_id = query.get("team_id", [None])[0]
                raw = self.facade.get_injury_report(team_id)
                if isinstance(raw, dict) and raw.get("ok") is False:
                    error = raw.get("error", "injury_report_failed")
                    self._send_json(400, {"ok": False, "error": error})
                    return
                resp_team_id = raw.get("team_id", team_id) if isinstance(raw, dict) else team_id
                entries = raw.get("entries", []) if isinstance(raw, dict) else raw if isinstance(raw, list) else []
                try:
                    resp = schemas.InjuryReportResponse.model_validate(
                        {"team_id": resp_team_id, "entries": entries}
                    )
                except Exception:
                    logging.exception("injury_report schema validation failed")
                    self._send_json(500, {"ok": False, "error": "schema_validation_error"})
                    return
                self._send_json(200, resp.model_dump())
                return
            if path == "/team_roster":
                raw = self.facade.get_team_roster()
                if isinstance(raw, dict) and raw.get("ok") is False:
                    error = raw.get("error", "team_roster_failed")
                    status = 200 if error == "No active league loaded." else (404 if error == "team_not_found" else 400)
                    self._send_json(status, {"ok": False, "error": error})
                    return
                self._send_json(200, raw)
                return
            if path.startswith("/team_roster/"):
                parts = [p for p in path.split("/") if p]
                if len(parts) == 2:
                    raw = self.facade.get_team_roster(parts[1])
                    if isinstance(raw, dict) and raw.get("ok") is False:
                        error = raw.get("error", "team_roster_failed")
                        status = 200 if error == "No active league loaded." else (404 if error == "team_not_found" else 400)
                        self._send_json(status, {"ok": False, "error": error})
                        return
                    self._send_json(200, raw)
                    return
            if path == "/next_user_game":
                raw = self.facade.get_next_user_game()
                # Extract game from facade response
                game = raw.get("game") if isinstance(raw, dict) else raw
                if game:
                    home_flag = game.get("home", True)
                    next_game_dict = {
                        "game_id": game.get("game_id"),
                        "week": game.get("week"),
                        "opponent_id": game.get("opponent_abbr"),
                        "home_away": "home" if home_flag else "away",
                    }
                else:
                    next_game_dict = {
                        "game_id": None,
                        "week": None,
                        "opponent_id": None,
                        "home_away": None,
                    }
                try:
                    resp = schemas.NextUserGameResponse.model_validate(next_game_dict)
                except Exception:
                    logging.exception("next_user_game schema validation failed")
                    self._send_json(500, {"ok": False, "error": "schema_validation_error"})
                    return
                self._send_json(200, resp.model_dump())
                return
            if path == "/health":
                save_name = os.path.basename(self.save_path) if self.save_path else ""
                self._send_json(
                    200,
                    {
                        "ok": True,
                        "schema_version": SCHEMA_VERSION,
                        "save_name": save_name,
                        "save_path": self.save_path,
                    },
                )
                return
            if path.startswith("/team/"):
                parts = [p for p in path.split("/") if p]
                if len(parts) == 2:
                    team_id = parts[1]
                    self._send_json(200, self.facade.get_team(team_id))
                    return
                if len(parts) == 3 and parts[2] == "roster":
                    team_id = parts[1]
                    include_details = query.get("include_details", ["0"])[0] == "1"
                    self._send_json(
                        200, self.facade._get_legacy_team_roster(team_id, include_details)
                    )
                    return
            if path == "/inbox":
                team_id = query.get("team_id", [None])[0]
                self._send_json(200, self.facade.get_inbox(team_id))
                return
            if path == "/decisions":
                self._send_json(200, self.facade.get_decisions())
                return
            if path == "/calendar_dashboard":
                self._send_json(200, self.facade.get_calendar_dashboard())
                return
            if path == "/dashboard_state":
                self._send_json(200, self.facade.get_dashboard_state())
                return
            if path == "/team_depth_chart":
                team_id = query.get("team_id", [None])[0]
                self._send_json(200, self.facade.get_team_depth_chart(team_id))
                return
            if path == "/game_result":
                game_id = query.get("game_id", [None])[0]
                self._send_json(200, self.facade.get_game_result(game_id))
                return
            if path == "/gm_profile":
                self._send_json(200, self.facade.get_gm_profile())
                return
            self._send_json(404, {"error": "not_found"})
        except KeyError as exc:
            self._send_json(404, {"error": str(exc)})
        except Exception as exc:
            self._handle_exception(exc)
        finally:
            duration = time.time() - start
            logging.info("GET %s completed in %.3fs", getattr(self, "path", ""), duration)

    def do_POST(self) -> None:
        start = time.time()
        try:
            if self.path == "/advance_day":
                with self.lock:
                    self._send_json(200, self.facade.advance_day())
                return
            if self.path == "/advance_to_next_event":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                max_hours = payload.get("max_hours", 336)
                with self.lock:
                    self._send_json(200, self.facade.advance_to_next_event(max_hours))
                return
            if self.path == "/advance_to_end_of_day":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                max_hours = payload.get("max_hours", 48)
                with self.lock:
                    self._send_json(200, self.facade.advance_to_end_of_day(max_hours))
                return
            if self.path == "/advance_one_week":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                max_hours = payload.get("max_hours", 24 * 8)
                with self.lock:
                    self._send_json(200, self.facade.advance_one_week(max_hours))
                return
            if self.path == "/advance_hour":
                with self.lock:
                    self._send_json(200, self.facade.advance_hour())
                return
            if self.path == "/advance_to_milestone":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                max_hours = payload.get("max_hours", 24 * 365)
                with self.lock:
                    self._send_json(
                        200,
                        self.facade.advance_to_milestone(
                            payload.get("target_type"),
                            payload.get("target_week"),
                            payload.get("target_value"),
                            max_hours=max_hours,
                        ),
                    )
                return
            if self.path == "/continue_until_pause":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                max_hours = payload.get("max_hours", 336)
                mode = str(payload.get("mode", "next_event") or "next_event").strip().lower()
                with self.lock:
                    if mode == "until_pause":
                        response = self.facade.continue_until_pause(max_hours)
                    else:
                        response = self.facade.continue_once(max_hours)
                    self._send_json(200, response)
                return
            if self.path == "/continue":
                payload = self._read_optional_json_body()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"ok": False, "error": "invalid_json"})
                    return
                if not self.facade.has_active_game():
                    self._send_json(200, {"ok": False, "error": "No active league loaded."})
                    return
                max_days = payload.get("max_days", 14)
                with self.lock:
                    response = self.facade.continue_until_pause(max_days=max_days, compact=True)
                    self._send_json(200, {"ok": True, "result": response})
                return
            if self.path == "/auto_fill_depth_chart":
                payload = self._read_optional_json_body()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"ok": False, "error": "invalid_json"})
                    return
                team_id = payload.get("team_id")
                with self.lock:
                    response = self.facade.auto_fill_depth_chart(team_id)
                    self._send_json(200, response)
                return
            if self.path == "/update_depth_chart":
                payload = self._read_optional_json_body()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"ok": False, "error": "invalid_json"})
                    return
                with self.lock:
                    response = self.facade.update_depth_chart(
                        payload.get("position", ""),
                        payload.get("player_id", ""),
                        payload.get("action", ""),
                        payload.get("team_id"),
                    )
                    self._send_json(200, response)
                return
            if self.path == "/stop_continue":
                _ = self._read_optional_json_body()
                with self.lock:
                    response = self.facade.request_continue_stop()
                    self._send_json(200, {"ok": True, "result": response})
                return
            if self.path == "/sim_until":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                max_iterations = payload.get("max_iterations", 10000)
                with self.lock:
                    self._send_json(
                        200,
                        self.facade.sim_until(
                            payload.get("target_type"),
                            payload.get("target_week"),
                            payload.get("target_value"),
                            max_iterations=max_iterations,
                        ),
                    )
                return
            if self.path == "/simulate_user_game":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                game_id = payload.get("game_id")
                if not game_id:
                    self._send_json(400, {"error": "missing_game_id"})
                    return
                with self.lock:
                    self._send_json(200, self.facade.simulate_user_game(game_id))
                return
            if self.path == "/inbox/mark_read":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                message_id = payload.get("notification_id", payload.get("message_id"))
                team_id = payload.get("team_id")
                include_messages = payload.get("include_messages", False)
                if isinstance(include_messages, str):
                    include_messages = include_messages.strip().lower() in {"1", "true", "yes", "y", "on"}
                if message_id is None:
                    self._send_json(400, {"error": "missing_notification_id"})
                    return
                with self.lock:
                    self._send_json(
                        200,
                        self.facade.mark_inbox_read(
                            message_id, team_id, include_messages=bool(include_messages)
                        ),
                    )
                return
            if self.path == "/inbox/acknowledge":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                notification_id = payload.get("notification_id")
                team_id = payload.get("team_id")
                include_messages = payload.get("include_messages", False)
                if isinstance(include_messages, str):
                    include_messages = include_messages.strip().lower() in {"1", "true", "yes", "y", "on"}
                if notification_id is None:
                    self._send_json(400, {"error": "missing_notification_id"})
                    return
                with self.lock:
                    self._send_json(
                        200,
                        self.facade.acknowledge_inbox_notification(
                            notification_id,
                            team_id,
                            include_messages=bool(include_messages),
                        ),
                    )
                return
            if self.path == "/inbox/mark_all_read":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                team_id = payload.get("team_id")
                with self.lock:
                    self._send_json(200, self.facade.mark_all_inbox_read(team_id))
                return
            if self.path == "/new_game":
                payload = self._read_optional_json_body()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"ok": False, "error": "invalid_json"})
                    return
                with self.lock:
                    status, response = self._new_game_response(payload)
                    self._send_json(status, response)
                return
            if self.path == "/roster/review":
                payload = self._read_optional_json_body()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"ok": False, "error": "invalid_json"})
                    return
                with self.lock:
                    self._send_json(200, self.facade.review_user_roster())
                return
            if self.path == "/decisions/resolve":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                decision_id = payload.get("decision_id")
                option_id = payload.get("option_id")
                if not decision_id:
                    self._send_json(400, {"error": "missing_decision_id"})
                    return
                if not option_id:
                    self._send_json(400, {"error": "missing_option_id"})
                    return
                with self.lock:
                    self._send_json(200, self.facade.resolve_decision(decision_id, option_id))
                return
            if self.path == "/set_user_team":
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                team_id = payload.get("team_id")
                if not team_id:
                    self._send_json(400, {"error": "missing_team_id"})
                    return
                with self.lock:
                    self._send_json(200, self.facade.set_user_team(team_id))
                return
            if self.path == "/reset_save":
                with self.lock:
                    self._send_json(200, self.facade.reset_save(self.save_path))
                return
            if self.path == "/shutdown":
                self._send_json(200, {"ok": True, "message": "shutting_down"})
                self._shutdown_server()
                return
            if self.path in ("/save", "/load"):
                content_length = int(self.headers.get("Content-Length", "0"))
                payload = self._read_json()
                if payload.get("__invalid_json__"):
                    self._send_json(400, {"error": "invalid_json"})
                    return
                path = payload.get("path")
                if not path and content_length > 0:
                    self._send_json(400, {"error": "missing_path"})
                    return
                if not path:
                    path = self.save_path
                with self.lock:
                    if self.path == "/save":
                        self._send_json(200, self.facade.save(path))
                    else:
                        self._send_json(200, self.facade.load(path))
                return
            self._send_json(404, {"error": "not_found"})
        except Exception as exc:
            self._handle_exception(exc)
        finally:
            duration = time.time() - start
            logging.info("POST %s completed in %.3fs", getattr(self, "path", ""), duration)


WATCHDOG_POLL_SECONDS = 0.5


def is_pid_alive(pid: int) -> bool:
    if pid <= 0:
        return True
    if os.name == "nt":
        return _is_pid_alive_windows(pid)
    return _is_pid_alive_posix(pid)


def _is_pid_alive_posix(pid: int) -> bool:
    try:
        os.kill(pid, 0)
    except ProcessLookupError:
        return False
    except PermissionError:
        return True
    except OSError as exc:
        if exc.errno == errno.ESRCH:
            return False
        if exc.errno == errno.EPERM:
            return True
        return False
    else:
        return True


def _is_pid_alive_windows(pid: int) -> bool:
    process_query_limited_information = 0x1000
    still_active = 259
    handle = ctypes.windll.kernel32.OpenProcess(
        process_query_limited_information, False, pid
    )
    if not handle:
        return False
    try:
        exit_code = wintypes.DWORD()
        if ctypes.windll.kernel32.GetExitCodeProcess(handle, ctypes.byref(exit_code)) == 0:
            return False
        return exit_code.value == still_active
    finally:
        ctypes.windll.kernel32.CloseHandle(handle)


def _start_parent_watchdog(server: ThreadingHTTPServer, parent_pid: int) -> None:
    if parent_pid <= 0:
        return

    def watch() -> None:
        try:
            while is_pid_alive(parent_pid):
                time.sleep(WATCHDOG_POLL_SECONDS)
        except Exception as exc:
            print(f"[API] Watchdog error: {exc}")
            return
        print("[API] Parent process exited. Shutting down.")
        try:
            server.shutdown()
        except Exception as exc:
            print(f"[API] Watchdog shutdown error: {exc}")

    threading.Thread(target=watch, daemon=True).start()


def make_server(
    host: str = "127.0.0.1",
    port: int = 8000,
    facade: GameFacade | None = None,
    save_path: str | None = None,
) -> ThreadingHTTPServer:
    if facade is not None:
        _Handler.facade = facade
    if save_path is not None:
        _Handler.save_path = save_path
    return ThreadingHTTPServer((host, port), _Handler)


def run(
    host: str = "127.0.0.1",
    port: int = 8000,
    facade: GameFacade | None = None,
    save_path: str | None = None,
    parent_pid: int = 0,
) -> None:
    server = make_server(host, port, facade, save_path)
    logging.info(
        "Starting backend; repo_root=%s cwd=%s save_path=%s host=%s port=%s",
        os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")),
        os.getcwd(),
        save_path,
        host,
        port,
    )
    _start_parent_watchdog(server, parent_pid)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.shutdown()
        server.server_close()


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Gridiron GM local API server")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--save-path", default="./savegame.json")
    parser.add_argument("--parent-pid", type=int, default=0)
    return parser.parse_args()


if __name__ == "__main__":
    args = _parse_args()
    run(args.host, args.port, save_path=args.save_path, parent_pid=args.parent_pid)
