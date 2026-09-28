# Gridiron GM 1.0 Roadmap

Build one playable C# vertical slice at a time. Do not start a later layer until the current slice is saved, loaded, tested, and usable through Godot.

## Player-development workshop — partial design approval, updated September 27, 2026

Current task is design resolution only. Do not start the new runtime development system until the remaining affected rules
are reviewed and the user explicitly authorizes implementation. Save version remains **38**; the existing runtime checkpoint
is `8565f95`, with **190 tests reported passing** in that checkpoint. This documentation update does not rerun or change them.

- [x] Audit the current player, annual/camp development, statistics, roster pools, medical recovery, staff, scouting,
  retirement, college handoff, and persistence paths.
- [x] Record three persistent DNA trajectories: physical, technical, and mental, with greater typical technical retention,
  individual aging rather than fixed positional decline birthdays, and distribution-based population variation.
- [x] Record that every player possesses the full skill catalog, with position-appropriate starting abilities. The expanded
  September 27 catalog has 47 ratings (10 physical, 7 mental, 30 technical); Overall weights remain provisional.
- [x] Record three fixed daily training slots, one slot per selection at any intensity, no staff-created slots, and no
  selectable sessions on the day before, of, or after a game. Adjacent away-game days are Travel; home-game days are Rest;
  Travel counts as rest. Game participation retains its own workload and experience.
- [x] Record Tuesday observation reports, retirement persuasion requiring a credible reason, and the direction that retired
  players can become coaches, including possible future coaching-role promises.
- [x] Record the revised passing, carrying, receiving, blocking, pass-rush, coverage and specialist skills; remove duplicate
  generic Evasion/Contact Running/Field Vision and trainable Holding. Separate release duration, throw velocity and range;
  Mechanics governs throwing consistency. Juke/Spin use Agility; Trucking uses Strength/Contact Balance; body dimensions
  are actual measurements with distinct, bounded simulation effects. Rushing/return vision belong to mental DNA.
- [x] Record generated persistent career curves, with a majority of standard rise/peak/decline shapes and rarer departures,
  plus catalog data requirements, interaction pseudocode, invariants and acceptance checks in BLUEPRINT.
- [ ] Finalize Overall weights and explain/approve XP conversion and Potential's influence.
- [ ] Finalize trajectory generation, aging parameters, correlations, and population/acceptance distributions.
- [ ] Review the named training/rest/recovery catalog, intensity/workload limits, practice opportunity, and readiness effects.
- [ ] Complete staff/modifier and observation details; define retirement decisions and persuasion/coaching commitment rules.
- [ ] Approve migration and event continuity details, balance targets, and the final implementation decision package.
- [ ] Consolidate approved documentation and deliver the complete implementation handoff before runtime authorization.

The eventual development macro-slice should sequence shared skills/DNA and additive migration, authoritative activity/time
processing across all ownership pools, college/pro identity continuity, legal schedules, and persisted observations on
existing surfaces. Replace the provisional annual and camp rating bonuses without stacking old and new development.
Validate deterministic retries/reloads, migration preservation, pool movement, staff authority, and long-run career/talent
distributions. Retirement persuasion and player-to-coach commitments require their own completed rules before implementation;
their approval in principle does not authorize invented contracts, promises, or medical outcomes.

Unaccepted workshop numbers remain proposals: no XP rates, growth-reference formula, fixed age cutoffs, weighted skill table,
weekly activity caps, training injury probabilities, or persuasion probabilities were adopted by this checkpoint. The earlier
proposal that heavy sessions consume extra time slots is superseded. No runtime, saves, UI, assets, or pre-existing `.uid`
sidecars change. Next continuation: clarify and approve Potential/learning, then specify Overall weights, numeric interaction
rules and migration. The hidden per-skill growth-reference recommendation and associated numeric examples remain proposals.

The requested Sol Medium runtime handoff is not yet unblocked by catalog approval alone. Once the remaining affected rules
and runtime work are authorized, the first bounded slice should establish the authoritative catalog/skill model, deterministic
generation and approved save migration, derived Overall and dated observed profile values, routing every existing pro/college/
camp rating writer through the same authority. Preserve the current snap resolver through a derived-Overall adapter initially;
action-specific use of the new skills and body measurements requires separately specified coefficients and validation. Do not
claim that storing a rating already makes the snap engine use it, or leave legacy direct Overall writes as a second authority.
The full implementation-ready formulas, migration algorithm, direct tests and acceptance thresholds must be completed before
handing that slice to Sol Medium. No runtime implementation is authorized by this documentation/push request.

## Rolling save recovery and play-log compaction — completed September 25, 2026

Smallest complete outcome: every successful save keeps a bounded recoverable history, damaged or unavailable primaries are reported rather than silently replaced, recovery is an explicit user action, and detailed pro play logs take materially less disk space without changing simulation state or breaking older saves.

- [x] Retain three ordered predecessors for autosaves, named saves, and test/diagnostic saves through the atomic replacement boundary.
- [x] Detect missing/corrupt primaries, validate backups independently, and explicitly restore the latest backup while preserving a damaged primary.
- [x] Expose backup counts in Franchise Settings and a recovery action on the startup error surface.
- [x] Compact event fields, boolean flags, participants, and sparse stat deltas while reading the verbose save-33–37 schema.
- [x] Add rolling-order, corrupt/missing recovery, deletion, compaction, round-trip, and legacy compatibility tests.
- [x] Verify the production build, full suite, GameCore smoke, Game Day UI smoke, editor import, startup, benchmark, and diff checks.

`GameCoreSaveService` now rotates at most three sibling backups immediately before atomic replacement. A failed new write still leaves the current primary untouched. Loading never silently falls back: its result distinguishes missing/corrupt state and reports available recovery copies. Explicit recovery validates the selected backup first, atomically installs it, and archives an existing damaged primary as a `.corrupt-*` file. Explicit career/save deletion removes its primary, backups, and preserved corrupt copies. The current UI offers the latest valid recovery copy and displays backup counts; lower-level APIs retain indexed access to all three.

Save **38** uses a compact JSON converter only for `GamePlayEventState`. It combines five booleans into flags, omits empty/default event fields, shortens repeated event keys, and compactly writes sparse per-play stat changes. Domain models, live RNG/cursor state, completed results, and simulation rules are unchanged. The converter reads both compact events and the prior verbose representation, so versions 33–37 retain exact play history on load. In a controlled 200-event save fixture, the compact file is **62,572 bytes versus 166,772 bytes (37.5%)** for the equivalent default verbose JSON.

Validation: **190 tests pass**, including five new recovery/compaction cases. Production build completes with zero warnings/errors; full three-season GameCore smoke and the actual Game Day control smoke pass. Godot editor import/startup and `git diff --check` pass. The unchanged resolver workload completes 272 pro games in **866.8 ms / 445.43 MiB cumulative allocation** and 783 lightweight college games in **113.6 ms / 8.84 MiB**; serialization remains outside those timed samples. Backup storage is intentionally additional disk usage bounded to three prior files per logical save; this slice does not add cloud sync, cross-machine export, automatic recovery, or the full multi-career library.

## Medical reporting and clearance context — completed September 25, 2026

Smallest complete outcome: existing injury and player-profile surfaces use one authoritative, read-only medical projection for diagnosis, staff-selected treatment, dated estimated clearance, current restrictions, and IR activation context. Reporting must not advance recovery or promise an exact return date.

- [x] Share the persisted medical-staff recovery rate between treatment progression and report projection.
- [x] Replace exact UI countdowns and blank return fields with dated, explicitly non-guaranteed clearance windows.
- [x] Distinguish medical clearance, game availability, performance readiness, and post-clearance IR activation.
- [x] Preserve incomplete legacy injuries without inventing diagnosis timing or a return date.
- [x] Add direct projection/idempotence tests and verify build, full tests, smoke, editor import, and startup.

`PlayerMedicalReportService` derives reports only from the saved calendar, current injury episode, ownership roster, and existing Medical Director rate. Opening or repeating a report cannot mutate league state. Active diagnosed injuries show a conservative date range around the current deterministic recovery outlook and label it as an estimate separate from performance readiness. The treatment is informational and staff-selected; no treatment-choice mechanic was added. Cleared IR players remain explicitly ineligible until the existing activation transaction succeeds. Legacy injuries without a valid current timetable display a pending medical evaluation instead of fabricated precision.

The user Team > Injuries workspace, general league injury table, and Player Profile history now consume that projection. The league table includes both active-roster injuries and IR players, with report date and estimated-clearance columns; the player profile keeps dated injury history below the current report. No save migration is required because the projection is deterministic from save-37 state.

Validation: **185 tests pass**, including four new cases for non-mutating dated reports, shared staff-rate behavior, cleared-IR activation context, and incomplete legacy records. Production build completes with zero warnings/errors; the full three-season GameCore smoke, Godot editor import, project startup, and `git diff --check` pass.

Remaining medical scope is unchanged where design or deeper mechanics are still needed: delayed diagnosis workflows, setbacks, limited clearance, permanent damage, recovery rust, snap limits, and a broader injury/treatment catalog are not implemented by this reporting slice.

## Calendar-driven medical continuity — completed September 25, 2026

Smallest complete outcome: injury recovery and ordinary fatigue recovery follow actual elapsed dates through the season, playoff rounds, offseason transitions, and rollover. A new season does not heal a long injury. Save/retry/pool movement must not duplicate recovery or overwrite history.

- [x] Inspect health/calendar/roster/save boundaries and approved medical direction.
- [x] Integrate dated recovery, playoff round spacing, offseason/rollover handoff, and additive save 37 migration.
- [x] Direct continuity, recovery-date, reserve/market, save/retry, and calendar integration tests.
- [x] Build, focused suite, full smoke, long-run diagnostic, and relevant performance verification.
- [x] Documentation, limitations, and focused commits.

Sequencing correction: the development continuation below cannot yet be implemented as a complete approved slice. BLUEPRINT's **Player DNA and career development → Audit status and implementation acceptance** explicitly requires numeric conversion, the training catalog, phase generation, skill-group effects, and trajectory distributions to be finalized before implementation; the catalog also awaits user review. This run selects medical calendar continuity instead of silently inventing those choices. Existing medical rates/catalog remain a bounded foundation; no new treatment or training choices are introduced.

