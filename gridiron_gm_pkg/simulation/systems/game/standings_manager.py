import os
import json
from pathlib import Path

def update_team_records(home_team, away_team, home_score, away_score):
    """
    Updates the team_record dict for both teams after a game.
    Increments wins, losses, ties, and points_for (PF), and tracks division/conference records.
    """
    # Ensure team_record dict exists
    for team in [home_team, away_team]:
        if not hasattr(team, "team_record"):
            team.team_record = {}
        team.team_record.setdefault("wins", 0)
        team.team_record.setdefault("losses", 0)
        team.team_record.setdefault("ties", 0)
        team.team_record.setdefault("PF", 0)
        team.team_record.setdefault("PA", 0)
        team.team_record.setdefault("div_wins", 0)
        team.team_record.setdefault("div_losses", 0)
        team.team_record.setdefault("div_ties", 0)
        team.team_record.setdefault("conf_wins", 0)
        team.team_record.setdefault("conf_losses", 0)
        team.team_record.setdefault("conf_ties", 0)
        team.team_record.setdefault("victories", [])
        team.team_record.setdefault("opponents", [])
        team.team_record.setdefault("net_touchdowns", 0)

    # Determine division/conference
    same_div = getattr(home_team, "division", None) == getattr(away_team, "division", None)
    same_conf = getattr(home_team, "conference", None) == getattr(away_team, "conference", None)

    # Track touchdowns if available
    home_td = getattr(home_team, "last_game_touchdowns", 0)
    away_td = getattr(away_team, "last_game_touchdowns", 0)
    home_team.team_record["net_touchdowns"] += home_td
    away_team.team_record["net_touchdowns"] += away_td

    # Track opponents
    if away_team.id not in home_team.team_record["opponents"]:
        home_team.team_record["opponents"].append(away_team.id)
    if home_team.id not in away_team.team_record["opponents"]:
        away_team.team_record["opponents"].append(home_team.id)

    # Update PF/PA
    home_team.team_record["PF"] += home_score
    home_team.team_record["PA"] += away_score
    away_team.team_record["PF"] += away_score
    away_team.team_record["PA"] += home_score

    # Update W/L/T
    if home_score > away_score:
        home_team.team_record["wins"] += 1
        away_team.team_record["losses"] += 1
        home_team.team_record["victories"].append(away_team.id)
    elif away_score > home_score:
        away_team.team_record["wins"] += 1
        home_team.team_record["losses"] += 1
        away_team.team_record["victories"].append(home_team.id)
    else:
        home_team.team_record["ties"] += 1
        away_team.team_record["ties"] += 1

    # Division record
    if same_div:
        if home_score > away_score:
            home_team.team_record["div_wins"] += 1
            away_team.team_record["div_losses"] += 1
        elif away_score > home_score:
            away_team.team_record["div_wins"] += 1
            home_team.team_record["div_losses"] += 1
        else:
            home_team.team_record["div_ties"] += 1
            away_team.team_record["div_ties"] += 1

    # Conference record
    if same_conf:
        if home_score > away_score:
            home_team.team_record["conf_wins"] += 1
            away_team.team_record["conf_losses"] += 1
        elif away_score > home_score:
            away_team.team_record["conf_wins"] += 1
            home_team.team_record["conf_losses"] += 1
        else:
            home_team.team_record["conf_ties"] += 1
            away_team.team_record["conf_ties"] += 1

