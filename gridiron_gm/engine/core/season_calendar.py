class Calendar:
    def __init__(self):
        self.day = 1
        self.week = 1
        self.season_phase = "PRESEASON"  # Options: PRESEASON, REG_SEASON, PLAYOFFS, OFFSEASON
        self.max_weeks = 17  # Regular season length for example

    def advance_day(self):
        print(f"Advancing from Day {self.day}")
        self.day += 1
        # Optionally trigger daily events here

    def advance_week(self):
        print(f"Advancing from Week {self.week}")
        self.week += 1
        self.day = 1
        self._check_phase_transition()

    def _check_phase_transition(self):
        if self.week > self.max_weeks and self.season_phase == "REG_SEASON":
            self.season_phase = "PLAYOFFS"
            print("Entering PLAYOFFS!")
        elif self.season_phase == "PLAYOFFS":
            self.season_phase = "OFFSEASON"
            print("Entering OFFSEASON!")

    def get_current_phase(self):
        return self.season_phase

    def get_week(self):
        return self.week

    def get_day(self):
        return self.day
