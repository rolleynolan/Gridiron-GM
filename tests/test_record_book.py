import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from gridiron_gm.gridiron_gm_pkg.stats.record_book import (
    update_single_game_record,
    update_single_season_record,
    update_career_record,
    update_leaderboard,
    update_team_single_game_record,
    update_team_single_season_record,
    update_team_career_record,
)


def make_game_world():
    return {
        "league_records": {
            "players": {"single_game": {}, "single_season": {}, "career": {}},
            "teams": {"single_game": {}, "single_season": {}, "career": {}},
            "leaderboards": {"current_season": {}}
        }
    }


def test_player_single_game_record_updates():
    gw = make_game_world()
    update_single_game_record(gw, "p1", "passing_yards", 300)
    update_single_game_record(gw, "p2", "passing_yards", 250)
    assert gw["league_records"]["players"]["single_game"]["passing_yards"]["player_id"] == "p1"
    update_single_game_record(gw, "p2", "passing_yards", 350)
    rec = gw["league_records"]["players"]["single_game"]["passing_yards"]
    assert rec == {"player_id": "p2", "value": 350}


def test_leaderboard_tracks_top_ten():
    gw = make_game_world()
    for i in range(12):
        update_leaderboard(gw, "passing_yards", f"p{i}", i * 100)
    board = gw["league_records"]["leaderboards"]["current_season"]["passing_yards"]
    assert len(board) == 10
    assert board[0] == ("p11", 1100)
    assert all(board[i][1] >= board[i+1][1] for i in range(len(board)-1))


def test_team_records_update():
    gw = make_game_world()
    update_team_single_game_record(gw, "t1", "points_scored", 40)
    update_team_single_game_record(gw, "t2", "points_scored", 35)
    assert gw["league_records"]["teams"]["single_game"]["points_scored"]["team_id"] == "t1"
    update_team_single_game_record(gw, "t2", "points_scored", 45)
    rec = gw["league_records"]["teams"]["single_game"]["points_scored"]
    assert rec == {"team_id": "t2", "value": 45}
