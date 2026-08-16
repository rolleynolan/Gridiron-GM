# Gridiron GM 1.0 Roadmap

This roadmap is the implementation plan for reaching the 1.0 target described in `BLUEPRINT.md`.
It starts from the current native Godot/C# baseline and breaks the remaining work into ordered,
detailed phases. Each phase should end with a usable, stable, save-safe game state before the next
phase begins.

Follow `AI_INSTRUCTIONS.md` for workflow and ownership boundaries while executing this roadmap.

## How to use this roadmap

- Treat `BLUEPRINT.md` as the product-design authority and this file as the execution plan.
- Build one complete vertical slice at a time.
- Finish core foundation gaps before adding feature depth.
- Prefer legal, fictional equivalents of pro-football structure, rules, and flow over placeholder formats.
- Do not add broad feature layers on top of unstable league, rules, save, or UI foundations.
- End every phase with focused tests, save/load verification, and a Godot smoke test.

## Current state as of August 16, 2026

The active runtime is now fully native Godot/C#. The retired Python, HTTP, and RPC runtime path is no longer part
of the playable game loop. The current implementation already supports:

- reusable GM profiles and franchise-start setup
- Standard versus Generated roster provenance metadata
- deterministic world seed handling
- native franchise save/load
- an in-process local backend implementation
- basic roster, depth chart, injuries, standings, results, and game simulation
- focused domain tests and successful command-line builds

The current baseline is still a narrow prototype and does **not** yet satisfy the blueprint's intended league model
or full franchise journey. Major gaps include:

- the active league structure is still a temporary eight-team scaffold rather than a fictional 32-team pro league
- schedule generation is not yet aligned to a full pro-football format
- standings and playoffs are not yet modeled around conferences, divisions, and full tiebreaker flow
- preseason, offseason, contracts, cap rules, draft, staff, AI front offices, and long-term history are incomplete
- many UI screens exist only as early slices rather than complete management surfaces

The remaining work is therefore: finish the foundation, expand it to the intended league shape, make every major
management loop usable, then add depth and polish.

## Phase 0. Preserve the native baseline

Purpose: keep the current C# runtime healthy while the larger rebuild continues.

- Keep `BLUEPRINT.md`, `ROADMAP.md`, and `AI_INSTRUCTIONS.md` as the only planning authorities.
- Keep Python code reference-only and out of runtime, tests, and shipping paths.
- Maintain successful local restore/build/test flow.
- Continue migrating any remaining legacy assumptions into explicit C# domain code.
- Keep save migration safe whenever schema changes are introduced.

**Complete when:** the native runtime remains the only source of truth and no planned work depends on legacy runtime paths.

## Phase 1. Finish the foundation of the playable season loop

Purpose: turn the current prototype into a stable, full-season management foundation consistent with the blueprint's
"League, calendar, simulation, and history" and "Roster, game-day, injuries, and development" sections.

### 1.1 League structure and data model

- Replace the temporary eight-team league with a fictional 32-team league.
- Add conference and division metadata for every team.
- Establish the canonical league settings model:
  regular-season length, preseason length, playoff field, bye structure, roster limits, phase progression, and tie rules.
- Keep all branding fictional and legally distinct while matching pro-football structure as closely as possible.

### 1.2 Calendar and phase model

- Replace the temporary week-only calendar with explicit phases:
  franchise setup, preseason, regular season, playoffs, offseason, free agency, draft, preseason handoff.
- Ensure phase restrictions are owned by rules, not UI.
- Add explicit season transition events such as `WeekAdvanced`, `RegularSeasonEnded`, `PlayoffRoundEnded`, and `SeasonEnded`.

### 1.3 Schedule generation

- Build a valid 32-team fictional pro-football schedule model.
- Support regular-season opponents, byes, and week assignment rules.
- Ensure schedule generation is deterministic where required by saved world state.
- Validate that schedule, results, standings, and playoff qualification always agree.

### 1.4 Game simulation and weekly loop

- Extend simulation inputs beyond team strength to include roster quality, depth chart, injuries, fatigue, and home field.
- Ensure weekly simulation updates standings, injuries, player stats, box scores, and inbox events coherently.
- Add explicit game logs and clearer post-game summary data for UI consumption.
- Keep simulation deterministic enough for save integrity and debugging.

