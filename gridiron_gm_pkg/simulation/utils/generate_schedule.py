import json
from pathlib import Path
from typing import Any, Dict, Iterable, List, Tuple

# Config paths
_BASE_DIR = Path(__file__).resolve().parents[2]
TEAMS_PATH = _BASE_DIR / "config" / "teams.json"
SAVE_ROOT = _BASE_DIR / "data" / "saves"

BYE_WEEK_LABEL = "Bye Week"
PRESEASON_WEEKS = 3
REGULAR_SEASON_WEEKS = 18
PRESEASON_BYE_WEEK = PRESEASON_WEEKS + 1
REGULAR_SEASON_START_WEEK = PRESEASON_BYE_WEEK + 1
REGULAR_SEASON_END_WEEK = REGULAR_SEASON_START_WEEK + REGULAR_SEASON_WEEKS - 1
FULL_SEASON_WEEKS = REGULAR_SEASON_END_WEEK
BYE_WEEK = PRESEASON_BYE_WEEK

def load_teams() -> List[Tuple[str, str]]:
    with TEAMS_PATH.open("r", encoding="utf-8") as f:
        teams = json.load(f)
    return [(team["id"], team["abbreviation"]) for team in teams]

def _get_save_dir(save_name: str) -> Path:
    return SAVE_ROOT / save_name

def _round_index_for_week(week: int) -> int | None:
    if week < 1:
        return None
    if week <= PRESEASON_WEEKS:
        return week - 1
    if week == PRESEASON_BYE_WEEK:
        return None
    if REGULAR_SEASON_START_WEEK <= week <= REGULAR_SEASON_END_WEEK:
        return week - REGULAR_SEASON_START_WEEK + PRESEASON_WEEKS
    return None


def _label_for_week(week: int) -> str | None:
    if week <= PRESEASON_WEEKS:
        return "Preseason"
    if week == PRESEASON_BYE_WEEK:
        return BYE_WEEK_LABEL
    if REGULAR_SEASON_START_WEEK <= week <= REGULAR_SEASON_END_WEEK:
        return "Regular Season"
    return None


def _season_info_for_week(week: int) -> tuple[str, int] | None:
    if week <= PRESEASON_WEEKS:
        return "preseason", week
    if week == PRESEASON_BYE_WEEK:
        return "bye", 0
    if REGULAR_SEASON_START_WEEK <= week <= REGULAR_SEASON_END_WEEK:
        return "regular", week - REGULAR_SEASON_START_WEEK + 1
    return None


def _week_key_for(season_type: str, season_week: int) -> str:
    return f"{season_type}:{season_week}"


def _build_round_robin_rounds(team_ids: List[str]) -> List[List[Tuple[str, str]]]:
    ids = list(team_ids)
    if len(ids) % 2 != 0:
        ids.append("BYE")
    if len(ids) < 2:
        return []
    rotation = list(ids)
    rounds: List[List[Tuple[str, str]]] = []
    total = len(rotation)
    half = total // 2
    for _ in range(total - 1):
        pairs: List[Tuple[str, str]] = []
        for i in range(half):
            pairs.append((rotation[i], rotation[total - 1 - i]))
        rounds.append(pairs)
        rotation = [rotation[0]] + [rotation[-1]] + rotation[1:-1]
    return rounds


def generate_schedule(team_objs=None, save_name: str = "test_league", weeks: int = FULL_SEASON_WEEKS):
    if isinstance(team_objs, str) and save_name == "test_league":
        save_name = team_objs
        team_objs = None
    if team_objs is None or isinstance(team_objs, str):
        team_tuples = load_teams()
    else:
        team_tuples = [(team.id, team.abbreviation) for team in team_objs]
    return generate_minimal_schedule(team_tuples, save_name=save_name, weeks=weeks)


