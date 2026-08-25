import datetime

from gridiron_gm_pkg.simulation.entities.player import Player


def test_generated_player_overall_clamped():
    player = Player(
        name="Clamp Test",
        position="QB",
        age=24,
        dob=datetime.date(2000, 1, 1),
        college="U",
        birth_location="USA",
        jersey_number=1,
        overall=150,
    )
    assert 0 <= player.overall <= 99


def test_loaded_player_clamps_attributes_and_overall():
    data = {
        "name": "Loaded",
        "position": "WR",
        "age": 22,
        "dob": "2002-01-01",
        "college": "U",
        "birth_location": "USA",
        "jersey_number": 10,
        "overall": 120,
        "attributes": {
            "core": {"speed": 150, "agility": 110},
            "position_specific": {"catching": 130},
        },
        "pot": 80,
    }
    player = Player.from_dict(data)
    assert player.attributes.core["speed"] <= 99
    assert player.attributes.position_specific["catching"] <= 99
    assert 0 <= player.overall <= 99


def test_pot_always_at_least_overall_and_clamped():
    player = Player(
        name="Pot Clamp",
        position="RB",
        age=23,
        dob=datetime.date(2001, 1, 1),
        college="U",
        birth_location="USA",
        jersey_number=20,
        overall=95,
        potential=80,
    )
    assert player.pot >= player.overall
    assert 0 <= player.pot <= 99
