from __future__ import annotations

from typing import Any, Dict, List

from gridiron_gm_pkg.simulation.systems.player.injury_status import is_available_for_game


def review_roster_rules(league: Any, team_id: str, context: Dict[str, Any] | None = None) -> Dict[str, Any]:
    context_data = dict(context or {})
    return {
        "team_id": str(team_id or ""),
        "hard_violations": get_roster_rule_violations(league, team_id, context=context_data),
        "advisories": get_roster_advisories(league, team_id, context=context_data),
    }


def get_roster_rule_violations(
    league: Any,
    team_id: str,
    context: Dict[str, Any] | None = None,
) -> List[Dict[str, Any]]:
    context_data = dict(context or {})
    team = _get_team(league, team_id)
    if team is None:
        return []

    active_players = list(getattr(team, "roster", []) or [])
    active_count = len(active_players)
    active_limit = getattr(team, "MAX_ROSTER_SIZE", None)
    violations: List[Dict[str, Any]] = []

    if isinstance(active_limit, int) and active_limit > 0 and active_count > active_limit:
        violations.append(
            _issue(
                "active_roster_over_limit",
                "blocking",
                "Active Roster Over Limit",
                f"You have {active_count} active players. The active roster limit is {active_limit}. You must reduce the active roster before advancing.",
                {
                    "team_id": getattr(team, "id", team_id),
                    "active_count": active_count,
                    "active_roster_limit": active_limit,
                },
            )
        )

    if not _is_game_day_check(context_data):
        return violations

    if active_count <= 0:
        return violations

    for position, rule_id, title in (
        ("QB", "no_healthy_qb_for_game", "No Healthy QB Available"),
        ("K", "no_healthy_k_for_game", "No Healthy K Available"),
        ("P", "no_healthy_p_for_game", "No Healthy P Available"),
    ):
        if _has_active_healthy_player(active_players, position):
            continue
        violations.append(
            _issue(
                rule_id,
                "blocking",
                title,
                f"You have no active healthy {position} available for the upcoming game. You must fix the roster before advancing.",
                {
                    "team_id": getattr(team, "id", team_id),
                    "position": position,
                    "active_healthy_count": 0,
                },
            )
        )

    return violations


def get_roster_advisories(
    league: Any,
    team_id: str,
    context: Dict[str, Any] | None = None,
) -> List[Dict[str, Any]]:
    _ = context
    team = _get_team(league, team_id)
    if team is None:
        return []

    active_players = list(getattr(team, "roster", []) or [])
    counts = _position_counts(active_players)
    advisories: List[Dict[str, Any]] = []

    if counts.get("LT", 0) < 2:
        advisories.append(
            _issue(
                "thin_lt_depth",
                "advisory",
                "Assistant GM Note: LT Depth",
                "We only have one true LT on the active roster. We may want to add depth before the regular season.",
                {"team_id": getattr(team, "id", team_id), "position": "LT", "active_count": counts.get("LT", 0)},
            )
        )
    if counts.get("QB", 0) > 3:
        advisories.append(
            _issue(
                "excess_qb_depth",
                "advisory",
                "Assistant GM Note: QB Depth",
                f"We currently have {counts.get('QB', 0)} QBs on the active roster. We may be carrying more quarterback depth than we need.",
                {"team_id": getattr(team, "id", team_id), "position": "QB", "active_count": counts.get("QB", 0)},
            )
        )
    if counts.get("K", 0) > 1:
        advisories.append(
            _issue(
                "excess_k_depth",
                "advisory",
                "Assistant GM Note: K Depth",
                f"We currently have {counts.get('K', 0)} kickers on the active roster. One roster spot may be tied up at a specialist position.",
                {"team_id": getattr(team, "id", team_id), "position": "K", "active_count": counts.get("K", 0)},
            )
        )
    if counts.get("P", 0) > 1:
        advisories.append(
            _issue(
                "excess_p_depth",
                "advisory",
                "Assistant GM Note: P Depth",
                f"We currently have {counts.get('P', 0)} punters on the active roster. One roster spot may be tied up at a specialist position.",
                {"team_id": getattr(team, "id", team_id), "position": "P", "active_count": counts.get("P", 0)},
            )
        )
    if counts.get("CB", 0) < 4:
        advisories.append(
            _issue(
                "thin_cb_depth",
                "advisory",
                "Assistant GM Note: CB Depth",
                "Cornerback depth looks thin. We may want another body there before attrition hits.",
                {"team_id": getattr(team, "id", team_id), "position": "CB", "active_count": counts.get("CB", 0)},
            )
        )
    if counts.get("WR", 0) < 5:
        advisories.append(
            _issue(
                "thin_wr_depth",
                "advisory",
                "Assistant GM Note: WR Depth",
                "Wide receiver depth looks thin for a full season workload. We may want another option there.",
                {"team_id": getattr(team, "id", team_id), "position": "WR", "active_count": counts.get("WR", 0)},
            )
        )
    return advisories


def _issue(rule_id: str, severity: str, title: str, message: str, payload: Dict[str, Any]) -> Dict[str, Any]:
    issue_payload = dict(payload or {})
    issue_payload["rule_id"] = rule_id
    return {
        "rule_id": rule_id,
        "severity": severity,
        "title": title,
        "message": message,
        "payload": issue_payload,
    }


def _get_team(league: Any, team_id: str) -> Any:
    if not team_id or league is None:
        return None
    id_map = getattr(league, "id_to_team", None)
    if isinstance(id_map, dict) and team_id in id_map:
        return id_map.get(team_id)
    for team in getattr(league, "teams", []) or []:
        if str(getattr(team, "id", "")) == str(team_id):
            return team
    return None


def _is_game_day_check(context: Dict[str, Any]) -> bool:
    token = str(context.get("check_type") or "").strip().lower()
    return bool(context.get("game_day_check")) or token in {"game_day", "pregame", "pre_game"}


def _position_counts(players: List[Any]) -> Dict[str, int]:
    counts: Dict[str, int] = {}
    for player in players:
        position = _normalize_position(getattr(player, "position", None))
        counts[position] = counts.get(position, 0) + 1
    return counts


def _has_active_healthy_player(players: List[Any], position: str) -> bool:
    normalized = _normalize_position(position)
    for player in players:
        if _normalize_position(getattr(player, "position", None)) != normalized:
            continue
        if is_available_for_game(player):
            return True
    return False


def _normalize_position(value: Any) -> str:
    return str(value or "").strip().upper() or "UNK"
