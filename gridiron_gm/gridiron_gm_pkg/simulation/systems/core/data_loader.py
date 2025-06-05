import os
import json
from pathlib import Path

def load_schedule_files(save_name, calendar=None):
    base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name
    schedule_path = base_path / "schedule_by_week.json"
    results_path = base_path / "results_by_week.json"
    if os.path.exists(schedule_path):
        with open(schedule_path, "r") as f:
            schedule_by_week = json.load(f)
    else:
        schedule_by_week = {}
    if os.path.exists(results_path):
        with open(results_path, "r") as f:
            results_by_week = json.load(f)
    else:
        results_by_week = {}
    return schedule_by_week, results_by_week

def save_results(results_by_week, save_name):
    results_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name / "results_by_week.json"
    os.makedirs(results_path.parent, exist_ok=True)
    with open(results_path, "w") as f:
        json.dump(results_by_week, f, indent=2)

def save_league_state(league, save_name):
    base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name
    os.makedirs(base_path, exist_ok=True)
    league_path = base_path / "league.json"
    with open(league_path, "w") as f:
        if hasattr(league, "to_dict"):
            # Ensures draft_prospects are included if present in league.to_dict()
            json.dump(league.to_dict(), f, indent=2)
        else:
            json.dump(league, f, indent=2)

def load_league_from_file(save_name, league_class):
    """
    Loads a league object from file, including draft prospects if present.
    """
    base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name
    league_path = base_path / "league.json"
    if not os.path.exists(league_path):
        raise FileNotFoundError(f"League file not found: {league_path}")
    with open(league_path, "r") as f:
        data = json.load(f)
    league = league_class.from_dict(data)
    # Ensure draft prospects are loaded if present
    if "draft_prospects" in data:
        from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
        league.draft_prospects = [Player.from_dict(p) for p in data["draft_prospects"]]
    return league

def save_playoff_bracket(playoff_bracket, save_name):
    base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name
    bracket_path = base_path / "playoff_bracket.json"
    os.makedirs(base_path, exist_ok=True)
    with open(bracket_path, "w") as f:
        json.dump(playoff_bracket, f, indent=2)

def save_playoff_results(playoff_results, save_name):
    base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name
    results_path = base_path / "playoff_results.json"
    os.makedirs(base_path, exist_ok=True)
    with open(results_path, "w") as f:
        json.dump(playoff_results, f, indent=2)
