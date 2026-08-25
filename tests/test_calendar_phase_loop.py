import shutil
import uuid
from pathlib import Path
import datetime

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.systems.game import season_manager as season_manager_module
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager
from gridiron_gm_pkg.simulation.utils.calendar import Calendar


def _build_playoff_ready_league():
    league = LeagueManager()
    for conf in ("Nova", "Atlas"):
        for idx in range(7):
            team = Team(
                team_name=f"{conf} {idx}",
                city=f"{conf} City {idx}",
                abbreviation=f"{conf[0]}{idx}",
                conference=conf,
                division="East",
                id=f"{conf[0]}{idx}",
            )
            league.add_team(team)
    return league


def _regular_schedule_through(calendar):
    first = calendar.REGULAR_SEASON_START_WEEK
    last = calendar.REGULAR_SEASON_END_WEEK
    return {
        str(week): [
            {
                "week": week,
                "day": "Sunday",
                "kickoff": "1:00 PM",
                "home_id": "N0",
                "away_id": "N1",
                "label": "Regular Season",
            }
        ]
        for week in range(first, last + 1)
    }


def test_regular_season_end_generates_playoffs_once(monkeypatch):
    calendar = Calendar(start_year=2026)
    league = _build_playoff_ready_league()
    schedule = _regular_schedule_through(calendar)
    save_name = f"unit_test_calendar_loop_{uuid.uuid4().hex}"
    save_dir = Path("gridiron_gm_pkg") / "data" / "saves" / save_name
    save_dir.mkdir(parents=True, exist_ok=True)
    monkeypatch.setattr(season_manager_module, "load_schedule_files", lambda _save_name: (dict(schedule), {}))
    manager = SeasonManager(calendar, league, save_name=save_name)
    manager.calendar.current_week = manager.calendar.REGULAR_SEASON_END_WEEK + 1
    manager.calendar.update_phase()

    try:
        manager.generate_playoff_bracket_if_ready()
        generated_once = {week: list(games) for week, games in manager.schedule_by_week.items()}
        manager.generate_playoff_bracket_if_ready()

        assert manager.playoffs_generated is True
        assert manager.calendar.season_phase == "playoffs"
        assert manager.playoff_bracket
        assert manager.schedule_by_week == generated_once
        playoff_weeks = range(
            manager.calendar.phase_boundaries[manager.calendar.PHASE_PLAYOFFS][0],
            manager.calendar.phase_boundaries[manager.calendar.PHASE_PLAYOFFS][1] + 1,
        )
        assert any(
            game.get("round") == "Gridiron Bowl"
            for game in manager.schedule_by_week[str(max(playoff_weeks))]
        )
        assert str(manager.calendar.phase_boundaries[manager.calendar.PHASE_POSTSEASON][0]) not in manager.schedule_by_week
    finally:
        shutil.rmtree(save_dir, ignore_errors=True)


def test_generated_playoff_dates_land_in_january_and_progress_forward(monkeypatch):
    calendar = Calendar(start_year=2026)
    league = _build_playoff_ready_league()
    schedule = _regular_schedule_through(calendar)
    save_name = f"unit_test_calendar_dates_{uuid.uuid4().hex}"
    save_dir = Path("gridiron_gm_pkg") / "data" / "saves" / save_name
    save_dir.mkdir(parents=True, exist_ok=True)
    monkeypatch.setattr(season_manager_module, "load_schedule_files", lambda _save_name: (dict(schedule), {}))
    manager = SeasonManager(calendar, league, save_name=save_name)
    manager.calendar.current_week = manager.calendar.REGULAR_SEASON_END_WEEK + 1
    manager.calendar.current_date = datetime.date(2027, 1, 11)
    manager.calendar.update_phase()

    try:
        manager.generate_playoff_bracket_if_ready()

        wild_card = manager.schedule_by_week[str(calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][0])][0]
        championship = manager.schedule_by_week[str(calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][1])][0]

        def _date_for(game):
            week_start = calendar.nfl_week1_start_date + datetime.timedelta(days=(int(game["week"]) - 1) * 7)
            return week_start + datetime.timedelta(days=calendar.DAYS_OF_WEEK.index(game["day"]))

        wild_card_date = _date_for(wild_card)
        championship_date = _date_for(championship)

        assert wild_card["round"] == "Wild Card"
        assert wild_card_date.month == 1
        assert wild_card_date.strftime("%B") != "February"
        assert championship_date > wild_card_date
        assert championship["round"] == "Gridiron Bowl"
    finally:
        shutil.rmtree(save_dir, ignore_errors=True)
