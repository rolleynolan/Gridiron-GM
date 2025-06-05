import random
from typing import Any, Dict


def get_scouted_rating(true_rating: int, accuracy: float) -> int:
    """Return a biased rating based on scouting accuracy using Gaussian noise."""
    accuracy = max(0.0, min(1.0, accuracy))
    std_dev = max(1.0, (1.0 - accuracy) * 20)
    estimate = int(random.gauss(true_rating, std_dev))
    return max(0, min(99, estimate))


def get_rookie_view(player: Any, scout: Any | None) -> Dict[str, Any]:
    """Return a dict representation of a rookie adjusted for the scout's accuracy."""
    if hasattr(player, "to_dict"):
        data = player.to_dict()
    else:
        data = dict(player.__dict__)

    is_prospect = getattr(player, "experience", 0) == 0 and getattr(player, "drafted_by", None) is None
    if not is_prospect:
        return data

    accuracy = getattr(scout, "scouting_accuracy", 1.0) if scout is not None else 1.0
    for key in ("overall", "potential"):
        if key in data and data[key] is not None:
            data[key] = get_scouted_rating(data[key], accuracy)

    return data
