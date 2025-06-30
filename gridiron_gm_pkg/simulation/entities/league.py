<<<<<<< HEAD
import os
import json
import random
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.utils.calendar import Calendar  # Update if calendar is moved elsewhere
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager  # Update if season_manager is moved elsewhere
ROOT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))

class LeagueManager:
    def __init__(self):
        self.teams = []
        self.free_agents = []
        self.draft_prospects = []  # <-- Add this line
        self.calendar = Calendar()
        # Standings now keyed by team ID
        self.standings = {}
        self.week_in_progress = False
        self.schedule = {}
        self.id_to_team = {}  # Use team.id as universal key
        self.id_to_abbr = {}  # For display
        self.abbr_to_id = {}  # For legacy conversion

    def _rebuild_team_maps(self):
        """Ensure all team mappings are up-to-date and complete."""
        self.id_to_team = {}
        self.id_to_abbr = {}
        self.abbr_to_id = {}
        for team in self.teams:
            if hasattr(team, "id"):
                self.id_to_team[team.id] = team
                self.id_to_abbr[team.id] = team.abbreviation
                self.abbr_to_id[team.abbreviation] = team.id

    def add_team(self, team):
        self.teams.append(team)
        # Ensure abbreviation and conference are set on the team object
        if not hasattr(team, "abbreviation") or team.abbreviation is None:
            raise ValueError("Team must have an abbreviation.")
        if not hasattr(team, "conference") or team.conference is None:
            team.conference = "Nova"
        self.standings[team.id] = {
            "wins": 0,
            "losses": 0,
            "ties": 0,
            "abbr": team.abbreviation,
            "conference": team.conference
        }
        self._rebuild_team_maps()

    def remove_team(self, team):
        if team in self.teams:
            self.teams.remove(team)
            if hasattr(team, "id") and team.id in self.standings:
                del self.standings[team.id]
            self._rebuild_team_maps()

    def advance_week(self):
        self.week_in_progress = True
        self.simulate_weekly_games()
        self.calendar.advance_week()
        self.week_in_progress = False

    def auto_fill_rosters(self, minimum_roster_size=53):
        for team in self.teams:
            while len(team.players) < minimum_roster_size and self.free_agents:
                player = self.free_agents.pop(0)
                team.add_player(player)

    def generate_schedule(self, weeks=14):
        # Machine-readable: use team IDs in schedule
        team_ids = [team.id for team in self.teams]
        num_teams = len(team_ids)
        if num_teams % 2 != 0:
            team_ids.append("BYE")

        total_weeks = weeks
        self.schedule = {}

        for week in range(1, total_weeks + 1):
            random.shuffle(team_ids)
            weekly_matchups = []
            for i in range(0, len(team_ids), 2):
                team1_id = team_ids[i]
                team2_id = team_ids[i + 1]
                if "BYE" not in (team1_id, team2_id):
                    weekly_matchups.append((team1_id, team2_id))
            self.schedule[week] = weekly_matchups

    def simulate_weekly_games(self, debug=False):
        current_week = self.calendar.current_week
        if current_week not in self.schedule:
            print(f"No games scheduled for Week {current_week}.")
            return

        print(f"📅 Simulating Week {current_week} games...")
        weekly_matchups = self.schedule[current_week]
        results = []
        for team1_id, team2_id in weekly_matchups:
            team1 = self.get_team_by_id(team1_id)
            team2 = self.get_team_by_id(team2_id)
            if not team1 or not team2:
                continue
            winner = random.choice([team1, team2])
            loser = team2 if winner == team1 else team1
            is_tie = random.random() < 0.02
            # Standings are keyed by team ID
            if is_tie:
                self.standings[team1.id]["ties"] += 1
                self.standings[team2.id]["ties"] += 1
                result = {
                    "type": "tie",
                    "team1": team1.id,
                    "team2": team2.id,
                    "score": (0, 0)
                }
                if debug:
                    print(f"🤝 {team1.abbreviation} and {team2.abbreviation} tie!")
            else:
                self.standings[winner.id]["wins"] += 1
                self.standings[loser.id]["losses"] += 1
                winner_score = random.randint(17, 35)
                loser_score = random.randint(10, winner_score - 3)
                result = {
                    "type": "win",
                    "winner": winner.id,
                    "loser": loser.id,
                    "score": (winner_score, loser_score)
                }
                if debug:
                    print(f"🏆 {winner.abbreviation} defeats {loser.abbreviation} ({winner_score}-{loser_score})")
            results.append(result)

        if not debug:
            ties = sum(1 for r in results if r["type"] == "tie")
            zero_zero = sum(1 for r in results if r.get("score") == (0, 0))
            win_games = [r for r in results if r["type"] == "win"]
            print(f"Summary for Week {current_week}:")
            print(f"  Total games: {len(results)}")
            print(f"  Ties: {ties}")
            print(f"  0-0 games: {zero_zero}")
            if win_games:
                score_counts = {}
                for r in win_games:
                    score = r["score"]
                    score_counts[score] = score_counts.get(score, 0) + 1
                print("  Win/loss scores (count):")
                for score, count in sorted(score_counts.items(), key=lambda x: (-x[1], x[0])):
                    print(f"    {score[0]}-{score[1]}: {count}")

    def get_team_by_id(self, team_id):
        # Universal lookup by team ID
        return self.id_to_team.get(team_id)

    def to_dict(self):
        team_dicts = []
        for team in self.teams:
            if hasattr(team, "to_dict"):
                t = team.to_dict()
                t["conference"] = getattr(team, "conference", t.get("conference", None))
                t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
                t["abbreviation"] = getattr(team, "abbreviation", t.get("abbreviation", None))
                team_dicts.append(t)
            else:
                t = dict(team.__dict__)
                t["conference"] = getattr(team, "conference", t.get("conference", None))
                t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
                t["abbreviation"] = getattr(team, "abbreviation", t.get("abbreviation", None))
                team_dicts.append(t)
        # Debug print before returning/writing the league dict
        print("[LeagueManager.to_dict] Teams to be serialized:")
        for team in self.teams:
            abbr = getattr(team, "abbreviation", None)
            name = getattr(team, "team_name", None)
            conf = getattr(team, "conference", None)
