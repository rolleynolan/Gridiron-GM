def league_to_dict(league):
    team_dicts = []
    for team in league.teams:
        if hasattr(team, "to_dict"):
            t = team.to_dict()
            t["conference"] = getattr(team, "conference", t.get("conference", None))
            t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
            team_dicts.append(t)
        elif isinstance(team, dict):
            t = dict(team)
            t["conference"] = t.get("conference", None)
            t["team_name"] = t.get("team_name", None)
            team_dicts.append(t)
        else:
            t = dict(team.__dict__)
            t["conference"] = getattr(team, "conference", t.get("conference", None))
            t["team_name"] = getattr(team, "team_name", t.get("team_name", None))
            team_dicts.append(t)
    return {
        "teams": team_dicts,
        "free_agents": [player.to_dict() for player in league.free_agents],
        "draft_prospects": [player.to_dict() for player in league.draft_prospects],  # <-- Added line
        "calendar": league.calendar.serialize(),
        "standings": league.standings,
        "schedule": league.schedule
    }