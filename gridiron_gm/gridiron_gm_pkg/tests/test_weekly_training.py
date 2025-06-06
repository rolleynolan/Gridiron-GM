import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.weekly_training import apply_weekly_training

class DummyAttrs:
    def __init__(self):
        self.core = {"throw_accuracy_short": 60, "throw_power": 60, "awareness": 50, "speed": 80}
        self.position_specific = {"route_running_short": 65}

class DummyPlayer:
    def __init__(self):
        self.attributes = DummyAttrs()
        self.training_focus = "throwing"
        self.is_injured = False

class DummyTeam:
    def __init__(self, player, quality=1.0):
        self.roster = [player]
        self.practice_squad = []
        self.coach_quality = quality
        self.current_week = 1


def test_training_applies_growth(monkeypatch):
    player = DummyPlayer()
    team = DummyTeam(player, quality=1.1)

    monkeypatch.setattr("random.uniform", lambda a, b: b)

    apply_weekly_training(player, team)

    # Accuracy (mental) should grow more than throw_power (physical)
    assert player.attributes.core["throw_accuracy_short"] > 60
    assert player.attributes.core["throw_power"] > 60
    assert player.attributes.core["throw_accuracy_short"] - 60 > player.attributes.core["throw_power"] - 60


def test_injured_or_no_focus_no_growth(monkeypatch):
    player = DummyPlayer()
    team = DummyTeam(player)
    player.training_focus = None

    monkeypatch.setattr("random.uniform", lambda a, b: b)
    apply_weekly_training(player, team)
    assert player.attributes.core["throw_accuracy_short"] == 60

    player.training_focus = "throwing"
    player.is_injured = True
    apply_weekly_training(player, team)
    assert player.attributes.core["throw_accuracy_short"] == 60


def test_physical_growth_cap(monkeypatch):
    player = DummyPlayer()
    player.training_focus = "speed"
    team = DummyTeam(player)

    monkeypatch.setattr("random.uniform", lambda a, b: b)
    for _ in range(20):
        apply_weekly_training(player, team)
    # cap at +2 total
    assert round(player.attributes.core["speed"] - 80, 3) <= 2.0


def test_coach_multiplier(monkeypatch):
    player = DummyPlayer()
    team = DummyTeam(player, quality=1.2)

    monkeypatch.setattr("random.uniform", lambda a, b: b)
    apply_weekly_training(player, team)

    # mental attribute gain should reflect multiplier 1.2
    assert round(player.attributes.core["throw_accuracy_short"] - 60, 2) == round(0.5 * 1.2, 2)