=======
import os
import json
import random
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.utils.calendar import Calendar  # Update if calendar is moved elsewhere
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager  # Update if season_manager is moved elsewhere
ROOT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))

class LeagueManager:
    def __init__(self):
        self.teams = []
        self.free_agents = []
        self.draft_prospects = []  # <-- Add this line
        self.retired_players: list[dict] = []
        self.calendar = Calendar()
        # Standings now keyed by team ID
        self.standings = {}
        self.week_in_progress = False
        self.schedule = {}
        self.id_to_team = {}  # Use team.id as universal key
        self.id_to_abbr = {}  # For display
        self.abbr_to_id = {}  # For legacy conversion

    def _rebuild_team_maps(self):
        """Ensure all team mappings are up-to-date and complete."""
        self.id_to_team = {}
        self.id_to_abbr = {}
        self.abbr_to_id = {}
        for team in self.teams:
            if hasattr(team, "id"):
                self.id_to_team[team.id] = team
                self.id_to_abbr[team.id] = team.abbreviation
                self.abbr_to_id[team.abbreviation] = team.id

    def add_team(self, team):
        self.teams.append(team)
        # Ensure abbreviation and conference are set on the team object
        if not hasattr(team, "abbreviation") or team.abbreviation is None:
            raise ValueError("Team must have an abbreviation.")
        if not hasattr(team, "conference") or team.conference is None:
            team.conference = "Nova"
        self.standings[team.id] = {
            "wins": 0,
            "losses": 0,
            "ties": 0,
            "abbr": team.abbreviation,
            "conference": team.conference
        }
        self._rebuild_team_maps()

    def remove_team(self, team):
        if team in self.teams:
            self.teams.remove(team)
            if hasattr(team, "id") and team.id in self.standings:
                del self.standings[team.id]
            self._rebuild_team_maps()

    def advance_week(self):
        self.week_in_progress = True
        self.simulate_weekly_games()
        self.calendar.advance_week()
        self.week_in_progress = False

    def auto_fill_rosters(self, minimum_roster_size=53):
        for team in self.teams:
            while len(team.players) < minimum_roster_size and self.free_agents:
                player = self.free_agents.pop(0)
                team.add_player(player)

    def generate_schedule(self, weeks=14):
        # Machine-readable: use team IDs in schedule
        team_ids = [team.id for team in self.teams]
        num_teams = len(team_ids)
        if num_teams % 2 != 0:
            team_ids.append("BYE")

        total_weeks = weeks
        self.schedule = {}

        for week in range(1, total_weeks + 1):
            random.shuffle(team_ids)
            weekly_matchups = []
            for i in range(0, len(team_ids), 2):
                team1_id = team_ids[i]
                team2_id = team_ids[i + 1]
                if "BYE" not in (team1_id, team2_id):
                    weekly_matchups.append((team1_id, team2_id))
            self.schedule[week] = weekly_matchups

    def simulate_weekly_games(self, debug=False):
        current_week = self.calendar.current_week
        if current_week not in self.schedule:
            print(f"No games scheduled for Week {current_week}.")
            return

        print(f"📅 Simulating Week {current_week} games...")
        weekly_matchups = self.schedule[current_week]
        results = []
        for team1_id, team2_id in weekly_matchups:
            team1 = self.get_team_by_id(team1_id)
            team2 = self.get_team_by_id(team2_id)
            if not team1 or not team2:
                continue
            winner = random.choice([team1, team2])
            loser = team2 if winner == team1 else team1
            is_tie = random.random() < 0.02
            # Standings are keyed by team ID
            if is_tie:
                self.standings[team1.id]["ties"] += 1
                self.standings[team2.id]["ties"] += 1
                result = {
                    "type": "tie",
                    "team1": team1.id,
                    "team2": team2.id,
                    "score": (0, 0)
                }
                if debug:
                    print(f"🤝 {team1.abbreviation} and {team2.abbreviation} tie!")
            else:
                self.standings[winner.id]["wins"] += 1
                self.standings[loser.id]["losses"] += 1
                winner_score = random.randint(17, 35)
                loser_score = random.randint(10, winner_score - 3)
                result = {
                    "type": "win",
                    "winner": winner.id,
                    "loser": loser.id,
                    "score": (winner_score, loser_score)
                }
                if debug:
                    print(f"🏆 {winner.abbreviation} defeats {loser.abbreviation} ({winner_score}-{loser_score})")
            results.append(result)

        if not debug:
            ties = sum(1 for r in results if r["type"] == "tie")
            zero_zero = sum(1 for r in results if r.get("score") == (0, 0))
            win_games = [r for r in results if r["type"] == "win"]
            print(f"Summary for Week {current_week}:")
            print(f"  Total games: {len(results)}")
            print(f"  Ties: {ties}")
            print(f"  0-0 games: {zero_zero}")
            if win_games:
                score_counts = {}
                for r in win_games:
                    score = r["score"]
                    score_counts[score] = score_counts.get(score, 0) + 1
                print("  Win/loss scores (count):")
                for score, count in sorted(score_counts.items(), key=lambda x: (-x[1], x[0])):
                    print(f"    {score[0]}-{score[1]}: {count}")

    def get_team_by_id(self, team_id):
        # Universal lookup by team ID
        return self.id_to_team.get(team_id)

    def to_dict(self):
        team_dicts = []
        for team in self.teams:
            if hasattr(team, "to_dict"):
                t = team.to_dict()
                t["conference"] = getattr(team, "conference", t.get("conference", None))
                t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
                t["abbreviation"] = getattr(team, "abbreviation", t.get("abbreviation", None))
                team_dicts.append(t)
            else:
                t = dict(team.__dict__)
                t["conference"] = getattr(team, "conference", t.get("conference", None))
                t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
                t["abbreviation"] = getattr(team, "abbreviation", t.get("abbreviation", None))
                team_dicts.append(t)
        # Debug print before returning/writing the league dict
        print("[LeagueManager.to_dict] Teams to be serialized:")
        for team in self.teams:
            abbr = getattr(team, "abbreviation", None)
            name = getattr(team, "team_name", None)
            conf = getattr(team, "conference", None)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        return {
            "teams": team_dicts,
            "free_agents": [player.to_dict() for player in self.free_agents],
            "draft_prospects": [player.to_dict() for player in self.draft_prospects],  # <-- Add this line
<<<<<<< HEAD
=======
            "retired_players": self.retired_players,
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
            "calendar": self.calendar.serialize(),
            "standings": self.standings,
            "schedule": self.schedule
        }
