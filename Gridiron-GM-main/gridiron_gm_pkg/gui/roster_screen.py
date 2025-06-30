import tkinter as tk
from tkinter import ttk
from typing import Any, Dict, List

class RosterScreen(tk.Frame):
    """
    Tkinter-based roster viewer for Gridiron GM.
    Supports team/position filtering, sorting, and player detail popups.
    """

    def __init__(self, master: Any, game_world: Dict[str, Any]):
        super().__init__(master)
        self.game_world = game_world
        self.pack(fill=tk.BOTH, expand=True)

        self.sort_column: str = ""
        self.sort_reverse: bool = False

        self.team_selector = ttk.Combobox(self, state="readonly")
        self.team_selector['values'] = [f"{t['city']} {t['name']}" for t in game_world["all_teams"]]
        self.team_selector.bind("<<ComboboxSelected>>", self.display_roster)
        self.team_selector.pack(pady=5)

        self.position_filter = ttk.Combobox(self, state="readonly")
        self.position_filter['values'] = ["All", "QB", "RB", "WR", "TE", "LT", "RT", "LG", "RG", "C", "DE", "DT", "LB", "CB", "S", "K", "P"]
        self.position_filter.current(0)
        self.position_filter.bind("<<ComboboxSelected>>", self.update_filters)
        self.position_filter.pack(pady=5)

        # Search bar
        search_frame = tk.Frame(self)
        search_frame.pack(pady=5)

        tk.Label(search_frame, text="Search:").pack(side=tk.LEFT)
        self.search_entry = tk.Entry(search_frame)
        self.search_entry.pack(side=tk.LEFT)
        self.search_entry.bind("<KeyRelease>", self.update_filters)

        # Table setup
        self.columns = ("Name", "Pos", "OVR", "POT", "Age", "Fatigue", "Morale", "Traits", "Injured", "Contract")
        self.roster_table = ttk.Treeview(self, columns=self.columns, show="headings")

        for col in self.columns:
            self.roster_table.heading(col, text=col, command=lambda c=col: self.sort_by_column(c))
            self.roster_table.column(col, width=100, anchor=tk.CENTER)

        self.roster_table.pack(fill=tk.BOTH, expand=True)
        self.roster_table.bind("<Double-1>", self.show_player_popup)

        self.current_team: Dict[str, Any] = {}
        self.current_roster: List[Dict[str, Any]] = []

    def display_roster(self, event: Any) -> None:
        """Display selected team's roster."""
        selected = self.team_selector.get()
        self.current_team = next(
            (t for t in self.game_world["all_teams"] if f"{t['city']} {t['name']}" == selected), None
        )
        self.roster_table.delete(*self.roster_table.get_children())
        if not self.current_team:
            self.current_roster = []
            return

        self.current_roster = self.current_team.get("roster", [])
        self.populate_table()

    def populate_table(self) -> None:
        """Populate the table based on filters."""
        self.roster_table.delete(*self.roster_table.get_children())
        pos_filter = self.position_filter.get()
        name_filter = self.search_entry.get().strip().lower()

        for player in self.current_roster:
            pos = player.get("position", "")
            name = player.get("name", "").lower()

            # Apply position filter
            if pos_filter != "All" and pos != pos_filter:
                continue

            # Apply name filter
            if name_filter and name_filter not in name:
                continue

            # Handle new-style trait dict or old flat list
            traits_val = player.get("traits", [])
            if isinstance(traits_val, dict):
                traits_str = ", ".join(trait for group in traits_val.values() for trait in group)
            else:
                traits_str = ", ".join(traits_val)

            values = (
                player.get("name"),
                pos,
                player.get("overall"),
                player.get("potential", ""),
                player.get("age"),
                round(player.get("fatigue", 0), 2),
                player.get("morale", 50),
                traits_str,
                "Yes" if player.get("injured", player.get("is_injured", False)) else "",
                self._format_contract(player.get("contract", {}))
            )

            tag = "injured" if player.get("injured", player.get("is_injured", False)) else ""
            self.roster_table.insert("", tk.END, values=values, tags=(tag,))

        self.roster_table.tag_configure("injured", foreground="red")

    def _format_contract(self, contract: Dict[str, Any]) -> str:
        salary = contract.get('salary', 0)
        years = contract.get('years', 0)
        if salary or years:
            return f"${salary}M/{years}y"
        return ""

    def sort_by_column(self, col: str) -> None:
        """Sort roster by the selected column."""
        key_map = {
            "Name": lambda p: p.get("name", ""),
            "Pos": lambda p: p.get("position", ""),
            "OVR": lambda p: p.get("overall", 0),
            "POT": lambda p: p.get("potential", 0),
            "Age": lambda p: p.get("age", 0),
            "Fatigue": lambda p: p.get("fatigue", 0),
            "Morale": lambda p: p.get("morale", 0),
            "Traits": lambda p: ", ".join(p.get("traits", [])) if isinstance(p.get("traits", []), list)
            else ", ".join(trait for group in p.get("traits", {}).values() for trait in group),
            "Injured": lambda p: p.get("injured", p.get("is_injured", False)),
            "Contract": lambda p: p.get("contract", {}).get("salary", 0),
        }

        if col not in key_map:
            return

        # Toggle sort direction if same column
        if self.sort_column == col:
            self.sort_reverse = not self.sort_reverse
        else:
            self.sort_column = col
            self.sort_reverse = False

        self.current_roster.sort(key=key_map[col], reverse=self.sort_reverse)
        self.populate_table()

    def show_player_popup(self, event: Any) -> None:
        """Show a popup with detailed info for the selected player."""
        selected_item = self.roster_table.selection()
        if not selected_item:
            return

        values = self.roster_table.item(selected_item, "values")
        if not values:
            return

        name, pos, ovr, pot, age, fatigue, morale, traits, injured, contract = values

        popup = tk.Toplevel(self)
        popup.title(f"{name} - Details")
        popup.geometry("350x300")

        fields = {
            "Name": name,
            "Position": pos,
            "Overall": ovr,
            "Potential": pot,
            "Age": age,
            "Fatigue": fatigue,
            "Morale": morale,
            "Traits": traits,
            "Injured": injured,
            "Contract": contract
        }

        for idx, (label, value) in enumerate(fields.items()):
            tk.Label(popup, text=f"{label}:", anchor="w", font=("Arial", 10, "bold")).grid(row=idx, column=0, sticky="w", padx=10, pady=2)
            tk.Label(popup, text=value, anchor="w", font=("Arial", 10)).grid(row=idx, column=1, sticky="w", padx=10, pady=2)

    def update_filters(self, event: Any = None) -> None:
        """Update table when filters/search are changed."""
        if self.current_team:
            self.populate_table()