def generate_full_schedule_files(
    league: Any,
    *,
    save_name: str,
    weeks: int = FULL_SEASON_WEEKS,
    day: str = "Sunday",
    kickoff: str = "19:00",
) -> Tuple[Dict[str, Any], Dict[str, Any]]:
    team_tuples = [(team.id, team.abbreviation) for team in getattr(league, "teams", [])]
    schedule_by_week = generate_minimal_schedule(
        team_tuples,
        save_name=save_name,
        weeks=weeks,
        day=day,
        kickoff=kickoff,
    )
    if not schedule_by_week:
        return {}, {}
    save_dir = _get_save_dir(save_name)
    schedule_by_team = _load_schedule(save_dir / "schedule_by_team.json")
    if not isinstance(schedule_by_team, dict):
        schedule_by_team = {}
    return schedule_by_week, schedule_by_team


def ensure_schedule_exists(save_name: str, league: Any, calendar: Any) -> Dict[str, Any]:
    save_dir = _get_save_dir(save_name)
    schedule_path = save_dir / "schedule_by_week.json"
    schedule = _load_schedule(schedule_path)
    if not isinstance(schedule, dict):
        schedule = {}
    schedule = _normalize_schedule_keys(schedule)
    expected_games = _expected_games_per_week(league)
    if _schedule_has_games(schedule) and (
        expected_games <= 0 or _schedule_has_full_week(schedule, expected_games)
    ):
        return schedule
    team_tuples = [(team.id, team.abbreviation) for team in getattr(league, "teams", [])]
    schedule = generate_minimal_schedule(team_tuples, save_name=save_name, weeks=FULL_SEASON_WEEKS)
    return schedule


def generate_minimal_schedule(
    team_tuples: Iterable[Tuple[str, str]],
    *,
    save_name: str,
    weeks: int = FULL_SEASON_WEEKS,
    day: str = "Sunday",
    kickoff: str = "19:00",
) -> Dict[str, Any]:
    team_list = [(tid, abbr) for tid, abbr in team_tuples if tid]
    if not team_list:
        return {}
    team_list = sorted(team_list, key=lambda item: item[0])
    team_ids = [tid for tid, _ in team_list]
    id_to_abbr = {tid: abbr for tid, abbr in team_list}

    schedule_by_week: Dict[str, List[Dict[str, Any]]] = {}
    schedule_by_team: Dict[str, List[Dict[str, Any]]] = {tid: [] for tid in id_to_abbr}
    rounds = _build_round_robin_rounds(team_ids)
    if not rounds:
        return {}
    total_rounds = len(rounds)

    for week in range(1, weeks + 1):
        if BYE_WEEK and week == BYE_WEEK:
            schedule_by_week[str(week)] = []
            for tid in schedule_by_team:
                schedule_by_team[tid].append(
                    {
                        "week": str(week),
                        "calendar_week": week,
                        "season_type": "bye",
                        "season_week": 0,
                        "week_key": "bye:0",
                        "opponent_id": None,
                        "home": None,
                        "day": "",
                        "kickoff": "",
                        "label": BYE_WEEK_LABEL,
                    }
                )
            continue

        season_info = _season_info_for_week(week)
        round_index = _round_index_for_week(week)
        if season_info is None or round_index is None:
            schedule_by_week[str(week)] = []
            continue
        season_type, season_week = season_info
        week_key = _week_key_for(season_type, season_week)
        round_pairs = rounds[round_index % total_rounds]
        label = _label_for_week(week)
        games: List[Dict[str, Any]] = []
        for pair_index, (home_id, away_id) in enumerate(round_pairs):
            if "BYE" in (home_id, away_id):
                continue
            if (round_index + pair_index) % 2 == 1:
                home_id, away_id = away_id, home_id
            game = {
                "week": week,
                "calendar_week": week,
                "season_type": season_type,
                "season_week": season_week,
                "week_key": week_key,
                "day": day,
                "kickoff": kickoff,
                "home_id": home_id,
                "away_id": away_id,
                "home_abbr": id_to_abbr.get(home_id),
                "away_abbr": id_to_abbr.get(away_id),
                "label": label,
            }
            games.append(game)
            for tid in (home_id, away_id):
                schedule_by_team[tid].append(
                    {
                        "week": str(week),
                        "calendar_week": week,
                        "season_type": season_type,
                        "season_week": season_week,
                        "week_key": week_key,
                        "opponent_id": away_id if tid == home_id else home_id,
                        "home": tid == home_id,
                        "day": day,
                        "kickoff": kickoff,
                        "label": label,
                    }
                )
        schedule_by_week[str(week)] = games

    save_dir = _get_save_dir(save_name)
    save_dir.mkdir(parents=True, exist_ok=True)
    _write_json(save_dir / "schedule_by_week.json", schedule_by_week)
    _write_json(save_dir / "schedule_by_team.json", schedule_by_team)
    return schedule_by_week


