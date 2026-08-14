# Gridiron GM 1.0 Roadmap

Build one playable C# vertical slice at a time. Do not start a later layer until the current slice is saved, loaded, tested, and usable through Godot.

## 0. Reset and audit

- Make this blueprint, roadmap, and AI manual the only planning authorities.
- Mark Python as reference-only; do not ship, extend, or invoke it.
- Inventory Godot scenes, C# scripts, data, assets, and legacy dependencies.
- Establish C# folders/namespaces for domain, systems, persistence, UI, and tests.

**Complete when:** the active runtime and reference material are unambiguous.

### Audit baseline — August 2026

- Planning authorities are now consolidated at the repository root: `BLUEPRINT.md`, `ROADMAP.md`, and `AI_INSTRUCTIONS.md`.
- `gridiron_gm_pkg` is retained as read-only behavioral reference. It is not part of the runtime, build, or test path.
- The Godot project has one main dashboard scene and a substantial C# `GameCore` layer for league bootstrap, schedules, game resolution, standings, playoffs, rosters, depth charts, saves, season history, retirements, and smoke-test coverage.
- The dashboard now starts directly in the C# runtime. The retired Python backend setting, backend process manager, HTTP client, and RPC client have been removed.
- The remaining dashboard fallback branches are unreachable and return a local error if called. Remove those dead branches incrementally while keeping the working C# screens intact; never restore an external backend.
- A project-level `NuGet.Config` clears an obsolete workstation fallback-package path. `dotnet build` now restores and compiles the Godot project successfully.
- `Godot/Godot` is a duplicate directory created during consolidation. It is excluded from the build and Git, but should be deleted locally once no Godot editor instance is using it.
- The next active implementation slice is to complete native dashboard coverage for every remaining screen/action, then delete the unreachable fallback branches as each native screen is verified.

## 1. C# playable season loop

- Implement league, team, player, schedule, calendar, result, and save models.
- Rebuild schedule generation, game simulation, standings, tiebreakers, playoffs, and save/load in C#.
- Add roster, depth chart, fatigue, availability, and first-pass injuries.
- Build Godot dashboard, team selection, schedule/results, standings, roster, depth chart, and sim controls.

**Complete when:** a save can run from preseason through playoffs and reload without state drift.

## 2. Multi-season continuity

- Add season-end statistics/history, aging, progression/regression, retirements, and offseason phases.
- Add record book, careers, championship history, and transaction history.

**Complete when:** three automated seasons maintain valid ages, history, standings, and saves.

## 3. Rules, contracts, and transactions

- Add contract expiry, payroll, cap space, cap validation, releases, waivers, IR, practice squad, and transaction log.
- Add free-agent offers/signings and Godot cap, transactions, and free-agent screens.

**Complete when:** all player movement is legal, persisted, explained, and cannot create duplicate ownership.

## 4. Draft and complete offseason

- Add prospects, draft order, picks, selections, rookies, undrafted players, roster cuts, and preseason handoff.
- Add basic scouting ranges, reports, combine/interview data, and a user draft board.

**Complete when:** a franchise can finish a season, draft, sign players, set a legal roster, and start the next year.

## 5. AI front offices and management depth

- Add team needs, valuation, strategy, GM personalities, AI draft/free-agency decisions, and validated trades.
- Add staff, training camp, position battles, call-ups, and deeper injury management.

**Complete when:** CPU teams build plausible legal rosters with inspectable rationale.

## 6. Balance and 1.0 polish

- Add morale, chemistry, traits, fan/media context, awards, Hall of Fame, historical browsing, onboarding, settings, accessibility, and migration polish.
- Balance simulation, development, contracts, AI, and offseason outcomes through long-run simulation.

**Complete when:** the 1.0 standard in `BLUEPRINT.md` is met and critical automated tests plus Godot playthroughs pass.

## Rules for every phase

- Fix broken core behavior before adding a feature layer.
- Prefer a narrow working version over a broad incomplete system.
- End each phase with focused tests, save/load verification, and a Godot smoke test.
