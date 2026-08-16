import json
import sys
import threading
import urllib.request

from gridiron_gm_pkg.api.rpc_server import RpcServer
from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.systems.time_engine import TimeEngine


class _DummySeason:
    schedule_by_week = {}


def test_calendar_dashboard_returns_compact_clock_fields():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_clock")
    facade.new_game()

    payload = facade.get_calendar_dashboard()

    assert payload["current_date"] == facade.calendar.current_date.isoformat()
    assert payload["current_time"] == facade._get_time_engine().clock.current_time_str
    assert payload["current_week"] == facade.calendar.current_week
    assert payload["current_phase"] == facade.calendar.season_phase
    assert payload["current_year"] == facade.calendar.current_year
    assert payload["gm"]["gm_id"]
    assert payload["gm"]["name"] == "User GM"
    assert payload["gm"]["current_role"] == "General Manager"
    assert payload["gm"]["reputation"] == 50
    assert payload["gm"]["job_security"] == 50
    assert payload["available_actions"] == [
        "advance_to_next_event",
        "advance_to_end_of_day",
        "advance_one_week",
        "advance_to_milestone",
    ]


def test_calendar_dashboard_includes_next_event():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_next_event")
    facade.new_game()

    payload = facade.get_calendar_dashboard()

    assert payload["next_event"] is not None
    assert payload["next_event"]["event_id"]
    assert payload["next_event"]["date"] == facade.calendar.current_date.isoformat()
    assert payload["next_event"]["time"] >= facade._get_time_engine().clock.current_time_str


def test_calendar_dashboard_today_events_chronological():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_today_events")
    facade.new_game()

    payload = facade.get_calendar_dashboard()
    times = [event["time"] for event in payload["today_events"]]

    assert times == sorted(times)
    assert payload["today_events"]


def test_calendar_dashboard_does_not_process_events():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_readonly")
    facade.new_game()
    engine = facade._get_time_engine()
    before_clock = (engine.clock.current_date, engine.clock.hour)
    before_queue_ids = [event.id for event in engine.queue.events()]
    before_results = facade._count_results()

    payload = facade.get_calendar_dashboard()

    after_queue_ids = [event.id for event in engine.queue.events()]
    after_clock = (engine.clock.current_date, engine.clock.hour)
    assert payload["today_events"]
    assert before_clock == after_clock
    assert before_queue_ids == after_queue_ids
    assert before_results == facade._count_results()


def test_calendar_dashboard_is_compact_and_omits_full_league_payload():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_compact")
    facade.new_game()

    payload = facade.get_calendar_dashboard()

    assert "league" not in payload
    assert "calendar" not in payload
    assert "schema_version" not in payload
    assert "save_name" not in payload
    assert set(payload.keys()) == {
        "current_date",
        "current_time",
        "current_week",
        "current_phase",
        "current_year",
        "gm",
        "team",
        "next_event",
        "today_events",
        "notifications",
        "blocking_decision_count",
        "blocking_decisions",
        "available_actions",
    }
    payload_json = json.dumps(payload)
    for forbidden_key in (
        '"players"',
        '"roster"',
        '"schedule_by_week"',
        '"results_by_week"',
        '"archive"',
        '"free_agents"',
        '"draft_prospects"',
    ):
        assert forbidden_key not in payload_json


def test_calendar_dashboard_handles_missing_fields_safely():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_missing")
    league = LeagueManager()
    league.user_team_id = None
    league.inboxes = None
    facade.league = league
    facade.calendar = league.calendar
    facade.season_manager = _DummySeason()
    facade._time_engine = TimeEngine(league, league.calendar, schedule_by_week={})

    payload = facade.get_calendar_dashboard()

    assert payload["gm"]["gm_id"]
    assert payload["gm"]["name"] == "User GM"
    assert payload["gm"]["current_team_id"] is None
    assert payload["gm"]["current_role"] == "General Manager"
    assert payload["gm"]["reputation"] == 50
    assert payload["gm"]["job_security"] == 50
    assert payload["team"]["team_id"] is None
    assert payload["team"]["name"] == ""
    assert payload["team"]["record"] == "0-0"
    assert isinstance(payload["today_events"], list)
    assert isinstance(payload["notifications"], list)


def test_dashboard_state_exposes_compact_action_items_list():
    facade = GameFacade(save_name="unit_test_dashboard_action_items_list")
    facade.new_game()

    payload = facade.get_dashboard_state()

    assert payload["ok"] is True
    assert "action_items" in payload["dashboard"]
    assert isinstance(payload["dashboard"]["action_items"], list)
    assert "recent_results" in payload["dashboard"]
    assert isinstance(payload["dashboard"]["recent_results"], list)
    payload_json = json.dumps(payload)
    for forbidden_key in ('"players"', '"roster"', '"league"', '"schedule_by_week"', '"results_by_week"'):
        assert forbidden_key not in payload_json


def test_calendar_dashboard_http_endpoint_returns_compact_payload():
    facade = GameFacade(save_name="unit_test_calendar_dashboard_http")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(f"http://{host}:{port}/calendar_dashboard", timeout=10) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["current_date"]
        assert "league" not in payload
        assert isinstance(payload["today_events"], list)
    finally:
        server.shutdown()
        server.server_close()


def test_calendar_dashboard_rpc_endpoint_returns_compact_payload():
    stdout = sys.stdout
    try:
        rpc = RpcServer(save_path="", parent_pid=0)
        rpc.facade.new_game()

        status, payload = rpc.dispatch("GET", "/calendar_dashboard", {})

        assert status == 200
        assert payload["current_date"]
        assert "league" not in payload
        assert isinstance(payload["notifications"], list)
    finally:
        sys.stdout = stdout
