import os
import sys
import pytest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager
from gridiron_gm_pkg.simulation.systems.game.offseason_manager import OffseasonManager


def test_generate_draft_class_basic():
    league = LeagueManager()
    league.calendar = Calendar()
    dm = DraftManager(league, None)
    prospects = dm.generate_draft_class(30)
    assert len(prospects) == 30
    assert all(getattr(p, "is_draft_eligible") for p in prospects)
    assert all(getattr(p, "rookie") for p in prospects)


def test_offseason_manager_create_rookie_class():
    league = LeagueManager()
    league.calendar = Calendar()
    om = OffseasonManager(league)
    om.create_rookie_draft_class(num_players=20)
    assert len(league.draft_prospects) == 20
