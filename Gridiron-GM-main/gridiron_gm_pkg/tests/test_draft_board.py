import os
import sys
import pytest

sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))

from gridiron_gm_pkg.simulation.entities.league import LeagueManager
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.scout import Scout
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.systems.game.draft_board_manager import DraftBoardManager
from gridiron_gm_pkg.simulation.utils.calendar import Calendar


def test_generate_draft_boards_basic():
    league = LeagueManager()
    league.calendar = Calendar()
    team1 = Team("Alpha", "A", "A")
    team2 = Team("Beta", "B", "B")
    team1.staff["scout"] = Scout("Alpha Scout", evaluation_skill=0.7)
    team2.staff["scout"] = Scout("Beta Scout", evaluation_skill=0.7)
    league.add_team(team1)
    league.add_team(team2)

    prospects = []
    for i in range(20):
        p = Player(
            name=f"Prospect {i}",
            position="QB" if i % 3 == 0 else "WR",
            age=21,
            dob="2004-01-01",
            college="Test",
            birth_location="Testville",
            jersey_number=i + 1,
            overall=70,
        )
        p.potential = 85
        prospects.append(p)
    league.draft_prospects = prospects

    board_manager = DraftBoardManager(league)
    boards = board_manager.generate_all_boards()

    assert team1.id in boards
    assert team2.id in boards
    assert len(boards[team1.id]) <= 20
    assert len(boards[team2.id]) <= 20
    assert boards[team1.id][0]["score"] >= boards[team1.id][-1]["score"]
