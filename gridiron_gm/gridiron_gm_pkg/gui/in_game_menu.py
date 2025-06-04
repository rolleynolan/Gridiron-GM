# ui/in_game_menu.py

from gridiron_gm.gridiron_gm_pkg.engine.phase_manager import advance_game_day, advance_game_week, phase_manager
from gridiron_gm.gridiron_gm_pkg.engine.core.game_simulator import simulate_week
from gridiron_gm.gridiron_gm_pkg.gui.roster_screen import roster_screen

def in_game_menu(game_world, gm_profile):
    save_slot = game_world.get("save_slot", None)

    while True:
        team_name = game_world.get("team_name", "Unknown Team")
        team_record = game_world.get("team_record", {"wins": 0, "losses": 0})
        wins = team_record.get("wins", 0)
        losses = team_record.get("losses", 0)

        print("\n--- GRIDIRON GM IN-GAME MENU ---")
        print(f"Team: {team_name} (Record: {wins}-{losses}) | GM: {gm_profile['gm_name']} | Year: {game_world['season_year']}")
        print(f"Phase: {game_world['current_phase']}\n")

        autosave = game_world.get("autosave_frequency", "weekly")
        print(f"Autosave: {autosave.capitalize()}\n")

        print("1. Advance Week")
        print("2. Advance Day")
        print("3. Enter Live Match (Coming Soon)")
        print("4. View Roster")
        print("5. Scouting Department")
        print("6. Free Agency Hub")
        print("7. Trade Center")
        print("8. Draft Room")
        print("9. Staff Management")
        print("10. View Standings")
        print("11. View GM Profile")
        print("12. Save Game")
        print("13. Exit to Main Menu")

        choice = input("\nEnter your choice (1-13): ").strip()

        if choice == "1":
            advance_game_week(game_world)
            phase_manager(game_world)
        elif choice == "2":
            advance_game_day(game_world)
            phase_manager(game_world)
        elif choice == "3":
            enter_live_match(game_world, gm_profile)
        elif choice == "4":
            user_team = game_world.get("user_team", {})
            roster_screen(game_world, user_team)
        elif choice == "5":
            scouting_menu(game_world)
        elif choice == "6":
            free_agency_menu(game_world)
        elif choice == "7":
            trade_center_menu(game_world)
        elif choice == "8":
            draft_room_menu(game_world)
        elif choice == "9":
            staff_management_menu(game_world)
        elif choice == "10":
            view_standings(game_world)
        elif choice == "11":
            view_gm_profile(gm_profile)
        elif choice == "12":
            print("💾 Save Game (Coming Soon)")  # Hook save_game() later
        elif choice == "13":
            confirm = input("Are you sure you want to exit to Main Menu? (Y/N): ").strip().lower()
            if confirm == "y":
                break
        else:
            print("Invalid choice, try again.")

# Placeholder Functions Below

def enter_live_match(game_world, gm_profile):
    print("\n🏈 Entering Live Match (Coming Soon)")

def view_roster(game_world):
    print("\n📋 Viewing Roster (Coming Soon)")

def scouting_menu(game_world):
    print("\n🔍 Scouting Department (Coming Soon)")

def free_agency_menu(game_world):
    print("\n📝 Free Agency Hub (Coming Soon)")

def trade_center_menu(game_world):
    print("\n🔄 Trade Center (Coming Soon)")

def draft_room_menu(game_world):
    print("\n🏈 Draft Room (Coming Soon)")

def staff_management_menu(game_world):
    print("\n👔 Staff Management (Coming Soon)")

def view_standings(game_world):
    print("\n📈 League Standings (Coming Soon)")

def view_gm_profile(gm_profile):
    print("\n--- GM PROFILE ---")
    print(f"Name: {gm_profile.get('gm_name', 'Unknown')}")
    print(f"DOB: {gm_profile.get('dob', 'Unknown')}")
    print(f"Nationality: {gm_profile.get('nationality', 'Unknown')}")
    appearance = gm_profile.get('appearance', {})
    print(f"Hair Color: {appearance.get('hair_color', 'Unknown')}")
    print(f"Skin Tone: {appearance.get('skin_tone', 'Unknown')}")
    print(f"Height: {appearance.get('height', 'Unknown')} inches")