Implemented behavior:
- `ScheduleService.AdvanceCalendarDate` is the common production boundary for elapsed-date recovery. Daily Continue, offseason phase transitions, playoff round simulation, and the preseason handoff use it. Same/older dates do not award recovery again. Ordinary fatigue recovery also covers free-agent and waiver pools, and time jumps apply the existing bounded daily rates without an unbounded day-by-day loop.
- Each injury persists `RecoveryProcessedThrough`. Recovery uses actual elapsed days and the current owner's existing medical-staff rate; basic recovery remains available without a team or medical director. Calendar movement settles recovery before subsequent transactions/staff changes. Signings/releases preserve the injury checkpoint. Treatment progression never moves a user player out of IR or practice status automatically.
- Recovery history records the actual completion date within a skipped interval, matching the injury's occurrence date, name, and game identity. An unrelated episode with the same empty game ID is not incorrectly completed. No treatment advances merely from opening a report, retrying a checkpoint, or loading a save.
- Removed unconditional injury/fatigue clearing at rollover. Short injuries can recover during the actual offseason interval; a long injury remains active in the new season with the remaining time intact. Normal CPU reserve/emergency services still handle its roster consequences, and user IR activation remains explicit.
- Playoff simulation previously left all rounds on the same calendar date. A persisted bracket date anchor now spaces rounds using the existing four `PhaseWeek` values (seven days apart), so rest and rehabilitation occur before the next round. Replays and partial-round retries retain the same target date. Older undated brackets anchor from the last played round/current date on their next simulation action, without moving the calendar on load.
- Additive save **37** retains injury checkpoints and playoff anchors. Legacy active injuries start at the loaded date with unchanged remaining days and history, avoiding retroactive healing. No pro resolver/event-generation changes or college injury-rule changes were made.

Validation passes **181 tests**, including twelve new cases for all five ownership pools, repeat/backward date handling, actual recovery dates, changing medical support after signing/release, episode matching, native save 36/37 continuity, offseason Continue fatigue/health, long-injury rollover and explicit user IR activation, and playoff timing/retry/reload. Production build has zero warnings/errors; full three-season GameCore smoke, editor import/startup, and `git diff --check` pass. Existing injury/history projections receive authoritative state without a presentation change.

The diagnostic now reports active injuries and accepts `--cpu-roster-injury-stress`. This optional developer-only scenario applies one 540-day recovery to an available CPU quarterback during each camp through the existing injury service, then fails if rollover erases it. It does not alter production injury frequency or write a user save. Command: Godot `--headless --path Godot -- --cpu-roster-diagnostic --cpu-roster-seasons=15 --cpu-roster-injury-stress`.

**15 stress seasons completed (2026–2040, entering 2041) in 92.3 seconds**, with annual disk reloads. All measured structural checks and Week 1/rollover starter checks passed, and every rollover retained the injected long recovery. The final state has one active injury, 5,010 free agents, 852 unsigned retirements in the completed year, 2,086 players observed unsigned for three or more years, and 48.1 recorded transactions per CPU club. Late year-end market counts were 5,015, 4,990, 4,946, and 5,010. The ordinary three-season diagnostic also passes in the full test suite. These are standard-world observations, not a universal injury/market balance guarantee.

Isolated resolver benchmark: one game **3.416 ms / 1.80 MiB**, 16-game week **55.4 ms / 26.54 MiB**, 272-game season **829.0 ms / 445.54 MiB**, and 783 college games **105.1 ms / 8.84 MiB** cumulative allocations. Resolver timing is effectively unchanged from the preceding checkpoint; the 92.3-second endurance measurement includes calendar recovery, CPU work, and disk reloads. Implementation/test/diagnostic commit: `237ffa3`. The twelve pre-existing untracked sidecars are preserved.

Remaining scope: this preserves the existing short-injury catalog and medical/conditioning rates; it does not yet implement delayed diagnosis, setbacks, limited medical clearance, permanent damage, rust, or snap limits. The offseason still advances through its existing coarse phase dates; this is not a full calendar/waiver-deadline redesign. Dated diagnosis/recovery reporting and clearance context are completed in the next checkpoint above. Activity-based development remains deferred until its explicitly unreviewed choices are resolved.

## Unsigned-player career continuity — completed September 25, 2026

Smallest complete outcome: the existing annual retirement lifecycle includes unsigned players, preserves their identity/history, and limits sustained market growth through deterministic career attrition. Keep draft declarations and every eligible UDFA available; measure long-run roster health before changing pipeline intake.

- [x] Inspect retirement, player development, market entry, transaction acquisition, and history/save boundaries.
- [x] Integrate unsigned tenure, value/age-aware retirement, immutable player snapshots, and authoritative retirement transactions.
- [x] Add save 36 migration without retroactive retirement or replay; preserve saved depth order.
- [x] Direct determinism, history, acquisition-reset, migration, and replay tests.
- [x] Extended normal-calendar roster/market diagnostic and required build/smoke validation.
- [x] Record measured balance limits, next continuation, and focused commits.

Implemented in `47b39b4`:
- The annual retirement pass previously visited only active rosters, leaving unsigned players in the market indefinitely. It now assesses uniquely owned, uncontracted free agents as well. Pending waivers and contracted reserves do not enter the unsigned assessment. Existing team-player retirement eligibility and starter-count protections remain.
- `UnsignedSinceSeasonYear` records the first observed annual assessment of the current spell without a team. Successful active or practice-squad acquisition resets it; declined offers do not. Young entrants receive two observed cycles before market-related attrition. Subsequent chances rise with time unsigned, using the shared CPU evaluator's bounded future value to retain stronger opportunities longer. Veteran age-related retirement still applies. Rates are deterministic tuning constants, not a fixed pool-size cap or a new visible player rating.
- `RetirementService` evaluates and snapshots; `TransactionService` removes retired identities from incompatible ownership pools and invitations, refreshes cap room, removes only their depth entries, and records one concise retirement transaction. This also fixes the previous retirement pass's unconditional reordering of every team's saved depth, including the user's. Stable team/player order and the existing deterministic year/identity roll drive decisions. A completed annual record prevents repeat assessments, tenure changes, and duplicate ledger entries.
- New retirement records retain a detached full player snapshot: identity, college and pro statistics, development, health, and the contract at retirement. Existing history/record-book projections continue using their established historical fields. Retired players cannot be acquired from the market. This preserves information needed for future rights/return work without implementing comebacks or changing finance rules.
- Additive save **36** persists unsigned tenure and new retirement snapshots. Older saves begin observing unsigned tenure in the loaded season; they do not retire anyone on load or replay a completed retirement pass. Old historical records remain valid without inventing missing player attributes. No college declaration, draft intake, UDFA entry, game resolver, or UI layout was changed.

Validation: **169 tests passed**, including nine new direct cases for collection-order determinism, replay, entry protection/value sensitivity, snapshot isolation and career record retention, saved depth preservation, retained contract data, active/practice acquisition reset, invalid ownership/contract safeguards, and native save 35/36 continuity. Production build has zero warnings/errors. Full three-season GameCore smoke (including college, history, save/load and the saved game-day flow), Godot editor import, startup, and `git diff --check` pass. No presentation change required a separate UI smoke.

Extended command: Godot `--headless --path Godot -- --cpu-roster-diagnostic --cpu-roster-seasons=15` (default remains three; allowed range 1–30). The report now also counts retired identities still owned, annual unsigned retirements, players unsigned for three or more years, and oldest market age. Retired ownership is a structural failure; population sizes remain balance observations.

The normal-calendar **15-season run completed 2026–2040, entering 2041, in 93.8 seconds**, with annual disk reloads. Every measured checkpoint had zero structural failures; all Week 1 and rollover checks had zero missing starters/unresolved clubs, and the existing pregame guard remained active throughout. Selected year-end observations:

| Completed season | Free agents entering next season | Unsigned retirements | Recorded transactions per CPU club |
| --- | ---: | ---: | ---: |
| 2026 | 557 | 7 | 37.5 |
| 2028 | 2,500 | 45 | 45.5 |
| 2030 | 3,567 | 347 | 38.6 |
| 2035 | 4,839 | 719 | 44.4 |
| 2037 | 5,035 | 843 | 45.2 |
| 2038 | 5,035 | 892 | 45.7 |
| 2039 | 4,988 | 903 | 45.9 |
| 2040 | 5,045 | 879 | 49.4 |

The final market contains 2,091 players observed unsigned for at least three years; its oldest player is 38. This shows a late-run population plateau for the standard world, not proof of ideal talent distribution or balance across all worlds. Transaction averages now include team-player retirements. The isolated unchanged resolver benchmark passes at **827.6 ms / 444.97 MiB** for 272 pro games, **65.4 ms / 26.54 MiB** for a 16-game week, **4.354 ms / 1.80 MiB** for one game, and **109.4 ms / 8.84 MiB** for 783 college games. Calendar work and disk persistence are represented by the endurance timing, not those resolver samples.

Remaining limits and next substantial continuation: the current shared annual development model still improves unsigned players without tracking training activity, and the market remains large. Implement the blueprint's activity-based development/readiness foundation across roster, practice, and unsigned states before deeper CPU franchise strategy, and retain long-run market-quality diagnostics while doing so. This slice does not add retirement-return negotiations, reinstatement, Reserve/Retired accounting, reserve-player retirement selection, or reconstruct old retired identities; those require their own rights/rules slice. No new user-design decision blocks this completed unsigned-career foundation. The twelve pre-existing untracked sidecars remain untouched.

## CPU roster lifecycle — completed September 25, 2026

Smallest complete outcome: CPU clubs assess, retain, acquire, draft, cut, and repair rosters through the existing rules/transaction services from offseason through Week 1, then sustain legal rosters through several seasons. The user club is excluded.

- [x] Bounded audit of existing CPU, contract, waiver, draft, depth, and calendar paths.
- [x] Unified availability-aware roster evaluation and verified rule corrections.
- [x] Offseason, market, draft, camp, reserve, and game-day lifecycle integration.
- [x] Additive persistence, checkpoint idempotency, cooldowns, and transaction rationale.
- [x] Direct tests and multi-season roster-health diagnostic.
- [x] Full validation, measured limitations, and documentation.
- [x] Focused local commits; preserve the twelve pre-existing untracked sidecars.

Verified audit findings: starter repair counts injured players; evaluation and depth validation omit completely absent positions; camp cuts may fall back to essential starters; rookie signing lacks cap validation; cap queries clamp deficits to zero; expiration skips IR/practice-squad contracts; expired waivers retain inherited contracts; waiver claim generation directly appends claims and exposes hidden rating deltas; trade cap previews omit reserve payroll. No pro resolver redesign is needed.

