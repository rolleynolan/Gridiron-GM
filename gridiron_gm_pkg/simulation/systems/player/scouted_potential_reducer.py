"""Utilities for updating scouted potential for a player's attributes."""

from typing import Dict, Any


def reevaluate_scouted_potential(player: Any, current_attributes: Dict[str, int]) -> Dict[str, int]:
    """Re-evaluate scouted potential values for a player's attributes.

    This routine adjusts the *scouted* ceiling for each attribute based on the
    player's recent development. If an attribute has not improved for two or
    more seasons, scouts begin to doubt the upside and the scouted potential is
    reduced slightly. A strong season can partially restore some of that lost
    faith.

    Parameters
    ----------
    player : Any
        Player object which is expected to contain ``hidden_caps`` and
        ``scouted_potential`` dictionaries.
    current_attributes : dict
        Mapping of attribute names to the player's current ratings.

    Returns
    -------
    dict
        Updated scouted potential values for each attribute.
    """

    # Ensure required containers exist on the player
    for field in ("hidden_caps", "scouted_potential", "last_attribute_values", "no_growth_years"):
        if not hasattr(player, field):
            setattr(player, field, {})

    strong_season = getattr(player, "strong_season", False)

    updated = {}
    for attr, value in current_attributes.items():
        hidden_cap = getattr(player, "hidden_caps", {}).get(attr, value)
        scouted = getattr(player, "scouted_potential", {}).get(attr, hidden_cap)

        # Track development over time
        last_val = player.last_attribute_values.get(attr, value)
        if value > last_val:
            player.no_growth_years[attr] = 0
        else:
            player.no_growth_years[attr] = player.no_growth_years.get(attr, 0) + 1
        player.last_attribute_values[attr] = value

        # If stagnant for two or more seasons, reduce perceived potential
        if player.no_growth_years[attr] >= 2 and scouted > value:
            scouted -= 1

        # Never let scouted potential drop below current ability
        scouted = max(value, scouted)

        # Optionally regain some lost potential after a strong season
        if strong_season and scouted < hidden_cap:
            scouted = min(hidden_cap, scouted + 1)

        player.scouted_potential[attr] = scouted
        updated[attr] = scouted

    return updated
