import random
from gridiron_gm.gridiron_gm_pkg.config.injury_catalog import INJURY_CATALOG
from gridiron_gm.gridiron_gm_pkg.config.position_importance import POSITION_IMPORTANCE

class Injury:
    def __init__(self, name, weeks_out, severity, long_term=None, career_ending=False):
        self.name = name
        self.weeks_out = weeks_out
        self.severity = severity
        self.long_term = long_term or []
        self.career_ending = career_ending

    def __repr__(self):
        return f"{self.name} - {self.severity} for {self.weeks_out} weeks"

class InjuryEngine:
    def __init__(self):
        self.injury_catalog = INJURY_CATALOG

    def _apply_injury(self, player, injury_name, injury_data, weeks_out, message):
        """Helper to apply an injury object to the player and log it."""
        career_ending = injury_data.get("career_ending", False)
        if career_ending and "career_ending_chance" in injury_data:
            if random.random() > injury_data["career_ending_chance"]:
                career_ending = False

        new_injury = Injury(
            injury_name,
            weeks_out,
            injury_data["severity"],
            long_term=injury_data.get("long_term", []),
            career_ending=career_ending,
        )
        if hasattr(player, "add_injury"):
            player.add_injury(new_injury)
        else:
            if not hasattr(player, "injuries"):
                player.injuries = []
            player.injuries.append(new_injury)

        player.weeks_out = weeks_out
        player.is_injured = True

        self.apply_long_term_effects(player, injury_data.get("long_term", []))

        # Apply short-term attribute penalties
        if not hasattr(player, "active_injury_effects"):
            player.active_injury_effects = []
        attr_effects = []
        for eff in injury_data.get("long_term", []):
            if eff.get("type") == "attribute":
                attr_effects.append((eff["target"], eff["change"]))
        for eff in injury_data.get("short_term", []):
            if eff.get("type") == "attribute":
                attr_effects.append((eff["target"], eff["change"]))
        # if no attribute effects defined, give a small default penalty for minor injuries
        if not attr_effects and injury_data.get("severity", "").lower() == "minor":
            attr_effects.append(("agility", -1))
        for attr, change in attr_effects:
            pos = getattr(player, "position", "").upper()
            pos_key = pos
            if pos in {"LT", "LG", "C", "RG", "RT"}:
                pos_key = "OL"
            elif pos in {"DE", "EDGE", "DT"}:
                pos_key = "DL"
            elif pos in {"OLB", "MLB", "LB"}:
                pos_key = "LB"
            elif pos in {"FS", "SS", "S"}:
                pos_key = "S"
            weight = POSITION_IMPORTANCE.get(pos_key, {}).get(attr, 1.0)
            player.active_injury_effects.append({
                "attribute": attr,
                "change": change * weight,
                "injury": injury_name,
            })

        print(f"{getattr(player, 'name', 'Player')} {message}: {new_injury}")
        return new_injury

    def check_for_injury(self, player, num_plays, context="game"):
        base_chance = 0.04 if context == "game" else 0.01
        per_play_chance = base_chance / max(1, num_plays)

        # Fatigue and season fatigue risk modifiers (if present)
        fatigue = getattr(player, "fatigue", 0.0)
        season_fatigue = getattr(player, "season_fatigue", 0.0)
        per_play_chance *= (1 + fatigue + 0.5 * season_fatigue)

        # Trait Modifiers
        traits = getattr(player, "traits", {})
        if "Glass Cannon" in traits.get("physical", []):
            per_play_chance += 0.10 / max(1, num_plays)
        if "Iron Man" in traits.get("physical", []):
            per_play_chance -= 0.10 / max(1, num_plays)
        per_play_chance = max(per_play_chance, 0.001)

        cumulative_injury_chance = 0.0
        for play in range(num_plays):
            cumulative_injury_chance += per_play_chance
            if random.random() < cumulative_injury_chance:
                possible_injuries = [
                    (name, data) for name, data in self.injury_catalog.items()
                    if data.get("injury_context", "on_field") in (context, "either")
                ]
                injury_name, injury_data = random.choice(possible_injuries)
                weeks_out = random.randint(*injury_data["weeks"])

                # Traits: Flexible, Fragile Bones, Quick Recovery
                if "Flexible" in traits.get("physical", []) and "soft_tissue" in injury_name.lower():
                    if random.random() < 0.05:
                        print(f"{getattr(player, 'name', 'Player')} avoided a soft tissue injury due to 'Flexible' trait!")
                        return None
                if "Fragile Bones" in traits.get("physical", []) and "bone" in injury_name.lower():
                    if random.random() < 0.10:
                        injury_name = "Fractured Tibia"
                        injury_data = self.injury_catalog.get(injury_name, injury_data)
                        weeks_out = random.randint(*injury_data["weeks"])
                if "Quick Recovery" in traits.get("physical", []):
                    weeks_out = max(1, int(weeks_out * 0.90))

                return self._apply_injury(
                    player,
                    injury_name,
                    injury_data,
                    weeks_out,
                    "suffered an injury",
                )

        return None  # No injury occurred

    def recover_weekly(self, team):
        """
        Decrement injury timeouts each week.
        Auto-remove from IR once recovered.
        """
        all_players = team.roster + team.ir_list  # Including active players and IR
        for player in all_players:
            if getattr(player, "weeks_out", 0) > 0:
                player.weeks_out -= 1

                # Update individual injuries
                still_injured = []
                for inj in getattr(player, "injuries", []):
                    inj.weeks_out -= 1
                    if inj.weeks_out > 0:
                        still_injured.append(inj)
                player.injuries = still_injured

                # Recovery complete
                if player.weeks_out <= 0:
                    player.weeks_out = 0
                    player.injuries.clear()
                    # Remove temporary effects
                    for effect in getattr(player, "temporary_effects", []):
                        if effect["type"] == "attribute" and effect["duration"] != "permanent":
                            attr = effect["target"]
                            dict_type = effect.get("dict", "core")
                            attr_dict = player.attributes.core if dict_type == "core" else player.attributes.position_specific
                            # Restore original value if tracked
                            if hasattr(player, "original_attributes") and attr in player.original_attributes:
                                attr_dict[attr] = player.original_attributes[attr]
                    player.temporary_effects = []
                    # Clear active injury penalties
                    player.active_injury_effects = []
                    if getattr(player, "on_injured_reserve", False):
                        team.remove_player_from_ir(player)
                        print(f"{getattr(player, 'name', 'Player')} has recovered and been activated from IR!")
                    player.is_injured = False  # Mark as healed

    def apply_long_term_effects(self, player, long_term_effects):
        """
        Applies long-term effects from an injury to the player's attributes or risk profile.
        """
        # Ensure tracking containers exist
        if not hasattr(player, "original_attributes"):
            player.original_attributes = {}
        if not hasattr(player, "temporary_effects"):
            player.temporary_effects = []

        for effect in long_term_effects:
            if effect["type"] == "attribute":
                attr = effect["target"]
                change = effect["change"]
                duration = effect["duration"]

                # Determine if this is a core or position-specific attribute
                if attr in player.attributes.core:
                    attr_dict = player.attributes.core
                elif attr in player.attributes.position_specific:
                    attr_dict = player.attributes.position_specific
                else:
                    # Default to core if not found (or create new)
                    attr_dict = player.attributes.core

                # Store original value if not already tracked
                if attr not in player.original_attributes:
                    player.original_attributes[attr] = attr_dict.get(attr, 0)
                attr_dict[attr] = attr_dict.get(attr, 0) + change

                # Track effect for later removal if not permanent
                if duration != "permanent":
                    player.temporary_effects.append({
                        "type": "attribute",
                        "target": attr,
                        "change": change,
                        "duration": duration,
                        "dict": "core" if attr_dict is player.attributes.core else "position_specific"
                    })

            elif effect["type"] == "recurrence":
                rec_key = f"recurrence_{effect['target']}"
                if not hasattr(player, rec_key):
                    setattr(player, rec_key, 0)
                setattr(player, rec_key, getattr(player, rec_key) + effect["change"])

    def assign_injury(self, player, injury_name=None):
        """
        Assigns a specific or random injury from the catalog to the player,
        applies long-term effects, and marks the player as injured.
        """
        # Select injury
        if injury_name is None:
            injury_name, injury_data = random.choice(list(self.injury_catalog.items()))
        else:
            injury_data = self.injury_catalog[injury_name]

        weeks_out = random.randint(*injury_data["weeks"])
        return self._apply_injury(
            player,
            injury_name,
            injury_data,
            weeks_out,
            "assigned injury",
        )

