import datetime
import json
import threading
import urllib.request

from gridiron_gm_pkg.api.rpc_server import RpcServer
from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.roster.roster_rules import review_roster_rules
from gridiron_gm_pkg.simulation.systems.time_engine import make_game_id


def _setup_single_game_day(
    save_name: str,
    *,
    kickoff: str = "1:00 PM",
    start_hour: int = 8,
) -> tuple[GameFacade, str]:
    facade = GameFacade(save_name=save_name)
    facade.new_game()
    teams = facade.league.teams
    home_team = teams[0]
    away_team = teams[1]
    facade.league.user_team_id = home_team.id
    facade.league.controlled_team_id = home_team.id
    facade.calendar.current_date = facade.calendar.nfl_week1_start_date
    facade.calendar.current_week = 1
    facade.calendar.update_phase()
    game = {
        "week": 1,
        "calendar_week": 1,
        "season_type": "preseason",
        "season_week": 1,
        "week_key": "preseason:1",
        "kickoff": kickoff,
        "home_id": home_team.id,
        "away_id": away_team.id,
        "label": "Preseason",
        "day": facade.calendar.current_date.strftime("%A"),
        "date": facade.calendar.current_date.isoformat(),
    }
    schedule = {"1": [game]}
    facade.season_manager.schedule_by_week = schedule
    facade.season_manager.results_by_week = {}
    facade._time_engine = None
    engine = facade._get_time_engine()
    engine.user_team_id = home_team.id
    engine.schedule_by_week = schedule
    engine.queue.remove_matching(lambda _event: True)
    engine.clock.current_date = facade.calendar.current_date
    engine.clock.hour = start_hour
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()
    return facade, make_game_id("1", home_team.id, away_team.id)


def _clone_player(player, *, name_suffix: str, position: str | None = None) -> Player:
    clone = Player.from_dict(player.to_dict())
    clone.id = f"{player.id}-{name_suffix}"
    clone.name = f"{player.name} {name_suffix}"
    if position is not None:
        clone.position = position
    clone.current_team = getattr(player, "current_team", None)
    return clone


def _find_first_player(team, position: str):
    for player in team.roster:
        if str(getattr(player, "position", "")).upper() == str(position).upper():
            return player
    raise AssertionError(f"missing {position} on roster")


def _keep_only_position_count(team, position: str, count: int) -> None:
    normalized = str(position).upper()
    kept = 0
    new_roster = []
    for player in team.roster:
        if str(getattr(player, "position", "")).upper() != normalized:
            new_roster.append(player)
            continue
        if kept < count:
            new_roster.append(player)
            kept += 1
    team.roster = new_roster
    team.generate_depth_chart()


def _remove_available_players(team, position: str) -> None:
    normalized = str(position).upper()
    for player in team.roster:
        if str(getattr(player, "position", "")).upper() != normalized:
            continue
        player.injury_status = "out"
        player.on_injured_reserve = False


def _position_count(team, position: str) -> int:
    normalized = str(position).upper()
    return sum(1 for player in team.roster if str(getattr(player, "position", "")).upper() == normalized)


def _ensure_position_count(team, position: str, target_count: int, *, protected_positions: set[str] | None = None) -> None:
    normalized = str(position).upper()
    protected = {str(item).upper() for item in (protected_positions or set())}
    current = _position_count(team, normalized)
    if current >= target_count:
        return
    for player in team.roster:
        player_position = str(getattr(player, "position", "")).upper()
        if player_position == normalized:
            continue
        if player_position == "LT" and normalized != "LT":
            continue
        if player_position in protected:
            continue
        player.position = normalized
        current += 1
        if current >= target_count:
            break
    team.generate_depth_chart()


def test_roster_review_detects_active_roster_over_limit_and_creates_blocking_decision():
    facade = GameFacade(save_name="unit_test_roster_over_limit")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.roster.append(_clone_player(team.roster[0], name_suffix="Extra"))
    team.generate_depth_chart()

    preview = review_roster_rules(facade.league, team.id)
    result = facade.review_user_roster()

    assert any(item["rule_id"] == "active_roster_over_limit" for item in preview["hard_violations"])
    assert any(item["rule_id"] == "active_roster_over_limit" for item in result["hard_violations"])
    assert len(result["created_decisions"]) == 1
    assert result["created_decisions"][0]["decision_type"] == "roster_rule_violation"
    assert facade.get_decisions()["blocking_decision_count"] == 1


def test_roster_rule_blocking_decision_stops_continue_before_time_advances():
    facade = GameFacade(save_name="unit_test_roster_review_continue_block")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.roster.append(_clone_player(team.roster[0], name_suffix="Extra"))
    team.generate_depth_chart()
    start_date = facade.calendar.current_date.isoformat()
    start_hour = facade._get_time_engine().clock.hour

    facade.review_user_roster()
    payload = facade.continue_until_pause(max_hours=24)

    assert payload["stop_reason"] == "blocking_decision"
    assert payload["advanced_hours"] == 0
    assert payload["current_date"] == start_date
    assert payload["clock"]["hour"] == start_hour


