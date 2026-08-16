import datetime
import json
import threading
import urllib.request

from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.systems.time_engine import InboxMessage, make_game_id


def _setup_single_game_day(
    save_name: str,
    *,
    include_day: bool = True,
    include_date: bool = False,
    kickoff: str = "1:00 PM",
    start_offset_days: int = 0,
    start_hour: int = 8,
) -> tuple[GameFacade, str]:
    facade = GameFacade(save_name=save_name)
    facade.new_game()
    teams = facade.league.teams
    home_team = teams[0]
    away_team = teams[1]
    facade.league.user_team_id = home_team.id
    facade.calendar.current_date = facade.calendar.nfl_week1_start_date + datetime.timedelta(days=start_offset_days)
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
    }
    if include_day:
        game["day"] = facade.calendar.current_date.strftime("%A")
    if include_date:
        game["date"] = facade.calendar.current_date.isoformat()
    schedule = {"1": [game]}
    facade.season_manager.schedule_by_week = schedule
    facade.season_manager.results_by_week = {}
    facade._time_engine = None
    engine = facade._get_time_engine()
    engine.schedule_by_week = schedule
    engine.queue.remove_matching(lambda _event: True)
    engine.clock.current_date = facade.calendar.current_date
    engine.clock.hour = start_hour
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()
    return facade, make_game_id("1", home_team.id, away_team.id)


def _result_count(facade: GameFacade) -> int:
    return sum(len(games) for games in facade.season_manager.results_by_week.values())


def _setup_multi_day_preseason_window(save_name: str) -> tuple[GameFacade, str, str]:
    facade = GameFacade(save_name=save_name)
    facade.new_game()
    teams = facade.league.teams
    user_team = teams[0]
    opponent_one = teams[1]
    opponent_two = teams[2]
    facade.league.user_team_id = user_team.id

    start_date = facade.calendar.nfl_week1_start_date
    facade.calendar.current_date = start_date
    facade.calendar.current_week = 1
    facade.calendar.update_phase()

    today_game = {
        "week": 1,
        "calendar_week": 1,
        "season_type": "preseason",
        "season_week": 1,
        "week_key": "preseason:1",
        "kickoff": "7:00 PM",
        "home_id": user_team.id,
        "away_id": opponent_one.id,
        "label": "Preseason",
        "day": start_date.strftime("%A"),
        "date": start_date.isoformat(),
    }
    tomorrow = start_date + datetime.timedelta(days=1)
    tomorrow_game = {
        "week": 1,
        "calendar_week": 1,
        "season_type": "preseason",
        "season_week": 1,
        "week_key": "preseason:1",
        "kickoff": "7:00 PM",
        "home_id": user_team.id,
        "away_id": opponent_two.id,
        "label": "Preseason",
        "day": tomorrow.strftime("%A"),
        "date": tomorrow.isoformat(),
    }
    schedule = {"1": [today_game, tomorrow_game]}
    facade.season_manager.schedule_by_week = schedule
    facade.season_manager.results_by_week = {}
    facade._time_engine = None
    engine = facade._get_time_engine()
    engine.schedule_by_week = schedule
    engine.queue.remove_matching(lambda _event: True)
    engine.clock.current_date = start_date
    engine.clock.hour = 8
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()
    return (
        facade,
        make_game_id("1", user_team.id, opponent_one.id),
        make_game_id("1", user_team.id, opponent_two.id),
    )


def _setup_background_only_day(save_name: str, *, start_hour: int = 8) -> GameFacade:
    facade = GameFacade(save_name=save_name)
    facade.new_game()
    facade.league.user_team_id = facade.league.teams[0].id
    facade.calendar.current_date = facade.calendar.nfl_week1_start_date
    facade.calendar.current_week = 1
    facade.calendar.update_phase()
    facade.season_manager.schedule_by_week = {}
    facade.season_manager.results_by_week = {}
    facade._time_engine = None
    engine = facade._get_time_engine()
    engine.schedule_by_week = {}
    engine.queue.remove_matching(lambda _event: True)
    engine.clock.current_date = facade.calendar.current_date
    engine.clock.hour = start_hour
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()
    return facade


