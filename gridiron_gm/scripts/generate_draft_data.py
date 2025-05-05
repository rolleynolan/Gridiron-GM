from gridiron_gm.engine.core.college_player_generator import (
    generate_initial_college_db,
    generate_draft_class
)
from gridiron_gm.engine.scouting.scout import Scout
import json
import os

# 1. Generate college database
college_db = generate_initial_college_db(num_per_class=450)

# 2. Generate draft class from seniors + early declares
draft_class = generate_draft_class(college_db)

# 3. Transform for GUI
scout = Scout(name="Alex Bright", role="College Director")
scouting_data = []

for player in draft_class:
    data = scout.build_structured_scouting_report(player)
    scouting_data.append(data)

# 4. Save to JSON
base_dir = os.path.dirname(os.path.abspath(__file__))
output_path = os.path.join(base_dir, "..", "gui", "draft_class.json")

with open(output_path, "w", encoding="utf-8") as f:
    json.dump(scouting_data, f, indent=2)

print("✅ Draft class exported to gui/draft_class.json")
