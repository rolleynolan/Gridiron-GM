import json
import os
from engine.core.standings_manager import StandingsManager

def load_teams_from_config(save_name=None):
    config_path = "config/teams.json"
    if not os.path.exists(config_path):
        raise FileNotFoundError(f"Missing file: {config_path}")
    with open(config_path, "r") as f:
        teams = json.load(f)
    return teams  # <--- returns a list of dicts




def initialize_league():
    teams = load_teams_from_config()
    league = {"teams": teams}
    standings_manager = StandingsManager(teams)
    return league, standings_manager
