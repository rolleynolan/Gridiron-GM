import random
import os
from typing import Dict, Any, List, Optional
from collections import defaultdict
from gridiron_gm.gridiron_gm_pkg.simulation.systems.roster.depth_chart import generate_depth_chart
from gridiron_gm.gridiron_gm_pkg.config.formations import FORMATION_SCHEMES
from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.fatigue import FatigueSystem
from gridiron_gm.gridiron_gm_pkg.simulation.engine.stat_utils import merge_player_stats, get_top_performers

# ==== Simulation Factor Stubs ====
# These functions are placeholders for more advanced simulation logic.
# You can expand them to include real calculations based on player ratings, weather, etc

def apply_roster_skill(offense: Dict[str, Any], defense: Dict[str, Any], play_context: Dict[str, Any]) -> float:
    """Stub: Returns a modifier based on overall roster skill."""
    return 1.0

def apply_coaching(offense: Dict[str, Any], defense: Dict[str, Any], play_context: Dict[str, Any]) -> float:
    """Stub: Returns a modifier based on coaching quality."""
    return 1.0

def apply_player_matchups(offense: Dict[str, Any], defense: Dict[str, Any], play_context: Dict[str, Any]) -> float:
    """Stub: Returns a modifier based on individual player matchups."""
    return 1.0

def apply_schemes(offense: Dict[str, Any], defense: Dict[str, Any], play_context: Dict[str, Any]) -> float:
    """Stub: Returns a modifier based on offensive and defensive schemes."""
    return 1.0

def apply_weather(weather: Optional[Dict[str, Any]], play_context: Dict[str, Any]) -> float:
    """Stub: Returns a modifier based on weather conditions."""
    return 1.0

def apply_home_field_advantage(home_team: Any, away_team: Any, play_context: Dict[str, Any]) -> float:
    """Stub: Returns a modifier for home field advantage."""
    return 1.0

# ---- Fatigue Integration (updated) ----

fatigue_system = FatigueSystem()

def inject_fatigue(team) -> None:
    """
    Ensures all players on a team have a fatigue attribute.
    """
    for player in team.roster:
        if not hasattr(player, "fatigue"):
            player.fatigue = fatigue_system.min_fatigue

def apply_fatigue(player, base_cost: float) -> float:
    """
    Applies fatigue to a player using the FatigueSystem, scaling by play intensity.
    """
    fatigue_system.add_fatigue(player, play_intensity=base_cost)
    # Support both dict and object
    if isinstance(player, dict):
        return player.get("fatigue", 0.0)
    else:
        return getattr(player, "fatigue", 0.0)

def recover_fatigue(team, context: str = "between_plays", is_on_field: bool = False) -> None:
    """
    Recovers fatigue for all players on a team, using context and on-field status.
    """
    for player in team.roster:
        fatigue_system.recover(player, context=context, is_on_field=is_on_field)

def passive_line_fatigue(team, positions: tuple = ("LT", "LG", "C", "RG", "RT"), cost: float = 1.5) -> None:
    """
    Applies passive fatigue to offensive linemen or other specified positions.
    """
    for player in team.roster:
        if getattr(player, "position", None) in positions:
            apply_fatigue(player, cost)

# ---- Performance & Injury Modifiers ----

def get_performance_modifier(player: Any) -> float:
    """
    Returns a performance modifier for a player based on fatigue and other factors.
    """
    return fatigue_system.performance_modifier(player)

def get_injury_risk_modifier(player: Any) -> float:
    """
    Returns an injury risk modifier for a player based on fatigue and other factors.
    """
    return fatigue_system.injury_risk_modifier(player)

# ---- Player Selection & Play Simulation ----

def select_fresh_player(depth_chart_list, threshold: float = 0.7, last_used=None):
    """
    Selects the freshest (least fatigued) player from a depth chart list, above a threshold.
    If all are fatigued, returns the primary (first) player.
    Optionally logs a substitution if last_used is provided.
    """
    if not depth_chart_list:
        return None
    primary = depth_chart_list[0]
    selected = None
    for player in depth_chart_list:
        if getattr(player, "fatigue", 0.0) < threshold:
            selected = player
            break
    selected = selected or primary
    if last_used and getattr(selected, "name", None) != getattr(last_used, "name", None):
        selected.subbed_in = f"[SUB] {getattr(last_used, 'name', 'Unknown')} (fatigue {getattr(last_used, 'fatigue', 0.0):.2f}) → {getattr(selected, 'name', 'Unknown')}"
    return selected

