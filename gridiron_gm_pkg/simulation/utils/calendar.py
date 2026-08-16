from __future__ import annotations

from datetime import date, datetime as dt, timedelta
from typing import Any, Dict


class Calendar:
    """Real-date calendar for league date, football week, and season phase."""

    DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
    PRESEASON_WEEKS = 3
    REGULAR_SEASON_WEEKS = 18
    PRESEASON_BYE_WEEK = PRESEASON_WEEKS + 1
    REGULAR_SEASON_START_WEEK = PRESEASON_BYE_WEEK + 1
    REGULAR_SEASON_END_WEEK = REGULAR_SEASON_START_WEEK + REGULAR_SEASON_WEEKS - 1
    PLAYOFF_WEEKS = 4
    POSTSEASON_WEEKS = 1
    MAX_FOOTBALL_WEEK = 52

    PHASE_OFFSEASON = "offseason"
    PHASE_PRESEASON = "preseason"
    PHASE_PRESEASON_BYE = "preseason_bye"
    PHASE_REGULAR_SEASON = "regular_season"
    PHASE_PLAYOFFS = "playoffs"
    PHASE_POSTSEASON = "postseason"

    PHASE_ALIASES = {
        "offseason": PHASE_OFFSEASON,
        "off season": PHASE_OFFSEASON,
        "preseason": PHASE_PRESEASON,
        "pre season": PHASE_PRESEASON,
        "preseason_bye": PHASE_PRESEASON_BYE,
        "preseason bye": PHASE_PRESEASON_BYE,
        "regular": PHASE_REGULAR_SEASON,
        "regular season": PHASE_REGULAR_SEASON,
        "regular_season": PHASE_REGULAR_SEASON,
        "playoff": PHASE_PLAYOFFS,
        "playoffs": PHASE_PLAYOFFS,
        "postseason": PHASE_POSTSEASON,
        "post season": PHASE_POSTSEASON,
    }

    PHASE_LABELS = {
        PHASE_OFFSEASON: "Offseason",
        PHASE_PRESEASON: "Preseason",
        PHASE_PRESEASON_BYE: "Preseason Bye / Final Cutdown",
        PHASE_REGULAR_SEASON: "Regular Season",
        PHASE_PLAYOFFS: "Playoffs",
        PHASE_POSTSEASON: "Postseason",
    }

    def __init__(self, start_year: int = 2025, start_date: date | str | None = None):
        self.season_year = int(start_year)
        self.nfl_week1_start_date = self.get_nfl_week1_start(self.season_year)
        self.current_date = self._coerce_date(start_date, self.nfl_week1_start_date)
        self._football_week = 1
        self._season_phase = self.PHASE_PRESEASON
        self.playoff_subphase = None
        self.offseason_subphase = None
        self._setup_phase_boundaries()
        self.update_phase()

    def _setup_phase_boundaries(self) -> None:
        playoff_start = self.REGULAR_SEASON_END_WEEK + 1
        playoff_end = playoff_start + self.PLAYOFF_WEEKS - 1
        postseason_start = playoff_end + 1
        postseason_end = postseason_start + self.POSTSEASON_WEEKS - 1
        self.phase_boundaries = {
            self.PHASE_PRESEASON: (1, self.PRESEASON_WEEKS),
            self.PHASE_PRESEASON_BYE: (self.PRESEASON_BYE_WEEK, self.PRESEASON_BYE_WEEK),
            self.PHASE_REGULAR_SEASON: (self.REGULAR_SEASON_START_WEEK, self.REGULAR_SEASON_END_WEEK),
            self.PHASE_PLAYOFFS: (playoff_start, playoff_end),
            self.PHASE_POSTSEASON: (postseason_start, postseason_end),
            self.PHASE_OFFSEASON: (postseason_end + 1, self.MAX_FOOTBALL_WEEK),
        }
        self.playoff_subphases = {
            playoff_start: "Wild Card",
            playoff_start + 1: "Divisional",
            playoff_start + 2: "Conference Championships",
            playoff_start + 3: "Gridiron Bowl",
        }
        self.offseason_subphases = {
            (postseason_start, postseason_end): "Postseason Wrap-Up",
            (postseason_end + 1, postseason_end + 2): "Combine",
            (postseason_end + 3, postseason_end + 6): "Free Agency",
            (postseason_end + 7, postseason_end + 8): "Rookie Camp",
            (postseason_end + 9, postseason_end + 12): "Minicamp",
            (postseason_end + 13, self.MAX_FOOTBALL_WEEK): "Dead Period",
        }

    @staticmethod
    def _coerce_date(value: date | str | None, fallback: date) -> date:
        if isinstance(value, date):
            return value
        if isinstance(value, str) and value:
            try:
                return date.fromisoformat(value)
            except ValueError:
                return fallback
        return fallback

    @classmethod
    def normalize_phase(cls, value: Any) -> str:
        text = str(value or "").strip().replace("-", "_")
        key = text.lower().replace("_", " ")
        return cls.PHASE_ALIASES.get(key, cls.PHASE_ALIASES.get(text.lower(), cls.PHASE_PRESEASON))

    @classmethod
    def phase_label(cls, value: Any) -> str:
        return cls.PHASE_LABELS.get(cls.normalize_phase(value), str(value or ""))

    @staticmethod
    def get_nfl_week1_start(year: int) -> date:
        """Return the preseason Week 1 Monday for the given season year."""
        sept1 = date(int(year), 9, 1)
        labor_day = sept1 + timedelta(days=(7 - sept1.weekday()) % 7)
        return labor_day - timedelta(weeks=4)

    @property
    def current_year(self) -> int:
        return self.season_year

    @current_year.setter
    def current_year(self, value: Any) -> None:
        self.season_year = int(value)
        self.nfl_week1_start_date = self.get_nfl_week1_start(self.season_year)
        self._setup_phase_boundaries()

    @property
    def football_week(self) -> int:
        return self._football_week

    @football_week.setter
    def football_week(self, value: Any) -> None:
        self._football_week = max(1, int(value))
        self.update_phase()

    @property
    def current_week(self) -> int:
        return self.football_week

    @current_week.setter
    def current_week(self, value: Any) -> None:
        self.football_week = value

    @property
    def season_phase(self) -> str:
        return self._season_phase

    @season_phase.setter
    def season_phase(self, value: Any) -> None:
        self._season_phase = self.normalize_phase(value)

    @property
    def season_phase_label(self) -> str:
        return self.phase_label(self.season_phase)

    @property
    def current_day_index(self) -> int:
        return self.current_date.weekday()

    @current_day_index.setter
    def current_day_index(self, value: Any) -> None:
        target = int(value) % 7
        self.current_date = self.current_date + timedelta(days=target - self.current_date.weekday())

    @property
    def current_day(self) -> str:
        return self.day_of_week

    @property
    def day_of_week(self) -> str:
        return self.current_date.strftime("%A")

    def advance_day(self) -> None:
        previous_weekday = self.current_date.weekday()
        self.current_date = self.current_date + timedelta(days=1)
        if previous_weekday == 6:
            self._advance_football_week()
        else:
            self.update_phase()

    def advance_week(self, days: int = 7) -> None:
        for _ in range(int(days)):
            self.advance_day()

    def _advance_football_week(self) -> None:
        self._football_week += 1
        if self._football_week > self.MAX_FOOTBALL_WEEK:
            self.season_year += 1
            self._football_week = 1
            self.nfl_week1_start_date = self.get_nfl_week1_start(self.season_year)
            self.playoff_subphase = None
            self.offseason_subphase = None
            self._setup_phase_boundaries()
        self.update_phase()

    def update_phase(self) -> None:
        for phase, (start, end) in self.phase_boundaries.items():
            if start <= self.football_week <= end:
                self._season_phase = phase
                break
        else:
            self._season_phase = self.PHASE_OFFSEASON

        if self.season_phase == self.PHASE_PLAYOFFS:
            self.playoff_subphase = self.playoff_subphases.get(self.football_week)
            self.offseason_subphase = None
        elif self.season_phase in {self.PHASE_OFFSEASON, self.PHASE_POSTSEASON}:
            self.playoff_subphase = None
            self.offseason_subphase = self.get_offseason_subphase()
        else:
            self.playoff_subphase = None
            self.offseason_subphase = None

    def get_week_label(self) -> str:
        if self.football_week <= self.PRESEASON_WEEKS:
            return f"Preseason Week {self.football_week}"
        if self.football_week == self.PRESEASON_BYE_WEEK:
            return "Preseason Bye / Final Cutdown"
        if self.REGULAR_SEASON_START_WEEK <= self.football_week <= self.REGULAR_SEASON_END_WEEK:
            return f"Regular Season Week {self.football_week - self.REGULAR_SEASON_START_WEEK + 1}"
        playoff_start, playoff_end = self.phase_boundaries[self.PHASE_PLAYOFFS]
        if playoff_start <= self.football_week <= playoff_end:
            subphase = self.playoff_subphases.get(self.football_week)
            return f"Playoffs - {subphase}" if subphase else f"Playoffs Week {self.football_week - playoff_start + 1}"
        return self.season_phase_label

    def get_display_info(self) -> Dict[str, Any]:
        return {
            "Year": self.season_year,
            "Phase": self.season_phase_label,
            "Label": self.get_week_label(),
            "Day of Week": self.day_of_week,
            "Date": self.current_date,
        }

    def is_regular_season_over(self) -> bool:
        return self.football_week > self.REGULAR_SEASON_END_WEEK

    def get_last_regular_season_week(self) -> int:
        return self.REGULAR_SEASON_END_WEEK

    def should_advance_week(self, ignore_game_check: bool = False) -> bool:
        return self.current_day_index == 1

    def get_offseason_subphase(self) -> str | None:
        for (start, end), name in self.offseason_subphases.items():
            if start <= self.football_week <= end:
                return name
        return None

    def serialize(self) -> Dict[str, Any]:
        current_time = getattr(self, "current_time_str", None) or "00:00"
        payload = {
            "current_date": self.current_date.isoformat(),
            "current_time": current_time,
            "day_of_week": self.day_of_week,
            "season_year": self.season_year,
            "season_phase": self.season_phase,
            "football_week": self.football_week,
            "current_year": self.season_year,
            "current_week": self.football_week,
            "phase_label": self.season_phase_label,
            "week_label": self.get_week_label(),
            "season_phase_label": self.season_phase_label,
            "playoff_subphase": self.playoff_subphase,
            "offseason_subphase": self.offseason_subphase,
        }
        payload["current_time_str"] = current_time
        return payload

    @classmethod
    def deserialize(cls, data: Dict[str, Any]) -> "Calendar":
        season_year = data.get("season_year", data.get("current_year", 2025))
        cal = cls(start_year=season_year)
        fallback = cal.nfl_week1_start_date
        cal.current_date = cls._coerce_date(data.get("current_date"), fallback)
        cal._football_week = int(data.get("football_week", data.get("current_week", 1)) or 1)
        current_time = data.get("current_time_str", data.get("current_time"))
        if current_time is not None:
            cal.current_time_str = str(current_time)
        phase_supplied = "season_phase" in data
        if phase_supplied:
            cal._season_phase = cls.normalize_phase(data.get("season_phase"))
        else:
            cal.update_phase()
        cal.playoff_subphase = data.get("playoff_subphase")
        cal.offseason_subphase = data.get("offseason_subphase")
        if cal.season_phase == cal.PHASE_PLAYOFFS and cal.playoff_subphase is None:
            cal.playoff_subphase = cal.playoff_subphases.get(cal.football_week)
        if cal.season_phase in {cal.PHASE_OFFSEASON, cal.PHASE_POSTSEASON} and cal.offseason_subphase is None:
            cal.offseason_subphase = cal.get_offseason_subphase()
        return cal

    def get_day_after_championship(self) -> date:
        playoffs_end_week = self.phase_boundaries[self.PHASE_PLAYOFFS][1]
        days_to_advance = (playoffs_end_week - 1) * 7
        return self.nfl_week1_start_date + timedelta(days=days_to_advance + 7)
