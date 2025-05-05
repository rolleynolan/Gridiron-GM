import random

def simulate_week(game_world):
    teams = game_world["teams"]
    season_phase = game_world["calendar"]["season_phase"]

    if not teams:
        print("\nNo teams available to simulate.")
        return

    print("\n--- Week Results ---")

    if season_phase == "Regular Season":
        simulate_regular_season_week(teams)
        check_end_of_regular_season(game_world)
    elif season_phase == "Playoffs":
        simulate_playoff_week(game_world)
        check_end_of_playoffs(game_world)
    else:
        print(f"\nNo games scheduled during {season_phase} phase.")

    print(f"\nGames completed. Current Date: {game_world['calendar']['current_date']}")

def simulate_regular_season_week(teams):
    random.shuffle(teams)

    for i in range(0, len(teams), 2):
        if i + 1 >= len(teams):
            print(f"{teams[i].city} {teams[i].name} has a bye week.")
            continue

        team1 = teams[i]
        team2 = teams[i+1]

        score1 = random.randint(10, 40)
        score2 = random.randint(10, 40)

        if score1 > score2:
            team1.record["W"] += 1
            team2.record["L"] += 1
            result = f"{team1.city} {team1.name} beat {team2.city} {team2.name} ({score1}-{score2})"
        elif score2 > score1:
            team2.record["W"] += 1
            team1.record["L"] += 1
            result = f"{team2.city} {team2.name} beat {team1.city} {team1.name} ({score2}-{score1})"
        else:
            team1.record["T"] += 1
            team2.record["T"] += 1
            result = f"{team1.city} {team1.name} tied with {team2.city} {team2.name} ({score1}-{score2})"

        print(result)

def simulate_playoff_week(game_world):
    teams = game_world["teams"]

    if len(teams) <= 1:
        if teams:
            champion = teams[0]
            print(f"\n🏆 {champion.city} {champion.name} wins the Super Bowl! 🏆")
        else:
            print("\nNo teams left. Playoffs error.")

        game_world["calendar"]["season_phase"] = "Offseason"
        print("\nSeason Phase has advanced to Offseason.")
        return

    playoff_teams = sorted(teams, key=lambda t: (t.record["W"], -t.record["L"]), reverse=True)

    winners = []

    for i in range(0, len(playoff_teams), 2):
        if i + 1 >= len(playoff_teams):
            winners.append(playoff_teams[i])
            continue

        team1 = playoff_teams[i]
        team2 = playoff_teams[i+1]

        score1 = random.randint(14, 35)
        score2 = random.randint(14, 35)

        if score1 > score2:
            winners.append(team1)
            result = f"{team1.city} {team1.name} beat {team2.city} {team2.name} ({score1}-{score2})"
        else:
            winners.append(team2)
            result = f"{team2.city} {team2.name} beat {team1.city} {team1.name} ({score2}-{score1})"

        print(result)

    game_world["teams"] = winners

def check_end_of_regular_season(game_world):
    current_date = game_world["calendar"]["current_date"]
    month = int(current_date.split("-")[1])
    day = int(current_date.split("-")[2])

    # Switch to Playoffs after January 1
    if month >= 1 and day >= 1:
        print("\nRegular Season Complete! Moving to Playoffs...")
        game_world["calendar"]["season_phase"] = "Playoffs"
        game_world["playoff_round"] = 1

def check_end_of_playoffs(game_world):
    if "playoff_round" not in game_world:
        game_world["playoff_round"] = 1
    else:
        game_world["playoff_round"] += 1

    if game_world["playoff_round"] > 3:
        print("\nPlayoffs Complete! Moving to Offseason...")
        game_world["calendar"]["season_phase"] = "Offseason"
