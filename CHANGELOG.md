# Changelog

All notable project changes are recorded here.

## Unreleased

### Changed

- Consolidated the active game, reference implementation, data assets, saves, and prior porting notes into this master folder.
- Established `Godot/` as the active C# game location.
- Established the root blueprint, roadmap, and AI instructions as the only active project-planning documents.
- Added the Godot seed asset pack: 32 team logos, seeded league data, the included font, and its readme.
- Removed the retired Python backend startup path, HTTP client, RPC client, and backend setting from the Godot runtime.
- Added a project-level NuGet configuration so the Godot/C# project builds without the obsolete Visual Studio fallback-package path.

### Notes

- The legacy Python material is retained only as reference while the game is rebuilt as one Godot/C# application.
