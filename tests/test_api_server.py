import json
import threading
import urllib.request
from pathlib import Path
from urllib.error import HTTPError

from gridiron_gm_pkg.api.schemas import REQUIRED_STATE_KEYS, validate_state
from gridiron_gm_pkg.api.rpc_server import RpcServer
from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.career.decision_item import DecisionItem
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.systems.time_engine import InboxMessage, TimeEngine


class _DummySeason:
    schedule_by_week = {}
    results_by_week = {}


def _get_json(url: str) -> dict:
    with urllib.request.urlopen(url, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _post_json(url: str, payload: dict | None = None) -> dict:
    body = json.dumps(payload or {}).encode("utf-8")
    request = urllib.request.Request(
        url,
        data=body,
        headers={"Content-Type": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=10) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _find_depth_chart_group_with_multiple_players(positions: list[dict]) -> dict:
    for position_row in positions:
        players = position_row.get("players", [])
        if isinstance(players, list) and len(players) >= 2:
            return position_row
    raise AssertionError("expected a depth chart position group with at least two players")


def test_api_state_smoke():
    facade = GameFacade(save_name="unit_test_api_state_smoke")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        health = _get_json(f"http://{host}:{port}/health")
        assert health.get("ok") is True
        with urllib.request.urlopen(f"http://{host}:{port}/new_game", data=b"{}", timeout=10) as resp:
            new_game_payload = json.loads(resp.read().decode("utf-8"))
        assert new_game_payload.get("ok") is True
        assert new_game_payload.get("save_name") == "savegame.json"
        assert isinstance(new_game_payload.get("user"), dict)
        assert isinstance(new_game_payload.get("state_summary"), dict)
        payload = _get_json(f"http://{host}:{port}/state")
        ok, reason = validate_state(payload)
        assert ok, reason
        for key in REQUIRED_STATE_KEYS:
            assert key in payload
        assert payload["ok"] is True
        assert payload["save_name"] == "savegame.json"
        assert payload["save_path"] == "./savegame.json"
        calendar = payload["calendar"]
        for key in (
            "current_date",
            "current_time",
            "day_of_week",
            "season_year",
            "season_phase",
            "football_week",
            "current_year",
            "current_week",
            "phase_label",
            "week_label",
        ):
            assert key in calendar
        assert isinstance(calendar["current_date"], str)
        assert isinstance(calendar["current_time"], str)
        assert isinstance(calendar["day_of_week"], str)
        assert isinstance(calendar["season_year"], int)
        assert isinstance(calendar["season_phase"], str)
        assert isinstance(calendar["football_week"], int)
        assert calendar["phase_label"] == "Preseason"
        assert calendar["week_label"] == "Preseason Week 1"
        assert payload["user"]["gm_id"]
        assert payload["user"]["gm_name"] == "User GM"
        assert payload["user"]["team_id"]
        assert payload["user"]["current_role"] == "General Manager"
        assert payload["today"]["has_user_game"] is False
        assert payload["today"]["user_game"] is None
        assert isinstance(payload["today"]["league_games_count"], int)
        assert isinstance(payload["today"]["games"], list)
        assert isinstance(payload["standings_summary"], list)
        assert payload["standings_summary"]
        assert set(payload["standings_summary"][0].keys()) == {
            "team_id",
            "team_name",
            "wins",
            "losses",
            "ties",
            "pct",
        }
        assert set(payload["inbox_summary"].keys()) == {"unread_count", "blocking_decision_count", "latest"}
        assert payload["available_actions"] == ["continue", "advance_day", "sim_until"]
        assert "league" not in payload
        assert "time_engine" not in payload
        forbidden_json_keys = [
            '"players"',
            '"roster"',
            '"schedule_by_week"',
            '"results_by_week"',
            '"archive"',
            '"free_agents"',
            '"draft_prospects"',
        ]
        payload_json = json.dumps(payload)
        for forbidden_key in forbidden_json_keys:
            assert forbidden_key not in payload_json
        assert len(payload_json.encode("utf-8")) < 100_000
        for key in (
            "games_today",
            "next_game",
            "next_game_date",
            "next_game_label",
            "can_simulate_today",
            "user_team_game_today",
            "user_team_next_game",
            "user_team_can_simulate",
            "league_games_today_count",
        ):
            assert key in payload
        assert isinstance(payload["games_today"], list)
        assert isinstance(payload["can_simulate_today"], bool)
        assert isinstance(payload["user_team_can_simulate"], bool)
        assert isinstance(payload["league_games_today_count"], int)
        summary = _get_json(f"http://{host}:{port}/state_summary")
        summary_calendar = summary.get("calendar", {})
        assert summary_calendar.get("current_date")
        assert summary_calendar.get("day_of_week")
        assert summary_calendar.get("season_year")
        assert summary_calendar.get("season_phase")
        assert summary_calendar.get("football_week")
        assert summary_calendar.get("phase_label") == "Preseason"
        assert summary_calendar.get("week_label") == "Preseason Week 1"
        for key in (
            "games_today",
            "next_game",
            "next_game_date",
            "next_game_label",
            "can_simulate_today",
            "user_team_game_today",
            "user_team_next_game",
            "user_team_can_simulate",
            "league_games_today_count",
        ):
            assert key in summary
        summary_size = len(json.dumps(summary))
        state_size = len(payload_json)
        assert state_size < summary_size * 3
        teams = summary.get("league", {}).get("teams", [])
        assert teams
        team_id = teams[0]["id"]
        with urllib.request.urlopen(f"http://{host}:{port}/team/{team_id}/roster", timeout=10) as resp:
            roster_payload = json.loads(resp.read().decode("utf-8"))
        assert "ir_list" in roster_payload
        assert "practice_squad" in roster_payload
        assert len(roster_payload.get("roster", [])) == 53
    finally:
        server.shutdown()
        server.server_close()


def test_api_state_is_read_only_and_does_not_process_events():
    facade = GameFacade(save_name="unit_test_api_state_readonly")
    facade.new_game()
    engine = facade._get_time_engine()
    before_clock = (engine.clock.current_date, engine.clock.hour)
    before_queue_ids = [event.id for event in engine.queue.events()]
    before_results = facade._count_results()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/state")
        after_queue_ids = [event.id for event in engine.queue.events()]
        after_clock = (engine.clock.current_date, engine.clock.hour)
        assert payload["ok"] is True
        assert before_clock == after_clock
        assert before_queue_ids == after_queue_ids
        assert before_results == facade._count_results()
    finally:
        server.shutdown()
        server.server_close()


def test_team_roster_endpoint_returns_compact_grouped_snapshot():
    facade = GameFacade(save_name="unit_test_team_roster_snapshot")
    facade.new_game()
    user_team_id = facade.league.user_team_id
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/team_roster")
        direct_payload = _get_json(f"http://{host}:{port}/team_roster/{user_team_id}")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert payload["team"]["team_id"] == user_team_id
    assert direct_payload["team"]["team_id"] == user_team_id
    assert payload["roster_status"] == {
        "is_valid": True,
        "roster_size": 53,
        "roster_limit": 53,
        "required_cuts": 0,
        "open_slots": 0,
        "injured_count": 0,
        "issues": [],
    }
    assert payload["position_counts"]
    assert payload["players"]
    first_count = payload["position_counts"][0]
    assert set(first_count.keys()) == {"position", "count"}
    first_player = payload["players"][0]
    assert set(first_player.keys()) == {
        "player_id", "name", "position", "overall", "age", "status", "injury", "depth_role"
    }
    payload_json = json.dumps(payload)
    for forbidden_key in ('"league"', '"schedule_by_week"', '"results_by_week"', '"career_stats"', '"scout_report"'):
        assert forbidden_key not in payload_json


def test_team_roster_endpoint_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_team_roster_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/team_roster")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_standings_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_standings_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/standings")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_standings_returns_compact_payload_after_new_game():
    facade = GameFacade(save_name="unit_test_standings_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/standings")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "standings"}
    assert isinstance(payload["standings"], list)
    assert payload["standings"]
    first = payload["standings"][0]
    assert set(first.keys()) == {
        "team_id",
        "team_name",
        "abbreviation",
        "wins",
        "losses",
        "ties",
        "win_pct",
        "points_for",
        "points_against",
        "division",
        "conference",
    }
    payload_json = json.dumps(payload)
    for forbidden_key in ('"league"', '"teams"', '"players"', '"roster"', '"schedule_by_week"', '"results_by_week"'):
        assert forbidden_key not in payload_json