class StandingsManager:
    def __init__(self, calendar, league, save_name="test_league", results_by_week=None):
        self.calendar = calendar
        self.league = league
        self.save_name = save_name
        self.results_by_week = results_by_week or {}

        # --- Universal team ID mapping ---
        self.id_to_team = {team.id: team for team in self.league.teams}
        self.id_to_abbr = {team.id: team.abbreviation for team in self.league.teams}
        self.abbr_to_id = {team.abbreviation: team.id for team in self.league.teams}

        for team in self.league.teams:
            abbr = getattr(team, "abbreviation", None)
            conf = getattr(team, "conference", None)
            if not hasattr(team, "conference") or not team.conference:
                team.conference = "Nova"
        self.standings = self.load_standings()
        if not isinstance(self.standings, dict):
            self.standings = {}
        self.load_playoff_seed_map(calendar.current_year, save_name)

    def get_standings_path(self):
        year = self.calendar.current_year
        base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / self.save_name
        return str(base_path / f"standings_{year}.json")

    def load_standings(self):
        path = self.get_standings_path()
        if os.path.exists(path):
            try:
                with open(path, "r") as f:
                    loaded = json.load(f)
                    if isinstance(loaded, dict):
                        # Convert any abbreviation keys to IDs (for legacy files)
                        new_loaded = {}
                        for tid, rec in loaded.items():
                            # If tid is not a known ID but is an abbreviation, convert
                            if tid not in self.id_to_team and tid in self.abbr_to_id:
                                real_id = self.abbr_to_id[tid]
                                tid = real_id
                            abbr = rec.get("abbr", self.id_to_abbr.get(tid, tid))
                            conf = rec.get("conference")
                            if conf is None or conf == "Unknown":
                                pass
                            rec["abbr"] = abbr
                            new_loaded[tid] = rec
                        return new_loaded
            except Exception:
                pass

        # Fallback: build fresh standings using team IDs as keys
        standings = {}
        for team in self.league.teams:
            team_id = team.id
            abbr = team.abbreviation
            conference = getattr(team, "conference", "Unknown")
            division = getattr(team, "division", "Unknown")
            if conference is None or conference == "Unknown":
                pass
            standings[team_id] = {
                "abbr": abbr,
                "conference": conference,
                "division": division,
                "W": 0, "L": 0, "T": 0,
                "PF": 0, "PA": 0,
                "div_wins": 0, "div_losses": 0, "div_ties": 0,
                "conf_wins": 0, "conf_losses": 0, "conf_ties": 0,
                "victories": [],
                "opponents": [],
                "net_touchdowns": 0
            }
        return standings

    def save_standings(self):
        path = self.get_standings_path()
        os.makedirs(os.path.dirname(path), exist_ok=True)
        with open(path, "w") as f:
            json.dump(self.standings, f, indent=2)

    def update_from_result(self, result):
        home_id = result["home"]
        away_id = result["away"]
        home_id = self.seed_alias_map.get(home_id, home_id)
        away_id = self.seed_alias_map.get(away_id, away_id)
        hs = result["home_score"]
        as_ = result["away_score"]

        if home_id not in self.standings or away_id not in self.standings:
            return

        self.standings[home_id]["PF"] += hs
        self.standings[home_id]["PA"] += as_
        self.standings[away_id]["PF"] += as_
        self.standings[away_id]["PA"] += hs

        if hs > as_:
            self.standings[home_id]["W"] += 1
            self.standings[away_id]["L"] += 1
            winner, loser = home_id, away_id
        elif hs < as_:
            self.standings[away_id]["W"] += 1
            self.standings[home_id]["L"] += 1
            winner, loser = away_id, home_id
        else:
            self.standings[home_id]["T"] += 1
            self.standings[away_id]["T"] += 1
            winner, loser = None, None

        # Track division/conference records and victories/opponents
        home_team = self.id_to_team.get(home_id)
        away_team = self.id_to_team.get(away_id)
        if home_team and away_team:
            update_team_records(home_team, away_team, hs, as_)

    # --- NFL Tiebreaker Helpers ---

    def get_division_record(self, team_id):
        rec = self.standings[team_id]
        games = rec["div_wins"] + rec["div_losses"] + rec["div_ties"]
        return (rec["div_wins"] + 0.5 * rec["div_ties"]) / games if games > 0 else 0.0

    def get_conference_record(self, team_id):
        rec = self.standings[team_id]
        games = rec["conf_wins"] + rec["conf_losses"] + rec["conf_ties"]
        return (rec["conf_wins"] + 0.5 * rec["conf_ties"]) / games if games > 0 else 0.0

    def get_common_games_record(self, team_id, tied_team_ids):
        # Find all common opponents
        common = set.intersection(*(set(self.standings[t]["opponents"]) for t in tied_team_ids)) if tied_team_ids else set()
        if not common:
            return 0.0
        wins = losses = ties = 0
        for opp_id in common:
            # Find all games vs this opponent
            for week_results in self.results_by_week.values():
                for game in week_results:
                    if (game.get("home") == team_id and game.get("away") == opp_id):
                        if game.get("home_score", 0) > game.get("away_score", 0):
                            wins += 1
                        elif game.get("home_score", 0) < game.get("away_score", 0):
                            losses += 1
                        else:
                            ties += 1
                    elif (game.get("away") == team_id and game.get("home") == opp_id):
                        if game.get("away_score", 0) > game.get("home_score", 0):
                            wins += 1
                        elif game.get("away_score", 0) < game.get("home_score", 0):
                            losses += 1
                        else:
                            ties += 1
        games = wins + losses + ties
        return (wins + 0.5 * ties) / games if games > 0 else 0.0

    def get_strength_of_victory(self, team_id):
        victories = self.standings[team_id]["victories"]
        if not victories:
            return 0.0
        total = 0.0
        for opp_id in victories:
            opp = self.standings.get(opp_id)
            if opp:
                games = opp["W"] + opp["L"] + opp["T"]
                wp = (opp["W"] + 0.5 * opp["T"]) / games if games > 0 else 0.0
                total += wp
        return total / len(victories) if victories else 0.0

    def get_strength_of_schedule(self, team_id):
        opponents = self.standings[team_id]["opponents"]
        if not opponents:
            return 0.0
        total = 0.0
        for opp_id in opponents:
            opp = self.standings.get(opp_id)
            if opp:
                games = opp["W"] + opp["L"] + opp["T"]
                wp = (opp["W"] + 0.5 * opp["T"]) / games if games > 0 else 0.0
                total += wp
        return total / len(opponents) if opponents else 0.0

    def get_combined_conf_rank(self, team_id):
        conf = self.standings[team_id]["conference"]
        conf_teams = [tid for tid, rec in self.standings.items() if rec["conference"] == conf]
        pf_rank = sorted(conf_teams, key=lambda t: self.standings[t]["PF"], reverse=True).index(team_id) + 1
        pa_rank = sorted(conf_teams, key=lambda t: self.standings[t]["PA"]).index(team_id) + 1
        return pf_rank + pa_rank

    def get_combined_all_rank(self, team_id):
        all_teams = list(self.standings.keys())
        pf_rank = sorted(all_teams, key=lambda t: self.standings[t]["PF"], reverse=True).index(team_id) + 1
        pa_rank = sorted(all_teams, key=lambda t: self.standings[t]["PA"]).index(team_id) + 1
        return pf_rank + pa_rank

    def get_net_points_common(self, team_id, tied_team_ids):
        common = set.intersection(*(set(self.standings[t]["opponents"]) for t in tied_team_ids)) if tied_team_ids else set()
        if not common:
            return 0
        net = 0
        for opp_id in common:
            for week_results in self.results_by_week.values():
                for game in week_results:
                    if (game.get("home") == team_id and game.get("away") == opp_id):
                        net += game.get("home_score", 0) - game.get("away_score", 0)
                    elif (game.get("away") == team_id and game.get("home") == opp_id):
                        net += game.get("away_score", 0) - game.get("home_score", 0)
        return net

    def get_net_points_all(self, team_id):
        rec = self.standings[team_id]
        return rec["PF"] - rec["PA"]

    def get_net_td_all(self, team_id):
        return self.standings[team_id].get("net_touchdowns", 0)

    def get_sorted_standings_by_conference(self):
        """
        Returns a dict: {"Nova": [team1, ...], "Atlas": [team2, ...]}
        Each team is a dict or object with id, abbreviation, conference, W, L, T, etc.
        """
        grouped = {"Nova": [], "Atlas": []}
        for team_id, rec in self.standings.items():
            team_obj = self.id_to_team.get(team_id)
            conf = getattr(team_obj, "conference", None) if team_obj else rec.get("conference")
            abbr = rec.get("abbr", self.id_to_abbr.get(team_id, team_id))
            team_info = {
                "id": team_id,
                "abbr": abbr,
                "conference": conf,
                "division": rec.get("division", None),
                "W": rec.get("W", 0),
                "L": rec.get("L", 0),
                "T": rec.get("T", 0),
                "PF": rec.get("PF", 0),
                "PA": rec.get("PA", 0),
                "div_wins": rec.get("div_wins", 0),
                "div_losses": rec.get("div_losses", 0),
                "div_ties": rec.get("div_ties", 0),
                "conf_wins": rec.get("conf_wins", 0),
                "conf_losses": rec.get("conf_losses", 0),
                "conf_ties": rec.get("conf_ties", 0),
                "victories": rec.get("victories", []),
                "opponents": rec.get("opponents", []),
                "net_touchdowns": rec.get("net_touchdowns", 0)
            }
            if conf == "Nova":
                grouped["Nova"].append(team_info)
            elif conf == "Atlas":
                grouped["Atlas"].append(team_info)
        for conf in ["Nova", "Atlas"]:
            grouped[conf] = sorted(
                grouped[conf],
                key=lambda t: (-t["W"], t["L"], -t["T"])
            )
        return grouped

    def load_playoff_seed_map(self, year, save_name="test_league"):
        base_path = Path(__file__).resolve().parents[3] / "data" / "saves" / save_name / "playoffs"
        bracket_path = base_path / f"playoff_bracket_{year}.json"
        if not os.path.exists(bracket_path):
            self.seed_alias_map = {}
            return
        with open(bracket_path, "r") as f:
            bracket = json.load(f)
        self.seed_alias_map = {}

    def get_full_standings(self):
        return self.standings

    def reset_for_new_season(self):
        """
        Resets all standings and records for a new season.
        """
        self.standings = {}
        self.results_by_week = {}
