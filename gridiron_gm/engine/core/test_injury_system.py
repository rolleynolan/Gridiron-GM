import datetime
# Import necessary classes
from gridiron_gm.engine.core.team_manager import Team
from gridiron_gm.engine.core.player import Player
from gridiron_gm.health.injury_manager import InjuryEngine, Injury

def test_injury_recovery_and_ir():
    # Initialize Team and InjuryEngine
    team = Team("Gridiron", "New York", "NYG")

    # Create players
    player1 = Player(name="John Doe", position="QB", age=25, dob=datetime.datetime(1998, 5, 20), 
                     college="University of Example", birth_location="New York", jersey_number=10, overall=80)
    player2 = Player(name="Sam Smith", position="RB", age=28, dob=datetime.datetime(1995, 6, 15), 
                     college="Example State", birth_location="Los Angeles", jersey_number=22, overall=85)

    # Add players to team
    team.add_player(player1)
    team.add_player(player2)

    # Create InjuryEngine instance
    injury_engine = InjuryEngine()

    # Create Injury objects and add them to players
    injury1 = Injury("ACL Tear", 10, "Severe")  # 10 weeks out
    injury2 = Injury("Fractured Tibia", 8, "Severe")  # 8 weeks out

    player1.add_injury(injury1)
    player2.add_injury(injury2)

    # Print player injury info before recovery
    print(f"Player1 (before injury): {player1.name} - Injured: {player1.is_injured}, Weeks Out: {player1.weeks_out}")
    print(f"Player2 (before injury): {player2.name} - Injured: {player2.is_injured}, Weeks Out: {player2.weeks_out}")

    # Recover players weekly and check if the is_injured flag updates correctly
    for week in range(3):
        print(f"\nWeek {week + 1}:")
        injury_engine.recover_weekly(team)  # Recover weekly for all players

        # Print player injury info after recovery
        print(f"Player1 (after week {week + 1}): {player1.name} - Injured: {player1.is_injured}, Weeks Out: {player1.weeks_out}")
        print(f"Player2 (after week {week + 1}): {player2.name} - Injured: {player2.is_injured}, Weeks Out: {player2.weeks_out}")