def test_advance_day_processes_scheduled_game_event():
    facade, game_id = _setup_single_game_day("unit_test_advance_day_processes_game")
    previous_date = facade.calendar.current_date

    payload = facade.advance_day()

    assert payload["status"] == "advanced"
    assert payload["current_date"] == (previous_date + datetime.timedelta(days=1)).isoformat()
    assert _result_count(facade) == 1
    assert facade._get_time_engine()._find_result(game_id) is not None
    assert payload["debug_game_events"]["scheduled_games_found"] == 1
    assert payload["debug_game_events"]["game_events_seeded"] == 1
    assert payload["debug_game_events"]["game_results_created"] == 1


def test_advance_day_does_not_leave_unprocessed_game_event_behind():
    facade, _game_id = _setup_single_game_day("unit_test_advance_day_no_leftover_game")
    previous_date = facade.calendar.current_date

    facade.advance_day()

    remaining = [
        event for event in facade._get_time_engine().queue.events()
        if event.date <= previous_date and event.type in {"GameKickoff", "GameWrap"}
    ]
    assert remaining == []


def test_game_event_seeding_uses_default_game_day_when_schedule_day_missing():
    facade, game_id = _setup_single_game_day(
        "unit_test_game_event_seeding_fallback_day",
        include_day=False,
        include_date=False,
        start_offset_days=6,
    )

    events_today = [
        event for event in facade._get_time_engine().queue.events()
        if event.date == facade.calendar.current_date and event.type == "GameKickoff"
    ]

    assert len(events_today) == 1
    assert events_today[0].payload["game_id"] == game_id


def test_game_event_seeding_uses_explicit_schedule_date():
    facade, game_id = _setup_single_game_day(
        "unit_test_game_event_seeding_explicit_date",
        include_day=False,
        include_date=True,
    )

    events_today = [
        event for event in facade._get_time_engine().queue.events()
        if event.date == facade.calendar.current_date and event.type == "GameKickoff"
    ]

    assert len(events_today) == 1
    assert events_today[0].payload["game_id"] == game_id


def test_advance_day_stops_for_required_user_attention_before_next_day():
    facade, _game_id = _setup_single_game_day("unit_test_advance_day_stops_attention")
    engine = facade._get_time_engine()
    team_id = facade.league.user_team_id
    start_date = facade.calendar.current_date
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=999,
            date=start_date,
            hour=8,
            category="News",
            subject="Needs review",
            body="Action required.",
            requires_ack=True,
            actions=[],
            read=False,
        )
    )

    payload = facade.advance_day()

    assert payload["status"] == "stopped_for_user_attention"
    assert payload["stopped_for_user_attention"] is True
    assert payload["current_date"] == start_date.isoformat()
    assert _result_count(facade) == 0


def test_advance_day_processes_overdue_unprocessed_game_events_first():
    facade, game_id = _setup_single_game_day("unit_test_advance_day_overdue")
    engine = facade._get_time_engine()
    stale_date = facade.calendar.current_date
    next_day = stale_date + datetime.timedelta(days=1)
    facade.calendar.current_date = next_day
    facade.calendar.update_phase()
    engine.clock.current_date = next_day
    engine.clock.hour = 8

    payload = facade.advance_day()

    assert payload["status"] == "advanced"
    assert facade._get_time_engine()._find_result(game_id) is not None
    assert _result_count(facade) == 1
    remaining = [
        event for event in facade._get_time_engine().queue.events()
        if event.date <= next_day and event.type == "GameKickoff"
    ]
    assert remaining == []


def test_game_not_simulated_twice_when_manual_sim_overlaps_advance_day():
    facade, game_id = _setup_single_game_day("unit_test_advance_day_no_double_sim")
    engine = facade._get_time_engine()

    first = engine.simulate_scheduled_game(game_id)
    assert first.get("already_simmed") is not True

    payload = facade.advance_day()

    assert payload["status"] == "advanced"
    assert _result_count(facade) == 1
    assert facade._get_time_engine()._find_result(game_id) is not None
    assert payload["debug_game_events"]["skipped_games"]
    assert payload["debug_game_events"]["skipped_games"][0]["reason"] == "already_completed_result_exists"


