import random

class DraftManager:
    def __init__(self, teams, draft_class, rounds=3):
        self.teams = teams
        self.draft_class = draft_class
        self.rounds = rounds
        self.draft_order = self.determine_draft_order()

    def determine_draft_order(self):
        """
        Shuffle teams to simulate reverse standings or lottery.
        In the future, base on actual season performance.
        """
        order = self.teams[:]
        random.shuffle(order)
        return order

    def run_draft(self, contract_engine):
        pick_number = 1
        for rnd in range(self.rounds):
            print(f"\n-- Round {rnd + 1} --")
            for team in self.draft_order:
                if self.draft_class:
                    player = self.draft_class.pop(0)
                    contract = contract_engine.assign_contract(player, pick_number)
                    player.contract = contract
                    player.dev_tier = self.assign_dev_tier(pick_number)

                    if team.add_player(player):
                        print(f"{team.name} selects {player.name}, {player.position} ({player.college}) - "
                              f"OVR: {player.overall} | Contract: {contract}")
                    else:
                        print(f"{team.name} passed due to full roster or cap.")
                    pick_number += 1
                else:
                    print(f"{team.name} has no available player to draft.")

    def assign_dev_tier(self, pick_number):
        """
        Assign development tier based on pick position.
        Can later be affected by college experience, age, or randomness.
        """
        if pick_number <= 10:
            return "Blue Chip"
        elif pick_number <= 32:
            return "High"
        elif pick_number <= 64:
            return "Mid"
        else:
            return random.choice(["Mid", "Low"])
