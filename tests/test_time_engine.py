import datetime

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.systems.game.playoff_manager import advance_playoff_schedule
from gridiron_gm_pkg.simulation.systems.time_engine import (
    InboxMessage,
    TimeEngine,
    make_game_id,
)
from gridiron_gm_pkg.simulation.utils.generate_schedule import add_nfl_style_playoff_schedule


def _make_engine(seed=123, date=None, kickoff="1:00 PM", away_tomorrow=False):
    league = LeagueManager()
    team_a = Team("Alpha", "Alpha City", "ALP", conference="Nova", division="East")
    team_b = Team("Bravo", "Bravo City", "BRV", conference="Nova", division="East")
    league.add_team(team_a)
    league.add_team(team_b)
    league.user_team_id = team_a.id
    league.base_seed = seed

    calendar = league.calendar
    if date is None:
        date = datetime.date(2025, 9, 1)
    calendar.current_date = date
    calendar.current_week = 1

    day_name = date.strftime("%A")
    schedule_by_week = {"1": []}
    if away_tomorrow:
        next_day = (date + datetime.timedelta(days=1)).strftime("%A")
        schedule_by_week["1"].append(
            {
                "home_id": team_b.id,
                "away_id": team_a.id,
                "day": next_day,
                "week": 1,
                "kickoff": kickoff,
            }
        )
    else:
        schedule_by_week["1"].append(
            {
                "home_id": team_a.id,
                "away_id": team_b.id,
                "day": day_name,
                "week": 1,
                "kickoff": kickoff,
            }
        )
    engine = TimeEngine(league, calendar, schedule_by_week=schedule_by_week)
    engine.clock.hour = 8
    engine.ensure_agenda_for_today()
    return engine, team_a, team_b


def test_continue_until_pause_deterministic():
    engine1, _, _ = _make_engine(seed=42)
    engine2, _, _ = _make_engine(seed=42)
    result1 = engine1.continue_until_pause()
    result2 = engine2.continue_until_pause()
    assert result1["ok"] is True
    assert result2["ok"] is True
    assert result1["paused"] is False
    assert result2["paused"] is False
    assert result1["debug_game_events"]["game_results_created"] == 1
    assert result2["debug_game_events"]["game_results_created"] == 1
    assert engine1.clock.hour == engine2.clock.hour


def test_requires_ack_blocks_continue_until_read():
    engine, team_a, _ = _make_engine(seed=55)
    engine.inboxes.setdefault(team_a.id, []).append(
        InboxMessage(
            id=1,
            date=engine.clock.current_date,
            hour=engine.clock.hour,
            category="News",
            subject="Needs review",
            body="Action required.",
            requires_ack=True,
            actions=[],
            read=False,
        )
    )
    blocked = engine.continue_until_pause()
    assert blocked["ok"] is True
    assert blocked["paused"] is True
    assert blocked["advanced_hours"] == 0
    assert blocked["stop_reason"] == "inbox_event"
    assert engine.mark_read(1) is True
    result = engine.continue_until_pause()
    assert result["paused"] is False
    assert result["debug_game_events"]["game_results_created"] == 1


def test_trade_deadline_pauses():
    base_date = datetime.date(2025, 9, 1)
    while base_date.weekday() != 1:
        base_date += datetime.timedelta(days=1)
    engine, _, _ = _make_engine(seed=11, date=base_date)
    calendar = engine.calendar
    preseason = int(getattr(calendar, "PRESEASON_WEEKS", 3))
    rs_start = int(getattr(calendar, "REGULAR_SEASON_START_WEEK", preseason + 1))
    calendar.current_week = rs_start + 7
    calendar.season_phase = "Regular Season"
    calendar.playoff_subphase = None
    calendar.offseason_subphase = None
    engine.last_phase_token = engine._phase_token()
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.ensure_agenda_for_today()
    result = engine.continue_until_pause()
    assert result["paused"] is True
    assert result["stop_reason"] == "trade_deadline"


