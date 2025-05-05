import sys
import random

# --- Inline Player Class ---
class Player:
    def __init__(self, name, position, age, overall, potential):
        self.name = name
        self.position = position
        self.age = age
        self.overall = overall
        self.potential = potential  # internal use only
        self.potential_role = None  # shown to user (e.g. "Future Starter")
        self.injury = None
        self.skills = self.generate_base_skills()
        self.generate_position_skills()
        self.generate_scout_evaluation()

    def generate_base_skills(self):
        return {
            "speed": random.randint(60, 90),
            "strength": random.randint(60, 90),
            "awareness": random.randint(60, 90)
        }

    def generate_position_skills(self):
        if self.position == "QB":
            self.skills.update({
                "throw_power": random.randint(60, 90),
                "accuracy": random.randint(60, 90)
            })
        elif self.position in ["RB", "WR"]:
            self.skills.update({
                "agility": random.randint(60, 90),
                "elusiveness": random.randint(60, 90),
                "catching": random.randint(60, 90) if self.position == "WR" else 0
            })
        elif self.position == "LB":
            self.skills.update({
                "tackling": random.randint(60, 90),
                "block_shedding": random.randint(60, 90),
                "coverage": random.randint(60, 90)
            })
        else:
            self.skills.update({
                "positioning": random.randint(60, 90),
                "discipline": random.randint(60, 90)
            })

    def generate_scout_evaluation(self, scout_skill=0.75):
        # Convert numeric potential to a label (true label based on potential)
        if self.potential >= 90:
            true_label = "Superstar"
        elif self.potential >= 85:
            true_label = "Franchise Player"
        elif self.potential >= 80:
            true_label = "Future Starter"
        elif self.potential >= 75:
            true_label = "Developmental"
        else:
            true_label = "Depth Player"

        # Simulate scout error based on scout_skill (lower skill = more error)
        if random.random() > scout_skill:
            possible_labels = ["Superstar", "Franchise Player", "Future Starter", "Developmental", "Depth Player"]
            self.potential_role = random.choice(possible_labels)
        else:
            self.potential_role = true_label

    def apply_injury(self, injury_name, severity):
        self.injury = {"name": injury_name, "severity": severity, "weeks_remaining": severity}

    def progress_one_week(self):
        for skill in self.skills:
            if self.age < 30 and self.overall < self.potential:
                if random.random() < 0.25:
                    self.skills[skill] += 1
            elif self.age >= 30:
                if random.random() < 0.25:
                    self.skills[skill] = max(40, self.skills[skill] - 1)
        self.overall = sum(self.skills.values()) // len(self.skills)

    def progress_offseason(self):
        for skill in self.skills:
            if self.age < 30 and self.overall < self.potential:
                self.skills[skill] += random.randint(1, 3)
            elif self.age >= 30:
                self.skills[skill] -= random.randint(0, 2)
        self.overall = sum(self.skills.values()) // len(self.skills)

    def __str__(self):
        status = "Injured" if self.injury else "Healthy"
        skills = ", ".join(f"{k.capitalize()}: {v}" for k, v in self.skills.items())
        return (
            f"{self.name} ({self.position}) - OVR: {self.overall} - "
            f"{self.potential_role} - {status}\n    Skills → {skills}"
        )
