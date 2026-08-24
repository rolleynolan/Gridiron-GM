import random
from typing import Any, Dict, List


def _split_points(total: int, rng: random.Random) -> List[int]:
    if total <= 0:
        return [0, 0, 0, 0]
    cuts = sorted(rng.randint(0, total) for _ in range(3))
    q1, q2, q3 = cuts
    return [q1, q2 - q1, q3 - q2, total - q3]


def _format_time(seconds: int) -> str:
    seconds = max(0, int(seconds))
    minutes = seconds // 60
    secs = seconds % 60
    return f"{minutes:02d}:{secs:02d}"


def sanitize_box_score_numbers(obj: Any) -> Any:
    if isinstance(obj, dict):
        for key, value in obj.items():
            obj[key] = sanitize_box_score_numbers(value)
        return obj
    if isinstance(obj, list):
        for idx, value in enumerate(obj):
            obj[idx] = sanitize_box_score_numbers(value)
        return obj
    if isinstance(obj, float):
        rounded = round(obj)
        if abs(obj - rounded) < 1e-9:
            return int(rounded)
        return obj
    return obj


def _stat_int(value: Any, minimum: int = 0, maximum: int | None = None) -> int:
    try:
        number = int(round(float(value)))
    except (TypeError, ValueError):
        number = minimum
    if number < minimum:
        number = minimum
    if maximum is not None and number > maximum:
        number = maximum
    return number


def _resolve_team(league: Any, team_id: str) -> Any | None:
    if league is None or not team_id:
        return None
    id_to_team = getattr(league, "id_to_team", None)
    if isinstance(id_to_team, dict) and team_id in id_to_team:
        return id_to_team[team_id]
    for team in getattr(league, "teams", []) or []:
        if getattr(team, "id", None) == team_id:
            return team
    return None


def _team_label(team: Any, team_id: str) -> str:
    if team is not None:
        label = getattr(team, "abbreviation", None) or getattr(team, "team_name", None)
        if label:
            return str(label)
    return str(team_id)


def _get_player_attr(player: Any, key: str) -> Any:
    if isinstance(player, dict):
        return player.get(key)
    return getattr(player, key, None)


def _pick_player(team: Any, positions: set[str], rng: random.Random) -> Any | None:
    roster = getattr(team, "roster", None) if team is not None else None
    roster = roster if isinstance(roster, list) else []
    candidates = []
    for player in roster:
        pos = _get_player_attr(player, "position")
        if positions and pos in positions:
            candidates.append(player)
    if not candidates:
        candidates = roster
    if not candidates:
        return None
    return rng.choice(candidates)


def _player_name(player: Any, fallback: str) -> str:
    name = _get_player_attr(player, "name") if player is not None else None
    if name:
        return str(name)
    fallback_id = _get_player_attr(player, "id") if player is not None else None
    if fallback_id:
        return str(fallback_id)
    return fallback


def _generate_team_stats(score: int, rng: random.Random) -> Dict[str, Any]:
    score = _stat_int(score)
    total_yards = _stat_int(250 + rng.randint(0, 170) + score * 4, minimum=180, maximum=650)
    pass_share = rng.uniform(0.45, 0.7)
    pass_yards = _stat_int(total_yards * pass_share, maximum=total_yards)
    rush_yards = _stat_int(total_yards - pass_yards)
    return {
        "total_yards": _stat_int(total_yards),
        "pass_yards": _stat_int(pass_yards),
        "rush_yards": _stat_int(rush_yards),
        "turnovers": _stat_int(rng.randint(0, 3)),
        "sacks": _stat_int(rng.randint(0, 6)),
        "penalties": _stat_int(rng.randint(3, 12)),
    }


