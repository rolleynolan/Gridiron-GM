import datetime

from gridiron_gm_pkg.simulation.entities.player import Player


def _make_player(position="QB", potential=85):
    return Player(
        name="Tester",
        position=position,
        age=25,
        dob=datetime.date(2000, 1, 1),
        college="U",
        birth_location="USA",
        jersey_number=1,
        overall=70,
        potential=potential,
    )


def test_get_fatigue_rate_returns_float():
    player = _make_player()
    player.attributes.core["stamina"] = None
    rate = player.get_fatigue_rate()
    assert isinstance(rate, float)
    assert rate > 0


def test_get_effective_attribute_core_and_position_specific():
    player = _make_player()
    player.attributes.core["speed"] = 88
    player.attributes.position_specific["throw_power"] = 92
    assert player.get_effective_attribute("speed") == 88
    assert player.get_effective_attribute("throw_power") == 92


def test_get_effective_attribute_missing_returns_zero():
    player = _make_player()
    assert player.get_effective_attribute("nonexistent_attr") == 0


def test_potential_round_trip():
    player = _make_player(potential=91)
    assert player.potential == 91
    data = player.to_dict()
    loaded = Player.from_dict(data)
    assert loaded.potential == 91
