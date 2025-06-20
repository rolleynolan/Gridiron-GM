"""
Create a test runner that:
1. Loads or generates mock teams and rosters if real data isn't available (please use pro players from the player_generation_output if possible) 
2. Simulates one week of games using the procedural play-by-play engine.
3. Stores each result in results_by_week[week].
4. Outputs:
   - Final score
   - Total yards
   - Key player stats (QB, RB, WR)
   - Any injuries that occurred

This is used to validate the realism of the procedural engine.
If team data exists, use real teams. Otherwise, create 16 fake teams with balanced rosters.
Week number can be hardcoded to 1 for now.
"""

import json
import os
import random
from pathlib import Path
from typing import Any, Dict, List

import sys

# Ensure the repository root is on the path so local modules can be imported
ROOT_DIR = Path(__file__).resolve().parents[1]
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.utils.player_generator import PlayerGenerator
from gridiron_gm_pkg.simulation.engine.game_engine import simulate_game

CONFIG_DIR = Path(__file__).resolve().parents[1] / "gridiron_gm_pkg" / "config"
PLAYER_DATA_PATH = Path("dna_output/player_generation_output.json")
TEAMS_PATH = CONFIG_DIR / "teams.json"

SAVE_DIR = Path(__file__).resolve().parents[1] / "save"
LEAGUE_STATE_FILE = SAVE_DIR / "league_state.json"


def ensure_league_state_file() -> None:
    """Create the league_state.json file if it doesn't exist."""
    os.makedirs(SAVE_DIR, exist_ok=True)
    if not LEAGUE_STATE_FILE.exists():
        with open(LEAGUE_STATE_FILE, "w", encoding="utf-8") as f:
            json.dump({"week": 0, "teams": [], "results_by_week": {}}, f, indent=2)


def update_league_state(teams: List[Team], week: int, results_by_week: Dict[int, List[Dict[str, Any]]]) -> None:
    """Write the current league state used by the Unity frontend."""
    os.makedirs(SAVE_DIR, exist_ok=True)
    state = {
        "week": week,
        "teams": [
            {
                "name": getattr(team, "team_name", getattr(team, "name", "")),
                "abbreviation": team.abbreviation,
                "roster": [
                    {
                        "name": p.name,
                        "position": p.position,
                        "age": getattr(p, "age", 0),
                        "overall": getattr(p, "overall", 0),
                        "contract": {
                            "years_left": (
                                p.contract.get("years_left", p.contract.get("years", 0))
                                if getattr(p, "contract", None)
                                else 0
                            )
                        },
                    }
                    for p in getattr(team, "players", [])
                ],
            }
            for team in teams
        ],
        "results_by_week": {str(k): v for k, v in results_by_week.items()},
    }
    with open(LEAGUE_STATE_FILE, "w", encoding="utf-8") as f:
        json.dump(state, f, indent=2)