def test_advance_to_end_of_day_and_advance_day_match_on_game_day():
    facade_day, game_id_day = _setup_single_game_day("unit_test_advance_day_match_day")
    facade_eod, game_id_eod = _setup_single_game_day("unit_test_advance_day_match_eod")

    day_payload = facade_day.advance_day()
    eod_payload = facade_eod.advance_to_end_of_day(max_hours=24)

    assert day_payload["status"] == "advanced"
    assert _result_count(facade_day) == 1
    assert facade_day._get_time_engine()._find_result(game_id_day) is not None
    assert facade_eod._get_time_engine()._find_result(game_id_eod) is not None
    assert _result_count(facade_eod) == 1
    assert day_payload["current_date"] == facade_eod.calendar.current_date.isoformat()


def test_advance_to_next_event_simulates_game_when_next_event_is_kickoff():
    facade, game_id = _setup_single_game_day(
        "unit_test_advance_to_next_event_processes_game",
        start_hour=12,
    )
    facade._get_time_engine().queue.remove_matching(lambda event: event.type != "GameKickoff")

    payload = facade.advance_to_next_event(max_hours=24)

    assert payload["status"] == "advanced"
    assert _result_count(facade) == 1
    assert facade._get_time_engine()._find_result(game_id) is not None


def test_advance_one_week_processes_weekly_games():
    facade, game_id = _setup_single_game_day("unit_test_advance_one_week_processes_game")

    payload = facade.advance_one_week(max_hours=24 * 8)

    assert payload["status"] == "advanced"
    assert _result_count(facade) == 1
    assert facade._get_time_engine()._find_result(game_id) is not None


def test_continue_until_pause_simulates_game_at_kickoff_time():
    facade, game_id = _setup_single_game_day(
        "unit_test_continue_until_pause_processes_game",
        kickoff="7:00 PM",
        start_hour=19,
    )

    result = facade.continue_until_pause(max_hours=4)

    assert result["paused"] is False
    assert _result_count(facade) == 1
    assert facade._get_time_engine()._find_result(game_id) is not None
    assert result["debug_game_events"]["game_events_due"] == 1
    assert result["debug_game_events"]["game_events_processed"] == 1
    assert result["debug_game_events"]["game_results_created"] == 1
    remaining = [
        event for event in facade._get_time_engine().queue.events()
        if event.type == "GameKickoff" and event.date <= facade._get_time_engine().clock.current_date
    ]
    assert remaining == []


def test_continue_endpoint_processes_only_one_meaningful_event():
    facade = _setup_background_only_day("unit_test_continue_endpoint_single_event")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 720}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["status"] == "advanced"
        assert payload["paused"] is True
        assert payload["stop_reason"] == "end_of_day"
        assert payload["advanced_hours"] > 1
        assert payload["current_date"] == facade.calendar.current_date.isoformat()
        assert payload["clock"]["hour"] == 0
        assert _result_count(facade) == 0
        assert [event["type"] for event in payload["processed_events"]] == [
            "InboxCheck",
            "TrainingSlot",
            "TrainingSlot",
            "TrainingSlot",
        ]
    finally:
        server.shutdown()
        server.server_close()


def test_continue_endpoint_skips_multiple_background_events():
    facade = _setup_background_only_day("unit_test_continue_endpoint_multi_background")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 720}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["paused"] is True
        assert payload["stop_reason"] == "end_of_day"
        assert payload["advanced_hours"] >= 16
        assert len(payload["processed_events"]) >= 4
        assert all(event["type"] in {"InboxCheck", "TrainingSlot", "Travel"} for event in payload["processed_events"])
    finally:
        server.shutdown()
        server.server_close()


def test_continue_endpoint_at_kickoff_simulates_one_game_then_stops():
    facade, today_game_id, tomorrow_game_id = _setup_multi_day_preseason_window(
        "unit_test_continue_endpoint_kickoff_once"
    )
    engine = facade._get_time_engine()
    engine.clock.hour = 19
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 720}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["status"] == "advanced"
        assert payload["paused"] is True
        assert payload["stop_reason"] == "game_simulated"
        assert facade._get_time_engine()._find_result(today_game_id) is not None
        assert facade._get_time_engine()._find_result(tomorrow_game_id) is None
        assert _result_count(facade) == 1
        assert payload["debug_game_events"]["game_results_created"] == 1
        assert payload["processed_events"]
        assert payload["processed_events"][-1]["type"] == "GameKickoff"
    finally:
        server.shutdown()
        server.server_close()


