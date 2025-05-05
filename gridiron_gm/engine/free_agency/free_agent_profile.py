import random

class FreeAgentProfile:
    def __init__(self, player):
        self.player = player
        self.offers_received = []
        self.days_waiting = 0
        self.patience_limit = self.determine_patience()

    def determine_patience(self):
        if self.player.overall >= 85:
            return random.randint(21, 35)  # top FA wait 3–5 weeks
        elif self.player.overall >= 75:
            return random.randint(14, 21)
        elif self.player.overall >= 65:
            return random.randint(7, 14)
        else:
            return random.randint(1, 5)

    def daily_tick(self):
        self.days_waiting += 1
        if self.days_waiting >= self.patience_limit:
            return self.evaluate_offers()
        return None

    def receive_offer(self, team, contract_offer):
        self.offers_received.append((team, contract_offer))

    def evaluate_offers(self):
        if not self.offers_received:
            return None

        # Prioritize offers (simple model: salary per year highest)
        best_offer = max(self.offers_received, key=lambda x: x[1].salary_per_year)
        return best_offer
