import tkinter as tk
from tkinter import ttk

class PlayerDetailModal:
    def __init__(self, master, player_data):
        self.top = tk.Toplevel(master)
        self.top.title(f"Player Profile: {player_data['name']}")
        self.player_data = player_data

        self.build_ui()

    def build_ui(self):
        notebook = ttk.Notebook(self.top)
        notebook.pack(fill='both', expand=True)

        # Overview Tab
        overview_tab = ttk.Frame(notebook)
        notebook.add(overview_tab, text='Overview')
        self.populate_overview(overview_tab)

        # Attributes Tab
        attr_tab = ttk.Frame(notebook)
        notebook.add(attr_tab, text='Attributes')
        self.populate_attributes(attr_tab)

        # Traits Tab
        traits_tab = ttk.Frame(notebook)
        notebook.add(traits_tab, text='Traits')
        self.populate_traits(traits_tab)

        # Stats Tab
        stats_tab = ttk.Frame(notebook)
        notebook.add(stats_tab, text='Career Stats')
        self.populate_stats(stats_tab)

    def populate_overview(self, tab):
        info = self.player_data
        fields = [
            ("Name", info['name']),
            ("Position", info['position']),
            ("Age", info['age']),
            ("School", info['school']),
            ("Drafted", info['draft_info']),
            ("Dev Curve", info.get('dev_curve', 'Unknown')),
        ]
        for i, (label, value) in enumerate(fields):
            ttk.Label(tab, text=f"{label}: {value}").grid(row=i, column=0, sticky='w', padx=10, pady=2)

    def populate_attributes(self, tab):
        for i, (attr, val) in enumerate(self.player_data['attributes'].items()):
            ttk.Label(tab, text=f"{attr}: {val}").grid(row=i, column=0, sticky='w', padx=10, pady=2)

    def populate_traits(self, tab):
        for i, trait in enumerate(self.player_data['traits']):
            ttk.Label(tab, text=f"- {trait}").grid(row=i, column=0, sticky='w', padx=10, pady=2)

    def populate_stats(self, tab):
        stats = self.player_data.get('career_stats', {})
        for i, (year, data) in enumerate(stats.items()):
            if not isinstance(data, dict):
                continue  # Skip malformed stats

            row_text = f"{year}: " + ", ".join([f"{k}: {v}" for k, v in data.items()])
            ttk.Label(tab, text=row_text).grid(row=i, column=0, sticky='w', padx=10, pady=2)


# Example usage for integration
def launch_example():
    root = tk.Tk()
    root.withdraw()
    sample_data = {
        'name': 'Jamal Rivers',
        'position': 'QB',
        'age': 23,
        'school': 'Ohio State',
        'draft_info': '2025, Round 1, Pick 5',
        'dev_curve': 'Late Bloomer',
        'attributes': {
            'Throw Power': 92,
            'Accuracy': 85,
            'Speed': 78,
            'Awareness': 70
        },
        'traits': ['Film Junkie', 'Clutch Performer', 'Team-First'],
        'career_stats': {
            '2025': {'Yards': 3400, 'TDs': 26, 'INTs': 10},
            '2026': {'Yards': 3900, 'TDs': 31, 'INTs': 8}
        }
    }
    PlayerDetailModal(root, sample_data)
    root.mainloop()
