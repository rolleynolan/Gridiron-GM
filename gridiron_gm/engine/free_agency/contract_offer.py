class ContractOffer:
    def __init__(self, total_value, years, rookie=False):
        self.salary_per_year = total_value  # ✅ standardize
        self.years = years
        self.rookie = rookie

    def to_dict(self):
        return {
            "salary_per_year": self.salary_per_year,
            "years": self.years,
            "rookie": self.rookie
        }

    @staticmethod
    def from_dict(data):
        return ContractOffer(
            total_value=data.get("salary_per_year", 0),
            years=data.get("years", 1),
            rookie=data.get("rookie", False)
        )
