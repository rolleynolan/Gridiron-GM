from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.player_progression import progress_player
from gridiron_gm.gridiron_gm_pkg.simulation.systems.player.fatigue import FatigueSystem
from gridiron_gm.gridiron_gm_pkg.simulation.systems.transactions.free_agency_manager import FreeAgencyManager
from gridiron_gm.gridiron_gm_pkg.simulation.systems.game.offseason_manager import OffseasonManager


class DailyOperationsManager:
    """Handles all non-game operations that occur each day."""

    def __init__(self, season_manager):
        self.season_manager = season_manager
        self.calendar = season_manager.calendar
        self.league = season_manager.league
        self.fatigue_system = FatigueSystem()
        self.free_agency_manager = FreeAgencyManager(self.league)
        self.offseason_manager = OffseasonManager(self.league)

    def process_end_of_day(self):
        """Run all daily updates after games have been played."""
        # Simulate scheduled games for today
        self.season_manager.simulate_games_for_today()

        # Training progression and fatigue recovery
        for team in self.league.teams:
            for player in getattr(team, "roster", []):
                progress_player(player, {})
                self.fatigue_system.recover(player, context="between_games")

        # Free agency / trade logic
        self.free_agency_manager.advance_free_agency_day()

        # Offseason step if applicable
        if self.calendar.season_phase == "Offseason":
            self.offseason_manager.step()

