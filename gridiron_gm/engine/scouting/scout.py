import random
import math

class Scout:
    def __init__(self, name, role, region=None):
        self.name = name
        self.role = role
        self.region = region
        self.age = random.randint(30, 65)
        self.accuracy = random.randint(40, 100)
        self.speed = random.randint(5, 20)
        self.position_skills = self.generate_position_skills()
        self.personality = random.choice(["Risk Averse", "Upside Seeker", "Old School", "Analytics Oriented"])
        self.bias_region = random.choice([None, self.region])
        self.auto_scouting_enabled = False if role != 'Head' else True
        self.active_assignments = []
        self.assignments = []  # ✅ Add this line
        self.region_focus = False
        self.task_type = "assigned_players"
        self.task_param = None
        self.history = []


    def generate_position_skills(self):
        positions = ["QB", "HB/FB", "WR", "TE", "OL", "DL", "EDGE", "LB", "CB/S", "K/P"]
        return {pos: random.randint(40, 100) for pos in positions}

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

    def assign_task(self):
        print(f"\nAssign Task for {self.name}:")
        print("1. Scout Region")
        print("2. Scout Specific Position")
        print("3. Scout Specific Draft Round")
        print("4. Scout Manually Assigned Players")
        choice = input("Choose task type (1-4): ").strip()
        if choice == "1":
            self.task_type = "region"
            self.task_param = None
        elif choice == "2":
            self.task_type = "position"
            self.task_param = input("Enter Position (e.g., QB, WR, OL): ").strip().upper()
        elif choice == "3":
            self.task_type = "round"
            self.task_param = int(input("Enter Draft Round (1-7): ").strip())
        else:
            self.task_type = "assigned_players"
            self.task_param = None

    def evaluate_player(self, player, team_name):
        if self.role == "College" and player.experience > 0:
            return None
        if self.role == "Pro" and player.experience == 0:
            return None

        base = player.true_overall
        pos_bonus = self.position_skills.get(player.position, 70)
        region_bonus = 5 if self.bias_region and self.bias_region == player.region else 0
        accuracy_influence = math.log(101 - self.accuracy + 1) * 5
        base_deviation = 1.25 if self.role == "College" else 1.0 if player.experience < 2 else 0.75
        deviation = int(accuracy_influence * base_deviation + (100 - pos_bonus) // 10)
        scouted = base + random.randint(-deviation, deviation) + region_bonus
        scouted = max(40, min(99, scouted))

        scouted_percent = min(100, max(10, self.speed * 5))
        projected_future = min(100, max(scouted + random.randint(0, 10), scouted))

        # Use true player attributes and simulate deviation
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
        completed = []
        if self.task_type == "region":
            return []
        elif self.task_type == "position":
            position_targets = [p for p in prospects if p.position == self.task_param and p.experience == 0]
            self.active_assignments = position_targets[:self.speed]
        elif self.task_type == "round":
            round_targets = [p for p in prospects if abs(p.projected_pick - self.task_param * 32) <= 16 and p.experience == 0]
            self.active_assignments = round_targets[:self.speed]
        for _ in range(self.speed):
            if self.active_assignments:
                player = self.active_assignments.pop(0)
                result = self.evaluate_player(player, team_name)
                if result is not None:
                    completed.append((player.name, result))
        return completed
    def build_structured_scouting_report(self, player):
        scouted_rating = player.overall
        scouted_percent = 75
        future_rating = player.potential
        skill_ratings = {
            "Speed": random.randint(60, 90),
            "Awareness": random.randint(55, 88),
            "Strength": random.randint(60, 85),
        }

        report_text = self.generate_scout_report(
            player, scouted_rating, scouted_percent, future_rating, skill_ratings
        )

        # Basic extraction (replace with real logic later)
        role = "Starter" if future_rating >= 75 else "Depth"
        curve_guess = player.dev_curve_projected or "normal"

        return {
            "name": player.name,
            "position": player.position,
            "overall_estimate": f"{scouted_rating - 2}–{scouted_rating + 2}",
            "dev_curve_guess": curve_guess,
            "role_projection": role,
            "commentary": report_text
        }
