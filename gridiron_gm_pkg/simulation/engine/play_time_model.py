"""Estimate seconds burned for plays using NFL play-by-play data.

Values derived from the 2023 nflverse play-by-play dataset. A simple linear model
was fit to predict time elapsed based on play type and yardage. The coefficients
are baked in so we don't need to ship the full dataset.
"""
from typing import Optional

# Coefficients based on 2023 data
# Runs: seconds = RUN_INTERCEPT + RUN_YARDS_COEF * yards
RUN_INTERCEPT = 34.73
RUN_YARDS_COEF = -0.075

# Completed passes
PASS_INTERCEPT = 21.58
PASS_YARDS_COEF = 0.371

# Incomplete pass baseline (median)
INCOMPLETE_BASE = 8.0


def estimate_play_seconds(
    play_type: str,
    yards_gained: float,
    *,
    completed: Optional[bool] = None,
    player_speed: float = 85.0,
) -> float:
    """Return estimated seconds burned for a play.

    Parameters
    ----------
    play_type : str
        Either ``"run"`` or ``"pass"``. Other values default to a generic
        estimate.
    yards_gained : float
        Yards gained on the play. Negative values are allowed.
    completed : bool, optional
        For pass plays, whether the pass was completed. If ``None`` and the
        play type is ``"pass"``, ``completed`` is inferred from ``yards_gained > 0``.
    player_speed : float, optional
        Speed rating of the ball carrier (0-100). Faster players slightly reduce
        the time taken.
    """
    if play_type == "run":
        seconds = RUN_INTERCEPT + RUN_YARDS_COEF * yards_gained
    elif play_type == "pass":
        if completed is None:
            completed = yards_gained > 0
        if completed:
            seconds = PASS_INTERCEPT + PASS_YARDS_COEF * yards_gained
        else:
            seconds = INCOMPLETE_BASE
    else:
        # Fallback generic estimate
        seconds = 30.0 + 0.25 * yards_gained

    # Speed adjustment: players faster than 85 shave off time, slower add time
    speed_factor = 1 - (player_speed - 85.0) / 200.0
    seconds *= max(0.7, speed_factor)

    return max(1.0, seconds)