def test_soft_lt_depth_advisory_creates_non_blocking_assistant_gm_notification():
    facade = GameFacade(save_name="unit_test_roster_lt_advisory")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    _keep_only_position_count(team, "LT", 1)

    result = facade.review_user_roster()
    inbox = facade.get_inbox()

    assert any(item["rule_id"] == "thin_lt_depth" for item in result["advisories"])
    note = next(msg for msg in inbox["messages"] if msg["payload"].get("rule_id") == "thin_lt_depth")
    assert note["category"] == "assistant_gm"
    assert note["blocks_advancement"] is False
    assert inbox["blocking_decision_count"] == 0


def test_soft_qb_k_p_advisories_do_not_block_advancement():
    facade = GameFacade(save_name="unit_test_roster_soft_specialists")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    _ensure_position_count(team, "QB", 4, protected_positions={"K", "P"})
    _ensure_position_count(team, "K", 2, protected_positions={"QB", "P"})
    _ensure_position_count(team, "P", 2, protected_positions={"QB", "K"})

    result = facade.review_user_roster()
    payload = facade.continue_until_pause(max_hours=24)

    rule_ids = {item["rule_id"] for item in result["advisories"]}
    assert {"excess_qb_depth", "excess_k_depth", "excess_p_depth"}.issubset(rule_ids)
    assert payload["stop_reason"] != "blocking_decision"
    assert payload["advanced_hours"] > 0


def test_duplicate_roster_review_does_not_create_duplicate_decisions_or_advisories():
    facade = GameFacade(save_name="unit_test_roster_review_duplicates")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    _keep_only_position_count(team, "LT", 1)
    while len(team.roster) <= getattr(team, "MAX_ROSTER_SIZE", 53):
        team.roster.append(_clone_player(team.roster[0], name_suffix=f"Extra{len(team.roster)}"))
    team.generate_depth_chart()

    first = facade.review_user_roster()
    second = facade.review_user_roster()
    inbox = facade.get_inbox()

    assert len(first["created_decisions"]) == 1
    assert second["created_decisions"] == []
    assert len([msg for msg in inbox["messages"] if msg["payload"].get("rule_id") == "thin_lt_depth"]) == 1
    assert second["created_notifications"] == []


def test_roster_review_http_and_rpc_return_compact_result():
    facade = GameFacade(save_name="unit_test_roster_review_http")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.roster.append(_clone_player(team.roster[0], name_suffix="Extra"))
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/roster/review",
            data=json.dumps({}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["ok"] is True
        assert set(payload.keys()) == {
            "ok",
            "team_id",
            "hard_violations",
            "advisories",
            "created_decisions",
            "created_notifications",
        }
    finally:
        server.shutdown()
        server.server_close()

    rpc = RpcServer(save_path="", parent_pid=0)
    rpc.facade = GameFacade(save_name="unit_test_roster_review_rpc")
    rpc.facade.new_game()
    status, rpc_payload = rpc.dispatch("POST", "/roster/review", {})
    assert status == 200
    assert rpc_payload["ok"] is True


def test_state_and_calendar_dashboard_remain_read_only_during_roster_issues():
    facade = GameFacade(save_name="unit_test_roster_review_readonly")
    facade.new_game()
    team = facade.league.id_to_team[facade.league.user_team_id]
    team.roster.append(_clone_player(team.roster[0], name_suffix="Extra"))
    _keep_only_position_count(team, "LT", 1)
    engine = facade._get_time_engine()
    before_decisions = len(engine.get_open_decisions())
    before_inbox = len(engine.get_inbox())

    state_payload = facade.get_state_snapshot("savegame.json")
    calendar_payload = facade.get_calendar_dashboard()

    assert len(engine.get_open_decisions()) == before_decisions
    assert len(engine.get_inbox()) == before_inbox
    assert set(state_payload["inbox_summary"].keys()) == {"unread_count", "blocking_decision_count", "latest"}
    assert "league" not in calendar_payload


def test_game_day_check_blocks_if_no_healthy_qb_k_or_p():
    facade, game_id = _setup_single_game_day("unit_test_roster_gameday_block", kickoff="1:00 PM", start_hour=12)
    team = facade.league.id_to_team[facade.league.user_team_id]
    for position in ("QB", "K", "P"):
        _remove_available_players(team, position)

    payload = facade.continue_until_pause(max_hours=4)
    engine = facade._get_time_engine()
    open_decisions = engine.get_open_decisions()
    rule_ids = {decision.payload.get("rule_id") for decision in open_decisions}

    assert payload["stop_reason"] == "blocking_decision"
    assert engine._find_result(game_id) is None
    assert {"no_healthy_qb_for_game", "no_healthy_k_for_game", "no_healthy_p_for_game"} == rule_ids


def test_legal_but_imbalanced_roster_does_not_block_game_advancement():
    facade, game_id = _setup_single_game_day("unit_test_roster_legal_imbalanced", kickoff="1:00 PM", start_hour=12)
    team = facade.league.id_to_team[facade.league.user_team_id]
    _keep_only_position_count(team, "LT", 1)

    result = facade.continue_until_pause(max_hours=4)

    assert result["stop_reason"] != "blocking_decision"
    assert facade._get_time_engine()._find_result(game_id) is not None
