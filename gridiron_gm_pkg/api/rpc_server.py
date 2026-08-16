import argparse
import ctypes
from ctypes import wintypes
import errno
import json
import logging
import os
import sys
import threading
import time
import urllib.parse
from typing import Any, Dict, Tuple

from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.persistence.savegame import SCHEMA_VERSION
from gridiron_gm_pkg.api import schemas


logging.basicConfig(
    filename=os.environ.get("GRIDIRON_BACKEND_LOG", "backend.log"),
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)

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


class RpcServer:
    def __init__(self, save_path: str, parent_pid: int) -> None:
        self.facade = GameFacade()
        self.save_path = save_path
        self.parent_pid = parent_pid
        self.lock = threading.Lock()
        self._shutdown_requested = False
        self._out = sys.stdout
        sys.stdout = sys.stderr

    def _start_parent_watchdog(self) -> None:
        if self.parent_pid <= 0:
            return

        def watch() -> None:
            try:
                while is_pid_alive(self.parent_pid):
                    time.sleep(WATCHDOG_POLL_SECONDS)
            except Exception:
                logging.exception("Watchdog error")
                return
            logging.info("Parent process exited. Shutting down.")
            os._exit(0)

        threading.Thread(target=watch, daemon=True).start()

    def _maybe_autosave_locked(self) -> None:
        if not self.save_path:
            return
        try:
            self.facade.save(self.save_path)
        except Exception:
            logging.exception("Autosave failed: %s", self.save_path)

    def serve(self) -> None:
        self._start_parent_watchdog()
        while not self._shutdown_requested:
            try:
                line = sys.stdin.readline()
            except Exception:
                logging.exception("Failed to read stdin")
                break
            if line == "":
                break
            line = line.strip()
            if not line:
                continue
            response = self._handle_line(line)
            if response is None:
                continue
            try:
                self._out.write(response + "\n")
                self._out.flush()
            except BrokenPipeError:
                break
            except Exception:
                logging.exception("Failed to write response")
                break
            if self._shutdown_requested:
                break

    def _handle_line(self, line: str) -> str | None:
        try:
            request = json.loads(line)
        except json.JSONDecodeError:
            return self._format_response(
                None, False, 400, {"ok": False, "error": "invalid_json"}
            )
        except Exception as exc:
            logging.exception("Failed to parse request")
            return self._format_response(
                None,
                False,
                400,
                {"ok": False, "error": f"{type(exc).__name__}: {exc}"},
            )
        return self._handle_request(request)

    def _handle_request(self, request: Dict[str, Any]) -> str:
        req_id = request.get("id")
        op = request.get("op")
        args = request.get("args") or {}
        if not isinstance(args, dict):
            return self._format_response(
                req_id, False, 400, {"ok": False, "error": "invalid_args"}
            )
        method = str(op or "").upper()
        if method not in {"GET", "POST"}:
            return self._format_response(
                req_id, False, 400, {"ok": False, "error": "invalid_op"}
            )
        path = args.get("path")
        if not isinstance(path, str) or not path:
            return self._format_response(
                req_id, False, 400, {"ok": False, "error": "missing_path"}
            )

        json_body: Dict[str, Any] = {}
        if method == "POST":
            body = args.get("json")
            if body is None:
                json_body = {}
            elif isinstance(body, dict):
                json_body = body
            elif isinstance(body, str):
                if body.strip():
                    try:
                        json_body = json.loads(body)
                    except json.JSONDecodeError:
                        return self._format_response(
                            req_id, False, 400, {"ok": False, "error": "invalid_json"}
                        )
                else:
                    json_body = {}
            else:
                return self._format_response(
                    req_id, False, 400, {"ok": False, "error": "invalid_json"}
                )

        status, payload = self.dispatch(method, path, json_body)
        ok = 200 <= status < 300
        return self._format_response(req_id, ok, status, payload)

    def _format_response(self, req_id: Any, ok: bool, status: int, payload: Dict[str, Any]) -> str:
        try:
            body = json.dumps(payload)
        except Exception as exc:
            body = json.dumps({"ok": False, "error": f"{type(exc).__name__}: {exc}"})
            status = 500
            ok = False
        response = {"id": req_id, "ok": ok, "status": status, "body": body}
        return json.dumps(response)

    def _error(self, message: str, status: int = 400) -> Tuple[int, Dict[str, Any]]:
        return status, {"ok": False, "error": message}

    def _new_game_response(self, options: Dict[str, Any] | None = None) -> Tuple[int, Dict[str, Any]]:
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
                "save_name": os.path.basename(self.save_path) if self.save_path else "",
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

    def dispatch(self, method: str, path: str, json_body: Dict[str, Any]) -> Tuple[int, Dict[str, Any]]:
        try:
            parsed = urllib.parse.urlparse(path)
            path_only = parsed.path
            query = urllib.parse.parse_qs(parsed.query)

            if method == "GET":
                if path_only == "/health":
                    save_name = os.path.basename(self.save_path) if self.save_path else ""
                    return 200, {
                        "ok": True,
                        "schema_version": SCHEMA_VERSION,
                        "save_name": save_name,
                        "save_path": self.save_path,
                    }
                if path_only == "/state":
                    return 200, self.facade.get_state_snapshot(self.save_path)
                if path_only == "/state_summary":
                    raw = self.facade.get_state_summary()
                    calendar_obj = raw.get("calendar", {})
                    current_week = calendar_obj.get("football_week", calendar_obj.get("current_week", 1))
                    current_year = calendar_obj.get("season_year", calendar_obj.get("current_year", 2025))
                    sim_seed = getattr(self.facade.league, "sim_seed", 42) if self.facade.league else 42
                    state_summary_dict = {
                        "current_week": current_week,
                        "season": current_year,
                        "sim_seed": sim_seed,
                    }
                    try:
                        schemas.StateSummary.model_validate(state_summary_dict)
                    except Exception:
                        logging.exception("state_summary schema validation failed")
                        return self._error("schema_validation_error", 500)
                    return 200, raw
                if path_only == "/standings":
                    raw = self.facade.get_standings()
                    if isinstance(raw, dict) and raw.get("ok") is False:
                        return 200, raw
                    standings_rows = raw.get("standings", []) if isinstance(raw, dict) else []
                    try:
                        resp = schemas.StandingsResponse.model_validate({"ok": True, "standings": standings_rows})
                    except Exception:
                        logging.exception("standings schema validation failed")
                        return self._error("schema_validation_error", 500)
                    return 200, resp.model_dump()
                if path_only == "/results":
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
                    week_value = 1
                    if isinstance(raw, dict):
                        week_value = raw.get("week", week)
                    elif week is not None:
                        week_value = week
                    week_text = str(week_value).strip() if week_value is not None else ""
                    week_value = int(week_text) if week_text.isdigit() else 1
                    if isinstance(raw, dict):
                        games = raw.get("games", [])
                    elif isinstance(raw, list):
                        games = raw
                    else:
                        games = []
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
                            results_games.append(
                                {
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
                                }
                            )
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
                        resp = schemas.WeekResultsResponse.model_validate(
                            {
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
                            }
                        )
                    except Exception:
                        logging.exception("results schema validation failed")
                        return self._error("schema_validation_error", 500)
                    return 200, resp.model_dump()
                if path_only.startswith("/game/"):
                    parts = [p for p in path_only.split("/") if p]
                    if len(parts) == 2:
                        game_id = urllib.parse.unquote(parts[1])
                        return 200, self.facade.get_game(game_id)
                if path_only == "/team_schedule":
                    team_id = query.get("team_id", [None])[0]
                    raw = self.facade.get_team_schedule(team_id)
                    if isinstance(raw, dict) and raw.get("ok") is False:
                        return 200, raw
                    schedule_games = raw.get("schedule", []) if isinstance(raw, dict) else []
                    try:
                        resp = schemas.TeamScheduleResponse.model_validate(
                            {"ok": True, "schedule": schedule_games}
                        )
                    except Exception:
                        logging.exception("team_schedule schema validation failed")
                        return self._error("schema_validation_error", 500)
                    return 200, resp.model_dump()
                if path_only == "/injury_report":
                    team_id = query.get("team_id", [None])[0]
                    raw = self.facade.get_injury_report(team_id)
                    if isinstance(raw, dict) and raw.get("ok") is False:
                        return self._error(raw.get("error", "injury_report_failed"), 400)
                    resp_team_id = raw.get("team_id", team_id) if isinstance(raw, dict) else team_id
                    entries = raw.get("entries", []) if isinstance(raw, dict) else raw if isinstance(raw, list) else []
                    try:
                        resp = schemas.InjuryReportResponse.model_validate(
                            {"team_id": resp_team_id, "entries": entries}
                        )
                    except Exception:
                        logging.exception("injury_report schema validation failed")
                        return self._error("schema_validation_error", 500)
                    return 200, resp.model_dump()
                if path_only == "/team_roster":
                    raw = self.facade.get_team_roster()
                    if isinstance(raw, dict) and raw.get("ok") is False:
                        error = raw.get("error", "team_roster_failed")
                        return self._error(error, 200 if error == "No active league loaded." else (404 if error == "team_not_found" else 400))
                    return 200, raw
                if path_only.startswith("/team_roster/"):
                    parts = [p for p in path_only.split("/") if p]
                    if len(parts) == 2:
                        raw = self.facade.get_team_roster(parts[1])
                        if isinstance(raw, dict) and raw.get("ok") is False:
                            error = raw.get("error", "team_roster_failed")
                            return self._error(error, 200 if error == "No active league loaded." else (404 if error == "team_not_found" else 400))
                        return 200, raw
                if path_only.startswith("/team/"):
                    parts = [p for p in path_only.split("/") if p]
                    if len(parts) == 3 and parts[2] == "roster":
                        team_id = parts[1]
                        include_details = query.get("include_details", ["0"])[0] == "1"
                        return 200, self.facade._get_legacy_team_roster(team_id, include_details)
                    if len(parts) == 2:
                        team_id = parts[1]
                        return 200, self.facade.get_team(team_id)
                if path_only == "/inbox":
                    team_id = query.get("team_id", [None])[0]
                    return 200, self.facade.get_inbox(team_id)
                if path_only == "/decisions":
                    return 200, self.facade.get_decisions()
                if path_only == "/calendar_dashboard":
                    return 200, self.facade.get_calendar_dashboard()
                if path_only == "/dashboard_state":
                    return 200, self.facade.get_dashboard_state()
                if path_only == "/team_depth_chart":
                    team_id = query.get("team_id", [None])[0]
                    return 200, self.facade.get_team_depth_chart(team_id)
                if path_only == "/game_result":
                    game_id = query.get("game_id", [None])[0]
                    return 200, self.facade.get_game_result(game_id)
                if path_only == "/gm_profile":
                    return 200, self.facade.get_gm_profile()
                return self._error("not_found", 404)

            if method == "POST":
                if path_only == "/advance_day":
                    with self.lock:
                        resp = self.facade.advance_day()
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/advance_to_next_event":
                    max_hours = json_body.get("max_hours", 336)
                    with self.lock:
                        resp = self.facade.advance_to_next_event(max_hours)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/advance_to_end_of_day":
                    max_hours = json_body.get("max_hours", 48)
                    with self.lock:
                        resp = self.facade.advance_to_end_of_day(max_hours)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/advance_one_week":
                    max_hours = json_body.get("max_hours", 24 * 8)
                    with self.lock:
                        resp = self.facade.advance_one_week(max_hours)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/continue_until_pause":
                    max_hours = json_body.get("max_hours", 336)
                    mode = str(json_body.get("mode", "next_event") or "next_event").strip().lower()
                    with self.lock:
                        if mode == "until_pause":
                            resp = self.facade.continue_until_pause(max_hours)
                        else:
                            resp = self.facade.continue_once(max_hours)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/continue":
                    if not self.facade.has_active_game():
                        return 200, {"ok": False, "error": "No active league loaded."}
                    max_days = json_body.get("max_days", 14)
                    with self.lock:
                        resp = self.facade.continue_until_pause(max_days=max_days, compact=True)
                        self._maybe_autosave_locked()
                        return 200, {"ok": True, "result": resp}
                if path_only == "/auto_fill_depth_chart":
                    team_id = json_body.get("team_id")
                    with self.lock:
                        resp = self.facade.auto_fill_depth_chart(team_id)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/update_depth_chart":
                    with self.lock:
                        resp = self.facade.update_depth_chart(
                            json_body.get("position", ""),
                            json_body.get("player_id", ""),
                            json_body.get("action", ""),
                            json_body.get("team_id"),
                        )
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/stop_continue":
                    with self.lock:
                        resp = self.facade.request_continue_stop()
                        return 200, {"ok": True, "result": resp}
                if path_only == "/advance_to_milestone":
                    max_hours = json_body.get("max_hours", 24 * 365)
                    with self.lock:
                        resp = self.facade.advance_to_milestone(
                            json_body.get("target_type"),
                            json_body.get("target_week"),
                            json_body.get("target_value"),
                            max_hours=max_hours,
                        )
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/sim_until":
                    max_iterations = json_body.get("max_iterations", 10000)
                    with self.lock:
                        resp = self.facade.sim_until(
                            json_body.get("target_type"),
                            json_body.get("target_week"),
                            json_body.get("target_value"),
                            max_iterations=max_iterations,
                        )
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/simulate_user_game":
                    game_id = json_body.get("game_id")
                    if not game_id:
                        return self._error("missing_game_id", 400)
                    with self.lock:
                        resp = self.facade.simulate_user_game(game_id)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/inbox/mark_read":
                    message_id = json_body.get("notification_id", json_body.get("message_id"))
                    team_id = json_body.get("team_id")
                    include_messages = json_body.get("include_messages", False)
                    if isinstance(include_messages, str):
                        include_messages = (
                            include_messages.strip().lower() in {"1", "true", "yes", "y", "on"}
                        )
                    if message_id is None:
                        return self._error("missing_notification_id", 400)
                    with self.lock:
                        resp = self.facade.mark_inbox_read(
                            message_id, team_id, include_messages=bool(include_messages)
                        )
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/inbox/acknowledge":
                    notification_id = json_body.get("notification_id")
                    team_id = json_body.get("team_id")
                    include_messages = json_body.get("include_messages", False)
                    if isinstance(include_messages, str):
                        include_messages = (
                            include_messages.strip().lower() in {"1", "true", "yes", "y", "on"}
                        )
                    if notification_id is None:
                        return self._error("missing_notification_id", 400)
                    with self.lock:
                        resp = self.facade.acknowledge_inbox_notification(
                            notification_id,
                            team_id,
                            include_messages=bool(include_messages),
                        )
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/new_game":
                    with self.lock:
                        status, resp = self._new_game_response(json_body)
                        self._maybe_autosave_locked()
                        return status, resp
                if path_only == "/roster/review":
                    with self.lock:
                        resp = self.facade.review_user_roster()
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/decisions/resolve":
                    decision_id = json_body.get("decision_id")
                    option_id = json_body.get("option_id")
                    if not decision_id:
                        return self._error("missing_decision_id", 400)
                    if not option_id:
                        return self._error("missing_option_id", 400)
                    with self.lock:
                        resp = self.facade.resolve_decision(decision_id, option_id)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/set_user_team":
                    team_id = json_body.get("team_id")
                    if not team_id:
                        return self._error("missing_team_id", 400)
                    with self.lock:
                        resp = self.facade.set_user_team(team_id)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/reset_save":
                    with self.lock:
                        resp = self.facade.reset_save(self.save_path)
                        self._maybe_autosave_locked()
                        return 200, resp
                if path_only == "/shutdown":
                    self._shutdown_requested = True
                    return 200, {"ok": True, "message": "shutting_down"}
                return self._error("not_found", 404)

            return self._error("invalid_method", 400)
        except KeyError as exc:
            return self._error(str(exc), 404)
        except Exception as exc:
            logging.exception("Unhandled exception during request")
            error = f"{type(exc).__name__}: {exc}" if str(exc) else type(exc).__name__
            return self._error(error, 500)


def _parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Gridiron GM local RPC server")
    parser.add_argument("--save-path", default="./savegame.json")
    parser.add_argument("--parent-pid", type=int, default=0)
    return parser.parse_args()


def run(save_path: str, parent_pid: int = 0) -> None:
    server = RpcServer(save_path=save_path, parent_pid=parent_pid)
    logging.info(
        "Starting RPC backend; repo_root=%s cwd=%s save_path=%s",
        os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")),
        os.getcwd(),
        save_path,
    )
    if save_path and os.path.isfile(save_path):
        try:
            logging.info("Auto-loading savegame: %s", save_path)
            server.facade.load(save_path)
        except Exception:
            logging.exception("Auto-load failed; starting new game")
            try:
                server.facade.new_game()
            except Exception:
                logging.exception("Fallback new_game failed; will rely on lazy init")
    else:
        logging.info("No savegame found at %s; starting fresh", save_path)
    server.serve()


if __name__ == "__main__":
    args = _parse_args()
    run(args.save_path, args.parent_pid)