def simulate_pass_play(qb: Any, wr_list: List[Any], depth: str, context: Dict[str, Any]) -> Dict[str, Any]:
    """
    Simulates a pass play with realistic NFL yardage and big-play chance.
    """
    # NFL average pass: 6.5 yards/attempt, but allow for big plays and incompletions
    completion_chance = {"short": 0.78, "medium": 0.62, "deep": 0.38}[depth]
    base_range = {"short": (3, 8), "medium": (8, 18), "deep": (18, 40)}[depth]
    big_play_chance = 0.08 if depth == "deep" else 0.03

    modifier = (
        apply_roster_skill(context["offense"], context["defense"], context) *
        apply_coaching(context["offense"], context["defense"], context) *
        apply_player_matchups(context["offense"], context["defense"], context) *
        apply_schemes(context["offense"], context["defense"], context) *
        apply_weather(context.get("weather"), context) *
        apply_home_field_advantage(context.get("home_team"), context.get("away_team"), context)
    )
    completion_chance *= modifier

    receiver = select_fresh_player(wr_list)
    wr_name = getattr(receiver, "name", "Unknown")
    qb_name = getattr(qb, "name", "Unknown")

    apply_fatigue(qb, 2)
    apply_fatigue(receiver, 4 + {"short": 0, "medium": 1, "deep": 2}[depth] * 1.5)
    qb_perf = get_performance_modifier(qb)
    wr_perf = get_performance_modifier(receiver)
    completion_chance *= (qb_perf + wr_perf) / 2

    is_complete = random.random() < completion_chance
    sub_log = getattr(receiver, "subbed_in", "")

    if is_complete:
        if random.random() < big_play_chance:
            yards = random.randint(base_range[1]+1, base_range[1]+30)
        else:
            yards = random.randint(base_range[0], base_range[1])
        yards = int(yards * wr_perf)
        log = f"{sub_log + ' ' if sub_log else ''}{qb_name} completed a {depth} pass to {wr_name} for {yards} yards"
        stats = {
            qb_name: {"pass_attempts": 1, "completions": 1, "pass_yards": yards, "player_obj": qb},
            wr_name: {"receptions": 1, "rec_yards": yards, "player_obj": receiver}
        }
        from .play_time_model import estimate_play_seconds
        avg_speed = (
            getattr(qb, "speed", getattr(qb, "overall", 85)) +
            getattr(receiver, "speed", getattr(receiver, "overall", 85))
        ) / 2
        time = estimate_play_seconds("pass", yards, completed=True, player_speed=avg_speed)
    else:
        yards = 0
        log = f"{sub_log + ' ' if sub_log else ''}{qb_name} attempted a {depth} pass to {wr_name} — incomplete"
        stats = {qb_name: {"pass_attempts": 1, "completions": 0, "player_obj": qb}}
        from .play_time_model import estimate_play_seconds
        avg_speed = (
            getattr(qb, "speed", getattr(qb, "overall", 85)) +
            getattr(receiver, "speed", getattr(receiver, "overall", 85))
        ) / 2
        time = estimate_play_seconds("pass", 0, completed=False, player_speed=avg_speed)

    from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.injury_manager import create_injury

    # --- Injury logic for QB and WR ---
    for player in [qb, receiver]:
        fatigue = getattr(player, "fatigue", 0.0)
        risk_mod = get_injury_risk_modifier(player)
        # Lower base injury chance, scale up for severe fatigue
        base_injury_chance = 0.002
        if fatigue > 0.95:
            base_injury_chance *= 3  # triple risk if extremely fatigued
        elif fatigue > 0.8:
            base_injury_chance *= 2  # double risk if very fatigued
        injury_chance = base_injury_chance * (1 + fatigue) * risk_mod

        if random.random() < injury_chance:
            injury_obj = create_injury(player, context="game", severity_roll=random.random())
            if injury_obj and "game_injuries" in context:
                context["game_injuries"].append({
                    "player": getattr(player, "name", "Unknown"),
                    "team": getattr(getattr(player, "team", None), "abbreviation", None),
                    "injury_type": getattr(injury_obj, "name", str(injury_obj)),
                    "severity": getattr(injury_obj, "severity", "unknown"),
                    "weeks_out": getattr(injury_obj, "weeks_out", None)
                })

    return {"yards": yards, "log": log.strip(), "player_stats": stats, "seconds_burned": time}