def test_continue_endpoint_before_kickoff_advances_to_kickoff_simulates_and_stops():
    facade, today_game_id, tomorrow_game_id = _setup_multi_day_preseason_window(
        "unit_test_continue_endpoint_before_kickoff"
    )
    engine = facade._get_time_engine()
    engine.clock.hour = 18
    engine.queue.remove_matching(lambda event: event.type != "GameKickoff")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 720}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["status"] == "advanced"
        assert payload["paused"] is True
        assert payload["stop_reason"] == "game_simulated"
        assert payload["advanced_hours"] == 1
        assert payload["clock"]["hour"] == 19
        assert facade._get_time_engine()._find_result(today_game_id) is not None
        assert facade._get_time_engine()._find_result(tomorrow_game_id) is None
        assert _result_count(facade) == 1
    finally:
        server.shutdown()
        server.server_close()


def test_continue_endpoint_stops_on_phase_change():
    facade = _setup_background_only_day("unit_test_continue_endpoint_phase_change", start_hour=8)
    engine = facade._get_time_engine()
    engine.queue.remove_matching(lambda _event: True)
    engine.last_phase_token = "preseason|old"
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 24}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["paused"] is True
        assert payload["stop_reason"] == "phase_change"
        assert payload["processed_events"]
        assert payload["processed_events"][0]["type"] == "PhaseChange"
    finally:
        server.shutdown()
        server.server_close()


def test_continue_endpoint_stops_for_requires_user_attention():
    facade = _setup_background_only_day("unit_test_continue_endpoint_attention")
    engine = facade._get_time_engine()
    team_id = facade.league.user_team_id
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=9999,
            date=facade.calendar.current_date,
            hour=8,
            category="News",
            subject="Needs review",
            body="Action required.",
            requires_ack=True,
            actions=[],
            read=False,
        )
    )
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 24}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["status"] == "stopped_for_user_attention"
        assert payload["paused"] is True
        assert payload["stop_reason"] == "user_attention_required"
        assert payload["advanced_hours"] == 0
    finally:
        server.shutdown()
        server.server_close()


