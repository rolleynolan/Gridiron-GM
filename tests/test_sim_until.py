import datetime
import json
import shutil
import sys
import threading
import urllib.request
import uuid
from pathlib import Path

from gridiron_gm_pkg.api.rpc_server import RpcServer
from gridiron_gm_pkg.api.server import make_server
from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.systems.time_engine import make_game_id
from gridiron_gm_pkg.simulation.utils.generate_schedule import add_nfl_style_playoff_schedule


def _save_name(prefix):
    return f"{prefix}_{uuid.uuid4().hex}"


def _cleanup_save(save_name):
    save_dir = Path("gridiron_gm_pkg") / "data" / "saves" / save_name
    shutil.rmtree(save_dir, ignore_errors=True)


def _monday_for_week(calendar, week):
    return calendar.nfl_week1_start_date + datetime.timedelta(days=(int(week) - 1) * 7)


def _set_calendar_to_week_start(facade, week):
    facade.calendar.current_week = week
    facade.calendar.current_date = _monday_for_week(facade.calendar, week)
    facade.calendar.update_phase()
    facade._time_engine = None


def _result_count(facade):
    return sum(
        len(games)
        for games in facade.season_manager.results_by_week.values()
        if isinstance(games, list)
    )


def _standings_win_total(facade):
    return sum(team["wins"] for team in facade.get_standings()["teams"])


def _make_two_game_schedule(facade, weeks, label, season_type):
    teams = facade.league.teams
    schedule = {}
    for offset, week in enumerate(weeks, start=1):
        schedule[str(week)] = [
            {
                "week": week,
                "calendar_week": week,
                "season_type": season_type,
                "season_week": offset,
                "day": "Sunday",
                "kickoff": "1:00 PM",
                "home_id": teams[0].id,
                "away_id": teams[1].id,
                "label": label,
            },
            {
                "week": week,
                "calendar_week": week,
                "season_type": season_type,
                "season_week": offset,
                "day": "Sunday",
                "kickoff": "4:30 PM",
                "home_id": teams[2].id,
                "away_id": teams[3].id,
                "label": label,
            },
        ]
    return schedule


def _conference_seed_teams(facade, conference):
    teams = [team for team in facade.league.teams if getattr(team, "conference", None) == conference]
    assert len(teams) >= 7
    return teams[:7]


def _build_playoff_fixture(facade):
    playoff_start, _ = facade.calendar.phase_boundaries[facade.calendar.PHASE_PLAYOFFS]
    standings_by_conf = {
        conf: [
            {"id": team.id, "abbr": team.abbreviation, "conference": conf}
            for team in _conference_seed_teams(facade, conf)
        ]
        for conf in ("Nova", "Atlas")
    }
    id_to_abbr = {team.id: team.abbreviation for team in facade.league.teams}
    schedule = {}
    add_nfl_style_playoff_schedule(schedule, standings_by_conf, id_to_abbr, playoff_start)
    facade.season_manager.schedule_by_week = schedule
    facade.season_manager.results_by_week = {}
    facade.season_manager.playoffs_generated = True
    facade._time_engine = None
    return playoff_start, schedule


def _schedule_game(facade, week, round_name, conference, index=0):
    games = [
        game
        for game in facade.season_manager.schedule_by_week[str(week)]
        if game.get("round") == round_name and game.get("conference") == conference
    ]
    assert len(games) > index
    return games[index]


def _record_result_for_game(facade, week, game, *, winning_side="home"):
    week_str = str(week)
    home_score, away_score = (27, 20) if winning_side == "home" else (20, 27)
    facade.season_manager.results_by_week.setdefault(week_str, []).append(
        {
            "game_id": make_game_id(week_str, game["home_id"], game["away_id"]),
            "week": week_str,
            "calendar_week": week,
            "season_type": "playoffs",
            "season_week": game.get("season_week"),
            "week_key": game.get("week_key"),
            "home_id": game["home_id"],
            "away_id": game["away_id"],
            "home": game["home_id"],
            "away": game["away_id"],
            "home_score": home_score,
            "away_score": away_score,
            "day": game.get("day"),
            "kickoff_time": game.get("kickoff"),
            "label": game.get("label"),
            "conference": game.get("conference"),
            "round": game.get("round"),
        }
    )