def _generate_passing_leader(
    team: Any,
    pass_yards: int,
    score: int,
    rng: random.Random,
    team_label: str,
) -> Dict[str, Any]:
    player = _pick_player(team, {"QB"}, rng)
    attempts = rng.randint(22, 42)
    completions = rng.randint(max(8, int(attempts * 0.5)), max(10, int(attempts * 0.75)))
    yards = max(0, int(pass_yards * (0.7 + rng.random() * 0.25)))
    tds = max(0, min(5, score // 7 + rng.randint(0, 1)))
    return {
        "player": _player_name(player, f"{team_label} QB"),
        "yards": yards,
        "td": tds,
        "int": rng.randint(0, 2),
        "comp": completions,
        "att": attempts,
    }


def _generate_rushing_leader(
    team: Any,
    rush_yards: int,
    score: int,
    rng: random.Random,
    team_label: str,
) -> Dict[str, Any]:
    player = _pick_player(team, {"RB", "FB", "QB"}, rng)
    carries = rng.randint(10, 26)
    yards = max(0, int(rush_yards * (0.4 + rng.random() * 0.35)))
    tds = max(0, min(4, score // 7))
    return {
        "player": _player_name(player, f"{team_label} RB"),
        "yards": yards,
        "td": tds,
        "carries": carries,
    }


def _generate_receiving_leader(
    team: Any,
    pass_yards: int,
    score: int,
    rng: random.Random,
    team_label: str,
) -> Dict[str, Any]:
    player = _pick_player(team, {"WR", "TE", "RB"}, rng)
    receptions = rng.randint(3, 11)
    yards = max(0, int(pass_yards * (0.35 + rng.random() * 0.35)))
    tds = max(0, min(3, score // 7))
    return {
        "player": _player_name(player, f"{team_label} WR"),
        "yards": yards,
        "td": tds,
        "receptions": receptions,
    }


def generate_box_score(
    home_id: str,
    away_id: str,
    home_score: int,
    away_score: int,
    *,
    league: Any = None,
    rng: random.Random | None = None,
) -> Dict[str, Any]:
    rng = rng or random.Random()
    home_score = _stat_int(home_score)
    away_score = _stat_int(away_score)
    home_team = _resolve_team(league, home_id)
    away_team = _resolve_team(league, away_id)
    home_label = _team_label(home_team, home_id)
    away_label = _team_label(away_team, away_id)

    home_quarters = _split_points(home_score, rng)
    away_quarters = _split_points(away_score, rng)
    quarters = [
        {"home": home_quarters[idx], "away": away_quarters[idx]}
        for idx in range(4)
    ]

    home_stats = _generate_team_stats(home_score, rng)
    away_stats = _generate_team_stats(away_score, rng)
    home_possession = rng.randint(24 * 60, 36 * 60)
    away_possession = 60 * 60 - home_possession
    home_stats["time_of_possession"] = _format_time(home_possession)
    away_stats["time_of_possession"] = _format_time(away_possession)

    return {
        "final": {"home": home_score, "away": away_score},
        "quarters": quarters,
        "team_stats": {"home": home_stats, "away": away_stats},
        "leaders": {
            "home": {
                "passing": _generate_passing_leader(
                    home_team,
                    home_stats["pass_yards"],
                    int(home_score),
                    rng,
                    home_label,
                ),
                "rushing": _generate_rushing_leader(
                    home_team,
                    home_stats["rush_yards"],
                    int(home_score),
                    rng,
                    home_label,
                ),
                "receiving": _generate_receiving_leader(
                    home_team,
                    home_stats["pass_yards"],
                    int(home_score),
                    rng,
                    home_label,
                ),
            },
            "away": {
                "passing": _generate_passing_leader(
                    away_team,
                    away_stats["pass_yards"],
                    int(away_score),
                    rng,
                    away_label,
                ),
                "rushing": _generate_rushing_leader(
                    away_team,
                    away_stats["rush_yards"],
                    int(away_score),
                    rng,
                    away_label,
                ),
                "receiving": _generate_receiving_leader(
                    away_team,
                    away_stats["pass_yards"],
                    int(away_score),
                    rng,
                    away_label,
                ),
            },
        },
    }
