import random

def should_declare_early(player):
    potential = getattr(player, "potential", 70)
    traits = getattr(player, "traits", [])
    stats = player.college_stats.get("Junior", {})

    base = max(0, (potential - 70) / 30)
    stat_score = 0
    if stats.get("games", 0) >= 8:
        stat_sum = sum(
            v for k, v in stats.items()
            if isinstance(v, (int, float)) and k not in ["games", "sacks", "interceptions", "fumbles", "drops", "tds_allowed"]
        )
        stat_score = min(stat_sum / 1500.0, 0.5)

    bonus = 0
    if "boom_bust" in traits:
        bonus += 0.1
    if "injury_prone" in traits:
        bonus += 0.1

    chance = min(base + stat_score + bonus, 0.9)
    return random.random() < chance

def generate_draft_class(college_db):
    draft_class = []
    for player in college_db:
        year = getattr(player, "year_in_college", 0)
        if year == 4:
            player.is_draft_eligible = True
            player.declared_early = False
            draft_class.append(player)
        elif year == 3:
            if should_declare_early(player):
                player.is_draft_eligible = True
                player.declared_early = True
                draft_class.append(player)
            else:
                player.is_draft_eligible = False
                player.declared_early = False
        else:
            player.is_draft_eligible = False
            player.declared_early = False
    return draft_class
