import datetime
import json

from gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.persistence.savegame import load_league, save_league
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager


def _wire_league_dict_access(league):
    LeagueManager.get = lambda self, key, default=None: getattr(self, key, default)
    LeagueManager.__getitem__ = lambda self, key: getattr(self, key)


def test_injury_blocks_training_and_heals_after_end_date(tmp_path):
    cal = Calendar(start_year=2025)
    league = LeagueManager()
    league.calendar = cal
    team = Team("Testers", "City", "TST")
    league.add_team(team)
    _wire_league_dict_access(league)

    player = Player(
        name="Tester One",
        position="QB",
        age=25,
        dob=datetime.date(2000, 1, 1),
        college="U",
        birth_location="USA",
        jersey_number=12,
        overall=75,
    )
    team.add_player(player)

    sm = SeasonManager(cal, league, save_name=f"unit_test_injury_{tmp_path.name}")

    start_date = cal.current_date
    player.injury_status = "out"
    player.injury_name = "Test Injury"
    player.injury_start_date = start_date
    player.injury_end_date = start_date + datetime.timedelta(days=2)
    player.injury_severity = 2

    baseline = player.get_effective_attribute("speed")

    sm.advance_day()
    assert player.get_effective_attribute("speed") == baseline
    assert player.injury_status == "out"

    save_path = tmp_path / "league.json"
    save_league(save_path, league)

    loaded = load_league(save_path)
    _wire_league_dict_access(loaded)
    loaded_player = loaded.teams[0].roster[0]

    assert loaded_player.injury_status == "out"
    assert loaded_player.injury_name == "Test Injury"
    assert loaded_player.injury_start_date == start_date
    assert loaded_player.injury_end_date == start_date + datetime.timedelta(days=2)
    assert loaded_player.injury_severity == 2

    loaded_baseline = loaded_player.get_effective_attribute("speed")
    assert loaded_baseline == baseline

    sm_loaded = SeasonManager(
        loaded.calendar, loaded, save_name=f"unit_test_injury_{tmp_path.name}"
    )
    sm_loaded.advance_day()

    assert loaded_player.get_effective_attribute("speed") == loaded_baseline
    assert loaded_player.injury_status == "healthy"
    assert loaded_player.injury_name is None
    assert loaded_player.injury_start_date is None
    assert loaded_player.injury_end_date is None
    assert loaded_player.injury_severity is None


def test_legacy_weeks_out_conversion(tmp_path):
    cal = Calendar(start_year=2025)
    league = LeagueManager()
    league.calendar = cal
    team = Team("Testers", "City", "TST")
    league.add_team(team)
    _wire_league_dict_access(league)

    player = Player(
        name="Legacy Player",
        position="RB",
        age=26,
        dob=datetime.date(1999, 2, 2),
        college="U",
        birth_location="USA",
        jersey_number=22,
        overall=70,
    )
    team.add_player(player)

    league_dict = league.to_dict()
    legacy_player = league_dict["teams"][0]["players"][0]
    legacy_player.pop("injury_status", None)
    legacy_player.pop("injury_start_date", None)
    legacy_player.pop("injury_end_date", None)
    legacy_player["is_injured"] = True
    legacy_player["weeks_out"] = 3

    save_path = tmp_path / "legacy_league.json"
    with save_path.open("w", encoding="utf-8") as f:
        json.dump({"schema_version": 1, "league": league_dict}, f, indent=2)

    loaded = load_league(save_path)
    loaded_player = loaded.teams[0].roster[0]
    expected_end = cal.current_date + datetime.timedelta(days=21)
    assert loaded_player.injury_status == "out"
    assert loaded_player.injury_start_date == cal.current_date
    assert loaded_player.injury_end_date == expected_end
    assert loaded_player.weeks_out == 0
    assert loaded_player.is_injured is False