def _record_round_results(facade, week, round_name, conference, winners_by_index):
    games = [
        game
        for game in facade.season_manager.schedule_by_week[str(week)]
        if game.get("round") == round_name and game.get("conference") == conference
    ]
    assert len(games) == len(winners_by_index)
    for game, winning_side in zip(games, winners_by_index):
        _record_result_for_game(facade, week, game, winning_side=winning_side)


def test_sim_until_regular_season_week_five_from_start_records_results_and_standings():
    save_name = _save_name("unit_test_sim_until_rs5")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        result = facade.sim_until("regular_season_week", target_week=5)

        assert result["ok"] is True
        assert result["target_reached"] is True
        assert result["stopped_at"]["week_label"] == "Regular Season Week 5"
        assert result["games_simulated"] > 0
        assert result["results_added"] == result["games_simulated"]
        assert _result_count(facade) == result["results_added"]
        assert _standings_win_total(facade) > 0

        preseason_results = facade.season_manager.results_by_week.get("1", [])
        assert preseason_results
        assert all(game.get("label") == "Preseason" for game in preseason_results)
    finally:
        _cleanup_save(save_name)


def test_preseason_results_do_not_affect_regular_standings():
    save_name = _save_name("unit_test_sim_until_preseason")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        result = facade.sim_until("regular_season_week", target_week=1)

        assert result["ok"] is True
        assert result["stopped_at"]["week_label"] == "Regular Season Week 1"
        assert _result_count(facade) > 0
        assert _standings_win_total(facade) == 0
    finally:
        _cleanup_save(save_name)


def test_rerunning_same_sim_until_target_does_not_duplicate_results():
    save_name = _save_name("unit_test_sim_until_no_dupes")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        first = facade.sim_until("regular_season_week", target_week=1)
        count_after_first = _result_count(facade)
        second = facade.sim_until("regular_season_week", target_week=1)

        assert first["ok"] is True
        assert second["ok"] is True
        assert second["games_simulated"] == 0
        assert second["results_added"] == 0
        assert _result_count(facade) == count_after_first
    finally:
        _cleanup_save(save_name)


def test_sim_until_playoffs_from_regular_season_generates_playoffs():
    save_name = _save_name("unit_test_sim_until_playoffs")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        regular_end = facade.calendar.REGULAR_SEASON_END_WEEK
        _set_calendar_to_week_start(facade, regular_end)
        facade.season_manager.schedule_by_week = _make_two_game_schedule(
            facade, [regular_end], "Regular Season", "regular"
        )
        facade._time_engine = None

        result = facade.sim_until("playoffs_start")

        assert result["ok"] is True
        assert result["stopped_at"]["season_phase"] == "playoffs"
        assert result["stopped_at"]["week_label"].startswith("Playoffs")
        assert facade.calendar.playoff_subphase == "Wild Card"
        assert result["results_added"] == 2
        assert facade.season_manager.playoffs_generated is True
    finally:
        _cleanup_save(save_name)


def test_playoff_schedule_starts_with_real_wild_card_teams_and_future_tbd():
    save_name = _save_name("unit_test_playoff_fixture_start")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, _schedule = _build_playoff_fixture(facade)

        wild_card_games = facade.season_manager.schedule_by_week[str(playoff_start)]
        divisional_games = facade.season_manager.schedule_by_week[str(playoff_start + 1)]

        assert all(game.get("round") == "Wild Card" for game in wild_card_games)
        assert all(not str(game["home_id"]).startswith("TBD") for game in wild_card_games)
        assert all(not str(game["away_id"]).startswith("TBD") for game in wild_card_games)
        assert any(str(game["away_id"]).startswith("TBD") for game in divisional_games)
    finally:
        _cleanup_save(save_name)


def test_wild_card_completion_populates_divisional_matchups():
    save_name = _save_name("unit_test_playoff_wild_card_advance")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, _schedule = _build_playoff_fixture(facade)
        _record_round_results(facade, playoff_start, "Wild Card", "Nova", ["home", "home", "away"])
        _record_round_results(facade, playoff_start, "Wild Card", "Atlas", ["away", "home", "away"])

        changed = facade.season_manager.advance_playoff_bracket_if_ready()

        assert changed is True
        divisional_nova = [
            game for game in facade.season_manager.schedule_by_week[str(playoff_start + 1)]
            if game.get("conference") == "Nova"
        ]
        divisional_atlas = [
            game for game in facade.season_manager.schedule_by_week[str(playoff_start + 1)]
            if game.get("conference") == "Atlas"
        ]
        assert all(not str(game["home_id"]).startswith("TBD") for game in divisional_nova + divisional_atlas)
        assert all(not str(game["away_id"]).startswith("TBD") for game in divisional_nova + divisional_atlas)
    finally:
        _cleanup_save(save_name)


