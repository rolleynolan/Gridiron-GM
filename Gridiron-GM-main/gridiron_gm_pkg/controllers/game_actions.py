from __future__ import annotations

"""Lightweight controller helpers for in-game menu actions."""

from typing import List, Any

from gridiron_gm_pkg.gui.free_agency_screen import free_agency_screen
from gridiron_gm_pkg.gui.scouting_menu import scouting_menu
from gridiron_gm_pkg.simulation.AI.cpu_trade_offers import generate_cpu_trade_offers


class SimpleScoutingSystem:
    """Minimal scouting system to track assignments."""

    def __init__(self) -> None:
        self.assignments: List[dict] = []

    def assign_task(self, scout: Any, task_type: str, player: Any) -> None:
        self.assignments.append({"scout": scout, "task": task_type, "player": player})
        # naive implementation: mark player fully scouted
        setattr(player, "scouted", True)
        setattr(player, "scouting_progress", 100)
        if not hasattr(player, "projected_overall"):
            setattr(player, "projected_overall", getattr(player, "overall", 0))
        if not hasattr(player, "projected_potential"):
            pot = getattr(player, "potential", getattr(player, "overall", 0))
            setattr(player, "projected_potential", pot)


class GameActionController:
    """Controller exposing backend actions for the text UI."""

    def __init__(self, game_world: dict) -> None:
        self.game_world = game_world
        # ensure a scouting system exists
        if "scouting_system" not in self.game_world:
            self.game_world["scouting_system"] = SimpleScoutingSystem()

    # ------------------------------------------------------------------
    def free_agency_hub(self) -> None:
        """Launch the free agency CLI screen."""
        user_team = self.game_world.get("user_team")
        free_agency_screen(self.game_world, user_team)

    # ------------------------------------------------------------------
    def scouting_department(self) -> None:
        """Open scouting management UI."""
        rookie_class = self.game_world.get("rookie_class", [])
        scouts = self.game_world.get("scouts", [])
        scouting_system = self.game_world["scouting_system"]
        scouting_menu(rookie_class, scouts, scouting_system)

    # ------------------------------------------------------------------
    def trade_center(self) -> None:
        """Display simple CPU trade offers."""
        teams = self.game_world.get("teams", [])
        week = self.game_world.get("week_number", 1)
        offers = generate_cpu_trade_offers(teams, week)
        if not offers:
            print("\nNo trade activity today.")
            return

        print("\n=== CPU Trade Offers ===")
        for offer in offers:
            players = ", ".join(p.name for p in offer["players_from_seller"])
            picks = ", ".join(offer.get("picks_from_buyer", []))
            print(f"{offer['from_team']} -> {offer['to_team']}: {players} for {picks} ({offer['status']})")