def simulate_run_play(runner: Any, gap: str, context: Dict[str, Any]) -> Dict[str, Any]:
    """
    Simulates a run play with realistic NFL yardage and big-play chance.
    """
    # NFL average run: 4.2 yards, but allow for big plays and losses
    base_range = {"inside": (1, 6), "outside": (2, 8)}[gap]
    big_play_chance = 0.06  # ~6% of runs are 15+ yards
    loss_chance = 0.07      # ~7% of runs lose yards

    if random.random() < big_play_chance:
        yards = random.randint(15, 40)
    elif random.random() < loss_chance:
        yards = -random.randint(1, 4)
    else:
        yards = random.randint(base_range[0], base_range[1])

    apply_fatigue(runner, 5 if gap == "inside" else 7)
    perf = get_performance_modifier(runner)
    yards = int(yards * perf)

    # Short-yardage conversion (e.g., 3rd/4th and 1)
    down = context.get("down", 1)
    to_go = context.get("to_go", 10)
    if to_go <= 2 and down in (3, 4):
        power_chance = 0.7 if gap == "inside" else 0.5
        if random.random() < power_chance:
            yards = max(yards, to_go + 1)  # Conversion

    # ...injury logic unchanged...

    name = getattr(runner, "name", "Unknown")
    sub_note = getattr(runner, "subbed_in", "")
    log = f"{sub_note} {name} ran {gap} for {yards} yards".strip()
    stats = {name: {"carries": 1, "rush_yards": yards, "player_obj": runner}}
    from .play_time_model import estimate_play_seconds
    speed = getattr(runner, "speed", getattr(runner, "overall", 85))
    time = estimate_play_seconds("run", yards, player_speed=speed)
    return {"yards": yards, "log": log, "player_stats": stats, "seconds_burned": time}

# Rename run_play to sim_play
def sim_play(offense, defense, down, to_go, yardline, sub_manager, fatigue_log, context):
    """
    Simulates a single play, selecting run/pass based on down, distance, score, clock, and weather.
    Weather impacts pass efficiency and completion.
    Returns play result dict.
    """
    package = getattr(offense, "package", "standard")
    scheme = {"QB": 1, "RB": 1, "WR": 2, "TE": 1, "LT": 1, "LG": 1, "C": 1, "RG": 1, "RT": 1}
    formation = scheme.get("formation", {})
    lineup, sub_log = sub_manager.get_active_lineup_with_bench_log(formation, offense, fatigue_log, scheme)

    for p in lineup.values():
        players = p if isinstance(p, list) else [p]
        for player in players:
            apply_fatigue(player, 1.0)

    for log in sub_log:
        fatigue_log.append(f"[SUB] {log}")

    # --- Intelligent play selection ---
    play_type = choose_play_type_intelligent(
        offense=offense,
        down=down,
        to_go=to_go,
        yardline=yardline,
        context=context
    )

    play_result = {}

    # --- Enhanced weather impact ---
    weather = context.get("weather", {})
    weather_mod = apply_weather_enhanced(weather, play_type)
    
    context["offense"] = offense
    context["defense"] = defense

    if play_type == "run":
        runner = lineup.get("RB1") or next((p for k, p in lineup.items() if getattr(p, "position", None) == "RB"), None)
        if runner:
            play_result = simulate_run_play(runner, gap="inside", context=context)
        else:
            play_result = {"desc": "No RB available", "yards": 0}
    else:
        qb = lineup.get("QB")
        wr = lineup.get("WR1")
        if isinstance(qb, list):
            qb = qb[0] if qb else None
        if isinstance(wr, list):
            wr = wr[0] if wr else None
        if qb and wr:
            play_result = simulate_pass_play(qb, [wr], depth="short", context=context)
            # Apply weather modifier to pass completion/yards
            if "yards" in play_result:
                play_result["yards"] = int(play_result["yards"] * weather_mod)
            play_result["weather_mod"] = weather_mod
            # Simulate increased incompletion in bad weather
            if weather_mod < 1.0 and play_result.get("yards", 0) > 0:
                # Chance to turn a completion into an incompletion
                if random.random() > weather_mod:
                    play_result["yards"] = 0
                    play_result["log"] = f"{getattr(qb, 'name', 'QB')} pass to {getattr(wr, 'name', 'WR')} fell incomplete due to weather"
        else:
            play_result = {"desc": "Missing QB or WR", "yards": 0}

    play_result["play_type"] = play_type
    play_result["down"] = down
    play_result["distance"] = to_go
    play_result["yardline"] = yardline
    play_result["weather"] = weather.get("type") if weather else None

    return play_result

def choose_play_type_intelligent(offense, down, to_go, yardline, context):
    """
    Chooses play type (run/pass) based on down, distance, score, clock, and weather.
    Always expects weather to have 'type' and 'wind_speed' keys.
    """
    score_diff = context.get("score_diff", 0)  # offense - defense
    clock = context.get("clock", 900)  # seconds left in half/quarter
    weather = context.get("weather", {})

    # Ensure weather has required keys
    weather_type = weather.get("type", "clear")
    wind_speed = weather.get("wind_speed", 0)
    severe_weather = weather_type in ("rain", "snow") or wind_speed > 20

    # Down/distance logic
    if to_go >= 8:
        if severe_weather:
            return "run" if random.random() < 0.5 else "pass"
        return "pass" if random.random() < 0.8 else "run"
    elif down == 1:
        if to_go <= 3:
            return "run" if random.random() < 0.7 else "pass"
        return "run" if random.random() < 0.6 else "pass"
    elif down in [2, 3] and to_go <= 3:
        return "run" if random.random() < 0.75 else "pass"
    elif down == 4:
        if to_go <= 2:
            return "run" if random.random() < 0.7 else "pass"
        return "pass"
    if clock < 120 and score_diff < 0:
        return "pass"
    if clock < 120 and score_diff > 0:
        return "run" if random.random() < 0.8 else "pass"
    return "pass" if random.random() < 0.55 else "run"

