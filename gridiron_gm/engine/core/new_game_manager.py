import random
import datetime
import json
from gridiron_gm.engine.core.team_manager import Team
from gridiron_gm.engine.core.league_manager import LeagueManager
from gridiron_gm.engine.core.calendar import Calendar
from gridiron_gm.engine.core.save_game_manager import save_game
from gridiron_gm.engine.draft.player_generator import PlayerGenerator
from gridiron_gm.engine.roster.roster_generator import RosterGenerator

def start_new_game(selected_gm_slot):
    print("\n[Starting a Real New Game Setup...]")

    # Initialize Calendar
    calendar = Calendar(start_year=2025, start_month=9, start_day=1)

    # Create Teams
    team_data = [
        {"city": "Atlanta", "name": "Falcons"},
        {"city": "Chicago", "name": "Bulls"},
        {"city": "San Diego", "name": "Sharks"},
        {"city": "Seattle", "name": "Storm"}
    ]

    teams = []
    for entry in team_data:
        team = Team(name=entry["name"], city=entry["city"])
        teams.append(team)

    # Initialize League
    league = LeagueManager(teams)
    league.generate_schedule(total_weeks=3)

    # Initialize Player Generator and Roster Generator
    player_generator = PlayerGenerator()
    roster_generator = RosterGenerator(player_generator)

    # Fill Teams with Initial Rosters
    for team in teams:
        team.roster = roster_generator.generate_team_roster()
        team.cap_used = sum(player.contract["salary_per_year"] for player in team.roster)
        team.SALARY_CAP = 200
        team.MAX_ROSTER_SIZE = 53
        team.user_controlled = False

    # Generate Free Agent Pool
    free_agents = roster_generator.generate_free_agents(count=100)
    rookie_class = player_generator.generate_class(count=224)
    
    game_world = {
        "calendar": {
            "current_date": calendar.current_date.isoformat(),
            "season_phase": calendar.get_season_phase()
        },
        "teams": teams,
        "free_agents": free_agents,
        "rookie_class": rookie_class,
        "auto_save_frequency": "never",
        "gm_profile_slot": selected_gm_slot  # ✅ Attach the chosen GM Profile Slot here
    }

    print("\nNew game world created successfully!")

    # Return live game_world (not saved yet)
    return game_world
