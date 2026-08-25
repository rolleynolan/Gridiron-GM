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
- The accidental `Godot/Godot` duplicate created during consolidation has been removed locally; the project now has one active Godot source tree.
- The native automated smoke run now validates a complete first season flow: league bootstrap, dashboard, roster/depth chart, preseason, regular season, playoffs, season transition, retirements, history, save/load, and cleanup.
- New franchises load the packaged 32-team seed data and present the included team logos during team selection.
- The roster, depth chart, standings, results, schedule, and injury-report views now call GameCore directly; remaining dead fallback code is limited to dashboard actions and legacy parsing helpers.
- The contract and transaction foundation now validates cap space, active-roster capacity, player-pool ownership, and postseason lockout before committing releases, extensions, free-agent signings, or expirations. Every committed mutation is persisted in the native transaction log.
- The offseason now processes expiring contracts, retirements, and opens a playable free-agency window before draft preparation. Franchise tags, the draft lifecycle, rookie signing, and training camp are playable; staff changes remain incomplete.
- The draft now runs seven rounds in strict pick order, advances CPU teams to each user selection, converts selected prospects into rostered rookies with contracts, and records every selection. Prospect evaluations provide persisted confidence, estimated ranges, traits, interviews, and public combine/pro-day results without treating hidden ratings as fact. Completed draft classes archive immutable pick, evaluation, placement, and rookie-contract recaps for season history. Existing three-round draft saves are extended safely when opened.
- Training camp now trims CPU teams to the 53-player active limit, blocks the user from rollover until their roster is legal, and then starts a new preseason with player aging, fresh standings/results/bracket/schedule, archived draft data, undrafted free agents, and a new prospect class.
- Waivers, injured reserve, and practice squad pools now preserve exclusive player ownership, cap accounting, save/load state, and transaction history. Roster actions are phase-gated, waiver expiration advances players to free agency, and native roster controls expose waiver and IR moves.
- Player box scores now include deterministic offensive and defensive stat lines. Regular-season totals persist per player and archive into career history during rollover. Game contributors gain bounded fatigue, players recover as days advance, fatigue reduces effective team strength, and rollover clears it. Potential-driven annual development/regression is applied at the same rollover point.
- Player reports now expose read-only current-season live totals separately from archived career seasons, including legacy-safe empty history handling and save/load continuity.
- Injuries are now deterministic game events with typed recovery timing and player history. Unavailable players are excluded from game lines, depth charts promote available backups, daily simulation recovers players, save migration preserves legacy injury labels, and rollover clears active injuries.
- Waiver claims, practice-squad signing/elevation, and transaction-history browsing are now playable through the Godot dashboard. UI actions call only native transaction services, display rule failures, autosave successful mutations, and refresh persisted roster state.
- Contract actions now use centralized phase rules with explainable availability: exclusive negotiation opens extensions, free agency opens extensions and signings, and in-season/training-camp signings remain validated while extensions are closed. Training camp now requires a one-time position focus and a finalized legal roster before rollover; both effects and decisions persist in the transaction history.
- Training camp now generates a saved, deterministic roster report for each position group. It combines depth requirements, player availability, potential, fatigue, and the selected camp focus into a clear focus recommendation and readiness notes; reports refresh after focus and reset at rollover.
- Training camp now resolves credible contested position groups deterministically from ratings, potential, fatigue, availability, and camp focus. Outcomes explain the winner, persist with camp state, and never overwrite position groups explicitly adjusted by the user.
- Preseason roster evaluation now provides deterministic player-role feedback derived from authoritative roster and depth state. It explains starter/backup/unavailable roles, readiness from fatigue and injuries, depth standing, and position-battle results without changing user decisions.
- Roster-evaluation presentation polish is complete: selecting a player in the normal roster workflow reveals the existing role, readiness, depth, injury, and battle feedback without changing domain decisions.
- Franchise-tag contract decisions are complete: one eligible final-year active-roster player per team can receive a deterministic, fully guaranteed one-year tag during the dedicated phase. Cap and roster validation, transaction history, save migration, expiry, and roster UI feedback are covered.
- Draft prospect evaluation depth is complete: the domain persists deterministic confidence, estimated ranges, reports, traits, interview notes, and public combine/pro-day data, while the Godot draft board clearly separates known facts from team estimates.
- Draft-class recap and history presentation is complete: completed drafts retain immutable pick context, pre-draft evaluation snapshots, and initial rookie placement/contract data through rollover and save/load, surfaced in the Godot season-history view.
- Player career and season-stat history presentation is complete: the roster workflow clearly separates authoritative live totals from immutable archived seasons, with multi-season, rollover, and missing-data safety coverage.
- The next active implementation slice is trade-asset foundations and a single user trade-proposal workflow: persist tradable pick ownership, validate player/pick packages through cap, roster, ownership, and phase rules, record accepted transactions, and provide deterministic counterparty rationale in Godot. Keep this bounded; do not broaden into generalized AI front-office behavior, staff, or schemes.

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