def test_preseason_game_does_not_change_regular_season_standings():
    facade = GameFacade(save_name="unit_test_standings_after_sim")
    facade.new_game()
    user_team_id = facade.league.user_team_id
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        continue_payload = _post_json(f"http://{host}:{port}/continue", {"max_days": 14})
        assert continue_payload["ok"] is True
        assert continue_payload["result"]["stop_reason"] == "game_day"

        dashboard_payload = _get_json(f"http://{host}:{port}/dashboard_state")
        game_id = dashboard_payload["dashboard"]["next_game"]["game_id"]
        assert game_id

        sim_payload = _post_json(f"http://{host}:{port}/simulate_user_game", {"game_id": game_id})
        assert sim_payload["ok"] is True

        standings_payload = _get_json(f"http://{host}:{port}/standings")
    finally:
        server.shutdown()
        server.server_close()

    assert standings_payload["ok"] is True
    standings = standings_payload["standings"]
    assert standings
    user_row = next(row for row in standings if row["team_id"] == user_team_id)
    assert user_row["wins"] + user_row["losses"] + user_row["ties"] == 0
    assert isinstance(user_row["points_for"], int)
    assert isinstance(user_row["points_against"], int)
    assert isinstance(user_row["win_pct"], float)

    sort_keys = [
        (
            -row["wins"],
            row["losses"],
            -row["win_pct"],
            -(row["points_for"] - row["points_against"]),
            row["abbreviation"] or row["team_name"] or row["team_id"],
        )
        for row in standings
    ]
    assert sort_keys == sorted(sort_keys)
    payload_json = json.dumps(standings_payload)
    for forbidden_key in (
        '"attributes"',
        '"career_stats"',
        '"season_stats"',
        '"hidden_caps"',
        '"scouted_potential"',
        '"contract"',
        '"traits"',
        '"skills"',
    ):
        assert forbidden_key not in payload_json


def test_team_roster_snapshot_handles_legacy_dict_players_and_missing_fields():
    facade = GameFacade(save_name="unit_test_team_roster_legacy")
    league = LeagueManager()
    team = Team("Legacy Team", "Legacy City", "LEG", conference="Nova", division="North", id="legacy-team")
    team.roster = [
        {
            "id": "p-active",
            "name": "Active Legacy",
            "position": "QB",
            "overall": 71,
            "age": 24,
            "injury_status": "healthy",
        }
    ]
    team.ir_list = [
        {
            "id": "p-ir",
            "name": "IR Legacy",
            "position": "WR",
            "overall": "68",
            "on_injured_reserve": True,
        }
    ]
    team.practice_squad = [{"id": "p-ps", "name": "PS Legacy", "position": "CB"}]
    league.add_team(team)
    league.user_team_id = team.id
    league.controlled_team_id = team.id
    facade.league = league
    facade.calendar = league.calendar
    facade.season_manager = _DummySeason()
    facade._time_engine = TimeEngine(league, league.calendar, schedule_by_week={})

    payload = facade.get_team_roster_snapshot()

    assert payload["ok"] is True
    assert payload["team"] == {
        "team_id": team.id,
        "name": "Legacy Team",
        "abbreviation": "LEG",
    }
    assert payload["roster_status"] == {
        "is_valid": True,
        "roster_size": 1,
        "roster_limit": 53,
        "required_cuts": 0,
        "open_slots": 52,
        "injured_count": 1,
        "issues": [],
    }
    players = {player["player_id"]: player for player in payload["players"]}
    assert players["p-active"]["status"] == "active"
    assert players["p-active"]["injury"] is None
    assert players["p-active"]["depth_role"] is None
    assert players["p-ir"]["status"] == "ir"
    assert players["p-ir"]["injury"] == "Ir"
    assert players["p-ps"]["overall"] is None
    assert players["p-ps"]["age"] is None
    assert players["p-ps"]["status"] == "practice_squad"