def test_phase_change_pauses():
    engine, _, _ = _make_engine(seed=21)
    engine.calendar.season_phase = "Regular Season"
    engine.calendar.playoff_subphase = None
    engine.calendar.offseason_subphase = None
    engine.last_phase_token = "Preseason|"
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.ensure_agenda_for_today()
    result = engine.continue_until_pause()
    assert result["paused"] is True
    assert result["stop_reason"] == "phase_change"


def test_preseason_label_propagates_to_results():
    engine, team_a, team_b = _make_engine(seed=31)
    engine.schedule_by_week["1"][0]["label"] = "Preseason"
    game_id = make_game_id("1", team_a.id, team_b.id)
    result = engine.simulate_user_game(game_id)
    assert result["label"] == "Preseason"


def test_agenda_travel_cancels_training():
    engine, _, _ = _make_engine(seed=99, away_tomorrow=True)
    date = engine.clock.current_date
    events_today = [ev for ev in engine.queue.events() if ev.date == date]
    has_travel = any(ev.type == "Travel" for ev in events_today)
    has_training = any(ev.type == "TrainingSlot" for ev in events_today)
    has_kickoff = any(ev.type == "GameKickoff" for ev in events_today)
    assert has_travel is True
    assert has_training is False
    assert has_kickoff is False


def test_agenda_game_day_schedules_kickoff_and_wrap():
    engine, team_a, team_b = _make_engine(seed=7)
    date = engine.clock.current_date
    events_today = [ev for ev in engine.queue.events() if ev.date == date]
    kickoff_hours = [ev.hour for ev in events_today if ev.type == "GameKickoff"]
    wrap_hours = [ev.hour for ev in events_today if ev.type == "GameWrap"]
    assert kickoff_hours == [13]
    assert wrap_hours == [16]
    game_id = make_game_id("1", team_a.id, team_b.id)
    assert any(ev.payload.get("game_id") == game_id for ev in events_today)


def test_kickoff_message_actions_include_sim_game():
    engine, team_a, team_b = _make_engine(seed=17)
    result = engine.continue_until_pause()
    assert result["paused"] is False
    game_id = make_game_id("1", team_a.id, team_b.id)
    inbox = engine.get_inbox(team_a.id)
    kickoff = next((msg for msg in inbox if msg.subject.startswith("Kickoff:")), None)
    assert kickoff is not None
    payload = kickoff.to_dict()
    actions = payload["actions"]
    assert isinstance(actions, list)
    assert actions
    assert actions[0]["type"] == "SIM_GAME"
    assert actions[0]["game_id"] == game_id
    assert actions[0]["payload"]["game_id"] == game_id


def test_clock_serializes_current_time():
    engine, _, _ = _make_engine(seed=18)
    payload = engine.clock.serialize()
    assert payload["current_date"] == engine.clock.current_date.isoformat()
    assert payload["current_time"] == "08:00"
    assert payload["hour"] == 8


def test_advance_to_next_event_processes_next_timestamp():
    engine, _, _ = _make_engine(seed=19, away_tomorrow=True)
    result = engine.advance_to_next_event(max_hours=8)
    assert result["ok"] is True
    assert result["paused"] is False
    assert result["target_reached"] is True
    assert result["stop_reason"] == "next_event"
    assert result["clock"]["current_time"] == "09:00"
    assert result["last_processed_event"]["type"] == "InboxCheck"


def test_advance_to_end_of_day_processes_user_game_and_reaches_next_day():
    engine, _, _ = _make_engine(seed=20)
    result = engine.advance_to_end_of_day(max_hours=24)
    assert result["ok"] is True
    assert result["paused"] is False
    assert result["target_reached"] is True
    assert result["stop_reason"] == "end_of_day"


def test_advance_one_week_reaches_target_timestamp_without_schedule():
    engine, _, _ = _make_engine(seed=22, away_tomorrow=True)
    start_date = engine.clock.current_date
    engine.schedule_by_week = {}
    engine.queue = engine._ensure_queue().__class__()
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()
    result = engine.advance_one_week(max_hours=24 * 8)
    assert result["ok"] is True
    assert result["paused"] is False
    assert result["target_reached"] is True
    assert result["stop_reason"] == "one_week"
    assert engine.clock.current_date == start_date + datetime.timedelta(days=7)
    assert engine.clock.hour == 8


