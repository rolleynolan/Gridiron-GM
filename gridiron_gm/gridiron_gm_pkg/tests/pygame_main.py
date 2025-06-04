import pygame
import sys
import os
import json
from pathlib import Path
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager
from gridiron_gm.gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm.gridiron_gm_pkg.simulation.entities.league import load_league_from_file, LeagueManager
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.offseason_manager import OffseasonManager
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.team_data import load_teams_from_json
from datetime import timedelta, date
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player  # adjust import as needed
from gridiron_gm.gridiron_gm_pkg.simulation.utils.college_player_generator import generate_freshman_class
from gridiron_gm.gridiron_gm_pkg.simulation.utils.draft_class_generator import generate_draft_class
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager
from gridiron_gm.gridiron_gm_pkg.simulation.systems.roster.transacion_manager import TransactionManager

save_name = "test_league"
calendar = Calendar()
BASE_DIR = Path(__file__).resolve().parents[1]

# --- Load or create league ---
league_path = os.path.join("data", "saves", save_name, "league.json")
try:
    league = load_league_from_file(save_name)
    print(f"Loaded league from {league_path}")
except FileNotFoundError:
    print(f"No league save found at {league_path}, creating new league from teams.json...")
    teams = load_teams_from_json(BASE_DIR / "config" / "teams.json")
    positions = (
        ["QB"] * 3 +
        ["RB"] * 4 +
        ["WR"] * 6 +
        ["TE"] * 3 +
        ["LT"] * 3 +
        ["LG"] * 3 +
        ["C"] * 3 +
        ["RG"] * 3 +
        ["RT"] * 3 +
        ["DL"] * 8 +
        ["LB"] * 7 +
        ["CB"] * 6 +
        ["S"] * 4 +
        ["K"] * 1 +
        ["P"] * 1 +
        ["FB"] * 1  # Optional: add/remove as needed
    )
    # Fill up to 53 if needed
    while len(positions) < 53:
        positions.append("ST")  # Special teams or extra

    # Fill rosters for all teams
    for team in teams:
        if not hasattr(team, "roster") or not getattr(team, "roster", None):
            team.roster = [
                Player(
                    name=f"Dummy Player {i+1}",
                    position=positions[i],
                    age=22,
                    dob=date(2002, 1, 1),
                    college="Dummy U",
                    birth_location="Nowhere, USA",
                    jersey_number=i+1,
                    overall=60,
                    potential=70
                ) for i in range(53)
            ]
    league = LeagueManager()
    league.teams = teams
    # Generate depth chart for all teams
    for team in league.teams:
        if hasattr(team, "generate_depth_chart"):
            team.generate_depth_chart()
    # Debug print
    for team in league.teams:
        print(f"{getattr(team, 'name', getattr(team, 'team_name', 'UNKNOWN'))} roster size: {len(getattr(team, 'roster', []))}")
    os.makedirs(os.path.dirname(league_path), exist_ok=True)
    with open(league_path, "w") as f:
        import json
        if hasattr(league, "to_dict"):
            json.dump(league.to_dict(), f, indent=2)
        else:
            json.dump(league, f, indent=2)
    print(f"New league created and saved to {league_path}")

# --- Ensure valid schedule exists ---
schedule_path = os.path.join("data", "saves", save_name, "schedule_by_week.json")
regenerate_schedule = False

if os.path.exists(schedule_path):
    try:
        with open(schedule_path, "r") as f:
            schedule_by_week = json.load(f)
        # Gather all valid team IDs from the league
        valid_team_ids = {getattr(team, "id", None) for team in league.teams}
        # Check all games for valid team IDs
        for week_games in schedule_by_week.values():
            for game in week_games:
                if (game.get("home_id") not in valid_team_ids) or (game.get("away_id") not in valid_team_ids):
                    print("Schedule contains invalid team IDs. Regenerating schedule.")
                    regenerate_schedule = True
                    break
            if regenerate_schedule:
                break
    except Exception as e:
        print(f"Error loading schedule: {e}. Regenerating schedule.")
        regenerate_schedule = True
else:
    regenerate_schedule = True

if regenerate_schedule:
    print("Generating new schedule...")
    from gridiron_gm.gridiron_gm_pkg.simulation.utils.generate_schedule import generate_schedule
    generate_schedule(league.teams, save_name)
    print("Schedule generated.")