def test_api_state_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_api_state_no_game")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/state")
        assert payload == {"ok": False, "error": "no_active_game"}
    finally:
        server.shutdown()
        server.server_close()


def test_state_snapshot_handles_missing_optional_data_safely():
    facade = GameFacade(save_name="unit_test_api_state_missing")
    league = LeagueManager()
    league.user_team_id = None
    league.inboxes = None
    facade.league = league
    facade.calendar = league.calendar
    facade.season_manager = _DummySeason()
    facade._time_engine = TimeEngine(league, league.calendar, schedule_by_week={})

    payload = facade.get_state_snapshot("savegame.json")

    assert payload["ok"] is True
    assert payload["user"]["gm_id"]
    assert payload["user"]["gm_name"] == "User GM"
    assert payload["user"]["team_id"] is None
    assert payload["user"]["current_role"] == "General Manager"
    assert payload["today"]["games"] == []
    assert payload["today"]["user_game"] is None
    assert payload["inbox_summary"]["unread_count"] == 0
    assert payload["inbox_summary"]["blocking_decision_count"] == 0
    assert isinstance(payload["standings_summary"], list)
    assert payload["next_user_game"] is None


def test_rpc_state_returns_compact_snapshot():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_state")
    rpc.facade.new_game()

    status, payload = rpc.dispatch("GET", "/state", {})

    assert status == 200
    assert payload["ok"] is True
    assert payload["save_name"] == "savegame.json"
    assert payload["save_path"] == "savegame.json"
    assert "league" not in payload
    assert "schedule_by_week" not in json.dumps(payload)


def test_dashboard_state_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_dashboard_state_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/dashboard_state")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_game_result_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_game_result_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/game_result?game_id=test_game")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_team_schedule_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_team_schedule_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/team_schedule")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_team_depth_chart_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_team_depth_chart_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/team_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_auto_fill_depth_chart_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_auto_fill_depth_chart_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_update_depth_chart_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_update_depth_chart_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(
            f"http://{host}:{port}/update_depth_chart",
            {"position": "QB", "player_id": "p_001", "action": "move_up"},
        )
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_dashboard_state_returns_compact_payload_after_new_game():
    facade = GameFacade(save_name="unit_test_dashboard_state_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/dashboard_state")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "dashboard"}
    dashboard = payload["dashboard"]
    assert set(dashboard.keys()) == {"team", "calendar", "next_game", "team_status", "action_items", "recent_results"}
    assert set(dashboard["team"].keys()) == {"name", "abbreviation", "record"}
    assert set(dashboard["calendar"].keys()) == {"year", "week", "phase"}
    assert set(dashboard["next_game"].keys()) == {
        "opponent",
        "opponent_abbreviation",
        "home_away",
        "week",
        "game_type",
        "game_id",
    }
    assert set(dashboard["team_status"].keys()) == {"roster_size", "injuries", "cap_room"}
    assert isinstance(dashboard["action_items"], list)
    assert isinstance(dashboard["recent_results"], list)
    assert isinstance(dashboard["team"]["name"], str)
    assert isinstance(dashboard["team"]["abbreviation"], str)
    assert isinstance(dashboard["team"]["record"], str)
    assert isinstance(dashboard["calendar"]["year"], int)
    assert isinstance(dashboard["calendar"]["week"], int)
    assert isinstance(dashboard["calendar"]["phase"], str)
    assert isinstance(dashboard["team_status"]["roster_size"], int)
    assert isinstance(dashboard["team_status"]["injuries"], int)
    payload_json = json.dumps(payload)
    for forbidden_key in (
        '"league"',
        '"teams"',
        '"players"',
        '"roster"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"time_engine"',
    ):
        assert forbidden_key not in payload_json


def test_rpc_dashboard_state_returns_compact_payload():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_dashboard_state")

    empty_status, empty_payload = rpc.dispatch("GET", "/dashboard_state", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    status, payload = rpc.dispatch("GET", "/dashboard_state", {})

    assert status == 200
    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "dashboard"}
    assert "action_items" in payload["dashboard"]
    assert isinstance(payload["dashboard"]["action_items"], list)
    assert "recent_results" in payload["dashboard"]
    assert isinstance(payload["dashboard"]["recent_results"], list)
    assert "players" not in json.dumps(payload)