def test_continue_pauses_for_user_playoff_game_and_inbox_manual_sim():
    engine, team_a, team_b = _make_engine(seed=27)
    playoff_start = engine.calendar.phase_boundaries[engine.calendar.PHASE_PLAYOFFS][0]
    engine.calendar.current_week = playoff_start
    engine.calendar.current_date = datetime.date(2026, 1, 10)
    engine.calendar.update_phase()
    engine.schedule_by_week = {
        str(playoff_start): [
            {
                "week": playoff_start,
                "calendar_week": playoff_start,
                "season_type": "playoffs",
                "season_week": 1,
                "week_key": "playoffs:1",
                "day": "Saturday",
                "kickoff": "1:00 PM",
                "home_id": team_a.id,
                "away_id": team_b.id,
                "label": "Playoffs - Wild Card",
            }
        ]
    }
    engine.clock.current_date = engine.calendar.current_date
    engine.clock.hour = 8
    engine.queue = engine._ensure_queue().__class__()
    engine.last_phase_token = engine._phase_token()
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()

    result = engine.continue_until_pause(max_hours=48)
    game_id = make_game_id(str(playoff_start), team_a.id, team_b.id)

    assert result["ok"] is True
    assert result["stop_reason"] != "user_game_ready"
    assert engine._find_result(game_id) is not None

    inbox = engine.get_inbox(team_a.id)
    kickoff = next((msg for msg in inbox if msg.subject.startswith("Kickoff:")), None)
    assert kickoff is not None
    assert kickoff.requires_ack is False
    assert kickoff.actions[0]["game_id"] == game_id


def test_placeholder_playoff_game_does_not_appear_in_inbox():
    engine, team_a, team_b = _make_engine(seed=29)
    playoff_start = engine.calendar.phase_boundaries[engine.calendar.PHASE_PLAYOFFS][0]
    engine.calendar.current_week = playoff_start + 1
    engine.calendar.current_date = datetime.date(2026, 1, 17)
    engine.calendar.update_phase()
    engine.schedule_by_week = {
        str(playoff_start + 1): [
            {
                "week": playoff_start + 1,
                "calendar_week": playoff_start + 1,
                "season_type": "playoffs",
                "season_week": 2,
                "week_key": "playoffs:2",
                "day": "Saturday",
                "kickoff": "4:30 PM",
                "home_id": team_a.id,
                "away_id": "TBD_LowestSeedWinner_Nova",
                "label": "Playoffs - Divisional",
                "playoff": True,
                "round": "Divisional",
                "conference": "Nova",
            }
        ]
    }
    engine.clock.current_date = engine.calendar.current_date
    engine.clock.hour = 8
    engine.queue = engine._ensure_queue().__class__()
    engine.last_phase_token = engine._phase_token()
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()

    result = engine.continue_until_pause(max_hours=24)

    assert result["ok"] is True
    assert result["stop_reason"] != "user_game_ready"
    assert not any(msg.subject.startswith("Kickoff:") for msg in engine.get_inbox(team_a.id))


