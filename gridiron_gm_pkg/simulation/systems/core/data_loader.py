import os
import json
from pathlib import Path
from gridiron_gm_pkg.simulation.persistence.savegame import load_league, save_league

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
    league_path = base_path / "league.json"
    save_league(league_path, league)

def load_league_from_file(save_name, league_class):
    """
    Loads a league object from file, including draft prospects if present.
    """
    base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name
    league_path = base_path / "league.json"
    if not os.path.exists(league_path):
        raise FileNotFoundError(f"League file not found: {league_path}")
    return load_league(league_path)

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
