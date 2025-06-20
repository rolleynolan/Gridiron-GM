import random
from typing import List, Tuple
from .contract_offer import ContractOffer

class FreeAgentProfile:
    """Tracks free agent negotiation state."""
    def __init__(self, player):
        self.player = player
        self.days_waiting = 0
        self.patience_limit = self.determine_patience()
        self.offers_received: List[Tuple[object, ContractOffer]] = []

    def determine_patience(self) -> int:
        return random.randint(2, 5)

    def receive_offer(self, team, offer: ContractOffer) -> None:
        self.offers_received.append((team, offer))

    def daily_tick(self):
        """Advance one day and decide if an offer is accepted."""
        self.days_waiting += 1
        if self.offers_received and self.days_waiting >= self.patience_limit:
            team, offer = max(self.offers_received, key=lambda x: x[1].salary_per_year)
            self.offers_received.clear()
            self.days_waiting = 0
            return team, offer
        return None
