import json
from pathlib import Path
from typing import Any, Dict

from gridiron_gm_pkg.simulation.entities.player import ensure_pot

SCHEMA_VERSION = 1
DEBUG_POT_BACKFILL = False


def migrate(data: Dict[str, Any]) -> Dict[str, Any]:
    if "schema_version" not in data:
        data = {"schema_version": 0, "league": data}

    version = data.get("schema_version", 0)
    if version == 0:
        return {"schema_version": SCHEMA_VERSION, "league": data.get("league", {})}
    return data


def save_league(path: str | Path, league: Any) -> None:
    payload = {
        "schema_version": SCHEMA_VERSION,
        "league": league.to_dict() if hasattr(league, "to_dict") else league,
    }
    save_path = Path(path)
    save_path.parent.mkdir(parents=True, exist_ok=True)
    with save_path.open("w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)


def load_league(path: str | Path):
    save_path = Path(path)
    with save_path.open("r", encoding="utf-8") as f:
        data = json.load(f)
    migrated = migrate(data)
    league_data = migrated.get("league", {})
    from gridiron_gm_pkg.simulation.entities.league import LeagueManager
    league = LeagueManager.from_dict(league_data)
    from gridiron_gm_pkg.simulation.systems.player.injury_status import (
        convert_legacy_injury_fields,
        iter_league_players,
    )

    current_date = getattr(league.calendar, "current_date", None)
    for player in iter_league_players(league):
        convert_legacy_injury_fields(player, current_date)
    backfilled = 0
    for team in league.teams:
        for player in team.roster + team.ir_list + team.practice_squad:
            if ensure_pot(player, "pro"):
                backfilled += 1
    for player in getattr(league, "free_agents", []):
        if ensure_pot(player, "pro"):
            backfilled += 1
    for player in getattr(league, "draft_prospects", []):
        if ensure_pot(player, "college"):
            backfilled += 1
    for player in getattr(league, "college_db", []):
        if ensure_pot(player, "college"):
            backfilled += 1
    if DEBUG_POT_BACKFILL and backfilled:
        print(f"[savegame] Backfilled pot for {backfilled} players.")
    return league