def load_player_pool() -> Dict[str, List]:
    """Return a pool of Player objects keyed by position."""
    pool: Dict[str, List] = {}
    if not PLAYER_DATA_PATH.exists():
        return pool

    gen = PlayerGenerator()
    try:
        with open(PLAYER_DATA_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception:
        return pool

    pro_players = [p for p in data if p.get("level") == "pro"]
    random.shuffle(pro_players)
    position_map = {"DT": "DL", "EDGE": "DL"}
    for entry in pro_players:
        pos = entry.get("position", "UNK")
        pos = position_map.get(pos, pos)
        if pos not in {
            "QB", "RB", "WR", "TE", "OL", "DL", "LB", "CB", "S", "K", "P"
        }:
            continue
        player = gen.generate_pro_player(pos)
        attrs = {k: int(v.get("value", 50)) for k, v in entry.get("attributes", {}).items()}
        if attrs:
            player.attributes.position_specific.update(attrs)
            player.position_specific.update(attrs)
            player.overall = int(sum(attrs.values()) / len(attrs))
        pool.setdefault(pos, []).append(player)
    return pool


ROSTER_TEMPLATE = {
    "QB": 2,
    "RB": 3,
    "WR": 4,
    "TE": 2,
    "LT": 1,
    "LG": 1,
    "C": 1,
    "RG": 1,
    "RT": 1,
    "DE": 4,
    "DT": 4,
    "LB": 5,
    "CB": 4,
    "S": 2,
    "K": 1,
    "P": 1,
}


def build_team_roster(pool: Dict[str, List]) -> List:
    """Generate a simple balanced roster using the pool if available."""
    generator = PlayerGenerator()
    roster: List = []
    position_base = {"LT": "OL", "LG": "OL", "C": "OL", "RG": "OL", "RT": "OL", "DE": "DL", "DT": "DL"}
    for pos, count in ROSTER_TEMPLATE.items():
        base_pos = position_base.get(pos, pos)
        for _ in range(count):
            if pool.get(base_pos):
                player = pool[base_pos].pop()
            else:
                player = generator.generate_pro_player(base_pos)
            player.position = pos
            if not hasattr(player, "discipline_rating"):
                player.discipline_rating = 75
            roster.append(player)
    return roster


def load_or_create_teams(num_teams: int = 16) -> List[Team]:
    teams: List[Team] = []
    if TEAMS_PATH.exists():
        try:
            with open(TEAMS_PATH, "r", encoding="utf-8") as f:
                templates = json.load(f)
            templates = templates[:num_teams]
            pool = load_player_pool()
            for tpl in templates:
                team = Team(
                    team_name=tpl.get("name"),
                    city=tpl.get("city"),
                    abbreviation=tpl.get("abbreviation"),
                    conference=tpl.get("conference", "Nova"),
                    division=tpl.get("division", "Unknown"),
                )
                for player in build_team_roster(pool):
                    team.add_player(player, position_override=player.position)
                team.generate_depth_chart()
                teams.append(team)
            return teams
        except Exception:
            pass

    # Fallback: create simple mock teams
    pool = load_player_pool()
    generator = PlayerGenerator()
    for i in range(num_teams):
        team = Team(
            team_name=f"Team {i+1}",
            city=f"City {i+1}",
            abbreviation=f"T{i+1:02d}",
            conference="Test",
            division="Test",
        )
        for player in build_team_roster(pool):
            team.add_player(player, position_override=player.position)
        team.generate_depth_chart()
        teams.append(team)
    return teams


def summarize_stats(team: Team, stats: Dict) -> Dict:
    """Return a summary of key player stats using team totals."""
    qb = team.depth_chart.get("QB", [None])[0]
    rb = team.depth_chart.get("RB", [None])[0]
    wr = team.depth_chart.get("WR", [None])[0]
    return {
        "qb": {
            "name": getattr(qb, "name", "N/A"),
            "pass_yards": stats.get("pass_yards", 0),
            "pass_td": stats.get("pass_td", 0),
        },
        "rb": {
            "name": getattr(rb, "name", "N/A"),
            "rush_yards": stats.get("rush_yards", 0),
            "rush_td": stats.get("rush_td", 0),
        },
        "wr": {
            "name": getattr(wr, "name", "N/A"),
            # Receiving yards/TDs aren't tracked separately yet, so use pass stats
            "rec_yards": stats.get("rec_yards", stats.get("pass_yards", 0)),
            "rec_td": stats.get("rec_td", stats.get("pass_td", 0)),
        },
    }


def main() -> None:
    week = 1
    ensure_league_state_file()
    teams = load_or_create_teams()
    random.shuffle(teams)
    results_by_week: Dict[int, List[Dict[str, Any]]] = {}
    total_injuries = 0
    stat_players = {"QB": 0, "RB": 0, "WR": 0}

    for i in range(0, len(teams), 2):
        home = teams[i]
        away = teams[i + 1]
        context = {"weather": None, "game_injuries": [], "week": week, "weekly_injuries": []}
        home_stats, away_stats = simulate_game(home, away, week=week, context=context)

        result = {
            "home": home.abbreviation,
            "away": away.abbreviation,
            "home_score": home_stats.get("points", 0),
            "away_score": away_stats.get("points", 0),
            "home_yards": home_stats.get("rush_yards", 0) + home_stats.get("pass_yards", 0),
            "away_yards": away_stats.get("rush_yards", 0) + away_stats.get("pass_yards", 0),
            "injuries": context["game_injuries"],
        }
        results_by_week.setdefault(week, [])
        results_by_week[week].append(result)
        print(f"[RESULT STORED] Week {week} now has {len(results_by_week[week])} game(s)")

        print(f"=== {home.abbreviation} vs {away.abbreviation} ===")
        print(f"Final Score: {result['home_score']} - {result['away_score']}")
        print(f"Total Yards: {result['home_yards']} / {result['away_yards']}")
        home_summary = summarize_stats(home, home_stats)
        away_summary = summarize_stats(away, away_stats)
        print("Home Key Stats:", home_summary)
        print("Away Key Stats:", away_summary)

        for summary in (home_summary, away_summary):
            qb = summary["qb"]
            rb = summary["rb"]
            wr = summary["wr"]
            if qb["pass_yards"] or qb["pass_td"]:
                stat_players["QB"] += 1
            if rb["rush_yards"] or rb["rush_td"]:
                stat_players["RB"] += 1
            if wr["rec_yards"] or wr["rec_td"]:
                stat_players["WR"] += 1
            if qb["pass_yards"] >= 300 and wr["rec_yards"] == 0:
                print(f"[STAT ISSUE] {wr['name']} has 0 rec_yards despite {qb['name']} throwing for {qb['pass_yards']}")
        if result["injuries"]:
            print("Injuries:")
            for inj in result["injuries"]:
                print(f"  {inj['player']} ({inj['team']}) - {inj['injury_type']} ({inj.get('severity','?')})")
                print(f"    Week: {week}")
            total_injuries += len(result["injuries"])
        print()

    print(f"Results stored in results_by_week[{week}]: {len(results_by_week.get(week, []))} game(s)")
    print(f"Total injuries this week: {total_injuries}")
    print("Players recording stats:")
    for pos, count in stat_players.items():
        print(f"  {pos}: {count}")

    update_league_state(teams, week, results_by_week)


if __name__ == "__main__":
    main()
