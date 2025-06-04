import datetime
from uuid import uuid4
from dataclasses import dataclass, field
from typing import List, Dict, Optional

@dataclass
class DevArc:
    type: str
    current_progress: float
    milestones: List[str] = field(default_factory=list)

@dataclass
class AttributeSet:
    core: Dict[str, int] = field(default_factory=dict)
    position_specific: Dict[str, int] = field(default_factory=dict)

@dataclass
class Contract:
    years: int
    salary_per_year: int
    bonuses: Dict[str, int] = field(default_factory=dict)

class Player:
    def __init__(self, name, position, age, dob, college, birth_location, jersey_number, overall, potential=None):
        self.id = str(uuid4())
        self.name = name
        self.position = position
        self.age = age
        self.dob = dob
        self.college = college
        self.birth_location = birth_location
        self.jersey_number = jersey_number
        self.overall = overall
        self.potential = None

        self.attributes = AttributeSet()
        self.dev_arc = DevArc("standard", 0.0)
        self.contract = None
        self.morale = 100
        self.fatigue = 0.0
        self.stamina = 80
        self.snaps = 0

        self.current_team = None
        self.rookie_year = None
        self.drafted_by = None
        self.draft_round = None
        self.draft_pick = None

        self.injuries = []
        self.injury_history = []
        self.weeks_out = 0
        self.retired_due_to_injury = False
        self.retired = False
        self.on_injured_reserve = False
        self.is_injured = False

        self.traits = {
            "training": [],
            "gameday": [],
            "physical": [],
            "mental": [],
            "media": []
        }

        self.skills = {}
        self.experience = 0
        self.notes = []
        self.playtime_history = []

        self.career_stats = {}
        self.stats_by_year = {}

    def add_trait(self, category, trait):
        if category in self.traits:
            self.traits[category].append(trait)

    def get_fatigue_rate(self):
        # Base fatigue rate for all players
        base = 0.1
        
        # Increase fatigue rate for positions requiring high physical exertion
        if self.position in ["WR", "CB", "RB", "LB"]:
            base += 0.05
        
        # Adjust fatigue rate based on stamina (lower stamina increases fatigue)
        base *= (100 - self.stamina) / 100
        
    def fatigue_threshold(self):
        # Base threshold for fatigue varies by position:
        # - RB, WR, DL: These positions are more physically demanding, so the threshold is lower (0.6).
        # - QB, OL, K, P: These positions are less physically demanding, so the threshold is higher (0.9).
        # - Other positions default to 0.7.
        base = 0.7
        if self.position in ["RB", "WR", "DL"]:
            base = 0.6
        elif self.position in ["QB", "OL", "K", "P"]:
            base = 0.9
        
        # Adjust the base threshold slightly based on stamina.
        base += (self.stamina - 80) * 0.002
        return base
        return base

    def fatigue_threshold(self):
        base = 0.7
        if self.position in ["RB", "WR", "DL"]:
            base = 0.6
        elif self.position in ["QB", "OL", "K", "P"]:
            base = 0.9
        base += (self.stamina - 80) * 0.002
        return base

    def is_fatigued(self):
        return self.fatigue >= self.fatigue_threshold()

    def play_snap(self, intensity=1.0):
        self.snaps += 1
        self.fatigue += self.get_fatigue_rate() * intensity
        self.fatigue = min(self.fatigue, 1.0)
        if self.sub_cooldown > 0:
            self.sub_cooldown -= 1

    def add_injury(self, injury):
        self.injuries.append(injury)
        self.injury_history.append(injury)
        self.weeks_out = injury.weeks_out
        self.is_injured = True

    def recover_one_week(self):
        if self.weeks_out > 0:
            if "Quick Recovery" in self.traits.get("physical", []):
                self.weeks_out = max(1, int((self.weeks_out - 1) * 0.90))
            else:
                self.weeks_out -= 1
            if self.weeks_out == 0:
                self.injuries.clear()
                self.is_injured = False

    def update_player_stats(self, stat_type, value):
        if stat_type in self.career_stats:
            self.career_stats[stat_type] += value

    def update_performance_due_to_traits(self):
        if "Clutch Performer" in self.traits["mental"]:
            self.overall += 2
        if "Lazy" in self.traits["training"]:
            self.overall -= 1
        # Ensure overall remains within valid bounds
        self.overall = max(0, min(self.overall, 100))

    def to_dict(self):
        return {
            "id": self.id,
            "name": self.name,
            "position": self.position,
            "age": self.age,
            "dob": self.dob.isoformat() if hasattr(self.dob, 'isoformat') else self.dob,
            "college": self.college,
            "birth_location": self.birth_location,
            "jersey_number": self.jersey_number,
            "overall": self.overall,
            "potential": self.potential,
            "fatigue": self.fatigue,
            "skills": self.skills,
            "traits": self.traits,
            "notes": self.notes,
            "contract": self.contract,
            "experience": self.experience,
            "injuries": [i for i in self.injuries],
            "weeks_out": self.weeks_out,
            "retired_due_to_injury": self.retired_due_to_injury,
            "retired": self.retired,
            "morale": self.morale,
            "playtime_history": self.playtime_history,
            "career_stats": self.career_stats,
            "on_injured_reserve": self.on_injured_reserve,
            "is_injured": self.is_injured,
            "snaps": self.snaps,
            "rookie_year": self.rookie_year,
            "drafted_by": self.drafted_by,
            "draft_round": self.draft_round,
            "draft_pick": self.draft_pick
        }

    @staticmethod
    def from_dict(data):
        player = Player(
            name=data["name"],
            position=data["position"],
            age=data.get("age", 22),
            dob=datetime.datetime.fromisoformat(data["dob"]) if isinstance(data["dob"], str) else data["dob"],
            college=data["college"],
            birth_location=data["birth_location"],
            jersey_number=data["jersey_number"],
            overall=data["overall"],
            potential=data.get("potential")
        )
        player.fatigue = data.get("fatigue", 0)
        player.skills = data.get("skills", {})
        player.traits = data.get("traits", {"training": [], "mental": [], "gameday": [], "media": []})
        player.notes = data.get("notes", [])
        player.contract = data.get("contract", None)
        player.experience = data.get("experience", 0)
        player.injuries = data.get("injuries", [])
        player.weeks_out = data.get("weeks_out", 0)
        player.retired_due_to_injury = data.get("retired_due_to_injury", False)
        player.retired = data.get("retired", False)
        player.morale = data.get("morale", 100)
        player.playtime_history = data.get("playtime_history", [])
        player.career_stats = data.get("career_stats", {})
        player.on_injured_reserve = data.get("on_injured_reserve", False)
        player.is_injured = data.get("is_injured", False)
        return player

def ensure_player_objects(team):
    from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player  # adjust import as needed
    new_roster = []
    for p in team.roster:
        if isinstance(p, dict):
            new_roster.append(Player.from_dict(p))
        else:
            new_roster.append(p)
    team.roster = new_roster
