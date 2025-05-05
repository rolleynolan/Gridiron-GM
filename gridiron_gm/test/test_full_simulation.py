from gridiron_gm.engine.core.team_manager import Team
from gridiron_gm.engine.core.calendar import Calendar
from gridiron_gm.engine.core.league_manager import LeagueManager
from gridiron_gm.engine.draft.player_generator import PlayerGenerator
from gridiron_gm.engine.draft.draft_manager import DraftManager
from gridiron_gm.engine.draft.rookie_contract_engine import RookieContractEngine
from gridiron_gm.engine.free_agency.free_agency_manager import FreeAgencyManager
from gridiron_gm.engine.training.training_schedule_manager import TrainingScheduleManager
from gridiron_gm.engine.training.training_effects import simulate_weekly_training_effects
from gridiron_gm.engine.health.injury_manager import InjuryEngine
from gridiron_gm.engine.core.depth_chart_manager import DepthChartManager
from gridiron_gm.engine.core.game_engine import GameEngine
from gridiron_gm.engine.scouting.scouting_system import ScoutingSystem
from gridiron_gm.engine.scouting.scout import Scout
from gridiron_gm.engine.core.game_day_effects import GameDayEffects  # ✅ NEW

import random

# Setup
calendar = Calendar()
calendar.season_phase = "REG_SEASON"
injury_engine = InjuryEngine()
game_engine = GameEngine()
game_day = GameDayEffects()  # ✅ NEW
scouting_system = ScoutingSystem()
free_agency = FreeAgencyManager()

# Teams
team_names = [("Falcons", "Atlanta"), ("Sharks", "San Diego"), ("Storm", "Seattle"), ("Bulls", "Chicago")]
teams = []
for name, city in team_names:
    team = Team(name, city)
    team.training_schedule = TrainingScheduleManager(team)
    team.depth_chart = DepthChartManager(team)
    teams.append(team)

# Players and Draft
generator = PlayerGenerator()
draft_class = generator.generate_class(20)
draft = DraftManager(teams, draft_class, rounds=2)
contract_engine = RookieContractEngine()
draft.run_draft(contract_engine)

# League
league = LeagueManager(teams)
league.generate_schedule(total_weeks=3)

# Scouts & Scouting
prospects = generator.generate_class(5)
scouts = [Scout(f"Scout {i}", role="College", region=None) for i in range(3)]
for i, scout in enumerate(scouts):
    scouting_system.assign_task(scout, "player", prospects[i % len(prospects)])

# Simulation Loop
for _ in range(3):
    print(f"\n== WEEK {calendar.get_week()} ({calendar.get_current_phase()}) ==")

    # Training
    for team in teams:
        simulate_weekly_training_effects(team)

    # Scouting
    scouting_system.run_weekly_scouting()

    # Pre-game effects and Injuries
    for team in teams:
        game_day.apply_pre_game_effects(team)
        for player in team.roster:
            injury_engine.check_for_injury(player, context="game")

    # Games
    league.simulate_week(calendar, game_engine)

    # Post-game effects and Recovery
    for team in teams:
        game_day.apply_post_game_effects(team, win=random.choice([True, False]))
        for player in team.roster:
            injury_engine.recover_weekly(player)

    # Advance week
    calendar.advance_week()

# Final Standings
league.print_standings()
