from gridiron_gm.gridiron_gm_pkg.engine.core.save_game_manager import save_game, save_gm_profile, load_gm_profile
from gridiron_gm.gridiron_gm_pkg.engine.core.load_game_manager import load_saved_game
from gridiron_gm.gridiron_gm_pkg.engine.core.new_game_manager import start_new_game
from gridiron_gm.gridiron_gm_pkg.gui.gm_creator import create_gm
from gridiron_gm.gridiron_gm_pkg.gui.in_game_menu import in_game_menu
from gridiron_gm.gridiron_gm_pkg.engine.draft.rookie_loader import rebuild_rookie_class  # ✅ ADD THIS!

def main_menu():
    game_world = None

    while True:
        print("\n--- GRIDIRON GM ---")
        print("1. Start New Game")
        print("2. Load Game")
        print("3. Settings (Coming Soon)")
        print("4. Exit")

        choice = input("Select an option (1-4): ").strip()

        if choice == "1":
            game_world = start_new_game_screen()
        elif choice == "2":
            game_world = load_saved_game_screen()
        elif choice == "3":
            print("\n[Settings Menu coming soon!]")
        elif choice == "4":
            exit_game()
        else:
            print("Invalid choice. Please try again.")

def start_new_game_screen():
    game_world = start_new_game()

    # ✅ REBUILD ROOKIE CLASS
    if "rookie_class" in game_world:
        game_world["rookie_class"] = rebuild_rookie_class(game_world["rookie_class"])

    try:
        existing_gm = load_gm_profile()
    except FileNotFoundError:
        existing_gm = None

    if existing_gm:
        print("\nA saved GM profile was found.")
        use_existing = input("Would you like to use the existing GM profile? (Y/N): ").strip().lower()
        if use_existing == "y":
            gm_profile = existing_gm
            print(f"\nLoaded GM: {gm_profile.get('name', 'Unknown GM')}")
        else:
            gm_profile = create_gm()
    else:
        gm_profile = create_gm()

    in_game_menu(game_world, gm_profile)
    return game_world

def load_saved_game_screen():
    game_world = load_saved_game()

    # ✅ REBUILD ROOKIE CLASS
    if "rookie_class" in game_world:
        game_world["rookie_class"] = rebuild_rookie_class(game_world["rookie_class"])

    if game_world:
        try:
            gm_profile = load_gm_profile()
        except FileNotFoundError:
            gm_profile = {"name": "Loaded GM"}

        in_game_menu(game_world, gm_profile)
    return game_world

def exit_game():
    print("\nExiting Gridiron GM. Goodbye!")
    exit()
