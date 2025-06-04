from simulation.entities.player import Player

# Growth curve by age: multiplier for growth/regression
GROWTH_CURVE = {
    20: 1.2, 21: 1.2, 22: 1.1, 23: 1.1, 24: 1.0, 25: 0.9,
    26: 0.8, 27: 0.7, 28: 0.6, 29: 0.5,
    30: 0.0, 31: -0.2, 32: -0.3, 33: -0.5, 34: -0.7, 35: -1.0,
}
def get_growth_multiplier(age):
    return GROWTH_CURVE.get(age, -1.0 if age > 35 else 1.0)

def progress_player(player: Player, performance_stats: dict, context="season"):
    """
    Progress a player based on their growth curve, potential, and performance.
    """
    # --- Growth Curve
    age = getattr(player, "age", 25)
    growth_mult = get_growth_multiplier(age)
    
    # --- Potential Ceiling
    potential = getattr(player, "potential", 80)
    current_ovr = player.overall

    # --- Performance Impact
    td = performance_stats.get("touchdowns", 0)
    yards = performance_stats.get("yards", 0)
    stat_growth = 0
    if td >= 10:
        stat_growth += 1
    if yards >= 1000:
        stat_growth += 1

    # --- Actual Growth Calculation
    # If aging, negative multiplier; if young, positive. Stats and potential modulate.
    projected_growth = int(stat_growth * growth_mult)
    if current_ovr + projected_growth > potential:
        projected_growth = potential - current_ovr
    elif current_ovr + projected_growth < 40:
        projected_growth = 40 - current_ovr

    # --- Apply Growth
    player.overall = min(max(current_ovr + projected_growth, 40), 99)

    # Optionally add morale, trait, injury, or training influences here

    return player
