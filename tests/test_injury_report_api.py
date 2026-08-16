import datetime
import json
import threading
import urllib.request

from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade


def test_injury_report_endpoint_filters_and_days_remaining():
    facade = GameFacade(save_name="unit_test_injury_report")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        facade.new_game()
        team = facade.league.teams[0]
        injured = team.roster[0]
        healthy = team.roster[1]

        start_date = facade.calendar.current_date
        end_date = start_date + datetime.timedelta(days=5)

        injured.injury_status = "out"
        injured.injury_name = "Test Injury"
        injured.injury_start_date = start_date
        injured.injury_end_date = end_date
        injured.on_injured_reserve = False

        healthy.injury_status = "healthy"
        healthy.injury_name = None
        healthy.injury_start_date = None
        healthy.injury_end_date = None
        healthy.on_injured_reserve = False

        with urllib.request.urlopen(
            f"http://{host}:{port}/injury_report?team_id={team.id}"
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))

        entries = payload.get("entries", [])
        assert isinstance(entries, list)

        injured_entry = next(
            (entry for entry in entries if entry.get("player_id") == injured.id), None
        )
        assert injured_entry is not None
        assert injured_entry.get("days_remaining") == 5
        assert all(entry.get("player_id") != healthy.id for entry in entries)
    finally:
        server.shutdown()
        server.server_close()
