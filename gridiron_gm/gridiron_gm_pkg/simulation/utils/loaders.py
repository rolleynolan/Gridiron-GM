import json
import os
from pathlib import Path
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.standings_manager import StandingsManager

BASE_DIR = Path(__file__).resolve().parents[2]

def load_teams_from_config(save_name=None):
    config_path = BASE_DIR / "config" / "teams.json"
    if not config_path.exists():
        raise FileNotFoundError(f"Missing file: {config_path}")
    with open(config_path, "r") as f:
        teams = json.load(f)
    return teams  # <--- returns a list of dicts




def initialize_league():
    teams = load_teams_from_config()
    league = {"teams": teams}
    standings_manager = StandingsManager(teams)
    return league, standings_manager
