# engine/core/save_game_manager.py

import os
import json
from datetime import datetime

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

SAVE_FOLDER = os.path.join(BASE_DIR, "..", "..", "saves")
GM_PROFILE_FOLDER = os.path.join(SAVE_FOLDER, "gm_profiles")
SAVE_SLOTS = 3

def ensure_gm_profile_folder():
    """Ensure the GM Profiles folder exists."""
    if not os.path.exists(GM_PROFILE_FOLDER):
        os.makedirs(GM_PROFILE_FOLDER)
        print(f"✅ Created GM profile folder at {GM_PROFILE_FOLDER}.")

def save_gm_profile(gm_profile):
    """Save a GM profile to disk."""
    ensure_gm_profile_folder()
    gm_name = gm_profile["gm_name"].replace(" ", "_")
    file_path = os.path.join(GM_PROFILE_FOLDER, f"{gm_name}.json")

    with open(file_path, "w", encoding="utf-8") as f:
        json.dump(gm_profile, f, indent=4)
    print(f"✅ GM Profile for {gm_profile['gm_name']} saved.")

def load_gm_profile(gm_name):
    """Load a GM profile by name."""
    ensure_gm_profile_folder()
    gm_file = os.path.join(GM_PROFILE_FOLDER, f"{gm_name}.json")
    if not os.path.exists(gm_file):
        print(f"⚠️ GM profile {gm_name} not found.")
        return None
    with open(gm_file, "r", encoding="utf-8") as f:
        return json.load(f)

def choose_gm_profile():
    """Let the user choose an existing GM profile."""
    ensure_gm_profile_folder()
    gm_files = [f for f in os.listdir(GM_PROFILE_FOLDER) if f.endswith(".json")]
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

def choose_save_slot():
    """Choose an empty save slot."""
    ensure_save_folder()
    print("\nChoose a Save Slot:")

    for slot in range(1, SAVE_SLOTS + 1):
        slot_path = os.path.join(SAVE_FOLDER, f"save_slot_{slot}.json")
        if os.path.exists(slot_path):
            print(f"[{slot}] (Occupied)")
        else:
            print(f"[{slot}] (Empty)")

    while True:
        try:
            choice = int(input("Enter slot number: "))
            if 1 <= choice <= SAVE_SLOTS:
                return choice
            else:
                print(f"Choose a number between 1 and {SAVE_SLOTS}.")
        except ValueError:
            print("Enter a valid number.")

def choose_load_slot():
    """Choose an existing save slot to load."""
    ensure_save_folder()
    available_slots = []
    print("\nAvailable Save Slots:")

    for slot in range(1, SAVE_SLOTS + 1):
        slot_path = os.path.join(SAVE_FOLDER, f"save_slot_{slot}.json")
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

def ensure_save_folder():
    """Ensure the main saves folder exists."""
    if not os.path.exists(SAVE_FOLDER):
        os.makedirs(SAVE_FOLDER)

def save_game(game_world, save_slot):
    """Save the current game world into a slot."""
    ensure_save_folder()
    file_path = os.path.join(SAVE_FOLDER, f"save_slot_{save_slot}.json")

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

def load_game(save_slot):
    """Load a saved game world from a slot."""
    file_path = os.path.join(SAVE_FOLDER, f"save_slot_{save_slot}.json")
    if not os.path.exists(file_path):
        print("⚠️ Save file not found.")
        return None

    with open(file_path, "r", encoding="utf-8") as f:
        save_data = json.load(f)

    return save_data.get("game_world", None)
