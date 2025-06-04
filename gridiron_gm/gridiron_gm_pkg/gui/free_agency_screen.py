from gridiron_gm.gridiron_gm_pkg.engine.free_agency.free_agency_manager import FreeAgencyManager
from gridiron_gm.gridiron_gm_pkg.engine.free_agency.contract_offer import ContractOffer

def free_agency_screen(game_world, user_team):
    fa_manager = FreeAgencyManager(game_world)

    while True:
        print("\n=== FREE AGENCY ===")
        free_agents = fa_manager.list_top_free_agents(limit=10)

        if not free_agents:
            print("\nNo free agents available.")
            input("\nPress Enter to return to the main menu...")
            break

        print(f"\n{'No.':<5}{'Name':<25}{'POS':<5}{'College':<20}{'OVR':<5}{'Offers':<10}")
        print("-" * 75)

        for idx, player in enumerate(free_agents, start=1):
            offer_count = len(player.free_agent_profile.offers_received)
            print(f"{idx:<5}{player.name:<25}{player.position:<5}{player.college:<20}{player.overall:<5}{offer_count:<10}")

        print("\nOptions:")
        print("1. Make Contract Offer")
        print("2. Withdraw Contract Offer")
        print("0. Return to Main Menu")

        choice = input("\nSelect an option: ").strip()

        if choice == "0":
            break
        elif choice == "1":
            offer_to_free_agent(fa_manager, user_team, free_agents, game_world)
        elif choice == "2":
            withdraw_offer_from_free_agent(fa_manager, user_team, free_agents)
        else:
            print("Invalid choice. Please enter a valid option.")

def offer_to_free_agent(fa_manager, user_team, free_agents, game_world):
    try:
        selection = input("\nEnter the number of the player to offer a contract to: ").strip()
        selection_idx = int(selection) - 1

        if selection_idx < 0 or selection_idx >= len(free_agents):
            print("Invalid player selection.")
            return

        player = free_agents[selection_idx]

        print(f"\nOffering contract to {player.name} ({player.position}) - OVR {player.overall}")

        salary = float(input("Enter salary per year (in millions): ").strip())
        years = int(input("Enter contract length (years): ").strip())

        contract_offer = ContractOffer(
            total_value=salary,
            years=years
        )

        player.free_agent_profile.receive_offer(user_team, contract_offer)
        print(f"\n✅ Offer submitted to {player.name} for ${salary}M/year over {years} years.")

        game_world['free_agents'] = fa_manager.free_agents

    except ValueError:
        print("Invalid input. Contract canceled.")

def withdraw_offer_from_free_agent(fa_manager, user_team, free_agents):
    offers = []

    # Build a list of players who received offers from the user
    for player in free_agents:
        for (team, _) in player.free_agent_profile.offers_received:
            if team == user_team:
                offers.append(player)
                break

    if not offers:
        print("\nYou have no active offers to withdraw.")
        return

    print("\n=== Your Active Offers ===")
    for idx, player in enumerate(offers, start=1):
        print(f"{idx}. {player.name} ({player.position}) - OVR {player.overall}")

    try:
        selection = input("\nEnter the number of the player to withdraw the offer from: ").strip()
        selection_idx = int(selection) - 1

        if selection_idx < 0 or selection_idx >= len(offers):
            print("Invalid selection.")
            return

        selected_player = offers[selection_idx]
        selected_player.free_agent_profile.offers_received = [
            (team, offer) for (team, offer) in selected_player.free_agent_profile.offers_received if team != user_team
        ]

        print(f"\n✅ Offer withdrawn from {selected_player.name}.")

    except ValueError:
        print("Invalid input. Withdrawal canceled.")
