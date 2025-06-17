# Unity Frontend

This folder contains a simple Unity UI scaffold for Gridiron GM.

## Features

- Loads `league_state.json` from the project root.
- Displays the current week number and recent game results.
- Allows the user to create or select a GM profile.
- Lets the player choose a team from a dropdown list.
- Selecting a team now shows a scrollable roster with key player info.
- A `Simulate Week` button executes `python scripts/run_weekly_simulation.py` and refreshes the data.

## Usage

1. Open this folder as a Unity project or copy the `Assets` directory into your existing project.
2. Ensure that `league_state.json` exists at the root of the repository.
3. Create a Canvas, add the necessary UI elements, and attach `MainMenuController`.
4. Hook up the dropdowns, text fields and buttons in the inspector.
5. Add a ScrollView for the roster display and assign the `PlayerRow` prefab to `MainMenuController`.

This setup is intentionally lightweight and is meant to get the frontend running quickly.
