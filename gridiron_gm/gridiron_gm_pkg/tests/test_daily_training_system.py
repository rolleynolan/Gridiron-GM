import sys
from pathlib import Path
from datetime import date
import random

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))
random.seed(0)

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.daily_training import (
    should_train_today,
    assign_training,
    apply_training,
    run_daily_training,
    log_season_progress_checkpoint,
)
from gridiron_gm.gridiron_gm_pkg.config.training_catalog import TRAINING_CATALOG


class DummyAttrs:
    def __init__(self):
        self.core = {
            "throw_accuracy_short": 60,
            "throw_power": 60,
            "strength": 50,
        }
        self.position_specific = {}


class DummyPlayer:
    def __init__(self, position="QB"):
        self.id = position
        self.position = position
        self.attributes = DummyAttrs()
        self.is_injured = False
        self.progress_history = {}


class DummyTeam:
    def __init__(self, players):
        self.id = "T1"
        self.roster = players
        self.training_schedule = {}


def test_training_eligibility():
    schedule = {
        "T1": [
            {"week": "1", "day": "Monday", "home": False},
        ]
    }
    team = DummyTeam([DummyPlayer()])
    # Game day
    assert not should_train_today(team, (1, "Monday"), schedule)
    # Travel day before away game
    assert not should_train_today(team, (1, "Sunday"), schedule)
    # Normal day
    assert should_train_today(team, (1, "Wednesday"), schedule)


def test_assign_and_apply_training(monkeypatch):
    player = DummyPlayer()
    team = DummyTeam([player])
    schedule = {"T1": []}
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.player.daily_training.random.choice",
        lambda seq: seq[0],
    )
    assignments = assign_training(team, (1, "Wednesday"))
    assert player.id in assignments
    apply_training(team, (1, "Wednesday"), assignments)
    drill = TRAINING_CATALOG[assignments[player.id]]
    expected = 60 + list(drill["attribute_weights"].values())[0]
    assert player.attributes.core["throw_accuracy_short"] == expected


def test_progress_checkpoint():
    player = DummyPlayer()
    log_season_progress_checkpoint(date(2025, 8, 1), [player], "preseason")
    assert "2025-preseason" in player.progress_history
    # Only keep two snapshots
    log_season_progress_checkpoint(date(2025, 12, 1), [player], "postseason")
    log_season_progress_checkpoint(date(2026, 8, 1), [player], "preseason")
    assert len(player.progress_history) == 2

