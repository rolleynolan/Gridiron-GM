from __future__ import annotations

import datetime
import random
import uuid
from dataclasses import dataclass, field
from typing import Any, Dict, Iterable, List, Optional, Tuple

from gridiron_gm_pkg.simulation.career.decision_item import DecisionItem
from gridiron_gm_pkg.simulation.roster.roster_rules import review_roster_rules
from gridiron_gm_pkg.simulation.utils.box_score import (
    generate_box_score,
    sanitize_box_score_numbers,
)
from gridiron_gm_pkg.simulation.systems.core.data_loader import save_results

def _clamp_hour(value: int) -> int:
    return max(0, min(23, int(value)))


def _normalize_day_name(day: str) -> str:
    return str(day or "").strip().capitalize()


def _normalize_phase_name(phase: Any) -> str:
    text = str(phase or "").strip().lower().replace("-", "_").replace(" ", "_")
    if text == "regular":
        return "regular_season"
    return text


def _is_placeholder_team_ref(value: Any) -> bool:
    text = str(value or "").strip()
    return not text or text.upper().startswith("TBD")


def _derive_seed(base_seed: int, label: str) -> int:
    seed = int(base_seed) & 0xFFFFFFFF
    for ch in str(label):
        seed = (seed * 31 + ord(ch)) & 0xFFFFFFFF
    return seed


def _normalize_event_type(event_type: Any) -> str:
    token = str(event_type or "").strip()
    lowered = token.lower().replace("-", "_").replace(" ", "_")
    if lowered in {"gamekickoff", "kickoff", "game", "game_simulation", "scheduled_game", "game_kickoff"}:
        return "GameKickoff"
    if lowered in {"gamewrap", "game_wrap", "game_final", "game_result", "game_result_finalize"}:
        return "GameWrap"
    if lowered == "phasechange":
        return "PhaseChange"
    if lowered == "tradedeadline":
        return "TradeDeadline"
    if lowered == "inboxcheck":
        return "InboxCheck"
    if lowered == "trainingslot":
        return "TrainingSlot"
    if lowered == "travel":
        return "Travel"
    return token


@dataclass
class GameClock:
    current_date: datetime.date
    hour: int = 0

    @property
    def current_time(self) -> datetime.time:
        return datetime.time(hour=_clamp_hour(self.hour), minute=0)

    @property
    def current_time_str(self) -> str:
        return self.current_time.strftime("%H:%M")

    @property
    def current_datetime(self) -> datetime.datetime:
        return datetime.datetime.combine(self.current_date, self.current_time)

    def advance_hour(self, calendar: Any) -> None:
        self.hour += 1
        if self.hour >= 24:
            self.hour = 0
            if hasattr(calendar, "advance_day"):
                calendar.advance_day()
                self.current_date = getattr(calendar, "current_date", self.current_date)
            else:
                self.current_date = self.current_date + datetime.timedelta(days=1)

    def serialize(self) -> Dict[str, Any]:
        return {
            "current_date": self.current_date.isoformat(),
            "current_time": self.current_time_str,
            "hour": self.hour,
        }

    @classmethod
    def deserialize(cls, data: Dict[str, Any], fallback_date: datetime.date) -> "GameClock":
        date_str = data.get("current_date")
        if date_str:
            try:
                current_date = datetime.date.fromisoformat(date_str)
            except ValueError:
                current_date = fallback_date
        else:
            current_date = fallback_date
        hour = data.get("hour")
        if hour is None:
            hour = parse_clock_hour(data.get("current_time"))
        return cls(current_date=current_date, hour=_clamp_hour(hour or 0))


@dataclass
class SimEvent:
    id: int
    date: datetime.date
    hour: int
    type: str
    payload: Dict[str, Any] = field(default_factory=dict)
    priority: int = 100

    def sort_key(self) -> Tuple[int, int, int, int]:
        return (self.date.toordinal(), self.hour, self.priority, self.id)

    def serialize(self) -> Dict[str, Any]:
        return {
            "id": self.id,
            "date": self.date.isoformat(),
            "time": f"{self.hour:02d}:00",
            "hour": self.hour,
            "type": self.type,
            "payload": self.payload,
            "priority": self.priority,
        }

    @classmethod
    def deserialize(cls, data: Dict[str, Any]) -> "SimEvent":
        return cls(
            id=int(data.get("id", 0)),
            date=datetime.date.fromisoformat(data["date"]),
            hour=_clamp_hour(data.get("hour", 0)),
            type=_normalize_event_type(data.get("type", "")),
            payload=dict(data.get("payload", {})),
            priority=int(data.get("priority", 100)),
        )


class EventQueue:
    def __init__(self, events: Optional[List[SimEvent]] = None, next_id: int = 1) -> None:
        self._events: List[SimEvent] = sorted(events or [], key=lambda e: e.sort_key())
        self._next_id = max(int(next_id), 1)

    def schedule(
        self,
        date: datetime.date,
        hour: int,
        event_type: str,
        payload: Optional[Dict[str, Any]] = None,
        priority: int = 100,
    ) -> SimEvent:
        event = SimEvent(
            id=self._next_id,
            date=date,
            hour=_clamp_hour(hour),
            type=_normalize_event_type(event_type),
            payload=payload or {},
            priority=int(priority),
        )
        self._next_id += 1
        self._insert_event(event)
        return event

    def _insert_event(self, event: SimEvent) -> None:
        key = event.sort_key()
        lo = 0
        hi = len(self._events)
        while lo < hi:
            mid = (lo + hi) // 2
            if self._events[mid].sort_key() < key:
                lo = mid + 1
            else:
                hi = mid
        self._events.insert(lo, event)

    def peek_next(self) -> Optional[SimEvent]:
        return self._events[0] if self._events else None

    def pop_next(self) -> Optional[SimEvent]:
        if not self._events:
            return None
        return self._events.pop(0)

    def remove_matching(self, predicate) -> None:
        self._events = [event for event in self._events if not predicate(event)]

    def events(self) -> List[SimEvent]:
        return list(self._events)

    def serialize(self) -> Dict[str, Any]:
        return {
            "next_id": self._next_id,
            "events": [event.serialize() for event in self._events],
        }

    @classmethod
    def deserialize(cls, data: Dict[str, Any]) -> "EventQueue":
        events = [SimEvent.deserialize(ev) for ev in data.get("events", [])]
        return cls(events=events, next_id=int(data.get("next_id", 1)))


@dataclass
class InboxMessage:
    id: int
    date: datetime.date
    hour: int
    category: str
    subject: str
    body: str
    priority: int = 50
    requires_ack: bool = False
    requires_user_attention: bool = False
    blocks_advancement: bool = False
    decision_type: Optional[str] = None
    decision_id: Optional[str] = None
    payload: Dict[str, Any] = field(default_factory=dict)
    actions: List[Any] = field(default_factory=list)
    read: bool = False

    def serialize(self) -> Dict[str, Any]:
        return self.to_dict()

    def to_dict(self) -> Dict[str, Any]:
        timestamp = f"{self.date.isoformat()}T{self.hour:02d}:00:00"
        return {
            "id": int(self.id),
            "notification_id": str(self.id),
            "date": self.date.isoformat(),
            "created_at_date": self.date.isoformat(),
            "hour": int(self.hour),
            "created_at_time": f"{int(self.hour):02d}:00",
            "time": timestamp,
            "category": _to_primitive(self.category),
            "subject": _to_primitive(self.subject),
            "title": _to_primitive(self.subject),
            "body": _to_primitive(self.body),
            "message": _to_primitive(self.body),
            "priority": int(self.priority),
            "requires_ack": bool(self.requires_ack),
            "requires_user_attention": bool(self.requires_user_attention or self.requires_ack),
            "blocks_advancement": bool(self.blocks_advancement),
            "decision_type": _to_primitive(self.decision_type),
            "decision_id": _to_primitive(self.decision_id),
            "payload": _to_primitive(self.payload or {}),
            "actions": _sanitize_actions(self.actions),
            "read": bool(self.read),
        }

    @classmethod
    def deserialize(cls, data: Dict[str, Any]) -> "InboxMessage":
        return cls(
            id=int(data.get("id", 0)),
            date=datetime.date.fromisoformat(data["date"]),
            hour=_clamp_hour(data.get("hour", 0)),
            category=str(data.get("category", "General")),
            subject=str(data.get("subject", "")),
            body=str(data.get("body", "")),
            priority=int(data.get("priority", 50)),
            requires_ack=bool(data.get("requires_ack", False)),
            requires_user_attention=bool(
                data.get("requires_user_attention", data.get("requires_ack", False))
            ),
            blocks_advancement=bool(data.get("blocks_advancement", False)),
            decision_type=data.get("decision_type"),
            decision_id=data.get("decision_id"),
            payload=dict(data.get("payload", {}) or {}),
            actions=list(data.get("actions", [])),
            read=bool(data.get("read", False)),
        )

    def preview(self) -> Dict[str, Any]:
        return {
            "id": self.id,
            "date": self.date.isoformat(),
            "hour": self.hour,
            "category": self.category,
            "subject": self.subject,
            "requires_ack": self.requires_ack,
            "requires_user_attention": self.requires_user_attention or self.requires_ack,
            "blocks_advancement": self.blocks_advancement,
            "decision_id": self.decision_id,
            "read": self.read,
        }


def _sanitize_actions(actions: Iterable[Any]) -> List[Dict[str, Any]]:
    if actions is None:
        return []
    sanitized: List[Dict[str, Any]] = []
    for item in actions:
        if isinstance(item, dict):
            sanitized.append(_sanitize_action_dict(item))
        else:
            sanitized.append({"value": _to_primitive(item)})
    return sanitized


def _sanitize_action_dict(payload: Dict[str, Any]) -> Dict[str, Any]:
    return {str(key): _to_primitive(value) for key, value in payload.items()}


def _to_primitive(value: Any) -> Any:
    if value is None:
        return None
    if isinstance(value, (bool, int, float, str)):
        return value
    if isinstance(value, (datetime.date, datetime.datetime)):
        return value.isoformat()
    if isinstance(value, dict):
        return {str(key): _to_primitive(val) for key, val in value.items()}
    if isinstance(value, (list, tuple)):
        return [_to_primitive(item) for item in value]
    return str(value)


class RngStreamSet:
    def __init__(self, base_seed: int, state: Optional[Dict[str, Any]] = None) -> None:
        self.base_seed = int(base_seed)
        self._streams: Dict[str, random.Random] = {}
        self._load_state(state or {})

    def _load_state(self, state: Dict[str, Any]) -> None:
        for name, payload in state.items():
            rng = random.Random()
            rng.setstate(_deserialize_rng_state(payload))
            self._streams[name] = rng

    def get(self, name: str) -> random.Random:
        if name not in self._streams:
            self._streams[name] = random.Random(_derive_seed(self.base_seed, name))
        return self._streams[name]

    def seed_for(self, name: str, extra: str) -> int:
        return _derive_seed(_derive_seed(self.base_seed, name), extra)

    def serialize(self) -> Dict[str, Any]:
        return {name: _serialize_rng_state(rng.getstate()) for name, rng in self._streams.items()}


def _serialize_rng_state(state: Tuple[Any, Any, Any]) -> Dict[str, Any]:
    version, inner, gaussian = state
    return {"version": version, "state": list(inner), "gaussian": gaussian}


def _deserialize_rng_state(payload: Dict[str, Any]) -> Tuple[Any, Any, Any]:
    version = payload.get("version", 3)
    inner_state = tuple(payload.get("state", ()))
    gaussian = payload.get("gaussian", None)
    return (version, inner_state, gaussian)


