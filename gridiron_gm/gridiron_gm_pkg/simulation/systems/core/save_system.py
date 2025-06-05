
import os
import json
from datetime import datetime

class SaveSystem:
    SAVE_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "saves")
    GM_PROFILE_DIR = os.path.join(SAVE_DIR, "gm_profiles")
    SAVE_SLOTS = 3

    def __init__(self):
        os.makedirs(self.SAVE_DIR, exist_ok=True)
        os.makedirs(self.GM_PROFILE_DIR, exist_ok=True)

    # Save and Load Game
    def save_game(self, game_world, save_slot):
        file_path = os.path.join(self.SAVE_DIR, f"save_slot_{save_slot}.json")
        save_data = {
            "_metadata": {
                "gm_name": game_world.get("gm_name", "Unknown"),
                "team_name": game_world.get("team_name", "Unknown"),
                "season_year": game_world.get("season_year", 2025),
                "current_phase": game_world.get("current_phase", "Unknown Phase"),
                "save_time": datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            },
            "game_world": game_world
        }
        with open(file_path, "w", encoding="utf-8") as f:
            json.dump(save_data, f, indent=4)
        print(f"✅ Game saved to Slot {save_slot}.")

    def load_game(self, save_slot):
        file_path = os.path.join(self.SAVE_DIR, f"save_slot_{save_slot}.json")
        if not os.path.exists(file_path):
            print("⚠️ Save file not found.")
            return None
        with open(file_path, "r", encoding="utf-8") as f:
            save_data = json.load(f)
        return save_data.get("game_world", None)

    def start_new_game(self, template=None):
        if template:
            return template
        return {
            "year": 2025,
            "teams": [],
            "players": [],
            "league_records": {
                "players": {
                    "single_game": {},
                    "single_season": {},
                    "career": {}
                },
                "teams": {
                    "single_game": {},
                    "single_season": {},
                    "career": {}
                },
                "leaderboards": {
                    "current_season": {}
                }
            }
        }

    # Save and Load GM Profile
    def save_gm_profile(self, gm_profile):
        gm_name = gm_profile["gm_name"].replace(" ", "_")
        file_path = os.path.join(self.GM_PROFILE_DIR, f"{gm_name}.json")
        with open(file_path, "w", encoding="utf-8") as f:
            json.dump(gm_profile, f, indent=4)
        print(f"✅ GM Profile for {gm_profile['gm_name']} saved.")

    def load_gm_profile(self, gm_name):
        gm_file = os.path.join(self.GM_PROFILE_DIR, f"{gm_name}.json")
        if not os.path.exists(gm_file):
            print(f"⚠️ GM profile {gm_name} not found.")
            return None
        with open(gm_file, "r", encoding="utf-8") as f:
            return json.load(f)

    def choose_gm_profile(self):
        gm_files = [f for f in os.listdir(self.GM_PROFILE_DIR) if f.endswith(".json")]
        if not gm_files:
            print("No GM profiles available.")
            return None
        print("\nAvailable GM Profiles:")
        for idx, filename in enumerate(gm_files, 1):
            print(f"[{idx}] {filename.replace('.json', '')}")
        while True:
            try:
                choice = int(input("Choose a GM profile number: "))
                if 1 <= choice <= len(gm_files):
                    selected_file = gm_files[choice - 1]
                    return selected_file.replace(".json", "")
                else:
                    print("Invalid choice.")
            except ValueError:
                print("Enter a valid number.")

    # Save Slot Selection
    def choose_save_slot(self):
        print("\nChoose a Save Slot:")
        for slot in range(1, self.SAVE_SLOTS + 1):
            slot_path = os.path.join(self.SAVE_DIR, f"save_slot_{slot}.json")
            status = "(Occupied)" if os.path.exists(slot_path) else "(Empty)"
            print(f"[{slot}] {status}")
        while True:
            try:
                choice = int(input("Enter slot number: "))
                if 1 <= choice <= self.SAVE_SLOTS:
                    return choice
                else:
                    print(f"Choose a number between 1 and {self.SAVE_SLOTS}.")
            except ValueError:
                print("Enter a valid number.")

    def choose_load_slot(self):
        available_slots = []
        print("\nAvailable Save Slots:")
        for slot in range(1, self.SAVE_SLOTS + 1):
            slot_path = os.path.join(self.SAVE_DIR, f"save_slot_{slot}.json")
            if os.path.exists(slot_path):
                available_slots.append(slot)
                print(f"[{slot}] (Occupied)")
            else:
                print(f"[{slot}] (Empty)")
        if not available_slots:
            print("No saved games available.")
            return None
        while True:
            try:
                choice = int(input("Enter slot number to load: "))
                if choice in available_slots:
                    return choice
                else:
                    print("Choose an occupied slot.")
            except ValueError:
                print("Enter a valid number.")
