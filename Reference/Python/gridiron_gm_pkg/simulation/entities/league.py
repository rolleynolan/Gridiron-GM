import os
import json
import random
import datetime
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.career.decision_item import DecisionItem
from gridiron_gm_pkg.simulation.career.gm_profile import GMProfile
from gridiron_gm_pkg.simulation.utils.calendar import Calendar  # Update if calendar is moved elsewhere
from gridiron_gm_pkg.simulation.systems.game.season_manager import SeasonManager  # Update if season_manager is moved elsewhere
ROOT_DIR = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))

class LeagueManager:
    def __init__(self):
        self.teams = []
        self.free_agents = []
        self.draft_prospects = []  # <-- Add this line
        self.calendar = Calendar()
        self.user_team_id = None
        self.controlled_team_id = None
        self.user_gm = None
        self.base_seed = None
        self.sim_seed = 42  # Default seed for reproducible RNG
        self.rng_state = {}
        self.game_clock = None
        self.event_queue = None
        self.inboxes = {}
        self.decisions = []
        self.last_agenda_date = None
        self.simulated_games = set()
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
            while len(team.roster) < minimum_roster_size and self.free_agents:
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
        return {
            "teams": team_dicts,
            "free_agents": [player.to_dict() for player in self.free_agents],
            "draft_prospects": [player.to_dict() for player in self.draft_prospects],  # <-- Add this line
            "calendar": self.calendar.serialize(),
            "standings": self.standings,
            "schedule": self.schedule,
            "results_by_week": getattr(self, "results_by_week", {}),
            "controlled_team_id": self.controlled_team_id,
            "user_gm": self.user_gm.to_dict() if hasattr(self.user_gm, "to_dict") else self.user_gm,
            "time_engine": {
                "user_team_id": self.user_team_id,
                "controlled_team_id": self.controlled_team_id,
                "base_seed": self.base_seed,
                "rng_state": self.rng_state if isinstance(self.rng_state, dict) else {},
                "clock": self._serialize_clock(),
                "event_queue": self._serialize_queue(),
                "inboxes": self._serialize_inboxes(),
                "decisions": self._serialize_decisions(),
                "last_agenda_date": self._serialize_date(self.last_agenda_date),
                "simulated_games": sorted(self.simulated_games) if isinstance(self.simulated_games, set) else list(self.simulated_games or []),
                "last_weekly_decay": getattr(self, "last_weekly_decay", None),
            },
        }

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
        league._rebuild_team_maps()
        for team in league.teams:
            if getattr(team, "conference", None) == "Unknown":
                unknown_conference_found = True
        # Debug print: show each created Team object
        for team in league.teams:
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
        league.results_by_week = data.get("results_by_week", {})
        if "calendar" in data:
            league.calendar = Calendar.deserialize(data["calendar"])
        time_engine = data.get("time_engine", {})
        controlled_team_id = (
            data.get("controlled_team_id")
            or time_engine.get("controlled_team_id")
            or data.get("selected_team_id")
            or time_engine.get("selected_team_id")
            or time_engine.get("user_team_id")
        )
        league.controlled_team_id = controlled_team_id
        league.user_team_id = time_engine.get("user_team_id") or controlled_team_id
        legacy_gm = data.get("gm")
        if isinstance(legacy_gm, dict) and "name" not in legacy_gm:
            legacy_gm = {}
        if isinstance(data.get("user_gm"), dict):
            league.user_gm = GMProfile.from_dict(data.get("user_gm"))
        elif isinstance(legacy_gm, dict):
            league.user_gm = GMProfile.from_dict(legacy_gm)
        elif data.get("gm_name"):
            league.user_gm = GMProfile.from_dict(
                {
                    "name": data.get("gm_name"),
                    "current_team_id": league.user_team_id,
                    "career_start_year": getattr(league.calendar, "current_year", 0),
                }
            )
        else:
            league.user_gm = None
        league.base_seed = time_engine.get("base_seed")
        league.rng_state = time_engine.get("rng_state", {})
        league.game_clock = time_engine.get("clock")
        league.event_queue = time_engine.get("event_queue")
        league.inboxes = time_engine.get("inboxes", {})
        raw_decisions = time_engine.get("decisions", data.get("decisions", []))
        if not isinstance(raw_decisions, list):
            raw_decisions = []
        league.decisions = [
            item if isinstance(item, DecisionItem) else DecisionItem.from_dict(item)
            for item in raw_decisions
        ]
        league.last_agenda_date = time_engine.get("last_agenda_date")
        league.last_weekly_decay = time_engine.get("last_weekly_decay")
        simulated = time_engine.get("simulated_games", [])
        league.simulated_games = set(simulated) if isinstance(simulated, list) else set()
        if unknown_conference_found:
            print("[!] WARNING: One or more teams have conference == 'Unknown'. Please check your league file.")
        return league

    def __repr__(self):
        return f"LeagueManager | Teams: {len(self.teams)} | Free Agents: {len(self.free_agents)}"

    def _serialize_clock(self):
        clock = getattr(self, "game_clock", None)
        if clock is None:
            return None
        if hasattr(clock, "serialize"):
            return clock.serialize()
        if isinstance(clock, dict):
            return clock
        return None

    def _serialize_queue(self):
        queue = getattr(self, "event_queue", None)
        if queue is None:
            return None
        if hasattr(queue, "serialize"):
            return queue.serialize()
        if isinstance(queue, dict):
            return queue
        return None

    def _serialize_inboxes(self):
        inboxes = getattr(self, "inboxes", None) or {}
        serialized = {}
        for team_id, messages in inboxes.items():
            serialized[team_id] = [
                msg.serialize() if hasattr(msg, "serialize") else msg for msg in messages
            ]
        return serialized

    def _serialize_decisions(self):
        decisions = getattr(self, "decisions", None) or []
        return [
            decision.to_dict() if hasattr(decision, "to_dict") else decision
            for decision in decisions
        ]

    @staticmethod
    def _serialize_date(value):
        if isinstance(value, datetime.date):
            return value.isoformat()
        return value

def load_league_from_file(save_name):
    path = os.path.join(ROOT_DIR, "data", "saves", save_name, "league.json")
    if not os.path.exists(path):
        raise FileNotFoundError(f"No league save found at: {path}")
    try:
        from gridiron_gm_pkg.simulation.persistence.savegame import load_league
        return load_league(path)
    except Exception as e:
        raise RuntimeError(f"Failed to load league from {path}: {e}")
