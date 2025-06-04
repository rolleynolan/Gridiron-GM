import json
from typing import Any, Dict

def load_settings(path: str = "config/settings.json") -> Dict[str, Any]:
    """Load simulation settings from a JSON config file."""
    with open(path, "r") as f:
        return json.load(f)
