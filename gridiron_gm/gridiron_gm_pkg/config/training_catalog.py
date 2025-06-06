TRAINING_CATALOG = {
    # === Quarterback ===
    "QB Target Drill": {
        "positions": ["QB"],
        "attribute_weights": {
            "throw_accuracy_short": 0.5,
            "throw_accuracy_medium": 0.5,
            "throw_accuracy_deep": 0.5,
        },
        "injury_chance": 0.002,
    },
    "QB Scramble Drill": {
        "positions": ["QB"],
        "attribute_weights": {
            "throw_on_the_run": 0.6,
            "pocket_presence": 0.3,
        },
        "injury_chance": 0.003,
    },
    "QB Pocket Drill": {
        "positions": ["QB"],
        "attribute_weights": {
            "pocket_presence": 0.6,
            "throw_under_pressure": 0.5,
            "read_progression": 0.4,
        },
        "injury_chance": 0.001,
    },
    "QB Read Progressions": {
        "positions": ["QB"],
        "attribute_weights": {
            "awareness": 0.7,
            "iq": 0.5,
        },
        "injury_chance": 0.0001,
    },
    "QB Footwork Drill": {
        "positions": ["QB"],
        "attribute_weights": {
            "throwing_footwork": 0.6,
            "throw_accuracy_short": 0.2,
        },
        "injury_chance": 0.002,
    },
    "QB Deep Ball Drill": {
        "positions": ["QB"],
        "attribute_weights": {
            "throw_accuracy_deep": 0.7,
            "throw_power": 0.5,
        },
        "injury_chance": 0.003,
    },
    "QB Short Pass Drill": {
        "positions": ["QB"],
        "attribute_weights": {
            "throw_accuracy_short": 0.8,
            "release_time": 0.4,
        },
        "injury_chance": 0.002,
    },


    # === Running Back ===
    "RB Cone Agility": {
        "positions": ["RB"],
        "attribute_weights": {
            "agility": 1.0,
            "acceleration": 0.8,
        },
        "injury_chance": 0.004,
    },
    "RB Ball Security": {
        "positions": ["RB"],
        "attribute_weights": {
            "carry_security": 1.0,
            "discipline": 0.3,
        },
        "injury_chance": 0.002,
    },
    "RB Inside Running": {
        "positions": ["RB"],
        "attribute_weights": {
            "acceleration": 1.0,
            "balance": 0.6,
        },
        "injury_chance": 0.003,
    },
    "RB Break Tackles": {
        "positions": ["RB"],
        "attribute_weights": {
            "balance": 0.8,
            "elusiveness": 0.5,
        },
        "injury_chance": 0.005,
    },
    "RB Route Running": {
        "positions": ["RB"],
        "attribute_weights": {
            "route_running_short": 0.7,
            "route_running_medium": 0.5,
        },
        "injury_chance": 0.003,
    },
    "RB Pass Protection": {
        "positions": ["RB"],
        "attribute_weights": {
            "pass_blocking": 1.0,
            "awareness": 0.4,
        },
        "injury_chance": 0.004,
    },
    "RB Receiving Skills": {
        "positions": ["RB"],
        "attribute_weights": {
            "catching": 0.8,
        },
        "injury_chance": 0.002,
    },

    # === Wide Receiver ===
    "WR Footwork": {
        "positions": ["WR"],
        "attribute_weights": {
            "route_running_short": 1.0,
            "agility": 0.8,
        },
        "injury_chance": 0.004,
    },
    "WR Route Tree": {
        "positions": ["WR"],
        "attribute_weights": {
            "route_running_medium": 0.3,
            "route_running_short": 0.3,
            "route_running_deep": 0.3,
        },
        "injury_chance": 0.004,
    },
    "WR Hands Drill": {
        "positions": ["WR"],
        "attribute_weights": {
            "catching": 1.0,
            "catch_in_traffic": 0.3,
        },
        "injury_chance": 0.002,
    },
    "WR Release Technique": {
        "positions": ["WR"],
        "attribute_weights": {
            "release": 1.0,
            "seperation": 0.4,
        },
        "injury_chance": 0.003,
    },
    "WR Blocking Technique": {
        "positions": ["WR"],
        "attribute_weights": {
            "blocking": 0.6,
            "strength": 0.4,
        },
        "injury_chance": 0.005,
    },
    "WR Catching on the Run": {
        "positions": ["WR"],
        "attribute_weights": {
            "catching": 0.8,
            "route_running_short": 0.5,
        },
        "injury_chance": 0.002,
    },
    "WR Deep Ball Tracking": {
        "positions": ["WR"],
        "attribute_weights": {
            "route_running_deep": 0.7,
            "catching": 0.5,
        },
        "injury_chance": 0.003,
    },
    "WR YAC Drills": {
        "positions": ["WR"],
        "attribute_weights": {
            "elusiveness": 0.6,
            "acceleration": 0.5,
        },
        "injury_chance": 0.004,
    },
    "WR Jump Ball Drills": {
        "positions": ["WR"],
        "attribute_weights": {
            "catching": 0.5,
            "jumping": 0.5,
            "catch_in_traffic": 0.5,
        },
        "injury_chance": 0.003,
    },

    # === Tight End ===
    "TE Blocking Technique": {
        "positions": ["TE"],
        "attribute_weights": {
            "run_blocking": 0.3,
            "pass_blocking": 0.3,
            "lead_blocking": 0.3,
        },
        "injury_chance": 0.005,
    },
    "TE Route Combos": {
        "positions": ["TE"],
        "attribute_weights": {
            "route_running_short": 0.6,
            "route_running_medium": 0.6,
        },
        "injury_chance": 0.003,
    },
    "TE Catching Drills": {
        "positions": ["TE"],
        "attribute_weights": {
            "catching": 0.8,
            "catch_in_traffic": 0.4,
        },
        "injury_chance": 0.002,
    },
    "TE Release Technique": {
        "positions": ["TE"],
        "attribute_weights": {
            "release": 0.5,
            "seperation": 0.5,
        },
        "injury_chance": 0.003,
    },
    "TE Deep Ball Training": {
        "positions": ["TE"],
        "attribute_weights": {
            "route_running_deep": 0.6,
            "catching": 0.5,
        },
        "injury_chance": 0.004,
    },

    # === Offensive Line Drills (Attribute-Accurate) ===
    "OL Anchor Strength": {
        "positions": ["OL"],
        "attribute_weights": {
            "impact_blocking": 0.8,
            "block_shed_resistance": 0.6,
        },
        "injury_chance": 0.006,
    },
    "OL Pass Set Footwork": {
        "positions": ["OL"],
        "attribute_weights": {
            "footwork_ol": 1.0,
            "pass_block": 0.5,
        },
        "injury_chance": 0.004,
    },
    "OL Combo Block Recognition": {
        "positions": ["OL"],
        "attribute_weights": {
            "awareness": 0.5,
            "run_block": 0.8,
        },
        "injury_chance": 0.005,
    },
    "OL Down Block Timing": {
        "positions": ["OL"],
        "attribute_weights": {
            "run_block": 0.6,
            "discipline": 0.5,
        },
        "injury_chance": 0.004,
    },
    "OL Mirror Step Drill": {
        "positions": ["OL"],
        "attribute_weights": {
            "footwork_ol": 0.7,
            "balance": 0.6,
        },
        "injury_chance": 0.004,
    },
    "OL Leverage & Pad Level": {
        "positions": ["OL"],
        "attribute_weights": {
            "block_shed_resistance": 0.6,
            "discipline": 0.4,
        },
        "injury_chance": 0.005,
    },
    "OL Trap & Pull Execution": {
        "positions": ["OL"],
        "attribute_weights": {
            "lead_blocking": 0.7,
            "acceleration": 0.5,
        },
        "injury_chance": 0.005,
    },



    # === Defensive Line ===
    "DL Hand Combat": {
        "positions": ["DL"],
        "attribute_weights": {
            "strength": 0.9,
            "discipline": 0.5,
        },
        "injury_chance": 0.005,
    },
    "DL Burst Drill": {
        "positions": ["DL"],
        "attribute_weights": {
            "acceleration": 1.0,
            "agility": 0.5,
        },
        "injury_chance": 0.006,
    },
    "DL Gap Control": {
        "positions": ["DL"],
        "attribute_weights": {
            "awareness": 0.6,
            "discipline": 0.5,
        },
        "injury_chance": 0.004,
    },

    # === Linebacker ===
    "LB Tackle Fit": {
        "positions": ["LB"],
        "attribute_weights": {
            "tackle_lb": 1.0,
            "balance": 0.5,
        },
        "injury_chance": 0.005,
    },
    "LB Coverage Drops": {
        "positions": ["LB"],
        "attribute_weights": {
            "coverage_zone": 1.0,
            "awareness": 0.5,
        },
        "injury_chance": 0.003,
    },
    "LB Blitz Timing": {
        "positions": ["LB"],
        "attribute_weights": {
            "acceleration": 1.0,
            "discipline": 0.4,
        },
        "injury_chance": 0.005,
    },

    # === Defensive Back ===
    "DB Press Jam": {
        "positions": ["CB", "S"],
        "attribute_weights": {
            "strength": 0.8,
            "coverage_man": 1.0,
        },
        "injury_chance": 0.004,
    },
    "DB Backpedal & Break": {
        "positions": ["CB", "S"],
        "attribute_weights": {
            "agility": 0.9,
            "acceleration": 0.7,
        },
        "injury_chance": 0.004,
    },
    "DB Ball Skills": {
        "positions": ["CB", "S"],
        "attribute_weights": {
            "catching": 0.9,
            "awareness": 0.5,
        },
        "injury_chance": 0.002,
    },

    # === Specialists ===
    "K Accuracy Drill": {
        "positions": ["K"],
        "attribute_weights": {
            "discipline": 0.7,
            "awareness": 0.4,
        },
        "injury_chance": 0.001,
    },
    "P Directional Kick": {
        "positions": ["P"],
        "attribute_weights": {
            "discipline": 0.6,
            "awareness": 0.5,
        },
        "injury_chance": 0.001,
    },

    # === General / Team-Wide ===
    "Weight Room": {
        "positions": "ALL",
        "attribute_weights": {
            "strength": 1.0,
            "stamina": 0.5,
            "durability": 0.5,
        },
        "injury_chance": 0.01,
    },
    "Film Study": {
        "positions": "ALL",
        "attribute_weights": {
            "iq": 1.0,
            "awareness": 0.5,
        },
        "injury_chance": 0.0,
    },
    "Full Speed Conditioning": {
        "positions": "ALL",
        "attribute_weights": {
            "stamina": 1.0,
            "acceleration": 0.6,
        },
        "injury_chance": 0.01,
    },
    "Agility Ladder": {
        "positions": "ALL",
        "attribute_weights": {
            "agility": 1.0,
            "balance": 0.5,
        },
        "injury_chance": 0.005,
    },
    "Walkthrough Practice": {
        "positions": "ALL",
        "attribute_weights": {
            "discipline": 0.3,
            "awareness": 0.3,
        },
        "injury_chance": 0.0,
    },
    "Mental Sharpness": {
        "positions": "ALL",
        "attribute_weights": {
            "awareness": 0.4,
            "iq": 0.6,
        },
        "injury_chance": 0.0,
    },
}
# Ensure all training drills have unique names