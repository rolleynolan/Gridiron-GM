class FatigueSystem:
    """
    Handles player fatigue accumulation, recovery, and performance impact for the simulation engine.
    """

    min_fatigue = 0.0
    max_fatigue = 1.0

    # Typical snap % for each position (used for auto-sub logic)
    POSITION_SNAP_PCT = {
        "QB": 0.98,
        "LT": 0.98,
        "RT": 0.98,
        "RG": 0.98,
        "LG": 0.98,
        "C": 0.98,
        "WR": 0.85,
        "TE": 0.71,
        "RB": 0.56,
        "DL": 0.81,
        "DT": 0.72,
        "LB": 0.92,
        "DB": 0.93,
        # Add more as needed
    }

    # Fatigue accumulation per play by position (tunable)
    POSITION_FATIGUE_RATE = {
        "QB": 0.005,
        "LT": 0.007,
        "RT": 0.007,
        "RG": 0.007,
        "LG": 0.007,
        "C": 0.007,
        "WR": 0.012,
        "TE": 0.010,
        "RB": 0.018,
        "DL": 0.015,
        "DT": 0.014,
        "LB": 0.010,
        "DB": 0.011,
    }

    def add_fatigue(self, player, play_intensity: float = 1.0) -> None:
        """
        Increases a player's fatigue, scaled by position and play intensity.

        Args:
            player (dict): The player dictionary.
            play_intensity (float): Multiplier for high-effort plays (default 1.0).
        """
        # Support both dict and object
        if isinstance(player, dict):
            pos = player.get("position", "WR")
            stamina = player.get("stamina", 80)
            fatigue = player.get("fatigue", 0.0)
            rate = self.POSITION_FATIGUE_RATE.get(pos, 0.01)
            fatigue_gain = rate * play_intensity * (100 / stamina)
            player["fatigue"] = min(self.max_fatigue, fatigue + fatigue_gain)
        else:
            pos = getattr(player, "position", "WR")
            stamina = getattr(player, "stamina", 80)
            fatigue = getattr(player, "fatigue", 0.0)
            rate = self.POSITION_FATIGUE_RATE.get(pos, 0.01)
            fatigue_gain = rate * play_intensity * (100 / stamina)
            player.fatigue = min(self.max_fatigue, fatigue + fatigue_gain)

    def recover(self, player: dict, context: str = None, is_on_field: bool = False) -> None:
        """
        Reduces a player's fatigue, not going below min_fatigue.

        Args:
            player (dict): The player dictionary.
            context (str, optional): Context for recovery (e.g., 'between_plays', 'bye_week').
            is_on_field (bool, optional): Whether the player is currently on the field.
        """
        # More recovery off-field, even more on bye week
        if context == "bye_week":
            recovery_rate = 0.5
        elif context == "between_games":
            recovery_rate = 0.2
        elif is_on_field:
            recovery_rate = 0.03
        else:
            recovery_rate = 0.08
        player.fatigue = max(self.min_fatigue, player.get("fatigue", 0.0) - recovery_rate)

    def performance_modifier(self, player: dict) -> float:
        """
        Returns a multiplier for performance based on fatigue.
        At 0 fatigue: 1.0 (full performance)
        At 1.0 fatigue: 0.7 (30% drop, tunable)

        Args:
            player (dict): The player dictionary.

        Returns:
            float: Performance multiplier.
        """
        fatigue = getattr(player, "fatigue", 0.0)
        return max(0.7, 1.0 - 0.3 * fatigue)

    def injury_risk_modifier(self, player: dict) -> float:
        """
        Returns a multiplier for injury risk based on fatigue.
        At 0 fatigue: 1.0 (baseline risk)
        At 1.0 fatigue: 2.0 (double risk, tunable)

        Args:
            player (dict): The player dictionary.

        Returns:
            float: Injury risk multiplier.
        """
        fatigue = getattr(player, "fatigue", 0.0)
        return 1.0 + fatigue  # Linear increase, tune as needed

    def accumulate_season_fatigue(self, player: dict, heavy_usage: bool = False) -> None:
        """
        Adds cumulative fatigue over the season, especially for high-usage players.

        Args:
            player (dict): The player dictionary.
            heavy_usage (bool): If player had a heavy workload this week.
        """
        # Small weekly increase, more if heavy usage
        season_fatigue = getattr(player, "season_fatigue", 0.0)
        increment = 0.03 if heavy_usage else 0.01
        player.season_fatigue = min(1.0, season_fatigue + increment)
        # Optionally, reduce max stamina as season fatigue rises
        player.stamina = max(60, getattr(player, "stamina", 80) - int(getattr(player, "season_fatigue", 0) * 10))

    def reset_for_offseason(self, player: dict) -> None:
        """
        Resets all fatigue at the end of the season.
        """
        player.fatigue = 0.0
        player.season_fatigue = 0.0
        player.stamina = getattr(player, "base_stamina", 80)

def accumulate_season_fatigue_for_team(team, heavy_usage_players):
    for player in team.players:
        is_heavy = getattr(player, "name") in heavy_usage_players
        FatigueSystem().accumulate_season_fatigue(player, heavy_usage=is_heavy)

