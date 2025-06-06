# ui/main_menu.py

import os
import json
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.save_system import (
    choose_save_slot, choose_load_slot, save_game, load_game,
    save_gm_profile, load_gm_profile, choose_gm_profile, ensure_gm_profile_folder
)
from gridiron_gm.gridiron_gm_pkg.gui.in_game_menu import in_game_menu
from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm.gridiron_gm_pkg.simulation.utils.roster_generator import RosterGenerator, PlayerGenerator  # ✅ NEW import

BASE_DIR = os.path.dirname(os.path.abspath(__file__))

TEAM_LIST = [
    "Chicago Cyclones", "Dallas Outlaws", "Miami Tritons", "New York Empire",
    "San Francisco Surge", "Tennessee Renegades", "Las Vegas Vipers", "Atlanta Flight",
    "Boston Redcoats", "Houston Stampede", "Los Angeles Paladins", "Philadelphia Liberty",
    "Phoenix Inferno", "Detroit Mechanics", "Seattle Eclipse", "Denver Bighorns",
    "Cleveland Vanguards", "Orlando Stingrays", "Minnesota Mammoths", "Baltimore Knights",
    "New Orleans Specters", "Indianapolis Racers", "Cincinnati Sabers", "Kansas City Kings",
    "Charlotte Stingers", "Tampa Bay Sharks", "Pittsburgh Ironmen", "Washington Generals",
    "Green Bay Lumberjacks", "Buffalo Blizzard", "San Diego Armada", "Portland Pioneers"
]

def main_menu():
    while True:
        print("\n--- GRIDIRON GM MAIN MENU ---")
        print("1. Start New Game")
        print("2. Load Saved Game")
        print("3. Exit")

        choice = input("Enter choice (1-3): ").strip()

        if choice == "1":
            start_new_game()
        elif choice == "2":
            load_existing_game()
        elif choice == "3":
            print("Exiting Gridiron GM. Goodbye!")
            break
        else:
            print("Invalid choice, try again.")

def start_new_game():
    print("\nStarting New Game...")

    ensure_gm_profile_folder()
    gm_profiles_path = os.path.join(BASE_DIR, "..", "saves", "gm_profiles")
    if not os.path.exists(gm_profiles_path):
        os.makedirs(gm_profiles_path)

    gm_files = [f for f in os.listdir(gm_profiles_path) if f.endswith(".json")]

    if gm_files:
        use_existing = input("Existing GM profiles found. Use one? (Y/N): ").strip().lower()
        if use_existing == "y":
            gm_name = choose_gm_profile()
            if gm_name:
                gm_profile = load_gm_profile(gm_name)
            else:
                print("No GM selected. Returning to menu.")
                return
        else:
            gm_profile = create_new_gm_profile()
    else:
        gm_profile = create_new_gm_profile()

    print("\nSelect Your Team:")
    for idx, team in enumerate(TEAM_LIST, 1):
        print(f"[{idx}] {team}")

    while True:
        try:
            team_choice = int(input("Enter team number (1-32): "))
            if 1 <= team_choice <= len(TEAM_LIST):
                team_entry = TEAM_LIST[team_choice - 1]
                team_name = team_entry["team_name"]
                team_city = team_entry["city"]

                break
            else:
                print("Invalid choice. Choose 1-32.")
        except ValueError:
            print("Enter a valid number.")

    # Create user team with roster
    user_team = Team(team_name, city=team_city)


    player_generator = PlayerGenerator()  # ✅ Create PlayerGenerator first
    roster_gen = RosterGenerator(player_generator)  # ✅ Pass into RosterGenerator
    generated_players = roster_gen.generate_team_roster()
    for player in generated_players:
        user_team.add_player(player)

    # Create game world
    game_world = {
        "gm_name": gm_profile["gm_name"],
        "season_year": 2025,
        "team_name": team_name,
        "teams": [],
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
        },
        "schedule": {},
        "calendar": {
            "current_date": "2025-08-01",
            "season_phase": "Preseason"
        },
        "current_phase": "Preseason Week 1",
        "autosave_frequency": "weekly",
        "team_record": {
            "wins": 0,
            "losses": 0
        },
        "user_team": user_team.to_dict()
    }

    save_slot = choose_save_slot()
    game_world["save_slot"] = save_slot
    save_game(game_world, save_slot)

    print(f"\n✅ New game started as GM {gm_profile['gm_name']} of the {team_name}!\n")

    # 🚀 Enter full GM Menu after starting new game
    in_game_menu(game_world, gm_profile)

def load_existing_game():
    print("\nLoading Existing Game...")
    slot = choose_load_slot()
    if slot is None:
        print("Returning to Main Menu.")
        return

    game_world = load_game(slot)
    if game_world:
        gm_name = game_world.get("gm_name")
        if gm_name:
            gm_profile = load_gm_profile(gm_name)
            if gm_profile:
                print(f"✅ Welcome back, GM {gm_profile['gm_name']}!\n")
            else:
                print("⚠️ GM profile not found. Continuing anyway.")
                gm_profile = {"gm_name": gm_name}  # dummy fallback
        else:
            print("⚠️ GM name not found in save data. Continuing anyway.")
            gm_profile = {"gm_name": "Unknown GM"}

        # 🚀 Enter full GM Menu after loading game
        in_game_menu(game_world, gm_profile)

def create_new_gm_profile():
    """Helper function to create and save a new GM profile with structured choices."""
    print("\n--- Create New GM Profile ---")

    gm_name = input("Enter your GM's full name: ").strip()
    dob = input("Enter your GM's date of birth (YYYY-MM-DD): ").strip()

    nationalities = ["USA", "Canada", "Mexico", "Germany", "France", "UK", "Japan", "Australia"]
    hair_colors = ["Black", "Brown", "Blonde", "Red", "Gray"]
    skin_tones = ["Light", "Medium", "Dark", "Olive"]

    print("\nChoose Nationality:")
    for idx, nation in enumerate(nationalities, 1):
        print(f"[{idx}] {nation}")
    while True:
        try:
            choice = int(input("Enter number: "))
            if 1 <= choice <= len(nationalities):
                nationality = nationalities[choice - 1]
                break
            else:
                print("Invalid choice.")
        except ValueError:
            print("Enter a valid number.")

    print("\nChoose Hair Color:")
    for idx, color in enumerate(hair_colors, 1):
        print(f"[{idx}] {color}")
    while True:
        try:
            choice = int(input("Enter number: "))
            if 1 <= choice <= len(hair_colors):
                hair_color = hair_colors[choice - 1]
                break
            else:
                print("Invalid choice.")
        except ValueError:
            print("Enter a valid number.")

    print("\nChoose Skin Tone:")
    for idx, tone in enumerate(skin_tones, 1):
        print(f"[{idx}] {tone}")
    while True:
        try:
            choice = int(input("Enter number: "))
            if 1 <= choice <= len(skin_tones):
                skin_tone = skin_tones[choice - 1]
                break
            else:
                print("Invalid choice.")
        except ValueError:
            print("Enter a valid number.")

    height = input("\nEnter height (inches): ").strip()
    weight = input("Enter weight (pounds): ").strip()

    new_gm_profile = {
        "gm_name": gm_name,
        "dob": dob,
        "nationality": nationality,
        "appearance": {
            "hair_color": hair_color,
            "skin_tone": skin_tone,
            "height": height,
            "weight": weight
        }
    }

    save_gm_profile(new_gm_profile)
    print(f"\n✅ New GM Profile created for {gm_name}!")
    return new_gm_profile
