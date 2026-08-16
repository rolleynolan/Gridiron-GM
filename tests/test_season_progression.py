import datetime

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.systems.player.season_progression import apply_season_progression


def _player(name: str, age: int) -> Player:
    return Player(name, "RB", age, datetime.date(2000, 1, 1), "U", "USA", 1, 70)


def test_season_progression_ages_players_records_history_and_is_idempotent():
    league = LeagueManager()
    league.base_seed = 17
    team = Team("Progress", "Test", "PRG")
    player = _player("Young", 22)
    team.add_player(player)
    league.add_team(team)

    result = apply_season_progression(league, 2030)

    assert result["applied"] is True
    assert player.age == 23
    assert player.progress_history["2030"]["age"] == 23
    assert league.last_season_progression == "2030"

    repeat = apply_season_progression(league, 2030)
    assert repeat["applied"] is False
    assert repeat["reason"] == "already_applied"
    assert player.age == 23


def test_season_progression_marks_age_limit_retirement():
    league = LeagueManager()
    team = Team("Progress", "Test", "PRG")
    veteran = _player("Veteran", 40)
    team.add_player(veteran)
    league.add_team(team)

    apply_season_progression(league, 2030)

    assert veteran.age == 41
    assert veteran.retired is True
    assert veteran.retirement_reason == "age_limit"


def test_season_progression_guard_survives_save_data():
    league = LeagueManager()
    team = Team("Progress", "Test", "PRG")
    team.add_player(_player("Young", 22))
    league.add_team(team)
    apply_season_progression(league, 2030)

    loaded = LeagueManager.from_dict(league.to_dict())

    assert loaded.last_season_progression == "2030"
    assert apply_season_progression(loaded, 2030)["applied"] is False
