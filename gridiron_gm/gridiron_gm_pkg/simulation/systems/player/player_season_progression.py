"""Seasonal player progression evaluation based on performance metrics."""

from __future__ import annotations

from typing import Any, Dict


def evaluate_player_season_progression(player: Any, season_stats: Dict[str, int], snap_counts: Dict[str, int]) -> Dict[str, float]:
    """Return attribute deltas after analyzing season performance.

    Parameters
    ----------
    player:
        Player object or dictionary with ``position``, ``attributes`` and ``hidden_caps``.
    season_stats:
        Dictionary of season totals (yards, touchdowns, sacks, etc.).
    snap_counts:
        Mapping of snap totals for the season. At minimum ``offense`` or ``defense``
        should be supplied depending on player type.

    Returns
    -------
    dict
        Mapping of attribute names to the delta that should be applied.
    """
    position = getattr(player, "position", "").upper()

    attrs = getattr(player, "attributes", {})
    if isinstance(attrs, dict):
        core = attrs.get("core", {})
        pos = attrs.get("position_specific", {})
    else:
        core = getattr(attrs, "core", {})
        pos = getattr(attrs, "position_specific", {})

    hidden_caps = getattr(player, "hidden_caps", {})

    def _current(attr: str) -> int:
        if attr in core:
            return int(core[attr])
        return int(pos.get(attr, core.get(attr, 50)))

    def _clamped_delta(attr: str, change: float) -> float:
        current = _current(attr)
        cap = hidden_caps.get(attr, 99)
        new_val = max(1, min(current + change, cap))
        return new_val - current

    deltas: Dict[str, float] = {}

    def _apply(attr: str, change: float) -> None:
        adj = _clamped_delta(attr, change)
        if adj != 0:
            deltas[attr] = deltas.get(attr, 0) + adj

    # --- Wide Receiver metrics
    if position == "WR":
        drops = season_stats.get("drops", 0)
        targets = season_stats.get("targets", 0)
        receptions = season_stats.get("receptions", 0)
        yac = season_stats.get("yards_after_catch", 0)

        drop_rate = drops / targets if targets else 0.0
        if drop_rate <= 0.05:
            _apply("catching", 1)
        elif drop_rate > 0.15:
            _apply("catching", -1)

        yac_per = yac / receptions if receptions else 0.0
        if yac_per >= 6:
            _apply("agility", 1)
            _apply("separation", 1)
        elif yac_per < 2:
            _apply("agility", -1)
            _apply("separation", -1)

    # --- Quarterback metrics
    elif position == "QB":
        td = season_stats.get("passing_td", 0)
        ints = season_stats.get("interceptions", 0)
        sacks_taken = season_stats.get("sacks_taken", 0)
        pass_snaps = snap_counts.get("pass", season_stats.get("pass_attempts", 0) + sacks_taken)

        ratio = td / (ints if ints else 1)
        if ratio >= 3:
            _apply("awareness", 2)
        elif ratio >= 2:
            _apply("awareness", 1)
        elif ratio < 1:
            _apply("awareness", -1)

        sack_rate = sacks_taken / pass_snaps if pass_snaps else 0
        if sack_rate <= 0.05:
            _apply("pocket_presence", 1)
        elif sack_rate > 0.10:
            _apply("pocket_presence", -1)

    # --- Running Back metrics
    elif position == "RB":
        attempts = season_stats.get("rush_attempts", 0)
        yards = season_stats.get("rushing_yards", 0)
        fumbles = season_stats.get("fumbles", 0)
        receptions = season_stats.get("receptions", 0)

        ypc = yards / attempts if attempts else 0
        if ypc >= 5:
            _apply("agility", 1)
            _apply("break_tackle", 1)
        elif ypc <= 3 and attempts > 50:
            _apply("agility", -1)
            _apply("break_tackle", -1)

        touches = attempts + receptions
        fumble_rate = fumbles / touches if touches else 0
        if fumble_rate <= 0.01 and touches >= 50:
            _apply("carry_security", 1)
        elif fumble_rate > 0.03:
            _apply("carry_security", -1)

    # --- Offensive Line metrics
    elif position in {"LT", "LG", "C", "RG", "RT", "OL"}:
        sacks_allowed = season_stats.get("sacks_allowed", 0)
        pass_snaps = snap_counts.get("pass_block", snap_counts.get("offense", 0))
        rate = sacks_allowed / pass_snaps if pass_snaps else 0
        if rate <= 0.02:
            _apply("pass_block", 1)
        elif rate > 0.06:
            _apply("pass_block", -1)

    # --- Defensive Line / Edge metrics
    elif position in {"DL", "DE", "EDGE"}:
        pressures = season_stats.get("pressures", 0) + season_stats.get("sacks", 0)
        snaps = snap_counts.get("pass_rush", snap_counts.get("defense", 0))
        rate = pressures / snaps if snaps else 0
        if rate >= 0.12:
            _apply("pass_rush_finesse", 1)
            _apply("pass_rush_power", 1)
        elif rate < 0.05 and snaps > 100:
            _apply("pass_rush_finesse", -1)
            _apply("pass_rush_power", -1)

    # --- Linebacker metrics
    elif position in {"LB", "ILB", "OLB"}:
        missed = season_stats.get("missed_tackles", 0)
        snaps = snap_counts.get("defense", 0)
        rate = missed / snaps if snaps else 0
        if rate <= 0.05:
            _apply("tackle_lb", 1)
            _apply("play_recognition_lb", 1)
        elif rate > 0.15:
            _apply("tackle_lb", -1)
            _apply("play_recognition_lb", -1)

    # --- Defensive Back metrics
    elif position in {"CB", "S", "DB"}:
        targets = season_stats.get("targets", 0)
        completions = season_stats.get("completions_allowed", 0)
        ints = season_stats.get("interceptions", 0)
        pbu = season_stats.get("pass_breakups", 0)
        if targets:
            success = (ints + pbu) / targets
            comp_rate = completions / targets
        else:
            success = 0
            comp_rate = 0
        if success >= 0.25:
            if position == "CB":
                _apply("man_coverage", 1)
                _apply("zone_coverage", 1)
            else:
                _apply("man_coverage_s", 1)
                _apply("zone_coverage_s", 1)
        elif comp_rate > 0.75 and targets > 30:
            if position == "CB":
                _apply("man_coverage", -1)
                _apply("zone_coverage", -1)
            else:
                _apply("man_coverage_s", -1)
                _apply("zone_coverage_s", -1)

    # --- Special Teams metrics
    elif position in {"K", "P", "LS"}:
        if position == "K":
            made = season_stats.get("fg_made", 0)
            att = season_stats.get("fg_attempts", 0)
            pct = made / att if att else 0
            if pct >= 0.90:
                _apply("kick_accuracy", 1)
            elif pct < 0.75:
                _apply("kick_accuracy", -1)
        elif position == "P":
            avg = season_stats.get("punt_net_avg", season_stats.get("punt_avg", 0))
            if avg >= 45:
                _apply("kick_power", 1)
            elif avg < 40 and snap_counts.get("special_teams", 0) > 40:
                _apply("kick_power", -1)

    return deltas
