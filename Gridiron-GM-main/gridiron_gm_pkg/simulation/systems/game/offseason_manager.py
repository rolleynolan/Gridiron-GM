<<<<<<< HEAD
from typing import List, Dict, Optional
from datetime import timedelta
import random
from gridiron_gm_pkg.simulation.engine.contract_engine import ContractEngine  # Add this import at the top

class OffseasonManager:
    """
    Manages the Gridiron GM offseason, handling all phases between the day after the championship
    (Gridiron Bowl) and the first day of preseason. Integrates with the LeagueManager and its calendar.
    Tracks overlapping phases, triggers event stubs, and manages year rollover.
    """

    # Define the order and duration (in days) of each phase
    PHASES = [
        {"name": "Combine", "duration": 7, "overlaps": []},
        {"name": "Draft", "duration": 3, "overlaps": []},
        {"name": "Free Agency", "duration": 30, "overlaps": ["Combine", "Draft", "Rookie Minicamp", "Team Minicamp"]},
        {"name": "Rookie Minicamp", "duration": 3, "overlaps": []},
        {"name": "Team Minicamp", "duration": 5, "overlaps": []},
        {"name": "Training Camp", "duration": 14, "overlaps": []},
    ]

    def __init__(self, league_manager):
        """
        Args:
            league_manager: Reference to the LeagueManager instance.
        """
        self.league_manager = league_manager
        self.calendar = league_manager.calendar
        self.league_year = getattr(league_manager, "year", 1)
        self.offseason_start_date = None
        self.phase_schedule = []  # List of dicts: {name, start, end}
        self.active_phases: List[str] = []
        self.current_day = None
        self._setup_phase_schedule()

    def _setup_phase_schedule(self):
        """
        Initializes the phase schedule based on the day after the Gridiron Bowl.
        """
        self.offseason_start_date = self.calendar.get_day_after_championship()
        day = self.offseason_start_date
        self.phase_schedule = []
        # Build phase schedule from PHASES (which has 'duration')
        for phase_def in self.PHASES:
            start = day
            end = day + timedelta(days=phase_def["duration"] - 1)
            self.phase_schedule.append({
                "name": phase_def["name"],
                "start": start,
                "end": end,
                "duration": phase_def["duration"],
                "overlaps": phase_def.get("overlaps", [])
            })
            # Free Agency starts at the same time as Combine, but lasts longer and overlaps
            if phase_def["name"] != "Free Agency":
                day = end + timedelta(days=1)
        # Free Agency may overlap, so adjust its start/end if needed
        for phase in self.phase_schedule:
            if phase["name"] == "Free Agency":
                phase["start"] = self.phase_schedule[0]["start"]  # Start with Combine
                phase["end"] = phase["start"] + timedelta(days=phase["duration"] - 1)

    def step(self):
        """
        Advances the offseason by one day, checks the calendar, and triggers phase events.
        Should be called once per day by the main league loop.
        Ensures college/draft pools are updated and draft is run at the correct phase.
        """
        # --- Ensure offseason setup happens first ---
        if not hasattr(self, "_offseason_initialized") or not self._offseason_initialized:
            self.refresh_college_and_draft_classes()
            self._offseason_initialized = True

        self.current_day = self.calendar.current_day
        self._update_active_phases()
        self._handle_phase_transitions()
        self._handle_year_rollover_if_needed()
        self._apply_phase_effects()

    def refresh_college_and_draft_classes(self):
        """
        Updates the college player database and generates the new draft class at the start of every offseason.
        Calls DraftManager.generate_draft_class for realism.
        """
        league = self.league_manager

        # 1. Increment year_in_college for all college players
        for player in getattr(league, "college_db", []):
            player.year_in_college = getattr(player, "year_in_college", 1) + 1

        # 2. Remove players who have graduated (year_in_college > 4)
        league.college_db = [p for p in league.college_db if getattr(p, "year_in_college", 1) <= 4]

        # 3. Generate and add new freshman class
        from gridiron_gm_pkg.simulation.utils.college_player_generator import generate_freshman_class
        new_freshmen = generate_freshman_class()
        league.college_db.extend(new_freshmen)

        # 4. Generate this year's draft class using DraftManager for realism
        from gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager
        draft_manager = DraftManager(league, None)  # TransactionManager will be set later for run_draft
        year = getattr(self.calendar, "current_year", 1)
        draft_class = draft_manager.generate_draft_class(year, n_players=256)
        league.draft_prospects = draft_class

    def _update_active_phases(self):
        """
        Updates the list of currently active phases based on the current day.
        """
        self.active_phases = [
            phase["name"]
            for phase in self.phase_schedule
            if phase["start"] <= self.current_day <= phase["end"]
        ]

    def _handle_phase_transitions(self):
        """
        Triggers stub event handlers for phase starts and ends.
        """
        for phase in self.phase_schedule:
            # Phase start
            if self.current_day == phase["start"]:
                self.on_phase_start(phase["name"])
            # Phase end
            if self.current_day == phase["end"]:
                self.on_phase_end(phase["name"])

    def _handle_year_rollover_if_needed(self):
        """
        Handles league year rollover and resets stats if the preseason is about to begin.
        """
        preseason_start = self.phase_schedule[-1]["end"] + timedelta(days=1)
        if self.current_day == preseason_start:
            self.league_year += 1
            self.league_manager.year = self.league_year
            self.league_manager.reset_league_stats()
            self.on_offseason_end()

    def _apply_phase_effects(self):
        """
        Applies the effects of each active phase to players, rosters, depth charts, and contracts.
        """
        for phase in self.active_phases:
            if phase == "Combine":
                self._apply_combine_effects()
            elif phase == "Draft":
                self._apply_draft_effects()
            elif phase == "Free Agency":
                self._apply_free_agency_effects()
            elif phase == "Rookie Minicamp":
                self._apply_rookie_minicamp_effects()
            elif phase == "Team Minicamp":
                self._apply_team_minicamp_effects()
            elif phase == "Training Camp":
                self._apply_training_camp_effects()

    # --- Phase Effect Methods (placeholders for future UI integration) ---

    def _apply_combine_effects(self):
        """
        NFL Combine: Evaluate draft prospects, update player ratings based on combine performance.
        """
        # Placeholder: In a real UI, show combine results and allow user/team to scout.
        for player in self.league_manager.get_draft_prospects():
            # Simulate combine performance and update ratings
            player.update_ratings_from_combine()
        # print("[Combine] Player ratings updated based on combine results.")

    def _apply_draft_effects(self):
        """
        NFL Draft: Teams select rookies, update roster composition and depth charts.
        Triggers the draft using DraftManager.
        """
        from gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager
        from gridiron_gm_pkg.simulation.systems.roster.transaction_manager import TransactionManager

        # Ensure transaction manager is available
        transaction_manager = TransactionManager(self.league_manager)
        draft_manager = DraftManager(self.league_manager, transaction_manager)
        draft_manager.run_draft()
        # Optionally: store or log draft_manager.draft_history for reporting

    def _apply_free_agency_effects(self):
        """
        Free Agency: Teams sign free agents, update contracts and rosters.
        Enhanced: Uses ContractEngine for realistic financial negotiation logic.
        """
        self.handle_free_agent_negotiations()

    def handle_free_agent_negotiations(self):
        """
        Handles free agent negotiations using ContractEngine for offer generation and evaluation.
        """
        contract_engine = ContractEngine()
        for team in self.league_manager.teams:
            available_cap = getattr(team, "salary_cap", 200_000_000) - getattr(team, "payroll", 0)
            free_agents = self.league_manager.get_available_free_agents()
            for player in free_agents:
                # Generate an offer using ContractEngine
                offer = contract_engine.calculate_offer(team, player, available_cap)
                if offer is None:
                    continue  # Skip if no valid offer can be made
                # Evaluate if the offer is acceptable to the player
                if contract_engine.is_offer_acceptable(player, offer, market_value=contract_engine.get_market_value(player)):
                    # Sign the player
                    player.contract = offer
                    player.team = team
                    team.roster.append(player)
                    team.payroll += offer.get("salary", 0)
                    available_cap -= offer.get("salary", 0)
                    self.league_manager.remove_free_agent(player)
            # Optionally update team finances after signings
            team.payroll = sum(getattr(p, "contract", {}).get("salary", 0) for p in team.roster)

    def _apply_training_camp_effects(self):
        """
        Training Camp: Intense practice, larger rating and chemistry boost, depth chart battles.
        Adds random variation to simulate gains and rare declines.
        """
        for team in self.league_manager.teams:
            for player in team.roster:
                # Random variation: most improve, some stagnate, rare decline
                change = random.choices(
                    [random.uniform(1.0, 2.0), random.uniform(0.0, 1.0), random.uniform(-1.0, 0.0)],
                    weights=[0.7, 0.25, 0.05]
                )[0]
                player.rating = max(40, min(player.rating + change, 99))
                # Chemistry boost
                player.chemistry = min(getattr(player, "chemistry", 0.5) + random.uniform(0.01, 0.04), 1.0)
            # Placeholder: In a real UI, resolve depth chart battles and position competitions.
        # print("[Training Camp] Players improved, chemistry increased, depth chart battles resolved.")

    # --- UI Integration Placeholders ---

    def on_phase_start(self, phase_name: str):
        """
        Called when a phase begins. Extend for custom logic or UI triggers.
        """
        # Example: print(f"Phase '{phase_name}' has started.")
        pass

    def on_phase_end(self, phase_name: str):
        """
        Called when a phase ends. Extend for custom logic or UI triggers.
        """
        # Example: print(f"Phase '{phase_name}' has ended.")
        pass

    def on_offseason_end(self):
        """
        Called when the offseason ends and preseason begins.
        """
        # Example: print("Offseason complete. Preseason begins!")
        pass

    def get_active_phases(self) -> List[str]:
        """
        Returns a list of currently active phases.
        """
        return self.active_phases

    def get_current_day(self) -> int:
        """
        Returns the current offseason day (relative to offseason start).
        """
        if self.current_day is None or self.offseason_start_date is None:
            return 0
        return (self.current_day - self.offseason_start_date).days + 1

    def get_phase_schedule(self) -> List[Dict]:
        """
        Returns the full schedule of offseason phases with their start and end days.
        """
        return self.phase_schedule

    def is_phase_active(self, phase_name: str) -> bool:
        """
        Returns True if the given phase is currently active.
        """
