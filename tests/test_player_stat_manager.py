import sys
from pathlib import Path

# Add repository root to path for imports
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.stats.player_stat_manager import (
    update_player_stats,
    update_career_stats,
)


class DummyPlayer:
    def __init__(self):
        self.season_stats = {}
        self.snap_counts = {}
        self.career_stats = {}


def test_update_player_stats_accumulates():
    player = DummyPlayer()
    update_player_stats(player, week=1, year=2025,
                        stat_dict={"targets": 5, "drops": 1},
                        snap_counts={"offense": 40})
    update_player_stats(player, week=2, year=2025,
                        stat_dict={"targets": 7, "drops": 0},
                        snap_counts={"offense": 42})

    year_stats = player.season_stats.get("2025")
    assert year_stats is not None
    totals = year_stats["season_totals"]
    assert totals["targets"] == 12
    assert totals["drops"] == 1
    assert totals["snap_counts"]["offense"] == 82
    assert year_stats["game_logs"][1]["targets"] == 5
    assert year_stats["game_logs"][2]["snaps"]["offense"] == 42


def test_update_career_stats():
    player = DummyPlayer()
    season_totals = {"targets": 10, "drops": 1, "snap_counts": {"offense": 70}}
    update_career_stats(player, season_totals)

    assert player.career_stats["targets"] == 10
    assert player.career_stats["drops"] == 1
    assert player.career_stats["snap_counts"]["offense"] == 70

    # second season aggregation
    season_totals2 = {"targets": 5, "drops": 0, "snap_counts": {"offense": 30}}
    update_career_stats(player, season_totals2)

    assert player.career_stats["targets"] == 15
    assert player.career_stats["drops"] == 1
    assert player.career_stats["snap_counts"]["offense"] == 100