<<<<<<< HEAD

    @staticmethod
    def from_dict(data):
        league = LeagueManager()
        # Debug print: show each team dict before creating Team objects
        for team_dict in data.get("teams", []):
            league.teams = []
        unknown_conference_found = False
        for team_data in data.get("teams", []):
            team_kwargs = dict(team_data)
            if "conference" not in team_kwargs or not team_kwargs["conference"]:
                team_kwargs["conference"] = "Nova"
            if "abbreviation" not in team_kwargs or not team_kwargs["abbreviation"]:
                raise ValueError("All teams must have an abbreviation.")
            team = Team.from_dict(team_kwargs)
            # Ensure abbreviation and conference are set on the team object
            team.abbreviation = team_kwargs["abbreviation"]
            team.conference = team_kwargs["conference"]
            league.teams.append(team)
=======

    @staticmethod
    def from_dict(data):
        league = LeagueManager()
        # Debug print: show each team dict before creating Team objects
        for team_dict in data.get("teams", []):
            league.teams = []
        unknown_conference_found = False
        for team_data in data.get("teams", []):
            team_kwargs = dict(team_data)
            if "conference" not in team_kwargs or not team_kwargs["conference"]:
                team_kwargs["conference"] = "Nova"
            if "abbreviation" not in team_kwargs or not team_kwargs["abbreviation"]:
                raise ValueError("All teams must have an abbreviation.")
            team = Team.from_dict(team_kwargs)
            # Ensure abbreviation and conference are set on the team object
            team.abbreviation = team_kwargs["abbreviation"]
            team.conference = team_kwargs["conference"]
            league.teams.append(team)
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        league._rebuild_team_maps()
        for team in league.teams:
            if getattr(team, "conference", None) == "Unknown":
                unknown_conference_found = True
        # Debug print: show each created Team object
        for team in league.teams:
