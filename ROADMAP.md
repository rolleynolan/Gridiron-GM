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
- College postseason-projections foundation is complete: a compact, read-only playoff and bowl outlook is deterministically derived from current college rankings and records, surfaced in the existing College Football workflow, and remains stable through save/load. It schedules or simulates no postseason games.
- College player-development foundation is complete: a completed college season applies one small, deterministic, bounded development pass per active player. Explainable development history persists safely, migrates legacy saves to empty histories, and scouting exposes only the non-rating development note.
- College news foundation is complete: a compact, read-only feed deterministically derives result, ranking, and performance hooks from authoritative college state and surfaces them in the existing College Football workflow. It stores no UI-owned news state and uses no external feed or generated articles.
- College player-injury foundation is complete: deterministic college-game injuries have bounded weekly recovery, availability-aware college simulation, persisted injury history, and safe legacy defaults. College injury state remains separate from pro-player injuries and scouting presents only current availability context.
- Public college big-board foundation is complete: distinct deterministic analyst and media prospect rankings use public workout and declared-outlook context, remain separate from private scouting estimates, and are available in the College Football workflow.
- College bowl-playoff simulation foundation is complete: a completed college regular schedule deterministically resolves and persists two playoff semifinals, a championship, and two bowl games without altering pro scheduling, standings, or player pools.
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
- The workstation UI-overhaul sequence is complete. Read-only CPU front-office evaluation is now complete: deterministic team reports expose roster/depth needs, cap and expiring-contract pressure, age/potential profile, and current draft-pick context in the Trade Desk without making or persisting any roster decision. Reports safely regenerate from saved and rolled-over league state.
- College-universe foundation is complete at full league scale: new seasons create 128 persisted fictional programs across eight balanced conferences, a 12-game schedule with 768 unique matchups, initial draft-linked players, and compact development rosters. Returning underclassmen now retain identity, team, development/injury history, and archived season statistics across rollover while incoming freshmen fill vacated position slots; declared seniors/juniors continue to leave for the unique pro prospect pool. The universe owns eligibility, results, standings, rankings, statistics, awards, news, public boards, postseason, draft decisions, archives, and save/load continuity. The searchable, conference-filterable Full Rankings workspace now scales into a read-only team encyclopedia: selecting any program shows its authoritative season record, archived year-by-year program results, 12-game slate, results/upcoming opponents, statistical leaders, complete current development roster, and player career totals with no hidden ratings. Existing active 16-team careers are not rewritten midseason; their next generated college season adopts the full catalog. Full recruiting/transfer depth and college logo assets remain later slices.
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
- AI roster-management foundation is complete: during free agency and training camp, CPU teams deterministically repair only immediate starter shortages through the normal cap-, roster-, pool-, and phase-validated free-agent transaction path. Each signing retains its shortage rationale in the persisted transaction ledger, never touches the user roster, and is covered for repeatability, save/load, and both permitted phases. It does not automate trades, counteroffers, staff, schemes, or broader CPU roster strategy.
- College draft-pipeline lifecycle is complete: once the college schedule is complete and the pro draft opens, college seniors and deterministically evaluated juniors receive persisted declare/return outcomes. Declared players enter the pro draft pool through a unique college-player link with an explainable public decision and draft-stock context; returns remain outside that pool. The transition is idempotent, save/migration safe, and visible in the existing Scouting inspection without revealing hidden ratings or enabling college control.
- College-awards foundation is complete: a compact, read-only three-award slate is deterministically derived only after the completed college schedule supplies authoritative player statistics. Immutable award snapshots persist in the college universe, safely derive for legacy completed seasons on load, and are available in the existing College Football workflow. College news, simulated bowls, public boards, Draft Stage, recruiting, NIL, transfers, and user-controlled college management remain outside this slice.
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
  idempotency, schedule-based tiebreak precedence, multi-team wildcard sweeps, and Head Coach authority persistence. The
  focused suite contains 66 passing tests in addition to smoke QA.
- A headless `--gamecore-benchmark` command records elapsed time and managed allocation for one detailed game, a pro week,
  a 272-game pro season, and a projected 128-team/12-game college workload. The September 23 baseline on the current
  workstation is 0.061 ms and roughly 30 KiB per game for the current matchup engine; the 768-game college workload took
  32.5 ms and allocated 22.82 MiB. These are engineering baselines, not targets for the future snap engine.
- Game-day commands and developer commands now live in focused dashboard partials, while playoff ordering and benchmarking
  live in non-UI services.

The stabilization gate is complete. Controller extraction remains an ongoing maintainability rule rather than a blocker:
new workspace behavior must enter a focused partial or non-UI service, and touched legacy regions should move with it.
Detailed snap-engine design may proceed, but each new simulation stage must retain direct tests and update the benchmark
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

