# Gridiron GM

Gridiron GM is a single-player American-football franchise simulation.

## Active runtime

`Godot/` is the active game and the only runtime that should be developed, built, tested, or shipped.

## Project layout

- `Godot/` contains the native Godot/C# runtime, simulation, save flow, and tests.
- `gridiron_gm/`, `gridiron_gm_pkg/`, `scripts/`, and `tests/` are legacy Python-era reference material retained only where still useful for behavior lookup during the native rewrite.
- `BLUEPRINT.md`, `ROADMAP.md`, and `AI_INSTRUCTIONS.md` are the planning and execution authorities for the project.

## Validation

Current native validation commands:

```powershell
dotnet build 'Godot/Gridiron GM.csproj'
dotnet test 'Godot/Tests/GridironGM.Domain.Tests.csproj'
```