# --- Pygame setup ---
pygame.init()
# Change to a resizable window
screen = pygame.display.set_mode((900, 700), pygame.RESIZABLE)
pygame.display.set_caption("Gridiron GM - Daily Simulation")
font = pygame.font.SysFont("Arial", 28)
small_font = pygame.font.SysFont("Arial", 20)

season_manager = SeasonManager(calendar, league, save_name=save_name)
offseason_manager = OffseasonManager(league)

user_team = league.teams[0] if league.teams else None
user_team_abbr = getattr(user_team, "abbreviation", None)

def get_weekly_games():
    week = str(calendar.current_week)
    return season_manager.schedule_by_week.get(week, [])

def get_weekly_results():
    week = str(calendar.current_week)
    return season_manager.results_by_week.get(week, [])

def get_games_played_today():
    week = str(calendar.current_week)
    today_str = calendar.DAYS_OF_WEEK[calendar.current_day_index].capitalize()
    games_today = []
    # Find scheduled games for today
    for game in get_weekly_games():
        if game.get("day", "").capitalize() == today_str:
            games_today.append(game)
    # Find results for today
    results_today = []
    for result in get_weekly_results():
        if result.get("day", "").capitalize() == today_str:
            results_today.append(result)
    # Match results to scheduled games
    return results_today

def draw_daily_view():
    screen.fill((20, 20, 30))
    week = calendar.current_week
    year = calendar.current_year
    phase = calendar.season_phase
    day_name = calendar.current_day
    # Title
    title = font.render(f"Year {year} - Week {week} ({phase}) - {day_name}", True, (255, 255, 0))
    screen.blit(title, (40, 30))
    y = 80

    # --- Offseason phase display ---
    if hasattr(season_manager, "offseason_manager") and season_manager.offseason_manager:
        offseason_mgr = season_manager.offseason_manager
        is_complete = getattr(offseason_mgr, "is_complete", False)
        if callable(is_complete):
            is_complete = offseason_mgr.is_complete()
        if not is_complete:
            phase_str = getattr(offseason_mgr, "current_phase", "Unknown")
            screen.blit(small_font.render(f"Offseason Phase: {phase_str}", True, (255, 200, 0)), (40, y))
            y += 30

    # User team info
    if user_team:
        team_name = getattr(user_team, "name", user_team_abbr)
        abbr = user_team_abbr
        team_label = font.render(f"Your Team: {team_name} ({abbr})", True, (180, 220, 255))
        screen.blit(team_label, (40, y))
        y += 50
    else:
        y += 50

    # Games played today
    results_today = get_games_played_today()
    if results_today:
        screen.blit(small_font.render("Today's Games & Results:", True, (180, 220, 255)), (40, y))
        y += 30
        for result in results_today:
            home = result["home"]
            away = result["away"]
            home_score = result["home_score"]
            away_score = result["away_score"]
            color = (255, 255, 255)
            if user_team_abbr in (home, away):
                color = (255, 220, 100)
            game_str = f"{home} {home_score} - {away} {away_score}"
            screen.blit(small_font.render(game_str, True, color), (60, y))
            y += 24
    else:
        screen.blit(font.render("No games played today.", True, (200, 200, 200)), (40, y))
        y += 30
    # Standings
    y += 16
    standings = season_manager.standings_manager.get_sorted_standings_by_conference()
    screen.blit(small_font.render("Standings:", True, (180, 220, 255)), (40, y))
    y += 30
    for conf in ["Nova", "Atlas"]:
        screen.blit(small_font.render(f"{conf} Conference", True, (180, 220, 255)), (40, y))
        y += 24
        for team in standings[conf]:
            rec = f"{team['abbr']}: {team['W']}W-{team['L']}L-{team['T']}T"
            color = (255, 255, 255) if team['abbr'] != user_team_abbr else (255, 220, 100)
            screen.blit(small_font.render(rec, True, color), (60, y))
            y += 20
        y += 10
    # Instructions
    y += 10
    # Updated instructions to include simulate week
    screen.blit(small_font.render("[N]ext Day  [W]eek  [S]tandings  [Q]uit", True, (180, 180, 180)), (40, y))
    pygame.display.flip()

