import json
import os
from pathlib import Path
import random

import sys

# Ensure repository root on sys.path so package imports work when run directly
ROOT_DIR = Path(__file__).resolve().parents[1]
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.utils.player_generator import PlayerGenerator
from gridiron_gm_pkg.simulation.utils.roster_generator import RosterGenerator

TEAMS_FILE = Path(__file__).resolve().parents[1] / "gridiron_gm_pkg" / "config" / "teams.json"
SAVE_DIR = Path(__file__).resolve().parents[1] / "save"
LEAGUE_STATE_PATH = SAVE_DIR / "league_state.json"


def generate_league_state() -> None:
    with open(TEAMS_FILE, "r", encoding="utf-8") as f:
        teams_data = json.load(f)

    pg = PlayerGenerator()
    rg = RosterGenerator(pg)
    teams = []

    for entry in teams_data:
        team = Team(
            team_name=entry.get("name"),
            city=entry.get("city"),
            abbreviation=entry.get("abbreviation"),
            conference=entry.get("conference", "Nova"),
            division=entry.get("division", "Unknown"),
            id=entry.get("id"),
        )
        roster = rg.generate_team_roster()
        for p in roster:
            team.add_player(p, position_override=p.position)
        team.generate_depth_chart()
        teams.append(team)

    free_agents = rg.generate_free_agents(120)

    league_state = {
        "week": 0,
        "teams": [
            {
                "id": t.id,
                "city": t.city,
                "name": t.team_name,
                "abbreviation": t.abbreviation,
                "roster": [p.to_dict() for p in t.players],
            }
            for t in teams
        ],
        "free_agents": [p.to_dict() for p in free_agents],
    }

    os.makedirs(SAVE_DIR, exist_ok=True)
    with open(LEAGUE_STATE_PATH, "w", encoding="utf-8") as f:
        json.dump(league_state, f, indent=2)
    print(f"League state written to {LEAGUE_STATE_PATH}")


if __name__ == "__main__":
    generate_league_state()
