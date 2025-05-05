DAYS_OF_WEEK = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]

DEFAULT_SCHEDULE = {
    "Monday": "Film Session",
    "Tuesday": "Gym Training",
    "Wednesday": "Tactical Practice",
    "Thursday": "Special Teams",
    "Friday": "Conditioning",
    "Saturday": "Rest",
    "Sunday": "Game"
}

class TrainingScheduleManager:
    def __init__(self, team):
        self.team = team
        self.schedule = DEFAULT_SCHEDULE.copy()

    def assign_training_slot(self, day, activity):
        if day in DAYS_OF_WEEK:
            self.schedule[day] = activity
        else:
            print(f"Invalid day: {day}")

    def get_daily_activity(self, day):
        return self.schedule.get(day, "Rest")

    def print_schedule(self):
        print(f"\nTraining Schedule for {self.team.name}")
        for day in DAYS_OF_WEEK:
            print(f"{day}: {self.schedule.get(day, 'Rest')}")
