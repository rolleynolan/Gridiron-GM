from datetime import datetime, timedelta

def advance_game_day(game_world):
    """Advance the game world by one day."""
    if "calendar" in game_world and "current_date" in game_world["calendar"]:
        try:
            current_date = datetime.strptime(game_world['calendar']['current_date'], "%Y-%m-%d").date()
            new_date = current_date + timedelta(days=1)
            game_world['calendar']['current_date'] = new_date.strftime("%Y-%m-%d")
            print(f"📅 Advanced to {new_date} (Day Advanced)")
        except Exception as e:
            print(f"Error advancing day: {e}")
    else:
        print("⚠️ Calendar not initialized properly.")

def advance_game_week(game_world):
    """Advance the game world by one week."""
    if "calendar" in game_world and "current_date" in game_world["calendar"]:
        try:
            current_date = datetime.strptime(game_world['calendar']['current_date'], "%Y-%m-%d").date()
            new_date = current_date + timedelta(weeks=1)
            game_world['calendar']['current_date'] = new_date.strftime("%Y-%m-%d")
            print(f"📅 Advanced to {new_date} (Week Advanced)")
        except Exception as e:
            print(f"Error advancing week: {e}")
    else:
        print("⚠️ Calendar not initialized properly.")

def phase_manager(game_world):
    """Manage season phase based on calendar date."""
    current_date = datetime.strptime(game_world['calendar']['current_date'], "%Y-%m-%d").date()
    season_year = game_world.get("season_year", 2025)

    preseason_end = datetime(season_year, 8, 31).date()
    regular_season_end = datetime(season_year + 1, 1, 8).date()

    if current_date > preseason_end and "Preseason" in game_world.get("current_phase", ""):
        game_world["current_phase"] = "Regular Season Week 1"
        print("🎮 Transitioned to Regular Season.")

    if current_date > regular_season_end and "Regular Season" in game_world.get("current_phase", ""):
        game_world["current_phase"] = "Playoffs"
        print("🏆 Transitioned to Playoffs.")