Implemented behavior:
- `FrontOfficeEvaluationService` derives all required positions from the authoritative starter catalog, distinguishes available starter emergencies, missing working depth, developmental opportunity, aging/expiring groups, excess depth, and cap pressure, and values bounded future upside internally. Saved healthy starters are protected from marginal cuts. Draft scores balance current value and need, then reassess after each pick; a sufficiently superior prospect can beat an urgent need. Pure waiver proposals use the same evaluation. CPU planning calls the normal transaction, contract, depth-chart, and training services; it owns no second transaction implementation.
- `CpuRosterManagementService` assesses the offseason, retains up to six eligible expiring players, reserves the known remaining rookie-contract budget, participates in free agency and camp, signs up to two UDFAs, makes legal camp cuts/focus/finalization choices, builds a small practice squad, and handles weekly and pregame emergencies. Discretionary active signings are capped at four per free-agency/camp checkpoint, with one acquisition per team per round. Emergency repair is separately bounded to 24 iterations and stops immediately when no legal action succeeds. Available depth targets are QB/TE/each OL 2, RB/S 3, WR 5, EDGE/DT/LB/CB 4, K/P 1; these are planning targets, not new league minimums.
- Young camp cuts use the existing one-week waiver process. Up to four clubs may propose claims, with at most two outstanding claims per CPU club. Resolution freezes the authoritative claim order and rechecks benefit, availability, cap, capacity, and conditional-release safety before CPU confirmation. The user retains the existing explicit confirmation/cancellation stop. CPU practice squads aim for at most six players, add at most two per camp/preseason checkpoint, use the existing age-25 eligibility rule and practice contracts, and promote through the permanent active-contract service when needed. IR movement retains its cap charge, uses genuine medical unavailability, and recovered players can return through the normal activation service.
- Quick games, live starts, playoff games, and season rollover validate CPU roster legality before play. Unresolvable shortages return an explicit failure rather than generating a player, overpaying through the cap, or simulating an illegal CPU roster. The user franchise is excluded from every autonomous roster/depth/draft action; the endurance harness supplies explicit diagnostic user inputs separately.
- Important signings, retention, cuts, waiver interest/declines, and passes carry concise rationale in the existing transaction ledger. No planning-document model or new screen was introduced. The Trade Desk roster report no longer exposes average hidden potential. Trade-market responses preserve essential CPU coverage, and preview/commit cap validation includes IR and practice-squad payroll. Autonomous CPU trading remains excluded.

Rule and continuity corrections:
- Cap room now reports real deficits; normal signing/extension/conditional-acquisition checks cannot use a clamped zero to hide overspending. Rookie transactions validate draft ownership and cap before mutation, and active signing honors the existing 90-player draft/rookie/camp capacity versus 53 elsewhere. Acquisitions append valid depth entries; departures remove stale entries. Conditional free-agent releases commit only after the full offer passes validation. CPU offers no longer inherit the user's GM negotiation attribute.
- Contract expiry covers active, IR, and practice-squad pools, preserves freshly agreed offseason extensions, and remains once per season. Waiver expiry clears inherited contracts and works across the season/week reset. Injury recovery includes free-agent and waiver pools; pending waived players age at rollover. Duplicate pool ownership blocks acquisition, and UDFA conversion checks every pro ownership pool.
- Postseason roster windows now permit the same validated in-season replacement actions for user and CPU clubs, while live-session locks remain binding. This closes an emergency-repair gap without changing the snap engine.
- The endurance run exposed the compact college catalog's generic OT/OG entrants. New declared prospects and legacy generic prospects entering pro rosters receive one deterministic LT/RT or LG/RG position from stable identity; this creates no secondary-position eligibility and does not rewrite existing pro rosters or college simulation.

Persistence and determinism:
- Save **35** adds season-scoped CPU completed-checkpoint keys, transaction absolute weeks for departure cooldowns, and frozen waiver resolution order. Plans/scores are derived, not persisted. Older saves mark the current and earlier offseason CPU checkpoints completed; loading does not rebuild CPU rosters or replay discretionary transactions. Later normal checkpoints and necessary pregame legality checks handle future work.
- Repeat Continue/checkpoint calls, game retries, and annual disk reloads do not repeat completed moves. Four-week departure cooldowns prevent immediate re-signing of released/waived players, including the camp-to-preseason boundary. Team, player, prospect, claim, and transaction iteration uses explicit stable order; no wall clock, global random generator, UI state, or unstable hash selects CPU actions. The cooldown cache is local and reconstructible from the persisted ledger.

Validation:
- `dotnet test 'Godot/Tests/GridironGM.Domain.Tests.csproj' --no-restore`: **160 passed**, including **26 CPU cases** (25 direct cases plus three-season endurance), with no warnings/errors. Coverage includes reordered-input determinism, native save/reload, legacy checkpoint migration, repeat calls, user exclusion, cap/capacity, empty positions, injuries, permanent practice promotion, negligible upgrades, conditional rejection, cooldowns, retention/expiry, protected cuts, priority/waiver expiry, rookie value/need, UDFA contracts, college line positions, duplicate ownership, and no-candidate failure.
- Production build, full GameCore smoke (including its existing three-season history/college/save lifecycle), Godot editor import, project startup, actual Game Day controls/timeout/postgame UI smoke, and `git diff --check` pass.
- New command: Godot `--headless --path Godot -- --cpu-roster-diagnostic`. It drives three consecutive seasons with normal CPU contracts and expirations, daily injury recovery, real Continue/draft/camp services, and annual disk reloads. It fails on structural ownership/cap/capacity/reserve/depth invalidity, verifies available starters at Week 1 and rollover, and relies on the pregame guard before every CPU game. Balance observations are reported rather than treated as brittle assertions.

Final isolated diagnostic: **2026–2028 completed, entering 2029, 12.7 seconds**. Every measured checkpoint had zero cap violations, duplicate ownership, illegal sizes, reserve-contract/eligibility violations, or invalid saved depth entries. All three Week 1 checks and rollover checks had zero missing required starters and zero unresolved clubs.

| Completed season | Recorded transactions per CPU club | Free agents entering next season |
| --- | ---: | ---: |
| 2026 | 36.7 | 566 |
| 2027 | 38.3 | 1,512 |
| 2028 | 44.4 | 2,560 |

The transaction average includes recorded personnel changes and expirations, excluding review/camp-advice entries and unexecuted claim submissions/cancellations. Final available depth min/average/max: QB 2/2.7/3; RB 3/3.4/4; WR 5/5.2/6; TE 2/2.6/3; LT 2/2.4/3; LG 2/2.1/3; C 2/2.3/3; RG 2/2.0/3; RT 2/2.0/2; EDGE 4/4.1/5; DT 4/4.0/4; LB 4/4.0/5; CB 4/4.2/5; S 3/3.0/3; K 1/1.5/2; P 1/1.2/2. Caching ledger-derived cooldown sets reduced the initial endurance run from 119.8 seconds to about 13 seconds as the market grew; decisions remain derived from the same saved facts.

Isolated Debug resolver benchmark: one pro game **3.565 ms / 1.80 MiB**, 16-game week **54.8 ms / 26.54 MiB**, 272-game season **868.6 ms / 445.24 MiB cumulative allocation**, and 783 lightweight college games **105.6 ms / 8.84 MiB**. The current pro measurement is about 5.7% above the prior 821.7 ms; allocation and resolver code remain essentially unchanged. This benchmark excludes CPU calendar work, disk, and rendering; the endurance timing includes the CPU lifecycle and annual saves/reloads. No pro rules version, event-log schema, or live response boundary changed.

Remaining limits and next continuation: this is a legal bounded foundation, not complete NFL finance/personnel realism. Existing flat annual-salary accounting, age-based practice eligibility, one-week waivers, and permanent practice promotion remain; no new dead-money/negotiation system, temporary elevation, scheme-fit model, personality/direction system, or autonomous trading was invented. Offseason vacancies may exist until the next legal acquisition window, and an impossible custom roster/market safely blocks its game. Standard-seed endurance is validated for three seasons, not every seed or decades. The expanding unsigned population (2,560 after three cycles) is the main measured balance/scale limitation: next, complete long-run unsigned-player aging/retirement and draft-pipeline market balance before deeper persisted CPU franchise direction. No user-design decision blocks this completed milestone.

Local implementation commits: `af3568c` (rules, evaluation, lifecycle, migration, rationale) and `73d47e2` (direct tests and diagnostic). Task-created UIDs are included; the twelve pre-existing untracked sidecars remain untouched.

## Pro clock-management continuation — September 24, 2026

- [x] Review existing snap/authority/persistence code and accepted game-management scope.
- [x] Add versioned timeouts, kneels/spikes, earlier-down kicks, and end-of-half recommendations without changing v1 live replays.
- [x] Bind validated clock controls and timeout counts to Game Day.
- [x] Verify deterministic scenarios, authority, save/reload, full tests/smoke, and performance; document and commit.

