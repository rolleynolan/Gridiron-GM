import datetime
import json
import sys
import threading
import urllib.request

from gridiron_gm_pkg.api.rpc_server import RpcServer
from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.systems.time_engine import InboxMessage, TimeEngine


class _DummySeason:
    schedule_by_week = {}


def test_inbox_payload_json_serializable():
    league = LeagueManager()
    team = Team("Alpha", "Alpha City", "ALP", conference="Nova", division="East")
    league.add_team(team)
    league.user_team_id = team.id
    calendar = league.calendar

    engine = TimeEngine(league, calendar, schedule_by_week={})
    engine.inboxes.setdefault(team.id, []).append(
        InboxMessage(
            id=1,
            date=calendar.current_date,
            hour=9,
            category="Game",
            subject="Kickoff",
            body="Sim when ready.",
            requires_ack=True,
            actions=[
                {"label": "Sim Game", "when": datetime.datetime(2025, 1, 1, 9, 0)},
                object(),
            ],
            read=False,
        )
    )

    facade = GameFacade()
    facade.league = league
    facade.calendar = calendar
    facade.season_manager = _DummySeason()
    facade._time_engine = engine

    payload = facade.get_inbox(team.id)
    json.dumps(payload)


def test_inbox_http_endpoint_payload_json_serializable():
    facade = GameFacade()
    facade.new_game()
    team_id = facade.league.user_team_id
    engine = facade._get_time_engine()
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=1,
            date=facade.calendar.current_date,
            hour=9,
            category="Game",
            subject="Kickoff: Playoff Test",
            body="Sim the playoff game when ready.",
            requires_ack=True,
            actions=[{"type": "SIM_GAME", "label": "Sim Game", "game_id": "playoffs|home|away"}],
            read=False,
        )
    )

    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(f"http://{host}:{port}/inbox", timeout=10) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["ok"] is True
        assert payload["messages"]
        json.dumps(payload)
    finally:
        server.shutdown()
        server.server_close()


def test_inbox_rpc_endpoint_payload_json_serializable():
    stdout = sys.stdout
    try:
        rpc = RpcServer(save_path="", parent_pid=0)
        rpc.facade.new_game()
        team_id = rpc.facade.league.user_team_id
        engine = rpc.facade._get_time_engine()
        engine.inboxes.setdefault(team_id, []).append(
            InboxMessage(
                id=1,
                date=rpc.facade.calendar.current_date,
                hour=9,
                category="Game",
                subject="Kickoff: Playoff Test",
                body="Sim the playoff game when ready.",
                requires_ack=True,
                actions=[{"type": "SIM_GAME", "label": "Sim Game", "game_id": "playoffs|home|away"}],
                read=False,
            )
        )

        status, payload = rpc.dispatch("GET", "/inbox", {})
        assert status == 200
        assert payload["ok"] is True
        assert payload["messages"]
        json.dumps(payload)
    finally:
        sys.stdout = stdout
