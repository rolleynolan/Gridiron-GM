import datetime

def create_gm():
    print("\n--- GM Creator ---")

    # GM Name
    gm_name = input("Enter your GM's name: ").strip()

    # GM Date of Birth
    while True:
        gm_dob = input("Enter your GM's Date of Birth (MM/DD/YYYY): ").strip()
        if validate_dob(gm_dob):
            break
        print("Invalid date format. Please enter as MM/DD/YYYY.")

    # GM Nationality
    nationalities = [
        "American", "Canadian", "Mexican", "British", "German",
        "Australian", "French", "Italian", "Spanish", "Japanese", "Other"
    ]

    print("\nSelect your GM's nationality:")
    for i, nation in enumerate(nationalities, 1):
        print(f"{i}. {nation}")

    while True:
        nationality_choice = input("Enter the number of your nationality: ").strip()
        if nationality_choice.isdigit():
            nationality_index = int(nationality_choice) - 1
            if 0 <= nationality_index < len(nationalities):
                gm_nationality = nationalities[nationality_index]
                break
        print("Invalid selection. Please enter a valid number.")

    # --- Physical Appearance ---

    # Height
    while True:
        gm_height = input("\nEnter your height in inches (e.g., 72): ").strip()
        if gm_height.isdigit() and 48 <= int(gm_height) <= 90:
            gm_height = int(gm_height)
            break
        print("Invalid height. Please enter a number between 48 and 90 inches.")

    # Weight
    while True:
        gm_weight = input("Enter your weight in pounds (e.g., 210): ").strip()
        if gm_weight.isdigit() and 100 <= int(gm_weight) <= 400:
            gm_weight = int(gm_weight)
            break
        print("Invalid weight. Please enter a number between 100 and 400 pounds.")

    # Skin Tone
    skin_tones = ["Light", "Medium", "Dark"]

    print("\nSelect your skin tone:")
    for i, tone in enumerate(skin_tones, 1):
        print(f"{i}. {tone}")

    while True:
        tone_choice = input("Enter the number of your skin tone: ").strip()
        if tone_choice.isdigit():
            tone_index = int(tone_choice) - 1
            if 0 <= tone_index < len(skin_tones):
                gm_skin_tone = skin_tones[tone_index]
                break
        print("Invalid selection. Please enter a valid number.")

    # Hair Style
    hair_styles = ["Short", "Medium", "Long", "Bald"]

    print("\nSelect your hair style:")
    for i, style in enumerate(hair_styles, 1):
        print(f"{i}. {style}")

    while True:
        style_choice = input("Enter the number of your hair style: ").strip()
        if style_choice.isdigit():
            style_index = int(style_choice) - 1
            if 0 <= style_index < len(hair_styles):
                gm_hair_style = hair_styles[style_index]
                break
        print("Invalid selection. Please enter a valid number.")

    # Facial Hair
    facial_hair_options = ["None", "Mustache", "Beard"]

    print("\nSelect your facial hair style:")
    for i, facial in enumerate(facial_hair_options, 1):
        print(f"{i}. {facial}")

    while True:
        facial_choice = input("Enter the number of your facial hair style: ").strip()
        if facial_choice.isdigit():
            facial_index = int(facial_choice) - 1
            if 0 <= facial_index < len(facial_hair_options):
                gm_facial_hair = facial_hair_options[facial_index]
                break
        print("Invalid selection. Please enter a valid number.")

    # --- Build GM Profile ---
    gm_profile = {
        "name": gm_name,
        "dob": gm_dob,
        "nationality": gm_nationality,
        "height_in": gm_height,
        "weight_lbs": gm_weight,
        "skin_tone": gm_skin_tone,
        "hair_style": gm_hair_style,
        "facial_hair": gm_facial_hair
    }

    # --- Confirm GM Profile ---
    print("\n--- GM Profile Summary ---")
    print(f"Name: {gm_profile['name']}")
    print(f"Date of Birth: {gm_profile['dob']}")
    print(f"Nationality: {gm_profile['nationality']}")
    print(f"Height: {gm_profile['height_in']} inches")
    print(f"Weight: {gm_profile['weight_lbs']} lbs")
    print(f"Skin Tone: {gm_profile['skin_tone']}")
    print(f"Hair Style: {gm_profile['hair_style']}")
    print(f"Facial Hair: {gm_profile['facial_hair']}")

    input("\nPress Enter to continue...")

    return gm_profile

def validate_dob(dob_str):
    try:
        datetime.datetime.strptime(dob_str, "%m/%d/%Y")
        return True
    except ValueError:
        return False
