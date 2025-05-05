import os
import json
from gridiron_gm.engine.core.team import Team
from gridiron_gm.engine.core.player import Player

SAVE_FOLDER = "saves"

def ensure_save_folder():
    if not os.path.exists(SAVE_FOLDER):
        os.makedirs(SAVE_FOLDER)

def get_save_file_path(slot_number):
    return os.path.join(SAVE_FOLDER, f"save_slot_{slot_number}.json")

def load_game(slot_number, league_manager, calendar, settings, free_agents_list, rookie_class_list):
    ensure_save_folder()
    save_path = get_save_file_path(slot_number)

    if not os.path.exists(save_path):
        print("❌ No save file found in that slot.")
        return False

    try:
        with open(save_path, "r", encoding="utf-8") as f:
            save_data = json.load(f)

        # Load Teams
        loaded_teams = []
        for team_data in save_data.get("teams", []):
            team = Team.from_dict(team_data)
            # Player reconstruction happens separately
            loaded_teams.append(team)
        league_manager.teams = loaded_teams

        # Load Standings
        league_manager.standings = save_data.get("standings", {})

        # Load Free Agents
        free_agents_list.clear()
        for player_data in save_data.get("free_agents", []):
            player = Player.from_dict(player_data)
            free_agents_list.append(player)

        # Load Rookie Class
        rookie_class_list.clear()
        for rookie_data in save_data.get("rookie_class", []):
            player = Player.from_dict(rookie_data)
            rookie_class_list.append(player)

        # Load Calendar
        calendar_data = save_data.get("calendar", {})
        calendar.deserialize(calendar_data)

        # Load Settings
        settings_data = save_data.get("settings", {})
        settings.deserialize(settings_data)

        print(f"✅ Game loaded from Slot {slot_number}.")
        return True

    except Exception as e:
        print(f"❌ Failed to load save from Slot {slot_number}: {e}")
        return False
