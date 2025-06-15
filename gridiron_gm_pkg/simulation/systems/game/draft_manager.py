from gridiron_gm_pkg.simulation.engine.contract_engine import ContractEngine

class DraftManager:
    """
    Handles the league draft process, including order, rounds, and pick transactions.
    """
    def __init__(self, league, transaction_manager):
        self.league = league
        self.transaction_manager = transaction_manager
        self.draft_history = []  # List of dicts: {"round": int, "pick": int, "team": team, "player": player}

    def determine_draft_order(self):
        """
        Returns a list of teams sorted by previous season record (worst to best).
        Assumes league.standings is a dict keyed by team id with "W" and "L" keys.
        """
        teams = list(self.league.teams)
        # Sort by (wins, losses), lowest wins first, then highest losses
        teams.sort(key=lambda t: (self.league.standings.get(t.id, {}).get("W", 0),
                                  -self.league.standings.get(t.id, {}).get("L", 0)))
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
        contract_engine = ContractEngine()

        drafted_players = set()

        for rnd in range(1, rounds + 1):
            for team in draft_order:
                if not prospects:
                    break  # No more prospects to draft
                # For now, pick the "best" available (e.g., highest rating)
                best_player = max(prospects, key=lambda p: getattr(p, "rating", 0))
                self.transaction_manager.draft_pick(team, best_player)
                # Assign rookie contract using ContractEngine
                rookie_contract = contract_engine.generate_rookie_contract(team, best_player, round=rnd, pick=pick_number)
                best_player.contract = rookie_contract
                self.draft_history.append({
                    "round": rnd,
                    "pick": pick_number,
                    "team": team,
                    "player": best_player
                })
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