### 1.5 Standings, tiebreakers, and playoffs

- Rebuild standings around divisions and conferences.
- Implement division standings, conference standings, playoff qualification, seeding, and bracket advancement.
- Add full tiebreaker flow appropriate to a fictional pro-football structure:
  head-to-head, division record, conference record, common opponents where applicable, strength-style fallback, point differential only if design-approved.
- Ensure preseason results never touch regular-season standings.
- Ensure playoff games never alter regular-season standings.

### 1.6 Team-management foundation

- Finish legal roster ownership states: active, injured reserve, practice squad, reserve pools if required by design.
- Enforce roster-size and game-day eligibility rules.
- Expand depth-chart validation, auto-repair, and availability handling.
- Add first-pass fatigue and workload handling that affects readiness without overcomplicating the system.
- Keep injury lifecycle, recovery, and availability explainable through UI.

### 1.7 Foundation UI pass

- Bring the dashboard, standings, schedule/results, roster, depth chart, injuries, and league views up to a stable usable baseline.
- Ensure blocked actions return clear explanations.
- Ensure the user can understand current phase, next required action, upcoming game, standings position, and roster issues at a glance.
- Remove prototype wording and formatting that reflects the temporary eight-team scaffold.

### 1.8 Save, migration, and validation hardening

- Version every save schema change.
- Add migration for existing prototype saves where reasonable.
- Validate league state on load:
  teams, schedule, results, standings, playoff bracket, roster ownership, injuries, and notifications.
- Add targeted tests for save-safe state transitions.

**Complete when:** a Standard-roster save and a Generated-roster save can run from preseason through playoffs in a 32-team fictional pro-football format, reload without state drift, and remain stable and understandable through the main management screens.

## Phase 2. Make the game fully usable across a complete franchise year

Purpose: finish the baseline franchise loop described in the blueprint so the user can play a complete year rather than only a season shell.

### 2.1 Season-end processing and history

- Finalize end-of-season standings, playoff archive, champion record, awards hooks, and season summaries.
- Record team seasons, playoff outcomes, championships, and major statistical leaders.
- Add persistent history browsing for recent season results.

### 2.2 Player lifecycle

- Add aging, progression, regression, breakouts, stagnation, and retirement rules.
- Ensure development occurs only at explicit lifecycle events.
- Add recovery effects, wear, and season-to-season durability outcomes.
- Record career stats and progression history.

### 2.3 Offseason phase skeleton

- Add a real offseason calendar with ordered phases.
- Support player retirements, reserve clean-up, futures handling if used, and season reset steps.
- Build the state transitions needed so a franchise can cleanly leave one season and enter the next.

### 2.4 History and record book foundation

- Add persistent team history, championship history, transaction history, and career summary data.
- Add record-book structures for season and career leaders where reasonable for 1.0.

**Complete when:** a franchise can finish a season, process season-end changes, enter the offseason, and start the next league year with valid state and visible history.

## Phase 3. Contracts, cap, and legal transactions

Purpose: deliver the blueprint's "Contracts, transactions, and free agency" system as a stable management layer.

### 3.1 Contract and payroll model

- Add contract structures with years, salary, guarantees, bonuses, and contract status.
- Add payroll, cap-space, and dead-money style accounting as defined for the fictional league.
- Ensure all cap math lives in the rules layer.

### 3.2 Transaction rules

- Validate signings, releases, waivers, injured-reserve moves, practice-squad moves, and trades.
- Enforce phase restrictions and ownership integrity.
- Record every approved transaction in durable history.

### 3.3 Free agency

- Build a free-agent pool and valid offer flow.
- Apply capped GM Negotiation and Player Management effects only where the blueprint allows.
- Add UI for offers, decisions, roster pressure, and cap impact.

### 3.4 Transaction UI and feedback

- Add cap and payroll views.
- Add transaction logs and legal-action explanations.
- Ensure the roster UI clearly shows who can be moved, why an action is blocked, and what consequences follow.

