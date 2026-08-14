import datetime
import json

from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.facade import game_facade
from gridiron_gm_pkg.simulation.systems import time_engine


def _extract_latest_kickoff_game_id(inbox_payload):
    messages = inbox_payload.get("messages", [])
    if not isinstance(messages, list):
        messages = []
    for msg in reversed(messages):
        if not isinstance(msg, dict):
            continue
        subject = str(msg.get("subject") or msg.get("title") or "")
        if not subject.lower().startswith("kickoff:"):
            continue
        payload = msg.get("payload", {})
        if isinstance(payload, dict) and payload.get("game_id"):
            return str(payload["game_id"])
        actions = msg.get("actions", [])
        for action in actions:
            if not isinstance(action, dict):
                continue
            game_id = action.get("game_id")
            if not game_id and isinstance(action.get("payload"), dict):
                game_id = action["payload"].get("game_id")
            if game_id:
                return str(game_id)
    raise AssertionError("No kickoff message with a game_id found in inbox.")


def _next_weekday(start_date, target_weekday):
    days_ahead = (target_weekday - start_date.weekday() + 7) % 7
    if days_ahead == 0:
        days_ahead = 7
    return start_date + datetime.timedelta(days=days_ahead)


def _collect_results(facade):
    results_by_week = {}
    if facade.season_manager is not None:
        results_by_week = getattr(facade.season_manager, "results_by_week", {}) or {}
    if not results_by_week:
        results_by_week = getattr(facade.league, "results_by_week", {}) or {}
    total = 0
    game_ids = []
    for entry in results_by_week.values():
        if isinstance(entry, dict):
            entry = [entry]
        if not isinstance(entry, list):
            continue
        for result in entry:
            if not isinstance(result, dict):
                continue
            total += 1
            game_id = result.get("game_id")
            if game_id is not None:
                game_ids.append(str(game_id))
    return total, game_ids


def _apply_fast_patches(monkeypatch):
    def load_two_teams(json_path):
        from gridiron_gm_pkg.simulation.entities.team import Team

        with open(json_path, "r", encoding="utf-8") as f:
            teams_data = json.load(f)
        teams = []
        for entry in teams_data[:2]:
            team_kwargs = dict(entry)
            if "name" in team_kwargs:
                team_kwargs["team_name"] = team_kwargs.pop("name")
            if "conference" not in team_kwargs:
                team_kwargs["conference"] = "Nova"
            team = Team(
                id=team_kwargs.get("id"),
                team_name=team_kwargs["team_name"],
                city=team_kwargs["city"],
                abbreviation=team_kwargs["abbreviation"],
                conference=team_kwargs["conference"],
                division=team_kwargs.get("division", "Unknown"),
            )
            teams.append(team)
        return teams

    def fill_tiny_rosters(teams):
        from gridiron_gm_pkg.simulation.entities.player import Player

        dob = datetime.date(2003, 1, 1)
        for idx, team in enumerate(teams):
            team.roster.clear()
            team.ir_list.clear()
            team.practice_squad.clear()
            base = idx * 10
            players = [
                Player(
                    name=f"{team.abbreviation} QB1",
                    position="QB",
                    age=22,
                    dob=dob,
                    college="Test U",
                    birth_location="USA",
                    jersey_number=base + 1,
                    overall=70,
                ),
                Player(
                    name=f"{team.abbreviation} RB1",
                    position="RB",
                    age=22,
                    dob=dob,
                    college="Test U",
                    birth_location="USA",
                    jersey_number=base + 2,
                    overall=68,
                ),
                Player(
                    name=f"{team.abbreviation} WR1",
                    position="WR",
                    age=22,
                    dob=dob,
                    college="Test U",
                    birth_location="USA",
                    jersey_number=base + 3,
                    overall=67,
                ),
                Player(
                    name=f"{team.abbreviation} LB1",
                    position="LB",
                    age=22,
                    dob=dob,
                    college="Test U",
                    birth_location="USA",
                    jersey_number=base + 4,
                    overall=66,
                ),
            ]
            for player in players:
                team.add_player(player)
            team.generate_depth_chart()

    def ensure_tiny_schedule(_save_name, league, calendar):
        teams = list(getattr(league, "teams", []) or [])
        if len(teams) < 2:
            return {}
        user_id = getattr(league, "user_team_id", None) or teams[0].id
        other_id = teams[1].id if teams[0].id == user_id else teams[0].id
        day_name = getattr(calendar, "current_day", None) or calendar.current_date.strftime("%A")
        return {
            "1": [
                {
                    "week": "1",
                    "day": day_name,
                    "kickoff": "1:00 PM",
                    "label": "Preseason",
                    "home_id": user_id,
                    "away_id": other_id,
                    "season_type": "Preseason",
                    "season_week": 1,
                }
            ]
        }

    monkeypatch.setattr(game_facade, "load_teams_from_json", load_two_teams)
    monkeypatch.setattr(game_facade, "fill_team_rosters_with_dummy_players", fill_tiny_rosters)
    monkeypatch.setattr(game_facade, "ensure_schedule_exists", ensure_tiny_schedule)
    monkeypatch.setattr(GameFacade, "_expected_games_per_week", lambda self, league: 1)
    monkeypatch.setattr(GameFacade, "_schedule_is_valid", lambda *args, **kwargs: True)
    monkeypatch.setattr(time_engine, "generate_box_score", lambda *args, **kwargs: {})


