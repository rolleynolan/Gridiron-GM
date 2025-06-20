import json
import os
import random
from datetime import datetime, timedelta
from gridiron_gm_pkg.simulation.systems.game.standings_manager import StandingsManager

# Config paths
TEAMS_PATH = os.path.join("config", "teams.json")
SAVES_PATH = os.path.join("data", "saves")
SCHEDULE_BY_WEEK_PATH = os.path.join(SAVES_PATH, "schedule_by_week.json")
SCHEDULE_BY_TEAM_PATH = os.path.join(SAVES_PATH, "schedule_by_team.json")

PRESEASON_WEEKS = 3
BYE_WEEK_LABEL = "Bye Week"
REGULAR_SEASON_WEEKS = 18
PLAYOFF_WEEKS = 4  # Placeholder for now

def load_teams():
    with open(TEAMS_PATH, "r") as f:
        teams = json.load(f)
    return [(team["id"], team["abbreviation"]) for team in teams]

def generate_bye_weeks(team_ids, start_week=9, end_week=18, teams_on_bye_each_week=6):
    num_bye_weeks = end_week - start_week + 1
    byes = {tid: None for tid in team_ids}
    all_bye_weeks = list(range(start_week, end_week + 1))
    random.shuffle(all_bye_weeks)
    week_pointer = 0
    for tid in team_ids:
        if week_pointer >= num_bye_weeks:
            week_pointer = 0
        bye_week = all_bye_weeks[week_pointer]
        byes[tid] = bye_week
        week_pointer += 1
    return byes

def generate_schedule(team_objs, save_name="test_league"):
    SAVES_PATH = os.path.join("data", "saves", save_name)
    os.makedirs(SAVES_PATH, exist_ok=True)
    SCHEDULE_BY_WEEK_PATH = os.path.join(SAVES_PATH, "schedule_by_week.json")
    SCHEDULE_BY_TEAM_PATH = os.path.join(SAVES_PATH, "schedule_by_team.json")
    team_tuples = [(team.id, team.abbreviation) for team in team_objs]
    team_ids = [tid for tid, abbr in team_tuples]
    id_to_abbr = {tid: abbr for tid, abbr in team_tuples}
    num_teams = len(team_ids)
    if num_teams % 2 != 0:
        raise ValueError("Odd number of teams: add a BYE team or ensure an even count.")

    # Preseason
    preseason = []
    preseason_ids = team_ids.copy()
    for week in range(1, PRESEASON_WEEKS + 1):
        random.shuffle(preseason_ids)
        games = []
        for i in range(0, num_teams, 2):
            games.append({
                "home_id": preseason_ids[i],
                "away_id": preseason_ids[i+1],
                "day": "Saturday",
                "week": week,
                "kickoff": "12:00 PM",
                "preseason": True
            })
        preseason.append(games)

    # Bye weeks
    bye_weeks = generate_bye_weeks(team_ids, start_week=9, end_week=18, teams_on_bye_each_week=6)

    # Regular season
    reg_season = []
    for week in range(4, 4 + REGULAR_SEASON_WEEKS):
        games = []
        week_teams = [tid for tid in team_ids if bye_weeks.get(tid) != week]
        random.shuffle(week_teams)
        for i in range(0, len(week_teams), 2):
            if i+1 < len(week_teams):
                games.append({
                    "home_id": week_teams[i],
                    "away_id": week_teams[i+1],
                    "day": "Sunday",
                    "week": week,
                    "kickoff": "1:00 PM",
                    "preseason": False
                })
        byes_this_week = [tid for tid, w in bye_weeks.items() if w == week]
        for tid in byes_this_week:
            games.append({
                "home_id": None,
                "away_id": tid,
                "day": BYE_WEEK_LABEL,
                "week": week,
                "kickoff": None,
                "preseason": False,
                "bye": True
            })
        reg_season.append(games)

    # Postseason: Placeholder for bracket logic
    postseason = []
    for playoff_week in range(4 + REGULAR_SEASON_WEEKS, 4 + REGULAR_SEASON_WEEKS + PLAYOFF_WEEKS):
        postseason.append([])  # Actual matchups to be filled by playoff logic

    # Merge full schedule by week
    schedule_by_week = {}
    week_num = 1
    for games in preseason:
        schedule_by_week[str(week_num)] = games
        week_num += 1
    schedule_by_week[str(week_num)] = []  # Bye week after preseason
    week_num += 1
    for games in reg_season:
        schedule_by_week[str(week_num)] = games
        week_num += 1
    for games in postseason:
        schedule_by_week[str(week_num)] = games
        week_num += 1

    # Build schedule by team
    schedule_by_team = {tid: [] for tid in team_ids}
    for week_str, games in schedule_by_week.items():
        for game in games:
            if game.get("preseason"):
                label = "Preseason"
            elif game.get("bye"):
                label = BYE_WEEK_LABEL
            else:
                label = "Regular Season"
            for tid in [game.get("home_id"), game.get("away_id")]:
                if tid and tid in schedule_by_team:
                    opponent = game["away_id"] if tid == game.get("home_id") else game["home_id"]
                    schedule_by_team[tid].append({
                        "week": week_str,
                        "opponent_id": opponent,
                        "home": tid == game.get("home_id"),
                        "day": game.get("day"),
                        "kickoff": game.get("kickoff"),
                        "label": label
                    })

    # Write out schedules
    os.makedirs(SAVES_PATH, exist_ok=True)
    with open(SCHEDULE_BY_WEEK_PATH, "w") as f:
        json.dump(schedule_by_week, f, indent=2)
    with open(SCHEDULE_BY_TEAM_PATH, "w") as f:
        json.dump(schedule_by_team, f, indent=2)

    print(f"Schedule generated and saved: {SCHEDULE_BY_WEEK_PATH}, {SCHEDULE_BY_TEAM_PATH}")

