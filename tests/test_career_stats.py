import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player


def make_game_world():
    return {
        "league_records": {
            "players": {"single_game": {}, "single_season": {}, "career": {}},
            "teams": {"single_game": {}, "single_season": {}, "career": {}},
            "leaderboards": {"current_season": {}, "career": {}}
        }
    }


def test_career_stats_aggregation_and_records():
    gw = make_game_world()
    p = Player("Test QB", "QB", 25, "2000-01-01", "U", "USA", 1, 80)
    p.season_stats = {
        "2025": {"season_totals": {"passing_yards": 4000, "sacks": 30}}
    }
    p.update_career_stats_from_season("2025", gw)

    assert p.career_stats["passing_yards"] == 4000
    assert p.season_stats["2025"]["career_added"] is True

    # call again to ensure no duplication
    p.update_career_stats_from_season("2025", gw)
    assert p.career_stats["passing_yards"] == 4000

    # next season
    p.season_stats["2026"] = {"season_totals": {"passing_yards": 3500, "sacks": 25}}
    p.update_career_stats_from_season("2026", gw)
    assert p.career_stats["passing_yards"] == 7500

    lb = gw["league_records"]["leaderboards"]["career"]["passing_yards"]
    assert lb[0][0] == p.id and lb[0][1] == 7500

