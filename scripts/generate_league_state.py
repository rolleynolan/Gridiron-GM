import json
import os
from pathlib import Path

import sys

# Ensure repository root on sys.path so package imports work when run directly
ROOT_DIR = Path(__file__).resolve().parents[1]
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

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

    def player_entry(player):
        data = {
            "name": player.name,
            "position": player.position,
            "college": player.college,
        }
        if getattr(player, "overall", None) is not None:
            data["overall"] = player.overall
        return data

    league_state = {
        "teams": [],
        "free_agents": [],
    }

    for entry in teams_data:
        roster = rg.generate_team_roster()
        league_state["teams"].append(
            {
                "team": entry.get("abbreviation"),
                "players": [player_entry(p) for p in roster],
            }
        )

    league_state["free_agents"] = [player_entry(p) for p in rg.generate_free_agents(120)]

    os.makedirs(SAVE_DIR, exist_ok=True)
    with open(LEAGUE_STATE_PATH, "w", encoding="utf-8") as f:
        json.dump(league_state, f, indent=2)
    print(f"League state written to {LEAGUE_STATE_PATH}")


if __name__ == "__main__":
    generate_league_state()
