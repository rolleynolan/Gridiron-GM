import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.player_season_progression import (
    evaluate_player_season_progression,
)


class DummyAttributes:
    def __init__(self, core=None, pos=None):
        self.core = core or {}
        self.position_specific = pos or {}


class DummyPlayer:
    def __init__(self, position):
        self.position = position
        self.attributes = DummyAttributes(
            core={"agility": 80, "tackling": 70, "play_recognition": 70},
            pos={"catching": 75, "break_tackle": 70, "coverage": 72},
        )
        self.hidden_caps = {
            "catching": 90,
            "agility": 90,
            "break_tackle": 80,
            "tackling": 90,
            "play_recognition": 90,
            "coverage": 95,
        }


def test_wr_progression_positive():
    player = DummyPlayer("WR")
    stats = {"drops": 2, "targets": 60, "receptions": 50, "yards_after_catch": 400}
    snaps = {"offense": 500}

    delta = evaluate_player_season_progression(player, stats, snaps)

    assert delta["catching"] > 0
    assert delta["agility"] > 0
    assert delta["break_tackle"] > 0


def test_lb_regression():
    player = DummyPlayer("LB")
    stats = {"missed_tackles": 20}
    snaps = {"defense": 100}

    delta = evaluate_player_season_progression(player, stats, snaps)

    assert delta["tackling"] < 0
    assert delta["play_recognition"] < 0
