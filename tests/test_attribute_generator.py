import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import inspect

import numpy as np

from gridiron_gm_pkg.simulation.systems.player import attribute_generator


import pytest


@pytest.mark.parametrize(
    "pos",
    [
        "QB",
        "RB",
        "WR",
        "TE",
        "OL",
        "EDGE",
        "DL",
        "LB",
        "CB",
        "S",
        "K",
        "P",
    ],
)
def test_generator_returns_values(pos):
    attrs, caps = attribute_generator.generate_attributes_for_position(pos)
    assert isinstance(attrs, dict)
    assert isinstance(caps, dict)
    for base_attr in ["iq", "awareness", "tackling", "catching"]:
        assert base_attr in attrs
        assert base_attr in caps
    for attr, value in attrs.items():
        assert attr in caps
        assert value <= caps[attr]
        assert 0 <= value <= 99
        assert 0 <= caps[attr] <= 99


def test_randomness_uses_numpy():
    source = inspect.getsource(attribute_generator.bell_curve_sample)
    assert "np.random.normal" in source