def test_divisional_completion_populates_conference_championship():
    save_name = _save_name("unit_test_playoff_divisional_advance")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, _schedule = _build_playoff_fixture(facade)
        _record_round_results(facade, playoff_start, "Wild Card", "Nova", ["home", "home", "away"])
        _record_round_results(facade, playoff_start, "Wild Card", "Atlas", ["away", "home", "away"])
        facade.season_manager.advance_playoff_bracket_if_ready()

        _record_round_results(facade, playoff_start + 1, "Divisional", "Nova", ["home", "away"])
        _record_round_results(facade, playoff_start + 1, "Divisional", "Atlas", ["away", "home"])
        facade.season_manager.advance_playoff_bracket_if_ready()

        nova_cc = _schedule_game(facade, playoff_start + 2, "Conference Championship", "Nova")
        atlas_cc = _schedule_game(facade, playoff_start + 2, "Conference Championship", "Atlas")
        assert not str(nova_cc["home_id"]).startswith("TBD")
        assert not str(nova_cc["away_id"]).startswith("TBD")
        assert not str(atlas_cc["home_id"]).startswith("TBD")
        assert not str(atlas_cc["away_id"]).startswith("TBD")
    finally:
        _cleanup_save(save_name)


def test_conference_championship_completion_populates_gridiron_bowl():
    save_name = _save_name("unit_test_playoff_bowl_advance")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, _schedule = _build_playoff_fixture(facade)
        _record_round_results(facade, playoff_start, "Wild Card", "Nova", ["home", "home", "away"])
        _record_round_results(facade, playoff_start, "Wild Card", "Atlas", ["away", "home", "away"])
        facade.season_manager.advance_playoff_bracket_if_ready()
        _record_round_results(facade, playoff_start + 1, "Divisional", "Nova", ["home", "away"])
        _record_round_results(facade, playoff_start + 1, "Divisional", "Atlas", ["away", "home"])
        facade.season_manager.advance_playoff_bracket_if_ready()
        _record_round_results(facade, playoff_start + 2, "Conference Championship", "Nova", ["home"])
        _record_round_results(facade, playoff_start + 2, "Conference Championship", "Atlas", ["away"])
        facade.season_manager.advance_playoff_bracket_if_ready()

        bowl = _schedule_game(facade, playoff_start + 3, "Gridiron Bowl", "Both")
        assert not str(bowl["home_id"]).startswith("TBD")
        assert not str(bowl["away_id"]).startswith("TBD")
    finally:
        _cleanup_save(save_name)


def test_sim_until_offseason_from_playoffs_records_playoff_results_without_standings():
    save_name = _save_name("unit_test_sim_until_offseason")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, playoff_end = facade.calendar.phase_boundaries[facade.calendar.PHASE_PLAYOFFS]
        _set_calendar_to_week_start(facade, playoff_start)
        facade.season_manager.playoffs_generated = True
        facade.season_manager.schedule_by_week = _make_two_game_schedule(
            facade, range(playoff_start, playoff_end + 1), "Playoffs", "playoffs"
        )
        facade._time_engine = None
        before_wins = _standings_win_total(facade)

        result = facade.sim_until("offseason_start")

        assert result["ok"] is True
        assert result["stopped_at"]["season_phase"] == "offseason"
        assert result["results_added"] == (playoff_end - playoff_start + 1) * 2
        assert _standings_win_total(facade) == before_wins
    finally:
        _cleanup_save(save_name)


