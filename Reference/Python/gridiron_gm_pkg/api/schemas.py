from __future__ import annotations
from typing import Any, Dict, List, Optional, Tuple
from pydantic import BaseModel, Field

STATE_SCHEMA_VERSION = 1

REQUIRED_STATE_KEYS = {
    "ok": bool,
    "schema_version": int,
    "save_name": str,
    "save_path": str,
    "calendar": dict,
    "user": dict,
    "today": dict,
    "standings_summary": list,
    "inbox_summary": dict,
    "available_actions": list,
}


def validate_state(state: Dict[str, Any]) -> Tuple[bool, str]:
    """Legacy validation function for backwards compatibility with tests."""
    for key, expected_type in REQUIRED_STATE_KEYS.items():
        if key not in state:
            return False, f"missing_key:{key}"
        if not isinstance(state[key], expected_type):
            return False, f"bad_type:{key}"
    return True, ""


class StateSummary(BaseModel):
    """Summary of current game state for dashboard updates."""
    current_week: int = Field(..., ge=0)
    season: int = Field(..., ge=0)
    sim_seed: int = Field(default=42)


class StandingsRow(BaseModel):
    """Single team standings entry."""
    team_id: str
    team_name: str
    abbreviation: str
    wins: int
    losses: int
    ties: int = 0
    win_pct: float
    points_for: int
    points_against: int
    division: str = ""
    conference: str = ""


class StandingsResponse(BaseModel):
    """Response containing standings table."""
    ok: bool = True
    standings: List[StandingsRow]


class GameResultSummary(BaseModel):
    """Summary of a single game result or scheduled game."""
    game_id: str
    week: int
    calendar_week: int
    season_type: str
    season_week: int
    home_team: str
    away_team: str
    home_score: Optional[int] = None
    away_score: Optional[int] = None
    status: str


class WeekResultsResponse(BaseModel):
    """Response containing all results/games for a week."""
    week: int
    week_key: str
    week_label: str
    games: List[GameResultSummary]
    available_week_keys: List[str]
    available_week_labels: List[str]
    completed_week_keys: List[str]
    completed_week_labels: List[str]
    available_weeks: List[int]
    completed_weeks: List[int]


class ScheduleGame(BaseModel):
    """Game in a team's schedule."""
    game_id: Optional[str] = None
    week: int
    game_type: str = ""
    opponent: str = ""
    home_away: str = "home"
    status: str = "upcoming"
    home_team: str = ""
    away_team: str = ""
    home_score: Optional[int] = None
    away_score: Optional[int] = None
    winner: Optional[str] = None


class TeamScheduleResponse(BaseModel):
    """Response containing a team's full schedule."""
    ok: bool = True
    schedule: List[ScheduleGame]


class InjuryReportEntry(BaseModel):
    """Single injury report entry for a player."""
    player_id: Optional[str] = None
    name: str
    position: str
    overall: int = 0
    injury_status: str
    injury_name: Optional[str] = None
    injury_start_date: Optional[str] = None
    injury_end_date: Optional[str] = None
    days_remaining: Optional[int] = None
    on_injured_reserve: bool = False


class InjuryReportResponse(BaseModel):
    """Response containing a team's injury report."""
    team_id: str
    entries: List[InjuryReportEntry]


class NextUserGameResponse(BaseModel):
    """Response containing the next upcoming user team game."""
    game_id: Optional[str] = None
    week: Optional[int] = None
    opponent_id: Optional[str] = None
    home_away: Optional[str] = None
