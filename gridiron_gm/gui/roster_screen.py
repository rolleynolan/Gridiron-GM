def roster_screen(game_world, user_team):
    print("\n=== Team Roster ===")
    print(f"{user_team['city']} {user_team['name']} Roster\n")

    roster = user_team.get("roster", [])

    if not roster:
        print("No players on roster.")
        input("\nPress Enter to return...")
        return

    print(f"{'Name':<25}{'Pos':<5}{'OVR':<5}{'Age':<5}{'Contract'}")
    print("-" * 60)

    for player in roster:
        name = player.get("name", "Unknown")
        position = player.get("position", "--")
        overall = player.get("overall", "--")
        age = player.get("age", "--")
        contract = player.get("contract", {})
        salary = contract.get("salary_per_year", 0)
        years = contract.get("years", 1)

        print(f"{name:<25}{position:<5}{overall:<5}{age:<5}${salary:.2f}M x {years}y")

    input("\nPress Enter to return...")
