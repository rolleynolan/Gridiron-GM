import random
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3]))
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.injury_manager import InjuryEngine

random.seed(0)

def make_player(name, position):
    return Player(name, position, 25, "2000-01-01", "U", "USA", 1, 80)

def test_leg_injury_affects_wr_more_than_qb():
    engine = InjuryEngine()
    wr = make_player("WR", "WR")
    qb = make_player("QB", "QB")
    wr.agility = qb.agility = 80
    wr.acceleration = qb.acceleration = 80
    wr.balance = qb.balance = 80

    engine.assign_injury(wr, injury_name="Knee Contusion")
    engine.assign_injury(qb, injury_name="Knee Contusion")

    wr_impact = abs(wr.active_injury_effects.get("agility", 0))
    qb_impact = abs(qb.active_injury_effects.get("agility", 0))
    assert wr_impact > qb_impact
    assert wr.get_effective_attribute("agility") == wr.agility - wr_impact


def test_qb_shoulder_injury_affects_throw_power():
    engine = InjuryEngine()
    qb = make_player("QB2", "QB")
    qb.position_specific["throw_power"] = 90
    qb.position_specific["throw_accuracy_short"] = 85

    engine.assign_injury(qb, injury_name="Shoulder Dislocation")
    impact = abs(qb.active_injury_effects.get("throw_power", 0))
    assert impact > 0
    assert qb.get_effective_attribute("throw_power") == 90 - impact


def test_injury_effect_clears_on_heal():
    engine = InjuryEngine()
    qb = make_player("QB3", "QB")
    qb.position_specific["throw_power"] = 90
    injury = engine.assign_injury(qb, injury_name="Shoulder Dislocation")
    qb.weeks_out = 1
    class DummyTeam:
        def __init__(self, roster):
            self.roster = roster
            self.ir_list = []
        def remove_player_from_ir(self, player):
            self.ir_list.remove(player)
    team = DummyTeam([qb])
    engine.recover_weekly(team)
    assert qb.active_injury_effects == {}

