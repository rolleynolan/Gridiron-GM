import json
from typing import Any, Dict
from pathlib import Path

BASE_DIR = Path(__file__).resolve().parents[2]

def load_settings(path: str | None = None) -> Dict[str, Any]:
    """Load simulation settings from a JSON config file."""
    if path is None:
        path = BASE_DIR / "config" / "settings.json"
    else:
        path = Path(path)
    with open(path, "r") as f:
        return json.load(f)