<<<<<<< HEAD
            league.free_agents = [Player.from_dict(p) for p in data.get("free_agents", [])]
        # Add draft prospects
        league.draft_prospects = [Player.from_dict(p) for p in data.get("draft_prospects", [])]  # <-- Add this line
        # Standings: convert any abbreviation keys to IDs (legacy support)
        standings = data.get("standings", {})
        new_standings = {}
        for k, v in standings.items():
            team_obj = league.id_to_team.get(k)
            if not team_obj and k in league.abbr_to_id:
                k = league.abbr_to_id[k]
                team_obj = league.id_to_team.get(k)
            if team_obj:
                abbr = getattr(team_obj, "abbreviation", None)
                conf = getattr(team_obj, "conference", None)
                v["abbr"] = abbr
                v["conference"] = conf
                new_standings[k] = v
            else:
                new_standings[k] = v
        league.standings = new_standings
        # Schedule: convert any abbreviation keys to IDs (legacy support)
        schedule = data.get("schedule", {})
        new_schedule = {}
        for week, games in schedule.items():
            new_games = []
            for matchup in games:
                t1, t2 = matchup
                if t1 in league.abbr_to_id:
                    t1 = league.abbr_to_id[t1]
                if t2 in league.abbr_to_id:
                    t2 = league.abbr_to_id[t2]
                new_games.append((t1, t2))
            new_schedule[week] = new_games
        league.schedule = new_schedule
        if "calendar" in data:
            league.calendar = Calendar.deserialize(data["calendar"])
        if unknown_conference_found:
            print("[!] WARNING: One or more teams have conference == 'Unknown'. Please check your league file.")
        return league

    def __repr__(self):
        return f"LeagueManager | Teams: {len(self.teams)} | Free Agents: {len(self.free_agents)}"

def load_league_from_file(save_name):
    path = os.path.join(ROOT_DIR, "data", "saves", save_name, "league.json")
    if not os.path.exists(path):
        raise FileNotFoundError(f"No league save found at: {path}")
    try:
        with open(path, "r") as f:
            data = json.load(f)
        # Load teams from JSON
        teams = []
        for team_data in data.get("teams", []):
            team = Team.from_dict(team_data)
            teams.append(team)
        # Fill rosters with dummy players if needed
        from gridiron_gm_pkg.simulation.systems.game.season_manager import fill_team_rosters_with_dummy_players
        fill_team_rosters_with_dummy_players(teams)
        # Now create the LeagueManager and assign teams
        league = LeagueManager()
        league.teams = teams
        league._rebuild_team_maps()
        # Continue with the rest of the LeagueManager setup
        league.free_agents = [Player.from_dict(p) for p in data.get("free_agents", [])]
        # Standings: convert any abbreviation keys to IDs (legacy support)
        standings = data.get("standings", {})
        new_standings = {}
        for k, v in standings.items():
            team_obj = league.id_to_team.get(k)
            if not team_obj and k in league.abbr_to_id:
                k = league.abbr_to_id[k]
                team_obj = league.id_to_team.get(k)
            if team_obj:
                abbr = getattr(team_obj, "abbreviation", None)
                conf = getattr(team_obj, "conference", None)
                v["abbr"] = abbr
                v["conference"] = conf
                new_standings[k] = v
            else:
                new_standings[k] = v
        league.standings = new_standings
        # Schedule: convert any abbreviation keys to IDs (legacy support)
        schedule = data.get("schedule", {})
        new_schedule = {}
        for week, games in schedule.items():
            new_games = []
            for matchup in games:
                t1, t2 = matchup
                if t1 in league.abbr_to_id:
                    t1 = league.abbr_to_id[t1]
                if t2 in league.abbr_to_id:
                    t2 = league.abbr_to_id[t2]
                new_games.append((t1, t2))
            new_schedule[week] = new_games
        league.schedule = new_schedule
        if "calendar" in data:
            league.calendar = Calendar.deserialize(data["calendar"])
        return league
    except Exception as e:
