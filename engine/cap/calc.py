from __future__ import annotations

from pathlib import Path
import json
from typing import Dict

from ..models.contracts import ContractDTO, ContractYearTerm


def cap_hit(term: ContractYearTerm) -> int:
    return term.base + term.signingProrated + term.rosterBonus + term.workoutBonus


def dead_cap_on_cut(contract: ContractDTO, year: int, pre_june1: bool = True) -> int:
    remaining = [t for t in contract.terms if t.year >= year]
    if not remaining:
        raise ValueError("GG-CAP-1004: year outside contract")
    signing = sum(t.signingProrated for t in remaining)
    current = remaining[0]
    return signing + current.guaranteedBase


def team_cap_sheet(team_id: str, year: int) -> Dict:
    data_path = Path(__file__).resolve().parents[2] / "data" / "league_state.json"
    with open(data_path, "r", encoding="utf-8") as f:
        league = json.load(f)
    teams = {t["abbr"]: t for t in league.get("teams", [])}
    if team_id not in teams:
        raise ValueError("GG-CAP-1005: unknown team")
    team = teams[team_id]
    rows = []
    total = 0
    for player in team.get("players", []):
        contract_data = player.get("contract")
        if not contract_data:
            continue
        contract = ContractDTO(**contract_data)
        term = next((t for t in contract.terms if t.year == year), None)
        if term is None:
            continue
        hit = cap_hit(term)
        rows.append({"player": player["name"], "capHit": hit})
        total += hit
    return {"team": team_id, "year": year, "players": rows, "total": total}
