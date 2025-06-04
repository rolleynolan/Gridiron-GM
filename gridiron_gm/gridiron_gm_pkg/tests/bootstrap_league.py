import os
import json
from pathlib import Path
from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm.gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager
from gridiron_gm.gridiron_gm_pkg.simulation.utils.generate_schedule import generate_schedule
from gridiron_gm.gridiron_gm_pkg.simulation.entities.league import LeagueManager

save_name = "test_league"
calendar = Calendar()

# === Build test teams and players ===

BASE_DIR = Path(__file__).resolve().parents[1]

with open(BASE_DIR / "config" / "teams.json", "r") as f:
    team_templates = json.load(f)

teams = []

for template in team_templates:
    team = Team(
        team_name=template["name"],
        city=template["city"],
        abbreviation=template["abbreviation"]
    )

    for i in range(53):
        player = Player(
            name=f"{team.abbreviation} Player {i}",
            position="RB" if i % 3 == 0 else "WR" if i % 3 == 1 else "QB",
            age=24,
            dob="2000-01-01",
            college="Test U",
            birth_location="Nowhere",
            jersey_number=i + 1,
            overall=65
        )
        team.add_player(player)
    teams.append(team)

# Save-ready dict
raw_league_data = { "teams": [team.to_dict() for team in teams] }

# Convert to LeagueManager for SeasonManager use
league = LeagueManager.from_dict(raw_league_data)

# Create and save
season_manager = SeasonManager(calendar, league, save_name)
season_manager.save_league_state()
season_manager.standings_manager.save_standings()
generate_schedule(save_name)

print("✅ League created and saved successfully.")
