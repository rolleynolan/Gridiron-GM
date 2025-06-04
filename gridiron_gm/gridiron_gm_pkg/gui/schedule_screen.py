import tkinter as tk
from tkinter import ttk, font
from datetime import datetime

class ScheduleScreen(tk.Frame):
    def __init__(self, master, schedule_data, team_records):
        super().__init__(master)
        self.schedule_data = schedule_data
        self.team_records = team_records
        self.pack(fill=tk.BOTH, expand=True)

        # Fonts
        self.bold_font = font.Font(weight="bold")
        self.normal_font = font.Font(weight="normal")
        self.large_font = font.Font(size=12, weight="bold")
        self.small_font = font.Font(size=10)

        # Week Dropdown
        tk.Label(self, text="Select Week:").pack(pady=5)
        self.week_var = tk.StringVar(value="1")
        week_list = sorted(self.schedule_data.keys(), key=int)
        self.week_selector = ttk.Combobox(self, state="readonly", values=week_list, textvariable=self.week_var)
        self.week_selector.bind("<<ComboboxSelected>>", self.display_week)
        self.week_selector.pack()

        # Labels
        self.week_label = tk.Label(self, text="Week 1 Matchups", font=self.large_font)
        self.week_label.pack(pady=3)
        self.sub_label = tk.Label(self, text="", font=self.small_font)
        self.sub_label.pack(pady=1)

        # Treeview
        columns = ("Day", "Date", "Home", "Score", "Away")
        self.tree = ttk.Treeview(self, columns=columns, show="headings")
        for col in columns:
            self.tree.heading(col, text=col)
            self.tree.column(col, width=140 if col != "Score" else 80, anchor=tk.CENTER)
        self.tree.pack(padx=10, pady=5, fill=tk.BOTH, expand=True)

        self.display_week()

    def display_week(self, event=None):
        week = self.week_var.get()
        games = self.schedule_data.get(week, [])

        # Count games finished
        completed = sum(1 for g in games if g.get("home_score") is not None)
        total = len(games)

        # Unified font
        self.week_label.config(font=("Arial", 12, "bold"))
        self.sub_label.config(font=("Arial", 10))

        if completed == total:
            self.week_label.config(text=f"Week {week} Final Scores")
            self.sub_label.config(text="All games completed — final scores below.")
        elif completed > 0:
            self.week_label.config(text=f"Week {week} In Progress")
            self.sub_label.config(text="Some games completed — scores updating live.")
        else:
            self.week_label.config(text=f"Week {week} Matchups")
            self.sub_label.config(text="Upcoming matchups and kickoff times.")

        self.tree.delete(*self.tree.get_children())

        for game in games:
            home = game["home"]
            away = game["away"]
            home_score = game.get("home_score")
            away_score = game.get("away_score")
            has_scores = home_score is not None and away_score is not None

            # Format date
            raw_date = game.get("date", "")
            date_str = self.format_date(raw_date)
            when_display = date_str if has_scores else f"{date_str} @ {game.get('time', 'TBD')}"

            # Team names with records
            home_display = f"{home} {self.get_record_str(home)}"
            away_display = f"{away} {self.get_record_str(away)}"

            # Highlight winner with asterisk (temporary workaround for per-cell styling)
            if has_scores:
                if home_score > away_score:
                    home_display = f"* {home_display}"
                elif away_score > home_score:
                    away_display = f"* {away_display}"

            score_display = f"{home_score} - {away_score}" if has_scores else "—"

            self.tree.insert("", tk.END, values=(
                game.get("day", "Sunday"),
                when_display,
                home_display,
                score_display,
                away_display
            ))


    def get_record_str(self, team_name):
        rec = self.team_records.get(team_name, (0, 0))
        return f"({rec[0]}-{rec[1]})"

    def format_date(self, date_str):
        try:
            date_obj = datetime.strptime(date_str, "%Y-%m-%d")
            return date_obj.strftime("%m/%d/%y")
        except Exception:
            return date_str
