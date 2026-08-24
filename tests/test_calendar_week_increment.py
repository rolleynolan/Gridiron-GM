import datetime

from gridiron_gm_pkg.simulation.utils.calendar import Calendar


def _next_weekday(start: datetime.date, target: int) -> datetime.date:
    date = start
    while date.weekday() != target:
        date += datetime.timedelta(days=1)
    return date


def test_calendar_week_advances_after_sunday():
    cal = Calendar(start_year=2025)
    sunday = _next_weekday(datetime.date(2025, 9, 1), 6)
    cal.current_date = sunday
    cal.current_week = 1

    cal.advance_day()

    assert cal.current_date.weekday() == 0
    assert cal.current_week == 2
    assert cal.football_week == 2


def test_calendar_week_stays_on_sunday_arrival():
    cal = Calendar(start_year=2025)
    sunday = _next_weekday(datetime.date(2025, 9, 1), 6)
    saturday = sunday - datetime.timedelta(days=1)
    cal.current_date = saturday
    cal.current_week = 1

    cal.advance_day()

    assert cal.current_date.weekday() == 6
    assert cal.current_week == 1


def test_initial_calendar_state_is_json_safe():
    cal = Calendar(start_year=2026)
    payload = cal.serialize()

    assert payload["current_date"] == "2026-08-10"
    assert payload["day_of_week"] == "Monday"
    assert payload["season_year"] == 2026
    assert payload["season_phase"] == "preseason"
    assert payload["football_week"] == 1
    assert payload["phase_label"] == "Preseason"
    assert payload["week_label"] == "Preseason Week 1"
    assert cal.get_week_label() == "Preseason Week 1"


def test_calendar_phase_loop_boundaries():
    cal = Calendar(start_year=2026)

    expected = [
        (1, "preseason", "Preseason Week 1"),
        (2, "preseason", "Preseason Week 2"),
        (3, "preseason", "Preseason Week 3"),
        (4, "preseason_bye", "Preseason Bye / Final Cutdown"),
        (5, "regular_season", "Regular Season Week 1"),
        (6, "regular_season", "Regular Season Week 2"),
        (22, "regular_season", "Regular Season Week 18"),
        (23, "playoffs", "Playoffs - Wild Card"),
        (26, "playoffs", "Playoffs - Gridiron Bowl"),
        (27, "postseason", "Postseason"),
        (28, "offseason", "Offseason"),
    ]
    for week, phase, label in expected:
        cal.current_week = week
        assert cal.season_phase == phase
        assert cal.get_week_label() == label


def test_preseason_week_three_advances_to_bye_then_regular_week_one():
    cal = Calendar(start_year=2026)
    cal.current_date = datetime.date(2026, 9, 20)
    cal.current_week = 3

    cal.advance_day()
    assert cal.football_week == 4
    assert cal.season_phase == "preseason_bye"
    assert cal.get_week_label() == "Preseason Bye / Final Cutdown"

    cal.advance_week()
    assert cal.football_week == 5
    assert cal.season_phase == "regular_season"
    assert cal.get_week_label() == "Regular Season Week 1"


def test_playoffs_postseason_offseason_and_new_season_rollover():
    cal = Calendar(start_year=2026)
    cal.current_date = datetime.date(2027, 8, 29)
    cal.current_week = 52

    cal.advance_day()

    assert cal.season_year == 2027
    assert cal.football_week == 1
    assert cal.season_phase == "preseason"
    assert cal.playoff_subphase is None
    assert cal.offseason_subphase is None


def test_advance_one_day_updates_real_date_and_day_name():
    cal = Calendar(start_year=2026)
    cal.current_date = datetime.date(2026, 9, 13)
    cal.current_week = 1

    cal.advance_day()

    assert cal.current_date == datetime.date(2026, 9, 14)
    assert cal.day_of_week == "Monday"
    assert cal.football_week == 2


def test_advance_multiple_days_across_month_boundary():
    cal = Calendar(start_year=2026)
    cal.current_date = datetime.date(2026, 9, 29)
    cal.current_week = 4

    for _ in range(3):
        cal.advance_day()

    assert cal.current_date == datetime.date(2026, 10, 2)
    assert cal.day_of_week == "Friday"
    assert cal.football_week == 4


def test_leap_year_february_29_behavior():
    cal = Calendar(start_year=2028)
    cal.current_date = datetime.date(2028, 2, 28)
    cal.current_week = 26

    cal.advance_day()
    assert cal.current_date == datetime.date(2028, 2, 29)
    assert cal.day_of_week == "Tuesday"

    cal.advance_day()
    assert cal.current_date == datetime.date(2028, 3, 1)
    assert cal.day_of_week == "Wednesday"


def test_non_leap_year_february_behavior():
    cal = Calendar(start_year=2027)
    cal.current_date = datetime.date(2027, 2, 28)
    cal.current_week = 26

    cal.advance_day()

    assert cal.current_date == datetime.date(2027, 3, 1)
    assert cal.day_of_week == "Monday"


def test_year_rollover_preserves_football_state():
    cal = Calendar(start_year=2026)
    cal.current_date = datetime.date(2026, 12, 31)
    cal.current_week = 18

    cal.advance_day()

    assert cal.current_date == datetime.date(2027, 1, 1)
    assert cal.day_of_week == "Friday"
    assert cal.season_year == 2026
    assert cal.football_week == 18


def test_advance_week_uses_real_day_advancement():
    cal = Calendar(start_year=2026)
    cal.current_date = datetime.date(2026, 9, 13)
    cal.current_week = 1

    cal.advance_week()

    assert cal.current_date == datetime.date(2026, 9, 20)
    assert cal.football_week == 2
    assert cal.day_of_week == "Sunday"


def test_calendar_deserializes_legacy_without_current_date():
    cal = Calendar.deserialize(
        {
            "current_year": 2027,
            "current_week": 4,
            "season_phase": "Regular Season",
        }
    )

    assert cal.current_date == Calendar.get_nfl_week1_start(2027)
    assert cal.season_year == 2027
    assert cal.football_week == 4
    assert cal.season_phase == "regular_season"
