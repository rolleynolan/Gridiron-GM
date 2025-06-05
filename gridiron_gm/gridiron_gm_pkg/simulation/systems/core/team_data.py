import json
import random
from gridiron_gm import VERBOSE_SIM_OUTPUT

try:
    from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
except ImportError:
    Team = None

def load_teams_from_json(json_path):
    import uuid
    with open(json_path, "r") as f:
        teams_data = json.load(f)
    teams = []
    for team_entry in teams_data:
        team_kwargs = dict(team_entry)
        # Use 'name' as 'team_name' if present
        if "name" in team_kwargs:
            team_kwargs["team_name"] = team_kwargs.pop("name")
        if "conference" not in team_kwargs:
            print(f"WARNING: Team '{team_kwargs.get('team_name', team_kwargs.get('abbreviation', 'UNK'))}' missing conference in data source. Defaulting to 'Nova'.")
            team_kwargs["conference"] = "Nova"
        for key in ["team_name", "city", "abbreviation", "conference"]:
            if key not in team_kwargs:
                raise ValueError(f"Missing required field '{key}' in team data: {team_kwargs}")
        # Assign or generate ID
        team_id = team_kwargs.get("id")
        if not team_id:
            team_id = str(uuid.uuid4())
            team_kwargs["id"] = team_id
        team = Team(
            id=team_id,
            team_name=team_kwargs["team_name"],
            city=team_kwargs["city"],
            abbreviation=team_kwargs["abbreviation"],
            conference=team_kwargs["conference"],
            division=team_kwargs.get("division", "Unknown"),
        )
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] After conversion: {team.abbreviation} conference={team.conference}")
        teams.append(team)
    if VERBOSE_SIM_OUTPUT:
        print("Teams loaded from JSON and their conferences:")
        for team in teams:
            abbr = getattr(team, "abbreviation", None)
            conf = getattr(team, "conference", None)
            print(f"{team.id} ({abbr}) - Conference: {conf}")
    return teams

def fill_team_rosters_with_dummy_players(teams):
    import random

    try:
        from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
    except ImportError:
        class Player:
            def __init__(self, name, position, overall, age=22, dob="2000-01-01", college="Dummy U", birth_location="Nowhere, USA", jersey_number=0, discipline_rating=70):
                self.name = name
                self.position = position
                self.overall = overall
                self.age = age
                self.dob = dob
                self.college = college
                self.birth_location = birth_location
                self.jersey_number = jersey_number
                self.fatigue = 0.0
                self.injuries = []
                self.is_injured = False
                self.games_remaining = 0
                self.discipline_rating = discipline_rating

    min_positions = {
        "QB": 2, "RB": 2, "WR": 3, "TE": 2,
        "LT": 1, "LG": 1, "C": 1, "RG": 1, "RT": 1,
        "DE": 2, "DT": 2, "LB": 3, "CB": 3, "S": 2,
        "K": 1, "P": 1
    }
    all_positions = [
        "QB", "RB", "WR", "TE",
        "LT", "LG", "C", "RG", "RT",
        "DE", "DT", "LB", "CB", "S",
        "K", "P"
    ]
    roster_size = 53

    for team in teams:
        abbr = getattr(team, 'abbreviation', 'UNK')
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Starting roster fill for {abbr}")

        # Always start with an empty roster for deterministic fill
        if hasattr(team, "players"):
            team.players = []
        else:
            team.players = []
        # team.roster will be synced at the end

        # 1. Add minimum required players for each position
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Filling minimum required players for each position for {abbr}...")
        player_count = 0
        for pos, min_count in min_positions.items():
            for i in range(min_count):
                discipline_rating = 60 + (i % 21)
                player = Player(
                    name=f"{abbr} {pos}{i+1}",
                    position=pos,
                    overall=60 + player_count,
                    age=22 + (player_count % 10),
                    dob="2000-01-01",
                    college="Dummy U",
                    birth_location="Nowhere, USA",
                    jersey_number=(player_count + 1)
                )
                player.discipline_rating = discipline_rating
                team.add_player(player)  # Only use add_player
                player_count += 1
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Finished minimums for {abbr}. Current roster size: {len(team.players)}")

        # 2. Fill the rest of the roster with random positions
        if VERBOSE_SIM_OUTPUT:
            print(f"[DEBUG] Filling remaining roster spots for {abbr} with random positions...")
        fill_attempts = 0
        max_attempts = 1000  # Prevent infinite loop
        while len(team.players) < roster_size and fill_attempts < max_attempts:
            pos = random.choice(all_positions)
            idx = sum(1 for p in team.players if getattr(p, "position", None) == pos) + 1
            discipline_rating = 60 + (idx % 21)
            player = Player(
                name=f"{abbr} {pos}{idx}",
                position=pos,
                overall=60 + player_count,
                age=22 + (player_count % 10),
                dob="2000-01-01",
                college="Dummy U",
                birth_location="Nowhere, USA",
                jersey_number=(player_count + 1)
            )
            player.discipline_rating = discipline_rating
            team.add_player(player)  # Only use add_player
            player_count += 1
            fill_attempts += 1
            if len(team.players) % 5 == 0 or len(team.players) == roster_size:
                if VERBOSE_SIM_OUTPUT:
                    print(f"[DEBUG] {abbr} roster length: {len(team.players)}")
        if fill_attempts >= max_attempts:
            print(f"[ERROR] Roster fill for {abbr} hit max attempts! Current size: {len(team.players)}")

        # 3. Sync team.roster and team.players
        team.roster = team.players

        # 4. Build depth_chart for all required positions
        team.generate_depth_chart()

        # 5. Final debug output
        print(f"[ROSTER FILL] {abbr} roster filled with {len(team.roster)} players.")
        print(f"[DEPTH CHART] {abbr}: " + ", ".join(f"{pos}:{len(players)}" for pos, players in team.depth_chart.items()))