"""Playoff management utilities."""

from __future__ import annotations

from typing import Any

from gridiron_gm import VERBOSE_SIM_OUTPUT
from gridiron_gm_pkg.simulation.engine.game_engine import simulate_game
from gridiron_gm_pkg.simulation.systems.core.data_loader import save_playoff_results

PLAYOFF_ROUND_SEQUENCE = (
    ("Wild Card", "Divisional"),
    ("Divisional", "Conference Championship"),
    ("Conference Championship", "Gridiron Bowl"),
    ("Gridiron Bowl", None),
)


def _is_placeholder_team(team_id: Any) -> bool:
    text = str(team_id or "").strip()
    return not text or text.upper().startswith("TBD")


def _iter_round_games(
    schedule_by_week: dict[str, Any],
    round_name: str,
    conference: str | None = None,
) -> list[dict[str, Any]]:
    games: list[dict[str, Any]] = []
    for games_in_week in schedule_by_week.values():
        if not isinstance(games_in_week, list):
            continue
        for game in games_in_week:
            if not isinstance(game, dict):
                continue
            if not game.get("playoff"):
                continue
            if game.get("round") != round_name:
                continue
            if conference is not None and game.get("conference") != conference:
                continue
            games.append(game)
    return games


def _find_result_for_game(results_by_week: dict[str, Any], game: dict[str, Any]) -> dict[str, Any] | None:
    week = str(game.get("calendar_week") or game.get("week") or "")
    if not week:
        return None
    game_id = f"{week}|{game.get('home_id')}|{game.get('away_id')}"
    results = results_by_week.get(week, [])
    if not isinstance(results, list):
        return None
    for result in results:
        if not isinstance(result, dict):
            continue
        if result.get("game_id") == game_id:
            return result
    home_id = str(game.get("home_id") or "")
    away_id = str(game.get("away_id") or "")
    for result in results:
        if not isinstance(result, dict):
            continue
        result_home = str(result.get("home_id") or result.get("home") or "")
        result_away = str(result.get("away_id") or result.get("away") or "")
        if result_home == home_id and result_away == away_id:
            return result
    return None


def _winner_info_for_game(results_by_week: dict[str, Any], game: dict[str, Any]) -> dict[str, Any] | None:
    result = _find_result_for_game(results_by_week, game)
    if result is None:
        return None
    home_score = result.get("home_score")
    away_score = result.get("away_score")
    if home_score is None or away_score is None or home_score == away_score:
        return None
    home_id = game.get("home_id")
    away_id = game.get("away_id")
    if home_score > away_score:
        return {
            "team_id": home_id,
            "seed": game.get("home_seed"),
            "conference": game.get("conference"),
        }
    return {
        "team_id": away_id,
        "seed": game.get("away_seed"),
        "conference": game.get("conference"),
    }


def _round_winners(
    schedule_by_week: dict[str, Any],
    results_by_week: dict[str, Any],
    round_name: str,
    conference: str | None = None,
) -> list[dict[str, Any]]:
    winners: list[dict[str, Any]] = []
    for game in _iter_round_games(schedule_by_week, round_name, conference):
        winner = _winner_info_for_game(results_by_week, game)
        if winner is not None:
            winners.append(winner)
    return winners


def _round_is_complete(
    schedule_by_week: dict[str, Any],
    results_by_week: dict[str, Any],
    round_name: str,
    conference: str | None = None,
) -> bool:
    games = _iter_round_games(schedule_by_week, round_name, conference)
    if not games:
        return False
    return all(_winner_info_for_game(results_by_week, game) is not None for game in games)


def _set_game_teams(game: dict[str, Any], home_id: str, away_id: str, id_to_abbr: dict[str, str]) -> bool:
    changed = False
    if game.get("home_id") != home_id:
        game["home_id"] = home_id
        changed = True
    if game.get("away_id") != away_id:
        game["away_id"] = away_id
        changed = True
    home_abbr = id_to_abbr.get(home_id, home_id)
    away_abbr = id_to_abbr.get(away_id, away_id)
    if game.get("home_abbr") != home_abbr:
        game["home_abbr"] = home_abbr
        changed = True
    if game.get("away_abbr") != away_abbr:
        game["away_abbr"] = away_abbr
        changed = True
    return changed


