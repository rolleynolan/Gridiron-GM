import random
import datetime
from gridiron_gm.engine.free_agency.free_agent_profile import FreeAgentProfile
from gridiron_gm.health.injury_manager import InjuryEngine
from gridiron_gm.health.injury_manager import Injury
    
def __repr__(self):
        return f"{self.name} - {self.severity} for {self.weeks_out} weeks"

class Player:
    def __init__(self, name, position, age, dob, college, birth_location, jersey_number, overall):
        # Basic Info
        self.name = name
        self.position = position
        self.age = age
        self.dob = dob
        self.college = college
        self.birth_location = birth_location
        self.jersey_number = jersey_number
        self.overall = overall
        self.fatigue = 0
        self.free_agent_profile = FreeAgentProfile(self)

        # Skills & Development
        self.skills = {}
        self.dev_tier = None

        # Traits system
        self.traits = {
            "training": [],
            "mental": [],
            "gameday": [],
            "media": [],
        }

        # Scouting
        self.scouted = False
        self.scouted_skills = {}
        self.scouting_progress = 0
        self.projected_overall = None
        self.projected_potential = None
        self.notes = []

        # Contract and Career
        self.contract = None
        self.experience = 0
        self.injuries = []  # List of current injuries (Injury objects)
        self.injury_history = []  # New attribute for tracking past injuries
        self.weeks_out = 0
        self.retired_due_to_injury = False
        self.retired = False
        self.morale = 100
        self.playtime_history = []
        self.career_stats = {
            "passing_yards": 0,
            "rushing_yards": 0,
            "receiving_yards": 0,
            "passing_touchdowns": 0,
            "rushing_touchdowns": 0,
            "receiving_touchdowns": 0,
            "sacks": 0,
            "interceptions": 0,
            "tackles": 0
        }
        self.on_injured_reserve = False
        self.is_injured = False
    
    def add_injury(self, injury):
        """ Adds an injury to both current injuries and injury history """
        self.injuries.append(injury)
        self.injury_history.append(injury)  # Save to injury history
        self.weeks_out = injury.weeks_out
        self.is_injured = True

    def recover_one_week(self):
        """ Simulate one week of recovery for injuries, factoring in traits """
        if self.weeks_out > 0:
            self.weeks_out -= 1
            if "Quick Recovery" in self.traits.get("physical", []):
                self.weeks_out = max(1, int(self.weeks_out * 0.90))  # 10% faster healing

            if self.weeks_out == 0:
                self.injuries.clear()
                self.is_injured = False

    def update_player_stats(self, stat_type, value):
        """ Updates career stats """
        if stat_type in self.career_stats:
            self.career_stats[stat_type] += value

    def update_performance_due_to_traits(self):
        """ Update performance based on player's traits """
        if "Clutch Performer" in self.traits["mental"]:
            self.overall += 2
        if "Lazy" in self.traits["training"]:
            self.overall -= 1

    def to_dict(self):
        """ Convert player object to a dictionary for saving """
        return {
            "name": self.name,
            "position": self.position,
            "age": self.age,
            "dob": self.dob.isoformat() if hasattr(self.dob, 'isoformat') else self.dob,
            "college": self.college,
            "birth_location": self.birth_location,
            "jersey_number": self.jersey_number,
            "overall": self.overall,
            "fatigue": self.fatigue,
            "skills": self.skills,
            "dev_tier": self.dev_tier,
            "traits": self.traits,
            "scouted": self.scouted,
            "scouted_skills": self.scouted_skills,
            "scouting_progress": self.scouting_progress,
            "projected_overall": self.projected_overall,
            "projected_potential": self.projected_potential,
            "notes": self.notes,
            "contract": self.contract,
            "experience": self.experience,
            "injuries": [{"injury_type": inj.injury_type, "weeks_out": inj.weeks_out} for inj in self.injuries],
            "weeks_out": self.weeks_out,
            "retired_due_to_injury": self.retired_due_to_injury,
            "retired": self.retired,
            "morale": self.morale,
            "playtime_history": self.playtime_history,
            "career_stats": self.career_stats,
            "on_injured_reserve": self.on_injured_reserve,
            "is_injured": self.is_injured
        }

    @staticmethod
    def from_dict(data):
        """ Convert dictionary data back into a Player object """
        player = Player(
            name=data["name"],
            position=data["position"],
            age=data["age"],
            dob=datetime.datetime.fromisoformat(data["dob"]) if isinstance(data["dob"], str) else data["dob"],
            college=data["college"],
            birth_location=data["birth_location"],
            jersey_number=data["jersey_number"],
            overall=data["overall"]
        )
        player.fatigue = data.get("fatigue", 0)
        player.skills = data.get("skills", {})
        player.dev_tier = data.get("dev_tier", None)
        player.traits = data.get("traits", {"training": [], "mental": [], "gameday": [], "media": []})
        player.scouted = data.get("scouted", False)
        player.scouted_skills = data.get("scouted_skills", {})
        player.scouting_progress = data.get("scouting_progress", 0)
        player.projected_overall = data.get("projected_overall", None)
        player.projected_potential = data.get("projected_potential", None)
        player.notes = data.get("notes", [])
        player.contract = data.get("contract", None)
        player.experience = data.get("experience", 0)
        player.injuries = [Injury(inj['injury_type'], inj['weeks_out'], 'Unknown', 'Unknown') for inj in data.get("injuries", [])]
        player.weeks_out = data.get("weeks_out", 0)
        player.retired_due_to_injury = data.get("retired_due_to_injury", False)
        player.retired = data.get("retired", False)
        player.morale = data.get("morale", 100)
        player.playtime_history = data.get("playtime_history", [])
        player.career_stats = data.get("career_stats", {})
        player.on_injured_reserve = data.get("on_injured_reserve", False)
        player.is_injured = data.get("is_injured", False)
        player.free_agent_profile = FreeAgentProfile(player)
        return player
