# Gridiron GM - Active TODOs

## 🔧 High Priority

### 1. Rookie Scouting Fog of War
- File(s): `scouting/scout_engine.py`, `players/player.py`
- Requires:
  - Team-specific scout accuracy bias
  - Attribute masking for POT and OVR
- Prompt: "Implement fog-of-war system that masks rookie OVR/POT based on team scouting bias"

### 2. Player Development Engine
- File(s): `development/dev_engine.py`, `players/player.py`
- Requires:
  - Attribute growth based on performance (e.g. 2000yd RB improves a lot)
  - Decline with age or poor play
- Prompt: "Create performance-based development engine for players that influences attributes gradually"

### 3. Add Free Agency Logic
- File(s): `offseason/free_agency.py`, `players/player_pool.py`
- Requires:
  - Contract evaluation
  - Team bidding logic
- Prompt: "Build a basic free agency signing system where teams bid on available players"

