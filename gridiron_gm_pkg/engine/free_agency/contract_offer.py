from dataclasses import dataclass

@dataclass
class ContractOffer:
    """Simple representation of a contract offer."""
    total_value: float
    years: int
    rookie: bool = False

    @property
    def salary_per_year(self) -> float:
        # Treat total_value as salary per year for now
        return self.total_value
