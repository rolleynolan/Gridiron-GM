
import random
import math

# Define detailed football positions
DETAILED_POSITIONS = [
    "QB", "RB", "WR", "TE",
    "LT", "LG", "C", "RG", "RT",
    "DE", "DT", "OLB", "MLB",
    "CB", "FS", "SS",
    "K", "P"
]

# Map clusters to group-level skills for generalization
POSITION_CLUSTERS = {
    "OL": ["LT", "LG", "C", "RG", "RT"],
    "DL": ["DE", "DT"],
    "LB": ["OLB", "MLB"],
    "DB": ["CB", "FS", "SS"],
    "K/P": ["K", "P"],
    "RB": ["RB"],
    "WR": ["WR"],
    "TE": ["TE"],
    "QB": ["QB"]
}

class Scout:
    def __init__(self, name, role, region=None, scouting_accuracy=None):
        self.name = name
        self.role = role
        self.region = region
        self.age = random.randint(30, 65)
        self.accuracy = random.randint(40, 100)
        self.scouting_accuracy = (
            scouting_accuracy if scouting_accuracy is not None else self.accuracy / 100.0
        )
        self.speed = random.randint(5, 20)
        self.position_skills = self.generate_position_skills()
        self.personality = random.choice(["Risk Averse", "Upside Seeker", "Old School", "Analytics Oriented"])
        self.bias_region = random.choice([None, self.region])
        self.auto_scouting_enabled = False if role != 'Head' else True
        self.active_assignments = []
        self.assignments = []
        self.region_focus = False
        self.task_type = "assigned_players"
        self.task_param = None
        self.history = []

    def generate_position_skills(self):
        # Skill by cluster
        return {cluster: random.randint(40, 100) for cluster in POSITION_CLUSTERS.keys()}

    def get_position_skill(self, position):
        # Translate detailed position into its cluster and return skill
        for cluster, detailed in POSITION_CLUSTERS.items():
            if position in detailed:
                return self.position_skills.get(cluster, 70)
        return 70  # default

    def evaluate_player(self, player, team_name):
        if self.role == "College" and getattr(player, "experience", 0) > 0:
            return None
        if self.role == "Pro" and getattr(player, "experience", 0) == 0:
            return None

        base = getattr(player, "true_overall", getattr(player, "overall", 60))
        pos_bonus = self.get_position_skill(player.position)
        region_bonus = 5 if self.bias_region and self.bias_region == getattr(player, "region", None) else 0

        accuracy_influence = math.log(101 - self.accuracy + 1) * 5
        base_deviation = 1.25 if self.role == "College" else 1.0 if getattr(player, "experience", 0) < 2 else 0.75
        deviation = int(accuracy_influence * base_deviation + (100 - pos_bonus) // 10)
        scouted = base + random.randint(-deviation, deviation) + region_bonus
        scouted = max(40, min(99, scouted))

        scouted_percent = min(100, max(10, self.speed * 5))
        projected_future = min(100, max(scouted + random.randint(0, 10), scouted))

        skill_ratings = {
            skill: max(40, min(100, rating + random.randint(-5, 5)))
            for skill, rating in player.true_attributes.items()
        }

        player.scouted_rating[team_name] = scouted
        player.scouted_skills[team_name] = skill_ratings

        report = self.generate_scout_report(player, scouted, scouted_percent, projected_future, skill_ratings)
        player.scout_reports[self.name] = report
        self.history.append((player.name, player.true_overall, scouted, report))
        return scouted
    
    def weekly_scout(self, team_name, prospects):
        """Evaluate a batch of players based on task type and return (name, rating) tuples."""
        results = []
        if self.task_type == "assigned_players":
            targets = self.task_param or []
        elif self.task_type == "position":
            targets = [p for p in prospects if p.position == self.task_param]
        elif self.task_type == "region":
            targets = [p for p in prospects if getattr(p, "region", None) == self.task_param]
        else:
            targets = prospects

        for player in targets[:self.speed]:  # Limit based on scout speed
            rating = self.evaluate_player(player, team_name)
            if rating is not None:
                results.append((player.name, rating))

        return results


    def generate_scout_report(self, player, scouted_rating, scouted_percent, future_rating, skill_ratings):
        traits = [
            "Elite physical tools", "Strong fundamentals", "Questionable decision making",
            "Raw but talented", "High football IQ", "Great motor", "Inconsistent technique",
            "Needs time to develop", "NFL ready", "Boom-or-bust prospect"
        ]
        skills_summary = "\n".join([f"- {skill}: {value}/100" for skill, value in skill_ratings.items()])
        summary = [
            f"Scouted {scouted_percent}%",
            f"Current Ability: {scouted_rating}/100",
            f"Projected Potential: {future_rating}/100",
            f"Evaluation: {random.choice(traits)}",
            f"- Positional fit appears appropriate for current scheme.",
            f"- Shows flashes of development in key situations.",
            f"- Expected role: {self.projected_role(future_rating)}",
            "Skill Ratings:",
            skills_summary
        ]
        return "\n" + "\n".join(summary)

    def projected_role(self, rating):
        if rating >= 90:
            return "Franchise Player"
        elif rating >= 80:
            return "Future Star"
        elif rating >= 70:
            return "Solid Starter"
        elif rating >= 60:
            return "Rotational Player"
        elif rating >= 50:
            return "Career Backup"
        else:
            return "Camp Body"
