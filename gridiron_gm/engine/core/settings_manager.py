import json
import os

SETTINGS_FILE_PATH = "gridiron_gm/data/settings.json"

DEFAULT_SETTINGS = {
    "graphics": {
        "quality": "high"
    },
    "audio": {
        "master_volume": 100,
        "music_volume": 80,
        "effects_volume": 100
    },
    "basic": {
        "language": "English",
        "ui_theme": "dark"
    }
}

def load_settings():
    if not os.path.exists(SETTINGS_FILE_PATH):
        save_settings(DEFAULT_SETTINGS)
        return DEFAULT_SETTINGS
    try:
        with open(SETTINGS_FILE_PATH, "r") as file:
            settings = json.load(file)
        print("\nSettings loaded successfully!")
        return settings
    except Exception as e:
        print(f"Error loading settings: {e}")
        return DEFAULT_SETTINGS

def save_settings(settings):
    try:
        with open(SETTINGS_FILE_PATH, "w") as file:
            json.dump(settings, file, indent=4)
        print("\nSettings saved successfully!")
    except Exception as e:
        print(f"Error saving settings: {e}")
