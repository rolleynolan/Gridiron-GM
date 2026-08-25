from pathlib import Path
import random
import sys

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA


LEAGUE_POSITIONS = [
    "QB",
    "RB",
    "WR",
    "TE",
    "LT",
    "LG",
    "C",
    "RG",
    "RT",
    "DE",
    "DT",
    "LB",
    "OLB",
    "MLB",
    "CB",
    "S",
    "FS",
    "SS",
    "K",
    "P",
    "EDGE",
    "OL",
]


def _relevant_attributes(position: str) -> list[str]:
    dummy = type("DummyPlayer", (), {"position": position})()
    core = Player.init_core_attributes(dummy)
    pos = Player.init_position_attributes(dummy)
    ordered = list(core.keys()) + list(pos.keys())
    seen = set()
    result: list[str] = []
    for name in ordered:
        if name not in seen:
            seen.add(name)
            result.append(name)
    return result


def _avg_current(attribute_caps: dict, relevant: list[str]) -> float:
    values = [attribute_caps[attr]["current"] for attr in relevant]
    return sum(values) / len(values) if values else 0.0


def test_player_dna_caps_cover_all_relevant_attributes():
    rng = random.Random(12345)
    for is_college in (False, True):
        for position in LEAGUE_POSITIONS:
            dna = PlayerDNA.generate_random_dna(position, is_college=is_college, rng=rng)
            relevant = _relevant_attributes(position)
            assert set(dna.attribute_caps.keys()) == set(relevant)
            for attr in relevant:
                caps = dna.attribute_caps[attr]
                assert "current" in caps
                assert "hard_cap" in caps
                cur = caps["current"]
                hard = caps["hard_cap"]
                assert isinstance(cur, int)
                assert isinstance(hard, int)
                assert 0 <= cur <= 99
                assert 0 <= hard <= 99
                assert hard >= cur
            assert _avg_current(dna.attribute_caps, relevant) > 40