def apply_weather_enhanced(weather, play_type):
    """
    Returns a modifier for play effectiveness based on weather and play type.
    - Passing is less effective in rain, snow, or high wind.
    - Running is less affected.
    """
    if not weather or play_type != "pass":
        return 1.0
    mod = 1.0
    if weather.get("type") == "rain":
        mod -= 0.18
    elif weather.get("type") == "snow":
        mod -= 0.22
    if weather.get("wind_speed", 0) > 15:
        mod -= 0.10
    return max(mod, 0.7)  # Don't reduce below 70% effectiveness

# Rename run_drive to sim_drive
def sim_drive(offense, defense, sub_mgr, fatigue_log, context, start_field_pos=25):
    """
    Simulates a drive with NFL-like play-by-play logic and realistic drive-ending conditions.
    Drives end only on: touchdown, field goal (made/missed), punt, turnover (INT/fumble), turnover on downs, safety, or time expiration.
    There is no arbitrary play cap; drives can be as long or short as real NFL drives.
    """
    from gridiron_gm.gridiron_gm_pkg.simulation.engine.penalty_engine import simulate_play as simulate_penalty_play

    def to_penalty_player(player):
        # Map your player object to the penalty engine's expected Player dataclass
        return type("PenaltyPlayer", (), {
            "name": getattr(player, "name", "Unknown"),
            "position": getattr(player, "position", "UNK"),
            "discipline_rating": getattr(player, "discipline_rating", 75),
            "traits": sum([v for v in getattr(player, "traits", {}).values() if isinstance(v, list)], [])
        })()

    drive_log = []
    player_stats = {}
    score = 0
    field_pos = start_field_pos
    plays = 0
    down = 1
    to_go = 10
    stalled = False
    penalties = 0
    penalty_yards = 0
    punts = 0
    sacks = 0
    fumbles = 0
    ints = 0
    fg_attempted = False
    fg_made = False
    explosive_plays = 0
    defensive_tds = 0
    special_tds = 0

    drive_team_stats = {
        "rush_attempts": 0,
        "rush_yards": 0,
        "rush_td": 0,
        "pass_attempts": 0,
        "completions": 0,
        "pass_yards": 0,
        "pass_td": 0,
        "interceptions": 0,
        "fumbles_lost": 0,
        "penalties": 0,
        "penalty_yards": 0,
        "explosive_plays": 0,
        "def_td": 0,
        "ret_td": 0,
    }

    from gridiron_gm.gridiron_gm_pkg.simulation.systems.roster.substitution_manager import SubstitutionManagerV2

    # For drive time simulation accumulate actual seconds burned per play
    drive_seconds = 0
    max_drive_seconds = context.get("max_drive_seconds", 600)  # e.g., 10 minutes max for a drive (rarely reached)

    # Main drive loop: ends only on NFL drive-ending conditions
    while True:
        # --- Build 11-man lineups for both sides using depth chart and SubstitutionManagerV2 ---
        package = getattr(offense, "package", "standard")
        scheme = {"QB": 1, "RB": 1, "WR": 2, "TE": 1, "LT": 1, "LG": 1, "C": 1, "RG": 1, "RT": 1}
        formation = scheme.get("formation", {})
        lineup, sub_log = sub_mgr.get_active_lineup_with_bench_log(formation, offense, fatigue_log, scheme)
        offense_lineup = lineup.get("offense")
        defense_lineup = lineup.get("defense")

        # Defensive: Ensure lineups are valid lists of player objects
        if offense_lineup is None or defense_lineup is None:
            drive_log.append("[ERROR] sim_drive: offense_lineup or defense_lineup is None! Aborting drive.")
            break
        if not isinstance(offense_lineup, list):
            offense_lineup = list(offense_lineup.values()) if hasattr(offense_lineup, "values") else [offense_lineup]
        if not isinstance(defense_lineup, list):
            defense_lineup = list(defense_lineup.values()) if hasattr(defense_lineup, "values") else [defense_lineup]

        if not offense_lineup or not defense_lineup:
            drive_log.append("[ERROR] sim_drive: offense_lineup or defense_lineup is empty! Aborting drive.")
            break

        # --- Penalty simulation for all on-field players ---
        on_field_players = offense_lineup + defense_lineup
        penalty_events = simulate_penalty_play(
            on_field_players,
            offense=offense,
            defense=defense,
            down=down,
            to_go=to_go,
            yardline=field_pos,
            context=context
        )
        penalty_applied = False
        if penalty_events:
            for pen in penalty_events:
                pen_team = pen.get("team", "offense")
                pen_player = pen.get("player")
                pen_type = pen.get("penalty_type", pen.get("type", "?"))
                pen_yards = pen.get("yards", 0)
                auto_first_down = pen.get("auto_first", False)
                replay_down = pen.get("replay_down", False)
                injury = pen.get("injury", None)
                drive_log.append(
                    f"PENALTY: {pen_type} on {getattr(pen_player, 'position', '?')} ({getattr(pen_player, 'name', '?')}), {pen_yards:+} yards"
                    + (" [Auto 1st down]" if auto_first_down else "")
                    + (" [Replay down]" if replay_down else "")
                    + (f" [INJURY: {injury}]" if injury else "")
                )
                penalties += 1
                penalty_yards += abs(pen_yards)
                drive_team_stats['penalties'] += 1
                drive_team_stats['penalty_yards'] += abs(pen_yards)
                if pen_team == "offense":
                    field_pos = max(1, field_pos - abs(pen_yards))
                else:
                    field_pos = min(99, field_pos + abs(pen_yards))
                if auto_first_down:
                    down = 1
                    to_go = 10
                if replay_down:
                    penalty_applied = True
            if penalty_applied:
                continue  # Redo play after replay-down penalty
        else:
            drive_log.append("No penalties this play.")

        # --- Play selection: weighted, but can be replaced with attribute logic ---
        third_down_conversion_chance = 0.41
        fourth_down_conversion_chance = 0.50
        red_zone = field_pos >= 60
        goal_to_go = field_pos >= 70
        play_type = "pass" if (down in [2, 3] and to_go > 7) or (random.random() < 0.56) else "run"

        yards_gained = 0
        play_desc = ""
        turnover = False
        td_type = None

        # --- Simulate play outcome ---
        if play_type == "run":
            drive_team_stats["rush_attempts"] += 1
            # Explosive run: ~4% of all runs
            if random.random() < 0.04:
                yards_gained = random.randint(10, 45)
                play_desc = f"Explosive run for {yards_gained} yards"
                explosive_plays += 1
            else:
                # Normal run: mean 5.2, stddev 2.5, clamp -2 to 13 (NFL avg ~4.7 ypc, +10% boost)  # [BOOSTED]
                yards_gained = int(random.gauss(5.2, 2.5))  # was 4.7
                yards_gained = max(-2, min(yards_gained, 13))  # was -3,12
                play_desc = f"Run for {yards_gained} yards"
            drive_team_stats["rush_yards"] += yards_gained
            # Fumble lost: ~1.2% of runs (+20% from 0.01)  # [BOOSTED]
            if random.random() < 0.012:
                fumbles += 1
                drive_team_stats["fumbles_lost"] += 1
                drive_log.append(f"Fumble lost on run! Turnover.")
                turnover = True
                stalled = True
            # Rushing TD: heavily weighted to inside 10, rare outside
            if field_pos + yards_gained >= 80:
                td_chance = 0.40 if field_pos >= 70 else 0.045  # was 0.35/0.04, +~15% [BOOSTED]
                if random.random() < td_chance:
                    drive_team_stats["rush_td"] += 1
                    play_desc += " (Rushing touchdown!)"
                    td_type = "rush"
                    score = 7
                    break
        else:  # pass
            drive_team_stats["pass_attempts"] += 1
            # Sack: ~6% of pass plays
            if random.random() < 0.06:
                sack_yards = int(random.gauss(7, 2))
                sack_yards = max(1, min(sack_yards, 15))
                yards_gained = -sack_yards
                sacks += 1
                play_desc = f"QB sacked for -{sack_yards} yards"
            else:
                # Completion: ~66% NFL average (unchanged)
                if random.random() < 0.66:
                    drive_team_stats["completions"] += 1
                    # Explosive pass: ~5% of passes
                    if random.random() < 0.05:
                        yards_gained = random.randint(20, 60)
                        play_desc = f"Explosive pass complete for {yards_gained} yards"
                        explosive_plays += 1
                    else:
                        # Normal pass: mean 10, stddev 7, clamp 0 to 30 (NFL avg ~10 ypc)
                        yards_gained = int(random.gauss(10, 7))
                        yards_gained = max(0, min(yards_gained, 30))
                        play_desc = f"Pass complete for {yards_gained} yards"
                    drive_team_stats["pass_yards"] += yards_gained
                    # Interception: ~2.7% of passes (+~23% from 0.022)  # [BOOSTED]
                    if random.random() < 0.027:
                        ints += 1
                        drive_team_stats["interceptions"] += 1
                        # Rare pick-six: ~10% of INTs
                        if random.random() < 0.10:
                            drive_log.append(f"Intercepted! Pick-six! Defensive touchdown.")
                            defensive_tds += 1
                            drive_team_stats["def_td"] += 1
                            score = 0
                            td_type = "def"
                            break
                        else:
                            drive_log.append(f"Intercepted! Turnover.")
                            turnover = True
                            stalled = True
                    # Passing TD: ~60% of all TDs, weighted to inside 20 (unchanged)
                    if field_pos + yards_gained >= 80:
                        td_chance = 0.60 if field_pos >= 60 else 0.03
                        if random.random() < td_chance:
                            drive_team_stats["pass_td"] += 1
                            play_desc += " (Passing touchdown!)"
                            td_type = "pass"
                            score = 7
                            break
                else:
                    play_desc = "Incomplete pass"
                    yards_gained = 0

        # Rare defensive/special teams TDs (1–2% of drives)
        if not td_type and not turnover and random.random() < 0.012:
            if random.random() < 0.7:
                drive_log.append("Fumble return for touchdown! Defensive TD.")
                defensive_tds += 1
                drive_team_stats["def_td"] += 1
            else:
                drive_log.append("Kick/punt return for touchdown! Special teams TD.")
                special_tds += 1
                drive_team_stats["ret_td"] += 1
            score = 7
            td_type = "def"
            break

        # Update field position and downs
        prev_field_pos = field_pos
        field_pos += yards_gained
        field_pos = max(1, min(field_pos, 99))
        drive_log.append(f"Play {plays+1}: {play_type.upper()} - {play_desc} | Ball at {field_pos}")

        # 3rd/4th down conversion logic
        if yards_gained >= to_go:
            down = 1
            to_go = 10
            drive_log.append(f"First down!")
        elif yards_gained > 0:
            to_go -= yards_gained
            down += 1
        elif yards_gained <= 0:
            down += 1

        plays += 1

        # --- Drive-ending conditions (NFL rules) ---
        # Turnover (INT/fumble)
        if turnover:
            break

        # Touchdown (if not already handled)
        if field_pos >= 80 and not td_type:
            if random.random() < 0.18:
                if random.random() < 0.6:
                    drive_team_stats["pass_td"] += 1
                    drive_log.append("Passing touchdown!")
                    td_type = "pass"
                else:
                    drive_team_stats["rush_td"] += 1
                    drive_log.append("Rushing touchdown!")
                    td_type = "rush"
                score = 7
            else:
                fg_attempted = True
                fg_distance = 100 - field_pos
                # Reduce long FG attempts and lower make chance from 40+ yards
                if fg_distance < 40:
                    fg_chance = 0.93
                elif fg_distance < 50:
                    fg_chance = 0.75
                elif fg_distance < 55:
                    fg_chance = 0.60
                else:
                    fg_chance = 0.20  # Very low chance for 55+ yards
                # Only attempt FG if distance < 55
                if fg_distance < 55 and random.random() < fg_chance:
                    drive_log.append(f"Field Goal is good! ({fg_distance} yards)")
                    fg_made = True
                    score = 3
                else:
                    drive_log.append(f"Field Goal missed from {fg_distance} yards.")
            break

        # Fourth down logic (outside red zone)
        if down > 4:
            fg_distance = 100 - field_pos
            # Go for it logic: trailing late, short distance, or aggressive team (stub: random for now)
            go_for_it = False
            if field_pos >= 65 and to_go <= 2 and random.random() < 0.25:
                go_for_it = True
            if go_for_it:
                # Simulate 4th down conversion
                conversion_chance = 0.50 if to_go <= 2 else 0.20
                if random.random() < conversion_chance:
                    down = 1
                    to_go = 10
                    drive_log.append("4th down conversion successful!")
                    continue
                else:
                    drive_log.append("Turnover on downs.")
                    break
            elif field_pos >= 45:
                # Reduce long FG attempts and lower make chance from 40+ yards
                if fg_distance < 40:
                    fg_chance = 0.93
                elif fg_distance < 50:
                    fg_chance = 0.75
                elif fg_distance < 55:
                    fg_chance = 0.60
                else:
                    fg_chance = 0.20  # Very low chance for 55+ yards
                fg_attempted = True
                # Only attempt FG if distance < 55
                if fg_distance < 55 and random.random() < fg_chance:
                    drive_log.append(f"Field Goal is good! ({fg_distance} yards)")
                    fg_made = True
                    score = 3
                else:
                    drive_log.append(f"Field Goal missed from {fg_distance} yards.")
                break
            else:
                net_punt = int(random.gauss(41, 5))
                net_punt = max(20, min(net_punt, 60))
                inside_20 = random.random() < 0.35
                punts += 1
                drive_log.append(f"Punt: {net_punt} yards{' (inside 20)' if inside_20 else ''}.")
                field_pos = 100 - (field_pos + net_punt)
                break

        # Safety (ball behind own goal line)
        if field_pos <= 0:
            drive_log.append("Safety! Defense scores 2 points.")
            score = -2
            safety = 1
            break

        # End of half/game (optional: use drive_seconds if simulating clock)
        from .play_time_model import estimate_play_seconds
        if play_type == "run":
            runner = next((p for p in offense_lineup if getattr(p, "position", "") == "RB"), None)
            speed = getattr(runner, "speed", getattr(runner, "overall", 85)) if runner else 85
            play_seconds = estimate_play_seconds("run", yards_gained, player_speed=speed)
        else:
            qb = next((p for p in offense_lineup if getattr(p, "position", "") == "QB"), None)
            wr = next((p for p in offense_lineup if getattr(p, "position", "") == "WR"), None)
            if qb or wr:
                avg_speed = ((getattr(qb, "speed", getattr(qb, "overall", 85)) if qb else 85) +
                             (getattr(wr, "speed", getattr(wr, "overall", 85)) if wr else 85)) / 2
            else:
                avg_speed = 85
            completed = yards_gained > 0 and "Incomplete" not in play_desc
            play_seconds = estimate_play_seconds("pass", yards_gained, completed=completed, player_speed=avg_speed)
        drive_seconds += play_seconds
        if drive_seconds >= max_drive_seconds:
            drive_log.append("End of half/game: drive stopped by clock.")
            break

    # Track drive summary
    drive_log.append(f"Drive summary: {plays} plays, {field_pos - start_field_pos} yards, "
                     f"{'TD' if score == 7 else 'FG' if score == 3 else 'No score'}, "
                     f"{ints} INT, {fumbles} FUM, {sacks} SACK, {punts} PUNT, {penalties} PEN, {explosive_plays} EXP")

    # Attach drive_team_stats to player_stats for aggregation in simulate_game
    player_stats["_drive_team_stats"] = drive_team_stats

    # For drive stats validation, return number of plays as well
    return {
        "score": score,
        "log": drive_log,
        "player_stats": player_stats,
        "turnovers": ints + fumbles,
        "penalties": penalties,
        "penalty_yards": penalty_yards,
        "sacks": sacks,
        "punts": punts,
        "fg_attempted": fg_attempted,
        "fg_made": fg_made,
        "end_field_pos": field_pos,
        "explosive_plays": explosive_plays,
        "def_td": defensive_tds,
        "ret_td": special_tds,
        "plays": plays,
        "drive_seconds": drive_seconds
    }

