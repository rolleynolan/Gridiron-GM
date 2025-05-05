import random

weather_matrix = {
    "continental": {
        "January": {"temp_range": (15, 40), "chance_snow": 0.4, "chance_rain": 0.2},
        "August": {"temp_range": (65, 85), "chance_snow": 0.0, "chance_rain": 0.3},
    },
    "desert": {
        "January": {"temp_range": (40, 70), "chance_snow": 0.0, "chance_rain": 0.05},
        "August": {"temp_range": (90, 110), "chance_snow": 0.0, "chance_rain": 0.0},
    },
    "humid_subtropical": {
        "January": {"temp_range": (35, 55), "chance_snow": 0.1, "chance_rain": 0.3},
        "August": {"temp_range": (75, 95), "chance_snow": 0.0, "chance_rain": 0.5},
    },
    "marine_west_coast": {
        "January": {"temp_range": (35, 50), "chance_snow": 0.1, "chance_rain": 0.6},
        "August": {"temp_range": (60, 75), "chance_snow": 0.0, "chance_rain": 0.3},
    }
}

# Sample city-to-region mapping
city_regions = {
    "Chicago": "continental",
    "Phoenix": "desert",
    "Atlanta": "humid_subtropical",
    "Seattle": "marine_west_coast",
    "Buffalo": "continental",
}

def get_weather_for_game(city: str, month: str) -> dict:
    region = city_regions.get(city, "continental")  # default to continental
    profile = weather_matrix.get(region, {}).get(month, {"temp_range": (40, 70), "chance_snow": 0.0, "chance_rain": 0.2})

    temp = random.randint(*profile["temp_range"])
    snow = random.random() < profile.get("chance_snow", 0)
    rain = random.random() < profile.get("chance_rain", 0)

    condition = "Clear"
    if snow:
        condition = "Snow"
    elif rain:
        condition = "Rain"

    return {
        "city": city,
        "month": month,
        "region": region,
        "temperature": temp,
        "condition": condition
    }

# Optional: test utility
if __name__ == "__main__":
    for city in city_regions:
        print(get_weather_for_game(city, "January"))