def test_sim_until_offseason_through_realistic_playoffs_propagates_winners_without_duplicates():
    save_name = _save_name("unit_test_sim_until_real_playoffs")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, _schedule = _build_playoff_fixture(facade)
        _set_calendar_to_week_start(facade, playoff_start)
        facade._time_engine = None

        result = facade.sim_until("offseason_start")

        assert result["ok"] is True
        assert result["stopped_at"]["season_phase"] == "offseason"
        all_results = [
            game
            for games in facade.season_manager.results_by_week.values()
            if isinstance(games, list)
            for game in games
        ]
        playoff_results = [game for game in all_results if game.get("season_type") == "playoffs"]
        playoff_game_ids = [str(game.get("game_id")) for game in playoff_results]
        assert len(playoff_results) == 13
        assert len(playoff_game_ids) == len(set(playoff_game_ids))

        divisional_week = str(playoff_start + 1)
        conference_week = str(playoff_start + 2)
        bowl_week = str(playoff_start + 3)
        assert all("TBD" not in str(game["home_id"]) for game in facade.season_manager.schedule_by_week[divisional_week])
        assert all("TBD" not in str(game["away_id"]) for game in facade.season_manager.schedule_by_week[divisional_week])
        assert all("TBD" not in str(game["home_id"]) for game in facade.season_manager.schedule_by_week[conference_week])
        assert all("TBD" not in str(game["away_id"]) for game in facade.season_manager.schedule_by_week[conference_week])
        assert all("TBD" not in str(game["home_id"]) for game in facade.season_manager.schedule_by_week[bowl_week])
        assert all("TBD" not in str(game["away_id"]) for game in facade.season_manager.schedule_by_week[bowl_week])

        for week in (divisional_week, conference_week, bowl_week):
            payload = facade.get_results(week)
            finals = [game for game in payload["games"] if game["status"] == "final"]
            assert finals
            assert all("TBD" not in str(game["home_abbr"]) for game in finals)
            assert all("TBD" not in str(game["away_abbr"]) for game in finals)
    finally:
        _cleanup_save(save_name)


def test_sim_until_offseason_second_run_does_not_duplicate_playoff_results():
    save_name = _save_name("unit_test_sim_until_playoff_dupes")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        playoff_start, playoff_end = facade.calendar.phase_boundaries[facade.calendar.PHASE_PLAYOFFS]
        _set_calendar_to_week_start(facade, playoff_start)
        facade.season_manager.playoffs_generated = True
        facade.season_manager.schedule_by_week = _make_two_game_schedule(
            facade, range(playoff_start, playoff_end + 1), "Playoffs", "playoffs"
        )
        facade._time_engine = None

        first = facade.sim_until("offseason_start")
        count_after_first = _result_count(facade)
        second = facade.sim_until("offseason_start")

        assert first["ok"] is True
        assert second["ok"] is True
        assert second["games_simulated"] == 0
        assert second["results_added"] == 0
        assert _result_count(facade) == count_after_first
    finally:
        _cleanup_save(save_name)


def test_sim_until_invalid_and_behind_targets_return_errors():
    save_name = _save_name("unit_test_sim_until_errors")
    facade = GameFacade(save_name=save_name)
    try:
        facade.new_game()
        invalid = facade.sim_until("trade_deadline")
        assert invalid["ok"] is False
        assert invalid["error"] == "invalid_target_type"

        facade.sim_until("regular_season_week", target_week=5)
        behind = facade.sim_until("regular_season_week", target_week=1)
        assert behind["ok"] is False
        assert behind["error"] == "target_behind_current_state"
    finally:
        _cleanup_save(save_name)


def test_sim_until_http_api_returns_json_safe_result():
    save_name = _save_name("unit_test_sim_until_api")
    facade = GameFacade(save_name=save_name)
    server = make_server("127.0.0.1", 0, facade)
    host, port = server.server_address
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        payload = json.dumps(
            {"target_type": "regular_season_week", "target_week": 1}
        ).encode("utf-8")
        req = urllib.request.Request(
            f"http://{host}:{port}/sim_until",
            data=payload,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with urllib.request.urlopen(req, timeout=10) as resp:
            result = json.loads(resp.read().decode("utf-8"))

        assert result["ok"] is True
        assert result["target_reached"] is True
        assert isinstance(result["stopped_at"]["current_date"], str)
        json.dumps(result)
    finally:
        server.shutdown()
        server.server_close()
        _cleanup_save(save_name)


def test_sim_until_rpc_api_returns_json_safe_result():
    save_name = _save_name("unit_test_sim_until_rpc")
    stdout = sys.stdout
    try:
        rpc = RpcServer(save_path="", parent_pid=0)
        rpc.facade.save_name = save_name
        status, result = rpc.dispatch(
            "POST",
            "/sim_until",
            {"target_type": "regular_season_week", "target_week": 1},
        )

        assert status == 200
        assert result["ok"] is True
        assert result["target_reached"] is True
        json.dumps(result)
    finally:
        sys.stdout = stdout
        _cleanup_save(save_name)
