<<<<<<< HEAD
import datetime
from datetime import date, timedelta, datetime as dt

class Calendar:
    """
    Calendar system for NFL-style scheduling, week tracking, and phase management.
    """

    DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
    # Typical NFL: preseason starts early Aug, regular season after Labor Day
    NFL_WEEK1_START_OFFSET = 5  # Aligns week numbers so regular season week 1 matches NFL timing

    def __init__(self, start_year=2025):
        self.current_year = start_year
        self.nfl_week1_start_date = self.get_nfl_week1_start(self.current_year)
        self.current_date = self.nfl_week1_start_date
        self._setup_phase_boundaries()
        self.season_phase = "Preseason"
        self.playoff_subphase = None
        self.offseason_subphase = None
        self._current_week = 1  # <-- Add this line

    def _setup_phase_boundaries(self):
        # Adjust week boundaries as needed for your league structure
        self.phase_boundaries = {
            "Preseason": (1, 3),
            "Bye Week": (4, 4),
            "Regular Season": (5, 22),
            "Playoffs": (23, 26),
            "Offseason": (27, 52),
        }
        self.playoff_subphases = {
            23: "Wild Card Round",
            24: "Divisional Round",
            25: "Conference Championships",
            26: "Gridiron Bowl"
        }
        self.offseason_subphases = {
            (27, 28): "Postseason Wrap-Up",
            (29, 30): "Combine",
            (31, 34): "Free Agency",
            (35, 36): "Rookie Camp",
            (37, 40): "Minicamp",
            (41, 52): "Dead Period"
        }

    @staticmethod
    def get_nfl_week1_start(year):
        """
        Returns the NFL Week 1 start date (first Tuesday after Labor Day).
        """
        sept1 = date(year, 9, 1)
        labor_day = sept1 + timedelta(days=(7 - sept1.weekday()) % 7)
        week1_start = labor_day + timedelta(days=1)
        return week1_start

    @property
    def current_week(self):
        return self._current_week  # <-- Use the private variable

    @current_week.setter
    def current_week(self, value):
        self._current_week = value

    @property
    def current_day_index(self):
        return self.current_date.weekday()

    @property
    def current_day(self):
        return self.DAYS_OF_WEEK[self.current_date.weekday()]

    def advance_day(self):
        self.current_date += timedelta(days=1)
        self.update_phase()
        print(f"Advancing day: {self.current_date}, week: {self.current_week}")

        # If it's the last day of the week, advance the week number
        if self.current_date.weekday() == 6:  # Sunday (or whatever your week end is)
            self.current_week += 1

            # If we've reached the end of the NFL calendar (week 52), roll over to a new season
            if self.current_week > 52:
                self.current_year += 1
                self.current_week = 1
                self.nfl_week1_start_date = self.get_nfl_week1_start(self.current_year)
                self.current_date = self.nfl_week1_start_date
                self._setup_phase_boundaries()
                self.season_phase = "Preseason"
                self.playoff_subphase = None
                self.offseason_subphase = None
                print(f"=== New NFL Season Begins: Year {self.current_year} ===")
                print(f"Calendar reset to {self.current_date}, week {self.current_week}, phase {self.season_phase}")

        # Always update phase after any change
        self.update_phase()

    def update_phase(self):
        for phase, (start, end) in self.phase_boundaries.items():
            if start <= self.current_week <= end:
                self.season_phase = phase
                break

        if self.season_phase == "Playoffs":
            self.playoff_subphase = self.playoff_subphases.get(self.current_week, None)
            self.offseason_subphase = None
        elif self.season_phase == "Offseason":
            self.playoff_subphase = None
            self.offseason_subphase = self.get_offseason_subphase()
        else:
            self.playoff_subphase = None
            self.offseason_subphase = None

    def get_week_label(self):
        if self.current_week <= 3:
            return f"Preseason Week {self.current_week}"
        elif self.current_week == 4:
            return "Bye Week"
        elif 5 <= self.current_week <= 22:
            return f"Regular Season Week {self.current_week - 4}"
        elif 23 <= self.current_week <= 26:
            return f"Playoffs Week {self.current_week - 22}"
        else:
            return "Offseason"

    def get_display_info(self):
        return {
            "Year": self.current_year,
            "Phase": self.season_phase,
            "Label": self.get_week_label(),
            "Day of Week": self.current_day,
            "Date": self.current_date
        }

    def is_regular_season_over(self):
        # Regular season ends after week 22
        return self.current_week > 22

    def get_last_regular_season_week(self):
        return 22

    def should_advance_week(self, ignore_game_check=False):
        # You can customize this, but default: advance week on Tuesday
        return self.current_day_index == 1

    def get_offseason_subphase(self):
        for (start, end), name in self.offseason_subphases.items():
            if start <= self.current_week <= end:
                return name
        return None

    def serialize(self):
        return {
            "current_year": self.current_year,
            "current_date": self.current_date.isoformat(),
            "season_phase": self.season_phase,
            "playoff_subphase": self.playoff_subphase,
            "offseason_subphase": self.offseason_subphase
        }

    @classmethod
    def deserialize(cls, data):
        cal = cls(start_year=data.get("current_year", 2025))
        cal.current_date = dt.fromisoformat(data["current_date"]).date() if "current_date" in data else cal.nfl_week1_start_date
        cal.season_phase = data.get("season_phase", "Preseason")
        cal.playoff_subphase = data.get("playoff_subphase")
        cal.offseason_subphase = data.get("offseason_subphase")
        return cal

    def get_day_after_championship(self):
        """
        Returns the date object for the day after the championship game (end of playoffs).
        Adjust this logic to match your playoff schedule.
        """
        # Example: Assume championship is last day of Playoffs phase
        playoffs_end_week = self.phase_boundaries["Playoffs"][1]
        # Find the first day of the week after playoffs
        days_to_advance = (playoffs_end_week - 1) * 7  # 0-based week
        champ_day = self.nfl_week1_start_date + timedelta(days=days_to_advance + 7)
        return champ_day

