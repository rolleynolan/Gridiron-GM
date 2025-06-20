import random
from typing import Dict, Any
from gridiron_gm_pkg.simulation.entities.player import Player
from gridiron_gm_pkg.simulation.entities.scout import Scout

SCOUTING_TEMPLATES = {
    "elite": [
        "Super {trait_desc} — {behavior_comment}",
        "Rare {trait_desc}, makes plays look easy",
    ],
    "good": [
        "Strong {trait_desc}, usually consistent",
        "Shows flashes of {trait_desc} when given time",
    ],
    "average": [
        "Decent {trait_desc}, but room to grow",
        "Generally solid in {trait}, but not a standout",
    ],
    "poor": [
        "Inconsistent {trait_desc}, needs coaching",
        "Struggles with {trait_desc} against pressure",
    ],
    "bad": [
        "Lacks {trait_desc} — clear area of concern",
        "Very raw {trait_desc}, could be a liability",
    ],
}

ATTRIBUTE_TITLES = {
    "throw_power": "arm strength",
    "throw_accuracy_deep": "deep accuracy",
    "throw_on_run": "mobility as a passer",
    "read_progression": "field vision",
    "pocket_presence": "pocket awareness",
    "speed": "long speed",
    "awareness": "game IQ",
    "tackle_lb": "tackling technique",
    "catching": "hands",
}


def estimate_attribute(actual: float, scout_accuracy: float, exposure: float) -> tuple[float, float]:
    """Return an estimated range for an attribute based on accuracy and exposure."""
    range_width = max(2.0, 12.0 - (scout_accuracy * 5.0) - (exposure * 5.0))
    noise = random.uniform(-1.0, 1.0)
    est_min = round(max(40.0, actual - range_width / 2 + noise), 1)
    est_max = round(min(99.0, actual + range_width / 2 + noise), 1)
    return est_min, est_max


def generate_text_snippets(attribute_name: str, est_avg: float) -> str:
    """Return a short text line based on estimated value."""
    trait = ATTRIBUTE_TITLES.get(attribute_name, attribute_name.replace("_", " "))
    tier = (
        "elite" if est_avg >= 90 else
        "good" if est_avg >= 80 else
        "average" if est_avg >= 70 else
        "poor" if est_avg >= 60 else "bad"
    )
    template = random.choice(SCOUTING_TEMPLATES[tier])
    behavior = "consistently shows it" if tier in ["elite", "good"] else "inconsistent in this area"
    return template.format(trait=trait, trait_desc=trait, behavior_comment=behavior)


def generate_scouting_report(player: Player, scout: Scout) -> Dict[str, Any]:
    """Generate a scouting report with fog-of-war attribute ranges and comments."""
    report = {
        "scout": scout.name,
        "player_name": player.name,
        "position": player.position,
        "text_eval": [],
        "estimates": {},
    }

    attributes = player.get_all_attributes()
    for attr, val in attributes.items():
        if attr not in ATTRIBUTE_TITLES:
            continue
        exposure = scout.exposure_map.get(player.id, 0.5)
        accuracy = scout.get_accuracy_for(attr)
        est_min, est_max = estimate_attribute(float(val or 50), accuracy, exposure)
        est_avg = (est_min + est_max) / 2
        report["estimates"][attr] = {"range": (est_min, est_max)}
        if random.random() < 0.25 + (accuracy * 0.5):
            report["text_eval"].append(generate_text_snippets(attr, est_avg))

    return report