def test_completed_playoff_game_does_not_reappear_in_inbox():
    engine, team_a, team_b = _make_engine(seed=41)
    playoff_start = engine.calendar.phase_boundaries[engine.calendar.PHASE_PLAYOFFS][0]
    engine.calendar.current_week = playoff_start
    engine.calendar.current_date = datetime.date(2026, 1, 10)
    engine.calendar.update_phase()
    engine.schedule_by_week = {
        str(playoff_start): [
            {
                "week": playoff_start,
                "calendar_week": playoff_start,
                "season_type": "playoffs",
                "season_week": 1,
                "week_key": "playoffs:1",
                "day": "Saturday",
                "kickoff": "1:00 PM",
                "home_id": team_a.id,
                "away_id": team_b.id,
                "label": "Playoffs - Wild Card",
                "playoff": True,
                "round": "Wild Card",
                "conference": "Nova",
            }
        ]
    }
    engine.clock.current_date = engine.calendar.current_date
    game_id = make_game_id(str(playoff_start), team_a.id, team_b.id)
    engine._record_result(
        {
            "game_id": game_id,
            "week": str(playoff_start),
            "calendar_week": playoff_start,
            "season_type": "playoffs",
            "season_week": 1,
            "week_key": "playoffs:1",
            "home_id": team_a.id,
            "away_id": team_b.id,
            "home": team_a.id,
            "away": team_b.id,
            "home_score": 24,
            "away_score": 17,
            "day": "Saturday",
            "kickoff_time": "1:00 PM",
            "label": "Playoffs - Wild Card",
            "playoff": True,
            "round": "Wild Card",
            "conference": "Nova",
        }
    )
    engine.clock.hour = 8
    engine.queue = engine._ensure_queue().__class__()
    engine.last_phase_token = engine._phase_token()
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()

    result = engine.continue_until_pause(max_hours=24)

    assert result["ok"] is True
    assert not any(msg.subject.startswith("Kickoff:") for msg in engine.get_inbox(team_a.id))


def test_manual_user_playoff_game_sim_records_result_and_advances_round():
    league = LeagueManager()
    teams = []
    for name, abbr, conf in [
        ("Alpha", "ALP", "Nova"),
        ("Bravo", "BRV", "Nova"),
        ("Charlie", "CHR", "Nova"),
        ("Delta", "DLT", "Nova"),
        ("Indigo", "IND", "Nova"),
        ("Juliet", "JLT", "Nova"),
        ("Kilo", "KLO", "Nova"),
        ("Echo", "ECH", "Atlas"),
        ("Foxtrot", "FOX", "Atlas"),
        ("Golf", "GLF", "Atlas"),
        ("Hotel", "HTL", "Atlas"),
        ("Lima", "LMA", "Atlas"),
        ("Mike", "MIK", "Atlas"),
        ("November", "NOV", "Atlas"),
    ]:
        team = Team(name, f"{name} City", abbr, conference=conf, division="East")
        league.add_team(team)
        teams.append(team)
    user_team = teams[1]
    league.user_team_id = user_team.id
    league.base_seed = 51

    calendar = league.calendar
    playoff_start = calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][0]
    calendar.current_week = playoff_start
    calendar.current_date = datetime.date(2026, 1, 10)
    calendar.update_phase()

    standings_by_conf = {
        "Nova": [{"id": team.id, "abbr": team.abbreviation, "conference": "Nova"} for team in teams[:7]],
        "Atlas": [{"id": team.id, "abbr": team.abbreviation, "conference": "Atlas"} for team in teams[7:14]],
    }
    schedule_by_week = {}
    add_nfl_style_playoff_schedule(
        schedule_by_week,
        standings_by_conf,
        {team.id: team.abbreviation for team in teams},
        playoff_start,
    )
    opp_team = next(
        game for game in schedule_by_week[str(playoff_start)]
        if game.get("conference") == "Nova" and game.get("home_id") == user_team.id
    )["away_id"]

    class _SeasonManager:
        def __init__(self, schedule):
            self.schedule_by_week = schedule
            self.results_by_week = {}
            self.playoffs_generated = True
            self.save_name = "unit_test_manual_playoff_user_game"
            self.id_to_abbr = {team.id: team.abbreviation for team in teams}

        def advance_playoff_bracket_if_ready(self):
            return advance_playoff_schedule(
                self.schedule_by_week,
                self.results_by_week,
                self.id_to_abbr,
            )

    season_manager = _SeasonManager(schedule_by_week)
    engine = TimeEngine(league, calendar, season_manager=season_manager, schedule_by_week=schedule_by_week)
    engine.clock.hour = 8
    engine.last_phase_token = engine._phase_token()
    engine.league.last_phase_token = engine.last_phase_token
    engine.last_agenda_date = None
    engine.league.last_agenda_date = None
    engine.ensure_agenda_for_today()

    kickoff_pause = engine.continue_until_pause(max_hours=48)
    game_id = make_game_id(str(playoff_start), user_team.id, opp_team)

    assert kickoff_pause["ok"] is True
    assert kickoff_pause["stop_reason"] != "user_game_ready"
    assert kickoff_pause["debug_game_events"]["game_results_created"] >= 1
    result = engine.simulate_user_game(game_id)
    assert result.get("error") is None
    assert result["game_id"] == game_id
    assert result.get("already_simmed") is True
    assert engine._find_result(game_id) is not None
    season_manager.results_by_week.setdefault(str(playoff_start), [])
    for game in schedule_by_week[str(playoff_start)]:
        candidate_game_id = make_game_id(str(playoff_start), game["home_id"], game["away_id"])
        if candidate_game_id == game_id:
            continue
        if any(entry.get("game_id") == candidate_game_id for entry in season_manager.results_by_week[str(playoff_start)]):
            continue
        home_wins = game.get("home_id")
        engine._record_result(
            {
                "game_id": candidate_game_id,
                "week": str(playoff_start),
                "calendar_week": playoff_start,
                "season_type": "playoffs",
                "season_week": 1,
                "week_key": "playoffs:1",
                "home_id": game["home_id"],
                "away_id": game["away_id"],
                "home": game["home_id"],
                "away": game["away_id"],
                "home_score": 24,
                "away_score": 17,
                "day": game.get("day"),
                "kickoff_time": game.get("kickoff"),
                "label": game.get("label"),
                "playoff": True,
                "round": game.get("round"),
                "conference": game.get("conference"),
                "home_seed": game.get("home_seed"),
                "away_seed": game.get("away_seed"),
                "winner_id": home_wins,
            }
        )
    divisional_game = season_manager.schedule_by_week[str(playoff_start + 1)][0]
    assert not str(divisional_game["away_id"]).startswith("TBD")
    assert any(msg.subject.startswith("Final:") for msg in engine.get_inbox(user_team.id))


