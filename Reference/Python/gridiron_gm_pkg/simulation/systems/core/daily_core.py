from __future__ import annotations

from typing import Any

from gridiron_gm_pkg.simulation.systems.player.fatigue import FatigueSystem


class CoreDailyOperations:
    """Daily core-loop work: games, recovery, and availability."""

    def __init__(self, season_manager: Any) -> None:
        self.season_manager = season_manager
        self.calendar = season_manager.calendar
        self.league = season_manager.league
        self.fatigue_system = FatigueSystem()

    def process_end_of_day(self) -> None:
        self.season_manager.simulate_games_for_today()
        for team in getattr(self.league, "teams", []) or []:
            for player in getattr(team, "roster", []) or []:
                self.fatigue_system.recover(player, context="between_games")
