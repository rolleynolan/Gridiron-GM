from gridiron_gm_pkg.players.player import get_rookie_view


def scouting_menu(rookie_class, scouts, scouting_system, team=None):
    favorites_only = False
    current_filter = None
    current_sort = "name"

    # Initialize favorites if missing
    for player in rookie_class:
        if not hasattr(player, 'favorite'):
            player.favorite = False

    while True:
        print("\n=== Scouting Menu ===")
        print("1. View Scouted Players")
        print("2. Assign Scout to Player")
        print("3. Toggle Favorites Only Mode")
        print("4. Set Position Filter")
        print("5. Clear Filters")
        print("6. Set Sort Option")
        print("7. Back to Main Menu")
        choice = input("Select an option (1-7): ").strip()

        if choice == "1":
            view_scouted_players(rookie_class, favorites_only, current_filter, current_sort)
        elif choice == "2":
            assign_scout_to_player(rookie_class, scouts, scouting_system)
        elif choice == "3":
            favorites_only = not favorites_only
            print(f"\nFavorites Only Mode {'Enabled' if favorites_only else 'Disabled'}.")
        elif choice == "4":
            current_filter = input("Enter Position to filter by (e.g., QB, WR, OL): ").strip().upper()
        elif choice == "5":
            current_filter = None
            print("\nFilters cleared.")
        elif choice == "6":
            current_sort = choose_sort_option()
        elif choice == "7":
            break
        else:
            print("Invalid choice. Try again.")

def view_scouted_players(rookie_class, favorites_only, position_filter, sort_key):
    players = rookie_class

    # Apply Favorites Filter
    if favorites_only:
        players = [p for p in players if p.favorite]

    # Apply Position Filter
    if position_filter:
        players = [p for p in players if p.position == position_filter]

    # Apply Sorting
    if sort_key == "name":
        players.sort(key=lambda p: p.name)
    elif sort_key == "position":
        players.sort(key=lambda p: p.position)
    elif sort_key == "progress":
        players.sort(key=lambda p: p.scouting_progress, reverse=True)
    elif sort_key == "projected_overall":
        players.sort(key=lambda p: (p.projected_overall if p.scouted else 0), reverse=True)
    elif sort_key == "projected_potential":
        players.sort(key=lambda p: (p.projected_potential if p.scouted else 0), reverse=True)

    print("\n--- Rookie Scouting Report ---")
    print(f"{'Fav':<3} {'Name':<25} {'Pos':<5} {'College':<15} {'Progress':<10} {'Proj OVR':<10} {'Proj POT':<10}")
    print("-" * 85)

    for idx, player in enumerate(players, start=1):
        progress = f"{player.scouting_progress}%"
        if player.scouted:
            view = get_rookie_view(player, getattr(player, "assigned_scout", None))
            projected_ovr = f"{view.get('overall')}"
            projected_pot = f"{view.get('potential')}"
        else:
            projected_ovr = f"{player.projected_overall}" if player.scouted else "???"
            projected_pot = f"{player.projected_potential}" if player.scouted else "???"
        fav_mark = "*" if player.favorite else " "

        print(f"{fav_mark:<3} {player.name:<25} {player.position:<5} {player.college:<15} {progress:<10} {projected_ovr:<10} {projected_pot:<10}")

    # Option to favorite players
    if players:
        favorite_choice = input("\nWould you like to (Un)Favorite a player? (Y/N): ").strip().lower()
        if favorite_choice == "y":
            try:
                player_idx = int(input(f"Enter player number (1-{len(players)}): ").strip()) - 1
                if 0 <= player_idx < len(players):
                    players[player_idx].favorite = not players[player_idx].favorite
                    print(f"{'Favorited' if players[player_idx].favorite else 'Unfavorited'} {players[player_idx].name}.")
                else:
                    print("Invalid player number.")
            except ValueError:
                print("Invalid input. Returning to menu.")

def assign_scout_to_player(rookie_class, scouts, scouting_system):
    unscouted = [p for p in rookie_class if not p.scouted]
    if not unscouted:
        print("\nNo unscouted players available.")
        return

    print("\nSelect a player to scout:")
    for idx, player in enumerate(unscouted, start=1):
        print(f"{idx}. {player.name} ({player.position}) from {player.college}")

    try:
        player_choice = int(input("Enter player number: ")) - 1
        player = unscouted[player_choice]
    except (ValueError, IndexError):
        print("Invalid player choice.")
        return

    print("\nAvailable Scouts:")
    for idx, scout in enumerate(scouts, start=1):
        print(f"{idx}. {scout.name} ({scout.role}) - Acc {scout.accuracy}, Speed {scout.speed}")

    try:
        scout_choice = int(input("Enter scout number: ")) - 1
        scout = scouts[scout_choice]
    except (ValueError, IndexError):
        print("Invalid scout choice.")
        return

    scouting_system.assign_task(scout, "player", player)
    setattr(player, "assigned_scout", scout)
    print(f"\nAssigned {scout.name} to scout {player.name}.")

def choose_sort_option():
    print("\n--- Sorting Options ---")
    print("1. Name (A-Z)")
    print("2. Position (A-Z)")
    print("3. Scouting Progress (High to Low)")
    print("4. Projected Overall (High to Low)")
    print("5. Projected Potential (High to Low)")
    choice = input("Select a sort option (1-5): ").strip()

    if choice == "1":
        return "name"
    elif choice == "2":
        return "position"
    elif choice == "3":
        return "progress"
    elif choice == "4":
        return "projected_overall"
    elif choice == "5":
        return "projected_potential"
    else:
        print("Invalid choice. Defaulting to Name.")
        return "name"
