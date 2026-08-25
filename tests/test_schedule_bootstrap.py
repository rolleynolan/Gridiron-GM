import datetime
import json

from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.utils.generate_schedule import (
    PRESEASON_BYE_WEEK,
    REGULAR_SEASON_START_WEEK,
)


def test_new_game_bootstraps_schedule():
    facade = GameFacade(save_name="unit_test_schedule_bootstrap_bye")
    facade.new_game()
    schedule = facade.season_manager.schedule_by_week
    assert schedule
    assert any(games for games in schedule.values())
    assert schedule[str(PRESEASON_BYE_WEEK)] == []
    assert schedule[str(REGULAR_SEASON_START_WEEK)]
    assert schedule[str(REGULAR_SEASON_START_WEEK)][0]["season_type"] == "regular"
    assert schedule[str(REGULAR_SEASON_START_WEEK)][0]["season_week"] == 1


def test_preseason_bye_has_no_games_or_standings_changes():
    facade = GameFacade(save_name="unit_test_schedule_bootstrap_bye")
    facade.new_game()
    facade.calendar.current_week = PRESEASON_BYE_WEEK
    facade.calendar.update_phase()
    before = facade.get_standings()["teams"]

    facade.season_manager.simulate_games_for_today()

    assert facade.calendar.season_phase == "preseason_bye"
    assert facade.season_manager.schedule_by_week[str(PRESEASON_BYE_WEEK)] == []
    assert facade.get_standings()["teams"] == before


def test_regular_season_week_one_immediately_follows_preseason_bye_with_games():
    facade = GameFacade(save_name="unit_test_schedule_bootstrap_bye")
    facade.new_game()
    schedule = facade.season_manager.schedule_by_week

    assert PRESEASON_BYE_WEEK + 1 == REGULAR_SEASON_START_WEEK
    assert schedule[str(PRESEASON_BYE_WEEK)] == []
    week_one_games = schedule[str(REGULAR_SEASON_START_WEEK)]
    assert week_one_games
    assert all(game["season_type"] == "regular" for game in week_one_games)
    assert all(game["season_week"] == 1 for game in week_one_games)

    facade.calendar.current_week = REGULAR_SEASON_START_WEEK
    facade.calendar.update_phase()
    results = facade.get_results(week=str(REGULAR_SEASON_START_WEEK))

    assert facade.calendar.season_phase == "regular_season"
    assert facade.calendar.get_week_label() == "Regular Season Week 1"
    assert results["week_key"] == "regular:1"
    assert results["week_label"] == "Regular Season Week 1"
    assert results["games"]


def test_continue_until_pause_auto_simulates_user_kickoff():
    facade = GameFacade(save_name="unit_test_schedule_bootstrap_pause")
    facade.new_game()
    result = facade.continue_until_pause(max_hours=200)
    assert result.get("paused") is False
    assert result["debug_game_events"]["game_results_created"] >= 1
    assert result.get("stop_reason") != "user_game_ready"


def test_regular_week_one_monday_has_next_game_but_none_today():
    facade = GameFacade(save_name="unit_test_schedule_context_monday")
    facade.new_game()
    facade.calendar.current_week = REGULAR_SEASON_START_WEEK
    facade.calendar.current_date = datetime.date(2025, 9, 29)
    facade.calendar.update_phase()

    context = facade.get_schedule_context()

    assert context["games_today"] == []
    assert context["can_simulate_today"] is False
    assert context["next_game"] is not None
    assert context["next_game_date"] == context["next_game"]["date"]
    assert context["next_game_label"] == context["next_game"]["label"]
    assert context["next_game"]["day_of_week"] == "Sunday"
    assert context["next_game"]["week_label"] == "Regular Season Week 1"
    json.dumps(context)


def test_user_team_game_today_is_exposed_separately_from_league_games_today():
    facade = GameFacade(save_name="unit_test_user_team_game_today")
    facade.new_game()
    teams = facade.league.teams
    user_team_id = teams[0].id
    opponent_id = teams[1].id
    other_home_id = teams[2].id
    other_away_id = teams[3].id
    facade.league.user_team_id = user_team_id
    facade.calendar.current_week = REGULAR_SEASON_START_WEEK
    facade.calendar.current_date = datetime.date(2025, 10, 5)
    facade.calendar.update_phase()
    facade.season_manager.schedule_by_week = {
        str(REGULAR_SEASON_START_WEEK): [
            {
                "week": REGULAR_SEASON_START_WEEK,
                "day": "Sunday",
                "kickoff": "1:00 PM",
                "home_id": user_team_id,
                "away_id": opponent_id,
                "season_type": "regular",
                "season_week": 1,
            },
            {
                "week": REGULAR_SEASON_START_WEEK,
                "day": "Sunday",
                "kickoff": "4:25 PM",
                "home_id": other_home_id,
                "away_id": other_away_id,
                "season_type": "regular",
                "season_week": 1,
            },
        ]
    }

    context = facade.get_schedule_context()

    assert len(context["games_today"]) == 2
    assert context["league_games_today_count"] == 2
    assert context["can_simulate_today"] is True
    assert context["user_team_can_simulate"] is True
    assert context["user_team_game_today"] is not None
    assert context["user_team_game_today"]["home_id"] == user_team_id
    assert context["user_team_game_today"]["away_id"] == opponent_id
    assert context["user_team_next_game"] == context["user_team_game_today"]
    json.dumps(context)