def test_continue_through_playoffs_when_user_team_eliminated():
    league = LeagueManager()
    user_team = Team("User", "User City", "USR", conference="Nova", division="East")
    team_a = Team("Alpha", "Alpha City", "ALP", conference="Nova", division="East")
    team_b = Team("Bravo", "Bravo City", "BRV", conference="Atlas", division="East")
    for team in (user_team, team_a, team_b):
        league.add_team(team)
    league.user_team_id = user_team.id
    league.base_seed = 33

    calendar = league.calendar
    playoff_start = calendar.phase_boundaries[calendar.PHASE_PLAYOFFS][0]
    offseason_start = calendar.phase_boundaries[calendar.PHASE_OFFSEASON][0]
    calendar.current_week = playoff_start
    calendar.current_date = datetime.date(2026, 1, 10)
    calendar.update_phase()

    schedule_by_week = {
        str(week): [
            {
                "week": week,
                "calendar_week": week,
                "season_type": "playoffs",
                "season_week": week - playoff_start + 1,
                "week_key": f"playoffs:{week - playoff_start + 1}",
                "day": "Sunday",
                "kickoff": "1:00 PM",
                "home_id": team_a.id,
                "away_id": team_b.id,
                "label": "Playoffs",
            }
        ]
        for week in range(playoff_start, offseason_start)
    }
    engine = TimeEngine(league, calendar, schedule_by_week=schedule_by_week)
    engine.clock.hour = 8
    engine.last_phase_token = engine._phase_token()
    engine.league.last_phase_token = engine.last_phase_token
    engine.ensure_agenda_for_today()

    result = engine.continue_until_pause(max_hours=800)

    assert result["ok"] is True
    assert result["paused"] is True
    assert result["stop_reason"] in {"end_of_season", "phase_change"}
    assert engine.calendar.season_phase in {"playoffs", "postseason", "offseason"}
    assert engine.league.results_by_week
