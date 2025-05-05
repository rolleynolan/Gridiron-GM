import random

class RookiePlayer:
    def __init__(self, name, position, college, height, weight, overall, potential, dev_tier="Mid", projected_pick=100):
        self.name = name
        self.position = position
        self.college = college
        self.height = height
        self.weight = weight
        self.true_overall = overall
        self.potential = potential
        self.dev_tier = dev_tier
        self.projected_pick = projected_pick
        self.experience = 0

        self.true_attributes = {
            "Speed": random.randint(50, 99),
            "Strength": random.randint(50, 99),
            "Agility": random.randint(50, 99),
            "Awareness": random.randint(50, 99),
            "Technique": random.randint(50, 99)
        }

        self.scouted_rating = {}
        self.scouted_skills = {}
        self.scout_reports = {}
        self.scouting_progress = 0
        self.scouted = False
        self.notes = []

    def __str__(self):
        return f"{self.name} ({self.position}) from {self.college}"

    def to_dict(self):
        return {
            "name": self.name,
            "position": self.position,
            "college": self.college,
            "height": self.height,
            "weight": self.weight,
            "true_overall": self.true_overall,
            "potential": self.potential,
            "dev_tier": self.dev_tier,
            "projected_pick": self.projected_pick,
            "experience": self.experience,
            "true_attributes": self.true_attributes,
            "scouted_rating": self.scouted_rating,
            "scouted_skills": self.scouted_skills,
            "scout_reports": self.scout_reports,
            "scouting_progress": self.scouting_progress,
            "scouted": self.scouted,
            "notes": self.notes
        }

    @classmethod
    def from_dict(cls, data):
        player = cls(
            name=data["name"],
            position=data["position"],
            college=data["college"],
            height=data["height"],
            weight=data["weight"],
            overall=data["true_overall"],
            potential=data["potential"],
            dev_tier=data.get("dev_tier", "Mid"),
            projected_pick=data.get("projected_pick", 100)
        )
        player.experience = data.get("experience", 0)
        player.true_attributes = data.get("true_attributes", {})
        player.scouted_rating = data.get("scouted_rating", {})
        player.scouted_skills = data.get("scouted_skills", {})
        player.scout_reports = data.get("scout_reports", {})
        player.scouting_progress = data.get("scouting_progress", 0)
        player.scouted = data.get("scouted", False)
        player.notes = data.get("notes", [])
        return player