=======
from typing import List, Dict, Optional
from datetime import timedelta
import random
from gridiron_gm_pkg.simulation.engine.contract_engine import ContractEngine  # Add this import at the top

class OffseasonManager:
    """
    Manages the Gridiron GM offseason, handling all phases between the day after the championship
    (Gridiron Bowl) and the first day of preseason. Integrates with the LeagueManager and its calendar.
    Tracks overlapping phases, triggers event stubs, and manages year rollover.
    """

    # Define the order and duration (in days) of each phase
    PHASES = [
        {"name": "Combine", "duration": 7, "overlaps": []},
        {"name": "Draft", "duration": 3, "overlaps": []},
        {"name": "Free Agency", "duration": 30, "overlaps": ["Combine", "Draft", "Rookie Minicamp", "Team Minicamp"]},
        {"name": "Rookie Minicamp", "duration": 3, "overlaps": []},
        {"name": "Team Minicamp", "duration": 5, "overlaps": []},
        {"name": "Training Camp", "duration": 14, "overlaps": []},
    ]

    def __init__(self, league_manager):
        """
        Args:
            league_manager: Reference to the LeagueManager instance.
        """
        self.league_manager = league_manager
        self.calendar = league_manager.calendar
        self.league_year = getattr(league_manager, "year", 1)
        self.offseason_start_date = None
        self.phase_schedule = []  # List of dicts: {name, start, end}
        self.active_phases: List[str] = []
        self.current_day = None
        self._setup_phase_schedule()

    def _setup_phase_schedule(self):
        """
        Initializes the phase schedule based on the day after the Gridiron Bowl.
        """
        self.offseason_start_date = self.calendar.get_day_after_championship()
        day = self.offseason_start_date
        self.phase_schedule = []
        # Build phase schedule from PHASES (which has 'duration')
        for phase_def in self.PHASES:
            start = day
            end = day + timedelta(days=phase_def["duration"] - 1)
            self.phase_schedule.append({
                "name": phase_def["name"],
                "start": start,
                "end": end,
                "duration": phase_def["duration"],
                "overlaps": phase_def.get("overlaps", [])
            })
            # Free Agency starts at the same time as Combine, but lasts longer and overlaps
            if phase_def["name"] != "Free Agency":
                day = end + timedelta(days=1)
        # Free Agency may overlap, so adjust its start/end if needed
        for phase in self.phase_schedule:
            if phase["name"] == "Free Agency":
                phase["start"] = self.phase_schedule[0]["start"]  # Start with Combine
                phase["end"] = phase["start"] + timedelta(days=phase["duration"] - 1)

    def step(self):
        """
        Advances the offseason by one day, checks the calendar, and triggers phase events.
        Should be called once per day by the main league loop.
        Ensures college/draft pools are updated and draft is run at the correct phase.
        """
        # --- Ensure offseason setup happens first ---
        if not hasattr(self, "_offseason_initialized") or not self._offseason_initialized:
            self.refresh_college_and_draft_classes()
            self._offseason_initialized = True

        self.current_day = self.calendar.current_day
        self._update_active_phases()
        self._handle_phase_transitions()
        self._handle_year_rollover_if_needed()
        self._apply_phase_effects()

    def refresh_college_and_draft_classes(self):
        """
        Updates the college player database and generates the new draft class at the start of every offseason.
        Calls DraftManager.generate_draft_class for realism.
        """
        league = self.league_manager

        # 1. Increment year_in_college for all college players
        for player in getattr(league, "college_db", []):
            player.year_in_college = getattr(player, "year_in_college", 1) + 1

        # 2. Remove players who have graduated (year_in_college > 4)
        league.college_db = [p for p in league.college_db if getattr(p, "year_in_college", 1) <= 4]

        # 3. Generate and add new freshman class
        from gridiron_gm_pkg.simulation.utils.college_player_generator import generate_freshman_class
        new_freshmen = generate_freshman_class()
        league.college_db.extend(new_freshmen)

        # 4. Generate this year's draft class
        self.create_rookie_draft_class()

    def create_rookie_draft_class(self, num_players: int = 256) -> None:
        """Generate and store the rookie draft class."""
        from gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager

        draft_manager = DraftManager(self.league_manager, None)
        draft_class = draft_manager.generate_draft_class(num_players)
        self.league_manager.draft_prospects = draft_class
        print(f"Draft class created with {len(draft_class)} players")

    def _update_active_phases(self):
        """
        Updates the list of currently active phases based on the current day.
        """
        self.active_phases = [
            phase["name"]
            for phase in self.phase_schedule
            if phase["start"] <= self.current_day <= phase["end"]
        ]

    def _handle_phase_transitions(self):
        """
        Triggers stub event handlers for phase starts and ends.
        """
        for phase in self.phase_schedule:
            # Phase start
            if self.current_day == phase["start"]:
                self.on_phase_start(phase["name"])
            # Phase end
            if self.current_day == phase["end"]:
                self.on_phase_end(phase["name"])

    def _handle_year_rollover_if_needed(self):
        """
        Handles league year rollover and resets stats if the preseason is about to begin.
        """
        preseason_start = self.phase_schedule[-1]["end"] + timedelta(days=1)
        if self.current_day == preseason_start:
            self.league_year += 1
            self.league_manager.year = self.league_year
            self.league_manager.reset_league_stats()
            self.on_offseason_end()

    def _apply_phase_effects(self):
        """
        Applies the effects of each active phase to players, rosters, depth charts, and contracts.
        """
        for phase in self.active_phases:
            if phase == "Combine":
                self._apply_combine_effects()
            elif phase == "Draft":
                self._apply_draft_effects()
            elif phase == "Free Agency":
                self._apply_free_agency_effects()
            elif phase == "Rookie Minicamp":
                self._apply_rookie_minicamp_effects()
            elif phase == "Team Minicamp":
                self._apply_team_minicamp_effects()
            elif phase == "Training Camp":
                self._apply_training_camp_effects()

    # --- Phase Effect Methods (placeholders for future UI integration) ---

    def _apply_combine_effects(self):
        """
        NFL Combine: Evaluate draft prospects, update player ratings based on combine performance.
        """
        # Placeholder: In a real UI, show combine results and allow user/team to scout.
        for player in self.league_manager.get_draft_prospects():
            # Simulate combine performance and update ratings
            player.update_ratings_from_combine()
        # print("[Combine] Player ratings updated based on combine results.")

    def _apply_draft_effects(self):
        """
        NFL Draft: Teams select rookies, update roster composition and depth charts.
        Triggers the draft using DraftManager.
        """
        from gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager
        from gridiron_gm_pkg.simulation.systems.roster.transaction_manager import TransactionManager

        # Ensure transaction manager is available
        transaction_manager = TransactionManager(self.league_manager)
        draft_manager = DraftManager(self.league_manager, transaction_manager)
        draft_manager.run_draft()
        # Optionally: store or log draft_manager.draft_history for reporting

    def _apply_free_agency_effects(self):
        """
        Free Agency: Teams sign free agents, update contracts and rosters.
        Enhanced: Uses ContractEngine for realistic financial negotiation logic.
        """
        self.handle_free_agent_negotiations()

    def handle_free_agent_negotiations(self):
        """
        Handles free agent negotiations using ContractEngine for offer generation and evaluation.
        """
        contract_engine = ContractEngine()
        for team in self.league_manager.teams:
            available_cap = getattr(team, "salary_cap", 200_000_000) - getattr(team, "payroll", 0)
            free_agents = self.league_manager.get_available_free_agents()
            for player in free_agents:
                # Generate an offer using ContractEngine
                offer = contract_engine.calculate_offer(team, player, available_cap)
                if offer is None:
                    continue  # Skip if no valid offer can be made
                # Evaluate if the offer is acceptable to the player
                if contract_engine.is_offer_acceptable(player, offer, market_value=contract_engine.get_market_value(player)):
                    # Sign the player
                    player.contract = offer
                    player.team = team
                    team.roster.append(player)
                    team.payroll += offer.get("salary", 0)
                    available_cap -= offer.get("salary", 0)
                    self.league_manager.remove_free_agent(player)
            # Optionally update team finances after signings
            team.payroll = sum(getattr(p, "contract", {}).get("salary", 0) for p in team.roster)

    def _apply_training_camp_effects(self):
        """
        Training Camp: Intense practice, larger rating and chemistry boost, depth chart battles.
        Adds random variation to simulate gains and rare declines.
        """
        for team in self.league_manager.teams:
            for player in team.roster:
                # Random variation: most improve, some stagnate, rare decline
                change = random.choices(
                    [random.uniform(1.0, 2.0), random.uniform(0.0, 1.0), random.uniform(-1.0, 0.0)],
                    weights=[0.7, 0.25, 0.05]
                )[0]
                player.rating = max(40, min(player.rating + change, 99))
                # Chemistry boost
                player.chemistry = min(getattr(player, "chemistry", 0.5) + random.uniform(0.01, 0.04), 1.0)
            # Placeholder: In a real UI, resolve depth chart battles and position competitions.
        # print("[Training Camp] Players improved, chemistry increased, depth chart battles resolved.")

    # --- UI Integration Placeholders ---

    def on_phase_start(self, phase_name: str):
        """
        Called when a phase begins. Extend for custom logic or UI triggers.
        """
        # Example: print(f"Phase '{phase_name}' has started.")
        pass

    def on_phase_end(self, phase_name: str):
        """
        Called when a phase ends. Extend for custom logic or UI triggers.
        """
        # Example: print(f"Phase '{phase_name}' has ended.")
        pass

    def on_offseason_end(self):
        """
        Called when the offseason ends and preseason begins.
        """
        # Example: print("Offseason complete. Preseason begins!")
        pass

    def get_active_phases(self) -> List[str]:
        """
        Returns a list of currently active phases.
        """
        return self.active_phases

    def get_current_day(self) -> int:
        """
        Returns the current offseason day (relative to offseason start).
        """
        if self.current_day is None or self.offseason_start_date is None:
            return 0
        return (self.current_day - self.offseason_start_date).days + 1

    def get_phase_schedule(self) -> List[Dict]:
        """
        Returns the full schedule of offseason phases with their start and end days.
        """
        return self.phase_schedule

    def is_phase_active(self, phase_name: str) -> bool:
        """
        Returns True if the given phase is currently active.
        """
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        return phase_name in self.active_phases