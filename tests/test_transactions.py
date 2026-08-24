import datetime
import json
import threading
import urllib.request

from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade


def _free_agent() -> Player:
    return Player("Free Agent", "WR", 25, datetime.date(2000, 1, 1), "U", "USA", 88, 70)


def test_sign_and_release_move_player_between_pool_and_roster():
    facade = GameFacade(save_name="unit_test_transactions")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.remove_player(team.roster[-1])
    team.salary_cap = 1_000_000
    player = _free_agent()
    facade.league.free_agents.append(player)

    signed = facade.sign_free_agent(player.id, {"years": 2, "salary_per_year": 100_000, "guaranteed": 50_000})

    assert signed["ok"] is True
    assert player in team.roster
    assert player not in facade.league.free_agents
    assert signed["summary"]["payroll"] == 100_000

    released = facade.release_player(player.id)

    assert released["ok"] is True
    assert player not in team.roster
    assert player in facade.league.free_agents
    assert player.contract is None
    assert [entry["action"] for entry in facade.league.transaction_log] == ["sign", "release"]


def test_sign_rejects_cap_overage_without_moving_player():
    facade = GameFacade(save_name="unit_test_transactions_cap")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.remove_player(team.roster[-1])
    team.salary_cap = 1
    player = _free_agent()
    facade.league.free_agents.append(player)

    result = facade.sign_free_agent(player.id, {"years": 1, "salary_per_year": 2})

    assert result["ok"] is False
    assert result["error"] == "contract_rejected"
    assert player in facade.league.free_agents
    assert player not in team.roster


def test_transaction_log_survives_league_serialization():
    facade = GameFacade(save_name="unit_test_transactions_save")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.remove_player(team.roster[-1])
    team.salary_cap = 1_000_000
    player = _free_agent()
    facade.league.free_agents.append(player)
    facade.sign_free_agent(player.id, {"years": 1, "salary_per_year": 10})

    from gridiron_gm_pkg.simulation.entities.league import LeagueManager

    restored = LeagueManager.from_dict(facade.league.to_dict())

    assert restored.transaction_log[0]["action"] == "sign"


def test_transaction_http_endpoints_sign_and_release_player():
    facade = GameFacade(save_name="unit_test_transactions_http")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.salary_cap = 1_000_000
    player = team.roster[-1]
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    base_url = f"http://{host}:{port}"
    try:
        request = urllib.request.Request(
            f"{base_url}/transactions/release_player",
            data=json.dumps({"player_id": player.id}).encode("utf-8"),
            headers={"Content-Type": "application/json"},
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            released = json.loads(response.read().decode("utf-8"))
        assert released["ok"] is True

        with urllib.request.urlopen(f"{base_url}/free_agents", timeout=30) as response:
            free_agents = json.loads(response.read().decode("utf-8"))
        assert any(entry["player_id"] == player.id for entry in free_agents["players"])

        request = urllib.request.Request(
            f"{base_url}/transactions/sign_free_agent",
            data=json.dumps({"player_id": player.id, "contract": {"years": 1, "salary_per_year": 10}}).encode("utf-8"),
            headers={"Content-Type": "application/json"},
        )
        with urllib.request.urlopen(request, timeout=30) as response:
            signed = json.loads(response.read().decode("utf-8"))
        assert signed["ok"] is True
    finally:
        server.shutdown()
        server.server_close()
