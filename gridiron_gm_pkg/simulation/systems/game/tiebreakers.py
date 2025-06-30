<<<<<<< HEAD
import json
import os

class StandingsManager:
    def __init__(self, calendar, league, save_name="test_league", results_by_week=None):
        self.calendar = calendar
        self.league = league
        self.save_name = save_name
        self.results_by_week = results_by_week
        self.standings = self.load_standings()
        self.load_playoff_seed_map(calendar.current_year, save_name)

    def get_standings_path(self):
        year = self.calendar.current_year
        return f"saves/{self.save_name}/standings_{year}.json"

    def load_standings(self):
        path = self.get_standings_path()
        if os.path.exists(path):
            with open(path, "r") as f:
                return json.load(f)
        standings = {
            team["abbreviation"]: {
                "W": 0,
                "L": 0,
                "T": 0,
                "PF": 0,
                "PA": 0,
                "opponents": {},
                "conference": team.get("conference", "Unknown"),
                "division": team.get("division", "Unknown"),
            }
            for team in self.league
        }
        print("Loaded standings teams:", list(standings.keys()))
        return standings

    def get_sorted_standings_by_conference(self):
        conferences = {"Nova": [], "Atlas": []}
        for team, data in self.standings.items():
            conf = data.get("conference")
            if conf in conferences:
                conferences[conf].append((team, data))

        def sort_key(item):
            team, record = item
            total_games = record["W"] + record["L"] + record["T"]
            win_pct = record["W"] / total_games if total_games > 0 else 0
            return win_pct

        team_dict_by_abbr = {t["abbreviation"]: t for t in self.league}

        for conf in conferences:
            tied = conferences[conf]
            tied.sort(key=sort_key, reverse=True)
            team_names = [team for team, _ in tied]
            ordered_abbrs = self.break_ties(team_names)
            conferences[conf] = [team_dict_by_abbr[abbr] for abbr in ordered_abbrs if abbr in team_dict_by_abbr]

        return conferences

    def save_standings(self):
        path = self.get_standings_path()
        with open(path, "w") as f:
            json.dump(self.standings, f, indent=2)

    def update_from_result(self, result):
        home = result["home"]
        away = result["away"]
        home = self.seed_alias_map.get(home, home)
        away = self.seed_alias_map.get(away, away)
        hs = result["home_score"]
        as_ = result["away_score"]

        if home not in self.standings or away not in self.standings:
            print(f"[ERROR] One or both teams not in standings: {home}, {away}")
            return

        self.standings[home]["PF"] += hs
        self.standings[home]["PA"] += as_
        self.standings[away]["PF"] += as_
        self.standings[away]["PA"] += hs

        if hs > as_:
            self.standings[home]["W"] += 1
            self.standings[away]["L"] += 1
            winner, loser = home, away
        elif hs < as_:
            self.standings[away]["W"] += 1
            self.standings[home]["L"] += 1
            winner, loser = away, home
        else:
            self.standings[home]["T"] += 1
            self.standings[away]["T"] += 1
            winner, loser = None, None

        self.standings[home].setdefault("opponents", {})
        self.standings[away].setdefault("opponents", {})
        self.standings[home]["opponents"].setdefault(away, {"W": 0, "L": 0, "T": 0})
        self.standings[away]["opponents"].setdefault(home, {"W": 0, "L": 0, "T": 0})

        if winner == home:
            self.standings[home]["opponents"][away]["W"] += 1
            self.standings[away]["opponents"][home]["L"] += 1
        elif winner == away:
            self.standings[away]["opponents"][home]["W"] += 1
            self.standings[home]["opponents"][away]["L"] += 1
        elif winner is None:
            self.standings[home]["opponents"][away]["T"] += 1
            self.standings[away]["opponents"][home]["T"] += 1

    def summarize(self):
        return {
            team: f"{record['W']}-{record['L']}-{record['T']}"
            for team, record in self.standings.items()
        }

    def break_ties(self, tied_teams):
        if len(tied_teams) <= 1:
            return tied_teams

        # Step 1: Head-to-head win count
        head_to_head = {team: 0 for team in tied_teams}
        for week_results in (self.results_by_week or {}).values():
            for result in week_results:
                home, away = result["home"], result["away"]
                winner = None
                if result["home_score"] > result["away_score"]:
                    winner = home
                elif result["home_score"] < result["away_score"]:
                    winner = away

                if home in tied_teams and away in tied_teams and winner in tied_teams:
                    head_to_head[winner] += 1

        max_hth = max(head_to_head.values())
        tied_teams = [team for team in tied_teams if head_to_head[team] == max_hth]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 2: Conference record
        def conf_wins(team):
            total = 0
            for opp, rec in self.standings[team]["opponents"].items():
                opp_conf = self.standings.get(opp, {}).get("conference")
                if opp_conf == self.standings[team]["conference"]:
                    total += rec["W"]
            return total
        max_conf = max(conf_wins(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if conf_wins(t) == max_conf]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 3: Division record
        def div_wins(team):
            total = 0
            for opp, rec in self.standings[team]["opponents"].items():
                opp_div = self.standings.get(opp, {}).get("division")
                if opp_div == self.standings[team]["division"]:
                    total += rec["W"]
            return total
        max_div = max(div_wins(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if div_wins(t) == max_div]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 4: Points For
        max_pf = max(self.standings[t]["PF"] for t in tied_teams)
        tied_teams = [t for t in tied_teams if self.standings[t]["PF"] == max_pf]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 5: Strength of victory (average points scored in wins)
        def strength_of_victory(team):
            wins = self.standings[team]["W"]
            if wins == 0:
                return 0
            return self.standings[team]["PF"] / wins
        max_sov = max(strength_of_victory(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if strength_of_victory(t) == max_sov]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 6: Strength of schedule (average opponent win %)
        def sos(team):
            win_percents = []
            for opp in self.standings[team]["opponents"]:
                opp_record = self.standings.get(opp)
                if opp_record:
                    total_games = opp_record["W"] + opp_record["L"] + opp_record["T"]
                    if total_games > 0:
                        wp = opp_record["W"] / total_games
                        win_percents.append(wp)
            return sum(win_percents) / len(win_percents) if win_percents else 0
        max_sos = max(sos(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if sos(t) == max_sos]
        return tied_teams

    def load_playoff_seed_map(self, year, save_name="test_league"):
        bracket_path = f"saves/{save_name}/playoffs/playoff_bracket_{year}.json"
        if not os.path.exists(bracket_path):
            self.seed_alias_map = {}
            return
        with open(bracket_path, "r") as f:
            bracket = json.load(f)
        self.seed_alias_map = {
            f"{i}A": bracket["Atlas"]["seeds"][i - 1]["team"]
            for i in range(1, 8)
        }
        self.seed_alias_map.update({
            f"{i}N": bracket["Nova"]["seeds"][i - 1]["team"]
            for i in range(1, 8)
        })

    def get_full_standings(self):
        return self.standings
=======
import json
import os

class StandingsManager:
    def __init__(self, calendar, league, save_name="test_league", results_by_week=None):
        self.calendar = calendar
        self.league = league
        self.save_name = save_name
        self.results_by_week = results_by_week
        self.standings = self.load_standings()
        self.load_playoff_seed_map(calendar.current_year, save_name)

    def get_standings_path(self):
        year = self.calendar.current_year
        return f"saves/{self.save_name}/standings_{year}.json"

    def load_standings(self):
        path = self.get_standings_path()
        if os.path.exists(path):
            with open(path, "r") as f:
                return json.load(f)
        standings = {
            team["abbreviation"]: {
                "W": 0,
                "L": 0,
                "T": 0,
                "PF": 0,
                "PA": 0,
                "opponents": {},
                "conference": team.get("conference", "Unknown"),
                "division": team.get("division", "Unknown"),
            }
            for team in self.league
        }
        print("Loaded standings teams:", list(standings.keys()))
        return standings

    def get_sorted_standings_by_conference(self):
        conferences = {"Nova": [], "Atlas": []}
        for team, data in self.standings.items():
            conf = data.get("conference")
            if conf in conferences:
                conferences[conf].append((team, data))

        def sort_key(item):
            team, record = item
            total_games = record["W"] + record["L"] + record["T"]
            win_pct = record["W"] / total_games if total_games > 0 else 0
            return win_pct

        team_dict_by_abbr = {t["abbreviation"]: t for t in self.league}

        for conf in conferences:
            tied = conferences[conf]
            tied.sort(key=sort_key, reverse=True)
            team_names = [team for team, _ in tied]
            ordered_abbrs = self.break_ties(team_names)
            conferences[conf] = [team_dict_by_abbr[abbr] for abbr in ordered_abbrs if abbr in team_dict_by_abbr]

        return conferences

    def save_standings(self):
        path = self.get_standings_path()
        with open(path, "w") as f:
            json.dump(self.standings, f, indent=2)

    def update_from_result(self, result):
        home = result["home"]
        away = result["away"]
        home = self.seed_alias_map.get(home, home)
        away = self.seed_alias_map.get(away, away)
        hs = result["home_score"]
        as_ = result["away_score"]

        if home not in self.standings or away not in self.standings:
            print(f"[ERROR] One or both teams not in standings: {home}, {away}")
            return

        self.standings[home]["PF"] += hs
        self.standings[home]["PA"] += as_
        self.standings[away]["PF"] += as_
        self.standings[away]["PA"] += hs

        if hs > as_:
            self.standings[home]["W"] += 1
            self.standings[away]["L"] += 1
            winner, loser = home, away
        elif hs < as_:
            self.standings[away]["W"] += 1
            self.standings[home]["L"] += 1
            winner, loser = away, home
        else:
            self.standings[home]["T"] += 1
            self.standings[away]["T"] += 1
            winner, loser = None, None

        self.standings[home].setdefault("opponents", {})
        self.standings[away].setdefault("opponents", {})
        self.standings[home]["opponents"].setdefault(away, {"W": 0, "L": 0, "T": 0})
        self.standings[away]["opponents"].setdefault(home, {"W": 0, "L": 0, "T": 0})

        if winner == home:
            self.standings[home]["opponents"][away]["W"] += 1
            self.standings[away]["opponents"][home]["L"] += 1
        elif winner == away:
            self.standings[away]["opponents"][home]["W"] += 1
            self.standings[home]["opponents"][away]["L"] += 1
        elif winner is None:
            self.standings[home]["opponents"][away]["T"] += 1
            self.standings[away]["opponents"][home]["T"] += 1

    def summarize(self):
        return {
            team: f"{record['W']}-{record['L']}-{record['T']}"
            for team, record in self.standings.items()
        }

    def break_ties(self, tied_teams):
        if len(tied_teams) <= 1:
            return tied_teams

        # Step 1: Head-to-head win count
        head_to_head = {team: 0 for team in tied_teams}
        for week_results in (self.results_by_week or {}).values():
            for result in week_results:
                home, away = result["home"], result["away"]
                winner = None
                if result["home_score"] > result["away_score"]:
                    winner = home
                elif result["home_score"] < result["away_score"]:
                    winner = away

                if home in tied_teams and away in tied_teams and winner in tied_teams:
                    head_to_head[winner] += 1

        max_hth = max(head_to_head.values())
        tied_teams = [team for team in tied_teams if head_to_head[team] == max_hth]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 2: Conference record
        def conf_wins(team):
            total = 0
            for opp, rec in self.standings[team]["opponents"].items():
                opp_conf = self.standings.get(opp, {}).get("conference")
                if opp_conf == self.standings[team]["conference"]:
                    total += rec["W"]
            return total
        max_conf = max(conf_wins(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if conf_wins(t) == max_conf]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 3: Division record
        def div_wins(team):
            total = 0
            for opp, rec in self.standings[team]["opponents"].items():
                opp_div = self.standings.get(opp, {}).get("division")
                if opp_div == self.standings[team]["division"]:
                    total += rec["W"]
            return total
        max_div = max(div_wins(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if div_wins(t) == max_div]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 4: Points For
        max_pf = max(self.standings[t]["PF"] for t in tied_teams)
        tied_teams = [t for t in tied_teams if self.standings[t]["PF"] == max_pf]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 5: Strength of victory (average points scored in wins)
        def strength_of_victory(team):
            wins = self.standings[team]["W"]
            if wins == 0:
                return 0
            return self.standings[team]["PF"] / wins
        max_sov = max(strength_of_victory(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if strength_of_victory(t) == max_sov]
        if len(tied_teams) == 1:
            return tied_teams

        # Step 6: Strength of schedule (average opponent win %)
        def sos(team):
            win_percents = []
            for opp in self.standings[team]["opponents"]:
                opp_record = self.standings.get(opp)
                if opp_record:
                    total_games = opp_record["W"] + opp_record["L"] + opp_record["T"]
                    if total_games > 0:
                        wp = opp_record["W"] / total_games
                        win_percents.append(wp)
            return sum(win_percents) / len(win_percents) if win_percents else 0
        max_sos = max(sos(t) for t in tied_teams)
        tied_teams = [t for t in tied_teams if sos(t) == max_sos]
        return tied_teams

    def load_playoff_seed_map(self, year, save_name="test_league"):
        bracket_path = f"saves/{save_name}/playoffs/playoff_bracket_{year}.json"
        if not os.path.exists(bracket_path):
            self.seed_alias_map = {}
            return
        with open(bracket_path, "r") as f:
            bracket = json.load(f)
        self.seed_alias_map = {
            f"{i}A": bracket["Atlas"]["seeds"][i - 1]["team"]
            for i in range(1, 8)
        }
        self.seed_alias_map.update({
            f"{i}N": bracket["Nova"]["seeds"][i - 1]["team"]
            for i in range(1, 8)
        })

    def get_full_standings(self):
        return self.standings
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
