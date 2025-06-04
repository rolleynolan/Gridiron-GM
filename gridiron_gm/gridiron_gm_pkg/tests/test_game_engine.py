import pytest
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3]))
from gridiron_gm.gridiron_gm_pkg.simulation.engine import game_engine
from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.team_data import fill_team_rosters_with_dummy_players  # <-- Import the shared roster generator

# --- Shared test team setup using the new roster generator ---

def make_real_team(abbreviation="TST"):
    """
    Create a test team and populate its roster and depth_chart using the shared generator.
    Ensures all required positions are filled for valid simulation.
    """
    team = Team(
        team_name=abbreviation,
        abbreviation=abbreviation,
        city="Test City",
        conference="Test Conference"
    )
    fill_team_rosters_with_dummy_players([team])  # <-- FIXED: pass as a list
    return team

# --- End shared setup ---

def test_apply_fatigue_increases_fatigue():
    player = Player(
        name="Test",
        position="QB",
        age=22,
        dob="2002-01-01",
        college="Test U",
        birth_location="Testville, USA",
        jersey_number=1,
        overall=75
    )
    before = getattr(player, "fatigue", 0)
    game_engine.apply_fatigue(player, 2.0)
    assert getattr(player, "fatigue", 0) > before

def test_simulate_pass_play_returns_valid_result():
    team = make_real_team("PASS")
    qb = team.depth_chart["QB"][0]
    wr = team.depth_chart["WR"][0]
    context = {
        "offense": {},
        "defense": {},
        "home_team": {},
        "away_team": {},
        "weather": None
    }
    result = game_engine.simulate_pass_play(qb, [wr], "short", context)
    assert "yards" in result
    assert "log" in result

def test_simulate_run_play_returns_valid_result():
    team = make_real_team("RUN")
    runner = team.depth_chart["RB"][0]
    context = {
        "offense": {},
        "defense": {},
        "home_team": {},
        "away_team": {},
        "weather": None
    }
    result = game_engine.simulate_run_play(runner, "inside", context)
    assert "yards" in result
    assert "log" in result

def test_run_drive_returns_log_and_score():
    team = make_real_team("OFF")
    defense = make_real_team("DEF")
    class DummySubManager:
        def get_active_lineup_with_bench_log(self, formation, offense, fatigue_log, scheme):
            # Use the actual depth_chart for the test team
            return {
                "offense": [p for pos in team.depth_chart for p in team.depth_chart[pos]],
                "defense": [p for pos in defense.depth_chart for p in defense.depth_chart[pos]]
            }, []
    fatigue_log = []
    context = {
        "offense": team,
        "defense": defense,
        "home_team": team,
        "away_team": defense,
        "weather": None
    }
    result = game_engine.sim_drive(team, defense, DummySubManager(), fatigue_log, context)
    assert "log" in result
    assert "score" in result

def test_simulate_game_runs_without_error():
    team_a = make_real_team("A")
    team_b = make_real_team("B")
    try:
        game_engine.simulate_game(team_a, team_b, week=1)
    except Exception as e:
        pytest.fail(f"simulate_game raised an exception: {e}")

if __name__ == "__main__":
    team_a = make_real_team("AAA")
    team_b = make_real_team("BBB")
    result = game_engine.simulate_game(team_a, team_b, week=1)
    if isinstance(result, tuple) and len(result) == 2:
        home_stats, away_stats = result
        print("HOME TEAM STATS:")
        print(home_stats)
        print("AWAY TEAM STATS:")
        print(away_stats)
        print(f"FINAL SCORE: {team_a.abbreviation} {home_stats['points']} - {team_b.abbreviation} {away_stats['points']}")
    else:
        print("[ERROR] simulate_game did not return (home_stats, away_stats):", result)
    print(f"[DEBUG] {team_a.abbreviation} depth_chart keys: {list(team_a.depth_chart.keys())}")
    print(f"[DEBUG] {team_a.abbreviation} QB depth: {team_a.depth_chart.get('QB')}")

# --- Documentation ---
# All test scripts that simulate games should use make_real_team() as shown above.
# This ensures every test team has a valid roster and depth_chart for simulation.