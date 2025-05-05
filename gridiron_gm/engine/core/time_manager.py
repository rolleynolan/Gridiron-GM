def advance_day(game_world):
    calendar_data = game_world["calendar"]

    # Advance 1 day
    from datetime import datetime, timedelta

    current_date = datetime.strptime(calendar_data["current_date"], "%Y-%m-%d").date()
    new_date = current_date + timedelta(days=1)
    calendar_data["current_date"] = new_date.isoformat()

    # Check milestones
    check_milestones(calendar_data)

def check_milestones(calendar_data):
    from datetime import datetime

    current_date = datetime.strptime(calendar_data["current_date"], "%Y-%m-%d").date()

    if current_date == datetime(2025, 9, 7).date():
        calendar_data["season_phase"] = "REGULAR_SEASON"
        print("\n🏈 Regular Season has begun!")
    elif current_date == datetime(2025, 12, 20).date():
        calendar_data["season_phase"] = "PLAYOFFS"
        print("\n🏆 Playoffs have started!")
    elif current_date == datetime(2026, 4, 25).date():
        calendar_data["season_phase"] = "DRAFT"
        print("\n🎯 NFL Draft is today!")
