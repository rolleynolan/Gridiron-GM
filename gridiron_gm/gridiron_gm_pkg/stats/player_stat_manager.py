from __future__ import annotations

from typing import Any, Dict


def update_player_stats(player: Any, week: int, year: int | str, stat_dict: Dict[str, int | float], snap_counts: Dict[str, int] | None = None) -> None:
    """Update a player's seasonal statistics.

    Parameters
    ----------
    player:
        Player object or dictionary with ``season_stats`` attribute.
    week:
        Week number of the game being logged.
    year:
        Season year for which stats are recorded.
    stat_dict:
        Mapping of stat names to values for this game.
    snap_counts:
        Mapping of snap counts by phase (offense/defense/special).
    """
    if snap_counts is None:
        snap_counts = {}

    if not hasattr(player, "season_stats"):
        player.season_stats = {}

    year_key = str(year)
    season_data = player.season_stats.setdefault(year_key, {"season_totals": {}, "game_logs": {}})

    season_totals = season_data.setdefault("season_totals", {})

    # Aggregate stats into season_totals
    for stat, val in stat_dict.items():
        if isinstance(val, (int, float)):
            season_totals[stat] = season_totals.get(stat, 0) + val
        else:
            season_totals[stat] = val

    # Aggregate snap counts
    totals_snaps = season_totals.setdefault("snap_counts", {"offense": 0, "defense": 0, "special": 0})
    for phase, cnt in snap_counts.items():
        totals_snaps[phase] = totals_snaps.get(phase, 0) + cnt

    if hasattr(player, "snap_counts"):
        player.snap_counts = totals_snaps

    # Store game log for the week
    game_log = stat_dict.copy()
    if snap_counts:
        game_log["snaps"] = snap_counts.copy()
    season_data.setdefault("game_logs", {})[int(week)] = game_log
