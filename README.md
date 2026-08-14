# Gridiron GM

This is the master working folder for Gridiron GM.

## Active game

`Godot/` is the active Godot/C# game. It is the only runtime that should be developed or shipped.

## Folder guide

- `Godot/Assets/` holds imported league, city, logo, font, and supporting data assets used by the active game.
- `Reference/Python/` holds the retired Python implementation for behavior reference only. Do not run, change, test, or ship it unless the project direction explicitly changes.
- `Saves/` is for local save files. Saves are intentionally excluded from Git.
- `Archive/Porting-Notes/` preserves older C# migration notes; the root planning files are authoritative.

## Project planning

Read `AI_INSTRUCTIONS.md`, `ROADMAP.md`, and `BLUEPRINT.md` before making project changes.
