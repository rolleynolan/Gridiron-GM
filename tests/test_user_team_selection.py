# ACCEPTANCE CRITERIA
# - User team can be set via the facade and reflected in API state_summary.
# - Saved games retain the chosen user team on load.

import json
import threading
import urllib.request
import shutil
import uuid
from pathlib import Path

from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade


def _get_json(url: str):
    with urllib.request.urlopen(url, timeout=2) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _post_json(url: str, payload: dict):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        url,
        data=data,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=2) as resp:
        return json.loads(resp.read().decode("utf-8"))


def _make_temp_dir(base_dir: Path) -> Path:
    for _ in range(10):
        candidate = base_dir / f"tmp_user_team_{uuid.uuid4().hex}"
        try:
            candidate.mkdir()
            return candidate
        except FileExistsError:
            continue
    raise RuntimeError("Failed to create a temp directory for save/load test.")


def _count_today_team_agenda_events(events, today, team_id, agenda_types):
    counts = {event_type: 0 for event_type in agenda_types}
    for event in events:
        if getattr(event, "type", None) not in agenda_types:
            continue
        if getattr(event, "date", None) != today:
            continue
        payload = getattr(event, "payload", {})
        if not isinstance(payload, dict):
            continue
        if payload.get("team_id") != team_id:
            continue
        counts[event.type] += 1
    return counts


def _count_kickoff_events_for_game(events, today, game_id):
    count = 0
    for event in events:
        if getattr(event, "type", None) != "GameKickoff":
            continue
        if getattr(event, "date", None) != today:
            continue
        payload = getattr(event, "payload", {})
        if not isinstance(payload, dict):
            continue
        if str(payload.get("game_id")) == str(game_id):
            count += 1
    return count


def test_set_user_team_facade_updates_league():
    facade = GameFacade()
    facade.new_game()
    team_ids = [team.id for team in facade.league.teams]
    assert team_ids
    target_id = team_ids[-1]
    result = facade.set_user_team(target_id)
    assert result.get("ok") is True
    assert result.get("user_team_id") == target_id
    assert facade.league.user_team_id == target_id
    assert facade._get_time_engine().user_team_id == target_id


def test_set_user_team_api_updates_state_summary():
    facade = GameFacade()
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        summary = _get_json(f"http://{host}:{port}/state_summary")
        teams = summary.get("league", {}).get("teams", [])
        assert teams
        current_id = summary.get("user_team_id")
        target_id = next((team["id"] for team in teams if team["id"] != current_id), teams[0]["id"])
        response = _post_json(f"http://{host}:{port}/set_user_team", {"team_id": target_id})
        assert response.get("ok") is True
        assert response.get("user_team_id") == target_id
        summary = _get_json(f"http://{host}:{port}/state_summary")
        assert summary.get("user_team_id") == target_id
        expected_abbr = next((team["abbreviation"] for team in teams if team["id"] == target_id), None)
        if "user_team_abbr" in summary and expected_abbr is not None:
            assert summary.get("user_team_abbr") == expected_abbr
    finally:
        server.shutdown()
        server.server_close()


def test_user_team_persists_through_save_load():
    facade = GameFacade()
    facade.new_game()
    team_ids = [team.id for team in facade.league.teams]
    assert team_ids
    target_id = team_ids[-1]
    facade.set_user_team(target_id)
    repo_root = Path(__file__).resolve().parents[1]
    temp_dir = _make_temp_dir(repo_root)
    try:
        save_path = temp_dir / "league.json"
        facade.save(save_path)
        loaded = GameFacade()
        loaded.load(save_path)
        assert loaded.league.user_team_id == target_id
    finally:
        shutil.rmtree(temp_dir, ignore_errors=True)


