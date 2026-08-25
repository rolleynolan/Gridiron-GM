import datetime
import json
from pathlib import Path

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.persistence.savegame import load_league, save_league


def _build_league():
    league = LeagueManager()
    team = Team("Testers", "City", "TST")
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
    league.add_team(team)
    fa = Player(
        name="Free Agent",
        position="RB",
        age=24,
        dob=datetime.date(2001, 2, 2),
        college="U",
        birth_location="USA",
        jersey_number=21,
        overall=68,
    )
    league.free_agents.append(fa)
    return league


def _local_test_path(name):
    base = Path(".test_tmp") / "savegame"
    base.mkdir(parents=True, exist_ok=True)
    return base / name


def test_savegame_round_trip():
    league = _build_league()
    league.calendar.current_date = datetime.date(2028, 2, 29)
    league.calendar.current_week = 23
    league.calendar.update_phase()
    save_path = _local_test_path("league.json")
    save_league(save_path, league)

    loaded = load_league(save_path)
    assert len(loaded.teams) == 1
    assert loaded.teams[0].team_name == "Testers"
    assert len(loaded.teams[0].roster) == 1
    assert len(loaded.free_agents) == 1
    assert loaded.calendar.current_date == datetime.date(2028, 2, 29)
    assert loaded.calendar.football_week == 23
    assert loaded.calendar.season_phase == "playoffs"


def test_savegame_migrates_v0():
    league = _build_league()
    v0_path = _local_test_path("league_v0.json")
    with v0_path.open("w", encoding="utf-8") as f:
        json.dump(league.to_dict(), f, indent=2)

    loaded = load_league(v0_path)
    assert len(loaded.teams) == 1
    assert loaded.teams[0].team_name == "Testers"


def test_savegame_loads_legacy_calendar_missing_current_date():
    league = _build_league()
    payload = league.to_dict()
    payload["calendar"] = {
        "current_year": 2027,
        "current_week": 4,
        "season_phase": "Regular Season",
    }
    legacy_path = _local_test_path("legacy_missing_date.json")
    with legacy_path.open("w", encoding="utf-8") as f:
        json.dump({"schema_version": 1, "league": payload}, f, indent=2)

    loaded = load_league(legacy_path)

    assert loaded.calendar.current_date == loaded.calendar.get_nfl_week1_start(2027)
    assert loaded.calendar.season_year == 2027
    assert loaded.calendar.football_week == 4
    assert loaded.calendar.season_phase == "regular_season"


def test_savegame_preserves_calendar_state_across_major_phases():
    phase_weeks = {
        "preseason": 2,
        "preseason_bye": 4,
        "regular_season": 5,
        "playoffs": 23,
        "postseason": 27,
        "offseason": 28,
    }
    for phase, week in phase_weeks.items():
        league = _build_league()
        league.calendar.current_week = week
        league.calendar.current_date = datetime.date(2026, 9, 8) + datetime.timedelta(days=(week - 1) * 7)
        save_path = _local_test_path(f"{phase}.json")

        save_league(save_path, league)
        loaded = load_league(save_path)

        assert loaded.calendar.current_date == league.calendar.current_date
        assert loaded.calendar.day_of_week == league.calendar.day_of_week
        assert loaded.calendar.season_year == league.calendar.season_year
        assert loaded.calendar.football_week == week
        assert loaded.calendar.season_phase == phase
