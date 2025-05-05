class Calendar:
    DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]

    def __init__(self):
        self.current_year = 2025
        self.current_week = 1  # Absolute week in the year (1–52)
        self.internal_phase_week = 1  # Week inside the current phase
        self.current_day_index = 0  # Monday = 0
        self.season_phase = "Preseason"
        self.playoff_subphase = None
        self.offseason_subphase = None
        self._setup_phase_boundaries()

    def _setup_phase_boundaries(self):
        self.phase_boundaries = {
            "Preseason": (1, 4),
            "Regular Season": (5, 18),
            "Playoffs": (19, 22),
            "Offseason": (23, 52),
        }

        self.playoff_subphases = {
            19: "Wild Card Round",
            20: "Divisional Round",
            21: "Conference Championships",
            22: "League Championship"
        }

        self.offseason_subphases = {
            (23, 25): "Combine",
            (26, 29): "Free Agency",
            (30, 32): "Draft",
            (33, 36): "Rookie Camp",
            (37, 40): "Minicamp",
            (41, 52): "Dead Period"
        }

    def advance_week(self):
        """Advances the calendar by one week, adjusting phase and subphase if necessary."""
        self.current_week += 1
        self.current_day_index = (self.current_day_index + 7) % 7  # Move forward by 7 days (still Monday)
        
        if self.current_week > 52:
            self.start_new_year()

        previous_phase = self.season_phase
        self.update_phase()

        if self.season_phase != previous_phase:
            self.internal_phase_week = 1
        else:
            self.internal_phase_week += 1

    def update_phase(self):
        """Updates the season phase and any active subphase based on the current week."""
        for phase, (start_week, end_week) in self.phase_boundaries.items():
            if start_week <= self.current_week <= end_week:
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

    def get_offseason_subphase(self):
        """Finds which offseason subphase matches the current week."""
        for (start_week, end_week), subphase_name in self.offseason_subphases.items():
            if start_week <= self.current_week <= end_week:
                return subphase_name
        return None

    def start_new_year(self):
        """Resets calendar for a new season."""
        self.current_year += 1
        self.current_week = 1
        self.internal_phase_week = 1
        self.season_phase = "Preseason"
        self.playoff_subphase = None
        self.offseason_subphase = None

    def get_display_info(self):
        """Returns clean display info: Year, Phase, Week, Day of Week."""
        day_of_week = self.DAYS_OF_WEEK[self.current_day_index]

        if self.season_phase == "Preseason":
            label = f"Preseason Week {self.internal_phase_week}"
        elif self.season_phase == "Regular Season":
            label = f"Regular Season Week {self.internal_phase_week}"
        elif self.season_phase == "Playoffs":
            label = self.playoff_subphase or "Playoffs"
        elif self.season_phase == "Offseason":
            label = self.offseason_subphase or "Offseason"
        else:
            label = self.season_phase

        return {
            "Year": self.current_year,
            "Phase": self.season_phase,
            "Label": label,
            "Day of Week": day_of_week
        }

    def serialize(self):
        """Prepares calendar data for saving."""
        return {
            "current_year": self.current_year,
            "current_week": self.current_week,
            "internal_phase_week": self.internal_phase_week,
            "season_phase": self.season_phase,
            "playoff_subphase": self.playoff_subphase,
            "offseason_subphase": self.offseason_subphase,
            "current_day_index": self.current_day_index
        }

    def deserialize(self, data):
        """Loads calendar data from save."""
        self.current_year = data.get("current_year", 2025)
        self.current_week = data.get("current_week", 1)
        self.internal_phase_week = data.get("internal_phase_week", 1)
        self.season_phase = data.get("season_phase", "Preseason")
        self.playoff_subphase = data.get("playoff_subphase", None)
        self.offseason_subphase = data.get("offseason_subphase", None)
        self.current_day_index = data.get("current_day_index", 0)