def _seed_value(team_info: dict[str, Any]) -> int:
    seed = team_info.get("seed")
    try:
        return int(seed)
    except (TypeError, ValueError):
        return 99


def _propagate_divisional_round(
    schedule_by_week: dict[str, Any],
    results_by_week: dict[str, Any],
    conference: str,
    id_to_abbr: dict[str, str],
) -> bool:
    winners = _round_winners(schedule_by_week, results_by_week, "Wild Card", conference)
    if len(winners) != 3:
        return False
    winners.sort(key=_seed_value)
    lowest_remaining_seed = max(winners, key=_seed_value)
    remaining = [winner for winner in winners if winner is not lowest_remaining_seed]
    remaining.sort(key=_seed_value)
    if len(remaining) != 2:
        return False

    divisional_games = _iter_round_games(schedule_by_week, "Divisional", conference)
    if len(divisional_games) != 2:
        return False
    divisional_games.sort(key=lambda game: 0 if int(game.get("home_seed") or 99) == 1 else 1)
    changed = False
    changed |= _set_game_teams(
        divisional_games[0],
        divisional_games[0].get("home_id"),
        lowest_remaining_seed["team_id"],
        id_to_abbr,
    )
    changed |= _set_game_teams(
        divisional_games[1],
        remaining[0]["team_id"],
        remaining[1]["team_id"],
        id_to_abbr,
    )
    divisional_games[0]["away_seed"] = lowest_remaining_seed.get("seed")
    divisional_games[1]["home_seed"] = remaining[0].get("seed")
    divisional_games[1]["away_seed"] = remaining[1].get("seed")
    return changed


def _propagate_conference_championship(
    schedule_by_week: dict[str, Any],
    results_by_week: dict[str, Any],
    conference: str,
    id_to_abbr: dict[str, str],
) -> bool:
    winners = _round_winners(schedule_by_week, results_by_week, "Divisional", conference)
    if len(winners) != 2:
        return False
    winners.sort(key=_seed_value)
    cc_games = _iter_round_games(schedule_by_week, "Conference Championship", conference)
    if len(cc_games) != 1:
        return False
    changed = _set_game_teams(cc_games[0], winners[0]["team_id"], winners[1]["team_id"], id_to_abbr)
    cc_games[0]["home_seed"] = winners[0].get("seed")
    cc_games[0]["away_seed"] = winners[1].get("seed")
    return changed


def _propagate_gridiron_bowl(
    schedule_by_week: dict[str, Any],
    results_by_week: dict[str, Any],
    id_to_abbr: dict[str, str],
) -> bool:
    nova = _round_winners(schedule_by_week, results_by_week, "Conference Championship", "Nova")
    atlas = _round_winners(schedule_by_week, results_by_week, "Conference Championship", "Atlas")
    if len(nova) != 1 or len(atlas) != 1:
        return False
    games = _iter_round_games(schedule_by_week, "Gridiron Bowl", "Both")
    if len(games) != 1:
        return False
    changed = _set_game_teams(games[0], nova[0]["team_id"], atlas[0]["team_id"], id_to_abbr)
    games[0]["home_seed"] = nova[0].get("seed")
    games[0]["away_seed"] = atlas[0].get("seed")
    return changed


def advance_playoff_schedule(
    schedule_by_week: dict[str, Any],
    results_by_week: dict[str, Any],
    id_to_abbr: dict[str, str],
) -> bool:
    """Propagate completed playoff winners into the next round."""
    changed = False
    for conference in ("Nova", "Atlas"):
        if _round_is_complete(schedule_by_week, results_by_week, "Wild Card", conference):
            changed |= _propagate_divisional_round(schedule_by_week, results_by_week, conference, id_to_abbr)
        if _round_is_complete(schedule_by_week, results_by_week, "Divisional", conference):
            changed |= _propagate_conference_championship(schedule_by_week, results_by_week, conference, id_to_abbr)
    if (
        _round_is_complete(schedule_by_week, results_by_week, "Conference Championship", "Nova")
        and _round_is_complete(schedule_by_week, results_by_week, "Conference Championship", "Atlas")
    ):
        changed |= _propagate_gridiron_bowl(schedule_by_week, results_by_week, id_to_abbr)
    if changed and VERBOSE_SIM_OUTPUT:
        print("[DEBUG] Propagated playoff winners into future rounds.")
    return changed


