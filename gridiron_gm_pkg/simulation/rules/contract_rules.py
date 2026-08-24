"""Hard contract and salary-cap rules.

This layer validates financial state only.  Free agency, trade AI, and contract
negotiation may propose terms, but must use these rules before mutating a
roster or player contract.
"""

from __future__ import annotations

from dataclasses import asdict, is_dataclass
from typing import Any, Dict, Iterable


DEFAULT_SALARY_CAP = 255_000_000


def _as_int(value: Any, default: int = 0) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


def contract_payload(contract: Any) -> Dict[str, Any] | None:
    """Return one normalized, JSON-safe contract payload or ``None``."""
    if contract is None:
        return None
    if isinstance(contract, dict):
        raw = contract
    elif hasattr(contract, "to_dict"):
        raw = contract.to_dict()
    elif is_dataclass(contract) and not isinstance(contract, type):
        raw = asdict(contract)
    else:
        return None
    bonuses = raw.get("bonuses", {})
    if not isinstance(bonuses, dict):
        bonuses = {}
    return {
        "years": _as_int(raw.get("years", raw.get("years_remaining"))),
        "salary_per_year": _as_int(raw.get("salary_per_year", raw.get("salary"))),
        "guaranteed": _as_int(raw.get("guaranteed")),
        "bonuses": {str(key): _as_int(value) for key, value in bonuses.items()},
        "contract_type": str(raw.get("contract_type", "veteran") or "veteran"),
    }


def validate_contract(contract: Any) -> list[Dict[str, str]]:
    payload = contract_payload(contract)
    if payload is None:
        return [{"code": "invalid_contract", "message": "Contract must be structured data."}]
    errors: list[Dict[str, str]] = []
    if payload["years"] <= 0:
        errors.append({"code": "invalid_years", "message": "Contract years must be positive."})
    if payload["salary_per_year"] < 0:
        errors.append({"code": "invalid_salary", "message": "Annual salary cannot be negative."})
    if payload["guaranteed"] < 0:
        errors.append({"code": "invalid_guaranteed", "message": "Guaranteed money cannot be negative."})
    total_value = payload["years"] * payload["salary_per_year"] + sum(payload["bonuses"].values())
    if payload["guaranteed"] > total_value:
        errors.append({"code": "guarantee_exceeds_value", "message": "Guaranteed money exceeds contract value."})
    if any(value < 0 for value in payload["bonuses"].values()):
        errors.append({"code": "invalid_bonus", "message": "Bonuses cannot be negative."})
    return errors


def _team_players(team: Any) -> Iterable[Any]:
    seen: set[str] = set()
    for group_name in ("roster", "ir_list", "practice_squad"):
        for player in getattr(team, group_name, []) or []:
            player_id = str(getattr(player, "id", id(player)))
            if player_id not in seen:
                seen.add(player_id)
                yield player


def team_payroll(team: Any) -> int:
    return sum(
        (contract_payload(getattr(player, "contract", None)) or {}).get("salary_per_year", 0)
        for player in _team_players(team)
    )


def cap_summary(team: Any, salary_cap: Any = None) -> Dict[str, int]:
    cap = _as_int(salary_cap if salary_cap is not None else getattr(team, "salary_cap", None), DEFAULT_SALARY_CAP)
    if cap <= 0:
        cap = DEFAULT_SALARY_CAP
    payroll = team_payroll(team)
    return {"salary_cap": cap, "payroll": payroll, "cap_space": cap - payroll}


def validate_team_finances(team: Any, salary_cap: Any = None) -> list[Dict[str, str]]:
    errors: list[Dict[str, str]] = []
    for player in _team_players(team):
        contract = getattr(player, "contract", None)
        if contract is None:
            continue
        for error in validate_contract(contract):
            errors.append({**error, "player_id": str(getattr(player, "id", ""))})
    summary = cap_summary(team, salary_cap)
    if summary["payroll"] > summary["salary_cap"]:
        errors.append({"code": "salary_cap_exceeded", "message": "Team payroll exceeds the salary cap."})
    return errors


def validate_contract_offer(team: Any, player: Any, contract: Any, salary_cap: Any = None) -> Dict[str, Any]:
    """Validate proposed terms without changing team or player state."""
    errors = validate_contract(contract)
    proposed = contract_payload(contract)
    if proposed is None:
        return {"ok": False, "errors": errors, "summary": cap_summary(team, salary_cap)}
    current = contract_payload(getattr(player, "contract", None)) or {"salary_per_year": 0}
    summary = cap_summary(team, salary_cap)
    projected_payroll = summary["payroll"] - current["salary_per_year"] + proposed["salary_per_year"]
    if projected_payroll > summary["salary_cap"]:
        errors.append({"code": "salary_cap_exceeded", "message": "Proposed contract exceeds available cap space."})
    return {
        "ok": not errors,
        "errors": errors,
        "summary": {**summary, "projected_payroll": projected_payroll, "projected_cap_space": summary["salary_cap"] - projected_payroll},
    }
