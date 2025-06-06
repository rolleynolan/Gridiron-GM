import random
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm.gridiron_gm_pkg.stats.player_stat_manager import update_player_stats
from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.breakout_tracker import (
    check_breakout_game,
)


random.seed(1)


def make_league_context(year=2025):
    return {"current_year": year, "weekly_stats": {}}


def test_backup_wr_breakout():
    p = Player("Breakout WR", "WR", 24, "2001-01-01", "U", "USA", 11, 72)
    p.attributes.position_specific = {"catching": 70, "route_running": 70}

    update_player_stats(p, 1, 2025, {"receiving_yards": 10}, {"offense": 10})
    update_player_stats(
        p,
        3,
        2025,
        {"receiving_yards": 120, "receiving_tds": 2, "receptions": 6},
        {"offense": 60},
    )

    ctx = make_league_context()
    result = check_breakout_game(
        p, {"receiving_yards": 120, "receiving_tds": 2}, ctx, 3
    )

    assert result is True
    assert 3 in p.breakout_log
    boosts = p.breakout_log[3]["boosts"]
    for attr, inc in boosts.items():
        if attr in p.attributes.position_specific:
            assert p.attributes.position_specific[attr] >= 70 + inc


def test_star_wr_no_breakout():
    p = Player("Star WR", "WR", 28, "1997-01-01", "U", "USA", 10, 92)
    p.dev_arc.type = "x_factor"
    p.attributes.position_specific = {"catching": 90}
    update_player_stats(
        p,
        3,
        2025,
        {"receiving_yards": 120, "receiving_tds": 2},
        {"offense": 70},
    )

    ctx = make_league_context()
    result = check_breakout_game(
        p, {"receiving_yards": 120, "receiving_tds": 2}, ctx, 3
    )

    assert result is False
    assert getattr(p, "breakout_log", {}) == {}


def test_low_usage_rb_breakout():
    p = Player("Backup RB", "RB", 23, "2002-01-01", "U", "USA", 22, 68)
    p.attributes.position_specific = {"speed": 80, "agility": 78}

    update_player_stats(p, 1, 2025, {"rushing_yards": 30, "rush_attempts": 10}, {"offense": 20})
    update_player_stats(
        p,
        4,
        2025,
        {"rushing_yards": 100, "rush_attempts": 15, "rushing_tds": 1},
        {"offense": 40},
    )

    ctx = make_league_context()
    result = check_breakout_game(
        p, {"rushing_yards": 100, "rushing_tds": 1}, ctx, 4
    )

    assert result is True
    assert 4 in p.breakout_log
    boosts = p.breakout_log[4]["boosts"]
    assert boosts
    for attr, inc in boosts.items():
        if attr in p.attributes.position_specific:
            assert p.attributes.position_specific[attr] >= 80 - 2 + inc or p.attributes.position_specific[attr] >= 78 + inc