**Current state:** player statistics, careers, season history, retirements, draft archives, and save migration work through the covered first rollover. The required three-automated-season stability validation, record book, awards, and broader historical browsing remain.

## 3. Rules, contracts, and transactions

- Add contract expiry, payroll, cap space, cap validation, releases, waivers, IR, practice squad, and transaction log.
- Add free-agent offers/signings and Godot cap, transactions, and free-agent screens.

**Complete when:** all player movement is legal, persisted, explained, and cannot create duplicate ownership.

**Current state:** cap-aware signings, releases, extensions, franchise tags, expirations, roster-capacity validation, explainable contract-phase rules, and persisted transaction records are implemented. Waivers, IR, practice squad, phase restrictions, duplicate-pool repair, and a transactions screen are implemented. Richer contract mechanics remain.

## 4. Draft and complete offseason

- Add prospects, draft order, picks, selections, rookies, undrafted players, roster cuts, and preseason handoff.
- Add basic scouting ranges, reports, combine/interview data, and a user draft board.

**Complete when:** a franchise can finish a season, draft, sign players, set a legal roster, and start the next year.

**Current state:** seven-round order, user picks, CPU selections, persisted prospect scouting evaluations, immutable completed-draft recaps, rookie roster placement, rookie contracts, draft transactions, offseason cuts, undrafted free agents, and a saved new-season handoff are implemented. Training camp has a persisted position-focus decision, deterministic roster reports, protected position-battle outcomes, and roster player reports now include role/readiness plus live and archived statistics. Further preseason presentation work should be scoped from workflow testing.

## 5. AI front offices and management depth

- Add team needs, valuation, strategy, GM personalities, AI draft/free-agency decisions, and validated trades.
- Add staff effects, player traits/chemistry, remaining call-up decisions, and deeper injury management.

**Complete when:** CPU teams build plausible legal rosters with inspectable rationale.

**Current state:** active roster, IR, practice squad, and waiver pools are distinct and persisted. Rules validate capacity, injury eligibility, active-roster activation, basic practice-squad eligibility, cap accounting, and several locked offseason phases. Game injuries have deterministic occurrence, recovery timing, availability-aware substitution, history, save migration, and rollover cleanup. Waiver claims, practice-squad signing/elevation, and transaction history have Godot controls backed by the same validated services; AI roster management remains.

## 6. Balance and 1.0 polish

- Add morale, chemistry, traits, fan/media context, awards, Hall of Fame, historical browsing, onboarding, settings, accessibility, and migration polish.
- Balance simulation, development, contracts, AI, and offseason outcomes through long-run simulation.

**Complete when:** the 1.0 standard in `BLUEPRINT.md` is met and critical automated tests plus Godot playthroughs pass.

## Rules for every phase

- Fix broken core behavior before adding a feature layer.
- Prefer a narrow working version over a broad incomplete system.
- End each phase with focused tests, save/load verification, and a Godot smoke test.