def test_core_loop_vertical_slice(monkeypatch, tmp_path):
    _apply_fast_patches(monkeypatch)
    save_name = f"core_loop_{tmp_path.name}"
    facade = GameFacade(save_name=save_name)
    facade.new_game()

    standings_before = facade.get_standings()["teams"]

    kickoff_result = facade.continue_until_pause(max_hours=20)
    assert kickoff_result.get("paused") is False
    assert kickoff_result.get("debug_game_events", {}).get("game_results_created") == 1
    assert facade.calendar.season_phase == "preseason"

    inbox = facade.get_inbox()
    game_id = _extract_latest_kickoff_game_id(inbox)

    sim_payload = facade.simulate_user_game(game_id)
    sim_result = sim_payload.get("result", {})
    assert sim_payload.get("ok") is True
    assert sim_result.get("already_simmed") is True
    assert sim_result.get("game_type") == "preseason"

    standings_after = facade.get_standings()["teams"]
    assert standings_after == standings_before

    total_results_before, game_ids_before = _collect_results(facade)
    assert game_id in game_ids_before
    assert len(game_ids_before) == len(set(game_ids_before))

    engine = facade._get_time_engine()
    engine.queue.remove_matching(lambda _event: True)
    facade.calendar.current_week = facade.calendar.REGULAR_SEASON_START_WEEK
    facade.calendar.update_phase()
    engine.clock.hour = 7
    engine.last_agenda_date = None
    engine.ensure_agenda_for_today()

    phase_result = facade.continue_until_pause(max_hours=5)
    assert phase_result.get("stop_reason") == "phase_change"

    phase_follow_result = facade.continue_until_pause(max_hours=5)
    assert phase_follow_result.get("advanced_hours", 0) > 0
    assert phase_follow_result.get("stop_reason") != "phase_change"

    tuesday = _next_weekday(facade.calendar.current_date, 1)
    facade.calendar.current_date = tuesday
    engine.clock.current_date = tuesday
    facade.calendar.current_week = facade.calendar.REGULAR_SEASON_START_WEEK + 7
    facade.calendar.update_phase()
    engine.queue.remove_matching(lambda _event: True)
    engine.clock.hour = 7
    engine.last_agenda_date = None
    engine.ensure_agenda_for_today()

    trade_result = facade.continue_until_pause(max_hours=5)
    assert trade_result.get("stop_reason") == "trade_deadline"

    trade_follow_result = facade.continue_until_pause(max_hours=5)
    assert trade_follow_result.get("advanced_hours", 0) > 0
    assert trade_follow_result.get("stop_reason") != "trade_deadline"

    save_path = tmp_path / "savegame.json"
    facade.save(save_path)

    loaded = GameFacade(save_name=save_name)
    loaded.load(save_path)

    loaded_standings = loaded.get_standings()["teams"]
    assert loaded_standings == standings_before

    total_results_after, game_ids_after = _collect_results(loaded)
    assert total_results_after == total_results_before
    assert sorted(game_ids_after) == sorted(game_ids_before)
    assert len(game_ids_after) == len(set(game_ids_after))


def test_kickoff_inbox_notification_uses_payload_game_id(monkeypatch, tmp_path):
    _apply_fast_patches(monkeypatch)
    save_name = f"core_loop_kickoff_payload_{tmp_path.name}"
    facade = GameFacade(save_name=save_name)
    facade.new_game()

    facade.continue_until_pause(max_hours=20)
    inbox = facade.get_inbox()
    messages = inbox.get("messages", [])
    kickoff = next(
        (
            msg for msg in messages
            if isinstance(msg, dict)
            and str(msg.get("subject") or msg.get("title") or "").lower().startswith("kickoff:")
        ),
        None,
    )

    assert kickoff is not None
    assert kickoff.get("requires_ack") is False
    assert kickoff.get("blocks_advancement") is False
    assert isinstance(kickoff.get("payload"), dict)
    assert kickoff["payload"].get("game_id")
