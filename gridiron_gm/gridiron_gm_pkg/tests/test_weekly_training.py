import sys
from pathlib import Path
import random

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))
random.seed(0)

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.weekly_training import (
    apply_training_plan,
)
from gridiron_gm.gridiron_gm_pkg.config.training_catalog import TRAINING_CATALOG

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.weekly_training import (
    apply_training_plan,
)
from gridiron_gm.gridiron_gm_pkg.config.training_catalog import TRAINING_CATALOG


class DummyAttrs:
    def __init__(self):
        self.core = {
            "throw_accuracy_short": 60,
            "throw_power": 60,
            "strength": 50,
            "agility": 70,
            "awareness": 50,
        }
        self.position_specific = {"route_running_short": 65}


class DummyPlayer:
    def __init__(self, position="QB"):
        self.position = position
        self.attributes = DummyAttrs()
        self.is_injured = False
        self.training_log = {}
        self._season_physical_growth = {}


class DummyTeam:
    def __init__(self, players, quality=1.0):
        self.roster = players
        self.coach_quality = quality
        self.training_plan = {}


def test_weighted_growth_player_drill():
    player = DummyPlayer("QB")
    team = DummyTeam([player])
    plan = {"type": "player", "player": player, "drill": "QB Accuracy"}

    apply_training_plan(team, plan, 1)

    assert round(player.attributes.core["throw_accuracy_short"], 2) == 60 + round(0.2 * 1.0 * 1.2, 2)
    assert round(player.attributes.core["throw_power"], 2) == 60 + round(0.2 * 0.5 * 1.2, 2)
    assert 1 in player.training_log


def test_team_vs_position_drill():
    qb = DummyPlayer("QB")
    wr = DummyPlayer("WR")
    team = DummyTeam([qb, wr])
    team_plan = {"type": "team", "drill": "Strength Circuit"}
    apply_training_plan(team, team_plan, 1)
    assert round(qb.attributes.core["strength"], 2) == 50 + round(0.2 * 1.0 * 1.0 * 0.5, 2)
    wr_strength_after = wr.attributes.core["strength"]

    pos_plan = {"type": "position", "position": "WR", "drill": "WR Footwork"}
    apply_training_plan(team, pos_plan, 2)
    assert round(wr.attributes.position_specific["route_running_short"], 2) == 65 + round(0.2 * 1.0 * 1.0, 2)
    assert wr.attributes.core["strength"] == wr_strength_after


def test_ineligible_players_skipped():
    qb = DummyPlayer("QB")
    injured_qb = DummyPlayer("QB")
    injured_qb.is_injured = True
    team = DummyTeam([qb, injured_qb])
    plan = {"type": "position", "position": "QB", "drill": "QB Accuracy"}
    apply_training_plan(team, plan, 1)
    assert "throw_accuracy_short" in qb.training_log[1]
    assert 1 not in injured_qb.training_log

def test_team_vs_position_drill():
    qb = DummyPlayer("QB")
    wr = DummyPlayer("WR")
    team = DummyTeam([qb, wr])
    team_plan = {"type": "team", "drill": "Strength Circuit"}
    apply_training_plan(team, team_plan, 1)
    assert round(qb.attributes.core["strength"], 2) == 50 + round(0.2 * 1.0 * 1.0 * 0.5, 2)
    wr_strength_after = wr.attributes.core["strength"]

def test_training_injury_triggered():
    player = DummyPlayer("QB")
    team = DummyTeam([player])
    plan = {
        "type": "player",
        "player": player,
        "drill": "Strength Circuit",
        "intensity": 1000,
    }

    apply_training_plan(team, plan, 1)

    assert player.is_injured