def _rotate(items: List[str], offset: int) -> List[str]:
    if not items:
        return items
    offset = offset % len(items)
    if offset == 0:
        return list(items)
    return list(items[offset:] + items[:offset])


def _load_schedule(path: Path) -> Dict[str, Any]:
    if not path.exists():
        return {}
    try:
        with path.open("r", encoding="utf-8") as f:
            return json.load(f)
    except json.JSONDecodeError:
        return {}


def _schedule_has_games(schedule: Dict[str, Any]) -> bool:
    if not schedule:
        return False
    for games in schedule.values():
        if not isinstance(games, list):
            continue
        for game in games:
            if isinstance(game, dict) and game.get("home_id") and game.get("away_id"):
                return True
    return False


def _normalize_schedule_keys(schedule: Dict[str, Any]) -> Dict[str, Any]:
    if not isinstance(schedule, dict):
        return {}
    normalized: Dict[str, Any] = {}
    for key, games in schedule.items():
        key_str = str(key)
        if key_str not in normalized:
            normalized[key_str] = games
            continue
        existing = normalized[key_str]
        if isinstance(existing, list) and isinstance(games, list):
            normalized[key_str] = existing + games
        elif isinstance(existing, list):
            normalized[key_str] = existing + [games]
        elif isinstance(games, list):
            normalized[key_str] = [existing] + games
        else:
            normalized[key_str] = games
    return normalized


def _expected_games_per_week(league: Any) -> int:
    teams = getattr(league, "teams", []) if league is not None else []
    team_count = sum(1 for team in teams if getattr(team, "id", None))
    if team_count < 2:
        return 0
    return team_count // 2


def _count_valid_games(games: Any) -> int:
    if not isinstance(games, list):
        return 0
    count = 0
    for game in games:
        if isinstance(game, dict) and game.get("home_id") and game.get("away_id"):
            count += 1
    return count


def _schedule_has_full_week(schedule: Dict[str, Any], expected_games: int) -> bool:
    if expected_games <= 0:
        return False
    for games in schedule.values():
        if _count_valid_games(games) >= expected_games:
            return True
    return False


