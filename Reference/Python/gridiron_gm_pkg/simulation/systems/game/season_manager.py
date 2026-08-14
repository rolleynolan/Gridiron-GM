"""Season management including regular season, playoffs, and offseason."""

import json
import sys
from pathlib import Path
import datetime
from gridiron_gm_pkg.simulation.systems.game.standings_manager import (
    StandingsManager,
    update_team_records,
)
from gridiron_gm_pkg.simulation.systems.game.tiebreakers import (
    StandingsManager as TiebreakerManager,
)
from gridiron_gm_pkg.simulation.systems.game.playoff_manager import (
    PlayoffManager,
    advance_playoff_schedule,
    update_playoff_schedule,
)
import gridiron_gm.gridiron_gm_pkg.simulation.engine.game_engine as game_engine
from gridiron_gm_pkg.simulation.systems.player.fatigue import accumulate_season_fatigue_for_team
from gridiron_gm_pkg.simulation.engine.game_engine import simulate_game
from gridiron_gm_pkg.simulation.utils.box_score import (
    generate_box_score,
    sanitize_box_score_numbers,
)

# NEW: Import from team_data
from gridiron_gm_pkg.simulation.systems.core.team_data import load_teams_from_json, fill_team_rosters_with_dummy_players
from gridiron_gm import VERBOSE_SIM_OUTPUT

from gridiron_gm_pkg.simulation.systems.core.data_loader import (
    load_schedule_files, save_results, save_playoff_bracket, save_playoff_results
)
from gridiron_gm_pkg.simulation.persistence.savegame import save_league
from gridiron_gm_pkg.simulation.utils.generate_schedule import add_nfl_style_playoff_schedule
from gridiron_gm_pkg.simulation.systems.core.daily_core import CoreDailyOperations


try:
    from gridiron_gm_pkg.simulation.entities.team import Team
except ImportError:
    Team = None