def update_playoff_schedule(schedule_by_week, playoff_results, round_name, next_round_name, conference):
    """Legacy wrapper retained for older call sites."""
    round_results_by_week: dict[str, list[dict[str, Any]]] = {"legacy": []}
    for game in playoff_results.get(round_name, []):
        if conference != "Both" and game.get("conference") != conference:
            continue
        round_results_by_week["legacy"].append(dict(game))
    id_to_abbr: dict[str, str] = {}
    for games in schedule_by_week.values():
        if not isinstance(games, list):
            continue
        for game in games:
            if not isinstance(game, dict):
                continue
            home_id = game.get("home_id")
            away_id = game.get("away_id")
            if home_id and game.get("home_abbr"):
                id_to_abbr[str(home_id)] = game.get("home_abbr")
            if away_id and game.get("away_abbr"):
                id_to_abbr[str(away_id)] = game.get("away_abbr")
    advance_playoff_schedule(schedule_by_week, round_results_by_week, id_to_abbr)


class PlayoffManager:
    """Orchestrates playoff simulation across all rounds."""

    def __init__(self, season_manager):
        self.season_manager = season_manager

    def run_playoffs(self):
        sm = self.season_manager
        results = {"Nova": [], "Atlas": [], "Championship": None}
        weeks = sorted(int(w) for w in sm.schedule_by_week.keys())

        for round_name, _next_round in PLAYOFF_ROUND_SEQUENCE:
            for week in weeks:
                week_str = str(week)
                games = sm.schedule_by_week.get(week_str, [])
                for game in games:
                    if not game.get("playoff") or game.get("round") != round_name:
                        continue
                    if _is_placeholder_team(game.get("home_id")) or _is_placeholder_team(game.get("away_id")):
                        continue
                    home_id = game["home_id"]
                    away_id = game["away_id"]
                    home_team = sm.id_to_team.get(home_id)
                    away_team = sm.id_to_team.get(away_id)
                    sim_home, sim_away = simulate_game(
                        home_team,
                        away_team,
                        week=week,
                        context={"weather": None, "current_date": sm.calendar.current_date},
                    )
                    home_score = sim_home.get("points", sim_home.get("score", 0))
                    away_score = sim_away.get("points", sim_away.get("score", 0))
                    if home_score == away_score:
                        home_score += 3
                    game["home_score"] = home_score
                    game["away_score"] = away_score
                    result = {
                        "home_id": home_id,
                        "away_id": away_id,
                        "home_score": home_score,
                        "away_score": away_score,
                        "conference": game.get("conference"),
                        "round": round_name,
                        "final_score": f"{sm.id_to_abbr.get(home_id, home_id)} {home_score} - {sm.id_to_abbr.get(away_id, away_id)} {away_score}",
                        "result_str": f"{sm.id_to_abbr.get(home_id, home_id)} {home_score}, {sm.id_to_abbr.get(away_id, away_id)} {away_score}",
                    }
                    if round_name == "Gridiron Bowl":
                        results["Championship"] = result
                        champ_id = home_id if home_score > away_score else away_id
                        sm.champion = sm.id_to_abbr.get(champ_id, champ_id)
                        sm.runner_up = sm.id_to_abbr.get(
                            away_id if champ_id == home_id else home_id,
                            away_id if champ_id == home_id else home_id,
                        )
                    else:
                        conf = game.get("conference")
                        results.setdefault(conf, []).append(result)
            advance_playoff_schedule(sm.schedule_by_week, sm.results_by_week, sm.id_to_abbr)

        save_playoff_results(results, sm.save_name)
        sm.playoff_results = results
        return results