def test_team_depth_chart_returns_compact_payload_after_new_game():
    facade = GameFacade(save_name="unit_test_team_depth_chart_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/team_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "team", "depth_chart_status", "positions"}
    assert set(payload["team"].keys()) == {"team_id", "name", "abbreviation"}
    assert isinstance(payload["depth_chart_status"]["is_valid"], bool)
    assert isinstance(payload["depth_chart_status"]["issues"], list)
    assert isinstance(payload["positions"], list)
    assert payload["positions"]
    first_position = payload["positions"][0]
    assert set(first_position.keys()) == {"position", "required_starters", "players"}
    assert isinstance(first_position["players"], list)
    if first_position["players"]:
        first_player = first_position["players"][0]
        assert set(first_player.keys()) == {"player_id", "name", "overall", "status", "injury", "role"}
    payload_json = json.dumps(payload)
    for forbidden_key in (
        '"league"',
        '"teams"',
        '"roster"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"time_engine"',
        '"savegame"',
    ):
        assert forbidden_key not in payload_json


def test_rpc_team_depth_chart_returns_compact_payload():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_team_depth_chart")

    empty_status, empty_payload = rpc.dispatch("GET", "/team_depth_chart", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    status, payload = rpc.dispatch("GET", "/team_depth_chart", {})

    assert status == 200
    assert payload["ok"] is True
    assert "positions" in payload
    assert isinstance(payload["positions"], list)
    assert '"roster"' not in json.dumps(payload)


def test_rpc_auto_fill_depth_chart_returns_compact_payload():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_auto_fill_depth_chart")

    empty_status, empty_payload = rpc.dispatch("POST", "/auto_fill_depth_chart", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    status, payload = rpc.dispatch("POST", "/auto_fill_depth_chart", {})

    assert status == 200
    assert payload["ok"] is True
    assert payload["message"] == "Depth chart auto-filled."
    assert "depth_chart" in payload
    assert isinstance(payload["depth_chart"].get("positions"), list)
    assert '"roster"' not in json.dumps(payload)


def test_rpc_update_depth_chart_validates_missing_fields_and_invalid_action():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_update_depth_chart")
    rpc.facade.new_game()

    missing_status, missing_payload = rpc.dispatch("POST", "/update_depth_chart", {})
    invalid_status, invalid_payload = rpc.dispatch(
        "POST",
        "/update_depth_chart",
        {"position": "QB", "player_id": "player-1", "action": "jump_to_top"},
    )

    assert missing_status == 200
    assert missing_payload == {"ok": False, "error": "Missing position, player_id, or action."}
    assert invalid_status == 200
    assert invalid_payload == {"ok": False, "error": "Invalid depth chart action."}


def test_auto_fill_depth_chart_returns_compact_payload_after_new_game():
    facade = GameFacade(save_name="unit_test_auto_fill_depth_chart_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert payload["message"] == "Depth chart auto-filled."
    assert set(payload.keys()) == {"ok", "message", "depth_chart"}
    depth_chart = payload["depth_chart"]
    assert set(depth_chart.keys()) == {"team", "depth_chart_status", "positions"}
    assert set(depth_chart["team"].keys()) == {"team_id", "name", "abbreviation"}
    assert isinstance(depth_chart["depth_chart_status"]["is_valid"], bool)
    assert isinstance(depth_chart["depth_chart_status"]["issues"], list)
    assert isinstance(depth_chart["positions"], list)
    assert depth_chart["positions"]
    payload_json = json.dumps(payload)
    for forbidden_key in (
        '"league"',
        '"teams"',
        '"roster"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"time_engine"',
        '"savegame"',
    ):
        assert forbidden_key not in payload_json


def test_auto_fill_depth_chart_and_get_team_depth_chart_stay_consistent():
    facade = GameFacade(save_name="unit_test_auto_fill_depth_chart_matches_get")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        auto_fill_payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
        depth_chart_payload = _get_json(f"http://{host}:{port}/team_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert auto_fill_payload["ok"] is True
    assert depth_chart_payload["ok"] is True
    assert auto_fill_payload["depth_chart"] == {
        "team": depth_chart_payload["team"],
        "depth_chart_status": depth_chart_payload["depth_chart_status"],
        "positions": depth_chart_payload["positions"],
    }


def test_auto_fill_depth_chart_does_not_fake_validity_when_required_position_is_unavailable():
    facade = GameFacade(save_name="unit_test_auto_fill_depth_chart_no_fake_validity")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    for player in team.roster:
        if getattr(player, "position", "") == "QB":
            player.injury_status = "out"
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    status = payload["depth_chart"]["depth_chart_status"]
    assert status["is_valid"] is False
    assert any("QB" in issue for issue in status["issues"])


def test_update_depth_chart_validates_missing_fields():
    facade = GameFacade(save_name="unit_test_update_depth_chart_missing_fields")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/update_depth_chart", {"position": "QB"})
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "Missing position, player_id, or action."}


def test_update_depth_chart_validates_invalid_action():
    facade = GameFacade(save_name="unit_test_update_depth_chart_invalid_action")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(
            f"http://{host}:{port}/update_depth_chart",
            {"position": "QB", "player_id": "p_001", "action": "promote"},
        )
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "Invalid depth chart action."}


def test_update_depth_chart_changes_order_after_auto_fill():
    facade = GameFacade(save_name="unit_test_update_depth_chart_changes_order")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        auto_fill_payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
        position_row = _find_depth_chart_group_with_multiple_players(auto_fill_payload["depth_chart"]["positions"])
        players = position_row["players"]
        target_index = 1
        target_player_id = players[target_index]["player_id"]

        payload = _post_json(
            f"http://{host}:{port}/update_depth_chart",
            {
                "position": position_row["position"],
                "player_id": target_player_id,
                "action": "move_up",
            },
        )
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert payload["message"] == "Depth chart updated."
    assert set(payload.keys()) == {"ok", "message", "depth_chart"}
    updated_positions = payload["depth_chart"]["positions"]
    updated_row = next(row for row in updated_positions if row["position"] == position_row["position"])
    assert updated_row["players"][0]["player_id"] == target_player_id
    payload_json = json.dumps(payload)
    for forbidden_key in (
        '"league"',
        '"teams"',
        '"roster"',
        '"players": [{"id"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"time_engine"',
        '"savegame"',
    ):
        assert forbidden_key not in payload_json


def test_update_depth_chart_set_starter_reorders_player_and_recalculates_roles():
    facade = GameFacade(save_name="unit_test_update_depth_chart_set_starter")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        auto_fill_payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
        position_row = _find_depth_chart_group_with_multiple_players(auto_fill_payload["depth_chart"]["positions"])
        players = position_row["players"]
        target_player = players[-1]

        payload = _post_json(
            f"http://{host}:{port}/update_depth_chart",
            {
                "position": position_row["position"],
                "player_id": target_player["player_id"],
                "action": "set_starter",
            },
        )
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    updated_row = next(
        row for row in payload["depth_chart"]["positions"] if row["position"] == position_row["position"]
    )
    assert updated_row["players"][0]["player_id"] == target_player["player_id"]
    required_starters = max(1, int(updated_row.get("required_starters", 1) or 1))
    for index, player in enumerate(updated_row["players"]):
        expected_role = "Starter" if index < required_starters else "Backup"
        assert player["role"] == expected_role


def test_update_depth_chart_and_get_team_depth_chart_stay_consistent():
    facade = GameFacade(save_name="unit_test_update_depth_chart_matches_get")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        auto_fill_payload = _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
        position_row = _find_depth_chart_group_with_multiple_players(auto_fill_payload["depth_chart"]["positions"])
        target_player_id = position_row["players"][1]["player_id"]
        update_payload = _post_json(
            f"http://{host}:{port}/update_depth_chart",
            {
                "position": position_row["position"],
                "player_id": target_player_id,
                "action": "move_up",
            },
        )
        depth_chart_payload = _get_json(f"http://{host}:{port}/team_depth_chart")
    finally:
        server.shutdown()
        server.server_close()

    assert update_payload["ok"] is True
    assert depth_chart_payload["ok"] is True
    assert update_payload["depth_chart"] == {
        "team": depth_chart_payload["team"],
        "depth_chart_status": depth_chart_payload["depth_chart_status"],
        "positions": depth_chart_payload["positions"],
    }


def test_update_depth_chart_returns_player_not_found_for_unknown_player():
    facade = GameFacade(save_name="unit_test_update_depth_chart_unknown_player")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        _post_json(f"http://{host}:{port}/auto_fill_depth_chart")
        payload = _post_json(
            f"http://{host}:{port}/update_depth_chart",
            {"position": "QB", "player_id": "missing-player", "action": "move_up"},
        )
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "Player not found in depth chart position group."}


def test_team_schedule_returns_compact_payload_after_new_game():
    facade = GameFacade(save_name="unit_test_team_schedule_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/team_schedule")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "schedule"}
    assert isinstance(payload["schedule"], list)
    assert payload["schedule"]
    first = payload["schedule"][0]
    assert set(first.keys()) == {
        "game_id",
        "week",
        "game_type",
        "opponent",
        "home_away",
        "status",
        "home_team",
        "away_team",
        "home_score",
        "away_score",
        "winner",
    }
    assert first["status"] in {"upcoming", "game_day", "final"}
    payload_json = json.dumps(payload)
    for forbidden_key in ('"league"', '"teams"', '"players"', '"roster"', '"results_by_week"', '"schedule_by_week"'):
        assert forbidden_key not in payload_json


def test_rpc_standings_returns_compact_payload_and_clean_error():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_standings")

    empty_status, empty_payload = rpc.dispatch("GET", "/standings", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    status, payload = rpc.dispatch("GET", "/standings", {})

    assert status == 200
    assert payload["ok"] is True
    assert isinstance(payload["standings"], list)
    assert payload["standings"]
    assert "players" not in json.dumps(payload)


def test_rpc_game_result_returns_clean_errors():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_game_result")

    empty_status, empty_payload = rpc.dispatch("GET", "/game_result?game_id=test_game", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    missing_status, missing_payload = rpc.dispatch("GET", "/game_result", {})
    assert missing_status == 200
    assert missing_payload == {"ok": False, "error": "Missing game_id."}


def test_rpc_team_schedule_marks_game_day_for_current_user_game():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_team_schedule_game_day")

    empty_status, empty_payload = rpc.dispatch("GET", "/team_schedule", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    continue_status, continue_payload = rpc.dispatch("POST", "/continue", {"max_days": 14})
    assert continue_status == 200
    assert continue_payload["ok"] is True
    assert continue_payload["result"]["stop_reason"] == "game_day"

    status, payload = rpc.dispatch("GET", "/team_schedule", {})
    assert status == 200
    assert payload["ok"] is True
    assert any(game["status"] == "game_day" for game in payload["schedule"])


def test_continue_returns_clean_error_without_active_game():
    facade = GameFacade(save_name="unit_test_continue_empty")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/continue")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "No active league loaded."}


def test_continue_returns_compact_result_after_new_game():
    facade = GameFacade(save_name="unit_test_continue_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/continue", {"max_days": 1})
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "result"}
    result = payload["result"]
    assert set(result.keys()) == {"advanced", "stop_reason", "days_advanced", "events_processed"}
    assert isinstance(result["advanced"], bool)
    assert isinstance(result["stop_reason"], str)
    assert isinstance(result["days_advanced"], int)
    assert isinstance(result["events_processed"], list)
    assert result["events_processed"]
    payload_json = json.dumps(payload)
    for forbidden_key in (
        '"league"',
        '"teams"',
        '"players"',
        '"roster"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"time_engine"',
    ):
        assert forbidden_key not in payload_json


def test_dashboard_state_includes_action_items_after_continue():
    facade = GameFacade(save_name="unit_test_dashboard_action_items_after_continue")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        continue_payload = _post_json(f"http://{host}:{port}/continue", {"max_days": 14})
        dashboard_payload = _get_json(f"http://{host}:{port}/dashboard_state")
    finally:
        server.shutdown()
        server.server_close()

    assert continue_payload["ok"] is True
    assert dashboard_payload["ok"] is True
    dashboard = dashboard_payload["dashboard"]
    assert "action_items" in dashboard
    assert isinstance(dashboard["action_items"], list)

    stop_reason = str(continue_payload["result"].get("stop_reason") or "")
    action_types = {item.get("type") for item in dashboard["action_items"] if isinstance(item, dict)}
    if stop_reason == "game_day":
        assert "game_day" in action_types
    elif stop_reason in {"season_phase_changed", "max_days_reached", "roster_invalid", "depth_chart_invalid"}:
        assert stop_reason in action_types

    payload_json = json.dumps(dashboard_payload)
    for forbidden_key in (
        '"players"',
        '"roster"',
        '"league"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"time_engine"',
        '"savegame"',
    ):
        assert forbidden_key not in payload_json


def test_simulate_user_game_returns_compact_result_and_clears_game_day_action():
    facade = GameFacade(save_name="unit_test_sim_user_game_compact")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        continue_payload = _post_json(f"http://{host}:{port}/continue", {"max_days": 14})
        assert continue_payload["ok"] is True
        assert continue_payload["result"]["stop_reason"] == "game_day"

        dashboard_before = _get_json(f"http://{host}:{port}/dashboard_state")
        assert dashboard_before["ok"] is True
        next_game = dashboard_before["dashboard"]["next_game"]
        game_id = next_game["game_id"]
        assert game_id
        action_types_before = {
            item.get("type")
            for item in dashboard_before["dashboard"]["action_items"]
            if isinstance(item, dict)
        }
        assert "game_day" in action_types_before

        sim_payload = _post_json(f"http://{host}:{port}/simulate_user_game", {"game_id": game_id})
        assert sim_payload["ok"] is True
        assert set(sim_payload.keys()) == {"ok", "result"}
        result = sim_payload["result"]
        assert set(result.keys()).issuperset(
            {"game_id", "week", "game_type", "home_team", "away_team", "home_score", "away_score", "winner", "summary", "box_score"}
        )
        assert isinstance(result["box_score"], dict)
        assert set(result["box_score"].keys()).issuperset({"final", "team_stats"})
        assert "league" not in json.dumps(result["box_score"])
        assert "players" not in json.dumps(result["box_score"])
        payload_json = json.dumps(sim_payload)
        for forbidden_key in (
            '"league"',
            '"teams"',
            '"players"',
            '"roster"',
            '"schedule_by_week"',
            '"results_by_week"',
            '"time_engine"',
        ):
            assert forbidden_key not in payload_json

        dashboard_after = _get_json(f"http://{host}:{port}/dashboard_state")
    finally:
        server.shutdown()
        server.server_close()

    assert dashboard_after["ok"] is True
    action_types_after = {
        item.get("type")
        for item in dashboard_after["dashboard"]["action_items"]
        if isinstance(item, dict)
    }
    assert "game_day" not in action_types_after
    recent_results = dashboard_after["dashboard"]["recent_results"]
    assert isinstance(recent_results, list)
    assert recent_results
    assert any(result.get("game_id") == game_id for result in recent_results if isinstance(result, dict))
    allowed_keys = {"game_id", "week", "game_type", "home_team", "away_team", "home_score", "away_score", "winner", "summary"}
    for result in recent_results:
        if not isinstance(result, dict):
            continue
        assert set(result.keys()) == allowed_keys
    recent_results_json = json.dumps(recent_results)
    for forbidden_key in ('"league"', '"teams"', '"players"', '"roster"', '"schedule_by_week"', '"results_by_week"', '"box_score"'):
        assert forbidden_key not in recent_results_json


def test_game_result_returns_missing_game_id_when_loaded_league_has_no_query_value():
    facade = GameFacade(save_name="unit_test_game_result_missing_game_id")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/game_result")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "Missing game_id."}


def test_game_result_returns_compact_completed_result_after_sim():
    facade = GameFacade(save_name="unit_test_game_result_after_sim")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        continue_payload = _post_json(f"http://{host}:{port}/continue", {"max_days": 14})
        assert continue_payload["ok"] is True
        assert continue_payload["result"]["stop_reason"] == "game_day"

        dashboard_payload = _get_json(f"http://{host}:{port}/dashboard_state")
        game_id = dashboard_payload["dashboard"]["next_game"]["game_id"]
        assert game_id

        sim_payload = _post_json(f"http://{host}:{port}/simulate_user_game", {"game_id": game_id})
        assert sim_payload["ok"] is True

        result_payload = _get_json(f"http://{host}:{port}/game_result?game_id={game_id}")
    finally:
        server.shutdown()
        server.server_close()

    assert result_payload["ok"] is True
    result = result_payload["result"]
    assert set(result.keys()).issuperset(
        {"game_id", "week", "game_type", "home_team", "away_team", "home_score", "away_score", "winner", "summary", "box_score"}
    )
    assert result["game_id"] == game_id
    assert isinstance(result["box_score"], dict)
    result_json = json.dumps(result_payload)
    for forbidden_key in ('"league"', '"teams"', '"players"', '"roster"', '"schedule_by_week"', '"results_by_week"', '"savegame"'):
        assert forbidden_key not in result_json


def test_team_schedule_marks_completed_user_game_final_and_result_reopens():
    facade = GameFacade(save_name="unit_test_team_schedule_after_sim")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        continue_payload = _post_json(f"http://{host}:{port}/continue", {"max_days": 14})
        assert continue_payload["ok"] is True
        assert continue_payload["result"]["stop_reason"] == "game_day"

        schedule_before = _get_json(f"http://{host}:{port}/team_schedule")
        game_day_game = next(game for game in schedule_before["schedule"] if game["status"] == "game_day")
        game_id = game_day_game["game_id"]
        assert game_id

        sim_payload = _post_json(f"http://{host}:{port}/simulate_user_game", {"game_id": game_id})
        assert sim_payload["ok"] is True

        schedule_after = _get_json(f"http://{host}:{port}/team_schedule")
        result_payload = _get_json(f"http://{host}:{port}/game_result?game_id={game_id}")
    finally:
        server.shutdown()
        server.server_close()

    final_game = next(game for game in schedule_after["schedule"] if game["game_id"] == game_id)
    assert final_game["status"] == "final"
    assert isinstance(final_game["home_score"], int)
    assert isinstance(final_game["away_score"], int)
    assert final_game["winner"] is None or isinstance(final_game["winner"], str)
    assert result_payload["ok"] is True
    assert result_payload["result"]["game_id"] == game_id


def test_game_result_returns_not_found_for_unknown_game_id():
    facade = GameFacade(save_name="unit_test_game_result_not_found")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _get_json(f"http://{host}:{port}/game_result?game_id=fake_game_id")
    finally:
        server.shutdown()
        server.server_close()

    assert payload == {"ok": False, "error": "Game result not found."}


def test_stop_continue_is_safe_when_no_sim_is_running():
    facade = GameFacade(save_name="unit_test_stop_continue_idle")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = _post_json(f"http://{host}:{port}/stop_continue")
    finally:
        server.shutdown()
        server.server_close()

    assert payload["ok"] is True
    assert set(payload.keys()) == {"ok", "result"}
    assert isinstance(payload["result"].get("stop_requested"), bool)
    assert isinstance(payload["result"].get("message"), str)
    assert payload["result"]["message"]


def test_rpc_continue_and_stop_endpoints_stay_compact():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_continue_compact")

    empty_status, empty_payload = rpc.dispatch("POST", "/continue", {})
    assert empty_status == 200
    assert empty_payload == {"ok": False, "error": "No active league loaded."}

    rpc.facade.new_game()
    status, payload = rpc.dispatch("POST", "/continue", {"max_days": 1})
    stop_status, stop_payload = rpc.dispatch("POST", "/stop_continue", {})

    assert status == 200
    assert payload["ok"] is True
    assert set(payload["result"].keys()) == {"advanced", "stop_reason", "days_advanced", "events_processed"}
    assert "players" not in json.dumps(payload)
    assert stop_status == 200
    assert stop_payload["ok"] is True
    assert isinstance(stop_payload["result"].get("message"), str)


def test_rpc_team_roster_returns_compact_snapshot():
    rpc = RpcServer(save_path="savegame.json", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_rpc_team_roster")
    rpc.facade.new_game()

    status, payload = rpc.dispatch("GET", "/team_roster", {})

    assert status == 200
    assert payload["ok"] is True
    assert payload["team"]["team_id"] == rpc.facade.league.user_team_id
    assert payload["roster_status"]["roster_size"] == 53
    assert payload["position_counts"]
    assert payload["players"]


def test_dashboard_state_roster_invalid_action_item_includes_count_detail():
    facade = GameFacade(save_name="unit_test_dashboard_roster_invalid")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.roster.append(team.roster[0])

    payload = facade.get_dashboard_state()

    assert payload["ok"] is True
    items = payload["dashboard"]["action_items"]
    roster_item = next(item for item in items if item["type"] == "roster_invalid")
    assert roster_item["title"] == "Roster Issue"
    assert roster_item["primary_action"] == "View Roster"
    assert roster_item["description"] == "Roster has 54 players. Limit is 53. Cut 1 players."


def test_dashboard_state_depth_chart_invalid_action_item_includes_issue_detail():
    facade = GameFacade(save_name="unit_test_dashboard_depth_chart_invalid")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.depth_chart = {}
    for player in team.roster:
        if getattr(player, "position", "") == "QB":
            player.injury_status = "out"

    payload = facade.get_dashboard_state()

    assert payload["ok"] is True
    items = payload["dashboard"]["action_items"]
    depth_chart_item = next(item for item in items if item["type"] == "depth_chart_invalid")
    assert depth_chart_item["title"] == "Depth Chart Issue"
    assert depth_chart_item["primary_action"] == "View Depth Chart"
    assert "Missing starting QB." in depth_chart_item["description"]


def test_auto_fill_depth_chart_clears_dashboard_action_item_when_chart_becomes_valid():
    facade = GameFacade(save_name="unit_test_auto_fill_depth_chart_clears_action")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    qb = next(player for player in team.roster if getattr(player, "position", "") == "QB")
    team.depth_chart = {"QB": [qb]}

    dashboard_before = facade.get_dashboard_state()
    action_types_before = {
        item.get("type")
        for item in dashboard_before["dashboard"]["action_items"]
        if isinstance(item, dict)
    }
    assert "depth_chart_invalid" in action_types_before

    auto_fill_payload = facade.auto_fill_depth_chart()
    dashboard_after = facade.get_dashboard_state()

    assert auto_fill_payload["ok"] is True
    assert auto_fill_payload["depth_chart"]["depth_chart_status"]["is_valid"] is True
    action_types_after = {
        item.get("type")
        for item in dashboard_after["dashboard"]["action_items"]
        if isinstance(item, dict)
    }
    assert "depth_chart_invalid" not in action_types_after


def test_old_notifications_load_with_safe_defaults():
    league = LeagueManager()
    team_id = "team-1"
    league.user_team_id = team_id
    league.controlled_team_id = team_id
    league.inboxes = {
        team_id: [
            {
                "id": 7,
                "date": "2026-01-01",
                "hour": 9,
                "category": "News",
                "subject": "Legacy item",
                "body": "Loaded from older save.",
            }
        ]
    }
    engine = TimeEngine(league, league.calendar, schedule_by_week={})
    message = engine.get_inbox(team_id)[0]

    assert message.read is False
    assert message.requires_ack is False
    assert message.requires_user_attention is False
    assert message.blocks_advancement is False
    assert message.decision_type is None
    assert message.decision_id is None
    assert message.payload == {}


def test_decision_item_serializes_and_deserializes_safely():
    restored = DecisionItem.from_dict(
        {
            "decision_id": "dec-1",
            "title": "Choose",
            "message": "Pick one",
            "options": [{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
        }
    )

    assert restored.status == "open"
    assert restored.blocks_advancement is False
    assert restored.payload == {}
    assert restored.to_dict()["decision_id"] == "dec-1"


def test_inbox_endpoint_returns_compact_notifications_and_counts():
    facade = GameFacade(save_name="unit_test_inbox_endpoint")
    facade.new_game()
    engine = facade._get_time_engine()
    team_id = facade.league.user_team_id
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=1,
            date=facade.calendar.current_date,
            hour=9,
            category="News",
            subject="Inbox Item",
            body="Compact notification",
            read=False,
        )
    )
    facade.create_decision(
        category="FrontOffice",
        decision_type="test_block",
        title="Blocking",
        message="Needs action",
        blocks_advancement=True,
        options=[{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
    )

    payload = facade.get_inbox()

    assert payload["ok"] is True
    assert payload["unread_count"] >= 1
    assert payload["blocking_decision_count"] == 1
    assert payload["messages"]
    assert "notification_id" in payload["messages"][0]
    assert payload["open_decisions"]


def test_inbox_mark_read_and_acknowledge_work():
    facade = GameFacade(save_name="unit_test_inbox_actions")
    facade.new_game()
    engine = facade._get_time_engine()
    team_id = facade.league.user_team_id
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=55,
            date=facade.calendar.current_date,
            hour=9,
            category="News",
            subject="Ack me",
            body="Needs ack",
            requires_ack=True,
            requires_user_attention=True,
            read=False,
        )
    )

    read_payload = facade.mark_inbox_read("55", include_messages=True)
    ack_payload = facade.acknowledge_inbox_notification("55", include_messages=True)

    assert read_payload["ok"] is True
    assert any(item["read"] is True for item in read_payload["messages"] if item["notification_id"] == "55")
    assert ack_payload["ok"] is True
    ack_item = next(item for item in ack_payload["messages"] if item["notification_id"] == "55")
    assert ack_item["requires_ack"] is False


def test_inbox_and_decision_http_endpoints_work():
    facade = GameFacade(save_name="unit_test_http_inbox_decisions")
    facade.new_game()
    engine = facade._get_time_engine()
    team_id = facade.league.user_team_id
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=77,
            date=facade.calendar.current_date,
            hour=9,
            category="News",
            subject="Read me",
            body="From HTTP test",
            requires_ack=True,
            requires_user_attention=True,
            read=False,
        )
    )
    created = facade.create_decision(
        category="FrontOffice",
        decision_type="test_http",
        title="HTTP Decision",
        message="Resolve over HTTP",
        blocks_advancement=True,
        options=[{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
    )
    decision_id = created["decision"]["decision_id"]
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        inbox_payload = _get_json(f"http://{host}:{port}/inbox")
        decisions_payload = _get_json(f"http://{host}:{port}/decisions")
        with urllib.request.urlopen(
            f"http://{host}:{port}/inbox/mark_read",
            data=json.dumps({"notification_id": "77"}).encode("utf-8"),
            timeout=10,
        ) as resp:
            mark_payload = json.loads(resp.read().decode("utf-8"))
        with urllib.request.urlopen(
            f"http://{host}:{port}/inbox/acknowledge",
            data=json.dumps({"notification_id": "77"}).encode("utf-8"),
            timeout=10,
        ) as resp:
            ack_payload = json.loads(resp.read().decode("utf-8"))
        with urllib.request.urlopen(
            f"http://{host}:{port}/decisions/resolve",
            data=json.dumps({"decision_id": decision_id, "option_id": "acknowledge"}).encode("utf-8"),
            timeout=10,
        ) as resp:
            resolve_payload = json.loads(resp.read().decode("utf-8"))
    finally:
        server.shutdown()
        server.server_close()

    assert inbox_payload["messages"]
    assert inbox_payload["blocking_decision_count"] == 1
    assert decisions_payload["decisions"]
    assert mark_payload["ok"] is True
    assert ack_payload["ok"] is True
    assert resolve_payload["ok"] is True


def test_decisions_endpoint_and_resolution_work():
    facade = GameFacade(save_name="unit_test_decisions_endpoint")
    facade.new_game()
    created = facade.create_decision(
        category="FrontOffice",
        decision_type="test_choice",
        title="Test Decision",
        message="Resolve me",
        blocks_advancement=True,
        options=[{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
    )
    decision_id = created["decision"]["decision_id"]

    listing = facade.get_decisions()
    resolved = facade.resolve_decision(decision_id, "acknowledge")

    assert listing["ok"] is True
    assert any(item["decision_id"] == decision_id for item in listing["decisions"])
    assert resolved["ok"] is True
    assert resolved["decision"]["status"] == "resolved"
    assert resolved["blocking_decision_count"] == 0


def test_state_and_calendar_dashboard_include_compact_decision_info():
    facade = GameFacade(save_name="unit_test_state_calendar_decisions")
    facade.new_game()
    facade.create_decision(
        category="FrontOffice",
        decision_type="test_block",
        title="Blocking",
        message="Needs action",
        blocks_advancement=True,
        options=[{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
    )

    state_payload = facade.get_state_snapshot("savegame.json")
    calendar_payload = facade.get_calendar_dashboard()

    assert state_payload["inbox_summary"]["blocking_decision_count"] == 1
    assert "payload" not in json.dumps(state_payload["inbox_summary"])
    assert calendar_payload["blocking_decision_count"] == 1
    assert calendar_payload["blocking_decisions"]


def test_api_new_game_returns_json_error_on_failure(monkeypatch):
    facade = GameFacade()

    def broken_new_game():
        raise RuntimeError("boom")

    monkeypatch.setattr(facade, "new_game", broken_new_game)
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        try:
            urllib.request.urlopen(f"http://{host}:{port}/new_game", data=b"{}")
            raise AssertionError("expected /new_game to fail")
        except HTTPError as exc:
            assert exc.code == 500
            payload = json.loads(exc.read().decode("utf-8"))
        assert payload == {"ok": False, "error": "RuntimeError: boom"}
    finally:
        server.shutdown()
        server.server_close()


def test_godot_top_bar_uses_backend_week_label():
    repo_root = Path(__file__).resolve().parents[1]
    source = (repo_root / "Godot" / "Scripts" / "DashboardController.cs").read_text(encoding="utf-8")

    assert 'GetFirstNonNil(cal, "week_label"' in source
    assert "BuildScheduleStatusLine(dict)" in source
    assert 'TryExtractObject(state, "user_team_game_today", "userTeamGameToday")' in source
    assert 'TryExtractObject(state, "user_team_next_game", "userTeamNextGame")' in source
    assert 'TryExtract(state, "league_games_today_count", "leagueGamesTodayCount")' in source
    assert "Next User Game:" in source
    assert 'PostWithTimeoutAsync("/sim_until"' in source
    assert "Regular Season Week 5" in source
    assert "Simulating to selected milestone..." in source
    assert 'GetFirstNonNil(cal, "season_year")' in source
    assert 'GetFirstNonNil(cal, "day_of_week")' in source
    assert 'GetFirstNonNil(cal, "current_date")' in source
    assert 'GetFirstNonNil(cal, "week_label")' in source
    assert "_calendarText.Text = $\"{year} Season\\n{weekLabel}\\n{date}\\n\\n{scheduleLine}\";" in source
