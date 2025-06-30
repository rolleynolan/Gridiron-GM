<<<<<<< HEAD
INJURY_CATALOG = {
    # -- MUSCLE & SOFT TISSUE INJURIES --
    "Hamstring Strain": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [
            {"type": "recurrence", "target": "soft_tissue", "change": 15, "duration": "season", "notes": "Slightly increased chance of similar injury"}
        ],
        "short_term": [{"type": "attribute", "target": "speed", "change": -2}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Hamstring Tear": {
        "severity": "Severe",
        "weeks": (6, 16),
        "long_term": [
            {"type": "attribute", "target": "speed", "change": -6, "duration": "permanent", "notes": "Permanent speed/agility loss"},
            {"type": "recurrence", "target": "soft_tissue", "change": 30, "duration": "season", "notes": "Major re-injury risk this season"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Groin Strain": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [
            {"type": "recurrence", "target": "groin", "change": 12, "duration": "season", "notes": "Re-injury risk for remainder of season"}
        ],
        "short_term": [{"type": "attribute", "target": "agility", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Groin Tear": {
        "severity": "Moderate",
        "weeks": (4, 10),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -3, "duration": "season", "notes": "Limited agility"},
            {"type": "recurrence", "target": "groin", "change": 20, "duration": "season", "notes": "Major re-injury risk"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Calf Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "recurrence", "target": "calf", "change": 10, "duration": "season", "notes": "Slight chance of re-injury"}
        ],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Quad Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "acceleration", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Quad Tear": {
        "severity": "Severe",
        "weeks": (6, 14),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -3, "duration": "permanent", "notes": "Reduced burst/acceleration"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Abdominal Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Oblique Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "balance", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Back Spasms": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "recurrence", "target": "back", "change": 10, "duration": "season", "notes": "Recurring minor back pain"}
        ],
=======
INJURY_CATALOG = {
    # -- MUSCLE & SOFT TISSUE INJURIES --
    "Hamstring Strain": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [
            {"type": "recurrence", "target": "soft_tissue", "change": 15, "duration": "season", "notes": "Slightly increased chance of similar injury"}
        ],
        "short_term": [{"type": "attribute", "target": "speed", "change": -2}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Hamstring Tear": {
        "severity": "Severe",
        "weeks": (6, 16),
        "long_term": [
            {"type": "attribute", "target": "speed", "change": -6, "duration": "permanent", "notes": "Permanent speed/agility loss"},
            {"type": "recurrence", "target": "soft_tissue", "change": 30, "duration": "season", "notes": "Major re-injury risk this season"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Groin Strain": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [
            {"type": "recurrence", "target": "groin", "change": 12, "duration": "season", "notes": "Re-injury risk for remainder of season"}
        ],
        "short_term": [{"type": "attribute", "target": "agility", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Groin Tear": {
        "severity": "Moderate",
        "weeks": (4, 10),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -3, "duration": "season", "notes": "Limited agility"},
            {"type": "recurrence", "target": "groin", "change": 20, "duration": "season", "notes": "Major re-injury risk"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Calf Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "recurrence", "target": "calf", "change": 10, "duration": "season", "notes": "Slight chance of re-injury"}
        ],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Quad Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "acceleration", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Quad Tear": {
        "severity": "Severe",
        "weeks": (6, 14),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -3, "duration": "permanent", "notes": "Reduced burst/acceleration"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Abdominal Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Oblique Strain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "balance", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Back Spasms": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "recurrence", "target": "back", "change": 10, "duration": "season", "notes": "Recurring minor back pain"}
        ],
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
        "short_term": [
            {"type": "attribute", "target": "strength", "change": -2},
            {"type": "attribute", "target": "throw_power", "change": -2}
        ],
<<<<<<< HEAD
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- LIGAMENT/TENDON & JOINT INJURIES --
    "ACL Tear": {
        "severity": "Severe",
        "weeks": (28, 52),
        "long_term": [
            {"type": "attribute", "target": "speed", "change": -7, "duration": "permanent", "notes": "Reduced max speed"},
            {"type": "attribute", "target": "agility", "change": -5, "duration": "permanent", "notes": "Reduced agility/quickness"},
            {"type": "recurrence", "target": "knee", "change": 35, "duration": "career", "notes": "Knee is re-injury prone"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.12,
        "injury_context": "on_field"
    },
    "Partial ACL Tear": {
        "severity": "Moderate",
        "weeks": (10, 20),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Slightly reduced agility"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "MCL Sprain": {
        "severity": "Minor",
        "weeks": (2, 5),
        "long_term": [
            {"type": "recurrence", "target": "knee", "change": 10, "duration": "season", "notes": "Slight risk of knee injury"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "MCL Tear": {
        "severity": "Moderate",
        "weeks": (6, 12),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Knee instability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Meniscus Tear": {
        "severity": "Moderate",
        "weeks": (3, 8),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Arthritis risk and instability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "PCL Tear": {
        "severity": "Severe",
        "weeks": (12, 26),
        "long_term": [
            {"type": "attribute", "target": "speed", "change": -3, "duration": "permanent", "notes": "Reduced speed/agility"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Achilles Tear": {
        "severity": "Severe",
        "weeks": (26, 52),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -7, "duration": "permanent", "notes": "Major explosiveness loss"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.15,
        "injury_context": "on_field"
    },
    "Achilles Tendonitis": {
        "severity": "Moderate",
        "weeks": (2, 6),
        "long_term": [
            {"type": "recurrence", "target": "achilles", "change": 25, "duration": "season", "notes": "Chronic pain/recurrence risk"}
        ],
        "short_term": [{"type": "attribute", "target": "acceleration", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Torn Rotator Cuff": {
        "severity": "Severe",
        "weeks": (18, 36),
        "long_term": [
            {"type": "attribute", "target": "throwing_power", "change": -6, "duration": "permanent", "notes": "Loss of arm power"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.10,
        "injury_context": "on_field"
    },
=======
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- LIGAMENT/TENDON & JOINT INJURIES --
    "ACL Tear": {
        "severity": "Severe",
        "weeks": (28, 52),
        "long_term": [
            {"type": "attribute", "target": "speed", "change": -7, "duration": "permanent", "notes": "Reduced max speed"},
            {"type": "attribute", "target": "agility", "change": -5, "duration": "permanent", "notes": "Reduced agility/quickness"},
            {"type": "recurrence", "target": "knee", "change": 35, "duration": "career", "notes": "Knee is re-injury prone"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.12,
        "injury_context": "on_field"
    },
    "Partial ACL Tear": {
        "severity": "Moderate",
        "weeks": (10, 20),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Slightly reduced agility"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "MCL Sprain": {
        "severity": "Minor",
        "weeks": (2, 5),
        "long_term": [
            {"type": "recurrence", "target": "knee", "change": 10, "duration": "season", "notes": "Slight risk of knee injury"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "MCL Tear": {
        "severity": "Moderate",
        "weeks": (6, 12),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Knee instability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Meniscus Tear": {
        "severity": "Moderate",
        "weeks": (3, 8),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Arthritis risk and instability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "PCL Tear": {
        "severity": "Severe",
        "weeks": (12, 26),
        "long_term": [
            {"type": "attribute", "target": "speed", "change": -3, "duration": "permanent", "notes": "Reduced speed/agility"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Achilles Tear": {
        "severity": "Severe",
        "weeks": (26, 52),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -7, "duration": "permanent", "notes": "Major explosiveness loss"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.15,
        "injury_context": "on_field"
    },
    "Achilles Tendonitis": {
        "severity": "Moderate",
        "weeks": (2, 6),
        "long_term": [
            {"type": "recurrence", "target": "achilles", "change": 25, "duration": "season", "notes": "Chronic pain/recurrence risk"}
        ],
        "short_term": [{"type": "attribute", "target": "acceleration", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Torn Rotator Cuff": {
        "severity": "Severe",
        "weeks": (18, 36),
        "long_term": [
            {"type": "attribute", "target": "throwing_power", "change": -6, "duration": "permanent", "notes": "Loss of arm power"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.10,
        "injury_context": "on_field"
    },
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
    "Shoulder Dislocation": {
        "severity": "Moderate",
        "weeks": (2, 6),
        "long_term": [
            {"type": "recurrence", "target": "shoulder", "change": 20, "duration": "career", "notes": "Shoulder instability"}
        ],
        "short_term": [
            {"type": "attribute", "target": "strength", "change": -2},
            {"type": "attribute", "target": "throw_power", "change": -2}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
<<<<<<< HEAD
    "Labrum Tear": {
        "severity": "Moderate",
        "weeks": (4, 8),
        "long_term": [
            {"type": "attribute", "target": "throwing_power", "change": -2, "duration": "season", "notes": "Reduced shoulder stability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Biceps Tendon Tear": {
        "severity": "Moderate",
        "weeks": (8, 18),
        "long_term": [
            {"type": "attribute", "target": "strength", "change": -2, "duration": "season", "notes": "Reduced arm strength"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "UCL Injury": {
        "severity": "Moderate",
        "weeks": (8, 20),
        "long_term": [
            {"type": "attribute", "target": "throwing_accuracy", "change": -3, "duration": "season", "notes": "Reduced throwing accuracy"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- FRACTURES/BREAKS/DISLOCATIONS --
    "Broken Hand": {
        "severity": "Moderate",
        "weeks": (3, 8),
        "long_term": [
            {"type": "attribute", "target": "catching", "change": -3, "duration": "season", "notes": "Reduced grip and catch ability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Finger": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "catching", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Wrist": {
        "severity": "Moderate",
        "weeks": (4, 8),
        "long_term": [
            {"type": "attribute", "target": "catching", "change": -2, "duration": "season", "notes": "Reduced wrist strength for catching"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Arm": {
        "severity": "Moderate",
        "weeks": (4, 10),
        "long_term": [
            {"type": "attribute", "target": "tackle_lb", "change": -2, "duration": "season", "notes": "Arm not fully strong for tackles"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Fractured Rib": {
        "severity": "Minor",
        "weeks": (2, 5),
        "long_term": [
            {"type": "attribute", "target": "toughness", "change": -2, "duration": "season", "notes": "Lingering pain"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Multiple Rib Fractures": {
        "severity": "Moderate",
        "weeks": (5, 10),
        "long_term": [
            {"type": "attribute", "target": "toughness", "change": -4, "duration": "season", "notes": "Pain on contact, breathing issues"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Collarbone": {
        "severity": "Moderate",
        "weeks": (6, 10),
        "long_term": [
            {"type": "attribute", "target": "run_block", "change": -3, "duration": "season", "notes": "Reduced upper body strength"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Dislocated Shoulder": {
        "severity": "Moderate",
        "weeks": (3, 6),
        "long_term": [
            {"type": "recurrence", "target": "shoulder", "change": 25, "duration": "career", "notes": "Major instability risk"}
        ],
        "short_term": [{"type": "attribute", "target": "strength", "change": -2}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Dislocated Finger": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "catching", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- FOOT/ANKLE/LOWER BODY --
    "High Ankle Sprain": {
        "severity": "Moderate",
        "weeks": (2, 5),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Agility limited for season"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Low Ankle Sprain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "agility", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Lisfranc Injury": {
        "severity": "Severe",
        "weeks": (20, 40),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -5, "duration": "permanent", "notes": "Loss of foot push-off"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.10,
        "injury_context": "on_field"
    },
    "Turf Toe": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -2, "duration": "season", "notes": "Push-off affected for season"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Plantar Fasciitis": {
        "severity": "Minor",
        "weeks": (2, 8),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -2, "duration": "season", "notes": "Foot pain slows burst"}
        ],
        "career_ending": False,
        "injury_context": "either"
    },
    "Stress Fracture (Foot)": {
        "severity": "Moderate",
        "weeks": (4, 12),
        "long_term": [
            {"type": "recurrence", "target": "foot", "change": 15, "duration": "season", "notes": "Recurring foot pain risk"}
        ],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Shin Splints": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Broken Toe": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "acceleration", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- HEAD & NECK --
    "Concussion": {
        "severity": "Moderate",
        "weeks": (1, 5),
        "long_term": [
            {"type": "attribute", "target": "awareness", "change": -4, "duration": "season", "notes": "Cognitive issues"},
            {"type": "recurrence", "target": "concussion", "change": 20, "duration": "career", "notes": "Higher risk of future concussion"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.07,
        "injury_context": "on_field"
    },
    "Severe Concussion": {
        "severity": "Severe",
        "weeks": (6, 14),
        "long_term": [
            {"type": "attribute", "target": "awareness", "change": -8, "duration": "permanent", "notes": "Chronic cognitive problems"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.25,
        "injury_context": "on_field"
    },
    "Neck Sprain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "awareness", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Stinger (Nerve)": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "recurrence", "target": "nerve", "change": 10, "duration": "season", "notes": "Minor risk of repeat nerve injury"}
        ],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Herniated Disc": {
        "severity": "Severe",
        "weeks": (16, 36),
        "long_term": [
            {"type": "attribute", "target": "strength", "change": -4, "duration": "permanent", "notes": "Chronic back pain/weakness"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.18,
        "injury_context": "on_field"
    },

    # -- ILLNESS / GENERAL / OFF-FIELD --
    "Flu (Mild)": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "stamina", "change": -2}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Stomach Virus": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "stamina", "change": -2}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Weight Room Pectoral Tear": {
        "severity": "Severe",
        "weeks": (12, 24),
        "long_term": [
            {"type": "attribute", "target": "strength", "change": -6, "duration": "permanent", "notes": "Upper body power loss"}
        ],
        "career_ending": False,
        "injury_context": "off_field"
    },
    "Back Spasm (Off-Field)": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -2}],
        "career_ending": False,
        "injury_context": "off_field"
    },

    # -- EYE, FACE, DENTAL --
    "Corneal Abrasion": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "attribute", "target": "awareness", "change": -1, "duration": "season", "notes": "Occasional vision issues"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Nasal Fracture": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "awareness", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Jaw Fracture": {
        "severity": "Moderate",
        "weeks": (4, 8),
        "long_term": [
            {"type": "attribute", "target": "toughness", "change": -2, "duration": "season", "notes": "Lingering pain affects toughness"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Dental Avulsion": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "awareness", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- CONTUSIONS/BRUISES/GENERIC --
    "Knee Contusion": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Thigh Contusion": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Shoulder Bruise": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Back Bruise": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Elbow Bruise": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- GENERIC FATIGUE/CRAMPING --
    "General Fatigue": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "stamina", "change": -1}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Cramps": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    }
}

# ~65 injuries (many with variants and realistic mappings).
# You can expand by adding position-specific versions or minor tweaks as needed.
=======
    "Labrum Tear": {
        "severity": "Moderate",
        "weeks": (4, 8),
        "long_term": [
            {"type": "attribute", "target": "throwing_power", "change": -2, "duration": "season", "notes": "Reduced shoulder stability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Biceps Tendon Tear": {
        "severity": "Moderate",
        "weeks": (8, 18),
        "long_term": [
            {"type": "attribute", "target": "strength", "change": -2, "duration": "season", "notes": "Reduced arm strength"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "UCL Injury": {
        "severity": "Moderate",
        "weeks": (8, 20),
        "long_term": [
            {"type": "attribute", "target": "throwing_accuracy", "change": -3, "duration": "season", "notes": "Reduced throwing accuracy"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- FRACTURES/BREAKS/DISLOCATIONS --
    "Broken Hand": {
        "severity": "Moderate",
        "weeks": (3, 8),
        "long_term": [
            {"type": "attribute", "target": "catching", "change": -3, "duration": "season", "notes": "Reduced grip and catch ability"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Finger": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "catching", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Wrist": {
        "severity": "Moderate",
        "weeks": (4, 8),
        "long_term": [
            {"type": "attribute", "target": "catching", "change": -2, "duration": "season", "notes": "Reduced wrist strength for catching"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Arm": {
        "severity": "Moderate",
        "weeks": (4, 10),
        "long_term": [
            {"type": "attribute", "target": "tackle_lb", "change": -2, "duration": "season", "notes": "Arm not fully strong for tackles"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Fractured Rib": {
        "severity": "Minor",
        "weeks": (2, 5),
        "long_term": [
            {"type": "attribute", "target": "toughness", "change": -2, "duration": "season", "notes": "Lingering pain"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Multiple Rib Fractures": {
        "severity": "Moderate",
        "weeks": (5, 10),
        "long_term": [
            {"type": "attribute", "target": "toughness", "change": -4, "duration": "season", "notes": "Pain on contact, breathing issues"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Broken Collarbone": {
        "severity": "Moderate",
        "weeks": (6, 10),
        "long_term": [
            {"type": "attribute", "target": "run_block", "change": -3, "duration": "season", "notes": "Reduced upper body strength"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Dislocated Shoulder": {
        "severity": "Moderate",
        "weeks": (3, 6),
        "long_term": [
            {"type": "recurrence", "target": "shoulder", "change": 25, "duration": "career", "notes": "Major instability risk"}
        ],
        "short_term": [{"type": "attribute", "target": "strength", "change": -2}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Dislocated Finger": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "catching", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- FOOT/ANKLE/LOWER BODY --
    "High Ankle Sprain": {
        "severity": "Moderate",
        "weeks": (2, 5),
        "long_term": [
            {"type": "attribute", "target": "agility", "change": -2, "duration": "season", "notes": "Agility limited for season"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Low Ankle Sprain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "agility", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Lisfranc Injury": {
        "severity": "Severe",
        "weeks": (20, 40),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -5, "duration": "permanent", "notes": "Loss of foot push-off"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.10,
        "injury_context": "on_field"
    },
    "Turf Toe": {
        "severity": "Minor",
        "weeks": (1, 3),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -2, "duration": "season", "notes": "Push-off affected for season"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Plantar Fasciitis": {
        "severity": "Minor",
        "weeks": (2, 8),
        "long_term": [
            {"type": "attribute", "target": "acceleration", "change": -2, "duration": "season", "notes": "Foot pain slows burst"}
        ],
        "career_ending": False,
        "injury_context": "either"
    },
    "Stress Fracture (Foot)": {
        "severity": "Moderate",
        "weeks": (4, 12),
        "long_term": [
            {"type": "recurrence", "target": "foot", "change": 15, "duration": "season", "notes": "Recurring foot pain risk"}
        ],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Shin Splints": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Broken Toe": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "acceleration", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- HEAD & NECK --
    "Concussion": {
        "severity": "Moderate",
        "weeks": (1, 5),
        "long_term": [
            {"type": "attribute", "target": "awareness", "change": -4, "duration": "season", "notes": "Cognitive issues"},
            {"type": "recurrence", "target": "concussion", "change": 20, "duration": "career", "notes": "Higher risk of future concussion"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.07,
        "injury_context": "on_field"
    },
    "Severe Concussion": {
        "severity": "Severe",
        "weeks": (6, 14),
        "long_term": [
            {"type": "attribute", "target": "awareness", "change": -8, "duration": "permanent", "notes": "Chronic cognitive problems"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.25,
        "injury_context": "on_field"
    },
    "Neck Sprain": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "awareness", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Stinger (Nerve)": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "recurrence", "target": "nerve", "change": 10, "duration": "season", "notes": "Minor risk of repeat nerve injury"}
        ],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Herniated Disc": {
        "severity": "Severe",
        "weeks": (16, 36),
        "long_term": [
            {"type": "attribute", "target": "strength", "change": -4, "duration": "permanent", "notes": "Chronic back pain/weakness"}
        ],
        "career_ending": True,
        "career_ending_chance": 0.18,
        "injury_context": "on_field"
    },

    # -- ILLNESS / GENERAL / OFF-FIELD --
    "Flu (Mild)": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "stamina", "change": -2}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Stomach Virus": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "stamina", "change": -2}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Weight Room Pectoral Tear": {
        "severity": "Severe",
        "weeks": (12, 24),
        "long_term": [
            {"type": "attribute", "target": "strength", "change": -6, "duration": "permanent", "notes": "Upper body power loss"}
        ],
        "career_ending": False,
        "injury_context": "off_field"
    },
    "Back Spasm (Off-Field)": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -2}],
        "career_ending": False,
        "injury_context": "off_field"
    },

    # -- EYE, FACE, DENTAL --
    "Corneal Abrasion": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [
            {"type": "attribute", "target": "awareness", "change": -1, "duration": "season", "notes": "Occasional vision issues"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Nasal Fracture": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "awareness", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Jaw Fracture": {
        "severity": "Moderate",
        "weeks": (4, 8),
        "long_term": [
            {"type": "attribute", "target": "toughness", "change": -2, "duration": "season", "notes": "Lingering pain affects toughness"}
        ],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Dental Avulsion": {
        "severity": "Minor",
        "weeks": (1, 2),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "awareness", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- CONTUSIONS/BRUISES/GENERIC --
    "Knee Contusion": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Thigh Contusion": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Shoulder Bruise": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Back Bruise": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },
    "Elbow Bruise": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "strength", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    },

    # -- GENERIC FATIGUE/CRAMPING --
    "General Fatigue": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "stamina", "change": -1}],
        "career_ending": False,
        "injury_context": "either"
    },
    "Cramps": {
        "severity": "Minor",
        "weeks": (0, 1),
        "long_term": [],
        "short_term": [{"type": "attribute", "target": "speed", "change": -1}],
        "career_ending": False,
        "injury_context": "on_field"
    }
}

# ~65 injuries (many with variants and realistic mappings).
# You can expand by adding position-specific versions or minor tweaks as needed.
>>>>>>> 79cffd4b947bd107948f6d67c5add907b1462802
