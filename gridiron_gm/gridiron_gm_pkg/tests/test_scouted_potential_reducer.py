import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.scouted_potential_reducer import (
    reevaluate_scouted_potential,
)


class DummyPlayer:
    def __init__(self):
        self.hidden_caps = {"catching": 95}
        self.scouted_potential = {"catching": 91}
        self.last_attribute_values = {"catching": 88}
        self.no_growth_years = {"catching": 3}
        self.strong_season = False


def test_stagnation_then_recovery():
    player = DummyPlayer()
    current = {"catching": 88}

    result = reevaluate_scouted_potential(player, current)
    assert result["catching"] == 90
    assert player.scouted_potential["catching"] >= 88

    player.strong_season = True
    improved_attrs = {"catching": 90}

    result2 = reevaluate_scouted_potential(player, improved_attrs)
    assert result2["catching"] == 91
    assert player.scouted_potential["catching"] == 91
