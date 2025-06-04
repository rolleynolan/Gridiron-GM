from gridiron_gm.gridiron_gm_pkg.engine.core.player import Player

def build_player_profile(player, team_name="Unknown Team"):
    """Extracts clean display-ready profile data for a given player."""
    if player.retired_due_to_injury:
        recovery_time = "Career Over"
    elif player.weeks_out > 0:
        recovery_time = f"{player.weeks_out} weeks remaining"
    else:
        recovery_time = "Fully Recovered"

    profile = {
        "Name": player.name,
        "Age": player.age,
        "Position": player.position,
        "Team": team_name,
        "Jersey Number": player.jersey_number,
        "Overall Rating": player.overall,
        "Scouted Potential": player.get_estimated_potential(),
        "Morale": f"{player.morale}%",
        "Traits": player.traits,
        "Injury Status": "Healthy" if not player.injuries else f"Injured: {', '.join(player.injuries)}",
        "Recovery Time": recovery_time,
        "Contract": format_contract_info(player.contract),
    }
    return profile

def format_contract_info(contract):
    """Formats contract details cleanly for display."""
    if not contract:
        return "Free Agent"
    years = contract.get("years", "N/A")
    salary = contract.get("salary", "N/A")
    fa_year = contract.get("free_agency_year", "N/A")
    return f"{years} yrs, ${salary}M, FA Year: {fa_year}"
