import json
import sys
import threading
import urllib.request
from pathlib import Path

from gridiron_gm_pkg.api.rpc_server import RpcServer
from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.career.gm_profile import GMProfile
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade


def test_new_game_creates_gm_profile():
    facade = GameFacade(save_name="unit_test_gm_profile_new_game")
    facade.new_game()

    gm = facade.league.user_gm
    assert gm is not None
    assert gm.name == "User GM"
    assert gm.current_role == "General Manager"
    assert gm.current_team_id == facade.league.controlled_team_id
    assert gm.career_history
    assert gm.career_history[0].status == "active"


def test_new_game_accepts_optional_gm_name_and_team_id():
    facade = GameFacade(save_name="unit_test_gm_profile_new_game_options")
    facade.new_game(gm_name="Casey Stone", team_id="CHI")

    gm = facade.league.user_gm
    expected_team_id = facade.league.abbr_to_id["CHI"]
    assert gm.name == "Casey Stone"
    assert gm.current_team_id == expected_team_id
    assert facade.league.controlled_team_id == expected_team_id
    assert facade.league.user_team_id == expected_team_id


def test_gm_profile_serializes_and_deserializes_safely():
    profile = GMProfile.from_dict(
        {
            "gm_id": "gm-1",
            "name": "Pat Rivers",
            "current_team_id": "DAL",
            "career_start_year": 2025,
            "career_history": [{"team_id": "DAL", "team_name": "Dallas Cowboys", "start_year": 2025}],
            "traits": ["aggressive"],
        }
    )

    payload = profile.to_dict()
    round_trip = GMProfile.from_dict(payload)

    assert round_trip.gm_id == "gm-1"
    assert round_trip.name == "Pat Rivers"
    assert round_trip.current_team_id == "DAL"
    assert round_trip.reputation == 50
    assert round_trip.job_security == 50
    assert round_trip.career_history[0].team_id == "DAL"
    assert round_trip.traits == ["aggressive"]


def test_missing_gm_data_loads_with_safe_defaults():
    facade = GameFacade(save_name="unit_test_gm_profile_legacy_load")
    facade.new_game(team_id="CHI")
    save_dir = Path(".test_tmp") / "gm_profile"
    save_dir.mkdir(parents=True, exist_ok=True)
    save_path = save_dir / "legacy_league.json"
    payload = facade.league.to_dict()
    payload.pop("user_gm", None)
    payload.pop("controlled_team_id", None)
    payload.get("time_engine", {}).pop("controlled_team_id", None)
    with save_path.open("w", encoding="utf-8") as f:
        json.dump({"schema_version": 1, "league": payload}, f, indent=2)

    loaded = GameFacade(save_name="unit_test_gm_profile_legacy_loaded")
    loaded.load(save_path)

    gm = loaded.league.user_gm
    assert gm is not None
    assert gm.name == "User GM"
    assert gm.current_team_id == loaded.league.user_team_id
    assert loaded.league.controlled_team_id == loaded.league.user_team_id


def test_calendar_dashboard_uses_real_gm_profile_data():
    facade = GameFacade(save_name="unit_test_gm_profile_dashboard")
    facade.new_game()
    facade.league.user_gm.name = "Riley Hart"
    facade.league.user_gm.reputation = 67
    facade.league.user_gm.job_security = 41

    payload = facade.get_calendar_dashboard()

    assert payload["gm"]["name"] == "Riley Hart"
    assert payload["gm"]["reputation"] == 67
    assert payload["gm"]["job_security"] == 41


def test_gm_profile_endpoint_returns_compact_data():
    facade = GameFacade(save_name="unit_test_gm_profile_endpoint")
    facade.new_game()

    payload = facade.get_gm_profile()

    assert payload["ok"] is True
    gm = payload["gm"]
    assert gm["gm_id"]
    assert gm["name"] == "User GM"
    assert gm["current_role"] == "General Manager"
    assert isinstance(gm["career_history"], list)
    payload_json = json.dumps(payload)
    for forbidden_key in ('"league"', '"teams"', '"roster"', '"players"', '"free_agents"'):
        assert forbidden_key not in payload_json


def test_gm_profile_http_endpoint_returns_compact_data():
    facade = GameFacade(save_name="unit_test_gm_profile_http")
    facade.new_game()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(f"http://{host}:{port}/gm_profile", timeout=10) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["ok"] is True
        assert payload["gm"]["gm_id"]
        assert "league" not in payload
    finally:
        server.shutdown()
        server.server_close()


def test_gm_profile_rpc_endpoint_returns_compact_data():
    stdout = sys.stdout
    try:
        rpc = RpcServer(save_path="savegame.json", parent_pid=0)
        rpc.facade.new_game()

        status, payload = rpc.dispatch("GET", "/gm_profile", {})

        assert status == 200
        assert payload["ok"] is True
        assert payload["gm"]["gm_id"]
    finally:
        sys.stdout = stdout


def test_http_new_game_accepts_optional_gm_name_and_team_id():
    facade = GameFacade(save_name="unit_test_gm_profile_http_new_game")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        data = json.dumps({"gm_name": "Jordan Vale", "team_id": "CHI"}).encode("utf-8")
        with urllib.request.urlopen(f"http://{host}:{port}/new_game", data=data, timeout=10) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["ok"] is True
        assert payload["user"]["gm_name"] == "Jordan Vale"
        assert payload["user"]["team_id"] == facade.league.abbr_to_id["CHI"]
    finally:
        server.shutdown()
        server.server_close()


def test_state_snapshot_includes_compact_gm_data_only():
    facade = GameFacade(save_name="unit_test_gm_profile_state")
    facade.new_game(gm_name="Morgan Lee", team_id="CHI")
    expected_team_id = facade.league.abbr_to_id["CHI"]

    payload = facade.get_state_snapshot("savegame.json")

    assert payload["user"] == {
        "gm_id": facade.league.user_gm.gm_id,
        "gm_name": "Morgan Lee",
        "team_id": expected_team_id,
        "current_role": "General Manager",
    }
    assert "career_history" not in json.dumps(payload["user"])


def test_state_snapshot_read_only_shape_remains_compact():
    facade = GameFacade(save_name="unit_test_gm_profile_state_compact")
    facade.new_game()
    payload = facade.get_state_snapshot("savegame.json")

    assert "league" not in payload
    assert "time_engine" not in payload
    assert "career_history" not in json.dumps(payload)


def test_missing_gm_data_snapshot_defaults_are_safe():
    facade = GameFacade(save_name="unit_test_gm_profile_snapshot_defaults")
    league = LeagueManager()
    league.user_team_id = None
    league.controlled_team_id = None
    league.user_gm = None
    facade.league = league
    facade.calendar = league.calendar
    facade.season_manager = type("_DummySeason", (), {"schedule_by_week": {}})()
    facade._time_engine = None

    payload = facade.get_state_snapshot("savegame.json")

    assert payload["user"]["gm_name"] == "User GM"
    assert payload["user"]["team_id"] is None