class SeasonManager:
    def __init__(self, calendar, league, save_name="test_league"):
        self.calendar = calendar
        self.league = league
        self.save_name = save_name
        self.playoffs_generated = False
        self.champion = None
        self.runner_up = None
        self.standings_reset = False  # <-- Add this flag

        # --- Add this line to initialize playoff_bracket ---
        self.playoff_bracket = {}

        # --- Universal team ID mapping and abbreviation mapping ---
        self.id_to_team = {team.id: team for team in self.league.teams}
        self.id_to_abbr = {team.id: team.abbreviation for team in self.league.teams}
        self.abbr_to_id = {team.abbreviation: team.id for team in self.league.teams}
        self.abbr_to_team = {team.abbreviation: team for team in self.league.teams}

        self.team_map = self.id_to_team  # For compatibility, but always use team.id as key

        self.schedule_by_week, self.results_by_week = load_schedule_files(save_name)
        self.last_scheduled_day_for_week = {
            str(week): (
                max(
                    self.calendar.DAYS_OF_WEEK.index(game["day"].strip().capitalize())
                    for game in games
                    if game["day"].strip().capitalize() in self.calendar.DAYS_OF_WEEK
                )
                if games else 6
            )
            for week, games in self.schedule_by_week.items()
        }

        self.daily_operations = CoreDailyOperations(self)

        if VERBOSE_SIM_OUTPUT:
            for team in self.league.teams:
                abbr = getattr(team, "abbreviation", None)
                conf = getattr(team, "conference", None)
                print(f"  {team.id} ({abbr}) - Conference: {conf}")

        self.standings_manager = StandingsManager(
            self.calendar, self.league, self.save_name, self.results_by_week
        )

    def _save_schedule_by_week(self) -> None:
        base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / self.save_name
        schedule_path = base_path / "schedule_by_week.json"
        base_path.mkdir(parents=True, exist_ok=True)
        with open(schedule_path, "w") as f:
            json.dump(self.schedule_by_week, f, indent=2)

    def advance_playoff_bracket_if_ready(self) -> bool:
        if not getattr(self, "playoffs_generated", False):
            return False
        changed = advance_playoff_schedule(
            self.schedule_by_week,
            self.results_by_week,
            self.id_to_abbr,
        )
        if changed:
            self._save_schedule_by_week()
        return changed

    def _reset_standings_for_regular_season(self):
        """Reset standings when transitioning from preseason to regular season."""
        phase = getattr(self.calendar, "season_phase", None)
        normalize = getattr(self.calendar, "normalize_phase", None)
        if callable(normalize):
            phase = normalize(phase)
        if phase != "regular_season" or self.standings_reset:
            return
        for team in self.league.teams:
            team.team_record = {"wins": 0, "losses": 0, "ties": 0, "PF": 0, "PA": 0}
        standings = {}
        for team in self.league.teams:
            standings[team.id] = {
                "abbr": getattr(team, "abbreviation", ""),
                "conference": getattr(team, "conference", "Unknown"),
                "division": getattr(team, "division", "Unknown"),
                "W": 0,
                "L": 0,
                "T": 0,
                "PF": 0,
                "PA": 0,
                "div_wins": 0,
                "div_losses": 0,
                "div_ties": 0,
                "conf_wins": 0,
                "conf_losses": 0,
                "conf_ties": 0,
                "victories": [],
                "opponents": [],
                "net_touchdowns": 0,
            }
        self.standings_manager.standings = standings
        self.standings_manager.save_standings()
        self.standings_reset = True


    def ensure_valid_depth_charts(self):
        for team in self.league.teams:
            # Generate depth chart if missing or empty
            if not hasattr(team, "depth_chart") or not team.depth_chart or not isinstance(team.depth_chart, dict):
                if hasattr(team, "generate_depth_chart"):
                    team.generate_depth_chart()
                else:
                    team.depth_chart = {}

            # Ensure QB depth exists and is not empty
            qb_depth = team.depth_chart.get("QB", [])
            if not qb_depth:
                # Find best available QB on roster
                qbs = [p for p in getattr(team, "roster", []) if getattr(p, "position", None) == "QB"]
                if qbs:
                    # Assign the first QB as starter
                    team.depth_chart["QB"] = [qbs[0]]
                else:
                    team.depth_chart["QB"] = []
                    team_name = getattr(team, "team_name", "UNKNOWN")
                    print(f"[WARNING] {team_name} has no QB on roster.")

    # Call this at the start of simulate_games_for_today
    def simulate_games_for_today(self):
        self.ensure_valid_depth_charts()

        def get_team_id(team):
            # Safely get a team identifier for results
            if hasattr(team, "abbreviation") and team.abbreviation:
                return team.abbreviation
            elif hasattr(team, "team_name") and team.team_name:
                return team.team_name
            elif hasattr(team, "name") and team.name:
                return team.name
            else:
                return "UNKNOWN"

        week = str(self.calendar.current_week)
        today_str = self.calendar.DAYS_OF_WEEK[self.calendar.current_day_index].capitalize()
        # Ensure current_time_str exists
        if not hasattr(self.calendar, "current_time_str"):
            self.calendar.current_time_str = "00:00"

        if week not in self.schedule_by_week:
            return

        # Only consider games scheduled for today
        today_games = [g for g in self.schedule_by_week[week] if g["day"].strip().capitalize() == today_str]
        # Sort games by kickoff time (HH:MM)
        today_games.sort(key=lambda g: g.get("kickoff", "00:00"))

        print(f"Week {week}, Day {today_str}, Time {self.calendar.current_time_str}: {len(today_games)} games scheduled.")
        if not today_games:
            # End of day: set time to 23:59
            self.calendar.current_time_str = "23:59"
            return

        if week not in self.results_by_week:
            self.results_by_week[week] = []

        for game in today_games:
            kickoff = game.get("kickoff", "00:00")
            # If current_time_str < kickoff, advance to kickoff
            if self.calendar.current_time_str < kickoff:
                self.calendar.current_time_str = kickoff

            already_simmed = any(
                (res["home"] == get_team_id(self.id_to_team.get(game["home_id"])) and res["away"] == get_team_id(self.id_to_team.get(game["away_id"])))
                or (res["home"] == get_team_id(self.id_to_team.get(game["away_id"])) and res["away"] == get_team_id(self.id_to_team.get(game["home_id"])))
                for res in self.results_by_week[week]
            )
            if already_simmed:
                continue
            # Always look up Team objects from IDs before simulating
            home_team = self.id_to_team.get(game["home_id"])
            away_team = self.id_to_team.get(game["away_id"])
            if home_team is None or away_team is None:
                print(f"ERROR: Could not find team object for {game.get('home_id')} or {game.get('away_id')}. Skipping.")
                continue
            sim_home, sim_away = simulate_game(
                home_team,
                away_team,
                week=self.calendar.current_week,
                context={"weather": None, "current_date": self.calendar.current_date},
            )
            if sim_home is not None and sim_away is not None:
                home_score = sim_home.get("points", sim_home.get("score", 0))
                away_score = sim_away.get("points", sim_away.get("score", 0))
                if game.get("playoff") and home_score == away_score:
                    home_score += 3
                box_score = generate_box_score(
                    home_team.id,
                    away_team.id,
                    home_score,
                    away_score,
                    league=self.league,
                )
                sanitize_box_score_numbers(box_score)
                label = game.get("label", "Regular Season")
                season_phase = getattr(self.calendar, "season_phase", None)

                if label == "Regular Season":
                    # Ensure team_record exists and initialize fields
                    for team_obj in [home_team, away_team]:
                        if not hasattr(team_obj, "team_record"):
                            team_obj.team_record = {}
                        team_obj.team_record.setdefault("wins", 0)
                        team_obj.team_record.setdefault("losses", 0)
                        team_obj.team_record.setdefault("ties", 0)
                        team_obj.team_record.setdefault("PF", 0)
                        team_obj.team_record.setdefault("PA", 0)
                    # Update records directly on the loaded team objects (from id_to_team)
                    if home_score > away_score:
                        home_team.team_record["wins"] += 1
                        away_team.team_record["losses"] += 1
                    elif away_score > home_score:
                        away_team.team_record["wins"] += 1
                        home_team.team_record["losses"] += 1
                    else:
                        home_team.team_record["ties"] += 1
                        away_team.team_record["ties"] += 1
                    home_team.team_record["PF"] += home_score
                    home_team.team_record["PA"] += away_score
                    away_team.team_record["PF"] += away_score
                    away_team.team_record["PA"] += home_score

                game_result = {
                    "game_id": f"{week}|{home_team.id}|{away_team.id}",
                    "week": week,
                    "calendar_week": self.calendar.current_week,
                    "season_type": game.get("season_type"),
                    "season_week": game.get("season_week"),
                    "week_key": game.get("week_key"),
                    "home": get_team_id(home_team),
                    "away": get_team_id(away_team),
                    "home_id": home_team.id,
                    "away_id": away_team.id,
                    "home_score": home_score,
                    "away_score": away_score,
                    "day": today_str,
                    "label": label,
                    "season_phase": season_phase,
                    "conference": game.get("conference"),
                    "round": game.get("round"),
                    "box_score": box_score,
                }
                self.results_by_week[week].append(game_result)
                self.standings_manager.update_from_result(game_result)
                self.standings_manager.results_by_week = self.results_by_week
                self.advance_playoff_bracket_if_ready()

                # Print result and updated records immediately (wrap in VERBOSE_SIM_OUTPUT)
                if VERBOSE_SIM_OUTPUT:
                    print(f"Simulated: {get_team_id(home_team)} {home_score} vs {get_team_id(away_team)} {away_score}")
                    for team_obj in [home_team, away_team]:
                        rec = team_obj.team_record
                        abbr = get_team_id(team_obj)
                        print(f"  {abbr} Record: {rec['wins']}-{rec['losses']}-{rec['ties']}, PF: {rec['PF']}, PA: {rec['PA']}")

        # End of day: set time to 23:59
        self.calendar.current_time_str = "23:59"
        save_results(self.results_by_week, self.save_name)
        self._save_schedule_by_week()

    def advance_day(self):
        """Advance the simulation through a single day."""
        self.start_day()
        self.end_day()

    def start_day(self):
        """Prepare the current day for simulation."""
        # Reset the in-day clock
        self.calendar.current_time_str = "00:00"

    def end_day(self):
        """
        Simulate all games for the current day and update standings/results.
        Should be called after start_day() and after the user has viewed the day's schedule.
        """
        self.daily_operations.process_end_of_day()

        prev_week = self.calendar.current_week
        self._apply_daily_birthdays()
        self.calendar.advance_day()
        from gridiron_gm_pkg.simulation.systems.player.injury_status import heal_league_players

        heal_league_players(self.league, self.calendar.current_date)
        self.calendar.current_time_str = "00:00"
        if self.calendar.current_week != prev_week:
            from gridiron_gm_pkg.simulation.systems.player.attribute_xp import apply_weekly_decay

            apply_weekly_decay(
                self.league,
                year=self.calendar.current_year,
                week=self.calendar.current_week,
                current_date=self.calendar.current_date,
            )
            self.handle_week_end(prev_week)
            if self.calendar.is_regular_season_over() and not self.playoffs_generated:
                self.generate_playoff_bracket_if_ready()

        self._reset_standings_for_regular_season()
        self.standings_manager.save_standings()

    def _apply_daily_birthdays(self):
        """Increment age for players whose birthday matches today's date."""
        today = self.calendar.current_date
        players = []
        for team in self.league.teams:
            team_players = getattr(team, "roster", None)
            if team_players is None:
                team_players = getattr(team, "players", [])
            players.extend(team_players)
        players.extend(getattr(self.league, "free_agents", []))

        for player in players:
            dob = getattr(player, "dob", None)
            if not dob:
                continue
            if isinstance(dob, str):
                try:
                    dob_dt = datetime.date.fromisoformat(dob)
                except ValueError:
                    continue
            elif isinstance(dob, datetime.datetime):
                dob_dt = dob.date()
            else:
                dob_dt = dob

            if dob_dt.month == today.month and dob_dt.day == today.day:
                player.age += 1

    def handle_week_end(self, just_ended_week):
        # Core-loop weekly hook: fatigue accumulation only. Training, morale,
        # awards, and coaching effects are design-doc-only in this build.
        for team in self.league.teams:
            accumulate_season_fatigue_for_team(team, [])
        # Persist standings at the end of the week as well
        self.standings_manager.save_standings()

    def generate_playoff_bracket_if_ready(self):
        # Only generate if not already done
        if self.playoffs_generated:
            return

        # Get up-to-date standings by conference (with tiebreakers)
        standings_by_conf = self.standings_manager.get_sorted_standings_by_conference()
        id_to_abbr = {team.id: team.abbreviation for team in self.league.teams}
        # Define REGULAR_SEASON_WEEKS if not imported
        REGULAR_SEASON_WEEKS = 14  # Set this to the correct number of regular season weeks for your league
        # Find the last week number in the schedule (should be the last regular season week)
        last_week = max(
            int(w) for w, games in self.schedule_by_week.items()
            if games and not any(g.get("playoff") for g in games)
        )
        playoff_start_week = last_week + 1

        # Generate the playoff schedule in the schedule_by_week dict
        add_nfl_style_playoff_schedule(self.schedule_by_week, standings_by_conf, id_to_abbr, playoff_start_week)

        # Set playoff_bracket for sim logic (extract from standings)
        self.playoff_bracket = {
            "Nova": [team["id"] for team in standings_by_conf["Nova"][:7]],
            "Atlas": [team["id"] for team in standings_by_conf["Atlas"][:7]]
        }

        # Optionally, save the updated schedule
        self._save_schedule_by_week()

        self.playoffs_generated = True
        if VERBOSE_SIM_OUTPUT:
            print("[DEBUG] Playoff schedule generated and saved.")

    def validate_team_rosters_and_depth_charts(self):
        """
        Ensures all teams have a complete roster and fully populated depth charts before the season starts.
        Prints warnings for any issues found.
        """
        required_positions = ["QB", "RB", "WR", "TE", "LT", "RT", "LG", "RG", "C", "DL", "LB", "CB", "S", "K", "P"]
        min_roster_size = 44  # NFL 53-man, but allow for some flexibility

        for team in self.league.teams:
            issues = []
            # Check roster size
            roster = getattr(team, "roster", [])
            if len(roster) < min_roster_size:
                issues.append(f"Roster too small ({len(roster)} players)")
            # Check for at least one player at each required position
            for pos in required_positions:
                if not any(getattr(p, "position", None) == pos for p in roster):
                    issues.append(f"Missing position: {pos}")
            # Check depth chart
            depth_chart = getattr(team, "depth_chart", {})
            for pos in required_positions:
                if pos not in depth_chart or not depth_chart[pos]:
                    issues.append(f"Depth chart missing or empty for {pos}")
            team_name = getattr(team, "team_name", getattr(team, "name", "UNKNOWN"))
            if VERBOSE_SIM_OUTPUT:
                if issues:
                    print(f"[VALIDATION WARNING] {team_name}: " + "; ".join(issues))
                else:
                    print(f"[VALIDATION OK] {team_name}: Roster and depth chart complete.")

    def generate_playoff_bracket(self):
        """Create the playoff bracket using tiebreakers from ``tiebreakers.py``."""
        if VERBOSE_SIM_OUTPUT:
            print("=== DEBUG: Generating playoff bracket ===")

        def build_tb_manager():
            league_data = [
                {
                    "abbreviation": t.abbreviation,
                    "conference": getattr(t, "conference", "Unknown"),
                    "division": getattr(t, "division", "Unknown"),
                }
                for t in self.league.teams
            ]
            tb = TiebreakerManager(self.calendar, league_data, self.save_name, self.results_by_week)
            tb.standings = {}
            for tid, rec in self.standings_manager.standings.items():
                abbr = self.id_to_abbr.get(tid, tid)
                tb.standings[abbr] = {
                    "W": rec.get("W", 0),
                    "L": rec.get("L", 0),
                    "T": rec.get("T", 0),
                    "PF": rec.get("PF", 0),
                    "PA": rec.get("PA", 0),
                    "conference": rec.get("conference", "Unknown"),
                    "division": rec.get("division", "Unknown"),
                    "opponents": {},
                }
            for week in self.results_by_week.values():
                for res in week:
                    h = res["home"]
                    a = res["away"]
                    hs = res["home_score"]
                    as_ = res["away_score"]
                    h_abbr = self.id_to_abbr.get(h, h)
                    a_abbr = self.id_to_abbr.get(a, a)
                    for t_abbr, o_abbr, ts, os in [
                        (h_abbr, a_abbr, hs, as_),
                        (a_abbr, h_abbr, as_, hs),
                    ]:
                        if t_abbr not in tb.standings:
                            continue
                        opps = tb.standings[t_abbr].setdefault("opponents", {})
                        opps.setdefault(o_abbr, {"W": 0, "L": 0, "T": 0})
                        if ts > os:
                            opps[o_abbr]["W"] += 1
                        elif ts < os:
                            opps[o_abbr]["L"] += 1
                        else:
                            opps[o_abbr]["T"] += 1
            return tb

        tb_manager = build_tb_manager()

        def rank(team_list):
            abbrs = [t.abbreviation for t in team_list]
            ranked_abbrs = tb_manager.break_ties(abbrs)
            return [self.abbr_to_team[a] for a in ranked_abbrs if a in self.abbr_to_team]

        def get_conference_teams(conf):
            return [t for t in self.league.teams if getattr(t, "conference", None) == conf]

        def get_division_teams(conf, div):
            return [t for t in self.league.teams if getattr(t, "conference", None) == conf and getattr(t, "division", None) == div]

        def get_divisions(conf):
            return sorted(set(getattr(t, "division", None) for t in self.league.teams if getattr(t, "conference", None) == conf))

        playoff_bracket = {}
        for conf in ["Nova", "Atlas"]:
            divisions = get_divisions(conf)
            champs = []
            for div in divisions:
                champs.append(rank(get_division_teams(conf, div))[0])
            champs = rank(champs)
            all_conf = get_conference_teams(conf)
            wild_cards = [t for t in rank([t for t in all_conf if t not in champs])[:3]]
            seeds = champs + wild_cards
            playoff_bracket[conf] = [t.id for t in seeds]

        self.playoff_bracket = playoff_bracket
        save_playoff_bracket(self.playoff_bracket, self.save_name)
        print("\n=== FINAL PLAYOFF BRACKET ===")
        for conf, ids in self.playoff_bracket.items():
            abbrs = [f"{tid} ({self.id_to_abbr.get(tid, '?')})" for tid in ids]
            print(f"{conf}: {abbrs}")
        return self.playoff_bracket

    def sim_to(self, target_year, target_week, target_day_index, max_steps=10000):
        """
        Advance the simulation day-by-day until the calendar reaches the target year, week, and day index.
        Prevents infinite loops by limiting the number of steps.
        """
        steps = 0
        while not (
            self.calendar.current_year == target_year and
            self.calendar.current_week == target_week and
            self.calendar.current_day_index == target_day_index
        ):
            self.advance_day()
            steps += 1
            if steps >= max_steps:
                print(f"[WARNING] sim_to: Exceeded {max_steps} steps without reaching the target. Stopping simulation to prevent infinite loop.")
                break

    def save_league_state(self):
        """
        Saves the entire league dictionary (teams + future keys) to JSON.
        """
        base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / self.save_name
        league_path = base_path / "league.json"
        save_league(league_path, self.league)

    def crown_champion(self):
        if self.champion is not None:
            print(f"=== {self.champion} wins the championship! Runner-up: {self.runner_up} ===")
            # ...rest of code...
        else:
            print("No champion was crowned this season.")

    def apply_season_progression(self):
        """No-op in the core-loop build.

        Performance-based player development has been moved to
        design_docs/player_development_system.txt.
        """
        return None

    def handle_offseason(self):
        print("=== Offseason: Healing injuries and resetting league ===")
        for team in self.league.teams:
            for player in getattr(team, "roster", []):
                # Heal all injuries
                from gridiron_gm_pkg.simulation.systems.player.injury_status import clear_injury_fields

                clear_injury_fields(player)
                player.games_remaining = 0
                if hasattr(player, "injuries"):
                    player.injuries.clear()
                # Optional: retire old/severely injured players
                if hasattr(player, "age") and player.age >= 38:
                    player.retired = True
        # Reset standings, playoff bracket, and schedule for new season
        self.standings_manager.reset_for_new_season()
        self.playoff_bracket = {}
        self.champion = None
        self.runner_up = None
        self.schedule_by_week, self.results_by_week = load_schedule_files(self.save_name)
        self.standings_manager.results_by_week = self.results_by_week

    def start_new_season(self):
        print("=== Starting New Season ===")
        self.calendar.current_year += 1
        self.calendar.current_week = 1
        self.calendar.current_date = self.calendar.get_nfl_week1_start(self.calendar.current_year)
        self.calendar.update_phase()
        # Validate rosters and depth charts before loading schedule
        self.validate_team_rosters_and_depth_charts()
        # Generate new schedule, reset standings, etc.
        self.schedule_by_week, self.results_by_week = load_schedule_files(self.save_name)
        self.standings_manager.results_by_week = self.results_by_week
        self.standings_manager.save_standings()

    def run_full_season_cycle(self):
        """
        Run the full NFL-style loop: preseason, regular season, playoffs, offseason, and new season.
        """
        print("=== Preseason ===")
        self.sim_to(self.calendar.current_year, 4, 0)  # Simulate through preseason (weeks 1-3)
        print("=== Preseason complete. Starting Regular Season. ===")

        print("=== Regular Season ===")
        last_reg_season_week = self.calendar.get_last_regular_season_week()
        self.sim_to(self.calendar.current_year, last_reg_season_week + 1, 0)  # Simulate through regular season

        # --- Playoff Bracket ---
        print("=== Playoff Qualification ===")
        bracket = self.generate_playoff_bracket()
        print("\n=== FINAL PLAYOFF BRACKET ===")
        for conf, teams in bracket.items():
            print(f"{conf}: {teams}")

        # --- Playoffs ---
        print("=== Playoffs Begin ===")
        playoff_mgr = PlayoffManager(self)
        playoff_results = playoff_mgr.run_playoffs()
        print("\n=== PLAYOFF RESULTS BY ROUND ===")
        for conf in ["Nova", "Atlas"]:
            print(f"\n{conf} Playoff Games:")
            for result in playoff_results[conf]:
                print(f"{result.get('final_score', '')} | {result.get('result_str', '')}")
        print("\nChampionship:")
        sb = playoff_results["Championship"]
        if sb is not None:
            print(f"{sb.get('final_score', '')} | {sb.get('result_str', '')}")
        else:
            print("No championship game was played.")

        # --- Champion ---
        self.crown_champion()
        print(f"\n=== CHAMPION: {self.champion} ===")
        print(f"Runner-up: {self.runner_up}")

        # Apply end-of-season progression before saving the league
        self.apply_season_progression()

        # Save league history with champion and updated ratings
        self.save_league_state()

        # --- Pause for user review (console) ---
        try:
            input("\nPress Enter to continue to the offseason and next season...")
        except Exception:
            pass  # If running in non-interactive mode, skip pause

        # --- Offseason ---
        print("=== Offseason ===")
        self.handle_offseason()
        print("=== Offseason complete. Injuries healed, players aged, league reset. ===")

        # --- New Season ---
        print("=== New Season Setup ===")
        self.start_new_season()
        print(f"=== Ready for next season: Year {self.calendar.current_year}! ===")
