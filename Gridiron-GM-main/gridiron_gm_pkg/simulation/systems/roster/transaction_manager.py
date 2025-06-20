class TransactionManager:
    """
    Centralizes all roster moves for teams and the league.
    Handles draft picks, free agent signings, releases, and pool integrity.
    """

    def __init__(self, league_manager):
        self.league = league_manager

    def draft_pick(self, team, player):
        """
        Assigns a draft prospect to a team, removes from draft pool, and assigns rookie contract.
        """
        # Remove from draft prospects if present
        if player in self.league.draft_prospects:
            self.league.draft_prospects.remove(player)
        # Remove from free agents if present (shouldn't be, but for safety)
        if player in self.league.free_agents:
            self.league.free_agents.remove(player)
        # Add to team roster
        if player not in team.roster:
            team.roster.append(player)
        player.team = team
        # Assign rookie contract if applicable
        if hasattr(player, "assign_rookie_contract"):
            player.assign_rookie_contract(team)
        else:
            # Fallback: set a basic rookie contract
            player.contract = {
                "type": "rookie",
                "years": 4,
                "salary": 1_000_000,
                "years_left": 4,
                "expiring": False,
            }

    def sign_free_agent(self, team, player):
        """Signs a free agent to a team and removes from free agent pool."""
        if player in self.league.free_agents:
            self.league.free_agents.remove(player)
        if player not in team.roster:
            team.roster.append(player)
        player.team = team

    def release_player(self, team, player):
        """Releases a player from a team and moves to free agent pool."""
        if player in team.roster:
            team.roster.remove(player)
        self.move_to_free_agents(player)

    def move_to_free_agents(self, player):
        """
        Removes player from draft prospects (if present), adds to free agent pool, and clears team.
        """
        if player in self.league.draft_prospects:
            self.league.draft_prospects.remove(player)
        if player not in self.league.free_agents:
            self.league.free_agents.append(player)
        player.team = None

    def move_to_draft_prospects(self, player):
        """Adds a player to the draft prospects pool if not already present."""
        if player not in self.league.draft_prospects:
            self.league.draft_prospects.append(player)
        player.team = None

    # In the future: add transaction logging for auditing or undo features.