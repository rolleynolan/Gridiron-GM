import os
import json
import sys
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.standings_manager import StandingsManager, update_team_records
import gridiron_gm.gridiron_gm_pkg.simulation.engine.game_engine as game_engine
from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.fatigue import accumulate_season_fatigue_for_team
from gridiron_gm.gridiron_gm_pkg.simulation.engine.game_engine import simulate_game

# NEW: Import from team_data
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.team_data import load_teams_from_json, fill_team_rosters_with_dummy_players

from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.data_loader import (
    load_schedule_files, save_results, save_league_state, save_playoff_bracket, save_playoff_results
)
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.serialization_utils import league_to_dict
from gridiron_gm.gridiron_gm_pkg.simulation.utils.generate_schedule import add_nfl_style_playoff_schedule

VERBOSE_SIM_OUTPUT = False

try:
    from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
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

        self.team_map = self.id_to_team  # For compatibility, but always use team.id as key

        self.schedule_by_week, self.results_by_week = self.load_schedule_files(save_name)
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

        for team in self.league.teams:
            abbr = getattr(team, "abbreviation", None)
            conf = getattr(team, "conference", None)
            print(f"  {team.id} ({abbr}) - Conference: {conf}")

        self.standings_manager = StandingsManager(self.calendar, self.league, self.save_name, self.results_by_week)

        for team in self.league.teams:
            abbr = getattr(team, "abbreviation", None)
            conf = getattr(team, "conference", None)
            print(f"  {team.id} ({abbr}) - Conference: {conf}")

    def _reset_standings_for_regular_season(self):
        """Reset standings when transitioning from preseason to regular season."""
        if self.calendar.current_week == 4 and not self.standings_reset:
            for team in self.league.teams:
                team.team_record = {"wins": 0, "losses": 0, "ties": 0, "PF": 0, "PA": 0}
            if hasattr(self.standings_manager, "reset"):
                self.standings_manager.reset()
            self.standings_manager.save_standings()
            self.standings_reset = True

    def load_schedule_files(self, save_name):
        base_path = os.path.join("data", "saves", save_name)
        schedule_path = os.path.join(base_path, "schedule_by_week.json")
        results_path = os.path.join(base_path, "results_by_week.json")
        if os.path.exists(schedule_path):
            with open(schedule_path, "r") as f:
                schedule_by_week = json.load(f)
        else:
            schedule_by_week = {}
        if os.path.exists(results_path):
            with open(results_path, "r") as f:
                results_by_week = json.load(f)
        else:
            results_by_week = {}
        return schedule_by_week, results_by_week

    def save_results(self):
        results_path = os.path.join("data", "saves", self.save_name, "results_by_week.json")
        os.makedirs(os.path.dirname(results_path), exist_ok=True)
        with open(results_path, "w") as f:
            json.dump(self.results_by_week, f, indent=2)

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
            sim_result = simulate_game(
                home_team,
                away_team,
                week=self.calendar.current_week,
                weather=None
            )
            if sim_result is not None:
                home_score = sim_result["home_score"]
                away_score = sim_result["away_score"]
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

                self.results_by_week[week].append({
                    "home": get_team_id(home_team),
                    "away": get_team_id(away_team),
                    "home_score": home_score,
                    "away_score": away_score,
                    "day": today_str
                })
                self.standings_manager.update_from_result(sim_result)
                self.standings_manager.results_by_week = self.results_by_week

                # Print result and updated records immediately (wrap in VERBOSE_SIM_OUTPUT)
                if VERBOSE_SIM_OUTPUT:
                    print(f"Simulated: {get_team_id(home_team)} {home_score} vs {get_team_id(away_team)} {away_score}")
                    for team_obj in [home_team, away_team]:
                        rec = team_obj.team_record
                        abbr = get_team_id(team_obj)
                        print(f"  {abbr} Record: {rec['wins']}-{rec['losses']}-{rec['ties']}, PF: {rec['PF']}, PA: {rec['PA']}")

        # End of day: set time to 23:59
        self.calendar.current_time_str = "23:59"
        self.save_results()

    def advance_day(self):
        prev_week = self.calendar.current_week
        self.calendar.advance_day()

        # Reset team records and standings when preseason ends
        self._reset_standings_for_regular_season()

        self.simulate_games_for_today()
        if self.calendar.current_week != prev_week:
            self.handle_week_end(prev_week)
        # Trigger playoffs if regular season is over and not already generated
        if self.calendar.is_regular_season_over() and not self.playoffs_generated:
            print("[DEBUG] Regular season over, generating playoff bracket...")
            self.generate_playoff_bracket_if_ready()
            print(f"[DEBUG] Playoff bracket: {self.playoff_bracket}")
        self.standings_manager.save_standings()

    def start_day(self):
        """
        Advance the calendar to the next day and prepare schedule info.
        Does NOT simulate games. Use end_day() to simulate games after the day is shown to the user.
        """
        prev_week = self.calendar.current_week
        self.calendar.advance_day()
        # (You may want to reset current_time_str here if needed)
        self.calendar.current_time_str = "00:00"
        # Handle week transitions, playoff bracket, etc. but do NOT simulate games yet
        if self.calendar.current_week != prev_week:
            self.handle_week_end(prev_week)
        if self.calendar.is_regular_season_over() and not self.playoffs_generated:
            print("[DEBUG] Regular season over, generating playoff bracket...")
            self.generate_playoff_bracket_if_ready()
            print(f"[DEBUG] Playoff bracket: {self.playoff_bracket}")
        # Reset standings at start of regular season (after preseason)
        self._reset_standings_for_regular_season()
        # Persist standings after each day
        self.standings_manager.save_standings()

    def end_day(self):
        """
        Simulate all games for the current day and update standings/results.
        Should be called after start_day() and after the user has viewed the day's schedule.
        """
        self.simulate_games_for_today()
        self.standings_manager.save_standings()

    def handle_week_end(self, just_ended_week):
        # Weekly hooks: injuries, morale, awards, etc.
        for team in self.league.teams:
            for player in getattr(team, "roster", []):
                # Check if player is injured and has a games_remaining counter
                if getattr(player, "is_injured", False) and hasattr(player, "games_remaining"):
                    player.games_remaining -= 1
                    if player.games_remaining <= 0:
                        player.is_injured = False
                        player.games_remaining = 0
                        # Optionally clear injury list or log recovery
                        if hasattr(player, "injuries"):
                            player.injuries.clear()
            # Call fatigue accumulation hook (empty list for heavy_usage_players for now)
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
        base_path = os.path.join("data", "saves", self.save_name)
        schedule_path = os.path.join(base_path, "schedule_by_week.json")
        with open(schedule_path, "w") as f:
            json.dump(self.schedule_by_week, f, indent=2)

        self.playoffs_generated = True
        print("[DEBUG] Playoff schedule generated and saved.")

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
        base_path = os.path.join("data", "saves", self.save_name)
        os.makedirs(base_path, exist_ok=True)
        league_path = os.path.join(base_path, "league.json")

        with open(league_path, "w") as f:
            if hasattr(self.league, "to_dict"):
                json.dump(self.league.to_dict(), f, indent=2)
            else:
                json.dump(self.league, f, indent=2)

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
            if issues:
                team_name = getattr(team, "team_name", getattr(team, "name", "UNKNOWN"))
                print(f"[VALIDATION WARNING] {team_name}: " + "; ".join(issues))
            else:
                team_name = getattr(team, "team_name", getattr(team, "name", "UNKNOWN"))
                print(f"[VALIDATION OK] {team_name}: Roster and depth chart complete.")

    def generate_playoff_bracket(self):
        """
        Generate the playoff bracket using full NFL seeding and tiebreaker logic.
        """
        print("=== DEBUG: Generating playoff bracket ===")

        # Helper: get all teams in a conference
        def get_conference_teams(conference):
            return [team for team in self.league.teams if getattr(team, "conference", None) == conference]

        # Helper: get all teams in a division
        def get_division_teams(conference, division):
            return [team for team in self.league.teams if getattr(team, "conference", None) == conference and getattr(team, "division", None) == division]

        # Helper: get all divisions in a conference
        def get_divisions(conference):
            return sorted(set(getattr(team, "division", None) for team in self.league.teams if getattr(team, "conference", None) == conference))

        # --- Tiebreaker helpers ---
        def win_pct(team):
            rec = getattr(team, "team_record", {})
            wins = rec.get("wins", 0)
            losses = rec.get("losses", 0)
            ties = rec.get("ties", 0)
            games = wins + losses + ties
            return (wins + 0.5 * ties) / games if games > 0 else 0.0

        def head_to_head(teams):
            # Only applies if all teams played each other
            if len(teams) < 2:
                return teams
            played = all(
                all(
                    any(
                        (result.get("home") == t1.id and result.get("away") == t2.id) or
                        (result.get("home") == t2.id and result.get("away") == t1.id)
                        for week in self.results_by_week.values() for result in week
                    )
                    for t2 in teams if t2 != t1
                )
                for t1 in teams
            )
            if not played:
                return teams
            # Sort by head-to-head win pct among tied teams
            def h2h_pct(team):
                wins = ties = games = 0
                for t2 in teams:
                    if t2 == team:
                        continue
                    for week in self.results_by_week.values():
                        for result in week:
                            if (result.get("home") == team.id and result.get("away") == t2.id):
                                if result.get("home_score", 0) > result.get("away_score", 0):
                                    wins += 1
                                elif result.get("home_score", 0) == result.get("away_score", 0):
                                    ties += 1
                                games += 1
                            elif (result.get("away") == team.id and result.get("home") == t2.id):
                                if result.get("away_score", 0) > result.get("home_score", 0):
                                    wins += 1
                                elif result.get("away_score", 0) == result.get("home_score", 0):
                                    ties += 1
                                games += 1
                return (wins + 0.5 * ties) / games if games > 0 else 0.0
            return sorted(teams, key=h2h_pct, reverse=True)

        def div_win_pct(team):
            rec = getattr(team, "div_record", {})
            wins = rec.get("wins", 0)
            losses = rec.get("losses", 0)
            ties = rec.get("ties", 0)
            games = wins + losses + ties
            return (wins + 0.5 * ties) / games if games > 0 else 0.0

        def conf_win_pct(team):
            rec = getattr(team, "conf_record", {})
            wins = rec.get("wins", 0)
            losses = rec.get("losses", 0)
            ties = rec.get("ties", 0)
            games = wins + losses + ties
            return (wins + 0.5 * ties) / games if games > 0 else 0.0

        def common_games_win_pct(team, tied_teams):
            # Find all common opponents
            common = set.intersection(*(set(getattr(t, "opponents", [])) for t in tied_teams)) if tied_teams else set()
            if not common:
                return 0.0
            wins = losses = ties = 0
            for opp_id in common:
                for result in self.results_by_week.values():
                    for game in result:
                        if (game.get("home") == team.id and game.get("away") == opp_id):
                            if game.get("home_score", 0) > game.get("away_score", 0):
                                wins += 1
                            elif game.get("home_score", 0) < game.get("away_score", 0):
                                losses += 1
                            else:
                                ties += 1
                        elif (game.get("away") == team.id and game.get("home") == opp_id):
                            if game.get("away_score", 0) > game.get("home_score", 0):
                                wins += 1
                            elif game.get("away_score", 0) < game.get("home_score", 0):
                                losses += 1
                            else:
                                ties += 1
            games = wins + losses + ties
            return (wins + 0.5 * ties) / games if games > 0 else 0.0

        def strength_of_victory(team):
            victories = getattr(team, "victories", [])
            if not victories:
                return 0.0
            total = 0.0
            for opp_id in victories:
                opp = self.id_to_team.get(opp_id)
                if opp:
                    total += win_pct(opp)
            return total / len(victories) if victories else 0.0

        def strength_of_schedule(team):
            opponents = getattr(team, "opponents", [])
            if not opponents:
                return 0.0
            total = 0.0
            for opp_id in opponents:
                opp = self.id_to_team.get(opp_id)
                if opp:
                    total += win_pct(opp)
            return total / len(opponents) if opponents else 0.0

        def combined_conf_rank(team):
            # Lower is better
            conf_teams = [t for t in self.league.teams if getattr(t, "conference", None) == getattr(team, "conference", None)]
            pf_rank = sorted(conf_teams, key=lambda t: getattr(t, "team_record", {}).get("PF", 0), reverse=True).index(team) + 1
            pa_rank = sorted(conf_teams, key=lambda t: getattr(t, "team_record", {}).get("PA", 0)).index(team) + 1
            return pf_rank + pa_rank

        def combined_all_rank(team):
            # Lower is better
            all_teams = self.league.teams
            pf_rank = sorted(all_teams, key=lambda t: getattr(t, "team_record", {}).get("PF", 0), reverse=True).index(team) + 1
            pa_rank = sorted(all_teams, key=lambda t: getattr(t, "team_record", {}).get("PA", 0)).index(team) + 1
            return pf_rank + pa_rank

        def net_points_common(team, tied_teams):
            common = set.intersection(*(set(getattr(t, "opponents", [])) for t in tied_teams)) if tied_teams else set()
            if not common:
                return 0
            net = 0
            for opp_id in common:
                for result in self.results_by_week.values():
                    for game in result:
                        if (game.get("home") == team.id and game.get("away") == opp_id):
                            net += game.get("home_score", 0) - game.get("away_score", 0)
                        elif (game.get("away") == team.id and game.get("home") == opp_id):
                            net += game.get("away_score", 0) - game.get("home_score", 0)
            return net

        def net_points_all(team):
            rec = getattr(team, "team_record", {})
            return rec.get("PF", 0) - rec.get("PA", 0)

        def net_td_all(team):
            return getattr(team, "net_touchdowns", 0)

        # --- Division tiebreakers (for division title and wild card elimination) ---
        def division_tiebreaker(teams):
            tiebreakers = [
                win_pct,
                lambda t: head_to_head(teams).index(t),
                div_win_pct,
                lambda t: common_games_win_pct(t, teams),
                conf_win_pct,
                strength_of_victory,
                strength_of_schedule,
                combined_conf_rank,
                combined_all_rank,
                lambda t: net_points_common(t, teams),
                net_points_all,
                net_td_all
            ]
            sorted_teams = teams[:]
            for tb in tiebreakers:
                sorted_teams = sorted(sorted_teams, key=tb, reverse=True)
                # Check for ties
                groups = []
                i = 0
                while i < len(sorted_teams):
                    group = [sorted_teams[i]]
                    while i + 1 < len(sorted_teams) and abs(tb(sorted_teams[i]) - tb(sorted_teams[i+1])) < 1e-6:
                        group.append(sorted_teams[i+1])
                        i += 1
                    groups.append(group)
                    i += 1
                sorted_teams = []
                for group in groups:
                    if len(group) > 1:
                        # If still tied, continue to next tiebreaker
                        continue
                    sorted_teams.extend(group)
                if len(sorted_teams) == len(teams):
                    break
            return sorted_teams

        # --- Wild card tiebreakers (for teams from different divisions) ---
        def wildcard_tiebreaker(teams):
            tiebreakers = [
                win_pct,
                lambda t: head_to_head(teams).index(t),
                conf_win_pct,
                lambda t: common_games_win_pct(t, teams),
                strength_of_victory,
                strength_of_schedule,
                combined_conf_rank,
                combined_all_rank,
                lambda t: net_points_all(t),
                net_td_all
            ]
            sorted_teams = teams[:]
            for tb in tiebreakers:
                sorted_teams = sorted(sorted_teams, key=tb, reverse=True)
                groups = []
                i = 0
                while i < len(sorted_teams):
                    group = [sorted_teams[i]]
                    while i + 1 < len(sorted_teams) and abs(tb(sorted_teams[i]) - tb(sorted_teams[i+1])) < 1e-6:
                        group.append(sorted_teams[i+1])
                        i += 1
                    groups.append(group)
                    i += 1
                sorted_teams = []
                for group in groups:
                    if len(group) > 1:
                        continue
                    sorted_teams.extend(group)
                if len(sorted_teams) == len(teams):
                    break
            return sorted_teams

        # --- Seeding ---
        playoff_bracket = {}
        for conference in ["Nova", "Atlas"]:
            # 1. Find all divisions in this conference
            divisions = get_divisions(conference)
            division_champs = []
            for division in divisions:
                div_teams = get_division_teams(conference, division)
                champ = division_tiebreaker(div_teams)[0]
                division_champs.append(champ)
            # 2. Seed division champs 1-4 by overall record (tiebreakers)
            division_champs = division_tiebreaker(division_champs)
            # 3. Find all non-division-champ teams for wild card
            all_conf_teams = get_conference_teams(conference)
            wild_card_candidates = [t for t in all_conf_teams if t not in division_champs]
            # 4. Pick top 3 wild cards using wild card tiebreakers
            wild_cards = []
            remaining = wild_card_candidates[:]
            for _ in range(3):
                if not remaining:
                    break
                wc = wildcard_tiebreaker(remaining)[0]
                wild_cards.append(wc)
                remaining.remove(wc)
            # 5. Final seeding: 1-4 division champs, 5-7 wild cards
            seeds = division_champs + wild_cards
            playoff_bracket[conference] = [t.id for t in seeds]

        self.playoff_bracket = playoff_bracket
        self.save_playoff_bracket()
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
        base_path = os.path.join("data", "saves", self.save_name)
        os.makedirs(base_path, exist_ok=True)
        league_path = os.path.join(base_path, "league.json")

        with open(league_path, "w") as f:
            if hasattr(self.league, "to_dict"):
                json.dump(self.league.to_dict(), f, indent=2)
            else:
                json.dump(self.league, f, indent=2)

    def crown_champion(self):
        if self.champion is not None:
            print(f"=== {self.champion} wins the championship! Runner-up: {self.runner_up} ===")
            # ...rest of code...
        else:
            print("No champion was crowned this season.")

    def handle_offseason(self):
        print("=== Offseason: Healing injuries, aging players, resetting league ===")
        for team in self.league.teams:
            for player in getattr(team, "roster", []):
                # Heal all injuries
                player.is_injured = False
                player.weeks_out = 0
                player.games_remaining = 0
                if hasattr(player, "injuries"):
                    player.injuries.clear()
                # Age up
                if hasattr(player, "age"):
                    player.age += 1
                # Optional: retire old/severely injured players
                if hasattr(player, "age") and player.age >= 38:
                    player.retired = True
        # Reset standings, playoff bracket, and schedule for new season
        self.standings_manager.reset_for_new_season()
        self.playoff_bracket = {}
        self.champion = None
        self.runner_up = None
        self.schedule_by_week, self.results_by_week = self.load_schedule_files(self.save_name)
        self.standings_manager.results_by_week = self.results_by_week

    def start_new_season(self):
        print("=== Starting New Season ===")
        self.calendar.current_year += 1
        self.calendar.current_week = 1
        self.calendar.current_day_index = 0
        # Validate rosters and depth charts before loading schedule
        self.validate_team_rosters_and_depth_charts()
        # Generate new schedule, reset standings, etc.
        self.schedule_by_week, self.results_by_week = self.load_schedule_files(self.save_name)
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
        playoff_results = self.simulate_playoffs()
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

        # Save league history with champion
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
        
