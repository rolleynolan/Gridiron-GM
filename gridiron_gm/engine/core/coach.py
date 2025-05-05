import datetime

class Coach:
    def __init__(self, name, age, dob, birth_location, team_name=None, record=None, traits=None):
        self.name = name
        self.age = age
        self.dob = dob  # Should be a datetime.date object ideally
        self.birth_location = birth_location
        self.team_name = team_name  # String: which team they're coaching
        self.record = record or {"wins": 0, "losses": 0, "ties": 0}  # Only matters if Head Coach
        self.traits = traits or []  # List of coaching style traits

    def to_dict(self):
        return {
            "name": self.name,
            "age": self.age,
            "dob": self.dob.isoformat() if isinstance(self.dob, datetime.date) else self.dob,
            "birth_location": self.birth_location,
            "team_name": self.team_name,
            "record": self.record,
            "traits": self.traits
        }

    @staticmethod
    def from_dict(data):
        return Coach(
            name=data.get("name"),
            age=data.get("age"),
            dob=datetime.date.fromisoformat(data["dob"]) if isinstance(data.get("dob"), str) else data.get("dob"),
            birth_location=data.get("birth_location"),
            team_name=data.get("team_name"),
            record=data.get("record", {"wins": 0, "losses": 0, "ties": 0}),
            traits=data.get("traits", [])
        )

    def __repr__(self):
        return f"{self.name} (Age: {self.age}) | Team: {self.team_name} | Record: {self.record['wins']}-{self.record['losses']}-{self.record['ties']}"
