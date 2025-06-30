<<<<<<< HEAD
def merge_player_stats(src, dest):
    """
    Merge player stats from src into dest, summing values for matching keys.
    """
    for name, stats in src.items():
        if name not in dest:
            dest[name] = stats.copy()
        else:
            for k, v in stats.items():
                if isinstance(v, (int, float)):
                    dest[name][k] = dest[name].get(k, 0) + v
                else:
                    dest[name][k] = v

def get_top_performers(stats, abbr):
    """
    Returns the top QB, RB, and WR for a team abbreviation.
    Only considers players whose Player object is on the specified team.
    """
    qbs = []
    rbs = []
    wrs = []
    for name, s in stats.items():
        player_obj = s.get("player_obj")
        team = getattr(player_obj, "team", None)
        team_abbr = getattr(team, "abbreviation", None)
        if team_abbr != abbr:
            continue  # Only include players from this team
        if s.get("pass_attempts", 0) > 0:
            qbs.append((name, s))
        if s.get("carries", 0) > 0:
            rbs.append((name, s))
        if s.get("receptions", 0) > 0:
            wrs.append((name, s))
    qbs.sort(key=lambda x: x[1].get("pass_yards", 0), reverse=True)
    rbs.sort(key=lambda x: x[1].get("rush_yards", 0), reverse=True)
    wrs.sort(key=lambda x: x[1].get("rec_yards", 0), reverse=True)
    return {
        "qb": qbs[0] if qbs else None,
        "rb": rbs[0] if rbs else None,
        "wr": wrs[0] if wrs else None,
    }
=======
def merge_player_stats(src, dest):
    """
    Merge player stats from src into dest, summing values for matching keys.
    """
    for name, stats in src.items():
        if name not in dest:
            dest[name] = stats.copy()
        else:
            for k, v in stats.items():
                if isinstance(v, (int, float)):
                    dest[name][k] = dest[name].get(k, 0) + v
                else:
                    dest[name][k] = v

def get_top_performers(stats, abbr):
    """
    Returns the top QB, RB, and WR for a team abbreviation.
    Only considers players whose Player object is on the specified team.
    """
    qbs = []
    rbs = []
    wrs = []
    for name, s in stats.items():
        player_obj = s.get("player_obj")
        team = getattr(player_obj, "team", None)
        team_abbr = getattr(team, "abbreviation", None)
        if team_abbr != abbr:
            continue  # Only include players from this team
        if s.get("pass_attempts", 0) > 0:
            qbs.append((name, s))
        if s.get("carries", 0) > 0:
            rbs.append((name, s))
        if s.get("receptions", 0) > 0:
            wrs.append((name, s))
    qbs.sort(key=lambda x: x[1].get("pass_yards", 0), reverse=True)
    rbs.sort(key=lambda x: x[1].get("rush_yards", 0), reverse=True)
    wrs.sort(key=lambda x: x[1].get("rec_yards", 0), reverse=True)
    return {
        "qb": qbs[0] if qbs else None,
        "rb": rbs[0] if rbs else None,
        "wr": wrs[0] if wrs else None,
    }
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