def test_set_user_team_clears_today_agenda_events():
    facade = GameFacade()
    facade.new_game()
    engine = facade._get_time_engine()
    engine.ensure_agenda_for_today()
    today = engine.clock.current_date
    agenda_types = {"InboxCheck", "TrainingSlot", "Travel", "GameKickoff", "GameWrap"}
    old_team_id = facade.league.user_team_id
    team_ids = [team.id for team in facade.league.teams]
    assert old_team_id in team_ids
    target_id = next(team_id for team_id in team_ids if team_id != old_team_id)

    old_counts = _count_today_team_agenda_events(engine.queue.events(), today, old_team_id, agenda_types)
    assert sum(old_counts.values()) > 0

    facade.set_user_team(target_id)
    engine = facade._get_time_engine()
    engine.ensure_agenda_for_today()
    today_after = engine.clock.current_date
    assert today_after == today

    events = engine.queue.events()
    new_counts = _count_today_team_agenda_events(events, today_after, target_id, agenda_types)
    old_counts_after = _count_today_team_agenda_events(events, today_after, old_team_id, agenda_types)

    assert sum(old_counts_after.values()) == 0
    assert sum(new_counts.values()) >= 1
    assert new_counts.get("InboxCheck", 0) <= 1
    assert new_counts.get("TrainingSlot", 0) <= 3
    assert new_counts.get("Travel", 0) <= 1


def test_set_user_team_removes_stale_duplicate_kickoff_agenda_and_messages():
    facade = GameFacade()
    facade.new_game()
    engine = facade._get_time_engine()
    today = engine.clock.current_date
    week = str(facade.calendar.current_week)
    old_team_id = facade.league.user_team_id
    team_ids = [team.id for team in facade.league.teams]
    assert old_team_id in team_ids
    new_team_id = next(team_id for team_id in team_ids if team_id != old_team_id)
    game_id = f"{week}|{old_team_id}|{new_team_id}"

    schedule_by_week = {
        week: [
            {
                "week": week,
                "day": facade.calendar.current_day,
                "kickoff": "1:00 PM",
                "label": "Regular Season",
                "home_id": old_team_id,
                "away_id": new_team_id,
            }
        ]
    }
    facade.season_manager.schedule_by_week = schedule_by_week
    engine.schedule_by_week = schedule_by_week

    engine.queue.remove_matching(lambda _event: True)
    engine.last_agenda_date = None
    facade.league.last_agenda_date = None
    engine.ensure_agenda_for_today()

    assert _count_kickoff_events_for_game(engine.queue.events(), today, game_id) == 1

    facade.set_user_team(new_team_id)
    engine = facade._get_time_engine()
    engine.schedule_by_week = schedule_by_week
    facade.season_manager.schedule_by_week = schedule_by_week
    assert _count_kickoff_events_for_game(engine.queue.events(), today, game_id) == 1

    facade.set_user_team(new_team_id)
    engine = facade._get_time_engine()
    engine.schedule_by_week = schedule_by_week
    facade.season_manager.schedule_by_week = schedule_by_week
    assert _count_kickoff_events_for_game(engine.queue.events(), today, game_id) == 1

    facade.simulate_user_game(game_id)
    inbox = facade.get_inbox()
    messages = inbox.get("messages", [])
    kickoff_messages = [
        msg for msg in messages
        if isinstance(msg, dict)
        and str(msg.get("subject") or msg.get("title") or "").lower().startswith("kickoff:")
    ]
    final_messages = [
        msg for msg in messages
        if isinstance(msg, dict)
        and str(msg.get("subject") or msg.get("title") or "").lower().startswith("final:")
    ]
    kickoff_game_ids = [
        str((msg.get("payload") or {}).get("game_id") or "")
        for msg in kickoff_messages
    ]
    final_game_ids = [
        str((msg.get("payload") or {}).get("game_id") or "")
        for msg in final_messages
    ]

    assert len(kickoff_messages) == 1
    assert kickoff_game_ids == [game_id]
    assert len(set(kickoff_game_ids)) == 1
    assert final_game_ids == [game_id]
