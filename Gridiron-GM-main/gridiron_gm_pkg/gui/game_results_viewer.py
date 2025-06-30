import tkinter as tk
from tkinter import ttk

class GameResultsViewer:
    def __init__(self, master, game_world):
        self.top = tk.Toplevel(master)
        self.top.title("Weekly Game Results")
        self.game_world = game_world

        self.build_ui()

    def build_ui(self):
        self.week_selector = ttk.Combobox(self.top, values=[f"Week {i+1}" for i in range(18)])
        self.week_selector.current(self.game_world['calendar']['week_number'] - 1)
        self.week_selector.bind("<<ComboboxSelected>>", self.display_results)
        self.week_selector.pack(pady=10)

        self.results_list = tk.Listbox(self.top, width=60)
        self.results_list.pack(padx=10, pady=10)

        self.display_results()

    def display_results(self, event=None):
        selected_week = self.week_selector.current()
        self.results_list.delete(0, tk.END)

        week_data = self.game_world.get('weekly_results', {}).get(selected_week, [])
        if not week_data:
            self.results_list.insert(tk.END, "No results available for this week.")
        else:
            for game in week_data:
                home = game['home_team']
                away = game['away_team']
                score = f"{away['name']} {away['score']} @ {home['name']} {home['score']}"
                self.results_list.insert(tk.END, score)

# Example usage to test UI
if __name__ == "__main__":
    def mock_game_world():
        return {
            'calendar': {'week_number': 1},
            'weekly_results': {
                0: [
                    {'home_team': {'name': 'Houston Bulls', 'score': 21}, 'away_team': {'name': 'Vegas Vultures', 'score': 17}},
                    {'home_team': {'name': 'Miami Kraken', 'score': 30}, 'away_team': {'name': 'Dallas Bandits', 'score': 28}}
                ]
            }
        }

    root = tk.Tk()
    root.withdraw()
    viewer = GameResultsViewer(root, mock_game_world())
    root.mainloop()
