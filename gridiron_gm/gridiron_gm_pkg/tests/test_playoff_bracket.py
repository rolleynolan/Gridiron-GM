import sys
from pathlib import Path
import pytest

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm.gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm.gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager


def build_small_league():
    cal = Calendar()
    league = LeagueManager()

    def add_team(abbr, conf, div):
        team = Team(abbr, f"{abbr}City", abbr, conf, div)
        league.add_team(team)
        return team

    nova_abbrs = ["NE1", "NE2", "NW1", "NW2", "NN1", "NN2", "NS1"]
    nova_divs = ["East", "East", "West", "West", "North", "North", "South"]
    atlas_abbrs = ["AE1", "AE2", "AW1", "AW2", "AN1", "AN2", "AS1"]
    atlas_divs = ["East", "East", "West", "West", "North", "North", "South"]

    for abbr, div in zip(nova_abbrs, nova_divs):
        add_team(abbr, "Nova", div)
    for abbr, div in zip(atlas_abbrs, atlas_divs):
        add_team(abbr, "Atlas", div)

    return cal, league


def test_generate_playoff_bracket_returns_seven_seeds(monkeypatch):
    class DummyFA:
        def __init__(self, lg):
            pass
        def advance_free_agency_day(self):
            pass

    class DummyOff:
        def __init__(self, lg):
            pass
        def step(self):
            pass

    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.FreeAgencyManager",
        DummyFA,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.OffseasonManager",
        DummyOff,
    )

    cal, league = build_small_league()
    sm = SeasonManager(cal, league, save_name="playoff_test")

    for tid, record in sm.standings_manager.standings.items():
        record["W"] = 10
        record["L"] = 4
        record["PF"] = 300

    bracket = sm.generate_playoff_bracket()

    assert len(bracket["Nova"]) == 7
    assert len(bracket["Atlas"]) == 7