def add_nfl_style_playoff_schedule(schedule_by_week, standings_by_conf, id_to_abbr, start_week):
    """
    Adds NFL-style playoff games to schedule_by_week.
    standings_by_conf: dict with "Nova" and "Atlas" keys, each value is a list of team dicts in seed order.
    id_to_abbr: dict mapping team id to abbreviation.
    start_week: int, the week number to start playoffs (first playoff week).
    """
    playoff_games = []
    playoff_rounds = ["Wild Card", "Divisional", "Conference Championship", "Gridiron Bowl"]

    # Seeds: [1,2,3,4,5,6,7] for each conference
    seeds = {}
    for conf in ["Nova", "Atlas"]:
        seeds[conf] = standings_by_conf[conf][:7]  # List of team dicts

    week = start_week

    # --- Wild Card Round (week 1 of playoffs) ---
    wc_games = []
    for conf in ["Nova", "Atlas"]:
        # 2 vs 7, 3 vs 6, 4 vs 5; 1 seed gets bye
        conf_seeds = seeds[conf]
        wc_games += [
            {
                "home_id": conf_seeds[1]["id"], "away_id": conf_seeds[6]["id"],
                "day": "Sunday" if conf == "Nova" else "Monday",
                "week": week, "kickoff": "1:00 PM", "playoff": True,
                "round": "Wild Card", "conference": conf,
                "home_abbr": id_to_abbr[conf_seeds[1]["id"]], "away_abbr": id_to_abbr[conf_seeds[6]["id"]]
            },
            {
                "home_id": conf_seeds[2]["id"], "away_id": conf_seeds[5]["id"],
                "day": "Sunday" if conf == "Nova" else "Monday",
                "week": week, "kickoff": "4:30 PM", "playoff": True,
                "round": "Wild Card", "conference": conf,
                "home_abbr": id_to_abbr[conf_seeds[2]["id"]], "away_abbr": id_to_abbr[conf_seeds[5]["id"]]
            },
            {
                "home_id": conf_seeds[3]["id"], "away_id": conf_seeds[4]["id"],
                "day": "Sunday" if conf == "Nova" else "Monday",
                "week": week, "kickoff": "8:15 PM", "playoff": True,
                "round": "Wild Card", "conference": conf,
                "home_abbr": id_to_abbr[conf_seeds[3]["id"]], "away_abbr": id_to_abbr[conf_seeds[4]["id"]]
            }
        ]
    schedule_by_week[str(week)] = wc_games
    week += 1

    # --- Divisional Round (week 2 of playoffs) ---
    div_games = []
    for conf in ["Nova", "Atlas"]:
        div_games += [
            {
                "home_id": seeds[conf][0]["id"], "away_id": "TBD_LowestSeedWinner_" + conf,
                "day": "Sunday" if conf == "Nova" else "Monday",
                "week": week, "kickoff": "3:00 PM", "playoff": True,
                "round": "Divisional", "conference": conf,
                "home_abbr": id_to_abbr[seeds[conf][0]["id"]], "away_abbr": "TBD"
            },
            {
                "home_id": "TBD_HighSeedHost_" + conf, "away_id": "TBD_OtherWinner_" + conf,
                "day": "Sunday" if conf == "Nova" else "Monday",
                "week": week, "kickoff": "6:30 PM", "playoff": True,
                "round": "Divisional", "conference": conf,
                "home_abbr": "TBD", "away_abbr": "TBD"
            }
        ]
    schedule_by_week[str(week)] = div_games
    week += 1

    # --- Conference Championship (week 3 of playoffs) ---
    cc_games = []
    for conf in ["Nova", "Atlas"]:
        cc_games.append({
            "home_id": "TBD_CC_Host_" + conf, "away_id": "TBD_CC_Away_" + conf,
            "day": "Sunday" if conf == "Nova" else "Monday",
            "week": week, "kickoff": "6:30 PM", "playoff": True,
            "round": "Conference Championship", "conference": conf,
            "home_abbr": "TBD", "away_abbr": "TBD"
        })
    schedule_by_week[str(week)] = cc_games
    week += 1

    # --- BYE WEEK before Gridiron Bowl ---
    schedule_by_week[str(week)] = []  # Empty week for bye
    week += 1

    # --- Gridiron Bowl (week 4 of playoffs, after bye) ---
    gb_game = [{
        "home_id": "TBD_Nova_Champ", "away_id": "TBD_Atlas_Champ",
        "day": "Sunday", "week": week, "kickoff": "6:30 PM", "playoff": True,
        "round": "Gridiron Bowl", "conference": "Both",
        "home_abbr": "TBD", "away_abbr": "TBD"
    }]
    schedule_by_week[str(week)] = gb_game

# --- Usage Example ---
# After you build your regular season schedule and have standings:
# standings_manager = StandingsManager(calendar, league, save_name, results_by_week)
# standings_by_conf = standings_manager.get_sorted_standings_by_conference()
# id_to_abbr = {team.id: team.abbreviation for team in team_objs}
# playoff_start_week = 4 + REGULAR_SEASON_WEEKS  # Adjust if needed
# add_nfl_style_playoff_schedule(schedule_by_week, standings_by_conf, id_to_abbr, playoff_start_week)

if __name__ == "__main__":
    generate_schedule()
