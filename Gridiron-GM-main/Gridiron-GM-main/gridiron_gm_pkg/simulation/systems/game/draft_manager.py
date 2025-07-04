from gridiron_gm_pkg.simulation.utils.college_player_generator import (
    generate_college_player,
)

import random


class DraftManager:
    """
    Handles the league draft process, including order, rounds, and pick transactions.
    """

    def __init__(self, league, transaction_manager):
        self.league = league
        self.transaction_manager = transaction_manager
        self.draft_history = (
            []
        )  # List of dicts: {"round": int, "pick": int, "team": team, "player": player}

    def generate_draft_class(self, num_players: int) -> list:
        """Generate a rookie draft class."""
        position_weights = {
            "QB": 0.05,
            "RB": 0.09,
            "WR": 0.17,
            "TE": 0.07,
            "OL": 0.14,
            "DL": 0.15,
            "LB": 0.1,
            "CB": 0.13,
            "S": 0.08,
            "K": 0.01,
            "P": 0.01,
        }

        positions = list(position_weights.keys())
        weights = list(position_weights.values())
        draft_year = getattr(self.league, "year", 0) + 1
        players = []

        for _ in range(num_players):
            pos = random.choices(positions, weights=weights, k=1)[0]
            college_year = random.choices([3, 4], weights=[0.3, 0.7])[0]
            player = generate_college_player(college_year, position=pos)
            player.is_draft_eligible = True
            player.draft_class_year = draft_year
            player.rookie = True
            players.append(player)

        return players

    def determine_draft_order(self):
        """
        Returns a list of teams sorted by previous season record (worst to best).
        Assumes league.standings is a dict keyed by team id with "W" and "L" keys.
        """
        teams = list(self.league.teams)
        # Sort by (wins, losses), lowest wins first, then highest losses
        teams.sort(
            key=lambda t: (
                self.league.standings.get(t.id, {}).get("W", 0),
                -self.league.standings.get(t.id, {}).get("L", 0),
            )
        )
        return teams

    def run_draft(self, rounds=7):
        """
        Runs a 7-round draft. Each team picks one player per round from league.draft_prospects.
        After each pick, calls transaction_manager.draft_pick(team, player).
        After the draft, moves undrafted prospects to free agency.
        Logs all picks in self.draft_history.
        """
        draft_order = self.determine_draft_order()
        prospects = list(self.league.draft_prospects)
        self.draft_history = []
        pick_number = 1
        drafted_players = set()

        def _need_bonus(team, position):
            count = sum(
                1
                for p in getattr(team, "roster", [])
                if getattr(p, "position", None) == position
            )
            if count == 0:
                return 10
            if count == 1:
                return 7
            if count == 2:
                return 4
            return 0

        for rnd in range(1, rounds + 1):
            for team in draft_order:
                if not prospects:
                    break  # No more prospects to draft

                board = getattr(team, "draft_board", [])
                available_entries = [
                    entry for entry in board if entry.get("player") in prospects
                ]
                if not available_entries:
                    available_entries = [
                        {"player": p, "score": getattr(p, "rating", 0)}
                        for p in prospects
                    ]

                def value(entry):
                    player = entry["player"]
                    scout_score = entry.get("score", 0)
                    bonus = _need_bonus(team, player.position)
                    return scout_score + bonus * 2

                best_entry = max(
                    available_entries,
                    key=lambda e: (value(e), e.get("score", 0)),
                )
                best_player = best_entry["player"]

                self.transaction_manager.draft_pick(team, best_player)

                # Determine base salary from draft slot (0-indexed)
                slot = pick_number - 1
                if slot < 5:
                    base_salary = 10_000_000
                elif slot < 10:
                    base_salary = 8_500_000
                elif slot < 20:
                    base_salary = 6_000_000
                elif slot < 32:
                    base_salary = 4_000_000
                elif slot < 64:
                    base_salary = 2_500_000
                elif slot < 100:
                    base_salary = 1_500_000
                else:
                    base_salary = 800_000

                rookie_contract = {
                    "years": 4,
                    "salary_per_year": base_salary,
                    "total_value": base_salary * 4,
                    "years_left": 4,
                    "expiring": False,
                    "year_signed": getattr(self.league, "year", 0),
                    "is_rookie_deal": True,
                }
                best_player.contract = rookie_contract
                if best_player not in team.roster:
                    team.roster.append(best_player)
                best_player.team = team
                self.draft_history.append(
                    {
                        "round": rnd,
                        "pick": pick_number,
                        "team": team,
                        "player": best_player,
                    }
                )
                prospects.remove(best_player)
                drafted_players.add(best_player)
                pick_number += 1

        # Move undrafted prospects to free agents
        for player in list(self.league.draft_prospects):
            if player not in drafted_players:
                self.transaction_manager.move_to_free_agents(player)

        # Update league.draft_prospects to only those drafted (optional)
        self.league.draft_prospects = [entry["player"] for entry in self.draft_history]

    def get_draft_history(self):
        """
        Returns the draft history for reporting or review.
        """
        return self.draft_history