class AgendaBuilder:
    def __init__(self, calendar: Any, schedule_by_week: Dict[str, Any]) -> None:
        self.calendar = calendar
        self.schedule_by_week = schedule_by_week

    def _week_start_date(self, week: Any) -> Optional[datetime.date]:
        try:
            week_int = int(week)
        except (TypeError, ValueError):
            return None
        base_date = getattr(self.calendar, "nfl_week1_start_date", None)
        if not isinstance(base_date, datetime.date):
            return None
        return base_date + datetime.timedelta(days=(week_int - 1) * 7)

    def _parse_game_date(self, game: Dict[str, Any]) -> Optional[datetime.date]:
        value = game.get("date")
        if isinstance(value, datetime.date):
            return value
        if isinstance(value, str) and value:
            try:
                return datetime.date.fromisoformat(value)
            except ValueError:
                return None
        return None

    def _iter_week_games(self, week: str) -> List[Dict[str, Any]]:
        week_str = str(week)
        direct = self.schedule_by_week.get(week_str, [])
        if isinstance(direct, list) and direct:
            return [game for game in direct if isinstance(game, dict)]
        fallback: List[Dict[str, Any]] = []
        seen: set[tuple[str, str, str]] = set()
        for bucket_key, games in self.schedule_by_week.items():
            if not isinstance(games, list):
                continue
            for game in games:
                if not isinstance(game, dict):
                    continue
                game_week = str(game.get("calendar_week") or game.get("week") or bucket_key or "")
                if game_week != week_str:
                    continue
                dedupe_key = (
                    str(game.get("home_id") or ""),
                    str(game.get("away_id") or ""),
                    str(game.get("kickoff") or ""),
                )
                if dedupe_key in seen:
                    continue
                seen.add(dedupe_key)
                fallback.append(game)
        return fallback

    def get_games_for_date(self, week: str, date: datetime.date) -> List[Dict[str, Any]]:
        day_name = _normalize_day_name(date.strftime("%A"))
        fallback_date = None
        week_start = self._week_start_date(week)
        if week_start is not None:
            fallback_date = week_start + datetime.timedelta(days=6)
        matched: List[Dict[str, Any]] = []
        for game in self._iter_week_games(week):
            home_id = game.get("home_id")
            away_id = game.get("away_id")
            if (
                not home_id
                or not away_id
                or _is_placeholder_team_ref(home_id)
                or _is_placeholder_team_ref(away_id)
            ):
                continue
            explicit_date = self._parse_game_date(game)
            game_day = _normalize_day_name(game.get("day", ""))
            if explicit_date is not None:
                if explicit_date != date:
                    continue
            elif game_day:
                if game_day != day_name:
                    continue
            else:
                if fallback_date is None or fallback_date != date:
                    continue
            enriched = dict(game)
            enriched["week"] = str(game.get("week") or game.get("calendar_week") or week)
            if explicit_date is not None:
                enriched["date"] = explicit_date.isoformat()
                enriched.setdefault("day", explicit_date.strftime("%A"))
            elif not game_day and fallback_date is not None:
                enriched["date"] = fallback_date.isoformat()
                enriched["day"] = fallback_date.strftime("%A")
                enriched["_used_schedule_fallback"] = True
            else:
                enriched.setdefault("date", date.isoformat())
            if not enriched.get("kickoff"):
                enriched["kickoff"] = "1:00 PM"
                enriched["_used_schedule_fallback"] = True
            matched.append(enriched)
        return matched

    def get_games_for_day(self, week: str, day_name: str) -> List[Dict[str, Any]]:
        week_start = self._week_start_date(week)
        if week_start is None:
            return []
        normalized = _normalize_day_name(day_name)
        try:
            offset = list(getattr(self.calendar, "DAYS_OF_WEEK", [])).index(normalized)
        except ValueError:
            return []
        return self.get_games_for_date(week, week_start + datetime.timedelta(days=offset))

    def is_game_day(self, week: str, day_name: str, team_id: Optional[str]) -> bool:
        if not team_id:
            return False
        games = self.get_games_for_day(week, day_name)
        for game in games:
            if team_id in (game.get("home_id"), game.get("away_id")):
                return True
        return False

    def is_travel_day(self, week: str, day_name: str, team_id: Optional[str]) -> bool:
        if not team_id:
            return False
        days = getattr(self.calendar, "DAYS_OF_WEEK", [])
        if not days:
            return False
        if day_name not in days:
            return False
        idx = days.index(day_name)
        next_day = days[(idx + 1) % 7]
        next_week = str(int(week) + 1) if idx == 6 else str(week)
        games = self.get_games_for_day(next_week, next_day)
        for game in games:
            if game.get("away_id") == team_id:
                return True
        return False