**Complete when:** all player movement is legal, persisted, understandable, and cannot create duplicate ownership or invalid cap state.

## Phase 4. Draft, scouting, and full offseason

Purpose: complete the offseason into a playable multi-system management experience.

### 4.1 Prospect pipeline

- Add prospects, draft classes, rankings, and hidden true ratings.
- Generate post-start players from each save's world state without mutating the Standard Roster definition.

### 4.2 Scouting

- Add estimated ratings, confidence, reports, traits, combine-style data, interviews, and limited-information discovery.
- Apply Scouting Judgment only as a capped information-quality modifier.

### 4.3 Draft system

- Add draft order, picks, user draft board, AI selections, rookie contracts, and draft history.
- Support tradeable picks once the transaction layer can validate them safely.

### 4.4 Post-draft roster construction

- Add undrafted free agents, rookie signings, roster cuts, and preseason handoff.
- Ensure the user can leave the draft and arrive at a legal, understandable roster state.

**Complete when:** a franchise can finish a season, scout, draft, sign players, set a legal roster, and enter the next preseason without manual intervention.

## Phase 5. AI front offices, staff, and management depth

Purpose: add the deeper management systems that make the league feel alive without violating the ownership rules in the blueprint.

### 5.1 AI team building

- Add team-needs evaluation, roster valuation, age curve awareness, cap reasoning, pick valuation, and strategic behavior.
- Ensure AI actions are inspectable and rules-compliant.
- Prevent AI from bypassing user-control boundaries.

### 5.2 Trades

- Add AI trade proposals and user-initiated trades.
- Validate all player and pick exchanges through the transaction rules layer.
- Surface rationale and fairness clearly enough for the user to evaluate offers.

### 5.3 Staff and organizational effects

- Add staff roles and influence areas aligned to the blueprint:
  scouting, development, injuries, and strategy.
- Apply staff effects as bounded modifiers, not deterministic overrides.

### 5.4 Preseason and camp depth

- Expand training camp, preseason evaluation, position battles, roster cuts, and call-up decisions.
- Connect depth-chart competition, development, and roster decisions into a coherent preseason experience.

### 5.5 Immersion systems

- Add morale, chemistry, fan pressure, media context, and traits in a controlled way.
- Keep these as understandable contextual systems, not dominant hidden math.

**Complete when:** CPU teams build plausible legal rosters, make understandable decisions, and create a believable league environment around the user.

## Phase 6. Presentation, balance, and 1.0 polish

Purpose: finish the game to the blueprint's 1.0 completion standard.

### 6.1 Interface completeness

- Fill remaining major UI gaps:
  finance, staff, free agency, scouting, draft, history, transactions, settings, and onboarding.
- Improve layout, readability, error handling, and controller flow.
- Make sure every major game system is visible and actionable without developer knowledge.

### 6.2 Balance and long-run simulation

- Run multi-season simulation passes to tune game outcomes, development curves, injury frequency, contracts, AI logic, and roster churn.
- Fix degenerate league states, exploit loops, and economic instability.
- Add focused simulation regression tests where practical.

### 6.3 Accessibility and quality-of-life

- Improve onboarding, terminology clarity, summaries, notifications, and help text.
- Add accessibility and usability improvements appropriate to the shipped Godot interface.
- Ensure the game is understandable even to players who are not reading design docs.

### 6.4 Shipping hardening

- Finalize migration handling for supported saves.
- Remove dead prototype code and stale assets.
- Perform full save/load, long-run, and smoke-test passes on the intended runtime.

**Complete when:** the 1.0 completion standard in `BLUEPRINT.md` is met and the game is stable, understandable, and enjoyable across a full multi-season franchise loop.

## Rules for every phase

- Fix broken core behavior before adding a new feature layer.
- Update existing appropriate classes before adding new files or abstractions.
- Keep domain ownership clear: simulation owns games and standings, rules own legality, persistence owns saves, UI only presents and requests actions.
- Prefer a narrow finished slice over a broad half-implemented system.
- Add or update tests whenever rules, simulation, save behavior, or deterministic generation changes.
- Never claim a phase is complete unless the affected build, tests, and smoke checks actually passed.