def draw_standings():
    screen.fill((10, 10, 20))
    standings = season_manager.standings_manager.get_sorted_standings_by_conference()
    y = 40
    screen.blit(font.render("Standings", True, (255, 255, 0)), (40, y))
    y += 40
    for conf in ["Nova", "Atlas"]:
        screen.blit(small_font.render(f"{conf} Conference", True, (180, 220, 255)), (40, y))
        y += 30
        for team in standings[conf]:
            rec = f"{team['abbr']}: {team['W']}W-{team['L']}L-{team['T']}T  PF:{team['PF']}  PA:{team['PA']}"
            color = (255, 255, 255) if team['abbr'] != user_team_abbr else (255, 220, 100)
            screen.blit(small_font.render(rec, True, color), (60, y))
            y += 24
        y += 16
    screen.blit(small_font.render("[B]ack", True, (180, 180, 180)), (40, y + 10))
    pygame.display.flip()

def simulate_day():
    # Offseason integration
    if getattr(season_manager, "offseason_manager", None):
        season_manager.offseason_step(calendar)
    else:
        # --- Playoff integration ---
        # If playoffs are ready but not yet simulated, run them before proceeding to offseason
        if hasattr(season_manager, "playoff_bracket") and season_manager.playoff_bracket and not season_manager.playoffs_generated:
            print("=== Simulating Playoffs ===")
            for round_name, matchups in season_manager.playoff_bracket.items():
                print(f"\n{round_name}:")
                for matchup in matchups:
                    print(f"  {matchup['home']} vs {matchup['away']}")
            season_manager.simulate_playoffs()
            # After simulation, print results
            playoff_results = getattr(season_manager, "playoff_results", {})
            for round_name, games in playoff_results.items():
                print(f"\n{round_name} Results:")
                for game in games:
                    print(f"  {game['home']} {game['home_score']} - {game['away']} {game['away_score']}")
            print("=== Playoffs Complete ===")
            if hasattr(season_manager, "crown_champion"):
                season_manager.crown_champion()
            return  # Skip normal day sim, as playoffs just ran

        season_manager.start_day()
        # (UI: show schedule, let user interact, etc.)
        season_manager.end_day()

def simulate_week():
    """Simulate days until the start of the next week."""
    current_week = calendar.current_week
    # Loop until the week changes (or sim ends)
    while calendar.current_week == current_week and not getattr(season_manager, "offseason_manager", None):
        simulate_day()

def run_offseason_updates(league):
    """
    Runs all college player and draft logic for the offseason.
    """
    # 1. Promote all college players
    for player in getattr(league, "college_db", []):
        player.year_in_college = getattr(player, "year_in_college", 1) + 1

    # 2. Remove graduates
    league.college_db = [p for p in league.college_db if getattr(p, "year_in_college", 1) <= 4]

    # 3. Generate and add new freshman class
    new_freshmen = generate_freshman_class()
    league.college_db.extend(new_freshmen)

    # 4. Generate this year's draft class
    draft_class = generate_draft_class(league.college_db)
    league.draft_prospects = draft_class

    # 5. Run the draft
    transaction_manager = TransactionManager(league)
    draft_manager = DraftManager(league, transaction_manager)
    draft_manager.run_draft()

    # 6. Move undrafted players to free agents (handled in draft_manager.run_draft())

# --- Main loop ---
viewing_standings = False
running = True
offseason_ran = False  # Track if we've run the offseason logic

while running:
    # Handle window resize events
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            running = False
        elif event.type == pygame.VIDEORESIZE:
            screen = pygame.display.set_mode((event.w, event.h), pygame.RESIZABLE)
        elif event.type == pygame.KEYDOWN:
            if viewing_standings:
                if event.key == pygame.K_b:
                    viewing_standings = False
            else:
                if event.key == pygame.K_q:
                    running = False
                elif event.key == pygame.K_n:
                    simulate_day()
                elif event.key == pygame.K_w:
                    simulate_week()
                elif event.key == pygame.K_s:
                    viewing_standings = True

    # --- Offseason integration ---
    if getattr(season_manager, "offseason_manager", None):
        # Only run offseason logic once per offseason
        if not offseason_ran:
            run_offseason_updates(league)
            offseason_ran = True
        screen.fill((20, 20, 30))
        screen.blit(font.render("Offseason in progress...", True, (255, 255, 0)), (40, 40))
        pygame.display.flip()
    else:
        offseason_ran = False  # Reset for next offseason
        if viewing_standings:
            draw_standings()
        else:
            draw_daily_view()

pygame.quit()
sys.exit()
