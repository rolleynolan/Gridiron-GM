import sys
from pathlib import Path

# Add repository root to path for imports
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.player_season_progression import (
    evaluate_player_season_progression,
)


class MockAttributes:
    def __init__(self, core=None, pos=None):
        self.core = core or {}
        self.position_specific = pos or {}


class MockPlayer:
    def __init__(self):
        self.name = "Integration WR"
        self.age = 24
        self.position = "WR"
        self.attributes = MockAttributes(
            core={"agility": 88},
            pos={"catching": 85, "separation": 82},
        )
        self.hidden_caps = {"catching": 95, "agility": 90, "separation": 90}
        self.scouted_potential = {
            "catching": 92,
            "agility": 89,
            "separation": 85,
        }


def test_wr_season_progression_integration():
    player = MockPlayer()
    season_stats = {
        "targets": 110,
        "drops": 2,
        "receptions": 90,
        "yards_after_catch": 600,
    }
    snap_counts = {"offense": 950}

    deltas = evaluate_player_season_progression(player, season_stats, snap_counts)

    # Apply deltas to player attributes
    for attr, change in deltas.items():
        if attr in player.attributes.core:
            player.attributes.core[attr] += change
        elif attr in player.attributes.position_specific:
            player.attributes.position_specific[attr] += change

    # Debug output
    print("Updated attributes:", player.attributes.core, player.attributes.position_specific)

    # Assertions
    expected_keys = {"catching", "agility", "separation"}
    assert set(deltas.keys()) == expected_keys

    assert player.attributes.position_specific["catching"] >= 86
    assert player.attributes.core["agility"] >= 89
    assert player.attributes.position_specific["separation"] >= 83

    assert player.attributes.position_specific["catching"] <= player.hidden_caps["catching"]
    assert player.attributes.core["agility"] <= player.hidden_caps["agility"]
    assert player.attributes.position_specific["separation"] <= player.hidden_caps["separation"]