def test_no_user_team_game_today_exposes_next_user_game_and_league_count():
    facade = GameFacade(save_name="unit_test_next_user_team_game")
    facade.new_game()
    teams = facade.league.teams
    user_team_id = teams[0].id
    opponent_id = teams[1].id
    other_home_id = teams[2].id
    other_away_id = teams[3].id
    facade.league.user_team_id = user_team_id
    facade.calendar.current_week = REGULAR_SEASON_START_WEEK
    facade.calendar.current_date = datetime.date(2025, 10, 5)
    facade.calendar.update_phase()
    facade.season_manager.schedule_by_week = {
        str(REGULAR_SEASON_START_WEEK): [
            {
                "week": REGULAR_SEASON_START_WEEK,
                "day": "Sunday",
                "kickoff": "1:00 PM",
                "home_id": other_home_id,
                "away_id": other_away_id,
                "season_type": "regular",
                "season_week": 1,
            },
        ],
        str(REGULAR_SEASON_START_WEEK + 1): [
            {
                "week": REGULAR_SEASON_START_WEEK + 1,
                "day": "Sunday",
                "kickoff": "1:00 PM",
                "home_id": opponent_id,
                "away_id": user_team_id,
                "season_type": "regular",
                "season_week": 2,
            },
        ],
    }

    context = facade.get_schedule_context()

    assert len(context["games_today"]) == 1
    assert context["league_games_today_count"] == 1
    assert context["user_team_game_today"] is None
    assert context["user_team_can_simulate"] is False
    assert context["user_team_next_game"] is not None
    assert context["user_team_next_game"]["home_id"] == opponent_id
    assert context["user_team_next_game"]["away_id"] == user_team_id
    assert context["user_team_next_game"]["date"] == "2025-10-12"
    json.dumps(context)


def test_regular_week_one_game_date_has_games_today():
    facade = GameFacade(save_name="unit_test_schedule_context_today")
    facade.new_game()
    facade.calendar.current_week = REGULAR_SEASON_START_WEEK
    facade.calendar.current_date = datetime.date(2025, 10, 5)
    facade.calendar.update_phase()

    context = facade.get_schedule_context()

    assert context["games_today"]
    assert context["can_simulate_today"] is True
    assert context["games_today"][0]["date"] == "2025-10-05"
    assert context["games_today"][0]["day_of_week"] == "Sunday"
    assert context["games_today"][0]["week_label"] == "Regular Season Week 1"


def test_preseason_bye_next_game_points_to_regular_opener():
    facade = GameFacade(save_name="unit_test_schedule_context_bye")
    facade.new_game()
    facade.calendar.current_week = PRESEASON_BYE_WEEK
    facade.calendar.current_date = datetime.date(2025, 9, 22)
    facade.calendar.update_phase()

    context = facade.get_schedule_context()

    assert facade.calendar.season_phase == "preseason_bye"
    assert context["games_today"] == []
    assert context["can_simulate_today"] is False
    assert context["next_game"] is not None
    assert context["next_game"]["week_label"] == "Regular Season Week 1"
    assert context["next_game"]["day_of_week"] == "Sunday"


def test_advance_day_during_playoffs_records_result_without_crashing():
    facade = GameFacade(save_name="unit_test_advance_day_playoffs")
    facade.new_game()
    teams = facade.league.teams
    playoff_start = facade.calendar.phase_boundaries[facade.calendar.PHASE_PLAYOFFS][0]
    facade.calendar.current_week = playoff_start
    facade.calendar.current_date = datetime.date(2026, 1, 10)
    facade.calendar.update_phase()
    facade.season_manager.playoffs_generated = True
    facade.season_manager.schedule_by_week = {
        str(playoff_start): [
            {
                "week": playoff_start,
                "calendar_week": playoff_start,
                "season_type": "playoffs",
                "season_week": 1,
                "week_key": "playoffs:1",
                "day": "Saturday",
                "kickoff": "1:00 PM",
                "home_id": teams[0].id,
                "away_id": teams[1].id,
                "label": "Playoffs - Wild Card",
                "playoff": True,
                "round": "Wild Card",
                "conference": "Nova",
            }
        ]
    }
    facade._time_engine = None
    facade.league.last_agenda_date = None
    facade.league.game_clock = None
    facade.league.event_queue = None

    payload = facade.advance_day()
    results = facade.season_manager.results_by_week.get(str(playoff_start), [])

    assert payload["current_phase"] in {"playoffs", "postseason", "offseason"}
    assert len(results) == 1
    assert results[0]["label"] == "Playoffs - Wild Card"
