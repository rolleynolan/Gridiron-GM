"""Validated roster transactions owned by the rules layer."""

from __future__ import annotations

from typing import Any, Dict

from gridiron_gm_pkg.simulation.rules.contract_rules import (
    cap_summary,
    contract_payload,
    validate_contract_offer,
)


def _team_for_id(league: Any, team_id: Any) -> Any:
    key = str(team_id or "")
    for team in getattr(league, "teams", []) or []:
        if str(getattr(team, "id", "")) == key:
            return team
    return None


def _find_player(players: list[Any], player_id: Any) -> Any:
    key = str(player_id or "")
    return next((player for player in players if str(getattr(player, "id", "")) == key), None)


def _record(league: Any, action: str, team: Any, player: Any, details: Dict[str, Any] | None = None) -> Dict[str, Any]:
    log = getattr(league, "transaction_log", None)
    if not isinstance(log, list):
        log = []
        league.transaction_log = log
    entry = {
        "transaction_id": f"{action}:{len(log) + 1}",
        "action": action,
        "team_id": str(getattr(team, "id", "")),
        "player_id": str(getattr(player, "id", "")),
        "season": int(getattr(getattr(league, "calendar", None), "current_year", 0) or 0),
        "details": dict(details or {}),
    }
    log.append(entry)
    return entry


def sign_free_agent(league: Any, team_id: Any, player_id: Any, contract: Any) -> Dict[str, Any]:
    team = _team_for_id(league, team_id)
    if team is None:
        return {"ok": False, "error": "team_not_found"}
    free_agents = getattr(league, "free_agents", [])
    player = _find_player(free_agents, player_id)
    if player is None:
        return {"ok": False, "error": "free_agent_not_found"}
    if len(getattr(team, "roster", []) or []) >= int(getattr(team, "MAX_ROSTER_SIZE", 53) or 53):
        return {"ok": False, "error": "active_roster_full", "summary": cap_summary(team)}

    validation = validate_contract_offer(team, player, contract)
    if not validation["ok"]:
        return {"ok": False, "error": "contract_rejected", **validation}

    player.contract = contract_payload(contract)
    free_agents.remove(player)
    team.add_player(player)
    player.current_team = getattr(team, "id", None)
    transaction = _record(league, "sign", team, player, {"contract": player.contract})
    return {"ok": True, "transaction": transaction, "summary": cap_summary(team)}


def release_player(league: Any, team_id: Any, player_id: Any) -> Dict[str, Any]:
    team = _team_for_id(league, team_id)
    if team is None:
        return {"ok": False, "error": "team_not_found"}
    player = _find_player(getattr(team, "roster", []) or [], player_id)
    if player is None:
        return {"ok": False, "error": "active_roster_player_not_found"}

    prior_contract = contract_payload(getattr(player, "contract", None))
    team.remove_player(player)
    player.current_team = None
    player.contract = None
    free_agents = getattr(league, "free_agents", None)
    if not isinstance(free_agents, list):
        free_agents = []
        league.free_agents = free_agents
    free_agents.append(player)
    transaction = _record(league, "release", team, player, {"prior_contract": prior_contract})
    return {"ok": True, "transaction": transaction, "summary": cap_summary(team)}