def test_blocking_decision_stops_continue_before_time_advances():
    facade = _setup_background_only_day("unit_test_continue_blocking_decision")
    start_date = facade.calendar.current_date
    start_hour = facade._get_time_engine().clock.hour
    facade.create_decision(
        category="FrontOffice",
        decision_type="test_block",
        title="Blocking decision",
        message="Stop now",
        blocks_advancement=True,
        options=[{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
    )

    payload = facade.continue_until_pause(max_hours=24)

    assert payload["status"] == "stopped_for_user_attention"
    assert payload["stop_reason"] == "blocking_decision"
    assert payload["advanced_hours"] == 0
    assert payload["current_date"] == start_date.isoformat()
    assert payload["clock"]["hour"] == start_hour
    assert payload["blocking_decision_count"] == 1


def test_resolving_blocking_decision_allows_continue_to_advance_again():
    facade = _setup_background_only_day("unit_test_continue_after_decision_resolved")
    created = facade.create_decision(
        category="FrontOffice",
        decision_type="test_block",
        title="Blocking decision",
        message="Stop now",
        blocks_advancement=True,
        options=[{"option_id": "acknowledge", "label": "Acknowledge", "result": "acknowledge"}],
    )
    decision_id = created["decision"]["decision_id"]

    blocked = facade.continue_until_pause(max_hours=24)
    resolved = facade.resolve_decision(decision_id, "acknowledge")
    advanced = facade.continue_until_pause(max_hours=24)

    assert blocked["stop_reason"] == "blocking_decision"
    assert resolved["ok"] is True
    assert advanced["advanced_hours"] > 0
    assert advanced["stop_reason"] != "blocking_decision"


def test_non_blocking_notifications_do_not_stop_continue():
    facade = _setup_background_only_day("unit_test_continue_non_blocking_notification")
    engine = facade._get_time_engine()
    team_id = facade.league.user_team_id
    engine.inboxes.setdefault(team_id, []).append(
        InboxMessage(
            id=8080,
            date=facade.calendar.current_date,
            hour=8,
            category="News",
            subject="FYI",
            body="Informational only.",
            requires_ack=False,
            read=False,
        )
    )

    payload = facade.continue_until_pause(max_hours=24)

    assert payload["stop_reason"] != "blocking_decision"
    assert payload["advanced_hours"] > 0


def test_continue_endpoint_safety_limit_prevents_runaway():
    facade = _setup_background_only_day("unit_test_continue_endpoint_safety_limit")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 1}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["paused"] is True
        assert payload["stop_reason"] == "safety_limit"
    finally:
        server.shutdown()
        server.server_close()


def test_continue_until_pause_http_endpoint_preserves_long_run_mode_when_requested():
    facade, game_id = _setup_single_game_day(
        "unit_test_continue_until_pause_http_endpoint_legacy_mode",
        kickoff="7:00 PM",
        start_hour=19,
    )
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(
            f"http://{host}:{port}/continue_until_pause",
            data=json.dumps({"max_hours": 4, "mode": "until_pause"}).encode("utf-8"),
            timeout=30,
        ) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["paused"] is False
        assert payload["debug_game_events"]["game_results_created"] == 1
        assert facade._get_time_engine()._find_result(game_id) is not None
        assert _result_count(facade) == 1
    finally:
        server.shutdown()
        server.server_close()


def test_continue_until_pause_kickoff_notification_is_not_blocking():
    facade, _game_id = _setup_single_game_day(
        "unit_test_continue_until_pause_non_blocking_kickoff",
        kickoff="7:00 PM",
        start_hour=19,
    )

    result = facade.continue_until_pause(max_hours=4)
    titles = [item["title"] for item in result["new_notifications"]]
    kickoff_notification = next(item for item in result["new_notifications"] if item["title"].startswith("Kickoff:"))

    assert result["paused"] is False
    assert kickoff_notification["requires_user_attention"] is False
    assert any(title.startswith("Final:") for title in titles)


def test_continue_until_pause_does_not_duplicate_existing_result():
    facade, game_id = _setup_single_game_day(
        "unit_test_continue_until_pause_no_duplicate_result",
        kickoff="7:00 PM",
        start_hour=19,
    )
    engine = facade._get_time_engine()

    first = engine.simulate_scheduled_game(game_id)
    assert first.get("already_simmed") is not True

    result = facade.continue_until_pause(max_hours=4)

    assert result["paused"] is False
    assert _result_count(facade) == 1
    assert result["debug_game_events"]["skipped_games"]
    assert result["debug_game_events"]["skipped_games"][0]["reason"] == "already_completed_result_exists"


def test_no_false_duplicate_skip_when_event_exists_without_result():
    facade, game_id = _setup_single_game_day("unit_test_no_false_duplicate_skip")
    engine = facade._get_time_engine()
    engine.simulated_games.add(game_id)
    engine.league.simulated_games = engine.simulated_games

    payload = facade.advance_day()

    assert payload["status"] == "advanced"
    assert _result_count(facade) == 1
    assert facade._get_time_engine()._find_result(game_id) is not None


def test_advance_day_response_is_compact():
    facade, _game_id = _setup_single_game_day("unit_test_advance_day_compact")

    payload = facade.advance_day()

    assert "league" not in payload
    assert "calendar" not in payload
    assert "schema_version" not in payload
    assert set(payload.keys()) == {
        "status",
        "paused",
        "stop_reason",
        "advanced_hours",
        "current_date",
        "current_time",
        "current_week",
        "current_phase",
        "clock",
        "processed_events",
        "new_notifications",
        "stopped_for_user_attention",
        "next_event",
        "debug_game_events",
    }


def test_advance_day_http_endpoint_simulates_game_and_stays_compact():
    facade, game_id = _setup_single_game_day("unit_test_advance_day_http_endpoint")
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        with urllib.request.urlopen(f"http://{host}:{port}/advance_day", data=b"{}", timeout=10) as resp:
            payload = json.loads(resp.read().decode("utf-8"))
        assert payload["status"] == "advanced"
        assert "league" not in payload
        assert facade._get_time_engine()._find_result(game_id) is not None
        assert _result_count(facade) == 1
    finally:
        server.shutdown()
        server.server_close()
