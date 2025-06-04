import pytest
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[3]))

from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm.gridiron_gm_pkg.simulation.utils.calendar import Calendar
from gridiron_gm.gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.team_data import fill_team_rosters_with_dummy_players
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager


class _StubFA(LeagueManager.__class__):
    pass


def build_league():
    cal = Calendar()
    league = LeagueManager()
    team_a = Team("A", "CityA", "AAA", "Nova")
    team_b = Team("B", "CityB", "BBB", "Nova")
    fill_team_rosters_with_dummy_players([team_a, team_b])
    league.add_team(team_a)
    league.add_team(team_b)
    return cal, league, team_a, team_b


def sim_today(sm):
    start_index = sm.calendar.current_day_index
    sm.start_day()
    sm.end_day()
    return start_index


def test_games_simulated_and_training(monkeypatch):
    cal, league, ta, tb = build_league()
    class DummyFA:
        def __init__(self, lg):
            pass
        def advance_free_agency_day(self):
            pass

    def fake_simulate_game(home_team, away_team, week=1, context=None, weather=None):
        return {"home": home_team.abbreviation, "away": away_team.abbreviation, "home_score": 0, "away_score": 0}

    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.FreeAgencyManager",
        DummyFA,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager.simulate_game",
        fake_simulate_game,
    )
    sm = SeasonManager(cal, league, save_name="unit_test")
    day_index = cal.current_day_index
    day_name = cal.DAYS_OF_WEEK[day_index]
    week = str(cal.current_week)
    sm.schedule_by_week = {
        week: [{"home_id": ta.id, "away_id": tb.id, "day": day_name, "week": cal.current_week, "kickoff": "00:00"}]
    }
    sm.results_by_week = {week: []}
    sm.last_scheduled_day_for_week = {week: day_index}

    progress_calls = []
    fatigue_calls = []

    def fake_progress(p, stats, context="season"):
        progress_calls.append(p)
        return p

    def fake_recover(self, player, context=None, is_on_field=False):
        fatigue_calls.append(player)

    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.progress_player",
        fake_progress,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.player.fatigue.FatigueSystem.recover",
        fake_recover,
    )

    old_index = sim_today(sm)

    assert len(sm.results_by_week[week]) == 1
    roster_size = len(ta.roster) + len(tb.roster)
    assert len(progress_calls) == roster_size
    assert len(fatigue_calls) == roster_size
    assert sm.calendar.current_day_index == (old_index + 1) % 7


def test_free_agency_runs_in_offseason(monkeypatch):
    cal, league, ta, tb = build_league()
    cal.current_week = 28
    cal.update_phase()
    fa_tick = []
    off_tick = []

    class DummyFA:
        def __init__(self, lg):
            pass
        def advance_free_agency_day(self):
            fa_tick.append(1)

    class DummyOff:
        def __init__(self, lg):
            pass
        def step(self):
            off_tick.append(1)

    def fake_progress(p, stats, context="season"):
        off_tick.append(0)  # use list for counting but not needed
        return p

    def fake_simulate_game(home_team, away_team, week=1, context=None, weather=None):
        return {"home": home_team.abbreviation, "away": away_team.abbreviation, "home_score": 0, "away_score": 0}

    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.FreeAgencyManager",
        DummyFA,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.OffseasonManager",
        DummyOff,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.progress_player",
        fake_progress,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.FatigueSystem.recover",
        lambda self, player, context=None, is_on_field=False: None,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.daily_manager.progress_player",
        fake_progress,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager.simulate_game",
        fake_simulate_game,
    )
    monkeypatch.setattr(
        "gridiron_gm.gridiron_gm_pkg.simulation.systems.game.season_manager.SeasonManager.generate_playoff_bracket_if_ready",
        lambda self: None,
    )
    sm = SeasonManager(cal, league, save_name="unit_test")
    sm.schedule_by_week = {"28": []}

    start_idx = sim_today(sm)

    assert fa_tick
    assert off_tick
    assert sm.calendar.current_day_index == (start_idx + 1) % 7

