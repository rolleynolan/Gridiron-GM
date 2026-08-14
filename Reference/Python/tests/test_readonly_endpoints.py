import datetime

from gridiron_gm_pkg.simulation.facade.game_facade import GameFacade
from gridiron_gm_pkg.simulation.systems.time_engine import make_game_id, parse_kickoff_hour
from gridiron_gm_pkg.simulation.utils.generate_schedule import add_nfl_style_playoff_schedule


def test_standings_sorted_order_stable():
    facade = GameFacade(save_name="unit_test_standings_sort")
    facade.new_game()
    teams = facade.league.teams
    assert len(teams) >= 3
    teams[0].team_record = {
        "wins": 3,
        "losses": 1,
        "ties": 0,
        "points_for": 100,
        "points_against": 80,
    }
    teams[1].team_record = {
        "wins": 3,
        "losses": 1,
        "ties": 0,
        "points_for": 90,
        "points_against": 70,
    }
    teams[2].team_record = {
        "wins": 4,
        "losses": 2,
        "ties": 0,
        "points_for": 80,
        "points_against": 70,
    }
    payload = facade.get_standings()
    order = [team["team_id"] for team in payload["teams"]]
    tied_ids = sorted([teams[0].id, teams[1].id])
    expected_ids = [teams[2].id] + tied_ids
    assert order[:3] == expected_ids


def test_results_merge_scheduled_and_finals():
    facade = GameFacade(save_name="unit_test_results_merge")
    facade.new_game()
    schedule = facade.season_manager.schedule_by_week
    assert schedule
    week = sorted(schedule.keys(), key=lambda w: int(str(w)) if str(w).isdigit() else str(w))[0]
    games = schedule[week]
    assert len(games) >= 2
    final_game = games[0]
    week_str = str(week)
    result_game_id = make_game_id(week_str, final_game["home_id"], final_game["away_id"])
    facade.season_manager.results_by_week = {
        week_str: [
            {
                "game_id": result_game_id,
                "week": week_str,
                "home_id": final_game["home_id"],
                "away_id": final_game["away_id"],
                "home_score": 24,
                "away_score": 17,
                "day": final_game.get("day"),
                "kickoff_time": final_game.get("kickoff"),
            }
        ]
    }
    payload = facade.get_results(week_str)
    assert payload["ok"] is True
    games_payload = payload["games"]
    final_entry = next((g for g in games_payload if g["game_id"] == result_game_id), None)
    assert final_entry is not None
    assert final_entry["status"] == "final"
    assert final_entry["home_score"] == 24
    assert final_entry["away_score"] == 17
    scheduled_entry = next((g for g in games_payload if g["status"] == "scheduled"), None)
    assert scheduled_entry is not None
    assert scheduled_entry["home_score"] is None
    assert scheduled_entry["away_score"] is None


def test_next_user_game_returns_future_game():
    facade = GameFacade(save_name="unit_test_next_user_game")
    facade.new_game()
    state = facade.get_state()
    clock_date = datetime.date.fromisoformat(state["time_engine"]["date"])
    clock_hour = state["time_engine"]["hour"]
    payload = facade.get_next_user_game()
    assert payload["ok"] is True
    game = payload["game"]
    assert game is not None
    game_date = datetime.date.fromisoformat(game["date"])
    kickoff_hour = parse_kickoff_hour(game.get("kickoff"))
    assert (game_date > clock_date) or (game_date == clock_date and kickoff_hour > clock_hour)
    assert game["status"] == "scheduled"


def test_results_hide_placeholder_playoff_finals():
    facade = GameFacade(save_name="unit_test_results_hide_tbd_playoff")
    facade.new_game()
    playoff_start, _ = facade.calendar.phase_boundaries[facade.calendar.PHASE_PLAYOFFS]
    standings_by_conf = {}
    for conf in ("Nova", "Atlas"):
        teams = [team for team in facade.league.teams if getattr(team, "conference", None) == conf][:7]
        standings_by_conf[conf] = [
            {"id": team.id, "abbr": team.abbreviation, "conference": conf}
            for team in teams
        ]
    schedule = {}
    add_nfl_style_playoff_schedule(
        schedule,
        standings_by_conf,
        {team.id: team.abbreviation for team in facade.league.teams},
        playoff_start,
    )
    facade.season_manager.schedule_by_week = schedule
    divisional_week = str(playoff_start + 1)
    placeholder_game = schedule[divisional_week][0]
    facade.season_manager.results_by_week = {
        divisional_week: [
            {
                "game_id": make_game_id(divisional_week, placeholder_game["home_id"], placeholder_game["away_id"]),
                "week": divisional_week,
                "home_id": placeholder_game["home_id"],
                "away_id": placeholder_game["away_id"],
                "home": placeholder_game["home_id"],
                "away": placeholder_game["away_id"],
                "home_score": 24,
                "away_score": 17,
                "label": placeholder_game["label"],
                "season_type": "playoffs",
                "season_week": 2,
            }
        ]
    }

    payload = facade.get_results(divisional_week)

    assert payload["ok"] is True
    assert all(game["status"] != "final" for game in payload["games"])
    assert all("TBD" not in str(game["game_id"]) for game in payload["games"] if game["status"] == "final")
