import random

class Injury:
    def __init__(self, name, weeks_out, severity, injury_type):
        self.name = name
        self.weeks_out = weeks_out
        self.severity = severity
        self.injury_type = injury_type  # "soft_tissue", "bone", etc.

    def __repr__(self):
        return f"{self.name} - {self.severity} for {self.weeks_out} weeks"


class InjuryEngine:
    def __init__(self):
        # Injury table with frequency weighting
        self.injury_table = [
            # Common Injuries (higher chance of happening)
            {"name": "Hamstring Strain", "weeks": (1, 3), "severity": "Minor", "type": "soft_tissue", "weight": 30},
            {"name": "High Ankle Sprain", "weeks": (2, 5), "severity": "Moderate", "type": "soft_tissue", "weight": 20},
            {"name": "Groin Strain", "weeks": (1, 3), "severity": "Minor", "type": "soft_tissue", "weight": 20},
            
            # Moderate Injuries (less frequent but still common)
            {"name": "Shoulder Dislocation", "weeks": (3, 6), "severity": "Moderate", "type": "joint", "weight": 7},
            {"name": "Tendonitis", "weeks": (2, 8), "severity": "Moderate", "type": "soft_tissue", "weight": 5},
            
            # Severe Injuries (rarer but more impactful)
            {"name": "ACL Tear", "weeks": (20, 52), "severity": "Severe", "type": "soft_tissue", "weight": 5},
            {"name": "Achilles Tear", "weeks": (30, 52), "severity": "Severe", "type": "soft_tissue", "weight": 3},
            {"name": "Fractured Tibia", "weeks": (16, 40), "severity": "Severe", "type": "bone", "weight": 3},
            {"name": "Herniated Disc", "weeks": (20, 40), "severity": "Severe", "type": "spine", "weight": 2},
            {"name": "Broken Hand", "weeks": (4, 8), "severity": "Moderate", "type": "bone", "weight": 5},
            {"name": "Fractured Fibula", "weeks": (8, 16), "severity": "Severe", "type": "bone", "weight": 5},
            {"name": "Pectoral Tear", "weeks": (16, 24), "severity": "Severe", "type": "soft_tissue", "weight": 5},
            
            # Less common injuries
            {"name": "Torn Labrum", "weeks": (4, 8), "severity": "Moderate", "type": "joint", "weight": 3},
            {"name": "Stress Fracture (Foot)", "weeks": (6, 10), "severity": "Moderate", "type": "bone", "weight": 5},
            {"name": "Groin Tear", "weeks": (12, 20), "severity": "Severe", "type": "soft_tissue", "weight": 2},
            {"name": "Lisfranc Injury", "weeks": (20, 40), "severity": "Severe", "type": "joint", "weight": 2},
            {"name": "Broken Collarbone", "weeks": (6, 10), "severity": "Moderate", "type": "bone", "weight": 4},
        ]
    
    def check_for_injury(self, player, num_plays, context="game"):
        """
        Randomly determine if a player gets injured based on the number of plays they participate in.
        - context: 'game' or 'practice'
        - num_plays: the number of plays the player participates in
        """
        base_chance = 0.04 if context == "game" else 0.01  # 4% for game, 1% for practice
        per_play_chance = base_chance / num_plays  # Divide by number of plays

        # Trait Modifiers to injury chance
        if "Glass Cannon" in player.traits.get("physical", []):
            per_play_chance += 0.10 / num_plays  # Adding 10% chance of injury risk
        if "Iron Man" in player.traits.get("physical", []):
            per_play_chance -= 0.10 / num_plays  # Reducing injury chance

        if per_play_chance < 0.001:
            per_play_chance = 0.001  # Minimum 0.1% chance per play

        # Cumulative injury risk over plays
        cumulative_injury_chance = 0.0
        for play in range(num_plays):
            cumulative_injury_chance += per_play_chance
            if random.random() < cumulative_injury_chance:
                # Injury occurs on this play
                injury = random.choice(self.injury_table)
                injury_name = injury["name"]
                injury_severity = injury["severity"]
                injury_type = injury["type"]
                weeks_out = random.randint(*injury["weeks"])

                # Adjust for Flexible trait (reduces soft tissue injuries)
                if injury_type == "soft_tissue" and "Flexible" in player.traits.get("physical", []):
                    if random.random() < 0.05:
                        print(f"{player.name} avoided a soft tissue injury due to 'Flexible' trait!")
                        return None  # Player avoids soft tissue injury entirely

                # Adjust for Fragile Bones trait (increases bone injuries)
                if injury_type == "bone" and "Fragile Bones" in player.traits.get("physical", []):
                    if random.random() < 0.10:
                        injury_name = "Fractured Tibia"
                        injury_severity = "Severe"
                        weeks_out = random.randint(16, 40)

                # Adjust for Quick Recovery trait (reduces recovery time)
                if "Quick Recovery" in player.traits.get("physical", []):
                    weeks_out = max(1, int(weeks_out * 0.90))  # 10% faster healing

                # Create a new Injury object and add to the player's injuries list
                new_injury = Injury(injury_name, weeks_out, injury_severity)
                player.add_injury(new_injury) # Ensure it's an Injury object, not a string
                player.weeks_out = weeks_out

                print(f"{player.name} suffered an injury: {new_injury}")
                player.is_injured = True  # Mark as injured
                return new_injury

        return None  # No injury occurred

    def recover_weekly(self, team):
        """
        Decrement injury timeouts each week.
        Auto-remove from IR once recovered.
        """
        all_players = team.roster + team.ir_list  # Including active players and IR
        for player in all_players:
            if player.weeks_out > 0:
                player.weeks_out -= 1

                # Update individual injuries
                still_injured = []
                for inj in player.injuries:
                    inj.weeks_out -= 1
                    if inj.weeks_out > 0:
                        still_injured.append(inj)
                player.injuries = still_injured

                # Recovery complete
                if player.weeks_out <= 0:
                    player.weeks_out = 0
                    player.injuries.clear()

                    if player.on_injured_reserve:
                        team.remove_player_from_ir(player)
                        print(f"{player.name} has recovered and been activated from IR!")
                    player.is_injured = False  # Mark as healed
