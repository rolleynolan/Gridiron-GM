from gridiron_gm.engine.core.player import Player, Injury

def rebuild_rookie_class(saved_rookies):
    rebuilt = []

    for data in saved_rookies:
        if isinstance(data, Player):
            rebuilt.append(data)
        else:
            player = Player(
                name=data["name"],
                position=data["position"],
                age=data.get("age", 22),
                dob=parse_date(data.get("dob", "2000-01-01")),
                college=data.get("college", "Unknown"),
                birth_location=data.get("birth_location", "Unknown"),
                jersey_number=data.get("jersey_number", 0),
                overall=data.get("overall", 60)
            )
            player.contract = data.get("contract", None)
            player.experience = data.get("experience", 0)
            player.injuries = load_player_injuries(data)  # Convert injuries data into Injury objects
            player.retired = data.get("retired", False)
            rebuilt.append(player)

    return rebuilt

def parse_date(date_string):
    from datetime import datetime
    return datetime.strptime(date_string, "%Y-%m-%d").date()

def load_player_injuries(player_data):
    injuries = []
    for injury_data in player_data.get("injuries", []):
        injury = Injury(injury_data['injury_type'], injury_data['weeks_out'])
        injuries.append(injury)
    return injuries
