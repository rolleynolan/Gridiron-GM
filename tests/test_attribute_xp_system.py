import datetime

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.systems.player.attribute_xp import (
    XP_TABLE,
    add_xp,
    apply_weekly_decay,
    rating_from_xp,
    weekly_decay_xp,
    xp_at_value,
)


def _make_player(position: str = "RB", overall: int = 70) -> Player:
    return Player(
        name="XP Tester",
        position=position,
        age=25,
        dob=datetime.date(2000, 1, 1),
        college="U",
        birth_location="USA",
        jersey_number=1,
        overall=overall,
    )


def test_training_xp_requires_milestone_crossing():
    player = _make_player()
    attr = "speed"
    start_rating = player.attributes.core[attr]
    start_xp = player.attribute_xp[attr]
    next_xp = xp_at_value(start_rating + 1)
    delta = max(0, next_xp - start_xp - 1)
    if delta:
        add_xp(player, attr, delta)
        assert player.attributes.core[attr] == start_rating
    add_xp(player, attr, 1)
    assert player.attributes.core[attr] == start_rating + 1


def test_xp_table_basics():
    assert XP_TABLE[0] == 0
    assert XP_TABLE[99] == 1_000_000
    assert XP_TABLE[1] - XP_TABLE[0] == 100


def test_piecewise_cost_ramps_and_elite_is_brutal():
    def step_cost(value: int) -> int:
        return XP_TABLE[value + 1] - XP_TABLE[value]

    assert step_cost(10) < step_cost(50) < step_cost(70) < step_cost(90) < step_cost(98)
    assert step_cost(95) >= step_cost(70) * 2


def test_rating_from_xp_consistent_with_table():
    for rating, xp_value in enumerate(XP_TABLE):
        assert rating_from_xp(xp_value) == rating
        if rating > 0:
            assert rating_from_xp(xp_value - 1) == rating - 1


def test_weekly_decay_can_drop_rating():
    player = _make_player()
    player.age = 40
    attr = "speed"
    player.dna.attribute_caps[attr]["hard_cap"] = 99
    player.attributes.core[attr] = 50
    player.attribute_xp[attr] = xp_at_value(50)
    add_xp(player, attr, 0)
    start_rating = player.attributes.core[attr]
    loss = weekly_decay_xp(player, attr, year=2025, week=1)
    assert loss >= 1
    add_xp(player, attr, -loss)
    assert player.attributes.core[attr] < start_rating


def test_weekly_decay_guard_survives_save_load():
    league = LeagueManager()
    team = Team("XP Team", "City", "XPT")
    player = _make_player()
    player.age = 40
    attr = "speed"
    player.dna.attribute_caps[attr]["hard_cap"] = 99
    player.attributes.core[attr] = 50
    team.add_player(player)
    league.add_team(team)
    league.base_seed = 123

    player.attribute_xp[attr] = xp_at_value(50)
    add_xp(player, attr, 0)

    apply_weekly_decay(league, year=2025, week=5)
    xp_after = player.attribute_xp[attr]
    apply_weekly_decay(league, year=2025, week=5)
    assert player.attribute_xp[attr] == xp_after

    data = league.to_dict()
    loaded = LeagueManager.from_dict(data)
    loaded_player = loaded.teams[0].roster[0]
    apply_weekly_decay(loaded, year=2025, week=5)
    assert loaded_player.attribute_xp[attr] == xp_after


def test_dna_caps_are_respected():
    player = _make_player()
    attr = "speed"
    current = player.attributes.core[attr]
    cap = min(99, current + 1)
    player.dna.attribute_caps[attr]["hard_cap"] = cap
    player.hidden_caps[attr] = cap
    add_xp(player, attr, 1_000_000)
    assert player.attributes.core[attr] <= cap
