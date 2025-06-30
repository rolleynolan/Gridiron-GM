import random
from typing import Dict, List, Tuple

from gridiron_gm_pkg.engine.free_agency.contract_offer import ContractOffer
from gridiron_gm_pkg.simulation.AI.cpu_free_agency import (
    evaluate_team_needs,
    estimate_player_value,
    generate_contract_offer,
)

class FreeAgencyManager:
    def __init__(self, game_world):
        self.game_world = game_world
        self.free_agents = game_world.get("free_agents", [])
        self.pending_offers: Dict[str, List[Tuple[object, Dict]]] = {}

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
        """Finalize signing a free agent to a team."""
        if player not in self.free_agents:
            print(f"{player.name} is no longer a free agent.")
            return False

        roster = getattr(team, "roster", getattr(team, "players", []))
        if len(roster) >= getattr(team, "MAX_ROSTER_SIZE", 53):
            print(f"{team.name} roster is full. Cannot sign {player.name}.")
            return False

        if not hasattr(team, "cap_used"):
            team.cap_used = 0
        if not hasattr(team, "SALARY_CAP"):
            team.SALARY_CAP = 200

        projected_cap = team.cap_used + contract_offer.salary_per_year
        if projected_cap > team.SALARY_CAP:
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
        roster.append(player)
        team.cap_used += contract_offer.salary_per_year
        self.remove_free_agent(player)

        print(f"{team.name} signed {player.name} for ${contract_offer.salary_per_year}M/year for {contract_offer.years} years.")

        self.cpu_withdraw_offers(team, player.position)

        return True

    def process_user_signing(self, team, player, offer):
        """Public wrapper used by the UI to sign a player immediately.

        Parameters
        ----------
        team : Team
            The user's team attempting to sign the player.
        player : Player
            The free agent being signed.
        offer : ContractOffer | dict
            Offer details. If a dict is provided, it must contain at least
            ``years`` and ``salary_per_year`` keys.
        """
        if isinstance(offer, dict):
            offer = ContractOffer(
                total_value=offer.get("salary_per_year", offer.get("total_value", 0)),
                years=offer.get("years", 1),
                rookie=offer.get("rookie", False),
            )

        return self.sign_player(team, player, offer)

    def generate_random_contract_offer(self, overall_rating):
        base_salary = 0.5 + ((overall_rating - 60) * 0.12)
        salary_per_year = max(0.5, round(base_salary, 2))
        years = random.randint(1, 3)
        return ContractOffer(
            total_value=salary_per_year,
            years=years
        )

    # ------------------------------------------------------------------
    def generate_cpu_offers(self) -> None:
        """Create contract offers from all CPU-controlled teams."""
        self.pending_offers.clear()
        for team in self.game_world["teams"]:
            if getattr(team, "user_controlled", False):
                continue

            needs = evaluate_team_needs(team)
            cap_left = getattr(team, "SALARY_CAP", 200) - getattr(team, "cap_used", 0)
            offers: List[Tuple[object, Dict]] = []

            roster = getattr(team, "roster", getattr(team, "players", []))
            for pos, weight in sorted(needs.items(), key=lambda x: x[1], reverse=True):
                if cap_left <= 0 or len(roster) >= getattr(team, "MAX_ROSTER_SIZE", 53):
                    break
                candidates = [p for p in self.free_agents if p.position == pos]
                if not candidates:
                    continue
                player = max(candidates, key=estimate_player_value)
                value_score = estimate_player_value(player)
                offer_dict = generate_contract_offer(player, team, value_score * weight)
                if offer_dict["salary_per_year"] > cap_left:
                    continue
                cap_left -= offer_dict["salary_per_year"]
                offers.append((player, offer_dict))
                player.free_agent_profile.receive_offer(team, ContractOffer(
                    total_value=offer_dict["salary_per_year"],
                    years=offer_dict["years"],
                ))

            if offers:
                self.pending_offers[team.id] = offers

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
        """Process a single day of free agency."""
        self.generate_cpu_offers()

        players_signed_today = []

        for player in list(self.free_agents):
            # gather all offers for this player from pending_offers
            player_offers = []
            for team_id, offers in self.pending_offers.items():
                for p, offer in offers:
                    if p == player:
                        team = next((t for t in self.game_world["teams"] if t.id == team_id), None)
                        if team:
                            player_offers.append((team, offer))

            # also include any user offers stored on the profile
            for team, offer_obj in player.free_agent_profile.offers_received:
                player_offers.append((team, {
                    "years": offer_obj.years,
                    "salary_per_year": offer_obj.salary_per_year,
                    "rookie": offer_obj.rookie,
                }))

            if not player_offers:
                continue

            # choose highest offer with slight randomness
            player_offers.sort(key=lambda x: x[1]["salary_per_year"], reverse=True)
            chosen_team, chosen_offer = random.choice(player_offers[:1])

            contract_obj = ContractOffer(
                total_value=chosen_offer["salary_per_year"],
                years=chosen_offer["years"],
                rookie=chosen_offer.get("rookie", False),
            )

            signed = self.sign_player(chosen_team, player, contract_obj)
            if signed:
                players_signed_today.append((player.name, chosen_team.name, contract_obj.salary_per_year))

                # remove signed player from other pending offers
                for t_id in list(self.pending_offers.keys()):
                    self.pending_offers[t_id] = [
                        (p, o) for (p, o) in self.pending_offers[t_id] if p != player
                    ]

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
