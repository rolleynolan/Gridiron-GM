from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


def _safe_str(value: Any, default: str = "") -> str:
    if value is None:
        return default
    return str(value)


def _safe_bool(value: Any, default: bool = False) -> bool:
    if value is None:
        return default
    return bool(value)


def _safe_int(value: Any, default: int = 0) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


@dataclass
class DecisionItem:
    decision_id: str = ""
    created_at_date: str = ""
    created_at_time: str = ""
    category: str = ""
    decision_type: str = ""
    title: str = ""
    message: str = ""
    priority: int = 50
    status: str = "open"
    blocks_advancement: bool = False
    options: List[Dict[str, Any]] = field(default_factory=list)
    payload: Dict[str, Any] = field(default_factory=dict)
    resolved_at_date: Optional[str] = None
    resolved_at_time: Optional[str] = None
    selected_option: Optional[str] = None

    def to_dict(self) -> Dict[str, Any]:
        return {
            "decision_id": _safe_str(self.decision_id),
            "created_at_date": _safe_str(self.created_at_date),
            "created_at_time": _safe_str(self.created_at_time),
            "category": _safe_str(self.category),
            "decision_type": _safe_str(self.decision_type),
            "title": _safe_str(self.title),
            "message": _safe_str(self.message),
            "priority": _safe_int(self.priority, 50),
            "status": _safe_str(self.status, "open"),
            "blocks_advancement": _safe_bool(self.blocks_advancement, False),
            "options": [dict(option) for option in (self.options or []) if isinstance(option, dict)],
            "payload": dict(self.payload or {}),
            "resolved_at_date": self.resolved_at_date,
            "resolved_at_time": self.resolved_at_time,
            "selected_option": self.selected_option,
        }

    @classmethod
    def from_dict(cls, data: Dict[str, Any] | None) -> "DecisionItem":
        payload = data if isinstance(data, dict) else {}
        options = payload.get("options")
        if not isinstance(options, list):
            options = []
        raw_payload = payload.get("payload")
        if not isinstance(raw_payload, dict):
            raw_payload = {}
        return cls(
            decision_id=_safe_str(payload.get("decision_id")),
            created_at_date=_safe_str(payload.get("created_at_date")),
            created_at_time=_safe_str(payload.get("created_at_time")),
            category=_safe_str(payload.get("category")),
            decision_type=_safe_str(payload.get("decision_type")),
            title=_safe_str(payload.get("title")),
            message=_safe_str(payload.get("message")),
            priority=_safe_int(payload.get("priority"), 50),
            status=_safe_str(payload.get("status"), "open") or "open",
            blocks_advancement=_safe_bool(payload.get("blocks_advancement"), False),
            options=[dict(option) for option in options if isinstance(option, dict)],
            payload=dict(raw_payload),
            resolved_at_date=payload.get("resolved_at_date"),
            resolved_at_time=payload.get("resolved_at_time"),
            selected_option=payload.get("selected_option"),
        )
