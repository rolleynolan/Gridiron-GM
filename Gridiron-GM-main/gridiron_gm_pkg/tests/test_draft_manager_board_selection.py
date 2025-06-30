import os
import sys

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.roster.transaction_manager import TransactionManager
from gridiron_gm_pkg.simulation.systems.game.draft_manager import DraftManager
from gridiron_gm_pkg.simulation.utils.calendar import Calendar


def test_cpu_team_uses_draft_board():
    league = LeagueManager()
    league.calendar = Calendar()

    team1 = Team("Alpha", "A", "A")
    team2 = Team("Beta", "B", "B")
    team1.roster = []
    team2.roster = []
    team1.players = team1.roster
    team2.players = team2.roster
    league.add_team(team1)
    league.add_team(team2)

    # existing QB on team1 so WR should be higher need
    existing_qb = Player(
        name="Vet QB",
        position="QB",
        age=28,
        dob="1997-01-01",
        college="Test",
        birth_location="Nowhere",
        jersey_number=1,
        overall=70,
    )
    team1.roster.append(existing_qb)

    qb_prospect = Player(
        name="QB Prospect",
        position="QB",
        age=21,
        dob="2004-01-01",
        college="Test",
        birth_location="Nowhere",
        jersey_number=10,
        overall=80,
    )
    wr_prospect = Player(
        name="WR Prospect",
        position="WR",
        age=21,
        dob="2004-01-01",
        college="Test",
        birth_location="Nowhere",
        jersey_number=11,
        overall=78,
    )
    league.draft_prospects = [qb_prospect, wr_prospect]

    team1.draft_board = [
        {"player": qb_prospect, "score": 60},
        {"player": wr_prospect, "score": 58},
    ]
    team2.draft_board = [
        {"player": qb_prospect, "score": 65},
        {"player": wr_prospect, "score": 55},
    ]

    tm = TransactionManager(league)
    dm = DraftManager(league, tm)
    dm.run_draft(rounds=1)

    assert wr_prospect in team1.roster