Implemented:
- The post-slice stabilization pass removed two avoidable copies from the live/save path. Live session commands return the full projected result and timeline only when Game Day opens or resumes, and return the full result again at completion; intermediate commands expose only the current event, controls, and situation. Native saves now write compact JSON directly into the flushed temporary sibling before atomic replacement rather than building an indented string and second UTF-8 buffer. The schema and save version remain unchanged, and direct regressions cover response payload boundaries plus compact save/load round trips.
- New games use `pro-snap-v2-clock-2025` and save version **34**. Existing `pro-snap-v1-2025` games remain on their original behavior, with a frozen pre-change replay digest covering scores, RNG, drives, plays, descriptions, participants, and statistics. Both rules versions load; older aggregate playback remains supported. New timeout fields default safely without restarting an unfinished game.
- Clock-stopping timeouts are validated for either side under **Overall Game Management**. A timeout creates an event, charges the requesting team once, stops the clock without consuming game time or RNG, and preserves the next play's queued choices. Already-stopped clocks, depleted budgets, coach-owned commands, and stale retries are rejected. The retained GM may select Use timeout or No timeout; empty input uses staff recommendations, and the opposing team's clock decisions remain independent.
- Teams receive three timeouts per regulation half, two in regular-season overtime, and three per two-period postseason overtime half. Two-minute warnings interrupt pre-snap runoff at 2:00, or follow the conclusion of a play that crossed the threshold. They preserve the next input and do not count as snaps. Timing reference: [official NFL rulebook, rules 4 and 16](https://static.www.nfl.com/image/upload/fl_attachment/league/tqivdkzt9mu6wdgsh1ku.pdf). This expands the existing bounded rules baseline, not every special timing/penalty exception.
- **Offensive Play-Calling** owns Kneel and Spike. Kneels credit an attempt and one lost rushing yard; spikes credit an incomplete pass attempt and a used down, including fourth-down loss of possession. Both require an eligible QB. A normal/hurry spike uses three seconds of preparation on a running clock plus one second for the snap; a kneel takes two seconds plus the selected tempo's runoff. Management's explicit Chew choice still governs runoff before a spike. Staff can recommend a safe kneel-out considering remaining downs and opposing timeouts, or a late spike when its own timeouts are exhausted.
- The existing Go/Kick management choice is available on every scrimmage down. Special teams separately chooses Punt/Field goal; an earlier-down special-teams selection alone cannot override management's Go decision. End-of-half recommendations can select a field goal before fourth down, use timeouts, and hurry when needed. Postseason overtime period expiry is not treated as a deadline for the current possession.
- Game Day shows timeout counts, ownership, timeout selection, and the expanded offense/down controls. The real Godot control smoke includes a charged timeout and still verifies all five result tabs and minimum-window horizontal bounds.
- The changed season outcomes exposed a dashboard defect for teams that did not qualify for the playoffs: their next-round header stayed at Playoffs Pending. Nonqualifiers now follow the league's remaining rounds. A direct regression covers advancement through the conference championships. The benchmark test now expects the actual 783 college games (768 regular, 11 playoff, four bowls); its previous assertion excluded postseason. The balance guard permits individual blowouts up to 70 points while retaining average-scoring and home-advantage checks, rather than treating a legal 44-point margin as an engine failure.

Remaining: out-of-bounds timing, procedural/injury stoppage penalties, timeout use on already-stopped clocks (including kicker icing), challenges, richer two-minute recommendations, and returns. Save formatting, per-command live response copying, event-log schema compaction, and rolling recovery backups are complete. The existing coarse 24/7/38-second tempo model remains outside intentional clock-play handling. No new player ratings, art, college snap engine, or authority-negotiation design was introduced.

Validation: **134 focused tests pass**, including 24 clock/compatibility cases, the nonqualifier regression, compact save/load, and bounded live-response payload coverage. The production assembly builds without warnings/errors. Full GameCore smoke passes three-season progression, postseason, awards/records/history, and save/load. Godot editor import, project startup, and actual decision/timeout/postgame UI smoke pass. `git diff --check` is clean. The new scripts' generated UIDs are included; the twelve original untracked sidecars remain untouched.

Updated isolated Debug benchmark (same command and warmup as the prior checkpoint):

| Workload | Time | Managed allocation |
| --- | ---: | ---: |
| One pro game | 3.761 ms | 1.80 MiB |
| 16-game pro week | 57.5 ms | 26.54 MiB |
| 272-game pro regular season | 823.6 ms (3.028 ms/game) | 445.10 MiB cumulative |
| Actual lightweight college season, 783 games | 107.5 ms | 8.84 MiB |

Against the prior snap checkpoint, the pro season takes 3.7% longer and allocates 7.9% more while resolving clock stoppages and the resulting additional plays/decisions. Representative indented diagnostic result JSON is now 360–401 KiB. The season remains under one second in this run. Repeated live DTO copying and indented save formatting were removed in that checkpoint; save-38 event-log compaction is recorded in the newer storage checkpoint above. College resolution is unchanged. These measurements exclude disk, live DTO projection, save serialization, and rendering and are not peak-memory measurements.

## Pro snap-engine checkpoint — September 24, 2026

Working checklist:
- [x] Read project authorities and recent history; baseline build, 73 focused tests, full GameCore smoke, Godot editor/project launch, and benchmark pass.
- [x] High: prevent normal depth commands from overriding Head Coach lineup authority; prevent running-game depth edits and live-game roster/contract transactions.
- [x] Critical: replace live substitutions that regenerate whole-game statistics and manufacture future score increments with forward-only snap resolution.
- [x] Implement authoritative pro drives, plays, clock, possession, scoring, deterministic randomness, and supported decisions.
- [x] Integrate live persistence, legacy compatibility, Game Day controls, and existing result consequences.
- [x] Verify direct rules, save/reload, authority, statistics, season integration, full smoke, and benchmark; document limits.
- [x] Preserve implementation and verification in focused local commits.

Audit baseline on this workstation: 272 games in 19.1 ms, 8.14 MiB allocated; the old 768-game pro-engine college-workload proxy took 37.4 ms, 22.98 MiB. The twelve pre-existing untracked `.uid` sidecars correspond to tracked C# files and are preserved outside task commits.

Verified audit corrections:
- **Critical:** live substitutions formerly regenerated whole-game production and manufactured future scoring increments. New games resolve only the next play; substitutions leave the resolved prefix unchanged.
- **Critical:** direct overwrite could truncate the last good save. Saving now writes and flushes a unique sibling file, then atomically replaces the destination; failed replacement retains the prior file.
- **High:** normal depth commands could override Head Coach lineup authority or change personnel during running playback. All user mutation entry points enforce the agreement and pause boundary. Live roster/contract/trade moves are locked to preserve participant ownership.
- **High:** calendar/full-game/playoff commands could conflict with an active live session. These commands stop before mutation; user-facing quick/live starts also require the scheduled day.
- **High:** reconstructing a missing legacy completed playoff result simulated another game and could invent player statistics and injuries. Repair now preserves the bracket's recorded score without manufactured play history or side effects.
- **Medium:** postseason completion did not share fatigue/statistics/result handling. All new pro completions now use one idempotent result commit. Old final-score-formula assertions and no-ties season assertions now test actual rules and statistical consistency.

Implemented architecture and rules:
- `ProSnapEngine.Step` emits one authoritative event. Quick simulation loops the same method. `ProGameState` saves possession, own-goal-relative field position, down/distance, phase, period/clock, drives, pending input, in-game unavailability, and explicit SplitMix64 random state. Seed derivation uses saved world seed, season, matchup identity, and team IDs; no global random generator or simulated future is retained.
- `GameResult.BoxScore.PlayByPlay` is the sole resolved log. Events retain participating IDs and sparse stat deltas; `ProGameStatistics` reduces those into the existing player/team box scores. Score, quarter scoring, season production, career history, awards, standings, and records use the existing result pipeline. Net team offense subtracts sack losses from gross passing plus rushing yards. A rebuild test reconciles every numerical player/team total against the log.
- Supported events: kickoffs, runs, complete/incomplete passes, sacks, interceptions, fumbles, downs, punts, field goals, extra points/two-point tries, touchdowns, safeties, period endings, halftime, and final results. Saved depth order, existing Overall, staff strength, fatigue, and availability shape participants/outcomes. Resolved game injuries exclude the player from later snaps and enter the existing medical history exactly once on completion.
- Rules are explicitly versioned `pro-snap-v1-2025`: four 15-minute quarters; preseason may tie; regular-season ties enter one 10-minute overtime; postseason uses successive 15-minute overtime periods until a winner. Both teams receive an initial overtime opportunity subject to the regular-season clock, with the safety exception; later unequal scores after completed opportunities finish the game. Kickoff touchbacks start at the 35. Baseline reference: [NFL 2025 rulebook](https://operations.nfl.com/media/ntif5hxb/2025-nfl-rulebook-final.pdf), rules 6 and 16. This is a bounded subset, not a claim to implement every NFL rule or a new 2026 rulebook.
- Four independent Head Coach domains are validated when submitting AND resolving a pending choice. Retained GM choices cover Run/Pass; Balanced/Run focus/Pass focus/Blitz; kick/try type; fourth-down Go/Kick and Normal/Hurry/Chew tempo. Empty choices use staff recommendations. Overall management decides whether to kick on fourth down; special teams chooses the kick if management selects Kick. Godot labels ownership and disables unavailable/coach-owned controls. Paused Next Play, resume, depth adjustments, and exit/save all use services.
- Live controls and result mapping moved out of the dashboard monolith into focused partials. Current period/clock/possession update even across quarter boundaries; overtime never appears final merely because an earlier period reached zero. Postgame includes all overtime periods, actual scoring plays, expanded team/player/specialist lines, and unknown legacy quarter splits. Controls wrap and vertical scrolling preserves access at 1024x576; no visual references or branding changed.

Persistence:
- Save version **33**, with additive defaults. Existing completed results remain unchanged. A legacy unfinished game continues its saved playback exactly, with substitutions disabled for that game; its recorded stats/fatigue commit once, without inventing new injuries or snap history. New games use the new resolver.
- Live saves retain resolved events, ordered depth, queued choices, drive state, and RNG cursor. Expected-sequence commands reject stale retries. Completed-result lookup prevents a second injury, fatigue, stats, or schedule commit. New sessions serialize only one played-event log; old `PlayedEvents` is still readable. Unsupported live rules versions fail load explicitly. Load does not write or regenerate an existing full league just to adopt this slice.

Validation commands (repository root; Godot console executable from the sibling Godot 4.5.1 Mono installation):
- `dotnet build 'Godot/Gridiron GM.csproj'` — zero warnings/errors.
- `dotnet test 'Godot/Tests/GridironGM.Domain.Tests.csproj' --no-restore` — **107 passed**. Covers deterministic full replay, legal transitions, all supported event families, ties/overtime, stat reduction, depth/availability/injury behavior, queued-input replay/revalidation, stale retries, live disk save/load/substitution, one-time health/stat consequences, command guards, authority, standings, and legacy result preservation.
- Godot `--headless --path Godot -- --gamecore-smoke-test` — passes college lifecycle, dashboard-to-live/save/postgame, whole-season progression, postseason, three-season continuity, history/awards/records, and save/load cleanup.
- Godot `--headless --path Godot -- --game-day-ui-smoke` — real generated league; actual Next Play/decision signals, coach-disabled choices, full-game completion, all five postgame tabs, and horizontal bounds at 1024x576. Uses an isolated in-memory context; never writes the user's autosave. This is functional UI QA, not a new visual acceptance review.
- Godot `--headless --path Godot --editor --quit` and `--headless --path Godot --quit-after 5` — clean import/startup.
- Godot `--headless --path Godot -- --gamecore-benchmark` — results below. `git diff --check` passes.

Benchmark, same workstation, Debug build, two warmup games; pro measures resolution without disk/rendering:

| Workload | Time | Managed allocation |
| --- | ---: | ---: |
| One detailed pro game | 3.711 ms | 1.56 MiB |
| 16-game pro week | 61.8 ms | 24.56 MiB |
| 272-game pro regular season | 793.9 ms (2.919 ms/game) | 412.36 MiB total allocated |
| Actual 128-team lightweight college season, 768 regular + 15 postseason games and lifecycle | 110.7 ms | 8.84 MiB |

The pre-snap pro baseline was 19.1 ms / 8.14 MiB for 272 aggregate results. The increased cost was investigated: the new workload resolves roughly 150–200 events per game with live personnel selection, stat deltas, injuries, and persistent play/drive histories instead of an aggregate score formula. Replacing per-participant zero-stat objects with participant IDs and emitting sparse numeric deltas reduced the initial detailed-season allocation from about 468 MiB to 412 MiB. New live saves no longer repeat the event log. Representative indented final-game JSON is 329–347 KiB (serialization excluded from timed samples); 272 comparable retained results imply roughly 88 MiB before other league state. Total allocated bytes above are cumulative allocations, not peak resident memory. Current season logs are discarded by the existing rollover after summary/stat history is archived. The old 768-game college number (37.4 ms / 22.98 MiB at task baseline) was repeated pro simulation, so it is not comparable to the corrected actual college benchmark. The college resolver itself is unchanged.

Material remaining work and next continuation:
- **Completed by save 38:** compact play-log encoding and three-file rolling recovery retention now sit behind the existing atomic save boundary. Further whole-save latency profiling remains appropriate before substantially expanding per-play detail.
- The v2 continuation above implements timeouts, kneels/spikes, earlier-down kicks, and two-minute warnings. Next: richer possession/returns and out-of-bounds timing, with deterministic scenarios before tactics expand. Saved v1 games retain the earlier coarse clock behavior for replay compatibility.
- Penalties remain deferred because no authoritative penalty model exists. Also deferred: onside kicks, blocked/returned kicks, return touchdowns, detailed safety-free-kick personnel, defensive try returns, emergency cross-position personnel, formations/coverage packages, and playbook creation. Extremely depleted attacks without an available QB or RB stop with a validation error instead of using an unavailable player. Current injuries are bounded to one new injury per game using the existing medical model; balance and richer injury frequency need later tuning.
- Preserve the lightweight college resolver until the pro engine's scenarios and performance mature. No new hidden attributes, animations, portraits, audio, or art work are part of this slice. No user design decision blocks this checkpoint; richer player attributes and playbook/penalty design require a later explicit design pass.
- New `.uid` sidecars for this slice's new scripts are included deliberately; the twelve pre-existing untracked sidecars remain outside task commits.

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
- The dashboard is native-only: retired endpoint calls, timeout helpers, response parsers, and backend fallback branches have been removed. Never restore an external gameplay backend.
- A project-level `NuGet.Config` clears an obsolete workstation fallback-package path. `dotnet build` now restores and compiles the Godot project successfully.
- The accidental `Godot/Godot` duplicate created during consolidation has been removed locally; the project now has one active Godot source tree.
- The native automated smoke run now validates a complete first season flow: league bootstrap, dashboard, roster/depth chart, preseason, regular season, playoffs, season transition, retirements, history, save/load, and cleanup.
- New franchises load the packaged 32-team seed data and present the included team logos during team selection.
- The roster, depth chart, standings, results, schedule, injury-report, simulation, save/load, and game-result views call GameCore directly.
- The contract and transaction foundation now validates cap space, active-roster capacity, player-pool ownership, and postseason lockout before committing releases, extensions, free-agent signings, or expirations. Every committed mutation is persisted in the native transaction log.
- The offseason now processes expiring contracts, retirements, staff changes, free agency, franchise tags, the draft lifecycle, rookie signing, training camp, and the preseason handoff.
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
- Trade-asset foundations and the first user trade-proposal workflow are complete: draft picks retain current and original ownership, explicit user proposals exchange legal active-roster players and unused picks only after phase, cap, roster, and ownership validation, accepted trades record both teams' history, and Godot displays deterministic counterparty value rationale. Pick ownership survives save/load and draft archival rollover; rejected or invalid proposals leave rosters unchanged.
- College league-leaders foundation is complete: a compact, read-only passing, rushing, receiving, and touchdown table is deterministically derived from authoritative persisted college statistics and surfaced in the existing College Football workflow. The view owns no state and remains stable through save/load.
- College postseason-projections foundation is complete: a compact, read-only 12-team playoff and bowl outlook is deterministically derived from current college rankings and records, including five conference-leader auto-bids, seven at-large selections, and four first-round byes. It is surfaced in the existing College Football workflow and remains stable through save/load. It schedules or simulates no postseason games.
- College player-development foundation is complete: a completed college season applies one small, deterministic, bounded development pass per active player. Explainable development history persists safely, migrates legacy saves to empty histories, and scouting exposes only the non-rating development note.
- College news foundation is complete: a compact, read-only feed deterministically derives result, ranking, and performance hooks from authoritative college state and surfaces them in the existing College Football workflow. It stores no UI-owned news state and uses no external feed or generated articles.
- College transfer-portal foundation is complete: between seasons, a bounded deterministic set of returning non-redshirt players can move for playing opportunity, competitive context, development outlook, and program fit. Destinations prioritize open positional paths with bounded program-strength and variation factors; transfers are immediately eligible, preserve identity and prior-school statistics, persist in player and season state, and surface in college rosters and news without user control.
- College recruiting foundation is complete: a deterministic CPU-only annual cycle fills positional vacancies and adds one planned redshirt per program. Program baseline, recent results, explicit roster need, and bounded variation shape player quality without guaranteeing dominant classes to successful teams. Generated freshmen receive stable identities and public signing context; current classes persist through save/load and appear in the team encyclopedia and college news without exposing exact ratings.
- College coaching-carousel foundation is complete: every program has a persisted lightweight head-coach identity with bounded program and recruiting influence. Coaches age and retain tenure; up to twelve annual retirements or performance-based changes receive deterministic replacements and public rationale. Coaching supplies only a capped score adjustment plus small recruiting/transfer context, survives save/load/migration, and appears in program profiles and college news without exposing contracts or user controls.
- College game box-score foundation is complete: each resolved college game now snapshots its participating players' passing, rushing, receiving, and touchdown production in the authoritative result. Per-game lines reconcile exactly with accumulated season totals, migrate legacy results to empty lines safely, persist through save/load, and supply compact top-performer context in each program's schedule without creating a playable college game view.
- College player-injury foundation is complete: deterministic college-game injuries have bounded weekly recovery, availability-aware college simulation, persisted injury history, and safe legacy defaults. College injury state remains separate from pro-player injuries and scouting presents only current availability context.
- Public college big-board foundation is complete: distinct deterministic analyst and media prospect rankings use public workout and declared-outlook context, remain separate from private scouting estimates, and are available in the College Football workflow.
- College bowl-playoff simulation foundation is complete: the explicit `college-postseason-12-team-v1` rules deterministically select and seed a 12-team field, resolve eleven playoff games through the national championship, and pair eight additional teams across four bowls without altering pro scheduling, standings, or player pools.
- College postseason presentation is complete: the existing College Football postseason route safely switches from current projections to persisted playoff and bowl final results once the college postseason completes.
- College season-archive foundation is complete: completed college postseason, awards, and all 128 final program records/rankings are copied into immutable league-owned archives before rollover replaces the live college universe, with multi-season smoke coverage and save/load continuity.
- College archive presentation is complete: the existing History workflow now includes a read-only completed college season tab with champion, postseason results, and awards context.
- UI overhaul sequencing has completed the persistent workstation shell, Home command center, and Squad/depth-chart workspace. Squad now exposes roster filtering, a dense selected-player inspector for role/readiness, injury, fatigue, evaluations, live/career stats, and contract details, with player actions beside their relevant context; depth chart remains a separate explicit editing mode. These presentation passes continue to use GameCore services as their only state authority.
- The Scouting and Draft Board workspace is complete: the existing draft service now sits behind a filterable prospect board, pick-order context, and a dense inspector that explicitly separates public combine/pro-day facts from scouting estimates, confidence, traits, interviews, and reports. Hidden ratings remain unavailable to the UI.
- The Transactions and Market workspace is complete: a persistent market desk now surfaces phase, cap room, roster capacity, market counts, and the latest persisted transaction ledger before routing users to focused Free Agency, Trade Desk, Waivers/Practice Squad, and player-contract contexts. Existing GameCore services remain the sole authority for all actions and rule feedback.
- The League and History workspace is complete: active franchise context now remains visible while users browse authoritative standings, schedule/results and box scores, injury reports, per-game leaders, playoff context, and immutable season archives. Schedule selection provides a read-only matchup inspector, and history is clearly separated into season navigation and season review.
- The Startup, Game Day, and Post-Game Recap workspace is complete: franchise entry, loading, and new-franchise choices now use an intentional full-screen workstation entry state; game day presents matchup, venue, record, status, and explicit simulation/watch choices; and post-game recap foregrounds final outcomes with direct box-score and continue routes. Existing save, lifecycle, simulation, recap, and box-score ownership remains unchanged.
- The Inbox and decision queue is complete: a compact categorized workspace now presents only authoritative active action items with priority and read-state context, selected-message detail, and explicit routes to the existing game-day, roster, depth-chart, league, offseason, and training-camp workflows. No inbox events or decision rules are controller-owned.
- The Franchise Settings and Profile workspace is complete: the utilities desk presents the active immutable GM/franchise snapshot, existing named save/autosave state and controls, pointers to already-persisted Squad display preferences, and a clearly separate session-only developer diagnostics section. It introduces no new profile, save, display, or league rules.
- The workstation UI-overhaul sequence is complete. Read-only CPU front-office evaluation is now complete: deterministic team reports expose roster/depth needs, cap and expiring-contract pressure, age profile and current draft-pick context in the Trade Desk (the September 25 CPU milestone removes hidden potential from this report) without making or persisting any roster decision. Reports safely regenerate from saved and rolled-over league state.
- College-universe foundation is complete at full league scale: new seasons create 128 persisted fictional programs across eight balanced conferences, a 12-game schedule with 768 unique matchups, initial draft-linked players, and compact development rosters. Returning underclassmen retain identity, team, development/injury/transfer history, and archived season statistics across rollover while the simulated recruiting cycle fills positional vacancies; declared players leave for the unique pro prospect pool. A deterministic first-year redshirt reserve at every program preserves one of four playable seasons within the five-year eligibility window, stays out of games and injuries, develops through practice, and becomes an active freshman the following season. A bounded CPU-only transfer portal moves selected returners into open positional paths with immediate eligibility and explainable persisted context. A lightweight head-coach carousel gives program performance, recruiting, and transfer destinations capped coaching context. Authoritative per-game player box scores reconcile with season totals. The universe owns eligibility, results, standings, rankings, statistics, awards, news, public boards, postseason, recruiting, transfers, coaching changes, draft decisions, archives, and save/load continuity. The searchable, conference-filterable Full Rankings workspace now scales into a read-only team encyclopedia: selecting any program shows its authoritative season record, coach, archived year-by-year program results, 12-game slate with compact box-score leaders, recruiting class, results/upcoming opponents, statistical leaders, complete current development roster, player career totals, eligibility, and transfer context with no hidden ratings. Existing active 16-team careers are not rewritten midseason; their next generated college season adopts the full catalog. Recruiting balance/depth, transfer/coach tuning, and college logo assets remain later slices.
- User trade-decision support is complete: the Trade Desk now requests an authoritative read-only preview for the selected player/pick packages before submission. It explains deterministic package value and the acceptance threshold, projected cap room, and active-roster counts for both clubs; invalid ownership, cap, roster, and phase conditions are shown before submission. Previews never mutate rosters, pick ownership, cap state, or transaction history, and explicit submission remains the only transaction path.
- The accepted Trade Block / Finder slice is complete: one combined Godot workspace shops up to eight owned players and unused picks, accepts an optional broad position target, and reveals no interested clubs or returns until explicit submission. GameCore then creates only cap-, roster-, ownership-, phase-, and valuation-valid concrete CPU packages; responses, rejections, and the submitted package persist through save/load. Open offers can be accepted or rejected, the package can be withdrawn, completed trades invalidate stale responses, and rollover clears the market. The retired separate Trade Block, Trade Finder, and Active Offers surfaces are no longer navigation destinations.
- The accepted player-release interaction is complete for the current contract model: right-clicking a roster row opens a nested Contract menu, and Release opens a compact confirmation rather than executing immediately. A read-only GameCore preview reports the saved contract, roster count, committed payroll, and cap room before and after the move without mutating state; only explicit confirmation releases the player, removes depth assignments, records the transaction, autosaves, and refreshes the roster. The existing profile action routes through the same confirmation.
- The accepted free-agency table interaction is complete: the discarded permanent right-side offer block has been removed so the full-width sortable table remains the primary comparison surface. Right-clicking a real free-agent row exposes player details or Make Offer; only Make Offer opens the separate validated negotiation dialog with the selected player, estimated requirement, team cap room, salary, guarantee, and term. Selection and closing the dialog are read-only, while explicit submission continues through the existing GameCore contract transaction and autosave path.
- The accepted Practice Squad presentation is complete as a Roster-style Team workspace rather than a bespoke card screen. It uses a full-width player table with position, age, known ratings, contract, health, eligibility, and active-slot context, plus a compact squad/roster/cap summary. Sign Players routes to the existing validated practice-squad market, while Elevate Selected uses the same phase-, ownership-, cap-, and roster-validated transaction service, records the move, autosaves, and refreshes both squads.
- The undrafted-free-agent lifecycle foundation is complete: finishing the seven-round draft now opens the UDFA market during Rookie Signing instead of delaying undrafted players until the next preseason rollover. Remaining eligible prospects move exactly once into a uniquely identified free-agent pool, and only those rookies may sign during that phase. Accepted offers require the supported three-year undrafted-rookie contract, use normal salary/guarantee, cap, roster, ownership, transaction, and autosave rules, and remain compatible with rollover migration fallback.
- The accepted UDFA market presentation is complete under Scouting. Its dense table shows only authoritative unsigned post-draft rookies with known position, age, ratings, trait/playstyle, asking salary, and minicamp status. Offer Contract opens the single shared negotiation dialog with Undrafted Rookie Contract and its required three-year term displayed; it does not create separate contract-type buttons. Up to ten optional rookie-minicamp invitations persist through save/load, can be withdrawn from the same screen, do not sign or reserve a player, and are removed automatically if that player signs. The market explains closed/empty states outside Rookie Signing.
- The accepted Final Roster Cut-Down presentation is complete inside Training Camp. A dense active-roster table combines position depth context, known ratings, contract, health, role, and readiness with authoritative roster/practice-squad counts and the exact number of required cuts. Selection is read-only; Release Selected opens the shared financial confirmation before any mutation, while Finalize Legal Roster continues through the existing focus, 53-player, and depth-validity rules. Successful moves and finalization autosave and refresh the table; staff context never executes cuts automatically.
- Final Roster Cut-Down now supports the blueprint's explicit proposed-batch workflow. Checkboxes build a non-mutating cut list and immediately project roster count, remaining required cuts, payroll, cap room, and starter-depth warnings. A single review names every player and combined consequence; confirmation revalidates the whole current list before committing every release, recording each transaction, autosaving, and refreshing the authoritative roster. Invalid or changed proposals return for review without executing a partial selection.
- The accepted Training Camp Player Focus presentation is complete for the current bounded focus model. A dedicated searchable, position-filterable roster table shows age, known ratings, health, fatigue, role/readiness, and authoritative focus status; selecting a row is read-only and clearly previews the effect. The GM may commit one available active-roster player for additional attention, improving readiness and at most one point of development without exceeding potential. The decision records, refreshes the camp report, autosaves, and survives save/load; unavailable players and repeated attempts are rejected. Unapproved drill catalogs and first-team-rep percentages are not fabricated by this slice.
- The accepted Training Camp Position Groups presentation is complete for the current bounded group-focus model. A dense comparison table derives roster count, starter need, availability, injuries, average ratings, fatigue, and staff recommendations from authoritative roster state. Selecting a group is read-only and previews the bounded effect; explicit application remains phase-gated, records and autosaves the decision, refreshes reports and depth state, persists through save/load, and cannot be repeated. It does not invent unapproved drill or rep-allocation rules.
- The accepted Weekly Camp Report presentation is complete as a dedicated dense report rather than another free-form text block. It derives position-group needs and staff recommendations from the persisted camp report, shows recorded position-battle leaders and explanations, and lists real injury/fatigue concerns from the current roster. Refreshing persists a newly derived report but never changes depth assignments; the report links directly to the authoritative depth chart for GM action.
- Training-camp position competitions now honor retained GM depth-chart authority. Updating staff assessments records explainable leaders and challengers inside the Weekly Camp Report without exposing hidden rating arithmetic or reordering any position group; the saved depth chart changes only through the established GM-controlled depth-chart actions.
- The opening-week readiness reminder is now GameCore-backed. During regular-season Week 1 before the user's game, the inbox names the opponent and directs the GM to review the authoritative active roster, availability, and saved depth order. It routes to the roster workspace, changes no personnel or assignments, and disappears naturally once that game is no longer pending.
- Weekly injury-depth advisories now distinguish a legal but thin unit from a blocking depth-chart violation. When injuries leave exactly the required available starters and no reserve at a position, the inbox names the affected unit and routes directly to the authoritative depth chart while explicitly making no signing, release, or assignment change.
- Practice-squad promotion wording and behavior now match the implemented permanent transaction. Both practice-squad surfaces open a confirmation that distinguishes the move from a temporary game-day elevation and previews the replacement active-roster contract, roster count, and cap effect. Confirmation revalidates the move, writes an Active Roster contract and transaction, autosaves, and refreshes both squads; temporary elevation remains unavailable until its exact limits and reversion rules are approved.
- Multi-season continuity hardening is complete: native smoke coverage now simulates three consecutive seasons through playoffs, offseason, draft, training camp, rollover, and save/load. Each boundary verifies accumulated season/draft history, deterministic college-universe completion and reset, next-year draft-pool continuity, and legal roster/cap state without state drift.
- Historical records foundation is complete: a read-only record-book service now derives season and career player records from persisted player history plus franchise records from immutable season archives. Retirement snapshots preserve newly retired players' existing stat histories as historical source data, and the record book remains deterministic through multi-season rollover and save/load. The existing History view shows a compact record-book summary without owning data or duplicating simulation totals.
- Season-awards foundation is complete: every completed season now archives a deterministic, read-only MVP, Offensive Player of the Year, and Defensive Player of the Year slate derived from authoritative player statistics. Award snapshots persist with season history, migrate safely when source history is available, survive multi-season rollover/save-load, and appear in the existing History detail without UI-owned data.
- Historical browsing expansion is complete: the existing History workflow now uses a read-only archive response to compare completed championship seasons and browse derived record-book entries, archived awards, and retirement history. Empty or migrated archive data is handled safely; controllers render service DTOs without creating history state.
- Initial AI roster-management foundation (superseded by the full CPU lifecycle checkpoint above): during free agency and training camp, CPU teams deterministically repaired only immediate starter shortages through the normal cap-, roster-, pool-, and phase-validated free-agent transaction path. Each signing retains its shortage rationale in the persisted transaction ledger, never touches the user roster, and is covered for repeatability, save/load, and both permitted phases. It does not automate trades, counteroffers, staff, schemes, or broader CPU roster strategy.
- College draft-pipeline lifecycle is complete: once the college schedule is complete and the pro draft opens, college seniors and deterministically evaluated juniors receive persisted declare/return outcomes. Declared players enter the pro draft pool through a unique college-player link with an explainable public decision, draft-stock context, and immutable college season history; returns remain outside that pool. Drafted and undrafted rookies preserve that same college identity and history in the pro player record instead of receiving an unrelated replacement identity. The normal pro player profile now carries that school and season-by-season college production forward alongside pro history. The transition is idempotent, save/migration safe, and visible without revealing hidden ratings or enabling college control.
- College-awards foundation is complete: a compact, read-only three-award slate is deterministically derived only after the completed college schedule supplies authoritative player statistics. Immutable award snapshots persist in the college universe, safely derive for legacy completed seasons on load, and are available in the existing College Football workflow. NIL, deeper recruiting/portal tuning, and user-controlled college management remain outside this slice.
- Staff-management foundation is complete: teams persist a deterministic native staff roster with roles, ratings, and ages, and the existing Staff workspace renders read-only organization and role context without changing gameplay rules.
- Staff changes foundation is complete: the Staff Carousel now supports phase-gated release-to-market and vacancy-only hiring from a deterministic persisted staff market. Every accepted change records an auditable staff transaction, survives save/load and legacy migration, and the Staff and team-history workspaces provide explicit rule feedback. Bounded role-specific staff effects are implemented; richer staff contracts and Head Coach authority negotiation remain later slices.
- Head Coach authority persistence foundation is complete: saves now support the twelve approved authority domains, only a Head Coach can hold negotiated control, lower staff authority is normalized away, legacy saves safely default every domain to user control, expired agreements clear when a coach leaves the role, and one service provides canonical validation and ownership queries. The Staff profile presents the current GM/Head Coach split without making it editable outside contract negotiation. Demand generation, contract economics, concessions, and negotiation UI remain deferred for explicit design.
- Staff continuity foundation is complete: staff tenure now persists, staff ages at rollover, age-70 retirements are deterministic, and CPU teams refill retirement vacancies only from the same persisted staff market. User vacancies remain explicit Staff Carousel decisions; broader coaching AI remains outside this slice.
- Staff influence foundation is complete: the Director of Player Personnel adds a small, displayed, capped confidence adjustment to private scouting presentation only, and elite Head Coaches add at most one point to existing annual development for eligible rostered players. Neither effect reveals hidden ratings or overrides player potential, aging, or normal regression.
- Staff influence presentation is complete: Staff profiles now disclose the exact capped personnel-scouting and Head Coach development effects, including vacancy and threshold behavior.
- Staff injury-support foundation is complete: the Medical Director role persists across staffing workflows, and elite Medical Directors remove at most one extra recovery day during daily injury recovery. Injury occurrence, severity, availability, and roster eligibility remain unchanged; Staff profiles disclose the threshold.
- Staff strategy foundation is complete: paired Offensive and Defensive Coordinators can shift simulated team strength by at most one point. Roster ratings, fatigue, injuries, home field, and controlled randomness remain primary, and Staff profiles disclose the cap.
- Player-chemistry foundation is complete: the roster summary deterministically derives a read-only chemistry context from persisted player traits and morale, clearly labels it informational, and makes no simulation, roster-rule, or contract change. Deeper injury-management guidance is complete: the existing injury recovery field now gives deterministic, nonbinding depth-planning guidance from persisted IR status and recovery days, without changing injuries, recovery, roster rules, or transactions. Contract-risk presentation is complete: the Contracts workspace explains recorded near-term expirations and expensive veteran commitments without fabricating future-cap or dead-cap values. Long-run balance diagnostics are complete: a read-only service derives regular-season game volume, points per team game, home-win rate, and largest margin from persisted results, with smoke coverage; the existing developer-only utilities panel surfaces the active snapshot without affecting tuning. Practice-squad call-up guidance is complete: the existing roster-moves workspace now explains whether an active opening exists and identifies the highest-rated eligible internal call-up without changing transaction validation. Main Hub native initialization is complete: both headless editor and project startup runs exit cleanly. Interactive visual inspection remains pending an interactive display session; the next active implementation slice is a fresh review of the remaining roadmap for a non-visual task.
- Strength-and-conditioning staff foundation is complete: new franchises staff the role, legacy franchises expose it as a fillable vacancy, Staff Carousel CPU replacement treats it as required, and the Staff workspace explains its bounded effect. An elite coach adds exactly one point to normal daily fatigue recovery without changing game workload, injury occurrence, injury recovery, or availability rules.
- The first interactive UI feedback pass is complete. The workstation header now wraps within the viewport instead of clipping its right-side controls, sidebar parent groups behave as a single-open accordion, all monetary presentation uses an explicit dollar sign, and Franchise Home uses a vertically scrollable responsive surface whose standings action opens the real standings workspace. Depth-chart row drops now distinguish the upper and lower half of a target, supporting validated insertion both before and after it—including downward reordering—through normal GameCore/autosave and live-game adjustment paths.
- Display-aware shell layout is complete: windowed startup uses the active screen's usable area, the main viewport supports a 1024x576 minimum through large displays, and resize events recalculate rail width, header height, content gutters, compact typography, logo treatment, and navigation scrolling so right-edge controls remain reachable.
- The persisted waiver queue, user confirmation, conditional-release, and bounded CPU-claim slice is complete. Both waiver-market entry points submit claims without transferring a player; claim entries and attached releases persist until the period closes, then resolve in the original worst-to-best authoritative standings order. The market labels submitted, pending, and declined states. CPU clubs now submit at most four deterministic claims only for meaningful same-position upgrades they can fit through cap and roster rules, excluding the user and waiving club. A winning user opportunity becomes Action Required and blocks Continue until finalized or cancelled. Finalization revalidates and atomically commits the release/acquisition; cancellation preserves the rostered player and advances the saved original queue, where a valid next CPU claimant can complete the claim. Unclaimed players enter free agency. Broader CPU roster philosophy and tuning remain later balance work.
- The negotiated practice-squad formation slice is complete. Eligible free agents expose a deterministic one-year asking salary derived from ability, morale, and the real positional opportunity on the offering team. Both the roster-moves workspace and the shared Free Agency Make Offer dialog submit explicit terms instead of auto-signing the selection; the latter selects Active Roster or Practice Squad as a contract type where eligible. Insufficient offers are declined without moving pools, accepted terms persist on the appropriate contract, and cap, age, capacity, phase, transaction-history, UDFA-term, and permanent-promotion validation remain authoritative. Richer agent personalities and competing practice-squad offers remain later negotiation work.
- The first Draft Room / Draft Stage presentation slice is complete on the existing authoritative draft workflow. During Draft Prep, the workspace behaves as a war room with the real full order, the user's complete remaining owned-pick inventory, filterable prospect board, scouting inspector, and one explicit Start Draft action. Starting advances through the normal offseason transition, autosaves, and opens the live stage at the user's next pick. During the draft, the same workspace keeps the owned-pick inventory current and adds current-pick context plus a recent-picks league wire while retaining immediate board, order, scouting, and selection access. The current owned pick can be carried directly into the existing validated Trade Block / Finder; it generates no offers before submission, supports withdrawal, and refreshes the live draft after an accepted trade. When the user shops draft capital without requesting a return position, interested teams can now answer with their highest value-valid unused pick, enabling concrete trade-down responses through the same persisted offer flow. Production countdown/announcement animation, multi-asset pick packages, unsolicited clock offers, and CPU-to-CPU draft trades remain later work.
- The private Team Draft Board is now authoritative and persisted. The GM can add or remove available prospects from the shared scouting list, drag a prospect before or after another entry to maintain an explicit manual order, attach a private note plus Target/Avoid tag and a concise named tier, and carry up to 100 entries through save/load into Draft Prep and the live stage. Position and scouting-confidence filters preserve the underlying rank, while each visible entry and inspector derive a transparent High/Medium/Low team-need and role-path context from rostered players versus required starter slots. Drafted or unavailable prospects disappear from the rendered active board without altering hidden ratings or the separate public analyst/media rankings. Richer scheme-fit modeling remains deferred until the tactics model can support it honestly.
- The pre-draft reminder is complete. During the single Draft Prep day, the authoritative inbox presents a non-blocking reminder whose action opens the existing private Team Draft Board directly. It explains that no approval or change is required and does not create a redundant briefing screen, mutate the board, or advance into the draft.
- Draft-night analyst reaction foundation is complete. Each committed selection snapshots its stable pre-draft Analyst Board rank and records commentary only for a material public-board reach/value gap or a genuinely thin positional room. Reactions appear in the live recent-picks wire, remain explicitly attributed contemporary opinion rather than hidden truth or career prediction, and archive with the immutable draft recap.
- Draft selection presentation is now functional. Every selection committed between one user turn and the next is queued in true overall-pick order with the actual post-trade selecting team logo, pick number, player, position, and college. Top-ten selections receive expanded public context, later selections use a compact format, each card and the remaining queue can be skipped without altering draft state, and the user's short-announcement preference persists across saves and seasons. Production portrait assets, motion, audio, and exact timing remain part of the later art/animation pass.

### Engineering stabilization gate — September 2026

Do not begin the detailed snap-simulation, playbook, or 128-college expansion until the current native runtime is easier to
change safely. The first hardening pass removed the retired HTTP game-simulation branch, moved the active game-day simulation
command into a focused dashboard partial, corrected stale season-complete messaging, and made the test project reference the
real GameCore assembly. Direct rule tests now cover the contract-phase matrix, rookie-signing restriction, and regular-season
diagnostic filtering in addition to the existing franchise tests and full GameCore smoke run.

The September hardening tranche is now in place:

- The previously uncommitted feature work is checkpointed into reviewable local commits, with generated output excluded.
- The unused local HTTP client/interface and duplicate legacy profile store are removed. Native-only guards, endpoint calls,
  response-parsing branches, timeout helpers, and HTTP/RPC error paths have been deleted from the dashboard. Settings,
  inbox, markets, roster actions, training camp, history, save/load, live games, and player reports now have one native path.
- Postseason selection now applies the adopted NFL division and wildcard sequence from persisted regular-season schedules:
  head-to-head, division/conference/common records, strength of victory/schedule, combined scoring ranks, net points, net
  touchdowns, and a deterministic season/team draw in place of the real-world coin toss.
- Direct production-service tests now cover failed-batch atomicity, save migration, draft and waiver ownership, live-game
  idempotency, schedule-based tiebreak precedence, multi-team wildcard sweeps, Head Coach authority persistence, and college
  redshirt eligibility, transfer continuity, college recruiting, the college coaching carousel, and college box-score reconciliation. The focused suite contains 72 passing tests in addition to smoke QA.
- A headless `--gamecore-benchmark` command records elapsed time and managed allocation for one detailed game, a pro week,
  a 272-game pro season, and a projected 128-team/12-game college workload. The September 23 baseline on the current
  workstation is 0.061 ms and roughly 30 KiB per game for the current matchup engine; after adding persisted redshirt
  reserves, transfers, recruiting, coaching context, and box-score persistence in the college runtime, the current 768-game workload proxy took 32.6 ms and allocated 22.82 MiB. These are engineering baselines, not targets
  for the future snap engine.
- Game-day commands and developer commands now live in focused dashboard partials, while playoff ordering and benchmarking
  live in non-UI services.

The stabilization gate is complete. Controller extraction remains an ongoing maintainability rule rather than a blocker:
new workspace behavior must enter a focused partial or non-UI service, and touched legacy regions should move with it.
The first detailed pro snap foundation is implemented above. Each later simulation stage must retain direct tests and update the benchmark
baseline before expanding the detailed engine to all 128 colleges.

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

**Current state:** player statistics, careers, season history, retirements, draft archives, save migration, the derived record book, archived season awards, and read-only historical browsing now survive three automated seasons, including college-universe reset, draft-pool replacement, roster/cap checks, and save/load at each rollover.

- Player development history is now persisted as compact annual before/after overall snapshots with explanatory notes. The Team Development table uses that authoritative history for its trailing-season trend, movement, and notes columns, with safe empty-history handling for migrated saves.
- Pro-player traits foundation is complete: every generated pro player has a deterministic, persisted, read-only trait. The trait is migration-safe and available as an optional sortable Roster column; it currently provides context only and does not override ratings, rules, or game results.

## 3. Rules, contracts, and transactions

- Add contract expiry, payroll, cap space, cap validation, releases, waivers, IR, practice squad, and transaction log.
- Add free-agent offers/signings and Godot cap, transactions, and free-agent screens.

**Complete when:** all player movement is legal, persisted, explained, and cannot create duplicate ownership.

**Current state:** cap-aware signings, confirmed releases with authoritative financial previews, extensions, franchise tags, expirations, roster-capacity validation, explainable contract-phase rules, and persisted transaction records are implemented. Waivers, IR, practice squad, phase restrictions, duplicate-pool repair, and a transactions screen are implemented. Richer year-by-year accounting, restructures, rookie options, and tender mechanics remain.

## 4. Draft and complete offseason

- Add prospects, draft order, picks, selections, rookies, undrafted players, roster cuts, and preseason handoff.
- Build the persisted college-football universe that supplies the draft pipeline: college teams, players and eligibility, schedules, results, standings, rankings, statistics, awards, news, public boards, and postseason projections.
- Add basic scouting ranges, reports, combine/interview data, and a user draft board.

**Complete when:** a franchise can follow a persistent college season and prospect pipeline, finish a pro season, draft, sign players, set a legal roster, and start the next year.

**Current state:** seven-round order, user picks, CPU selections, persisted prospect scouting evaluations, immutable completed-draft recaps, rookie roster placement, rookie contracts, draft transactions, offseason cuts, a correctly timed Rookie Signing UDFA market with validated three-year contracts, and a saved new-season handoff are implemented. The college source is now a persisted deterministic 128-team competition with eight conferences and 12 games per team, plus player eligibility/redshirts, annual recruiting, a bounded transfer portal, a lightweight coaching carousel, standings/rankings, per-game player box scores, compact statistics, finalized awards, read-only leader tables, postseason projections/results, bounded development, news, public boards, deterministic injuries, and season archives; prospect inspection exposes only read-only college context. At the completed college-season/pro-draft boundary, deterministic declaration decisions add uniquely linked entrants to the pro draft pool or retain returning players, with public draft-stock and rationale context persisted through save/load and migration. The first Draft Room/Stage presentation and UDFA market are implemented; production draft animation, draft-day trade presentation, college logo assets, full college rosters, recruiting/transfer/coach balance depth, and deeper stage polish remain later slices. Training camp has a persisted position-focus decision, deterministic roster reports, protected position-battle outcomes, and roster player reports now include role/readiness plus live and archived statistics.

## 5. AI front offices and management depth

- Add team needs, valuation, strategy, GM personalities, AI draft/free-agency decisions, and validated trades.
- Add staff effects, player traits/chemistry, remaining call-up decisions, and deeper injury management.

**Complete when:** CPU teams build plausible legal rosters with inspectable rationale.

**Current state:** active roster, IR, practice squad, and waiver pools are distinct and persisted. Rules validate capacity, injury eligibility, active-roster activation, basic practice-squad eligibility, cap accounting, and several locked offseason phases. Game injuries have deterministic occurrence, recovery timing, availability-aware substitution, history, save migration, and rollover cleanup. Waiver claims, practice-squad signing/elevation, transaction history, and the combined persisted Trade Block / Finder have Godot controls backed by the same validated services. CPU teams now share deterministic availability-aware evaluation across retention, free agency, draft, UDFAs, camp cuts, waivers, practice reserves, and pregame emergency repair, with validated transactions, saved checkpoint idempotency, cooldowns, and inspectable rationale. Three normal-contract seasons pass structural diagnostics; they also produce bounded, concrete responses to an explicitly submitted user shopping package without executing a user transaction. Staff now has phase-gated hiring, persistence, retirements, a staff market, and small capped scouting, development, recovery, and simulation effects; autonomous CPU-to-CPU trades, counteroffers, and deeper roster strategy remain.

## 6. Balance and 1.0 polish

- Add morale, chemistry, traits, fan/media context, awards, Hall of Fame, historical browsing, onboarding, settings, accessibility, and migration polish.
- Balance simulation, development, contracts, AI, and offseason outcomes through long-run simulation.

**Complete when:** the 1.0 standard in `BLUEPRINT.md` is met and critical automated tests plus Godot playthroughs pass.

## Rules for every phase

- Fix broken core behavior before adding a feature layer.
- Prefer a narrow working version over a broad incomplete system.
- End each phase with focused tests, save/load verification, and a Godot smoke test.

## Visual-reference rebuild status

- **First-pass visual audit — complete.** `Visual References/AUDIT-STATUS.md` indexes the accepted working reference for every
  major 1.0 workflow. References marked provisional are deferred for a later workshop and do not block implementation. The folder
  now retains only the 77 manifest-listed screen references and three approved in-game placement examples; 210 superseded images and
  the chronological generation logs were removed so rejected concepts can no longer be mistaken for implementation direction.
- **League-logo pass — complete.** All 32 accepted hard-pixel team marks are installed in `Godot/Assets/team_logos`. Runtime
  identity changes include Miami Neon, Tennessee Copperheads, Las Vegas Spades, Los Angeles Condors, Cleveland Rhinos,
  Kansas City Wolves, Washington Wardens, and San Diego Riptide. Superseded logo drafts were removed from the reference archive;
  the 32 installed production marks remain authoritative under `Godot/Assets/team_logos`.
  Godot now defaults CanvasItem textures to nearest-neighbor filtering, logo controls explicitly preserve nearest filtering,
  and the headless asset smoke verifies all 32 expected marks load as valid textures. Interactive alpha/readability QA and
  helmet placement remain part of the future uniform/production-art pass.
- **Cross-system presentation contract — complete.** `BLUEPRINT.md` now maps each accepted surface group to its entry point,
  consequence boundary, persistence requirement, and blocked/empty-state explanation. This contract supersedes older roadmap
  claims that screen-specific concepts do not exist.
- **Game-level rebrand and production character art — deferred.** The current game name/logo remain placeholders. Portrait
  generation, modular player/coach models, uniforms, poses, and gameplay animation remain separate production workstreams.

## Immediate implementation sequence — visual vertical slice

Implement this route before broadening the visual rebuild:

1. **Dashboard shell and state binding**
   - Reconcile the current native dashboard with `home-dashboard-compact-v2.png`.
   - Bind the controlled team's saved name, colors, and installed logo everywhere; remove embedded reference-team identity.
   - Preserve working navigation, inbox, meaningful-stop advancement, configurable layout persistence, and validator messages.
   - **Implemented:** installed-logo binding is live in the header, navigation rail, standings rows, and GM profile tile. The
     accepted two-row composition is restored; standings, news, GM attributes, prospects, and category-based team leaders are
     bound to persisted or derived GameCore state rather than reference-only claims. The shell builds cleanly, passes all 31
     domain tests, and completes a headless Godot launch with no runtime errors or warnings.
2. **Roster and Player Profile**
   - Reconcile the dense roster table with `roster-compact-v2.png` and route selection to the accepted player profile.
   - Share one player-view model for role, availability, contract, known ratings, scouting estimates, live stats, career history,
     development, and contextual actions. Persist table preferences; domain state remains authoritative.
   - **Implemented for the first slice:** the roster's default dense columns and one-line squad/cap summary match the accepted information hierarchy.
     Player selection now opens a full-width profile route with an explicit return to the roster, and the profile no longer invents
     missing confidence, portrait, trait, or job-context claims. Staff-facing overall/potential ranges are deterministic across
     reloads, respond to the saved GM scouting attribute, carry confidence, and keep true simulation ratings out of the UI.
     The shared profile binds role/readiness, availability, contract, current and career statistics, known traits, development
     direction, injury history, and transaction history. Build, 31 domain tests, and the headless Godot smoke check pass.
3. **Depth Chart**
   - Use `depth-chart-table-v3.png` as the primary table-first workspace.
   - Preserve ordered roles, starters, user locks, availability substitution, seasonal context, and explainable invalid moves.
   - Do not rebuild the discarded oversized field diagram as the main interaction surface.
   - **Implemented for the first slice:** the discarded field diagram has been removed and the full-width assignment table now includes staff evaluation
     ranges/confidence, health, morale, position-relevant seasonal production, and contract context. Injured or otherwise unavailable
     players remain visible in their saved order, while starter assignment and completeness validation use available players only.
     Clickable Offense/Defense/Special Teams subheaders and live player search now filter the same authoritative table without
     changing depth state. Player rows now support validated same-position drag-and-drop ordering; the former Move Up/Move Down
     buttons are removed, and successful drops update GameCore and autosave. Position-group locks persist with the franchise,
     display directly in the table, and protect the GM's saved order from Auto-Fill while safely appending newly eligible players.
     Build, 31 domain tests, and the headless Godot smoke check pass.
4. **Game Day**
   - Enter from the scheduled-game stop and reconcile the observer with `live-game-observer-v1.png`.
   - Preserve score, clock, possession, play log, statistics, injuries, pause/resume, and a live depth-chart substitution route.
   - Apply lineup changes only through GameCore and only to subsequent legal snaps.
   - **Implemented and superseded by the September 24 snap foundation:** Watch Game creates a paused session with no future
     events. Each step resolves one real play through `ProSnapEngine`, updates score/clock/possession, and appends to the saved
     authoritative log. Pause/Next Play, retained GM decisions, staff recommendations, four coach authority domains, and
     saved-depth substitutions affect only later plays. Resume uses the same engine as quick simulation. Leaving pauses and
     saves; loading resumes the same RNG cursor and queued choice. Completion commits existing schedule/stat/medical/fatigue
     consequences once. Legacy sessions preserve their old playback and cannot rebuild history through substitutions.
     Result projections include overtime periods, actual scoring summaries, and expanded stat lines. Existing marks,
     field/log hierarchy, speed controls, and postgame route are retained. See the checkpoint above for exact rules, testing,
     performance, and deferred tactical work; the previous aggregate playback/rebuilt-future implementation is removed.
5. **Postgame and return loop**
   - Bind `postgame-hub-v2.png` to the immutable completed result and existing box-score/game-log data.
   - Apply result, statistics, injuries, fatigue, milestones, standings, news, and inbox delivery exactly once.
   - Return to a refreshed dashboard with no stale score, duplicate consequence, or lost team branding.
   - **Implemented for the first slice:** the small recap dialog has been superseded by a full-screen postgame hub using the
     accepted final-score header and Summary, Box Score, Team Stats, Player Stats, and Game Log navigation. Quarter scoring is
     derived from the authoritative playback timeline; comparison rows, leaders, player lines, injuries, and the complete log
     render only recorded GameCore data and explain genuinely empty legacy results. Returning closes the immutable result view
     and refreshes Dashboard state. Build, 31 domain tests, and the full headless GameCore smoke run pass.

**Complete when:** one saved career traverses Dashboard → Roster/Profile → Depth Chart → Game Day → Postgame, can close and
reload safely between stages, shows the controlled team's installed mark at every existing logo slot, retains UI preferences,
and produces identical action availability and validator explanations before and after reload. Finish with focused unit tests,
save/load coverage, and one Godot smoke test of the entire route.

**Verified runtime checkpoint:** the dedicated vertical-slice smoke scenario now traverses Dashboard → Roster → reordered and
locked Depth Chart → scheduled Game Day → paused incremental playback → save/reload → future-only live starter adjustment →
Postgame → save/reload → refreshed Dashboard in one career. It verifies exact midgame progress restoration, one immutable final
result, a final-score-consistent game log, preserved depth decisions, and no duplicate consequence application. The full build,
31 domain tests, and headless GameCore smoke run pass. Interactive display QA and production sprite art remain separate passes.
