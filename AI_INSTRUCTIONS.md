# Gridiron GM AI Instructions

Read this manual before changing the project.

## Authority

Follow, in order: the user's current request, this file, `ROADMAP.md`, `BLUEPRINT.md`, then active C# code and tests.

## Active architecture

- Godot/C# is the only active runtime and source of truth.
- The game is one application. Do not create or rely on Python processes, HTTP/RPC servers, or a client/server split.
- Python folders are historical reference only. They may be read to recover intent or port behavior, but must not be modified, run, tested, or shipped unless the user explicitly changes this policy.
- Rebuild legacy behavior cleanly in C#; do not mechanically port legacy architecture.

## Required workflow

1. Read the relevant blueprint and roadmap sections.
2. Inspect active Godot/C# code before editing.
3. State the smallest complete outcome.
4. Implement only needed C#, Godot scenes/assets, and tests.
5. Validate with build, focused tests, affected integration tests, and Godot smoke check where appropriate.
6. Remove task-created temp files, stale imports, abandoned code, and generated test artifacts.
7. Report outcome, exact paths, validation, and remaining limits.

## Ownership boundaries

- Simulation owns schedules, game resolution, results, standings, and playoffs.
- Rules own cap, contracts, roster limits, eligibility, phase restrictions, and validation.
- Transactions execute only rule-approved actions and record every mutation.
- Development changes ratings only at explicit lifecycle events.
- AI front offices evaluate and propose; they never bypass rules or directly alter unrelated state.
- Persistence owns save versions, serialization, migration, and restore validation.
- UI displays state and requests actions; it never duplicates rules or directly edits domain state.

Use typed events or explicit service calls for cross-system work. Avoid hidden static state, circular dependencies, and UI-driven game logic.

## Data and save rules

- Validate every state-changing action before commit.
- A player belongs to one roster/pool only.
- Schedule, results, standings, and playoffs must agree.
- Preseason never changes regular-season standings.
- Save data is versioned, JSON-safe, deterministic where needed, and migrated safely.

## Implementation rules

- Prefer focused classes with one durable responsibility.
- Update an existing appropriate class before creating another file.
- Do not add placeholders, duplicate helpers, temporary architecture, or unused abstractions.
- Keep UI controllers thin, defensive, and readable.
- Do not delete user assets, saves, or configuration without explicit authorization.

## Documentation rules

Only `BLUEPRINT.md`, `ROADMAP.md`, and this file are project-planning authorities. Update the blueprint for 1.0 design changes, roadmap for sequencing/progress, and this file for durable working rules. Do not create parallel agent manuals, roadmaps, blueprints, or subsystem design docs without an explicit request.

## Communication

Lead with the outcome, stay concise, and state material assumptions. Do not claim a build or test suite passes unless it completed successfully.
