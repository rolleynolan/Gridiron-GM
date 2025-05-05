class RookieContract:
    def __init__(self, years, total_value, guaranteed):
        self.years = years
        self.total_value = total_value  # in millions
        self.guaranteed = guaranteed    # in millions

    def __repr__(self):
        return f"{self.years} yrs, ${self.total_value}M total, ${self.guaranteed}M guaranteed"

class RookieContractEngine:
    def assign_contract(self, player, pick_number):
        """
        Assign contract based on draft position.
        Top picks get larger contracts.
        """
        if pick_number <= 10:
            years = 4
            total_value = round(34 - (pick_number * 0.8), 2)
            guaranteed = round(total_value * 0.75, 2)
        elif pick_number <= 32:
            years = 4
            total_value = round(20 - ((pick_number - 10) * 0.6), 2)
            guaranteed = round(total_value * 0.6, 2)
        elif pick_number <= 64:
            years = 3
            total_value = round(10 - ((pick_number - 32) * 0.2), 2)
            guaranteed = round(total_value * 0.5, 2)
        else:
            years = 3
            total_value = round(5 - ((pick_number - 64) * 0.05), 2)
            guaranteed = round(total_value * 0.4, 2)

        return RookieContract(years, total_value, guaranteed)