class InjurySystem:
    """
    Handles injury risk and assignment for players during simulation.
    """

    BASE_INJURY_CHANCE = 0.00025  # 0.025% per play, tune as needed

    def check_for_injury(self, player: dict) -> bool:
        """
        Determines if a player is injured on a play, factoring in fatigue.

        Args:
            player (dict): The player dictionary.

        Returns:
            bool: True if injured, False otherwise.
        """
        fatigue_risk = player.get("fatigue", 0.0)
        season_risk = player.get("season_fatigue", 0.0)
        risk = self.BASE_INJURY_CHANCE * (1 + fatigue_risk + 0.5 * season_risk)
        return random.random() < risk

    def assign_injury(self, player: dict) -> dict:
        """
        Assigns an injury to the player (stub for now).

        Args:
            player (dict): The player dictionary.

        Returns:
            dict: Injury details.
        """
        injury = {"type": "muscle strain", "weeks_out": random.randint(1, 4)}
        player["injury"] = injury
        return injury

# Instantiate a single InjuryEngine for module-level use
_injury_engine = InjuryEngine()

def create_injury(player, context="game", severity_roll=None):
    """
    Public function to assign an injury to a player using the shared InjuryEngine instance.
    Args:
        player: The player object to injure.
        context (str): The context for the injury (default "game").
        severity_roll: Optional custom severity roll for advanced logic.
    Returns:
        Injury: The Injury object created and assigned to the player.
    """
    # The assign_injury method does not use context or severity_roll directly,
    # but you can extend it to do so if needed.
    return _injury_engine.assign_injury(player)