=======
            pass
        league.free_agents = [Player.from_dict(p) for p in data.get("free_agents", [])]
        league.draft_prospects = [Player.from_dict(p) for p in data.get("draft_prospects", [])]
        league.retired_players = data.get("retired_players", [])
        # Standings: convert any abbreviation keys to IDs (legacy support)
        standings = data.get("standings", {})
        new_standings = {}
        for k, v in standings.items():
            team_obj = league.id_to_team.get(k)
            if not team_obj and k in league.abbr_to_id:
                k = league.abbr_to_id[k]
                team_obj = league.id_to_team.get(k)
            if team_obj:
                abbr = getattr(team_obj, "abbreviation", None)
                conf = getattr(team_obj, "conference", None)
                v["abbr"] = abbr
                v["conference"] = conf
                new_standings[k] = v
            else:
                new_standings[k] = v
        league.standings = new_standings
        # Schedule: convert any abbreviation keys to IDs (legacy support)
        schedule = data.get("schedule", {})
        new_schedule = {}
        for week, games in schedule.items():
            new_games = []
            for matchup in games:
                t1, t2 = matchup
                if t1 in league.abbr_to_id:
                    t1 = league.abbr_to_id[t1]
                if t2 in league.abbr_to_id:
                    t2 = league.abbr_to_id[t2]
                new_games.append((t1, t2))
            new_schedule[week] = new_games
        league.schedule = new_schedule
        if "calendar" in data:
            league.calendar = Calendar.deserialize(data["calendar"])
        if unknown_conference_found:
            print("[!] WARNING: One or more teams have conference == 'Unknown'. Please check your league file.")
        return league

    def __repr__(self):
        return f"LeagueManager | Teams: {len(self.teams)} | Free Agents: {len(self.free_agents)}"

def load_league_from_file(save_name):
    path = os.path.join(ROOT_DIR, "data", "saves", save_name, "league.json")
    if not os.path.exists(path):
        raise FileNotFoundError(f"No league save found at: {path}")
    try:
        with open(path, "r") as f:
            data = json.load(f)
        # Load teams from JSON
        teams = []
        for team_data in data.get("teams", []):
            team = Team.from_dict(team_data)
            teams.append(team)
        # Fill rosters with dummy players if needed
        from gridiron_gm_pkg.simulation.systems.game.season_manager import fill_team_rosters_with_dummy_players
        fill_team_rosters_with_dummy_players(teams)
        # Now create the LeagueManager and assign teams
        league = LeagueManager()
        league.teams = teams
        league._rebuild_team_maps()
        # Continue with the rest of the LeagueManager setup
        league.free_agents = [Player.from_dict(p) for p in data.get("free_agents", [])]
        league.draft_prospects = [Player.from_dict(p) for p in data.get("draft_prospects", [])]
        league.retired_players = data.get("retired_players", [])
        # Standings: convert any abbreviation keys to IDs (legacy support)
        standings = data.get("standings", {})
        new_standings = {}
        for k, v in standings.items():
            team_obj = league.id_to_team.get(k)
            if not team_obj and k in league.abbr_to_id:
                k = league.abbr_to_id[k]
                team_obj = league.id_to_team.get(k)
            if team_obj:
                abbr = getattr(team_obj, "abbreviation", None)
                conf = getattr(team_obj, "conference", None)
                v["abbr"] = abbr
                v["conference"] = conf
                new_standings[k] = v
            else:
                new_standings[k] = v
        league.standings = new_standings
        # Schedule: convert any abbreviation keys to IDs (legacy support)
        schedule = data.get("schedule", {})
        new_schedule = {}
        for week, games in schedule.items():
            new_games = []
            for matchup in games:
                t1, t2 = matchup
                if t1 in league.abbr_to_id:
                    t1 = league.abbr_to_id[t1]
                if t2 in league.abbr_to_id:
                    t2 = league.abbr_to_id[t2]
                new_games.append((t1, t2))
            new_schedule[week] = new_games
        league.schedule = new_schedule
        if "calendar" in data:
            league.calendar = Calendar.deserialize(data["calendar"])
        return league
    except Exception as e:
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        raise RuntimeError(f"Failed to load league from {path}: {e}")