=======
import datetime
from datetime import date, timedelta, datetime as dt

class Calendar:
    """
    Calendar system for NFL-style scheduling, week tracking, and phase management.
    """

    DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
    # Typical NFL: preseason starts early Aug, regular season after Labor Day
    NFL_WEEK1_START_OFFSET = 5  # Aligns week numbers so regular season week 1 matches NFL timing

    def __init__(self, start_year=2025):
        self.current_year = start_year
        self.nfl_week1_start_date = self.get_nfl_week1_start(self.current_year)
        self.current_date = self.nfl_week1_start_date
        self._setup_phase_boundaries()
        self.season_phase = "Preseason"
        self.playoff_subphase = None
        self.offseason_subphase = None
        self._current_week = 1  # <-- Add this line

    def _setup_phase_boundaries(self):
        # Adjust week boundaries as needed for your league structure
        self.phase_boundaries = {
            "Preseason": (1, 3),
            "Bye Week": (4, 4),
            "Regular Season": (5, 22),
            "Playoffs": (23, 26),
            "Offseason": (27, 52),
        }
        self.playoff_subphases = {
            23: "Wild Card Round",
            24: "Divisional Round",
            25: "Conference Championships",
            26: "Gridiron Bowl"
        }
        self.offseason_subphases = {
            (27, 28): "Postseason Wrap-Up",
            (29, 30): "Combine",
            (31, 34): "Free Agency",
            (35, 36): "Rookie Camp",
            (37, 40): "Minicamp",
            (41, 52): "Dead Period"
        }

    @staticmethod
    def get_nfl_week1_start(year):
        """
        Returns the NFL Week 1 start date (first Tuesday after Labor Day).
        """
        sept1 = date(year, 9, 1)
        labor_day = sept1 + timedelta(days=(7 - sept1.weekday()) % 7)
        week1_start = labor_day + timedelta(days=1)
        return week1_start

    @property
    def current_week(self):
        return self._current_week  # <-- Use the private variable

    @current_week.setter
    def current_week(self, value):
        self._current_week = value

    @property
    def current_day_index(self):
        return self.current_date.weekday()

    @property
    def current_day(self):
        return self.DAYS_OF_WEEK[self.current_date.weekday()]

    def advance_day(self):
        self.current_date += timedelta(days=1)
        self.update_phase()
        print(f"Advancing day: {self.current_date}, week: {self.current_week}")

        # If it's the last day of the week, advance the week number
        if self.current_date.weekday() == 6:  # Sunday (or whatever your week end is)
            self.current_week += 1

            # If we've reached the end of the NFL calendar (week 52), roll over to a new season
            if self.current_week > 52:
                self.current_year += 1
                self.current_week = 1
                self.nfl_week1_start_date = self.get_nfl_week1_start(self.current_year)
                self.current_date = self.nfl_week1_start_date
                self._setup_phase_boundaries()
                self.season_phase = "Preseason"
                self.playoff_subphase = None
                self.offseason_subphase = None
                print(f"=== New NFL Season Begins: Year {self.current_year} ===")
                print(f"Calendar reset to {self.current_date}, week {self.current_week}, phase {self.season_phase}")

        # Always update phase after any change
        self.update_phase()

    def update_phase(self):
        for phase, (start, end) in self.phase_boundaries.items():
            if start <= self.current_week <= end:
                self.season_phase = phase
                break

        if self.season_phase == "Playoffs":
            self.playoff_subphase = self.playoff_subphases.get(self.current_week, None)
            self.offseason_subphase = None
        elif self.season_phase == "Offseason":
            self.playoff_subphase = None
            self.offseason_subphase = self.get_offseason_subphase()
        else:
            self.playoff_subphase = None
            self.offseason_subphase = None

    def get_week_label(self):
        if self.current_week <= 3:
            return f"Preseason Week {self.current_week}"
        elif self.current_week == 4:
            return "Bye Week"
        elif 5 <= self.current_week <= 22:
            return f"Regular Season Week {self.current_week - 4}"
        elif 23 <= self.current_week <= 26:
            return f"Playoffs Week {self.current_week - 22}"
        else:
            return "Offseason"

    def get_display_info(self):
        return {
            "Year": self.current_year,
            "Phase": self.season_phase,
            "Label": self.get_week_label(),
            "Day of Week": self.current_day,
            "Date": self.current_date
        }

    def is_regular_season_over(self):
        # Regular season ends after week 22
        return self.current_week > 22

    def get_last_regular_season_week(self):
        return 22

    def should_advance_week(self, ignore_game_check=False):
        # You can customize this, but default: advance week on Tuesday
        return self.current_day_index == 1

    def get_offseason_subphase(self):
        for (start, end), name in self.offseason_subphases.items():
            if start <= self.current_week <= end:
                return name
        return None

    def serialize(self):
        return {
            "current_year": self.current_year,
            "current_date": self.current_date.isoformat(),
            "season_phase": self.season_phase,
            "playoff_subphase": self.playoff_subphase,
            "offseason_subphase": self.offseason_subphase
        }

    @classmethod
    def deserialize(cls, data):
        cal = cls(start_year=data.get("current_year", 2025))
        cal.current_date = dt.fromisoformat(data["current_date"]).date() if "current_date" in data else cal.nfl_week1_start_date
        cal.season_phase = data.get("season_phase", "Preseason")
        cal.playoff_subphase = data.get("playoff_subphase")
        cal.offseason_subphase = data.get("offseason_subphase")
        return cal

    def get_day_after_championship(self):
        """
        Returns the date object for the day after the championship game (end of playoffs).
        Adjust this logic to match your playoff schedule.
        """
        # Example: Assume championship is last day of Playoffs phase
        playoffs_end_week = self.phase_boundaries["Playoffs"][1]
        # Find the first day of the week after playoffs
        days_to_advance = (playoffs_end_week - 1) * 7  # 0-based week
        champ_day = self.nfl_week1_start_date + timedelta(days=days_to_advance + 7)
        return champ_day

>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