def assign_random_injury(player):
    """
    Assigns a random injury to the player if not already injured, with realistic frequency and severity.
    Returns the Injury object if assigned, else None.
    """
    # 1. If already injured, 99.9% chance to skip injury
    if getattr(player, "is_injured", False) or getattr(player, "weeks_out", 0) > 0:
        if random.random() > 0.001:  # 0.1% chance of compounding injury
            return None

    # 2. Lowered injury frequency: 0.2% per play (realistic)
    injury_chance = 0.002
    if random.random() > injury_chance:
        return None

    # 3. Determine severity (tuned for realism)
    severity_roll = random.random()
    if severity_roll < 0.85:
        severity = "minor"
        weeks_range = (1, 2)
    elif severity_roll < 0.97:
        severity = "moderate"
        weeks_range = (3, 5)
    else:
        severity = "severe"
        weeks_range = (6, 12)

    # 4. Pick injury matching severity from catalog
    matching_injuries = [
        (name, data) for name, data in _injury_engine.injury_catalog.items()
        if data.get("severity") == severity
    ]
    if not matching_injuries:
        matching_injuries = list(_injury_engine.injury_catalog.items())  # Fallback

    injury_name, injury_data = random.choice(matching_injuries)

    # 5. Assign the injury using InjuryEngine
    return _injury_engine.assign_injury(player, injury_name=injury_name)

