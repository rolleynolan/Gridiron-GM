def find_user_controlled_team(teams):
    """
    Finds and returns the team that is currently user-controlled.
    If no user-controlled team is found, defaults to the first team in the list.
    """
    for team in teams:
        if getattr(team, "user_controlled", False):
            return team

    print("Warning: No user-controlled team found. Defaulting to first team.")
    return teams[0]
