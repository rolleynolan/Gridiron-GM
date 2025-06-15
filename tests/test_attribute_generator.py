import sys, pathlib
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1]))
import inspect

import numpy as np

from gridiron_gm_pkg.simulation.systems.player import attribute_generator


def test_generator_returns_values():
    attrs, caps = attribute_generator.generate_attributes_for_position("QB")
    assert isinstance(attrs, dict)
    assert isinstance(caps, dict)
    # Each attribute should have corresponding cap
    for attr, value in attrs.items():
        assert attr in caps
        assert value <= caps[attr]
        assert 0 <= value <= 99
        assert 0 <= caps[attr] <= 99


def test_randomness_uses_numpy():
    source = inspect.getsource(attribute_generator.bell_curve_sample)
    assert "np.random.normal" in source
