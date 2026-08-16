from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any
import uuid


def _safe_int(value: Any, default: int) -> int:
    try:
        return int(value)
    except (TypeError, ValueError):
        return default


@dataclass(slots=True)
class CareerHistoryEntry:
    team_id: str = ""
    team_name: str = ""
    role: str = "General Manager"
    start_year: int = 0
    end_year: int | None = None
    wins: int = 0
    losses: int = 0
    playoff_appearances: int = 0
    championships: int = 0
    status: str = "active"

    def to_dict(self) -> dict[str, Any]:
        return {
            "team_id": self.team_id,
            "team_name": self.team_name,
            "role": self.role,
            "start_year": self.start_year,
            "end_year": self.end_year,
            "wins": self.wins,
            "losses": self.losses,
            "playoff_appearances": self.playoff_appearances,
            "championships": self.championships,
            "status": self.status,
        }

    @classmethod
    def from_dict(cls, data: Any) -> "CareerHistoryEntry":
        payload = data if isinstance(data, dict) else {}
        end_year = payload.get("end_year")
        if end_year in ("", None):
            normalized_end_year = None
        else:
            normalized_end_year = _safe_int(end_year, 0)
        return cls(
            team_id=str(payload.get("team_id") or ""),
            team_name=str(payload.get("team_name") or ""),
            role=str(payload.get("role") or "General Manager"),
            start_year=_safe_int(payload.get("start_year"), 0),
            end_year=normalized_end_year,
            wins=_safe_int(payload.get("wins"), 0),
            losses=_safe_int(payload.get("losses"), 0),
            playoff_appearances=_safe_int(payload.get("playoff_appearances"), 0),
            championships=_safe_int(payload.get("championships"), 0),
            status=str(payload.get("status") or "active"),
        )


@dataclass(slots=True)
class GMProfile:
    gm_id: str = field(default_factory=lambda: uuid.uuid4().hex)
    name: str = "User GM"
    current_team_id: str | None = None
    reputation: int = 50
    job_security: int = 50
    career_start_year: int = 0
    current_role: str = "General Manager"
    career_history: list[CareerHistoryEntry] = field(default_factory=list)
    traits: list[str] = field(default_factory=list)
    created_at_date: str | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "gm_id": self.gm_id,
            "name": self.name,
            "current_team_id": self.current_team_id,
            "reputation": self.reputation,
            "job_security": self.job_security,
            "career_start_year": self.career_start_year,
            "current_role": self.current_role,
            "career_history": [entry.to_dict() for entry in self.career_history],
            "traits": list(self.traits),
            "created_at_date": self.created_at_date,
        }

    @classmethod
    def from_dict(cls, data: Any) -> "GMProfile":
        payload = data if isinstance(data, dict) else {}
        raw_traits = payload.get("traits", [])
        raw_history = payload.get("career_history", [])
        return cls(
            gm_id=str(payload.get("gm_id") or uuid.uuid4().hex),
            name=str(payload.get("name") or "User GM"),
            current_team_id=str(payload.get("current_team_id")) if payload.get("current_team_id") else None,
            reputation=_safe_int(payload.get("reputation"), 50),
            job_security=_safe_int(payload.get("job_security"), 50),
            career_start_year=_safe_int(payload.get("career_start_year"), 0),
            current_role=str(payload.get("current_role") or "General Manager"),
            career_history=[
                CareerHistoryEntry.from_dict(entry)
                for entry in raw_history
                if isinstance(entry, dict)
            ],
            traits=[str(trait) for trait in raw_traits] if isinstance(raw_traits, list) else [],
            created_at_date=str(payload.get("created_at_date")) if payload.get("created_at_date") else None,
        )