def to_dict(self):
    team_dicts = []
    for team in self.teams:
        if hasattr(team, "to_dict"):
            t = team.to_dict()
            t["conference"] = getattr(team, "conference", t.get("conference", None))
            t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
            team_dicts.append(t)
        elif isinstance(team, dict):
            t = dict(team)
            t["conference"] = t.get("conference", None)
            t["team_name"] = t.get("team_name", None)
            team_dicts.append(t)
        else:
            t = dict(team.__dict__)
            t["conference"] = getattr(team, "conference", t.get("conference", None))
            t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
            team_dicts.append(t)
    return {
        "teams": team_dicts,
        "free_agents": [player.to_dict() for player in self.free_agents],
        "calendar": self.calendar.serialize(),
        "standings": self.standings,
        "schedule": self.schedule
    }

def update_playoff_schedule(schedule_by_week, playoff_results, round_name, next_round_name, conference):
    """
    Replace 'TBD' placeholders in the next round with actual winners from the current round.
    playoff_results: dict of round_name -> list of game dicts with 'home_id', 'away_id', 'home_score', 'away_score'
    round_name: e.g. "Wild Card"
    next_round_name: e.g. "Divisional"
    conference: "Nova" or "Atlas" or "Both"
    """
    # Find winners from this round
    winners = []
    for game in playoff_results.get(round_name, []):
        if conference != "Both" and game.get("conference") != conference:
            continue
        if game["home_score"] > game["away_score"]:
            winners.append(game["home_id"])
        else:
            winners.append(game["away_id"])

    if not winners:
        print(f"[DEBUG] No winners found for {round_name} ({conference})")
        return

    # For Divisional: 1 seed hosts lowest remaining, other two play each other
    if next_round_name == "Divisional":
        one_seed_id = None
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Divisional" and game.get("conference") == conference and "TBD_LowestSeedWinner" in str(game.get("away_id")):
                    one_seed_id = game.get("home_id")
        if one_seed_id in winners:
            winners.remove(one_seed_id)
        winners_sorted = sorted(winners)
        if len(winners_sorted) < 3:
            print(f"[DEBUG] Not enough winners for Divisional ({conference}): {winners_sorted}")
            return
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Divisional" and game.get("conference") == conference:
                    if "TBD_LowestSeedWinner" in str(game.get("away_id")):
                        game["away_id"] = winners_sorted[0]
                        game["away_abbr"] = None
                    elif "TBD_HighSeedHost" in str(game.get("home_id")):
                        game["home_id"] = winners_sorted[1]
                        game["away_id"] = winners_sorted[2]
                        game["home_abbr"] = None
                        game["away_abbr"] = None

    elif next_round_name == "Conference Championship":
        winners_sorted = sorted(winners)
        if len(winners_sorted) < 2:
            print(f"[DEBUG] Not enough winners for Conference Championship ({conference}): {winners_sorted}")
            return
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Conference Championship" and game.get("conference") == conference:
                    game["home_id"] = winners_sorted[0]
                    game["away_id"] = winners_sorted[1]
                    game["home_abbr"] = None
                    game["away_abbr"] = None

    elif next_round_name == "Gridiron Bowl":
        winners_sorted = sorted(winners)
        if len(winners_sorted) < 2:
            print(f"[DEBUG] Not enough winners for Gridiron Bowl: {winners_sorted}")
            return
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Gridiron Bowl":
                    game["home_id"] = winners_sorted[0]
                    game["away_id"] = winners_sorted[1]
                    game["home_abbr"] = None
                    game["away_abbr"] = None



