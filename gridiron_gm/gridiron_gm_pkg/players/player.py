import random
from typing import Any, Dict


def get_scouted_rating(true_rating: int, accuracy: float) -> int:
    """Return a biased rating based on scouting accuracy."""
    accuracy = max(0.0, min(1.0, accuracy))
    deviation = int((1.0 - accuracy) * 40)
    estimate = true_rating + random.randint(-deviation, deviation)
    return max(40, min(99, estimate))


def get_rookie_view(player: Any, team: Any) -> Dict[str, Any]:
    """Return a dict representation of a rookie adjusted for the team's scouting accuracy."""
    if hasattr(player, "to_dict"):
        data = player.to_dict()
    else:
        data = dict(player.__dict__)

    is_prospect = getattr(player, "experience", 0) == 0 and getattr(player, "drafted_by", None) is None
    if not is_prospect:
        return data

    accuracy = getattr(team, "scouting_accuracy", 1.0)
    data["overall"] = get_scouted_rating(getattr(player, "overall", 0), accuracy)
    if data.get("potential") is not None:
        data["potential"] = get_scouted_rating(getattr(player, "potential", 0), accuracy)

    return data