def simulate_game(home_team, away_team, week=1, context=None):
    """
    Simulates a full NFL game between home_team and away_team.
    Alternates possessions, tracks score and stats, and returns (home_stats, away_stats).
    """
    if context is None:
        context = {}
    from gridiron_gm.gridiron_gm_pkg.simulation.systems.roster.substitution_manager import SubstitutionManagerV2

    # Game parameters
    NUM_QUARTERS = 4
    QUARTER_SECONDS = 900  # 15 minutes per quarter
    TOTAL_SECONDS = NUM_QUARTERS * QUARTER_SECONDS
    clock = TOTAL_SECONDS

    # Possession alternates: 1st half - home, away, home, away...; 2nd half - away, home, away, home...
    possession_order = []
    for half in range(2):
        for i in range(8):  # 8 drives per half (typical NFL average)
            if half == 0:
                possession_order.append(home_team if i % 2 == 0 else away_team)
            else:
                possession_order.append(away_team if i % 2 == 0 else home_team)

    # Substitution managers
    home_sub_mgr = SubstitutionManagerV2(getattr(home_team, "depth_chart", {}))
    away_sub_mgr = SubstitutionManagerV2(getattr(away_team, "depth_chart", {}))

    # Fatigue logs
    home_fatigue_log = []
    away_fatigue_log = []

    # Stats
    home_stats = defaultdict(int)
    away_stats = defaultdict(int)
    home_stats["team"] = getattr(home_team, "abbreviation", "HOME")
    away_stats["team"] = getattr(away_team, "abbreviation", "AWAY")
    home_stats["log"] = []
    away_stats["log"] = []

    # Game state
    possession_idx = 0
    current_pos_team = home_team
    other_team = away_team
    current_sub_mgr = home_sub_mgr
    other_sub_mgr = away_sub_mgr
    current_fatigue_log = home_fatigue_log
    other_fatigue_log = away_fatigue_log
    current_stats = home_stats
    other_stats = away_stats
    field_pos = 25  # Start at own 25

    # Main game loop
    while clock > 0 and possession_idx < len(possession_order):
        # Alternate possession
        current_pos_team = possession_order[possession_idx]
        other_team = home_team if current_pos_team is away_team else away_team
        current_sub_mgr = home_sub_mgr if current_pos_team is home_team else away_sub_mgr
        other_sub_mgr = away_sub_mgr if current_pos_team is home_team else home_sub_mgr
        current_fatigue_log = home_fatigue_log if current_pos_team is home_team else away_fatigue_log
        other_fatigue_log = away_fatigue_log if current_pos_team is home_team else home_fatigue_log
        current_stats = home_stats if current_pos_team is home_team else away_stats

        # Simulate drive
        drive_context = dict(context)
        drive_context["clock"] = clock
        drive_result = sim_drive(
            offense=current_pos_team,
            defense=other_team,
            sub_mgr=current_sub_mgr,
            fatigue_log=current_fatigue_log,
            context=drive_context,
            start_field_pos=field_pos
        )

        # Update stats
        current_stats["points"] += drive_result.get("score", 0)
        current_stats["turnovers"] += drive_result.get("turnovers", 0)
        current_stats["penalties"] += drive_result.get("penalties", 0)
        current_stats["penalty_yards"] += drive_result.get("penalty_yards", 0)
        current_stats["sacks"] += drive_result.get("sacks", 0)
        current_stats["punts"] += drive_result.get("punts", 0)
        current_stats["fg"] += int(drive_result.get("fg_made", False))
        current_stats["rush_td"] += drive_result.get("player_stats", {}).get("_drive_team_stats", {}).get("rush_td", 0)
        current_stats["pass_td"] += drive_result.get("player_stats", {}).get("_drive_team_stats", {}).get("pass_td", 0)
        current_stats["completions"] += drive_result.get("player_stats", {}).get("_drive_team_stats", {}).get("completions", 0)
        current_stats["pass_attempts"] += drive_result.get("player_stats", {}).get("_drive_team_stats", {}).get("pass_attempts", 0)
        current_stats["rush_yards"] += drive_result.get("player_stats", {}).get("_drive_team_stats", {}).get("rush_yards", 0)
        current_stats["pass_yards"] += drive_result.get("player_stats", {}).get("_drive_team_stats", {}).get("pass_yards", 0)
        current_stats["safety"] += drive_result.get("safety", 0)
        current_stats["log"].extend(drive_result.get("log", []))

        # Advance clock by the seconds actually burned during the drive
        drive_seconds = drive_result.get("drive_seconds", drive_result.get("plays", 0) * 40)
        clock -= drive_seconds
        if clock < 0:
            clock = 0

        # Next possession
        possession_idx += 1
        field_pos = 25  # Reset to own 25 after each score/change

    # Fill in missing stats for compatibility
    for stat in [
        "rush_td", "pass_td", "def_td", "ret_td", "fg", "explosive_plays", "plays", "turnovers",
        "penalties", "penalty_yards", "sacks", "punts",
        "rush_yards", "pass_yards", "completions", "pass_attempts", "safety"  # <-- add "safety"
    ]:
        home_stats.setdefault(stat, 0)
        away_stats.setdefault(stat, 0)

    # Calculate completion percentage for compatibility
    for stats in (home_stats, away_stats):
        attempts = stats.get("pass_attempts", 0)
        completions = stats.get("completions", 0)
        if attempts > 0:
            stats["completion_pct"] = completions / attempts
        else:
            stats["completion_pct"] = 0.0

    return dict(home_stats), dict(away_stats)

