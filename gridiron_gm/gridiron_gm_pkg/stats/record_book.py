"""Utilities for tracking league records and leaderboards."""
from __future__ import annotations
from typing import Dict, Any, List, Tuple


def _ensure_structure(game_world: Dict[str, Any]) -> Dict[str, Any]:
    """Ensure the league_records structure exists and return it."""
    league_records = game_world.setdefault("league_records", {})
    players = league_records.setdefault("players", {})
    players.setdefault("single_game", {})
    players.setdefault("single_season", {})
    players.setdefault("career", {})

    teams = league_records.setdefault("teams", {})
    teams.setdefault("single_game", {})
    teams.setdefault("single_season", {})
    teams.setdefault("career", {})

    leaderboards = league_records.setdefault("leaderboards", {})
    leaderboards.setdefault("current_season", {})
    return league_records


def _update_record(store: Dict[str, Dict[str, Any]], stat: str, entity_key: str, entity_id: str, value: int | float) -> None:
    """Helper to update a record if ``value`` exceeds the current record."""
    record = store.get(stat)
    if not record or value > record.get("value", 0):
        store[stat] = {entity_key: entity_id, "value": value}


# ----- Player Records -----

def update_single_game_record(game_world: Dict[str, Any], player_id: str, stat_name: str, value: int | float) -> None:
    records = _ensure_structure(game_world)["players"]["single_game"]
    _update_record(records, stat_name, "player_id", player_id, value)


def update_single_season_record(game_world: Dict[str, Any], player_id: str, stat_name: str, value: int | float) -> None:
    records = _ensure_structure(game_world)["players"]["single_season"]
    _update_record(records, stat_name, "player_id", player_id, value)


def update_career_record(game_world: Dict[str, Any], player_id: str, stat_name: str, value: int | float) -> None:
    records = _ensure_structure(game_world)["players"]["career"]
    _update_record(records, stat_name, "player_id", player_id, value)


def update_leaderboard(game_world: Dict[str, Any], stat_name: str, player_id: str, value: int | float, limit: int = 10) -> None:
    boards = _ensure_structure(game_world)["leaderboards"]["current_season"]
    board: List[Tuple[str, int | float]] = boards.setdefault(stat_name, [])

    for i, (pid, val) in enumerate(board):
        if pid == player_id:
            if value > val:
                board[i] = (player_id, value)
            break
    else:
        board.append((player_id, value))

    board.sort(key=lambda x: x[1], reverse=True)
    del board[limit:]


# ----- Team Records -----

def update_team_single_game_record(game_world: Dict[str, Any], team_id: str, stat_name: str, value: int | float) -> None:
    records = _ensure_structure(game_world)["teams"]["single_game"]
    _update_record(records, stat_name, "team_id", team_id, value)


def update_team_single_season_record(game_world: Dict[str, Any], team_id: str, stat_name: str, value: int | float) -> None:
    records = _ensure_structure(game_world)["teams"]["single_season"]
    _update_record(records, stat_name, "team_id", team_id, value)


def update_team_career_record(game_world: Dict[str, Any], team_id: str, stat_name: str, value: int | float) -> None:
    records = _ensure_structure(game_world)["teams"]["career"]
    _update_record(records, stat_name, "team_id", team_id, value)

