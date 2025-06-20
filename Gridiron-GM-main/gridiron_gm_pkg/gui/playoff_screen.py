import tkinter as tk
from tkinter import ttk

class PlayoffScreen(tk.Frame):
    def __init__(self, master, bracket_data):
        super().__init__(master)
        self.bracket_data = bracket_data
        self.pack(fill=tk.BOTH, expand=True)

        title = tk.Label(self, text="Playoff Bracket", font=("Arial", 16, "bold"))
        title.grid(row=0, column=0, columnspan=3, pady=10)

        # Headings
        tk.Label(self, text="Nova Conference", font=("Arial", 12, "bold")).grid(row=1, column=0, padx=20)
        tk.Label(self, text="", width=5).grid(row=1, column=1)  # Spacer
        tk.Label(self, text="Atlas Conference", font=("Arial", 12, "bold")).grid(row=1, column=2, padx=20)

        # Build both brackets side by side
        self.build_conference_bracket(self.bracket_data.get("Nova", []), side="left", col=0)
        self.build_conference_bracket(self.bracket_data.get("Atlas", []), side="right", col=2)

    def build_conference_bracket(self, teams, side, col):
        row = 2
        spacer = tk.Label(self, text="", height=1)
        spacer.grid(row=row, column=col)

        # Sort teams by seed
        teams_by_seed = {team['seed']: team for team in teams}
        ordered_seeds = [1, 2, 3, 4]

        for seed in ordered_seeds:
            team_info = teams_by_seed.get(seed)
            if not team_info:
                continue

            # BYE case (seed 1)
            if team_info['opponent'] is None:
                bye_label = f"({seed}) {team_info['team']} — BYE"
                tk.Label(self, text=bye_label, font=("Arial", 10)).grid(row=row, column=col, sticky="w", padx=20)
                row += 2
                continue

            opp = team_info['opponent']
            score = team_info.get('score', "")

            # First team line
            line = f"({seed}) {team_info['team']} vs ({opp['seed']}) {opp['team']}"
            if score:
                line += f" — {score}"

            tk.Label(self, text=line, font=("Arial", 10)).grid(row=row, column=col, sticky="w", padx=20)
            row += 2
