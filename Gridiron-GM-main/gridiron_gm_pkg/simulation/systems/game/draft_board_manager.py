"""Utilities for generating team-specific draft boards."""

from __future__ import annotations

from typing import List, Dict, Any

from gridiron_gm_pkg.simulation.entities.scout import Scout
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.entities.league import LeagueManager


def evaluate_team_need(team: Team, position: str) -> int:
    """Return a rough score for how badly a team needs a position."""
    position_players = [p for p in team.players if p.position == position]
    if not position_players:
        return 10
    if all(p.overall < 72 for p in position_players):
        return 7
    if all(p.overall < 78 for p in position_players):
        return 4
    return 0


class DraftBoardManager:
    """Generate draft boards ranking prospects for each team."""

    def __init__(self, league: LeagueManager) -> None:
        self.league = league
        self.draft_boards: Dict[str, List[Dict[str, Any]]] = {}

    # ------------------------------------------------------------------
    def _get_team_scout(self, team: Team) -> Scout:
        """Return the scout assigned to the team or a generic one."""
        scout = team.staff.get("scout") if hasattr(team, "staff") else None
        if not isinstance(scout, Scout):
            scout = Scout(
                name=f"{team.abbreviation} Scout",
                evaluation_skill=getattr(team, "scouting_accuracy", 0.6),
            )
        return scout

    # ------------------------------------------------------------------
    def generate_team_board(
        self, team: Team, prospects: List[Player], top_n: int = 150
    ) -> List[Dict[str, Any]]:
        """Generate and store a draft board for a single team."""
        scout = self._get_team_scout(team)
        board: List[Dict[str, Any]] = []

        for player in prospects:
            report = scout.evaluate_player(player)
            ovr_est = sum(report.get("ovr_range", [50, 50])) / 2
            pot_est = sum(report.get("pot_range", [50, 50])) / 2
            need_bonus = evaluate_team_need(team, player.position)
            score = ovr_est * 0.6 + pot_est * 0.4 + need_bonus
            board.append({"player": player, "score": round(score, 2), "report": report})

        board.sort(key=lambda x: x["score"], reverse=True)
        team.draft_board = board[:top_n]
        self.draft_boards[team.id] = team.draft_board
        return team.draft_board

    # ------------------------------------------------------------------
    def generate_all_boards(
        self, draft_prospects: List[Player] | None = None, teams: List[Team] | None = None
    ) -> Dict[str, List[Dict[str, Any]]]:
        """Generate draft boards for all teams in the league."""
        prospects = draft_prospects or list(self.league.draft_prospects)
        targets = teams or list(self.league.teams)
        for team in targets:
            self.generate_team_board(team, prospects)
        return self.draft_boards
