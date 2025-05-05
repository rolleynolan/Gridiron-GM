from gridiron_gm.engine.core.coach import Coach

def build_coach_profile(coach, role="Assistant Coach"):
    """Extracts clean display-ready profile data for a given coach."""
    profile = {
        "Name": coach.name,
        "Age": coach.age,
        "Date of Birth": coach.dob.strftime("%B %d, %Y") if hasattr(coach.dob, 'strftime') else coach.dob,
        "Birth Location": coach.birth_location,
        "Team": coach.team_name or "Free Agent",
        "Traits": coach.traits if coach.traits else ["None Listed"]
    }

    if role == "Head Coach":
        profile["Record"] = f"{coach.record['wins']}-{coach.record['losses']}-{coach.record['ties']}"

    return profile
