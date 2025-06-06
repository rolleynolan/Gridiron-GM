import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.injury_manager import InjuryEngine

engine = InjuryEngine()

class DummyTeam:
    def __init__(self, player):
        self.roster = [player]
        self.ir_list = []
    def remove_player_from_ir(self, player):
        if player in self.ir_list:
            self.ir_list.remove(player)


def make_player(position):
    p = Player("Test", position, 25, "2000-01-01", "U", "USA", 1, 80)
    p.attributes.core["speed"] = 80
    p.attributes.core["agility"] = 80
    p.attributes.core["strength"] = 80
    return p


def test_attribute_penalty_applied_and_weighted():
    rb = make_player("RB")
    qb = make_player("QB")
    engine._apply_injury(rb, "ACL Tear", engine.injury_catalog["ACL Tear"], 10, "injured")
    engine._apply_injury(qb, "ACL Tear", engine.injury_catalog["ACL Tear"], 10, "injured")
    rb_val = rb.get_effective_attribute("speed")
    qb_val = qb.get_effective_attribute("speed")
    assert rb_val < qb_val  # RB penalty larger due to weight


def test_minor_injury_has_effect_and_clears():
    player = make_player("WR")
    engine._apply_injury(player, "Shin Splints", engine.injury_catalog["Shin Splints"], 1, "injured")
    effective_before = player.get_effective_attribute("speed")
    assert effective_before < 80
    team = DummyTeam(player)
    engine.recover_weekly(team)
    effective_after = player.get_effective_attribute("speed")
    assert effective_after == player.attributes.core["speed"]
