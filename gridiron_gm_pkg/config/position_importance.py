# Basic positional attribute importance weights for temporary injury effects
# Values roughly represent how critical a given attribute is for each position.
# Attributes not listed default to 1.0 weight.
POSITION_IMPORTANCE = {
    "QB": {
        "throw_power": 1.5,
        "throwing_power": 1.5,
        "throwing_accuracy": 1.3,
        "awareness": 1.2,
        "agility": 0.8,
        "speed": 0.7,
    },
    "WR": {
        "catching": 1.5,
        "speed": 1.2,
        "agility": 1.2,
        "strength": 0.8,
    },
    "RB": {
        "speed": 1.2,
        "agility": 1.3,
        "strength": 1.0,
    },
    "OL": {
        "strength": 1.3,
        "balance": 1.2,
    },
    "DL": {
        "strength": 1.2,
    },
    "LB": {
        "strength": 1.1,
        "agility": 1.0,
    },
    "CB": {
        "speed": 1.3,
        "agility": 1.3,
        "awareness": 1.1,
    },
    "S": {
        "speed": 1.2,
        "agility": 1.1,
        "awareness": 1.1,
    },
    "TE": {
        "catching": 1.2,
        "strength": 1.1,
    },
    "K": {
        "kick_power": 1.3,
        "kick_accuracy": 1.3,
    },
    "P": {
        "kick_power": 1.3,
        "kick_accuracy": 1.3,
    },
}
