import random
from gridiron_gm_pkg.players.player import get_rookie_view

def draft_screen(game_world, user_team=None):
    current_phase = game_world["calendar"]["season_phase"]

    print("\n=== Rookie Draft Hub ===")
    print(f"Current Phase: {current_phase}")

    if current_phase != "Draft":
        print("\nThe Rookie Draft is not currently available.")
        return

    while True:
        print("\n1. Start Draft")
        print("2. View Top Prospects")
        print("3. Exit Draft Menu")

        choice = input("Select an option (1-3): ").strip()

        if choice == "1":
            start_draft(game_world, user_team)
            break
        elif choice == "2":
            view_top_prospects(game_world, user_team)
        elif choice == "3":
            break
        else:
            print("Invalid choice. Try again.")

def start_draft(game_world, user_team=None):
    rookie_class = game_world.get("rookie_class", [])
    teams = game_world.get("teams", [])
    draft_order = list(teams)
    drafted_players = []

    rounds = 7

    print("\n=== Starting the Rookie Draft ===")

    # Initialize drafted status
    for player in rookie_class:
        player.drafted = False

    # Assign GM personalities
    gm_personalities = {}
    for team in teams:
        gm_personalities[team.name] = random.choice(["Aggressive", "Balanced", "Cautious"])

    for current_round in range(1, rounds + 1):
        print(f"\n--- Round {current_round} ---")
        for idx in range(len(draft_order)):
            team = draft_order[idx]

            available_players = [p for p in rookie_class if not p.drafted]
            if not available_players:
                print("\nNo rookies left to draft!")
                return

            if not getattr(team, "user_controlled", False):
                maybe_trade_up(team, idx, draft_order, available_players, gm_personalities, current_round)

            if getattr(team, "user_controlled", False):
                pick_player_user(team, available_players, drafted_players, team)
            else:
                pick_player_cpu(team, available_players, drafted_players)

    print("\n=== Draft Complete ===")
    game_world["calendar"]["season_phase"] = "Post-Draft"
    print("\nSeason Phase has advanced to Post-Draft.")

def view_top_prospects(game_world, team=None):
    rookie_class = game_world.get("rookie_class", [])
    print("\n--- Top Prospects ---")
    for idx, player in enumerate(
        sorted(rookie_class, key=lambda p: p.projected_overall if p.scouted else p.overall, reverse=True)[:10],
        start=1
    ):
        if player.scouted:
            view = get_rookie_view(player, getattr(player, "assigned_scout", None))
            rating = view.get("overall")
        else:
            rating = player.projected_overall if player.scouted else "??"
        print(f"{idx}. {player.name} ({player.position}) - {player.college} - Projected: {rating} OVR")

def maybe_trade_up(team, current_idx, draft_order, available_players, gm_personalities, current_round):
    if current_idx == 0:
        return  # No one to trade with if you're picking first

    personality = gm_personalities.get(team.name, "Balanced")

    top_targets = sorted(available_players, key=lambda p: p.projected_overall if p.scouted else random.randint(50, 70), reverse=True)[:5]
    favorite = top_targets[0]

    risk_threshold = {
        "Aggressive": 0.8,
        "Balanced": 0.6,
        "Cautious": 0.4
    }.get(personality, 0.6)

    risk_of_losing = random.random()

    if risk_of_losing < risk_threshold:
        swap_idx = current_idx - 1
        team_ahead = draft_order[swap_idx]

        willing_to_trade = random.random() < 0.5

        if willing_to_trade:
            # Calculate pick labels
            pick_current = f"Round {current_round}, Pick {current_idx + 1}"
            pick_swap = f"Round {current_round}, Pick {swap_idx + 1}"

            # 50% chance to throw in extra pick
            extra_pick = None
            if random.random() < 0.5:
                extra_round = random.choice([3, 4, 5])
                extra_pick = f"Round {extra_round}, Pick {random.randint(1, 32)}"

            # Swap picks
            draft_order[swap_idx], draft_order[current_idx] = draft_order[current_idx], draft_order[swap_idx]

            print(f"\n** TRADE ALERT! **")
            if extra_pick:
                print(f"{team.city} {team.name} traded {pick_current} and {extra_pick} to {team_ahead.city} {team_ahead.name} for {pick_swap}.")
            else:
                print(f"{team.city} {team.name} traded {pick_current} to {team_ahead.city} {team_ahead.name} for {pick_swap}.")


def pick_player_user(team, available_players, drafted_players, user_team=None):
    print(f"\n{team.city} {team.name} is on the clock!")
    print("\nTop Available Prospects:")
    for idx, player in enumerate(available_players[:10], start=1):
        if player.scouted:
            view = get_rookie_view(player, getattr(player, "assigned_scout", None))
            rating = view.get("overall")
        else:
            rating = "??"
        print(f"{idx}. {player.name} ({player.position}) from {player.college} - {rating} OVR")

    try:
        choice = int(input("\nSelect a player number to draft: ").strip()) - 1
        if 0 <= choice < len(available_players[:10]):
            player = available_players[choice]
        else:
            print("Invalid choice. Auto-picking best available.")
            player = available_players[0]
    except (ValueError, IndexError):
        print("Invalid input. Auto-picking best available.")
        player = available_players[0]

    player.drafted = True
    drafted_players.append({"team": f"{team.city} {team.name}", "player": player})
    team.roster.append(player)

    print(f"\n{team.city} {team.name} selected {player.name} ({player.position}) from {player.college}!")

def pick_player_cpu(team, available_players, drafted_players):
    scored_players = []
    for player in available_players:
        score = player.projected_overall if player.scouted else random.randint(60, 75)
        scored_players.append((score, player))

    scored_players.sort(key=lambda x: x[0], reverse=True)
    player = scored_players[0][1]

    player.drafted = True
    drafted_players.append({"team": f"{team.city} {team.name}", "player": player})
    team.roster.append(player)

    print(f"{team.city} {team.name} selected {player.name} ({player.position}) from {player.college}.")
