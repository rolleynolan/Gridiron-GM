import datetime
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager


def test_player_age_increases_on_birthday(tmp_path):
    cal = Calendar(start_year=2025)
    league = LeagueManager()
    team = Team("Testers", "City", "TST")
    league.add_team(team)
    # Allow FreeAgencyManager to access free_agents via dict-style get
    LeagueManager.get = lambda self, key, default=None: getattr(self, key, default)
    LeagueManager.__getitem__ = lambda self, key: getattr(self, key)

    today = cal.current_date
    p1 = Player("Birthday Today", "QB", 25, today, "U", "USA", 1, 70)
    p2 = Player(
        "Birthday Tomorrow",
        "RB",
        22,
        today + datetime.timedelta(days=1),
        "U",
        "USA",
        2,
        70,
    )
    team.add_player(p1)
    team.add_player(p2)

    sm = SeasonManager(cal, league, save_name="unit_test_league")

    sm.advance_day()
    assert p1.age == 26
    assert p2.age == 22

    sm.advance_day()
    assert p2.age == 23