**Current state:** seven-round order, user picks, CPU selections, persisted prospect scouting evaluations, immutable completed-draft recaps, rookie roster placement, rookie contracts, draft transactions, offseason cuts, a correctly timed Rookie Signing UDFA market with validated three-year contracts, and a saved new-season handoff are implemented. The college source is now a persisted deterministic 128-team competition with eight conferences and 12 games per team, plus player eligibility, standings/rankings, compact statistics, finalized awards, read-only leader tables, postseason projections/results, bounded development, news, public boards, deterministic injuries, and season archives; prospect inspection exposes only read-only college context. At the completed college-season/pro-draft boundary, deterministic senior and junior declaration decisions add uniquely linked entrants to the pro draft pool or retain returning players, with public draft-stock and rationale context persisted through save/load and migration. The first Draft Room/Stage presentation and UDFA market are implemented; production draft animation, draft-day trade presentation, college logo assets, full college rosters, recruiting/transfers, and deeper stage polish remain later slices. Training camp has a persisted position-focus decision, deterministic roster reports, protected position-battle outcomes, and roster player reports now include role/readiness plus live and archived statistics.

## 5. AI front offices and management depth

- Add team needs, valuation, strategy, GM personalities, AI draft/free-agency decisions, and validated trades.
- Add staff effects, player traits/chemistry, remaining call-up decisions, and deeper injury management.

**Complete when:** CPU teams build plausible legal rosters with inspectable rationale.

**Current state:** active roster, IR, practice squad, and waiver pools are distinct and persisted. Rules validate capacity, injury eligibility, active-roster activation, basic practice-squad eligibility, cap accounting, and several locked offseason phases. Game injuries have deterministic occurrence, recovery timing, availability-aware substitution, history, save migration, and rollover cleanup. Waiver claims, practice-squad signing/elevation, transaction history, and the combined persisted Trade Block / Finder have Godot controls backed by the same validated services. CPU teams now make a narrow deterministic free-agency/training-camp repair for immediate starter shortages only, using the same approved signing path and persisted rationale; they also produce bounded, concrete responses to an explicitly submitted user shopping package without executing a user transaction. Staff now has phase-gated hiring, persistence, retirements, a staff market, and small capped scouting, development, recovery, and simulation effects; autonomous CPU-to-CPU trades, counteroffers, and deeper roster strategy remain.

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
  major 1.0 workflow. References marked provisional are deferred for a later workshop and do not block implementation.
- **League-logo pass — complete.** All 32 accepted hard-pixel team marks are installed in `Godot/Assets/team_logos`. Runtime
  identity changes include Miami Neon, Tennessee Copperheads, Las Vegas Spades, Los Angeles Condors, Cleveland Rhinos,
  Kansas City Wolves, Washington Wardens, and San Diego Riptide. Original runtime marks are retained in the branding audit.
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
   - **Implemented for the first functional slice:** every newly simulated game now produces a deterministic, ordered GameCore playback timeline containing
     quarter/clock, possession, down-and-distance, field position, play description, scoring state, and score after each event.
     The timeline is part of the authoritative box score, survives save/load, and is exposed to the Godot result payload so the
     observer can replay it without inventing a second outcome. `Watch Game` now opens a full-screen observer modeled on the
     accepted scorebar/field/play-log/control hierarchy, with pause/resume, 1x/2x/4x speeds, all-play/key-play filtering, a live
     running score and field-position view, box-score access, and explicit exit to postgame. Deterministic game injuries enter
     the same playback stream as medical-timeout events. Both observer and postgame scorebars use the installed team marks with
     nearest-neighbor filtering. Game resolution and recorded player lines now use each team's saved top available depth-chart
     players rather than silently selecting the highest-rated roster members. The GameCore incremental-session foundation now
     starts paused without committing a result, advances one authoritative event at a time, persists midgame progress, records
     paused depth-chart adjustments without rewriting played events, rebuilds only the unplayed outcome from the changed lineup,
     and commits injuries/statistics/fatigue/schedule state exactly once at completion. Watch Game now uses that incremental
     session directly rather than atomically completing the game before playback. Live Adjustments pauses the timer, routes to
     the familiar full depth-chart table, validates drag/drop or Set Starter through the live session, autosaves the revised
     future, and provides an explicit Return to Live Game action. Full-game simulation is refused while a live session exists.
     Production sprite animation and broader tactical package adjustments remain separate art/simulation workstreams.
     Build, 31 domain tests, and the full headless GameCore smoke run pass.
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