def _write_json(path: Path, payload: Dict[str, Any]) -> None:
    with path.open("w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)

def add_nfl_style_playoff_schedule(schedule_by_week, standings_by_conf, id_to_abbr, start_week):
    """
    Adds NFL-style playoff games to schedule_by_week.
    standings_by_conf: dict with "Nova" and "Atlas" keys, each value is a list of team dicts in seed order.
    id_to_abbr: dict mapping team id to abbreviation.
    start_week: int, the week number to start playoffs (first playoff week).
    """
    def _playoff_game(
        *,
        week: int,
        season_week: int,
        round_name: str,
        conference: str,
        day: str,
        kickoff: str,
        home_id: str,
        away_id: str,
        home_abbr: str | None,
        away_abbr: str | None,
        home_seed: int | None = None,
        away_seed: int | None = None,
    ) -> dict:
        week_label = f"Playoffs - {round_name}"
        return {
            "week": week,
            "calendar_week": week,
            "season_type": "playoffs",
            "season_week": season_week,
            "week_key": f"playoffs:{season_week}",
            "week_label": week_label,
            "day": day,
            "kickoff": kickoff,
            "home_id": home_id,
            "away_id": away_id,
            "playoff": True,
            "round": round_name,
            "conference": conference,
            "home_abbr": home_abbr,
            "away_abbr": away_abbr,
            "home_seed": home_seed,
            "away_seed": away_seed,
            "label": week_label,
        }

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
            _playoff_game(
                week=week,
                season_week=1,
                round_name="Wild Card",
                conference=conf,
                day="Saturday" if conf == "Nova" else "Sunday",
                kickoff="1:00 PM",
                home_id=conf_seeds[1]["id"],
                away_id=conf_seeds[6]["id"],
                home_abbr=id_to_abbr[conf_seeds[1]["id"]],
                away_abbr=id_to_abbr[conf_seeds[6]["id"]],
                home_seed=2,
                away_seed=7,
            ),
            _playoff_game(
                week=week,
                season_week=1,
                round_name="Wild Card",
                conference=conf,
                day="Saturday" if conf == "Nova" else "Sunday",
                kickoff="4:30 PM",
                home_id=conf_seeds[2]["id"],
                away_id=conf_seeds[5]["id"],
                home_abbr=id_to_abbr[conf_seeds[2]["id"]],
                away_abbr=id_to_abbr[conf_seeds[5]["id"]],
                home_seed=3,
                away_seed=6,
            ),
            _playoff_game(
                week=week,
                season_week=1,
                round_name="Wild Card",
                conference=conf,
                day="Saturday" if conf == "Nova" else "Sunday",
                kickoff="8:15 PM",
                home_id=conf_seeds[3]["id"],
                away_id=conf_seeds[4]["id"],
                home_abbr=id_to_abbr[conf_seeds[3]["id"]],
                away_abbr=id_to_abbr[conf_seeds[4]["id"]],
                home_seed=4,
                away_seed=5,
            ),
        ]
    schedule_by_week[str(week)] = wc_games
    week += 1

    # --- Divisional Round (week 2 of playoffs) ---
    div_games = []
    for conf in ["Nova", "Atlas"]:
        div_games += [
            _playoff_game(
                week=week,
                season_week=2,
                round_name="Divisional",
                conference=conf,
                day="Saturday" if conf == "Nova" else "Sunday",
                kickoff="4:30 PM",
                home_id=seeds[conf][0]["id"],
                away_id="TBD_LowestSeedWinner_" + conf,
                home_abbr=id_to_abbr[seeds[conf][0]["id"]],
                away_abbr="TBD",
                home_seed=1,
            ),
            _playoff_game(
                week=week,
                season_week=2,
                round_name="Divisional",
                conference=conf,
                day="Saturday" if conf == "Nova" else "Sunday",
                kickoff="8:15 PM",
                home_id="TBD_HighSeedHost_" + conf,
                away_id="TBD_OtherWinner_" + conf,
                home_abbr="TBD",
                away_abbr="TBD",
            ),
        ]
    schedule_by_week[str(week)] = div_games
    week += 1

    # --- Conference Championship (week 3 of playoffs) ---
    cc_games = []
    for conf in ["Nova", "Atlas"]:
        cc_games.append(
            _playoff_game(
                week=week,
                season_week=3,
                round_name="Conference Championship",
                conference=conf,
                day="Sunday",
                kickoff="6:30 PM",
                home_id="TBD_CC_Host_" + conf,
                away_id="TBD_CC_Away_" + conf,
                home_abbr="TBD",
                away_abbr="TBD",
            )
        )
    schedule_by_week[str(week)] = cc_games
    week += 1

    # --- Gridiron Bowl (week 4 of playoffs) ---
    gb_game = [
        _playoff_game(
            week=week,
            season_week=4,
            round_name="Gridiron Bowl",
            conference="Both",
            day="Sunday",
            kickoff="6:30 PM",
            home_id="TBD_Nova_Champ",
            away_id="TBD_Atlas_Champ",
            home_abbr="TBD",
            away_abbr="TBD",
        )
    ]
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
