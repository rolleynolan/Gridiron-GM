import random

class FreeAgencyManager:
    def __init__(self, game_world):
        self.game_world = game_world
        self.free_agents = game_world.get("free_agents", [])

    def add_free_agent(self, player):
        if player not in self.free_agents:
            self.free_agents.append(player)
            player.free_agent_profile.days_waiting = 0
            player.free_agent_profile.patience_limit = player.free_agent_profile.determine_patience()
            player.free_agent_profile.offers_received = []

    def remove_free_agent(self, player):
        if player in self.free_agents:
            self.free_agents.remove(player)

    def list_top_free_agents(self, position=None, limit=10):
        agents = self.free_agents
        if position:
            agents = [p for p in agents if p.position == position]
        return sorted(agents, key=lambda p: p.overall, reverse=True)[:limit]

    def sign_player(self, team, player, contract_offer):
        if player not in self.free_agents:
            print(f"{player.name} is no longer a free agent.")
            return False

        if len(team.roster) >= getattr(team, "MAX_ROSTER_SIZE", 53):
            print(f"{team.name} roster is full. Cannot sign {player.name}.")
            return False

        projected_cap = getattr(team, "cap_used", 0) + contract_offer.salary_per_year
        if projected_cap > getattr(team, "SALARY_CAP", 200):
            over_by = round(projected_cap - team.SALARY_CAP, 2)
            print(f"{team.name} cannot sign {player.name}; would exceed cap by ${over_by}M.")
            return False

        player.contract = {
            "years": contract_offer.years,
            "salary_per_year": contract_offer.salary_per_year,
            "rookie": contract_offer.rookie,
            "years_left": contract_offer.years,
            "expiring": False,
        }
        team.roster.append(player)
        team.cap_used += contract_offer.salary_per_year
        self.remove_free_agent(player)

        print(f"{team.name} signed {player.name} for ${contract_offer.salary_per_year}M/year for {contract_offer.years} years.")

        self.cpu_withdraw_offers(team, player.position)

        return True

    def generate_random_contract_offer(self, overall_rating):
        base_salary = 0.5 + ((overall_rating - 60) * 0.12)
        salary_per_year = max(0.5, round(base_salary, 2))
        years = random.randint(1, 3)
        return ContractOffer(
            total_value=salary_per_year,
            years=years
        )

    def cpu_submit_offers(self):
        for team in self.game_world["teams"]:
            if getattr(team, "user_controlled", False):
                continue  # ✅ Skip user-controlled teams completely

            num_offers = random.randint(1, 2)
            available_agents = self.list_top_free_agents(limit=num_offers)

            for agent in available_agents:
                # Prevent duplicate offers
                already_offered = any(t == team for t, _ in agent.free_agent_profile.offers_received)

                if not already_offered:
                    offer = self.generate_random_contract_offer(agent.overall)
                    agent.free_agent_profile.receive_offer(team, offer)

    def advance_free_agency_day(self):
        # CPU teams submit new offers daily
        self.cpu_submit_offers()

        players_signed_today = []

        for player in list(self.free_agents):
            result = player.free_agent_profile.daily_tick()

            if result is not None:
                team, contract_offer = result

                signed = self.sign_player(team, player, contract_offer)
                if signed:
                    players_signed_today.append((player.name, team.name, contract_offer.salary_per_year))

        if players_signed_today:
            print("\n[Free Agency Signings Today]")
            for name, team_name, salary in players_signed_today:
                print(f"{name} signed with {team_name} for ${salary}M/year.")

    def cpu_withdraw_offers(self, team, signed_position):
        for player in self.free_agents:
            profile = player.free_agent_profile
            profile.offers_received = [
                (t, offer) for (t, offer) in profile.offers_received if t != team or player.position != signed_position
            ]