class TimeEngine:
    MORNING_INBOX_HOUR = 9
    TRAINING_HOURS = (10, 13, 16)
    TRAVEL_HOUR = 18
    DEFAULT_KICKOFF_HOUR = 13

    def __init__(
        self,
        league: Any,
        calendar: Any,
        season_manager: Any | None = None,
        schedule_by_week: Optional[Dict[str, Any]] = None,
    ) -> None:
        self.league = league
        self.calendar = calendar
        self.season_manager = season_manager
        self.schedule_by_week = schedule_by_week or getattr(season_manager, "schedule_by_week", {}) or {}
        self.clock = self._ensure_clock()
        self.queue = self._ensure_queue()
        self.inboxes = self._ensure_inboxes()
        self.decisions = self._ensure_decisions()
        self.user_team_id = self._ensure_user_team_id()
        self.base_seed = self._ensure_base_seed()
        self.rng_streams = self._ensure_rng_streams()
        self.last_agenda_date = getattr(league, "last_agenda_date", None)
        if isinstance(self.last_agenda_date, datetime.date):
            self.last_agenda_date = self.last_agenda_date.isoformat()
        self.last_phase_token = getattr(self.league, "last_phase_token", None)
        if self.last_phase_token is None:
            self.last_phase_token = self._phase_token()
            self.league.last_phase_token = self.last_phase_token
        self.simulated_games = self._ensure_simulated_games()
        self._debug_game_events: Optional[Dict[str, Any]] = None
        self._sync_calendar_time()

    def _ensure_clock(self) -> GameClock:
        current = getattr(self.league, "game_clock", None)
        if isinstance(current, GameClock):
            return current
        if isinstance(current, dict):
            clock = GameClock.deserialize(current, getattr(self.calendar, "current_date", datetime.date.today()))
        else:
            clock = GameClock(getattr(self.calendar, "current_date", datetime.date.today()), 0)
        self.league.game_clock = clock
        return clock

    def _ensure_queue(self) -> EventQueue:
        current = getattr(self.league, "event_queue", None)
        if isinstance(current, EventQueue):
            return current
        if isinstance(current, dict):
            queue = EventQueue.deserialize(current)
        else:
            queue = EventQueue()
        self.league.event_queue = queue
        return queue

    def _ensure_inboxes(self) -> Dict[str, List[InboxMessage]]:
        current = getattr(self.league, "inboxes", None)
        if isinstance(current, dict):
            inboxes = {}
            for team_id, items in current.items():
                inboxes[team_id] = [
                    msg if isinstance(msg, InboxMessage) else InboxMessage.deserialize(msg)
                    for msg in items
                ]
            self.league.inboxes = inboxes
            return inboxes
        inboxes: Dict[str, List[InboxMessage]] = {}
        self.league.inboxes = inboxes
        return inboxes

    def _ensure_decisions(self) -> List[DecisionItem]:
        current = getattr(self.league, "decisions", None)
        if isinstance(current, list):
            decisions = [
                item if isinstance(item, DecisionItem) else DecisionItem.from_dict(item)
                for item in current
            ]
            self.league.decisions = decisions
            return decisions
        decisions: List[DecisionItem] = []
        self.league.decisions = decisions
        return decisions

    def _ensure_user_team_id(self) -> Optional[str]:
        user_team_id = getattr(self.league, "user_team_id", None)
        if user_team_id:
            return user_team_id
        teams = getattr(self.league, "teams", [])
        if teams:
            user_team_id = getattr(teams[0], "id", None)
            self.league.user_team_id = user_team_id
            return user_team_id
        return None

    def _ensure_base_seed(self) -> int:
        base_seed = getattr(self.league, "base_seed", None)
        if base_seed is None:
            base_seed = random.randint(1, 2**31 - 1)
            self.league.base_seed = base_seed
        return int(base_seed)

    def _ensure_rng_streams(self) -> RngStreamSet:
        state = getattr(self.league, "rng_state", None)
        rng_set = RngStreamSet(self.base_seed, state=state if isinstance(state, dict) else None)
        for name in ("games", "training", "scouting", "time"):
            rng_set.get(name)
        self.league.rng_state = rng_set.serialize()
        return rng_set

    def _ensure_simulated_games(self) -> set:
        current = getattr(self.league, "simulated_games", None)
        if isinstance(current, set):
            return current
        if isinstance(current, list):
            simulated = set(current)
        else:
            simulated = set()
        self.league.simulated_games = simulated
        return simulated

    def _sync_calendar_time(self) -> None:
        if hasattr(self.calendar, "current_time_str"):
            self.calendar.current_time_str = self.clock.current_time_str
        else:
            self.calendar.current_time_str = self.clock.current_time_str

    def _phase_token(self) -> str:
        phase = getattr(self.calendar, "season_phase", "") or ""
        playoff = getattr(self.calendar, "playoff_subphase", None) or ""
        offseason = getattr(self.calendar, "offseason_subphase", None) or ""
        sub = playoff if playoff else offseason
        return f"{phase}|{sub}"

    def _is_trade_deadline_day(self, week: str, day_name: str) -> bool:
        phase = getattr(self.calendar, "season_phase", "")
        normalize = getattr(self.calendar, "normalize_phase", None)
        phase_name = normalize(phase) if callable(normalize) else _normalize_phase_name(phase)
        if phase_name != "regular_season":
            return False
        try:
            week_int = int(week)
        except (TypeError, ValueError):
            return False
        preseason = int(getattr(self.calendar, "PRESEASON_WEEKS", 3))
        rs_start = int(getattr(self.calendar, "REGULAR_SEASON_START_WEEK", preseason + 1))
        deadline_week = int(getattr(self.calendar, "TRADE_DEADLINE_WEEK", rs_start + 7))
        if week_int != deadline_week:
            return False
        return str(day_name or "").strip().lower() == "tuesday"

    def ensure_agenda_for_today(self) -> None:
        if self.last_agenda_date == self.clock.current_date.isoformat():
            return
        self._build_agenda_for_date(self.clock.current_date)

    def _start_debug_game_events(self) -> Dict[str, Any]:
        self._debug_game_events = {
            "scheduled_games_found": 0,
            "game_events_seeded": 0,
            "game_events_due": 0,
            "game_events_processed": 0,
            "game_results_created": 0,
            "skipped_games": [],
            "_scheduled_ids": set(),
            "_seeded_ids": set(),
            "_due_ids": set(),
            "_processed_ids": set(),
            "_result_ids": set(),
            "_skipped_ids": set(),
        }
        for event in self.queue.events():
            if _normalize_event_type(event.type) != "GameKickoff":
                continue
            game_id = str((event.payload or {}).get("game_id") or "")
            self._debug_mark_game("scheduled_games_found", game_id)
            self._debug_mark_game("game_events_seeded", game_id)
        return self._debug_game_events

    def _current_debug_game_events(self) -> Optional[Dict[str, Any]]:
        return self._debug_game_events

    def _finalize_debug_game_events(self) -> Optional[Dict[str, Any]]:
        debug = self._debug_game_events
        if debug is None:
            return None
        payload = {
            "scheduled_games_found": debug["scheduled_games_found"],
            "game_events_seeded": debug["game_events_seeded"],
            "game_events_due": debug["game_events_due"],
            "game_events_processed": debug["game_events_processed"],
            "game_results_created": debug["game_results_created"],
            "skipped_games": list(debug["skipped_games"]),
        }
        self._debug_game_events = None
        return payload

    def _debug_mark_game(self, bucket: str, game_id: Optional[str]) -> None:
        debug = self._current_debug_game_events()
        if debug is None or not game_id:
            return
        key_map = {
            "scheduled_games_found": "_scheduled_ids",
            "game_events_seeded": "_seeded_ids",
            "game_events_due": "_due_ids",
            "game_events_processed": "_processed_ids",
            "game_results_created": "_result_ids",
        }
        key = key_map.get(bucket)
        if key is None:
            return
        if game_id in debug[key]:
            return
        debug[key].add(game_id)
        debug[bucket] += 1

    def _debug_skip_game(self, game_id: Optional[str], reason: str) -> None:
        debug = self._current_debug_game_events()
        if debug is None or not game_id:
            return
        marker = (game_id, reason)
        if marker in debug["_skipped_ids"]:
            return
        debug["_skipped_ids"].add(marker)
        debug["skipped_games"].append({"game_id": game_id, "reason": reason})

    def _build_agenda_for_date(self, date: datetime.date) -> None:
        from gridiron_gm_pkg.simulation.systems.player.injury_status import heal_league_players

        heal_league_players(self.league, date)
        builder = AgendaBuilder(self.calendar, self.schedule_by_week)
        week = str(getattr(self.calendar, "current_week", "1"))
        day_name = _normalize_day_name(getattr(self.calendar, "current_day", date.strftime("%A")))
        games_today = builder.get_games_for_date(week, date)
        is_game_day = any(
            self.user_team_id in {game.get("home_id"), game.get("away_id")}
            for game in games_today
        )
        is_travel_day = builder.is_travel_day(week, day_name, self.user_team_id)

        current_token = self._phase_token()
        if self.last_phase_token is None:
            self.last_phase_token = current_token
            self.league.last_phase_token = self.last_phase_token
        elif current_token != self.last_phase_token:
            self.queue.schedule(
                date,
                8,
                "PhaseChange",
                {"from": self.last_phase_token, "to": current_token},
                priority=5,
            )
            self.last_phase_token = current_token
            self.league.last_phase_token = self.last_phase_token

        if self._is_trade_deadline_day(week, day_name):
            self.queue.schedule(
                date,
                8,
                "TradeDeadline",
                {"week": week},
                priority=6,
            )

        self.queue.schedule(
            date,
            self.MORNING_INBOX_HOUR,
            "InboxCheck",
            {"team_id": self.user_team_id},
            priority=10,
        )

        if not is_game_day and not is_travel_day:
            for slot_index, hour in enumerate(self.TRAINING_HOURS):
                self.queue.schedule(
                    date,
                    hour,
                    "TrainingSlot",
                    {"team_id": self.user_team_id, "slot_index": slot_index},
                    priority=20,
                )
        if is_travel_day:
            self.queue.schedule(
                date,
                self.TRAVEL_HOUR,
                "Travel",
                {"team_id": self.user_team_id},
                priority=30,
            )

        for game in games_today:
            kickoff_hour = parse_kickoff_hour(game.get("kickoff"))
            game_week = str(game.get("week") or week)
            game_id = make_game_id(game_week, game.get("home_id"), game.get("away_id"))
            self._debug_mark_game("scheduled_games_found", game_id)
            payload = {
                "game_id": game_id,
                "week": game_week,
                "home_id": game.get("home_id"),
                "away_id": game.get("away_id"),
                "day": game.get("day"),
                "date": game.get("date"),
                "kickoff": game.get("kickoff"),
                "round": game.get("round"),
                "conference": game.get("conference"),
                "kickoff_hour": kickoff_hour,
            }
            self.queue.schedule(date, kickoff_hour, "GameKickoff", payload, priority=40)
            wrap_hour = min(23, kickoff_hour + 3)
            self.queue.schedule(date, wrap_hour, "GameWrap", payload, priority=50)
            self._debug_mark_game("game_events_seeded", game_id)

        self.last_agenda_date = date.isoformat()
        self.league.last_agenda_date = self.last_agenda_date

    def advance_hour(self) -> Dict[str, Any]:
        self.clock.advance_hour(self.calendar)
        self._sync_calendar_time()
        if self.clock.hour == 0:
            from gridiron_gm_pkg.simulation.systems.player.attribute_xp import apply_weekly_decay

            apply_weekly_decay(
                self.league,
                year=getattr(self.calendar, "current_year", None),
                week=getattr(self.calendar, "current_week", None),
                current_date=getattr(self.calendar, "current_date", None),
            )
            self._sync_playoff_schedule_if_ready()
            self.ensure_agenda_for_today()

        processed: List[Dict[str, Any]] = []
        paused = False
        pause_event = None
        while True:
            next_event = self.queue.peek_next()
            if not next_event:
                break
            if not self._event_due(next_event):
                break
            event = self.queue.pop_next()
            if _normalize_event_type(event.type) in {"GameKickoff", "GameWrap"}:
                self._debug_mark_game("game_events_due", str((event.payload or {}).get("game_id") or ""))
            pause = self._handle_event(event)
            processed.append(self._event_summary(event))
            if pause and pause_event is None:
                pause_event = self._event_summary(event)
            if pause:
                paused = True
        self._persist_rng_state()
        self._persist_queue_state()
        return {"paused": paused, "processed": processed, "pause_event": pause_event}

    def continue_until_pause(self, max_hours: int = 168) -> Dict[str, Any]:
        hours_advanced = 0
        if max_hours <= 0 or not self.user_team_id:
            return self._continue_result(False, "no_user_events", 0)
        if self.has_blocking_decisions():
            return self._blocking_decision_stop_result()
        if self.pending_ack_count() > 0:
            return self._continue_result(True, self._stop_reason_from_pending_inbox(), 0)
        self._start_debug_game_events()
        self.ensure_agenda_for_today()
        processed_events: List[Dict[str, Any]] = []
        inbox_before = self._snapshot_inbox_message_ids()
        for _ in range(max_hours):
            due_result = self._process_due_events_at_current_time(auto_simulate_user_games=True)
            processed_events.extend(due_result.get("processed", []))
            if self.has_blocking_decisions():
                return self._blocking_decision_stop_result(
                    hours_advanced,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
            if due_result.get("paused"):
                pause_event = due_result.get("pause_event") or {}
                if self._should_skip_postseason_wrap_pause(pause_event):
                    continue
                stop_reason = self._stop_reason_from_pause_event(pause_event)
                return self._continue_result(
                    True,
                    stop_reason,
                    hours_advanced,
                    pause_event=pause_event,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
            result = self.advance_hour()
            hours_advanced += 1
            if result.get("paused"):
                pause_event = result.get("pause_event") or {}
                if self._consume_pause_event(
                    pause_event,
                    auto_simulate_user_games=True,
                    auto_continue_non_game_pauses=False,
                ):
                    result["paused"] = False
                    result["pause_event"] = None
            processed_events.extend(result.get("processed", []))
            if self.has_blocking_decisions():
                return self._blocking_decision_stop_result(
                    hours_advanced,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
            if result.get("paused"):
                pause_event = result.get("pause_event") or {}
                if self._should_skip_postseason_wrap_pause(pause_event):
                    continue
                stop_reason = self._stop_reason_from_pause_event(pause_event)
                return self._continue_result(
                    True,
                    stop_reason,
                    hours_advanced,
                    pause_event=pause_event,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
        return self._continue_result(
            False,
            "max_reached",
            hours_advanced,
            processed_events=processed_events,
            new_notifications=self._collect_new_notifications(inbox_before),
            debug_game_events=self._finalize_debug_game_events(),
        )

    def continue_once(
        self,
        max_hours: int = 24,
        *,
        max_processed_events: int = 50,
        max_days: int = 7,
    ) -> Dict[str, Any]:
        if max_hours <= 0:
            return self._continue_result(False, "invalid_max_hours", 0, target_reached=False)
        if self.has_blocking_decisions():
            return self._blocking_decision_stop_result()
        if self.pending_ack_count() > 0:
            return self._continue_result(
                True,
                "user_attention_required",
                0,
                target_reached=False,
            )
        self._start_debug_game_events()
        self.ensure_agenda_for_today()
        hours_advanced = 0
        processed_count = 0
        start_date = self.clock.current_date
        start_week = int(getattr(self.calendar, "current_week", 0) or 0)
        last_processed = None
        processed_events: List[Dict[str, Any]] = []
        inbox_before = self._snapshot_inbox_message_ids()

        while hours_advanced < max_hours:
            due_result = self._process_due_events_at_current_time(
                auto_simulate_user_games=True,
                auto_continue_non_game_pauses=True,
            )
            due_processed = due_result.get("processed") or []
            if due_processed:
                processed_events.extend(due_processed)
                processed_count += len(due_processed)
                last_processed = due_processed[-1]
                if self.has_blocking_decisions():
                    return self._blocking_decision_stop_result(
                        hours_advanced,
                        last_processed_event=last_processed,
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                stop_reason = self._continue_stop_reason_for_batch(due_processed)
                if stop_reason:
                    return self._continue_result(
                        True,
                        stop_reason,
                        hours_advanced,
                        target_reached=True,
                        last_processed_event=last_processed,
                        pause_event=due_result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                if self.pending_ack_count() > 0:
                    return self._continue_result(
                        True,
                        "user_attention_required",
                        hours_advanced,
                        target_reached=True,
                        last_processed_event=last_processed,
                        pause_event=due_result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                if processed_count >= max_processed_events:
                    return self._continue_result(
                        True,
                        "safety_limit",
                        hours_advanced,
                        target_reached=False,
                        last_processed_event=last_processed,
                        pause_event=due_result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
            if due_result.get("paused"):
                return self._continue_result(
                    True,
                    "user_attention_required",
                    hours_advanced,
                    target_reached=False,
                    last_processed_event=last_processed,
                    pause_event=due_result.get("pause_event"),
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )

            previous_date = self.clock.current_date
            previous_week = int(getattr(self.calendar, "current_week", 0) or 0)
            result = self.advance_hour()
            hours_advanced += 1
            current_week = int(getattr(self.calendar, "current_week", 0) or 0)

            processed = result.get("processed") or []
            if processed:
                processed_events.extend(processed)
                processed_count += len(processed)
                last_processed = processed[-1]
                if self.has_blocking_decisions():
                    return self._blocking_decision_stop_result(
                        hours_advanced,
                        last_processed_event=last_processed,
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                stop_reason = self._continue_stop_reason_for_batch(processed)
                if stop_reason:
                    return self._continue_result(
                        True,
                        stop_reason,
                        hours_advanced,
                        target_reached=True,
                        last_processed_event=last_processed,
                        pause_event=result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                if self.pending_ack_count() > 0:
                    return self._continue_result(
                        True,
                        "user_attention_required",
                        hours_advanced,
                        target_reached=True,
                        last_processed_event=last_processed,
                        pause_event=result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                if processed_count >= max_processed_events:
                    return self._continue_result(
                        True,
                        "safety_limit",
                        hours_advanced,
                        target_reached=False,
                        last_processed_event=last_processed,
                        pause_event=result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )

            if result.get("paused"):
                pause_event = result.get("pause_event") or {}
                if self._consume_pause_event(
                    pause_event,
                    auto_simulate_user_games=True,
                    auto_continue_non_game_pauses=True,
                ):
                    stop_reason = self.should_stop_for_continue(pause_event)
                    if stop_reason:
                        return self._continue_result(
                            True,
                            stop_reason,
                            hours_advanced,
                            target_reached=True,
                            last_processed_event=last_processed,
                            pause_event=pause_event,
                            processed_events=processed_events,
                            new_notifications=self._collect_new_notifications(inbox_before),
                            debug_game_events=self._finalize_debug_game_events(),
                        )
                    continue
                return self._continue_result(
                    True,
                    "user_attention_required",
                    hours_advanced,
                    target_reached=False,
                    last_processed_event=last_processed,
                    pause_event=pause_event,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )

            if current_week != previous_week:
                return self._continue_result(
                    True,
                    "week_change",
                    hours_advanced,
                    target_reached=True,
                    last_processed_event=last_processed,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )

            if self.clock.current_date != previous_date:
                days_advanced = (self.clock.current_date - start_date).days
                if days_advanced >= max_days:
                    return self._continue_result(
                        True,
                        "safety_limit",
                        hours_advanced,
                        target_reached=False,
                        last_processed_event=last_processed,
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                return self._continue_result(
                    True,
                    "end_of_day",
                    hours_advanced,
                    target_reached=True,
                    last_processed_event=last_processed,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )

        return self._continue_result(
            True,
            "safety_limit",
            hours_advanced,
            target_reached=False,
            last_processed_event=last_processed,
            processed_events=processed_events,
            new_notifications=self._collect_new_notifications(inbox_before),
            debug_game_events=self._finalize_debug_game_events(),
        )

    def advance_to_next_event(self, max_hours: int = 336) -> Dict[str, Any]:
        return self._advance_until(
            stop_reason_on_target="next_event",
            max_hours=max_hours,
            stop_on_first_processed_event=True,
            auto_simulate_user_games=True,
        )

    def advance_to_end_of_day(self, max_hours: int = 48) -> Dict[str, Any]:
        target = datetime.datetime.combine(
            self.clock.current_date + datetime.timedelta(days=1),
            datetime.time(0, 0),
        )
        return self._advance_until(
            stop_reason_on_target="end_of_day",
            max_hours=max_hours,
            target_datetime=target,
            auto_simulate_user_games=True,
        )

    def advance_day(self, max_hours: int = 48) -> Dict[str, Any]:
        target = datetime.datetime.combine(
            self.clock.current_date + datetime.timedelta(days=1),
            datetime.time(0, 0),
        )
        return self._advance_until(
            stop_reason_on_target="end_of_day",
            max_hours=max_hours,
            target_datetime=target,
            auto_simulate_user_games=True,
        )

    def advance_one_week(self, max_hours: int = 24 * 8) -> Dict[str, Any]:
        target = self.clock.current_datetime + datetime.timedelta(days=7)
        return self._advance_until(
            stop_reason_on_target="one_week",
            max_hours=max_hours,
            target_datetime=target,
            auto_simulate_user_games=True,
        )

    def advance_to_timestamp(
        self,
        target_datetime: datetime.datetime,
        stop_reason_on_target: str,
        max_hours: int = 24 * 365,
    ) -> Dict[str, Any]:
        return self._advance_until(
            stop_reason_on_target=stop_reason_on_target,
            max_hours=max_hours,
            target_datetime=target_datetime,
        )

    def _advance_until(
        self,
        stop_reason_on_target: str,
        *,
        max_hours: int,
        target_datetime: Optional[datetime.datetime] = None,
        stop_on_first_processed_event: bool = False,
        auto_simulate_user_games: bool = False,
    ) -> Dict[str, Any]:
        if max_hours <= 0:
            return self._continue_result(False, "invalid_max_hours", 0, target_reached=False)
        if self.has_blocking_decisions():
            return self._blocking_decision_stop_result()
        if self.pending_ack_count() > 0:
            return self._continue_result(
                True,
                self._stop_reason_from_pending_inbox(),
                0,
                target_reached=False,
            )
        self._start_debug_game_events()
        self.ensure_agenda_for_today()
        hours_advanced = 0
        last_processed = None
        processed_events: List[Dict[str, Any]] = []
        inbox_before = self._snapshot_inbox_message_ids()
        while hours_advanced < max_hours:
            due_result = self._process_due_events_at_current_time(
                auto_simulate_user_games=auto_simulate_user_games,
                auto_continue_non_game_pauses=auto_simulate_user_games,
            )
            due_processed = due_result.get("processed") or []
            if due_processed:
                processed_events.extend(due_processed)
                last_processed = due_processed[-1]
                if self.has_blocking_decisions():
                    return self._blocking_decision_stop_result(
                        hours_advanced,
                        last_processed_event=last_processed,
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                if stop_on_first_processed_event:
                    paused = bool(due_result.get("paused"))
                    stop_reason = stop_reason_on_target
                    if paused:
                        pause_event = due_result.get("pause_event") or {}
                        stop_reason = self._stop_reason_from_pause_event(pause_event)
                    return self._continue_result(
                        paused,
                        stop_reason,
                        hours_advanced,
                        target_reached=True,
                        last_processed_event=last_processed,
                        pause_event=due_result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
            if due_result.get("paused"):
                pause_event = due_result.get("pause_event") or {}
                if self._should_skip_postseason_wrap_pause(pause_event):
                    continue
                return self._continue_result(
                    True,
                    self._stop_reason_from_pause_event(pause_event),
                    hours_advanced,
                    target_reached=False,
                    last_processed_event=last_processed,
                    pause_event=pause_event,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
            if target_datetime is not None and self.clock.current_datetime >= target_datetime:
                return self._continue_result(
                    False,
                    stop_reason_on_target,
                    hours_advanced,
                    target_reached=True,
                    last_processed_event=last_processed,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
            result = self.advance_hour()
            hours_advanced += 1
            if result.get("paused"):
                pause_event = result.get("pause_event") or {}
                if self._consume_pause_event(
                    pause_event,
                    auto_simulate_user_games=auto_simulate_user_games,
                    auto_continue_non_game_pauses=auto_simulate_user_games,
                ):
                    result["paused"] = False
                    result["pause_event"] = None
            processed = result.get("processed") or []
            if processed:
                processed_events.extend(processed)
                last_processed = processed[-1]
                if self.has_blocking_decisions():
                    return self._blocking_decision_stop_result(
                        hours_advanced,
                        last_processed_event=last_processed,
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
                if stop_on_first_processed_event:
                    paused = bool(result.get("paused"))
                    stop_reason = stop_reason_on_target
                    if paused:
                        pause_event = result.get("pause_event") or {}
                        stop_reason = self._stop_reason_from_pause_event(pause_event)
                    return self._continue_result(
                        paused,
                        stop_reason,
                        hours_advanced,
                        target_reached=True,
                        last_processed_event=last_processed,
                        pause_event=result.get("pause_event"),
                        processed_events=processed_events,
                        new_notifications=self._collect_new_notifications(inbox_before),
                        debug_game_events=self._finalize_debug_game_events(),
                    )
            if result.get("paused"):
                pause_event = result.get("pause_event") or {}
                if self._consume_pause_event(
                    pause_event,
                    auto_simulate_user_games=auto_simulate_user_games,
                    auto_continue_non_game_pauses=auto_simulate_user_games,
                ):
                    continue
                if self._should_skip_postseason_wrap_pause(pause_event):
                    continue
                return self._continue_result(
                    True,
                    self._stop_reason_from_pause_event(pause_event),
                    hours_advanced,
                    target_reached=False,
                    last_processed_event=last_processed,
                    pause_event=pause_event,
                    processed_events=processed_events,
                    new_notifications=self._collect_new_notifications(inbox_before),
                    debug_game_events=self._finalize_debug_game_events(),
                )
        target_reached = target_datetime is not None and self.clock.current_datetime >= target_datetime
        stop_reason = stop_reason_on_target if target_reached else "max_reached"
        return self._continue_result(
            False,
            stop_reason,
            hours_advanced,
            target_reached=target_reached,
            last_processed_event=last_processed,
            processed_events=processed_events,
            new_notifications=self._collect_new_notifications(inbox_before),
            debug_game_events=self._finalize_debug_game_events(),
        )

    def _sync_playoff_schedule_if_ready(self) -> None:
        if self.season_manager is None or self.calendar is None:
            return
        if not self.calendar.is_regular_season_over():
            return
        if getattr(self.season_manager, "playoffs_generated", False):
            self.season_manager.advance_playoff_bracket_if_ready()
            self.schedule_by_week = getattr(self.season_manager, "schedule_by_week", self.schedule_by_week)
            return
        self.season_manager.generate_playoff_bracket_if_ready()
        self.season_manager.advance_playoff_bracket_if_ready()
        self.schedule_by_week = getattr(self.season_manager, "schedule_by_week", self.schedule_by_week)
        self.last_agenda_date = None
        self.league.last_agenda_date = None

    def _should_skip_postseason_wrap_pause(self, pause_event: Dict[str, Any]) -> bool:
        if pause_event.get("type") != "PhaseChange":
            return False
        payload = pause_event.get("payload") or {}
        to_token = str(payload.get("to", "")).lower()
        from_token = str(payload.get("from", "")).lower()
        return to_token.startswith("postseason|") and from_token.startswith("playoffs|")

    def _stop_reason_from_pause_event(self, pause_event: Dict[str, Any]) -> str:
        typ = _normalize_event_type(pause_event.get("type"))
        if typ == "GameKickoff":
            return "user_game_ready"
        if typ == "TradeDeadline":
            return "trade_deadline"
        if typ == "PhaseChange":
            payload = pause_event.get("payload") or {}
            to_tok = str(payload.get("to", ""))
            from_tok = str(payload.get("from", ""))
            if to_tok.lower().startswith("offseason|") and (
                from_tok.lower().startswith("playoffs|") or from_tok.lower().startswith("postseason|")
            ):
                return "end_of_season"
            return "phase_change"
        if typ == "InboxCheck":
            return "inbox_event"
        return "paused"

    def _process_due_events_at_current_time(
        self,
        *,
        auto_simulate_user_games: bool = False,
        auto_continue_non_game_pauses: bool = False,
    ) -> Dict[str, Any]:
        processed: List[Dict[str, Any]] = []
        pause_event = None
        while True:
            next_event = self.queue.peek_next()
            if not next_event or not self._event_due(next_event):
                break
            event = self.queue.pop_next()
            if _normalize_event_type(event.type) in {"GameKickoff", "GameWrap"}:
                self._debug_mark_game("game_events_due", str((event.payload or {}).get("game_id") or ""))
            pause = self._handle_event(event)
            summary = self._event_summary(event)
            processed.append(summary)
            if pause:
                pause_event = summary
                if self._consume_pause_event(
                    summary,
                    auto_simulate_user_games=auto_simulate_user_games,
                    auto_continue_non_game_pauses=auto_continue_non_game_pauses,
                ):
                    continue
                self._persist_rng_state()
                self._persist_queue_state()
                return {"paused": True, "processed": processed, "pause_event": pause_event}
        self._persist_rng_state()
        self._persist_queue_state()
        return {"paused": False, "processed": processed, "pause_event": pause_event}

    def _consume_pause_event(
        self,
        pause_event: Dict[str, Any],
        *,
        auto_simulate_user_games: bool,
        auto_continue_non_game_pauses: bool,
    ) -> bool:
        if auto_simulate_user_games and self._auto_simulate_pause_event(pause_event):
            return True
        if auto_continue_non_game_pauses and self._auto_continue_pause_event(pause_event):
            return True
        return False

    def _auto_simulate_pause_event(self, pause_event: Dict[str, Any]) -> bool:
        if _normalize_event_type(pause_event.get("type")) != "GameKickoff":
            return False
        if self.has_blocking_decisions():
            return False
        payload = pause_event.get("payload") or {}
        game_id = payload.get("game_id")
        if not game_id:
            return False
        self.simulate_scheduled_game(str(game_id))
        return True

    def _auto_continue_pause_event(self, pause_event: Dict[str, Any]) -> bool:
        return _normalize_event_type(pause_event.get("type")) in {"PhaseChange", "TradeDeadline"}

    def _stop_reason_from_pending_inbox(self) -> str:
        if self.has_blocking_decisions():
            return "blocking_decision"
        team_id = self.user_team_id
        if not team_id:
            return "inbox_event"
        pending = [
            msg
            for msg in self.inboxes.get(team_id, [])
            if msg.requires_ack and not msg.read
        ]
        if not pending:
            return "inbox_event"
        for msg in pending:
            subject = str(getattr(msg, "subject", "") or "")
            if subject.lower().startswith("kickoff:"):
                return "user_game_ready"
        for msg in pending:
            subject = str(getattr(msg, "subject", "") or "")
            category = str(getattr(msg, "category", "") or "")
            if "trade deadline" in subject.lower() or category.lower() == "trade":
                return "trade_deadline"
        for msg in pending:
            subject = str(getattr(msg, "subject", "") or "")
            category = str(getattr(msg, "category", "") or "")
            if category == "Milestone" and subject.startswith("Phase Change"):
                return "phase_change"
        return "inbox_event"

    def is_background_event(self, event_summary: Dict[str, Any], result: Optional[Dict[str, Any]] = None) -> bool:
        return self.should_stop_for_continue(event_summary, result=result) is None

    def should_stop_for_continue(
        self,
        event_summary: Dict[str, Any],
        result: Optional[Dict[str, Any]] = None,
    ) -> Optional[str]:
        _ = result
        event_type = _normalize_event_type(event_summary.get("type"))
        payload = event_summary.get("payload") or {}

        if self.has_blocking_decisions():
            return "blocking_decision"
        if self.pending_ack_count() > 0:
            return "user_attention_required"
        if event_type == "PhaseChange":
            return "phase_change"
        if event_type == "TradeDeadline":
            return "trade_deadline"
        if event_type == "GameKickoff":
            game = self._resolve_game_by_payload(payload)
            if game and self._is_user_game(game):
                game_id = self._resolved_game_id(game)
                if game_id and self._find_result(game_id) is not None:
                    return "game_simulated"
        return None

    def _continue_stop_reason_for_batch(self, processed_events: List[Dict[str, Any]]) -> Optional[str]:
        for event_summary in processed_events:
            stop_reason = self.should_stop_for_continue(event_summary)
            if stop_reason:
                return stop_reason
        return None

    def simulate_user_game(self, game_id: str) -> Dict[str, Any]:
        existing = self._find_result(game_id)
        if existing is not None:
            payload = dict(existing)
            payload["already_simmed"] = True
            self._debug_skip_game(game_id, "already_completed_result_exists")
            return payload
        game = self._resolve_game_by_id(game_id)
        if not game:
            return {"game_id": game_id, "error": "game_not_found"}
        details = self._lookup_game_details(game)
        result = self._simulate_game({**game, **details})
        self._debug_mark_game("game_events_processed", game_id)
        self.simulated_games.add(game_id)
        self.league.simulated_games = self.simulated_games
        if self._is_user_game(game) and not self._has_kickoff_message(game_id):
            message = self._make_kickoff_message(game, game_id)
            message.requires_ack = False
            self._add_inbox_message(self.user_team_id, message)
        self._resolve_kickoff_message(game_id)
        if self._is_user_game(game):
            self._add_game_summary_message(game, result)
        start_date = self.clock.current_date
        hours_advanced = 0
        # Finish the rest of today so CPU GameWrap events can finalize results, ignoring kickoff pauses.
        while self.clock.current_date == start_date and self.clock.hour < 23:
            advance = self.advance_hour()
            hours_advanced += 1
            for event in advance.get("processed", []):
                if event.get("type") != "GameKickoff":
                    continue
                payload = event.get("payload") or {}
                kickoff_game_id = payload.get("game_id")
                if kickoff_game_id:
                    self._resolve_kickoff_message(kickoff_game_id)
        result["finished_day"] = (
            self.clock.current_date != start_date or self.clock.hour >= 23
        )
        result["hours_advanced"] = hours_advanced
        return result

    def simulate_scheduled_game(self, game_id: str) -> Dict[str, Any]:
        existing = self._find_result(game_id)
        if existing is not None:
            payload = dict(existing)
            payload["already_simmed"] = True
            self._debug_skip_game(game_id, "already_completed_result_exists")
            return payload
        game = self._resolve_game_by_id(game_id)
        if not game:
            return {"game_id": game_id, "error": "game_not_found"}
        details = self._lookup_game_details(game)
        result = self._simulate_game({**game, **details})
        self._debug_mark_game("game_events_processed", game_id)
        self.simulated_games.add(game_id)
        self.league.simulated_games = self.simulated_games
        self._resolve_kickoff_message(game_id)
        if self._is_user_game(game):
            self._add_game_summary_message(game, result)
        return result

    def unread_inbox_count(self, team_id: Optional[str] = None) -> int:
        team_id = team_id or self.user_team_id
        if not team_id:
            return 0
        return sum(1 for msg in self.inboxes.get(team_id, []) if not msg.read)

    def pending_ack_count(self, team_id: Optional[str] = None) -> int:
        team_id = team_id or self.user_team_id
        if not team_id:
            return 0
        return sum(
            1
            for msg in self.inboxes.get(team_id, [])
            if msg.requires_ack and not msg.read
        )

    def get_inbox(self, team_id: Optional[str] = None) -> List[InboxMessage]:
        team_id = team_id or self.user_team_id
        if not team_id:
            return []
        messages = list(self.inboxes.get(team_id, []))
        return list(reversed(messages))

    def get_open_decisions(self) -> List[DecisionItem]:
        return [decision for decision in self.decisions if str(decision.status or "open") == "open"]

    def get_blocking_decisions(self) -> List[DecisionItem]:
        return [
            decision
            for decision in self.get_open_decisions()
            if bool(getattr(decision, "blocks_advancement", False))
        ]

    def has_blocking_decisions(self) -> bool:
        return bool(self.get_blocking_decisions())

    def find_open_decision(
        self,
        decision_type: str,
        *,
        rule_id: str | None = None,
        team_id: str | None = None,
    ) -> DecisionItem | None:
        target_type = str(decision_type or "")
        target_rule = str(rule_id or "")
        target_team = str(team_id or "")
        for decision in self.get_open_decisions():
            if str(getattr(decision, "decision_type", "") or "") != target_type:
                continue
            payload = getattr(decision, "payload", {}) or {}
            if target_rule and str(payload.get("rule_id") or "") != target_rule:
                continue
            if target_team and str(payload.get("team_id") or "") != target_team:
                continue
            return decision
        return None

    def find_notification(
        self,
        team_id: Optional[str],
        *,
        category: str | None = None,
        rule_id: str | None = None,
        season: int | None = None,
        week: int | None = None,
        unread_only: bool = False,
    ) -> InboxMessage | None:
        if not team_id:
            return None
        target_category = str(category or "")
        target_rule = str(rule_id or "")
        for msg in self.inboxes.get(team_id, []):
            if unread_only and bool(getattr(msg, "read", False)):
                continue
            if target_category and str(getattr(msg, "category", "") or "") != target_category:
                continue
            payload = getattr(msg, "payload", {}) or {}
            if target_rule and str(payload.get("rule_id") or "") != target_rule:
                continue
            if season is not None and int(payload.get("season", -1) or -1) != int(season):
                continue
            if week is not None and int(payload.get("week", -1) or -1) != int(week):
                continue
            return msg
        return None

    def create_notification(
        self,
        *,
        team_id: Optional[str],
        category: str,
        subject: str,
        body: str,
        priority: int = 50,
        requires_ack: bool = False,
        requires_user_attention: bool = False,
        blocks_advancement: bool = False,
        decision_type: Optional[str] = None,
        decision_id: Optional[str] = None,
        payload: Optional[Dict[str, Any]] = None,
        actions: Optional[List[Any]] = None,
        read: bool = False,
    ) -> InboxMessage:
        message = InboxMessage(
            id=self._next_message_id(team_id),
            date=self.clock.current_date,
            hour=self.clock.hour,
            category=str(category or ""),
            subject=str(subject or ""),
            body=str(body or ""),
            priority=int(priority),
            requires_ack=bool(requires_ack),
            requires_user_attention=bool(requires_user_attention),
            blocks_advancement=bool(blocks_advancement),
            decision_type=decision_type,
            decision_id=decision_id,
            payload=dict(payload or {}),
            actions=list(actions or []),
            read=bool(read),
        )
        self._add_inbox_message(team_id, message)
        return message

    def create_decision(
        self,
        *,
        category: str,
        decision_type: str,
        title: str,
        message: str,
        priority: int = 50,
        blocks_advancement: bool = False,
        options: Optional[List[Dict[str, Any]]] = None,
        payload: Optional[Dict[str, Any]] = None,
        linked_notification_id: Optional[str] = None,
    ) -> DecisionItem:
        payload_data = dict(payload or {})
        if linked_notification_id:
            payload_data.setdefault("linked_notification_id", str(linked_notification_id))
        decision = DecisionItem(
            decision_id=uuid.uuid4().hex,
            created_at_date=self.clock.current_date.isoformat(),
            created_at_time=self.clock.current_time_str,
            category=str(category or ""),
            decision_type=str(decision_type or ""),
            title=str(title or ""),
            message=str(message or ""),
            priority=int(priority),
            status="open",
            blocks_advancement=bool(blocks_advancement),
            options=[dict(option) for option in (options or []) if isinstance(option, dict)],
            payload=payload_data,
        )
        self.decisions.append(decision)
        self.league.decisions = self.decisions
        if linked_notification_id:
            self._link_notification_to_decision(str(linked_notification_id), decision)
        return decision

    def review_team_roster(
        self,
        team_id: Optional[str],
        *,
        include_advisories: bool = True,
        game_day_check: bool = False,
        game: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        resolved_team_id = str(team_id or self.user_team_id or "")
        if not resolved_team_id:
            return {
                "ok": False,
                "error": "missing_team_id",
                "team_id": "",
                "hard_violations": [],
                "advisories": [],
                "created_decisions": [],
                "created_notifications": [],
            }
        review = review_roster_rules(
            self.league,
            resolved_team_id,
            context={
                "check_type": "game_day" if game_day_check else "explicit_review",
                "game_day_check": game_day_check,
                "game": dict(game or {}),
            },
        )
        hard_violations = list(review.get("hard_violations") or [])
        advisories = list(review.get("advisories") or []) if include_advisories else []
        created_decisions: List[Dict[str, Any]] = []
        created_notifications: List[Dict[str, Any]] = []
        season = int(getattr(self.calendar, "current_year", 0) or 0)
        week = int(getattr(self.calendar, "current_week", 0) or 0)

        for violation in hard_violations:
            created = self._create_roster_violation_decision(violation)
            if created is not None:
                created_decisions.append(created.to_dict())

        for advisory in advisories:
            created = self._create_roster_advisory_notification(advisory, season=season, week=week)
            if created is not None:
                created_notifications.append(self._notification_summary(created))

        return {
            "ok": True,
            "team_id": resolved_team_id,
            "hard_violations": hard_violations,
            "advisories": advisories,
            "created_decisions": created_decisions,
            "created_notifications": created_notifications,
        }

    def _create_roster_violation_decision(self, violation: Dict[str, Any]) -> DecisionItem | None:
        payload = dict(violation.get("payload") or {})
        rule_id = str(violation.get("rule_id") or payload.get("rule_id") or "")
        team_id = str(payload.get("team_id") or "")
        if not rule_id or not team_id:
            return None
        if self.find_open_decision("roster_rule_violation", rule_id=rule_id, team_id=team_id) is not None:
            return None
        return self.create_decision(
            category="FrontOffice",
            decision_type="roster_rule_violation",
            title=str(violation.get("title") or "Roster Rule Violation"),
            message=str(violation.get("message") or ""),
            priority=90,
            blocks_advancement=True,
            options=[{"option_id": "acknowledge", "label": "Review Roster", "result": "acknowledge"}],
            payload=payload,
        )

    def _create_roster_advisory_notification(
        self,
        advisory: Dict[str, Any],
        *,
        season: int,
        week: int,
    ) -> InboxMessage | None:
        payload = dict(advisory.get("payload") or {})
        rule_id = str(advisory.get("rule_id") or payload.get("rule_id") or "")
        team_id = str(payload.get("team_id") or "")
        if not rule_id or not team_id:
            return None
        payload["season"] = int(season)
        payload["week"] = int(week)
        existing = self.find_notification(
            team_id,
            category="assistant_gm",
            rule_id=rule_id,
            season=season,
            week=week,
            unread_only=True,
        )
        if existing is not None:
            return None
        return self.create_notification(
            team_id=team_id,
            category="assistant_gm",
            subject=str(advisory.get("title") or "Assistant GM Note"),
            body=str(advisory.get("message") or ""),
            priority=40,
            requires_ack=False,
            requires_user_attention=False,
            blocks_advancement=False,
            payload=payload,
            actions=[],
            read=False,
        )

    def resolve_decision(self, decision_id: str, option_id: str) -> Dict[str, Any]:
        target = str(decision_id or "")
        selected = str(option_id or "")
        for decision in self.decisions:
            if str(decision.decision_id) != target:
                continue
            if str(decision.status or "open") != "open":
                return {"ok": False, "error": "decision_not_open"}
            matched = next(
                (
                    option
                    for option in (decision.options or [])
                    if isinstance(option, dict) and str(option.get("option_id")) == selected
                ),
                None,
            )
            if matched is None:
                return {"ok": False, "error": "invalid_option"}
            decision.status = "resolved"
            decision.selected_option = selected
            decision.resolved_at_date = self.clock.current_date.isoformat()
            decision.resolved_at_time = self.clock.current_time_str
            self.league.decisions = self.decisions
            self._update_linked_notification_for_resolution(decision)
            return {
                "ok": True,
                "decision": decision.to_dict(),
                "blocking_decision_count": len(self.get_blocking_decisions()),
            }
        return {"ok": False, "error": "decision_not_found"}

    def acknowledge_notification(self, notification_id: Any, team_id: Optional[str] = None) -> bool:
        team_id = team_id or self.user_team_id
        if not team_id:
            return False
        messages = self.inboxes.get(team_id, [])
        if not messages:
            return False
        target = str(notification_id or "")
        found = False
        for msg in messages:
            if str(msg.id) != target:
                continue
            msg.requires_ack = False
            msg.requires_user_attention = False
            msg.read = True
            found = True
            break
        if found:
            self.inboxes[team_id] = messages
            self.league.inboxes = self.inboxes
        return found

    def mark_read(self, message_id: Any, team_id: Optional[str] = None) -> bool:
        team_id = team_id or self.user_team_id
        if not team_id:
            return False
        messages = self.inboxes.get(team_id, [])
        if not messages:
            return False
        target = str(message_id)
        found = False
        for msg in messages:
            if str(msg.id) == target:
                msg.read = True
                found = True
                break
        if found:
            self.inboxes[team_id] = messages
            self.league.inboxes = self.inboxes
        return found

    def mark_all_read(self, team_id: Optional[str] = None) -> int:
        team_id = team_id or self.user_team_id
        if not team_id:
            return 0
        messages = self.inboxes.get(team_id, [])
        count = 0
        for msg in messages:
            if not msg.read:
                msg.read = True
                count += 1
        if count:
            self.inboxes[team_id] = messages
            self.league.inboxes = self.inboxes
        return count

    def latest_inbox_preview(self, team_id: Optional[str] = None) -> Optional[Dict[str, Any]]:
        team_id = team_id or self.user_team_id
        if not team_id:
            return None
        messages = self.inboxes.get(team_id, [])
        if not messages:
            return None
        return messages[-1].preview()

    def next_user_event_time(self) -> Optional[Dict[str, Any]]:
        for event in self.queue.events():
            if self._is_user_facing_event(event):
                return {
                    "date": event.date.isoformat(),
                    "time": f"{event.hour:02d}:00",
                    "hour": event.hour,
                    "type": event.type,
                }
        return None

    def _persist_queue_state(self) -> None:
        self.league.event_queue = self.queue

    def _persist_rng_state(self) -> None:
        self.league.rng_state = self.rng_streams.serialize()

    def _event_due(self, event: SimEvent) -> bool:
        if event.date < self.clock.current_date:
            return True
        if event.date > self.clock.current_date:
            return False
        return event.hour <= self.clock.hour

    def _handle_event(self, event: SimEvent) -> bool:
        event_type = _normalize_event_type(event.type)
        if event_type == "InboxCheck":
            return self.pending_ack_count(event.payload.get("team_id")) > 0
        if event_type == "TrainingSlot":
            self._handle_training(event)
            return False
        if event_type == "Travel":
            self._handle_travel(event)
            return False
        if event_type == "GameKickoff":
            return self._handle_kickoff(event)
        if event_type == "GameWrap":
            self._handle_game_wrap(event)
            return False
        if event_type == "PhaseChange":
            return self._handle_phase_change(event)
        if event_type == "TradeDeadline":
            return self._handle_trade_deadline(event)
        return False

    def _training_slot_index(self, event: SimEvent) -> int:
        payload = event.payload or {}
        slot_index = payload.get("slot_index", payload.get("slot"))
        if slot_index is None:
            try:
                return self.TRAINING_HOURS.index(event.hour)
            except ValueError:
                return 0
        try:
            return int(slot_index)
        except (TypeError, ValueError):
            return 0

    def _handle_training(self, event: SimEvent) -> None:
        # Training is intentionally inactive in the core-loop build.
        # See design_docs/player_development_system.txt.
        _ = event
        return None

    def _handle_travel(self, event: SimEvent) -> None:
        _ = event
        # Travel fatigue effects are stubbed in v0.
        return None

    def _handle_kickoff(self, event: SimEvent) -> bool:
        game = self._resolve_game_by_payload(event.payload)
        if not game:
            return False
        game_id = self._resolved_game_id(game)
        if not game_id or _is_placeholder_team_ref(game.get("home_id")) or _is_placeholder_team_ref(game.get("away_id")):
            return False
        if self._find_result(game_id) is not None:
            self._debug_skip_game(game_id, "already_completed_result_exists")
            return False
        if self._is_user_game(game):
            review = self.review_team_roster(
                self.user_team_id,
                include_advisories=False,
                game_day_check=True,
                game=game,
            )
            if review.get("hard_violations"):
                return True
            message = self._make_kickoff_message(game, game_id)
            self._add_inbox_message(self.user_team_id, message)
            return True
        return False

    def _handle_game_wrap(self, event: SimEvent) -> None:
        game = self._resolve_game_by_payload(event.payload)
        if not game:
            return
        game_id = self._resolved_game_id(game)
        if not game_id or _is_placeholder_team_ref(game.get("home_id")) or _is_placeholder_team_ref(game.get("away_id")):
            return
        if self._is_user_game(game) and game_id not in self.simulated_games:
            self._debug_skip_game(game_id, "waiting_for_user_simulation")
            return
        existing = self._find_result(game_id)
        if existing is not None:
            self._debug_skip_game(game_id, "already_completed_result_exists")
            return
        if game_id and game_id in self.simulated_games:
            self._debug_skip_game(game_id, "already_marked_simulated")
            return
        details = self._lookup_game_details(game)
        result = self._simulate_game({**game, **details})
        self._debug_mark_game("game_events_processed", game_id)
        if game_id:
            self.simulated_games.add(game_id)
            self.league.simulated_games = self.simulated_games
        if self._is_user_game(game):
            self._add_game_summary_message(game, result)

    def _handle_phase_change(self, event: SimEvent) -> bool:
        payload = event.payload or {}
        from_token = str(payload.get("from", ""))
        to_token = str(payload.get("to", ""))
        last_notified = getattr(self.league, "last_phase_change_notified", None)
        if last_notified is not None and str(last_notified) == to_token:
            return False
        subject = f"Phase Change: {to_token}" if to_token else "Phase Change"
        body = f"Season phase updated from {from_token} to {to_token}."
        message = InboxMessage(
            id=self._next_message_id(self.user_team_id),
            date=self.clock.current_date,
            hour=self.clock.hour,
            category="Milestone",
            subject=subject,
            body=body,
            requires_ack=False,
            actions=[],
            read=False,
        )
        self._add_inbox_message(self.user_team_id, message)
        self.league.last_phase_change_notified = to_token
        return True

    def _handle_trade_deadline(self, event: SimEvent) -> bool:
        payload = event.payload or {}
        week = str(payload.get("week", ""))
        last_notified = getattr(self.league, "last_trade_deadline_notified", None)
        if last_notified is not None and str(last_notified) == week:
            return False
        message = InboxMessage(
            id=self._next_message_id(self.user_team_id),
            date=self.clock.current_date,
            hour=self.clock.hour,
            category="Trade",
            subject="Trade Deadline",
            body="Trade deadline is today. Make any final trades before continuing.",
            requires_ack=False,
            actions=[],
            read=False,
        )
        self._add_inbox_message(self.user_team_id, message)
        self.league.last_trade_deadline_notified = week
        return True

    def _resolve_game_by_payload(self, payload: Dict[str, Any]) -> Optional[Dict[str, Any]]:
        if not payload:
            return None
        week = str(payload.get("week") or "")
        if week:
            builder = AgendaBuilder(self.calendar, self.schedule_by_week)
            week_games = builder._iter_week_games(week)
            exact_home = payload.get("home_id")
            exact_away = payload.get("away_id")
            if exact_home and exact_away:
                for entry in week_games:
                    if not isinstance(entry, dict):
                        continue
                    if entry.get("home_id") == exact_home and entry.get("away_id") == exact_away:
                        resolved = dict(entry)
                        resolved["week"] = week
                        return resolved
            for entry in week_games:
                if not isinstance(entry, dict):
                    continue
                if payload.get("round") and entry.get("round") != payload.get("round"):
                    continue
                if payload.get("conference") and entry.get("conference") != payload.get("conference"):
                    continue
                if payload.get("day") and entry.get("day") != payload.get("day"):
                    continue
                if payload.get("kickoff") and entry.get("kickoff") != payload.get("kickoff"):
                    continue
                if entry.get("home_id") and entry.get("away_id"):
                    resolved = dict(entry)
                    resolved["week"] = week
                    return resolved
        if payload.get("home_id") and payload.get("away_id"):
            return dict(payload)
        return None

    def _resolved_game_id(self, game: Optional[Dict[str, Any]]) -> str:
        if not game:
            return ""
        return make_game_id(game.get("week"), game.get("home_id"), game.get("away_id"))

    def _resolve_game_by_id(self, game_id: str) -> Optional[Dict[str, Any]]:
        parts = str(game_id).split("|")
        if len(parts) != 3:
            return None
        week, home_id, away_id = parts
        return {"week": week, "home_id": home_id, "away_id": away_id}

    def _simulate_game(self, game: Dict[str, Any]) -> Dict[str, Any]:
        home_id = game.get("home_id")
        away_id = game.get("away_id")
        week = str(game.get("week", ""))
        game_id = make_game_id(week, home_id, away_id)
        season_phase = getattr(self.calendar, "season_phase", None) if self.calendar is not None else None
        seed = self.rng_streams.seed_for("games", game_id)
        rng = random.Random(seed)
        home_strength = self._team_strength(home_id)
        away_strength = self._team_strength(away_id)
        home_score = self._compute_score(home_strength, rng)
        away_score = self._compute_score(away_strength, rng)
        if (game.get("playoff") or game.get("season_type") == "playoffs") and home_score == away_score:
            if rng.random() < 0.5:
                home_score += 3
            else:
                away_score += 3
        winner_id = None
        if home_score > away_score:
            winner_id = home_id
        elif away_score > home_score:
            winner_id = away_id
        box_score = generate_box_score(
            home_id,
            away_id,
            home_score,
            away_score,
            league=self.league,
            rng=rng,
        )
        sanitize_box_score_numbers(box_score)
        home_team = self.league.id_to_team.get(home_id) if hasattr(self.league, "id_to_team") else None
        away_team = self.league.id_to_team.get(away_id) if hasattr(self.league, "id_to_team") else None
        if home_team is not None and away_team is not None:
            from gridiron_gm_pkg.simulation.systems.player.injury_status import assign_game_injuries

            assign_game_injuries(home_team, away_team, self.clock.current_date, rng)
        result = {
            "game_id": game_id,
            "week": week,
            "date": self.clock.current_date.isoformat(),
            "kickoff_time": game.get("kickoff_time") or f"{self.clock.hour:02d}:00",
            "home_id": home_id,
            "away_id": away_id,
            "home": home_id,
            "away": away_id,
            "label": game.get("label", "Regular Season"),
            "season_phase": season_phase,
            "season_type": game.get("season_type"),
            "season_week": game.get("season_week"),
            "calendar_week": game.get("calendar_week"),
            "week_key": game.get("week_key"),
            "playoff": game.get("playoff"),
            "round": game.get("round"),
            "conference": game.get("conference"),
            "home_seed": game.get("home_seed"),
            "away_seed": game.get("away_seed"),
            "home_score": home_score,
            "away_score": away_score,
            "winner_id": winner_id,
            "was_user_game": self._is_user_game(game),
            "summary_text": self._summary_text(home_id, away_id, home_score, away_score),
            "box_score": box_score,
        }
        self._record_result(result)
        return result

    def _record_result(self, result: Dict[str, Any]) -> None:
        result = self._canonicalize_result(result)
        week = str(result.get("week", ""))
        game_id = result.get("game_id")
        if self.season_manager is not None and hasattr(self.season_manager, "results_by_week"):
            results_by_week = self.season_manager.results_by_week
        else:
            results_by_week = getattr(self.league, "results_by_week", {})
        if week:
            results_by_week.setdefault(week, [])
            if not any(game_id == entry.get("game_id") for entry in results_by_week[week]):
                results_by_week[week].append(result)
                self._debug_mark_game("game_results_created", str(game_id or ""))
        if self.season_manager is not None and hasattr(self.season_manager, "standings_manager"):
            # Avoid double-counting by routing all updates through the standings manager.
            self.season_manager.standings_manager.update_from_result(result)
        else:
            self._update_league_standings(result)
            self._update_team_records(result)
        self.league.results_by_week = results_by_week
        if self.season_manager is not None:
            self.season_manager.results_by_week = results_by_week
            self.season_manager.advance_playoff_bracket_if_ready()
            self.schedule_by_week = getattr(self.season_manager, "schedule_by_week", self.schedule_by_week)
            save_results(results_by_week, getattr(self.season_manager, "save_name", "test_league"))

    def _canonicalize_result(self, result: Dict[str, Any]) -> Dict[str, Any]:
        week = str(result.get("week", "") or "")
        if not week:
            return result
        schedule_games = AgendaBuilder(self.calendar, self.schedule_by_week)._iter_week_games(week)
        home_id = str(result.get("home_id") or result.get("home") or "")
        away_id = str(result.get("away_id") or result.get("away") or "")
        exact_match = next(
            (
                game for game in schedule_games
                if isinstance(game, dict)
                and str(game.get("home_id") or "") == home_id
                and str(game.get("away_id") or "") == away_id
            ),
            None,
        )
        target_game = exact_match
        if target_game is None and result.get("season_type") == "playoffs":
            target_game = next(
                (
                    game for game in schedule_games
                    if isinstance(game, dict)
                    and game.get("round") == result.get("round")
                    and game.get("conference") == result.get("conference")
                    and game.get("day") == result.get("day")
                    and str(game.get("kickoff") or "") == str(result.get("kickoff_time") or result.get("kickoff") or "")
                ),
                None,
            )
        if target_game is None:
            return result
        canonical = dict(result)
        canonical["home_id"] = target_game.get("home_id")
        canonical["away_id"] = target_game.get("away_id")
        canonical["home"] = target_game.get("home_id")
        canonical["away"] = target_game.get("away_id")
        canonical["game_id"] = make_game_id(week, target_game.get("home_id"), target_game.get("away_id"))
        for key in (
            "season_type",
            "season_week",
            "calendar_week",
            "week_key",
            "playoff",
            "round",
            "conference",
            "home_seed",
            "away_seed",
            "label",
        ):
            if key in target_game:
                canonical[key] = target_game.get(key)
        home_score = canonical.get("home_score")
        away_score = canonical.get("away_score")
        if home_score is not None and away_score is not None:
            if home_score > away_score:
                canonical["winner_id"] = target_game.get("home_id")
            elif away_score > home_score:
                canonical["winner_id"] = target_game.get("away_id")
            else:
                canonical["winner_id"] = None
        canonical["summary_text"] = self._summary_text(
            canonical.get("home_id"),
            canonical.get("away_id"),
            int(home_score or 0),
            int(away_score or 0),
        )
        return canonical

    def _add_game_summary_message(self, game: Dict[str, Any], result: Dict[str, Any]) -> None:
        home_id = game.get("home_id")
        away_id = game.get("away_id")
        game_id = str(result.get("game_id") or make_game_id(game.get("week"), home_id, away_id))
        home_name = self._team_label(home_id)
        away_name = self._team_label(away_id)
        subject = f"Final: {away_name} {result['away_score']} - {home_name} {result['home_score']}"
        record_line = self._user_team_record_line()
        body = "Game complete. Check the box score for full stats."
        if record_line:
            body = f"{body}\n{record_line}"
        message = InboxMessage(
            id=self._next_message_id(self.user_team_id),
            date=self.clock.current_date,
            hour=self.clock.hour,
            category="Game",
            subject=subject,
            body=body,
            requires_ack=False,
            payload={"game_id": game_id} if game_id else {},
            actions=[],
            read=False,
        )
        self._add_inbox_message(self.user_team_id, message)

    def _add_inbox_message(self, team_id: Optional[str], message: InboxMessage) -> None:
        if not team_id:
            return
        self.inboxes.setdefault(team_id, []).append(message)
        self.league.inboxes = self.inboxes

    def _link_notification_to_decision(self, notification_id: str, decision: DecisionItem) -> None:
        for messages in self.inboxes.values():
            for msg in messages:
                if str(getattr(msg, "id", "")) != str(notification_id):
                    continue
                msg.decision_id = decision.decision_id
                msg.decision_type = decision.decision_type
                msg.blocks_advancement = bool(decision.blocks_advancement)
                msg.requires_user_attention = True
                return

    def _next_message_id(self, team_id: Optional[str]) -> int:
        if not team_id:
            return 1
        existing = self.inboxes.get(team_id, [])
        if not existing:
            return 1
        return max(msg.id for msg in existing) + 1

    def _make_kickoff_message(self, game: Dict[str, Any], game_id: Optional[str]) -> InboxMessage:
        home_id = game.get("home_id")
        away_id = game.get("away_id")
        home_name = self._team_label(home_id)
        away_name = self._team_label(away_id)
        subject = f"Kickoff: {away_name} at {home_name}"
        body = "Kickoff is live. Sim the game when ready."
        actions = []
        if game_id:
            actions = [
                {
                    "type": "SIM_GAME",
                    "label": "Sim Game",
                    "game_id": game_id,
                    "payload": {"game_id": game_id},
                }
            ]
        return InboxMessage(
            id=self._next_message_id(self.user_team_id),
            date=self.clock.current_date,
            hour=self.clock.hour,
            category="Game",
            subject=subject,
            body=body,
            requires_ack=False,
            payload={"game_id": game_id} if game_id else {},
            actions=actions,
            read=False,
        )

    def _resolve_kickoff_message(self, game_id: str) -> None:
        if not game_id or not self.user_team_id:
            return
        messages = self.inboxes.get(self.user_team_id, [])
        for msg in messages:
            if not msg.requires_ack:
                continue
            for action in msg.actions or []:
                if isinstance(action, dict) and str(action.get("game_id")) == str(game_id):
                    msg.requires_ack = False
                    return

    def _update_linked_notification_for_resolution(self, decision: DecisionItem) -> None:
        linked_notification_id = None
        if isinstance(decision.payload, dict):
            linked_notification_id = decision.payload.get("linked_notification_id")
        for messages in self.inboxes.values():
            for msg in messages:
                matches_decision = str(getattr(msg, "decision_id", "") or "") == str(decision.decision_id)
                matches_link = linked_notification_id is not None and str(getattr(msg, "id", "")) == str(linked_notification_id)
                if not matches_decision and not matches_link:
                    continue
                msg.requires_ack = False
                msg.requires_user_attention = False
                msg.blocks_advancement = False
                return

    def _has_kickoff_message(self, game_id: str) -> bool:
        if not game_id or not self.user_team_id:
            return False
        messages = self.inboxes.get(self.user_team_id, [])
        for msg in messages:
            for action in msg.actions or []:
                if isinstance(action, dict) and str(action.get("game_id")) == str(game_id):
                    return True
        return False

    def _event_summary(self, event: SimEvent) -> Dict[str, Any]:
        return {
            "id": event.id,
            "type": _normalize_event_type(event.type),
            "date": event.date.isoformat(),
            "time": f"{event.hour:02d}:00",
            "hour": event.hour,
            "payload": event.payload,
        }

    def _snapshot_inbox_message_ids(self) -> set[tuple[str, str]]:
        snapshot = set()
        for team_id, messages in self.inboxes.items():
            for msg in messages:
                snapshot.add((str(team_id), str(getattr(msg, "id", ""))))
        return snapshot

    def _collect_new_notifications(self, previous_ids: set[tuple[str, str]]) -> List[Dict[str, Any]]:
        notifications: List[Dict[str, Any]] = []
        for team_id, messages in self.inboxes.items():
            for msg in messages:
                key = (str(team_id), str(getattr(msg, "id", "")))
                if key in previous_ids:
                    continue
                notifications.append(self._notification_summary(msg))
        notifications.sort(
            key=lambda item: (
                item.get("created_at_date", ""),
                item.get("created_at_time", ""),
                item.get("notification_id", ""),
            )
        )
        return notifications

    def _notification_summary(self, msg: InboxMessage) -> Dict[str, Any]:
        payload = msg.to_dict()
        return {
            "notification_id": str(payload.get("notification_id") or ""),
            "created_at_date": str(payload.get("created_at_date") or ""),
            "created_at_time": str(payload.get("created_at_time") or ""),
            "category": str(payload.get("category") or ""),
            "subject": str(payload.get("subject") or ""),
            "title": str(payload.get("title") or ""),
            "message": str(payload.get("message") or ""),
            "priority": int(payload.get("priority", 50) or 50),
            "read": bool(payload.get("read", False)),
            "requires_ack": bool(payload.get("requires_ack", False)),
            "requires_user_attention": bool(payload.get("requires_user_attention", False)),
            "blocks_advancement": bool(payload.get("blocks_advancement", False)),
            "decision_type": payload.get("decision_type"),
            "decision_id": payload.get("decision_id"),
            "payload": dict(payload.get("payload") or {}),
            "actions": list(payload.get("actions") or []),
        }

    def _continue_result(
        self,
        paused: bool,
        stop_reason: str,
        hours: int,
        *,
        target_reached: Optional[bool] = None,
        last_processed_event: Optional[Dict[str, Any]] = None,
        pause_event: Optional[Dict[str, Any]] = None,
        processed_events: Optional[List[Dict[str, Any]]] = None,
        new_notifications: Optional[List[Dict[str, Any]]] = None,
        blocking_decisions: Optional[List[Dict[str, Any]]] = None,
        debug_game_events: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        payload = {
            "ok": True,
            "paused": paused,
            "stop_reason": stop_reason,
            "advanced_hours": hours,
            "clock": {
                "date": self.clock.current_date.isoformat(),
                "current_time": self.clock.current_time_str,
                "hour": self.clock.hour,
            },
            "unread_count": self.unread_inbox_count(),
        }
        if target_reached is not None:
            payload["target_reached"] = target_reached
        if last_processed_event is not None:
            payload["last_processed_event"] = last_processed_event
        if pause_event is not None:
            payload["pause_event"] = pause_event
        if processed_events is not None:
            payload["processed_events"] = processed_events
        if new_notifications is not None:
            payload["new_notifications"] = new_notifications
        if blocking_decisions is not None:
            payload["blocking_decisions"] = blocking_decisions
        if debug_game_events is not None:
            payload["debug_game_events"] = debug_game_events
        return payload

    def _compact_decision_summary(self, decision: DecisionItem) -> Dict[str, Any]:
        return {
            "decision_id": str(decision.decision_id or ""),
            "created_at_date": str(decision.created_at_date or ""),
            "created_at_time": str(decision.created_at_time or ""),
            "category": str(decision.category or ""),
            "decision_type": str(decision.decision_type or ""),
            "title": str(decision.title or ""),
            "message": str(decision.message or ""),
            "priority": int(decision.priority or 0),
            "status": str(decision.status or "open"),
            "blocks_advancement": bool(decision.blocks_advancement),
            "options": [dict(option) for option in (decision.options or []) if isinstance(option, dict)],
            "selected_option": decision.selected_option,
        }

    def _blocking_decision_stop_result(
        self,
        hours: int = 0,
        *,
        last_processed_event: Optional[Dict[str, Any]] = None,
        processed_events: Optional[List[Dict[str, Any]]] = None,
        new_notifications: Optional[List[Dict[str, Any]]] = None,
        debug_game_events: Optional[Dict[str, Any]] = None,
    ) -> Dict[str, Any]:
        return self._continue_result(
            True,
            "blocking_decision",
            hours,
            target_reached=False,
            last_processed_event=last_processed_event,
            processed_events=processed_events,
            new_notifications=new_notifications,
            blocking_decisions=[self._compact_decision_summary(item) for item in self.get_blocking_decisions()],
            debug_game_events=debug_game_events,
        )

    def _is_user_game(self, game: Dict[str, Any]) -> bool:
        if not self.user_team_id:
            return False
        return self.user_team_id in {game.get("home_id"), game.get("away_id")}

    def _is_user_facing_event(self, event: SimEvent) -> bool:
        if event.type == "InboxCheck":
            return self.pending_ack_count(event.payload.get("team_id")) > 0
        if event.type in {"TradeDeadline", "PhaseChange"}:
            return True
        if event.type == "GameKickoff":
            game = self._resolve_game_by_payload(event.payload)
            return bool(game and self._is_user_game(game))
        return False

    def _team_label(self, team_id: Optional[str]) -> str:
        team = None
        if team_id and hasattr(self.league, "id_to_team"):
            team = self.league.id_to_team.get(team_id)
        if team is None:
            return team_id or "Unknown"
        return getattr(team, "abbreviation", None) or getattr(team, "team_name", None) or team_id

    def _lookup_game_details(self, game: Dict[str, Any]) -> Dict[str, Any]:
        week = str(game.get("week", ""))
        home_id = game.get("home_id")
        away_id = game.get("away_id")
        builder = AgendaBuilder(self.calendar, self.schedule_by_week)
        games = builder._iter_week_games(week)
        for entry in games:
            if (
                entry.get("home_id") == home_id
                and entry.get("away_id") == away_id
            ):
                details: Dict[str, Any] = {}
                kickoff = entry.get("kickoff")
                if kickoff is not None:
                    details["kickoff_time"] = kickoff
                day = entry.get("day")
                if day is not None:
                    details["day"] = day
                label = entry.get("label")
                if label is not None:
                    details["label"] = label
                elif entry.get("playoff"):
                    details["label"] = "Playoffs"
                season_type = entry.get("season_type")
                if season_type is not None:
                    details["season_type"] = season_type
                elif entry.get("playoff"):
                    details["season_type"] = "playoffs"
                if "playoff" in entry:
                    details["playoff"] = entry.get("playoff")
                if entry.get("round") is not None:
                    details["round"] = entry.get("round")
                if entry.get("conference") is not None:
                    details["conference"] = entry.get("conference")
                if entry.get("home_seed") is not None:
                    details["home_seed"] = entry.get("home_seed")
                if entry.get("away_seed") is not None:
                    details["away_seed"] = entry.get("away_seed")
                week_key = entry.get("week_key")
                if week_key is not None:
                    details["week_key"] = week_key
                season_week = entry.get("season_week")
                if season_week is not None:
                    details["season_week"] = season_week
                calendar_week = entry.get("calendar_week")
                if calendar_week is not None:
                    details["calendar_week"] = calendar_week
                return details
        return {}

    def _find_result(self, game_id: Optional[str]) -> Optional[Dict[str, Any]]:
        if not game_id:
            return None
        results_by_week = getattr(self.league, "results_by_week", {}) or {}
        for games in results_by_week.values():
            if not isinstance(games, list):
                continue
            for result in games:
                if result.get("game_id") == game_id:
                    return result
        if self.season_manager is not None and hasattr(self.season_manager, "results_by_week"):
            for games in self.season_manager.results_by_week.values():
                if not isinstance(games, list):
                    continue
                for result in games:
                    if result.get("game_id") == game_id:
                        return result
        return None

    def _team_strength(self, team_id: Optional[str]) -> float:
        if not team_id:
            return 70.0
        team = None
        if hasattr(self.league, "id_to_team"):
            team = self.league.id_to_team.get(team_id)
        if not team:
            roster = []
        else:
            roster = getattr(team, "roster", None) or getattr(team, "players", [])
        if not roster:
            return 70.0
        total = 0.0
        count = 0
        for player in roster:
            total += float(getattr(player, "overall", 70) or 70)
            count += 1
        return total / max(count, 1)

    def _compute_score(self, strength: float, rng: random.Random) -> int:
        expected = 20 + (strength - 70) * 0.3
        expected = max(10.0, min(40.0, expected))
        score = int(round(expected + rng.randint(-7, 7)))
        return max(3, min(60, score))

    def _summary_text(self, home_id: Optional[str], away_id: Optional[str], home_score: int, away_score: int) -> str:
        home_name = self._team_label(home_id)
        away_name = self._team_label(away_id)
        return f"{away_name} {away_score}, {home_name} {home_score}"

    def _update_league_standings(self, result: Dict[str, Any]) -> None:
        standings = getattr(self.league, "standings", None)
        if not isinstance(standings, dict):
            return
        home_id = result.get("home_id") or result.get("home")
        away_id = result.get("away_id") or result.get("away")
        home_score = int(result.get("home_score", 0))
        away_score = int(result.get("away_score", 0))
        for tid in (home_id, away_id):
            if tid not in standings:
                standings[tid] = {
                    "wins": 0,
                    "losses": 0,
                    "ties": 0,
                    "points_for": 0,
                    "points_against": 0,
                    "PF": 0,
                    "PA": 0,
                }
            standings[tid].setdefault("wins", 0)
            standings[tid].setdefault("losses", 0)
            standings[tid].setdefault("ties", 0)
            standings[tid].setdefault("points_for", 0)
            standings[tid].setdefault("points_against", 0)
            standings[tid].setdefault("PF", 0)
            standings[tid].setdefault("PA", 0)
        standings[home_id]["points_for"] += home_score
        standings[home_id]["points_against"] += away_score
        standings[away_id]["points_for"] += away_score
        standings[away_id]["points_against"] += home_score
        standings[home_id]["PF"] = standings[home_id].get("PF", 0) + home_score
        standings[home_id]["PA"] = standings[home_id].get("PA", 0) + away_score
        standings[away_id]["PF"] = standings[away_id].get("PF", 0) + away_score
        standings[away_id]["PA"] = standings[away_id].get("PA", 0) + home_score
        if home_score > away_score:
            standings[home_id]["wins"] += 1
            standings[away_id]["losses"] += 1
        elif away_score > home_score:
            standings[away_id]["wins"] += 1
            standings[home_id]["losses"] += 1
        else:
            standings[home_id]["ties"] += 1
            standings[away_id]["ties"] += 1
        self.league.standings = standings

    def _update_team_records(self, result: Dict[str, Any]) -> None:
        home_id = result.get("home_id") or result.get("home")
        away_id = result.get("away_id") or result.get("away")
        home_score = int(result.get("home_score", 0))
        away_score = int(result.get("away_score", 0))
        home_team = self.league.id_to_team.get(home_id) if hasattr(self.league, "id_to_team") else None
        away_team = self.league.id_to_team.get(away_id) if hasattr(self.league, "id_to_team") else None
        for team in (home_team, away_team):
            if team is None:
                continue
            record = getattr(team, "team_record", {})
            record.setdefault("wins", 0)
            record.setdefault("losses", 0)
            record.setdefault("ties", 0)
            record.setdefault("points_for", 0)
            record.setdefault("points_against", 0)
            record.setdefault("PF", 0)
            record.setdefault("PA", 0)
            team.team_record = record
        if home_team:
            home_team.team_record["points_for"] += home_score
            home_team.team_record["points_against"] += away_score
            home_team.team_record["PF"] += home_score
            home_team.team_record["PA"] += away_score
        if away_team:
            away_team.team_record["points_for"] += away_score
            away_team.team_record["points_against"] += home_score
            away_team.team_record["PF"] += away_score
            away_team.team_record["PA"] += home_score
        if home_score > away_score:
            if home_team:
                home_team.team_record["wins"] += 1
            if away_team:
                away_team.team_record["losses"] += 1
        elif away_score > home_score:
            if away_team:
                away_team.team_record["wins"] += 1
            if home_team:
                home_team.team_record["losses"] += 1
        else:
            if home_team:
                home_team.team_record["ties"] += 1
            if away_team:
                away_team.team_record["ties"] += 1

    def _user_team_record_line(self) -> str:
        if not self.user_team_id:
            return ""
        team = self.league.id_to_team.get(self.user_team_id) if hasattr(self.league, "id_to_team") else None
        if not team:
            return ""
        record = getattr(team, "team_record", {})
        wins = record.get("wins", 0)
        losses = record.get("losses", 0)
        ties = record.get("ties", 0)
        return f"Record: {wins}-{losses}-{ties}"


def parse_kickoff_hour(kickoff: Any) -> int:
    if kickoff is None:
        return TimeEngine.DEFAULT_KICKOFF_HOUR
    if isinstance(kickoff, (int, float)):
        return _clamp_hour(int(kickoff))
    text = str(kickoff).strip()
    if not text:
        return TimeEngine.DEFAULT_KICKOFF_HOUR
    if "AM" in text.upper() or "PM" in text.upper():
        parts = text.replace(" ", "").upper().split(":")
        hour = int(parts[0])
        minute_part = parts[1] if len(parts) > 1 else "00"
        minute = int(minute_part[:2])
        is_pm = "PM" in text.upper()
        if hour == 12:
            hour = 0
        if is_pm:
            hour += 12
        if minute >= 30:
            hour += 1
        return _clamp_hour(hour)
    if ":" in text:
        try:
            hour = int(text.split(":")[0])
            return _clamp_hour(hour)
        except ValueError:
            return TimeEngine.DEFAULT_KICKOFF_HOUR
    try:
        return _clamp_hour(int(text))
    except ValueError:
        return TimeEngine.DEFAULT_KICKOFF_HOUR


def parse_clock_hour(value: Any) -> int:
    if value is None:
        return 0
    if isinstance(value, (int, float)):
        return _clamp_hour(int(value))
    text = str(value).strip()
    if not text:
        return 0
    if ":" in text:
        text = text.split(":", 1)[0]
    try:
        return _clamp_hour(int(text))
    except ValueError:
        return 0


def make_game_id(week: Any, home_id: Any, away_id: Any) -> str:
    return f"{week}|{home_id}|{away_id}"