# --- After batch simulation, print summary stats for realism validation ---
def print_sim_summary(stats):
    total_drives = len(stats["points"])
    avg_points = sum(stats["points"]) / total_drives
    total_rush_td = sum(stats["rush_td"])
    total_pass_td = sum(stats["pass_td"])
    total_fg = sum(stats["fg"])
    total_def_td = sum(stats.get("def_td", [0]*total_drives))
    total_ret_td = sum(stats.get("ret_td", [0]*total_drives))
    total_exp = sum(stats.get("explosive_plays", [0]*total_drives))
    total_td = total_rush_td + total_pass_td + total_def_td + total_ret_td
    avg_plays = sum(stats.get("plays", [0]*total_drives)) / total_drives if "plays" in stats else 0
    max_plays = max(stats.get("plays", [0]*total_drives)) if "plays" in stats else 0
    print(f"\n[SIM SUMMARY]")
    print(f"Avg points per drive: {avg_points:.2f}")
    print(f"TDs: {total_td} (Pass: {total_pass_td}, Rush: {total_rush_td}, Def: {total_def_td}, Ret: {total_ret_td})")
    if total_td > 0:
        print(f"  Pass TD %: {100*total_pass_td/total_td:.1f}%")
        print(f"  Rush TD %: {100*total_rush_td/total_td:.1f}%")
        print(f"  Def/Ret TD %: {100*(total_def_td+total_ret_td)/total_td:.1f}%")
    print(f"FGs: {total_fg}")
    print(f"Explosive plays (20+ pass/10+ run): {total_exp} ({100*total_exp/(total_drives*6):.2f}% of plays est.)")
    print(f"Avg plays per drive: {avg_plays:.2f}, Max plays in a drive: {max_plays}")

# --- Notes ---
# - Drive now models real NFL rules for ending: no arbitrary play cap, ends only on TD, FG, punt, turnover, downs, safety, or clock.
# - Handles long or short drives organically, with realistic play/drive distributions.