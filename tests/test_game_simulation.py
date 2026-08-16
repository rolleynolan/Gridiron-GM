import datetime

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.engine.penalty_engine import simulate_penalty
from gridiron_gm_pkg.simulation.systems.time_engine import TimeEngine, make_game_id


def _setup_engine(seed=123):
    league = LeagueManager()
    team_a = Team("Alpha", "Alpha City", "ALP", conference="Nova", division="East")
    team_b = Team("Bravo", "Bravo City", "BRV", conference="Nova", division="East")
    league.add_team(team_a)
    league.add_team(team_b)
    league.user_team_id = team_a.id
    league.base_seed = seed

    calendar = league.calendar
    calendar.current_date = datetime.date(2025, 9, 1)
    calendar.current_week = 1

    schedule_by_week = {
        "1": [
            {
                "week": 1,
                "day": "Sunday",
                "kickoff": "19:00",
                "home_id": team_a.id,
                "away_id": team_b.id,
                "home_abbr": team_a.abbreviation,
                "away_abbr": team_b.abbreviation,
            }
        ]
    }
    engine = TimeEngine(league, calendar, schedule_by_week=schedule_by_week)
    return engine, team_a, team_b


def test_simulate_user_game_deterministic_and_updates():
    engine, team_a, team_b = _setup_engine(seed=77)
    game_id = make_game_id("1", team_a.id, team_b.id)

    result1 = engine.simulate_user_game(game_id)
    result2 = engine.simulate_user_game(game_id)

    assert result1["home_score"] == result2["home_score"]
    assert result1["away_score"] == result2["away_score"]
    assert result2["already_simmed"] is True
    assert result1["game_id"] == game_id
    assert result1["week"] == "1"
    assert result1["home_id"] == team_a.id
    assert result1["away_id"] == team_b.id

    records = [
        team_a.team_record.get("wins", 0),
        team_a.team_record.get("losses", 0),
        team_a.team_record.get("ties", 0),
    ]
    assert sum(records) == 1
    assert "points_for" in team_a.team_record
    assert "points_against" in team_a.team_record

    results_by_week = engine.league.results_by_week
    assert "1" in results_by_week
    assert len(results_by_week["1"]) == 1
    assert results_by_week["1"][0]["game_id"] == game_id

    inbox = engine.get_inbox()
    finals = [msg for msg in inbox if msg.subject.startswith("Final:")]
    assert finals
    assert finals[0].requires_ack is False


def test_penalty_engine_supports_players_without_discipline_rating():
    player = Player(
        name="Legacy Lineman",
        position="LT",
        age=28,
        dob=datetime.date(1997, 1, 1),
        college="Legacy U",
        birth_location="USA",
        jersey_number=71,
        overall=72,
    )
    if "discipline_rating" in player.__dict__:
        del player.__dict__["discipline_rating"]
    player.discipline = 61
    player.traits["mental"] = []

    result = simulate_penalty(player)

    assert result in {None, "False Start", "Holding"}
    assert player.discipline_rating == 61
