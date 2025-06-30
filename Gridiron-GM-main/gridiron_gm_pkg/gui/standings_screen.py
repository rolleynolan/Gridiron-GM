import tkinter as tk
from tkinter import ttk

class StandingsScreen(tk.Frame):
    def __init__(self, master, standings_data):
        super().__init__(master)
        self.standings_data = standings_data  # dict with 'Nova' and 'Atlas' as keys
        self.pack(fill=tk.BOTH, expand=True)

        self.build_conference_section("Nova Conference", self.standings_data.get("Nova", []))
        self.build_conference_section("Atlas Conference", self.standings_data.get("Atlas", []))

    def build_conference_section(self, title, teams):
        tk.Label(self, text=title, font=("Arial", 14, "bold")).pack(pady=5)

        columns = ("Team", "W", "L", "T", "PF", "PA")
        tree = ttk.Treeview(self, columns=columns, show="headings", height=len(teams))
        for col in columns:
            tree.heading(col, text=col)
            tree.column(col, width=80, anchor=tk.CENTER)
        tree.column("Team", width=200, anchor=tk.W)

        for team in teams:
            tree.insert("", tk.END, values=(
                f"{team['city']} {team['name']}",
                team["wins"], team["losses"], team["ties"],
                team["points_for"], team["points_against"]
            ))

        tree.pack(pady=5, padx=10, fill=tk.X)
