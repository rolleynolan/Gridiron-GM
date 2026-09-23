# Gridiron GM 1.0 Blueprint

## Vision

Gridiron GM is a single-player American-football franchise simulation. At 1.0, a user can take control of a team, make meaningful football and business decisions, simulate seasons, and build a persistent multi-year league.

Godot and C# are the complete runtime. Simulation, rules, saves, and interface live in one application. Python files in this repository are historical reference only and are never part of the shipped game.

## Player journey

1. **Create or resume a GM career.** Choose or create a GM profile, appearance, and management attributes; select a Standard
   or Generated world, difficulty, and team; then name and confirm the career. Loading resumes the saved career and calendar.
2. **Take over an established franchise OR start unemployed** Review the owner, expectations, staff, roster, contracts, finances, and schedule
   in a living fictional league with generated player careers, records, and honors. Player evaluations reflect scouting
   knowledge; staff, owner, and agent traits are visible. Optionally start unemployed and wait for an opportunity to arise, or send your application to teams and hope one gives you      a chance.
3. **Set responsibilities and priorities.** Hire staff personally, choose supported coaching/scouting delegation, review
   depth charts and tactics, and set training drills and intensity. Use ongoing pro scouting, targeted assignments, college
   assignments, named shortlists, and the draft board to plan ahead.
4. **Manage the football week.** Read inbox reports and news, respond to players and agents, manage contracts and roster
   needs, and review preparation, injuries, readiness, and staff observations. Development and regression occur throughout
   the calendar; new knowledge reaches the GM through evaluations rather than visible XP.
5. **Advance when ready and follow games.** Continue only on the user's instruction, stopping for meaningful events,
   decisions, games, and deadlines. Watch or simulate games while coaches call plays; manage permitted personnel and tactical
   changes. Work through preseason, the regular season, the hourly trade-deadline day, and any playoff run.
6. **Review the season and the GM's future.** When the team's season ends, meet the owner to review agreed expectations,
   employment status, and staff, then negotiate next-season goals and discuss resources or owner-controlled projects if
   retained. League playoffs and other teams' calendars continue independently. Follow All-Star and All-Pro honors, the
   awards show, championship recap, and subsequent Top 100 releases at their scheduled times.
7. **Build the next roster through an overlapping offseason.** Use the roster-planning guide, contract decisions, free
   agency, trades, and staffing windows. At the Combine, confirm interview/evaluation selections and conduct interviews or
   delegate them to the Head Coach. Allocate daily pro-day staff among colleges, review findings, and prepare the draft board.
8. **Draft and prepare for the next season.** Make draft selections and trades, pursue UDFAs, invite rookie-minicamp tryouts,
   and assess camp/preseason reports. Manage position battles, confirm roster cuts and any winning waiver claims, form the
   practice squad, and resolve roster legality before opening week. Receive the Hall class in the inbox after the league's
   first preseason game is simulated.
9. **Continue a lasting career.** Carry forward decisions, relationships, player development, finances, and league history.
   Pursue new GM jobs only while unemployed after a qualifying departure; voluntary GM retirement permanently ends that
   playable career. Across seasons, follow record chases, player retirements and rare returns, ownership changes, Hall classes,
   and team honors while building the franchise's history.

## Blueprint audit checkpoint

Checkpoint updated September 16, 2026. This tracks design discussion, not implementation completion.
Continue the audit in player-journey order, covering functionality, presentation, decisions/consequences, and persistence/rules
within each section. Ask small batches of related questions. Record side ideas in their relevant existing sections, then
return to the active section; defer tuning and low-level edge cases unless they are necessary to settle the current design.

- **Season/offseason flow recorded:** awards, All-Star/All-Pro honors, Top 100 releases, championship recap, owner reviews,
  staffing, roster planning, negotiations, free agency, draft preparation/execution, UDFAs, minicamps, training camp, cuts,
  waiver confirmations, and practice-squad formation. Core workflows have been discussed; this is not a claim that all details are final.
- **Staffing discussion recorded:** owner-meeting staff review, interview presentation, GM-controlled hiring, competing offers,
  contracts, promotions, interview permissions, and vacancy consequences. Detailed tuning remains deferred.
- **Time-advancement direction accepted:** user-initiated progression to the next meaningful event or attention point;
  quiet stretches may span days while busy periods stop sooner, including within a day. Optional fixed-step/date controls remain undecided.
- **Advancement stops accepted:** routine updates accumulate; offers, required decisions, meetings, and games stop progression.
  Optional stops are configurable, while mandatory decisions remain protected.
- **Additional main design passes recorded:** opening-week and weekly roster flow; DNA/XP, training, mentoring, injuries,
  rare returns and two-way players; ownership, facilities and finances; promises, relationships and media; generated history,
  records, Hall of Fame and team honors; trade-deadline pacing, trade workflows, and CPU direction. These are no longer
  unreviewed sections, although specific open details remain in their text.
- **Current stopping point:** the broad feature interview and core scouting-capacity decisions are recorded. The rules
  findings and draft content package below are ready for review; remaining verification is explicitly listed there.
- **Consolidation review recorded:** reconciled stale deadline behavior, award timing, two-way rules, cap projections,
  pre-acceptance versus exclusive contract confirmation, college/player continuity and development timing, staff effects,
  coach observations during scouting vacancies, and Hall of Fame cross-references. These are documentation corrections,
  not claims that the current implementation supports the full design. Remaining work is grouped in the bounded register below.
- **Remaining pass 2 — rules and catalogs:** verify calendar/eligibility/contract rules and define required catalogs such as
  training activities, interview questions, owner traits and facility types. Propose coherent defaults for review instead of
  asking the user to invent every value. Leave numerical balance coefficients for simulation testing.
- **Presentation pass reconciled September 15, 2026:** the first-pass screen audit covers every major 1.0 workflow.
  Provisionally accepted screens are deliberately deferred for a later refinement workshop and do not block the next phase.
  `Visual References/AUDIT-STATUS.md` is the authoritative implementation-facing reference index; older pending labels in the
  chronological generation log are historical. Continue with cross-system/rules reconciliation and the implementation handoff.
- **Visual implementation handoff recorded September 16, 2026:** the accepted screen inventory now has a cross-system contract
  covering entry points, consequences, persistence, and blocked states. All 32 team identities have accepted hard-pixel marks
  installed in the runtime. The next implementation target is the bounded Dashboard → Roster/Player Profile → Depth Chart →
  Game Day → Postgame vertical slice is now verified in the runtime. The combined Trade Block / Finder is the next completed
  GameCore-backed workflow: submitted packages and concrete responses persist without restoring the discarded separate Trade
  Finder or Active Offers surfaces. Provisional screens, the game-level rebrand, copy editing, and the modular portrait and
  animation pipeline remain later work and do not reopen the completed first-pass screen audit.

### Consolidated remaining audit register

This register groups the remaining work into six review batches. Detailed open items in their relevant sections belong to
these batches, not an expanding feature interview. Recommendations are proposals until reviewed; do not silently treat a
suggested default as an accepted decision. Resolve questions already answered by this blueprint without asking again.

1. **Calendar and recognition:** starting year is 2026 for the current baseline, with new careers intended to use their real-world creation year as specified below. Starting record values use the preceding completed NFL season (through 2025 for a 2026 start), with the reference dataset preserved per save. All-Stars are announced after the conference round
   in the week before the championship; All-Pros at the awards show; the Hall class through an inbox message after the league's first preseason game is simulated, without a cinematic.
   Top 100 ability uses the accepted end-of-regular-season snapshot,
   matching its production window. Verify remaining league windows, deadline-day hours, and Hall ballot details against
   one explicitly adopted rules baseline. Keep the accepted five-week ranking reveal and championship-week awards show.
2. **Contract and roster edge cases:** declining a GM extension preserves the current contract; expiry without renewal leads
   to unemployment. Financial pressure limits budgets/upgrades and affects owner expectations; serious ownership financial
   problems are rare and may contribute to a sale, with no routine bankruptcy spiral. Verify tags/options, restructures, retirement-return
   rights, waivers, practice squads, and promotion contracts. Retired numbers remain unavailable until explicitly unretired.
   Emergency injury coverage follows the saved depth-chart order, including suitable cross-position emergency entries at its
   lower ranks, with out-of-position penalties and no permanent secondary eligibility.
   Preserve the explicitly fictional waiver-confirmation workflow.
3. **Scouting and staffing capacity recorded:** 15 interviews, 30 combined evaluations, Confirm consumes slots, Combine
   attendees participate, optional Head Coach interview delegation, and 10 unsigned rookie-minicamp tryouts. Pro days use
   a daily numerical staff allocation. Pro scouting provides routine coverage plus targeted assignments; college scouting
   requires assignments, which may continue until canceled. Remaining booking cutoffs and coverage amounts are rules/tuning work.
4. **Required content catalogs:** assemble the training activities, prospect/staff interview questions, owner and agent
   personalities, mentoring/trait interactions, supported facility projects, conversation choices, and team honors within
   the accepted feature scope. Audit attribute/role coverage and the existing trait catalog. Present coherent catalogs for
   review; leave XP coefficients, injury probabilities, voting weights, construction costs, and similar balance values for tuning.
5. **Presentation and settings:** review the existing navigation screen by screen, covering contracts/offers, scouting and
   comparisons, draft and pro days, training/staff feedback, owner meetings/facilities, game day, hourly trade deadline,
   news, awards/history, and their empty/blocked states. Set difficulty behavior, optional advancement controls, news retention,
   and deferred cinematic/layout details here. Preserve hidden XP, scouting estimates, and inferred CPU direction throughout.
6. **Final handoff:** check cross-system consistency and that each accepted workflow has an entry point, consequence,
   persistence requirement, and explanation when blocked. Separate target design from current implementation, then update
   implementation sequencing in ROADMAP.md when that handoff is performed. Do not call the audit or implementation complete
   while the preceding review batches still contain unresolved decisions.

Current discussion: review the Rules findings and proposed content package near the end of this blueprint. Draft catalogs
are not yet approved design. Finish the specifically listed rule verification before claiming a complete implementation handoff.

## GM identity and franchise starts

Players create and maintain a reusable GM profile outside individual franchise saves. The profile records the GM's
name, selected management attributes, and character-design choices. Starting a new franchise selects an existing
profile or creates one; the profile is copied into the franchise as that save's GM record, so later changes to the
reusable profile do not rewrite an existing career.

GM attributes add modest, Football-Manager-style management advantages without becoming a path to guaranteed success.
The initial attributes are **Negotiation** (small contract-offer and counteroffer advantages), **Player Management**
(small retention and happiness advantages), **Scouting Judgment** (clearer player and prospect information), and
**Leadership** (small staff and team-culture advantages). Ratings, contracts, player personalities, team situation,
cap space, and gameplay results remain the primary drivers. Attributes never alter a player's underlying talent or
directly determine game results. Every modifier is capped, shown when relevant, and designed so a low-rated GM can
still succeed through sound decisions while a high-rated GM can still fail through poor decisions.

Every new franchise must choose a roster source as part of the combined Team & Roster selection screen:

- **Standard roster:** a fixed, versioned world-generation seed shipped with the game. The deterministic world
  generator creates the same fictional pro-player, coach, college-team, and college-player database, including ratings, contracts,
  histories, and draft classes, for every new standard-roster franchise. Each save receives its own generated copy.
  Two saves started with the same standard seed and generator version are identical at kickoff. Their state diverges
  only through decisions and simulation after the save begins; any newly introduced players are generated for that
  save.
- **Generated roster:** a new world seed creates the full fictional pro-player, college-team, college-player, and coach database when the save is
  started. Each generated franchise stores its seed and generated data, and is expected to differ from other generated
  franchises while remaining reproducible from its own saved state.

Choosing Generated from Team & Roster runs this one-time world build before team selection is finalized. Team & Roster remains
visible in a simple blocking loading state until all 32 pro rosters, contracts, cap positions, owners, starting histories, and
other required populations are complete and validated; there is no separate world-generation screen. It then derives each
team's competitive outlook from that completed world and populates the team/roster previews. Team identities and league rules
remain fixed; generated people and seeded world context may vary. The generated seed and completed data persist through Review
and into the career without rerolling.

The save metadata records GM-profile identity, roster source, standard-world seed, world-generator version, and world
seed. The UI explains the choice and never modifies the Standard Roster definition during play.

## Architecture

- **Domain:** deterministic C# models and services own the pro league, college universe, teams, players, rules, schedules, games, and transactions.
- **Presentation:** Godot screens render state and request actions; they never duplicate rules or directly mutate domain state.
- **Persistence:** versioned JSON-safe saves preserve complete league, GM-profile snapshot, roster-source metadata,
  standard-world seed, world-generator version, and random-seed state, with migration for supported old saves.
  Reusable GM profiles and the immutable Standard Roster definition are persisted separately from franchise saves.
- **Events:** typed events such as `GameEnded`, `WeekAdvanced`, `SeasonEnded`, `PlayerInjured`, `ContractSigned`, and `DraftPickMade` connect systems without hidden dependencies.
- **Validation:** rules approve every mutation before it commits. No UI or AI system bypasses cap, roster, eligibility, or phase rules.

## Application flow, careers, and settings

### Boot and cinematic

The current **GridironGM / Gridiron GM** name and game-level logo are working placeholders. A later rebrand pass will replace them consistently across the executable identity, boot/cinematic assets, title menu, UI labels, save metadata, credits, documentation, and distributable branding without changing the accepted title-menu composition by default.

The shipped game launches directly into a polished boot experience, never a development panel or test scene. It initializes
global settings, keyboard/mouse input bindings, audio, reusable GM profiles, the local career index, and required game content
before it enables actions. A short, skippable football-themed cinematic plays before the title menu. It may use pixel-art visuals,
music, and sound, but honors saved volume, reduced-motion, and flashing-content settings. The game falls back gracefully to a
static title background if cinematic content cannot be presented. Initialization and loading feedback uses plain language; it
never presents a misleading loading state or silently discards data.

### Title menu and navigation

The title menu is the game's normal opening screen after the cinematic. It contains **Continue Career**, **New Career**,
**Load Career**, **GM Profiles**, **Options**, **Credits**, and **Quit Game**. Continue Career loads the most recently played
valid career and is visibly unavailable, with an explanation, when none exists. Credits are a small, complete acknowledgement
screen. Every menu and sub-screen supports mouse and keyboard operation; controller support is outside 1.0 scope.

All navigation has a consistent Back action, preserves the user's focused item when returning to a menu, and makes the
consequence of destructive actions explicit. The user can create any number of independent careers. A career is a separate
universe and save file with its own league state, history, settings, world seed, and GM snapshot; it is presented as a distinct
"GM career" in the interface without imposing an artificial career limit.

### Career library, saves, and recovery

The career library presents each career with its controlled team, GM name, current year and phase, last game/result,
creation date, last-played time, and roster source. It supports load, duplicate, and deletion; it does not expose custom
career names, a permanent save-health column, or manual-save/export actions before a career is loaded. Selecting a career
shows its latest autosave, latest manual save, and rolling-backup count. Automatic saves occur at safe lifecycle points and
use atomic writes plus rolling backups, so a crash or power loss cannot replace the last valid save with a partial file.

The game does not silently delete a damaged save. It distinguishes a save that is unavailable, migratable, recoverable from a
backup, or corrupt; explains the state in player language; and offers only safe recovery actions. Career deletion requires a
confirmation that names the exact career and explains that the action removes its local saves and backups.

### Reusable GM profiles

GM profiles are created and managed outside careers. Each profile stores its name, character-design choices, management
attributes, and metadata. Starting a career selects a profile or creates one, then copies it into the career as an independent
GM record. Editing or deleting a reusable profile never rewrites, damages, or removes an existing career: that career retains
its own GM snapshot and profile-origin history. A user may delete a reusable profile after an explicit confirmation; it simply
becomes unavailable for future career creation.

### Global options and career settings

Global Options are available from the title menu and inside a career. They apply across the application and include display and
performance options, audio channels, keyboard/mouse bindings, UI scale, text readability, contrast and color-vision support,
reduced motion, flash/screen-shake controls, confirmation behavior, and save/backup location information. Display changes
provide Apply/Revert protection and automatically revert after a countdown if they leave the game unusable. Resetting global
options never deletes careers or GM profiles.

Every career also has a distinct **Career Settings** / commissioner-settings screen, available from within that career. It
contains clearly described, persisted simulation and world-customization controls that let the player tune their separate
universe to their preferred experience: for example prospect-generation strength and other approved gameplay sliders. Each
setting states its current value, effect, valid range, whether it applies immediately or only to future generated content, and
whether changing it may affect competitive balance or historical comparability. These settings affect only the current career,
are never silently changed by updates, and cannot bypass save integrity, phase rules, or data validation.

### Presentation and accessibility baseline

The interface uses a professional 32-bit pixel-art visual system with a restrained nostalgic character. It must preserve clear
information hierarchy, strong team identity, readable dense data, and contemporary usability rather than imitating an old game
at the expense of clarity. Screen-specific visual references will be rebuilt for this direction. Color is never the sole signal
for selection, injury, warnings, cap status, or a blocked action. Empty, loading, saving, error, and confirmation states are
intentional parts of the UI and always explain what happened and what the player can do next.

The user prefers the familiar management-interface direction of OOTP and Football Manager: compact, information-rich
screens, clearly grouped tiles where useful, and easy access to relevant actions. Favor modest text/control sizing, compact
table rows, restrained padding, and multiple useful panels visible together over oversized cards occupying half the screen
for little information. Preserve legibility and the existing accessibility/scaling options; compact does not mean unreadable.
These are interaction and density references within the game's established visual direction, not a requirement to copy
another game's artwork or exact layout.

**League-logo pass completed September 16, 2026:** all 32 fictional pro teams now have accepted hard-pixel marks installed in
the runtime. Preserve the approved identities and source references. Implementation must normalize import settings and export
sizes without redesigning the marks, then verify alpha, nearest-neighbor scaling, helmet placement, and readability in team
selection, navigation, standings, schedules, news, scoreboards, tables, and other compact-icon contexts.

Right-clicking a player or coach opens a contextual action menu wherever that person appears in a supported list or tile.
Offer relevant actions such as opening the profile, managing a shortlist or scouting assignment, discussing a contract,
starting a conversation, or accessing eligible roster/staff decisions. Respect ownership, role, phase, and all existing
permissions; explain unavailable actions. Menus route to the normal review/confirmation workflows and never immediately
execute a consequential transaction. Make the same actions discoverable through a visible Actions control or the profile,
so right-click is a convenient shortcut rather than the only way to find a function.

For the screen-layout audit, present concrete visual alternatives using this compact direction rather than asking the user
to imagine a layout from abstract density choices. Exact sizing, tile arrangements, and action-menu organization remain
subject to visual review; no specific mockup or layout is approved by this preference alone.

Visual review September 10, 2026: the user liked the compact Home v2 and Roster v2 directions saved in Visual References.
Use their density, dark framing, restrained team accents, and contextual action-menu treatment as the working visual
direction. Generated example values and omitted controls are not authoritative; retain the blueprint's navigation, knowledge,
and confirmation rules. The images and generation prompts are indexed in Visual References/generated-concepts.md.

### New-career creation, GM identity, and difficulty

Creating a career follows a clear, reviewable sequence: choose a reusable GM profile or create one through the profile
workflow, which includes the GM's shoulders-up pixel-art portrait and management-attribute allocation; choose difficulty;
select a team and review that team's roster in the same screen; review and confirm the career; then enter the franchise. The
combined Team & Roster screen also presents the Standard or Generated roster-source choice and shows which roster context is
being selected. The main career wizard therefore uses the steps **GM Profile**, **Difficulty**, **Team & Roster**, and **Review**. Franchise-world settings
are not presented as a setup checklist. After creation, they are changed from the in-franchise Settings tab, with the
exception of the dedicated difficulty choice during career creation.

GM character design is a professional, expressive shoulders-up portrait rather than a full-body avatar. It supports the
appropriate appearance choices for that presentation and is used consistently on profile, owner, and career-management
surfaces. Visual options follow the game's 32-bit pixel-art direction.

The four starting management attributes are Negotiation, Player Management, Scouting Judgment, and Leadership. They have a
1-10 rating scale and use a 16-point point-buy allocation at career creation. Their effect curves are deliberately modest at
low and middle ratings and become more meaningful near 10. No attribute produces a guaranteed outcome, bypasses a game rule,
reveals hidden player truth, changes a player's underlying talent, or directly determines game results. The UI states both an
attribute's present effect and its capped maximum effect whenever that information helps a decision.

Team & Roster selection uses a complete grid of all 32 teams without a search field or pagination. Selecting a team updates an adjacent
detail view before confirmation, showing its identity, the roster being selected, current competitive strength, roster and cap
snapshot, owner, financial position, and expected first-season context. The roster-source choice is visible on this same screen;
there is no separate Roster wizard step. Use 2026 as the current starting-year baseline. The intended default for new careers is the
real-world year when the career is created; persist that starting year so loading an existing career never resets its calendar
to the current real-world year. Calendar dates and generated history must align with the saved starting year. This does not
automatically update an existing save's versioned NFL record dataset or rules baseline. New careers use the record cutoff
specified in Established league history and career recognition below.
A player may create as many independent career
universes as desired.

Difficulty is a transparent career rule, not a hidden simulation bias. It can change AI decision quality, information
advantages, management pressure, and other clearly stated challenge conditions, but it must not rig individual game results
or cause football simulation to treat the user-controlled team differently from an equivalent AI team. Its exact settings,
effects, and whether it can later be altered are always visible in the career's Settings tab.

### Owner expectations, job security, and firing

After a new career loads into the franchise, the controlled team's owner sends a welcome email. This establishes the initial
expectations using the team's competitive strength, roster situation, and financial position rather than arbitrary universal
win targets. The email explains the owner's assessment, the specific short- and longer-term expectations, and why they matter.

Owners and job security are real 1.0 systems. Performance against stated expectations, financial stewardship, roster and
cap management, and other explicitly communicated franchise outcomes affect the GM's standing. The player receives clear
feedback and warnings before an avoidable firing; a firing must be represented in career history and never occur as an
unexplained surprise. Firing does not end the career or reset its league universe. The GM enters an unemployed state in the
same persistent league and must wait for a suitable team opportunity to open before returning to work. The career records the
departure, unemployment period, and subsequent hiring history.
GM firings occur only after the affected team's season has ended. For staffing purposes, non-playoff teams enter their
offseason after the regular season concludes; playoff teams enter it upon elimination, or after the championship for its
two participants. During the team's active season, poor performance may trigger owner feedback, warnings, and
hot-seat status, but never immediate dismissal. Resolve any firing at an offseason owner review, using the communicated
expectations and documented performance concerns.

#### Annual owner review and offseason planning meeting

Each team's staffing offseason begins with a dedicated meeting between the employed GM and the owner after its season ends.
Non-playoff teams can hold this review while the playoffs proceed; playoff teams wait until elimination, with the championship
participants holding theirs after the final game. The league championship recap is not a prerequisite for other teams' reviews.
First revisit the expectations established for the completed season, compare each with actual outcomes, and explain the
owner's assessment in the context of team performance and franchise stewardship. Preserve the original expectations so
the review cannot silently replace them with hindsight-based targets.

The GM has a persisted employment contract with a defined term. In a GM contract year, the review explicitly addresses
whether the owner offers an extension or ends the GM's employment, with reasons consistent with the communicated assessment
and job-security feedback. An extension offer is presented to the GM rather than silently accepted. Record any completed
contract change or departure in career history. If the GM is fired, end the team-planning portion of the meeting and enter
the existing unemployment/job-opportunity flow in the same league.

If retained, the GM and owner discuss new short- and longer-term expectations for the upcoming season. The GM can raise
specific priorities or requests, and the owner responds with an explained acceptance, rejection, or alternative. Clearly
distinguish proposed requests from agreed expectations and commitments; persist the agreed outcome for subsequent reviews.
Expectation negotiation includes give and take. For example, the GM may request a rebuilding year with a lower immediate
win target, while the owner counters with cost-saving requirements, exceptional draft performance, or other explicit
franchise objectives. Owners may accept, counter, or reject a proposal based on franchise circumstances and their priorities;
negotiation does not guarantee easier expectations. Show the full proposed package, including relaxed targets and added
commitments, before the GM agrees. Every agreed condition has a defined evaluation measure and deadline. Cost-saving goals
distinguish cash spending from salary-cap usage, and draft-performance goals use an explained, observable evaluation standard
and suitable assessment period rather than hidden player potential or an unexplained instant draft grade. Preserve the
negotiated package for later owner reviews, including longer-term commitments that extend beyond the next season.
This give-and-take applies to all owner goals, not just rebuilding. If the owner demands a championship, for example, the GM
may ask for an owner cash injection or other support needed to pursue it. The owner can approve, counter, or deny the request;
requested support is not guaranteed. Show whether the competitive target remains unchanged after a denial, and distinguish
approved funding from a request still under discussion. A cash injection increases the applicable franchise cash or spending
budget; it does not create salary-cap space or bypass contract, roster, or other league rules. Record the amount, intended
use, availability timing, and any attached conditions for approved funding so it has a concrete finance-system effect.
The owner also asks for the GM's feedback on potential stadium, facility, and other owner-controlled upgrades. The GM can
express needs and preferences, while approval, funding, and project decisions remain owner-led and reflect franchise
conditions. Record feedback and any resulting owner commitments for relevant owner, finance, and news follow-up.

Present the meeting as an ordered conversation with supporting expectation/result summaries and a final record of decisions,
contract status, next-season expectations, requests, and upgrade feedback. Declined extensions follow the GM job-market rules
below. Exact GM contract terms and negotiation options, the request/upgrade catalog, and the owner's decision weights remain
to be defined in the audit.

#### Owner identity, personality, and ownership changes

Owners have persistent identities and exactly four personality trait dimensions: Patience, Spending, Ambition, and Involvement.
All four are visible from the start, without discovery or fog of war. They affect time allowed for results, funding willingness,
competitive expectations, and how closely the owner participates in decisions. Decisions combine these tendencies with actual owner resources and franchise circumstances. Owner
dialogue and feedback communicate these priorities consistently rather than changing personality arbitrarily for each offer.
Present owner personality through readable profile descriptions and tendencies rather than exact numerical ratings for
these four dimensions. Negotiation, project preferences, responses to pressure, and continuity decisions arise from these
traits and circumstances rather than additional owner trait categories. Exact effect strengths remain to be balanced.

Owner priorities may evolve following sustained success, failure, or material financial changes while retaining a recognizable
core personality. Ground shifts in the franchise's history and circumstances rather than arbitrary changes between meetings.
Communicate meaningful shifts through the profile, owner feedback, and subsequent expectation discussions. Changing priorities
does not retroactively rewrite agreed goals or silently cancel existing commitments; use the established review and negotiation
flows for any permitted revisions.

Franchise ownership can change occasionally through a sale or an owner's death. After a death, the team may be inherited
by a family member or become available for sale; neither outcome is universal. An inheriting family member has their own
personality, traits, and priorities rather than automatically copying the deceased owner's behavior.
Resolve the resulting purchase or succession
into a new controlling owner with their own identity, resources, personality, and traits. Ownership changes preserve the
franchise, league, roster, player contracts, finances, and history rather than recreating the team. Record outgoing and incoming
owners, timing, and the reason for the transition in franchise history, and communicate the change through league news and
an introduction to the new owner for the affected GM.

An ownership change alone cannot justify firing the user GM. The incoming owner gives the incumbent a chance under clearly
communicated expectations rather than replacing them simply to appoint their own hire. A dismissal at the transition is
permitted only during the offseason and when documented poor performance had already placed the GM on the hot seat before ownership changed, with
the existing feedback and warnings preserved. The transition must not manufacture a retroactive hot-seat status or use
newly imposed expectations to judge past performance. This protection does not erase existing performance concerns or
prevent later performance-based dismissal under communicated expectations.
If ownership changes during the season, even an already struggling GM remains employed through the season; any dismissal
decision waits for the offseason owner review.

New ownership may change future priorities, patience, funding willingness, and upgrade plans. Explicitly review inherited
GM expectations, employment terms, funding commitments, and active projects; never silently rewrite completed-season targets,
erase existing obligations, or treat previously approved support as though it never existed. Any permitted changes must be
communicated through the relevant owner, contract, and finance flows. Ownership-event frequency, sale/succession mechanics,
transition timing, and rules for revising inherited commitments remain to be defined in the audit.

Potential franchise sales may attract media rumors before a confirmed announcement. Distinguish reported interest or talks
from an agreed/completed sale; rumors do not guarantee a transaction or change the controlling owner. Use the existing media
knowledge and story rules so confidential negotiations are not automatically public.

When new ownership takes control, the incumbent GM receives an introductory owner meeting covering the incoming owner's
priorities and the franchise's existing agreements, expectations, employment terms, funding commitments, and projects.
Clearly separate inherited commitments from proposed changes. Apply the established protection against ownership-change-only
firing and the team-specific offseason dismissal rules; an in-season introduction does not permit an immediate GM dismissal.
Persist the discussion and agreed outcomes through the same owner-meeting records used for annual reviews.

#### GM job market and hiring

The user GM may pursue another team's job only while unemployed. An employed GM cannot apply, interview, or accept a rival
team's offer while under contract. The GM can voluntarily resign before the contract ends, including to retire or for other
occasional personal/career reasons. Record voluntary resignation, retirement, contract expiration, and firing as distinct
departure types. A non-retirement resignation enters the existing unemployment flow; it does not guarantee another job.
Declining an extension does not constitute resignation or end the current contract. The GM continues under its existing
terms until it ends, unless a separate departure occurs under the existing rules. If the contract expires without a new
agreement, record contract expiration and move the GM into unemployment, with access to the normal job market. Declining
alone does not unlock rival-team applications while the GM remains employed.
GM retirement permanently ends that GM's playable career: a retired GM cannot unretire, apply for jobs, or receive employment
offers. Preserve the completed career history and clearly communicate permanence before the retirement decision is finalized.

An unemployed GM can both receive unsolicited offers and actively apply and interview for available GM openings. Direct
interest reflects the GM's reputation, career record, and fit with the hiring owner's priorities; applications provide an
active path rather than requiring the user to wait passively for an offer. Neither route guarantees an offer or appointment.
Both use actual vacancies in the same persistent league and lead to an explicit offer the user can accept or decline.
Before accepting a job, the GM can discuss the hiring owner's expectations and negotiate resources through the interview
and offer process. Apply the same give-and-take used in annual owner meetings: requests for funding, support, or adjusted
targets may be accepted, countered, or denied according to owner priorities and franchise circumstances. Present the final
expectations, approved resources and conditions, and employment terms together before acceptance. Persist accepted commitments
as the starting agreement for the new job and subsequent owner reviews; an unapproved request is not promised support.
GM employment contracts include negotiable salary and contract length, both for new appointments and extensions. GM salary
is an actual franchise operating expense, recorded in team finances alongside other expenses, rather than a cosmetic career
figure. It does not count against the player salary cap. Persist the agreed term and pay schedule and account for each payment
once. If the team fires the GM before the contract ends, it owes the remaining contracted salary and retains that obligation
in team finances after a replacement is hired. Voluntary resignation or retirement forfeits remaining unearned salary;
already earned pay remains recorded. Exact payout timing remains to be defined before implementation.

Hiring activity and unsolicited offers are concentrated in the offseason after the league's main dismissal period, referred
to as its "Black Monday" period. This is an offseason hiring wave, not an exception permitting in-season GM firings.
The initial dismissal and hiring wave follows the end of the regular season for non-playoff teams, with further openings
as eliminated playoff teams complete their reviews and the championship participants finish their seasons. Teams whose
seasons have ended can resolve staffing issues, including dismissals, vacancies, interviews, offers, and hiring, while the
playoffs continue. Teams still competing wait until elimination or the championship's conclusion to resolve their own
staffing changes. Team-specific staffing eligibility does not advance the shared league calendar, alter playoff participation,
or unlock unrelated offseason player transactions. Available openings, application/interview status, and outstanding offers are visible through career-management
and inbox surfaces. Record the accepted employment contract and hiring in career history, preserve the ongoing league and
unemployment interval, and close incompatible pending applications/offers when the GM takes a job.
Interview choices, offer terms, response deadlines, and hiring cadence outside the main wave remain to be defined.

### Franchise dashboard, navigation, inbox, and time advance

Franchise time advancement uses user-initiated progression to the next meaningful event or attention point. Franchise calendar
time moves only when the user explicitly instructs it to advance; browsing screens or leaving the game idle does not advance
time. An advance may span several quiet days, but busy periods stop sooner for relevant responses, decisions, or scheduled
events, including within the same in-game day. Return control at the stop and wait for the user's next advance instruction.
Do not require one click per empty day or skip actionable responses simply to complete a fixed daily or weekly step. Process
intervening simulation and lifecycle events in chronological order, even when they do not warrant a user-facing stop.
Routine news and scouting updates accumulate in their normal surfaces without stopping advancement. Offers and relevant
negotiation responses, required decisions, scheduled meetings, and games return control to the GM. Deadline prompts also
stop progression while the decision can still be made. Group simultaneous attention items so the GM can review them at the
same calendar point rather than advancing repeatedly through events with the same timestamp.

The GM can customize optional stop triggers, including stopping when a shortlisted player receives a competing offer that
the franchise is entitled to know about. Optional alert preferences do not reveal private negotiations or hidden information.
Persist these preferences with the career and clearly distinguish optional alerts from mandatory compliance/decision stops,
which cannot be disabled to bypass rules. Routine updates remain available whether or not they trigger a stop. Optional
one-day and advance-to-date controls are not yet approved; the full optional-trigger catalog and detailed presentation remain open.
Preserve explicit lifecycle processing and rule
validation independently of the eventual controls. This revision concerns franchise calendar progression; it does not by
itself replace the separately defined live-game playback or live-draft clock behavior. Trade-deadline day uses a special
user-controlled hourly clock: each advance simulates one in-game hour, processing negotiations, responses, and league activity
within that interval. Time does not run while the user browses or negotiates. Show the current time and time remaining until
the trade cutoff. Preserve mandatory decision stops and the deadline prompt; never advance past an unresolved required
decision or execute a user trade without approval. Trade execution must satisfy the actual cutoff even if talks began earlier.
Exact opening hour and treatment of intrahour responses remain to be defined. Deadline-day screen layout and whether its
information is combined into one dedicated view remain deferred to the UI pass; no specific arrangement is approved yet.
Ensure the eventual presentation supports reviewing negotiations, offers, league trade activity, and remaining time.

As the trade deadline approaches, teams may adjust demands according to urgency, roster objectives, alternatives, and
bargaining leverage. A team eager to complete a move may become more flexible, while a team with strong leverage can hold
firm or demand more. Do not apply a universal late-day discount or force trades merely because time is running out. Changes
flow through the existing explainable negotiation and validation systems.

During busy free agency, player/agent responses may arrive within the same in-game day or after one or two days, depending
on competing offer volume and the player's or agent's patience and urgency to complete a deal. Do not require every response
to consume a full day or force all candidates to respond immediately. Exact same-day interaction and time-step mechanics
follow the user-initiated event-based advancement model; exact intraday clock granularity remains to be defined. This response
timing does not authorize calendar time to run while the GM browses.

The franchise dashboard is the GM's customizable command center. It is a launch point for detailed screens, not a replacement
for roster, finances, scouting, game, or league views. Its default purpose is to answer: where does the team stand, what is
happening around the league, and which storylines deserve attention now. It uses the game's professional 32-bit pixel-art
style, while its layout prioritizes readable data density over decorative art.

The persistent application shell has a fixed left navigation rail, a slim top quick-glance/status bar, contextual secondary
navigation for the active area, and a tile-grid workspace. The rail's permanent destinations are **Home**, **Inbox**, **Team**,
**Finances**, **League**, **Scouting**, and **Trade Center**. Detailed pages live under their relevant destination instead of
becoming additional top-level tabs. Team is an expandable group with Team Info, Roster, Depth Chart, Team Standings, Team
History, Staff, Team Stats, and Injuries. The other primary areas expose similarly compact, predictable contextual navigation:
Finances includes team finances, contracts, and accounting; League includes standings, stats, scores/schedule, news, leaders,
awards, history, and player search; Scouting includes scouting, draft board, college football, and draft; Trade Center includes
trades, trade block, free agency, waivers, and league transactions. Trade Block absorbs the former standalone
Trade Finder workflow.

The accepted Team Stats presentation direction uses a customizable compact tile grid with shared season, timeframe, game,
comparison, and split filters. Its default view combines precise offense and defense ranking tables, a restrained trend chart,
situational performance, player contributions, and special-teams context. Every statistic identifies its units, timeframe,
and comparison scope. The September 10, 2026 mockup establishes the overall structure only; exact default tiles, sizing,
filters, chart treatment, wording, and illustrative data remain open to refinement.

Team History defaults to a compact franchise-encyclopedia overview rather than a full-screen season spreadsheet. It combines
persisted franchise identity and historical summary with honors, retired jerseys, selected record holders, notable figures,
and a substantial recent-season table. Do not create a Notable Eras section; use that space for more factual season rows and
columns instead. Focused routes lead to complete Seasons, Records, Honors & Retired Jerseys,
Draft History, Transactions, Staff History, and Financial History archives, which retain dense sortable tables. Every fact,
leader, honor, and historical statement comes from authoritative persisted records; presentation does not invent decorative
lore. The earlier full-screen season-table mockup is rejected as the default but remains useful for the deeper Seasons view.

The September 10, 2026 encyclopedia mockup without a Notable Eras section is accepted as the Team History structural
reference. Its exact spacing, copy, historical images, summary fields, record selection, and illustrative history remain open
for refinement and must ultimately be populated from persisted world data.

The status bar remains intentionally compact across every franchise screen: team identity, current date/week and season phase,
regular-season record, an unmistakable unread/action-required Inbox count, and the primary Advance control. It omits cap and
GM-profile information so those can remain useful rather than compressed on their dedicated pages. Advance labels the next
meaningful time step when useful. When an unresolved blocking decision prevents advance, it explains why and links directly to
the relevant Inbox message or decision screen.

The Home workspace is a responsive tile grid. At the reference desktop size it defaults to six readable cells in a 3-column by
2-row workspace; screen-size changes add or remove grid capacity rather than shrinking tiles below legibility. Tiles snap to
the grid and support valid adjacent sizes, including 1x1, 2x1, 3x1, 1x2, and 2x2. Edit Dashboard mode lets the player add,
remove, rearrange, resize, and reset tiles. The layout engine prevents overlap, reflows a changed layout predictably, and
preserves a valid usable dashboard. Tile content responds to its allocated size, and every tile has one obvious route to its
complete underlying screen.

The default Home layout contains a large standings tile, GM Profile/Career Stats, Top League News, Top Prospects/Draft Board,
and Team Stat Leaders. Standings defaults to the user's conference, displays complete division standings, and uses a compact
two-conference carousel; its arrows change conference and clicking elsewhere opens full League Standings. The GM tile shows
the portrait, GM identity, current team, career record, playoff/championship history, current owner expectation, and a clear
job-security status. The News tile is a curated editorial snapshot of trade rumors, transactions, performances, injuries,
game results, awards, playoff context, and personnel news; it routes to full stories or League News. The prospect tile cycles
between notable prospects, a public analyst draft board, and college rankings without exposing private scouting truth. Team
Stat Leaders summarizes passing, rushing, receiving, tackles, sacks, interceptions, and, when space permits, kicking; each
entry links to its player or team-stat detail. Recent results, upcoming game, calendar, injury report, cap snapshot, position
needs, development watchlist, Inbox summary, and league leaders are available as optional dashboard modules.

Inbox is a dedicated two-pane email screen, not a duplicate dashboard. Its message list displays sender, subject, preview,
date/time, and unread/action-needed state; the reading pane provides full authored communication, relevant context, and actions.
It has two categories. **Action Required** contains time-sensitive offers, illegal-roster decisions, urgent cap/contract/budget/
attendance/stadium issues, and other decisions that must be resolved before the applicable simulation point. **Updates** holds
non-blocking team, player, scouting, game, and league information and can be read or archived without halting the calendar.
Messages link naturally to their relevant player, game, offer, report, or management screen.

## 1.0 systems

### League, calendar, simulation, and history

The offseason uses one continuous calendar with overlapping activities. Staff hiring, eligible contract negotiations,
scouting, and other available work may proceed concurrently; the GM does not have to finish each activity as an isolated
stage before accessing another. Each action remains subject to its own opening window, deadline, eligibility, and league-year
rules. Team-specific staffing availability does not open league-wide free agency early.

Use the [NFL Football Operations calendar](https://operations.nfl.com/calendar-events/nfl-important-dates) as the reference
for supported windows, including tag designations, permitted free-agent negotiations, official signing and league-year
opening, draft events, and roster deadlines. Distinguish permission to negotiate from permission to execute a contract or
transaction. Generate and persist a versioned schedule for each in-game year rather than copying one real season's dates
into every year; document deliberate fictional-league exceptions. Calendar and relevant management screens show upcoming
openings, deadlines, and reasons an action is unavailable. Exact window mapping remains for the rules pass. Continue uses
the accepted event/attention stops and configurable optional stops; the optional-trigger catalog and controls remain for the UI pass.

Time-sensitive management deadlines generate advance reminders and a prompt at the deadline while the applicable decision
can still be made. Show the affected action, due date/time, consequences of taking no action, and a direct route to resolve
it. Distinguish mandatory compliance from optional opportunities that the GM may consciously let expire. The reminder and
deadline prompt stops advancement while the action can still be taken. Mandatory compliance must be resolved; optional
opportunities may consciously be allowed to expire. Exact reminder lead times and prompt controls remain for the calendar/UI
pass. This does not override the existing live-draft clock expiry rule.

The league owns all teams, pools, standings, schedule, current phase, results, settings, transactions, and historical archive. The calendar supports preseason, regular season, playoffs, offseason, draft, free agency, and preseason handoff. Game simulation resolves games from ratings, lineups, schemes, fatigue, injuries, home field, and controlled randomness, producing final scores, box scores, player statistics, and key events.

The fictional pro league follows a real-scale current NFL structure: 32 teams, two 16-team conferences, four four-team divisions in each conference, a 17-game regular season across 18 weeks, one bye per team, and a rotating/standings-aware scheduling formula. The postseason fields seven teams per conference through division titles and wild cards, with the top conference seed receiving the first-round bye. The versioned schedule, tiebreaker, seeding, and playoff rules are enforced consistently and preserved in history.

Preseason is a full football-operations phase rather than a skipped exhibition. It includes training camp, roster battles, depth-chart and package evaluation, staff reports, injuries, roster-cut deadlines, and three simulated preseason games. Preseason results do not enter regular-season standings, but game performance, health, fatigue, role competition, player knowledge, and staff evaluations carry real consequences into final roster construction. The GM can manage or delegate eligible preseason roster/depth administration under the same visible delegation rules used during the regular season.

During training camp, the GM directly sets player and position-group training focus plus first-team practice reps. These choices create meaningful roster-battle opportunity and affect staff observations, player knowledge, readiness, fatigue/health exposure, and competition outcomes within bounded development/injury rules. Staff reports make the tradeoffs and emerging results clear; the system never treats a practice-rep choice as a guaranteed depth-chart outcome.

During the regular season, the GM sets a weekly practice and game-plan focus before each matchup. The planning layer supports offensive/defensive emphasis, opponent preparation, player or group workload, and other supported weekly priorities. It feeds coach preparation, player readiness/fatigue, staff reports, tactical fit, and the following game without guaranteeing results or overriding individual-play calling. The UI makes the current plan, staff recommendation, expected tradeoffs, and any health/availability concern clear before the weekly advance.

Schedules enforce valid weekly matchups and byes. Standings use only regular-season results, apply tiebreakers, seed playoffs, and advance a bracket to one champion. History records careers, team seasons, awards, records, playoffs, championships, transactions, and retirements without changing current gameplay state.

Standings and postseason seeding apply the full NFL-style tiebreaker order. When records are tied, the Standings and Playoff Picture surfaces show the applicable criteria in order and identify the exact resolved reason a team is above or below another. Tiebreaker calculations remain reproducible from persisted results and rules, never hidden as an unexplained sort order.

The default League Standings layout does not reserve a permanent bottom panel for tiebreaker detail. Tied placements carry a
small information control that opens the applicable ordered criteria and exact deciding reason contextually; the complete
rules remain available through a dedicated action. This preserves explainability without reducing the main standings area.

The September 10, 2026 League Standings mockup without the permanent tiebreaker panel is accepted as the structural reference.
Its exact table spacing, columns, controls, and illustrative teams/results remain open for refinement.

The 1.0 pro league includes annual All-Star roster selections and separate All-Pro honors. All-Star selections use simulated
fan votes that weigh regular-season production heavily, with a smaller reputation/popularity influence. All-Pro honors use
simulated committee votes based on regular-season production and position-appropriate performance evaluation, without the
fan-popularity component. Voting evaluates season evidence rather than hidden true ratings or future potential. The two
honors are resolved independently; an All-Star selection does not automatically confer All-Pro recognition. All-Pro honors
include both First Team and Second Team, with the awarded tier preserved in season honors, player career history, and recaps.

These are honors only: there is no associated all-star game, skills competition, or minigame event. Each honor is recorded
distinctly by season and player in league honors and player career history, with recognition in the postseason recap.
Selection does not create games, statistics, injuries, fatigue, or additional calendar obligations. Persist resolved voting
outcomes so save/load cannot reroll selections. Announce All-Star selections after the conference championship round,
in the week before the league championship. Announce First- and Second-Team All-Pro honors during the annual awards show
alongside individual awards. Keep selections unrevealed until their respective announcements. Exact voting weights,
positional allocation, and tie handling remain to be defined in the audit.

Annual individual season awards include Most Valuable Player, Offensive Player of the Year, Defensive Player of the Year,
Offensive Rookie of the Year, Defensive Rookie of the Year, Comeback Player of the Year, and Coach of the Year. Executive
of the Year is outside 1.0 scope. Persist each award by season and recipient, expose it in league honors and the postseason
recap, and retain it in the appropriate player or coach career history. Exact eligibility, voting criteria, tie handling,
remain to be defined; announcement uses the championship-week awards show specified below. Comeback Player of the Year eligibility includes both returns from injury or
absence and rebounds from a poor season. Evaluation considers the documented prior setback and the subsequent season's
performance; eligibility does not automatically confer the award. Exact qualifying thresholds and comparative weighting
between comeback cases remain to be defined.

Season awards are announced in a dedicated awards presentation on Thursday evening of championship week, three days before
the Sunday championship game, following the NFL Honors timing model. The presentation recognizes each winner, followed by a concentrated
wave of league news coverage shortly after the show. Media revisits the results through winner profiles, season retrospectives,
snub discussions, and debates about deserving candidates, with multiple distinct angles rather than repeated identical
announcements. Coverage draws from actual season evidence and resolved awards; media disagreement never changes the winners.
The presentation and subsequent stories reference the same persisted results, and award-related reaction stories do not
reveal winners before their presentation announcement. The show presents the finalists for each award before revealing its
winner. Before entering the show, the user chooses whether to attend and watch its cinematics or skip the entire show and
view the results afterward. Attendance is a presentation choice, not an obligation imposed on individual simulated players.
All-Pro honors are announced during the same show and included in its results. The Hall of Fame class is announced through
the inbox after the league's first preseason game is simulated, as specified below. The GM can also skip ahead to
the full results while already watching. Skipping completes the presentation and reveals the
same persisted results without changing winners, duplicating honors, or suppressing subsequent news coverage. The duration
and cadence of the follow-up coverage remain to be defined in the audit.

The league also publishes its own annual Top 100 player ranking as a distinct recognition feature. Ranking scores use
80% production and 20% player ability, rather than simulated player voting. Production uses only the completed regular
season, excluding preseason and playoffs so postseason qualification does not grant extra scoring opportunities.
Snapshot the ability component at the end of the regular season, using that saved value for this year's ranking even if
players develop, regress, or suffer injuries before the reveals. This freezes only the ranking input; normal player
development continues. Persist the snapshot so later calculation or save/load cannot substitute newer ability values.
Normalize both components to a comparable scale
and evaluate production appropriately by position so raw statistical volume does not automatically favor certain positions.
Ability contributes internally; the published ranking does not expose true ratings or a component breakdown from which they
can be recovered. Preserve each year's
ordered list and player identities in league history, with player-profile recognition and links from the ranking to profiles.
The ranking is an in-universe assessment, not a disclosure of hidden true ratings or potential. Persist the published result
so save/load does not reroll it. Reveal the list in five weekly batches after the league championship: ranks 100-81 one week
after the game, 80-61 two weeks after, 60-41 three weeks after, 40-21 four weeks after, and 20-1 five weeks after. Finalize the
full ranking before the first reveal; offseason changes do not reorder later batches. Persist reveal progress, keep unreleased
entries hidden on all user-facing surfaces, and deliver each batch once through league news with access to the revealed list.
Calendar advancement processes due reveals even when the GM advances across multiple weeks. Its fictional branding,
position-specific scoring details and detailed reveal presentation remain to be defined in the audit.

After the championship, show a dedicated recap screen rather than a celebration sequence. Summarize the champion and
runner-up, final score, championship MVP, key game performances, playoff outcome, major season awards, records or milestones,
and the season's major storylines. Link to the championship box score, playoff bracket, relevant player/team profiles, and
season history for deeper inspection. Use authoritative results and recorded honors, preserve the recap in season history,
and provide a clear route to continue into the offseason. The recap does not reveal unreleased Top 100 entries.

### Roster, game-day, injuries, and development

Teams own active rosters, injured reserve, practice squad, depth chart, and position requirements. Availability, fatigue, and substitutions affect game-day lineups. Injuries carry status, recovery timing, availability, history, and roster decisions; recovery presentation gives nonbinding depth-planning context from persisted status and time remaining. Practice-squad management clearly indicates whether an active opening exists and identifies an internal call-up option while the transaction service remains authoritative.

Roster construction, player status, game-day eligibility, injured reserve, practice squad, waivers, and transaction timing follow NFL rules as closely as practical for 1.0. Any deliberate fictional-league or usability exception is individually named, explained in the rules/UI, and never silently changes between careers.

The GM directly controls every roster transaction: signings, releases, waivers, practice-squad moves, IR moves, elevations, and returns. The game never lets staff silently execute a player-movement transaction. Game-day administration—such as choosing the game-day active/inactive list, depth assignments, and ordinary lineup administration—may be performed manually by the GM or delegated to the relevant coach/staff. Delegation is visible, reversible, and uses the delegated staff member's preferences and knowledge; the GM can review or override the result before the game where timing permits. Ordinary players have one football position; general secondary-position eligibility and position-switch learning are outside 1.0 scope. Permanent dual-role eligibility is limited to very rare two-way players with explicitly supported offensive and defensive roles. Separately, suitable cross-position players may occupy emergency entries at the bottom of the depth chart under the next subsection; those entries do not grant permanent secondary-position eligibility. The two-way subsection below defines generated eligibility, reversible one-way usage, and shared workload. Exact rarity, supported role pairs, and numerical workload/evaluation effects remain for rules and tuning.

#### Emergency position coverage

Emergency replacements follow the next available eligible player in the saved depth-chart order. The lower ranks of a
position's chart can contain suitable cross-position emergency entries: for example, the seventh WR may be a backup TE,
or the third safety may be a depth CB. A backup RB may similarly appear as emergency FB depth. These are visible, persisted
depth-chart assignments managed by the GM or delegated staff under the normal depth-chart controls, not a separate live
position-proximity selection when an injury occurs. Suitable nearby positions inform how emergency entries are assigned;
the game then follows that order as unavailable players are skipped. Apply substantial, role-appropriate out-of-position performance penalties.
Medical unavailability and game-day eligibility remain binding; do not activate an inactive player or use a medically barred
player to fill the gap. Explain the emergency assignment and its cause in game-day personnel information.

Cross-position entries provide emergency depth without granting a permanent secondary position, position-switch learning,
or two-way ability. Preserve the player's normal position. Availability changes use the same saved depth-chart order rather
than creating an independent replacement hierarchy. The supported emergency pairings, including specialist positions, and
penalty strengths remain for the position/rules catalog and simulation tuning.

The initial 1.0 depth-chart package set is fully editable by the GM or delegable to staff: offense has Base, Third Down, Short Yardage/Goal Line, and Two-Minute packages; defense has Base, Nickel, Dime, Short Yardage/Goal Line, and Third-and-Long packages; special teams has Kickoff, Kick Return, Punt, Punt Return, Field Goal/PAT, and Hands Team units. Every package displays its assigned personnel, availability, and relevant scheme fit. The data model supports more packages in future releases without replacing current assignments or requiring an incompatible roster reset.

Depth Chart uses a full-width, compact table rather than an on-field lineup diagram. The reclaimed space shows more of each
position's replacement order and more decision-useful columns, including position-relevant current-season statistics. Unit
and package controls remain in the same workspace. Configurable view presets may exchange statistical columns for contract,
evaluation, workload, scheme-fit, or other supported context while preserving explicit order controls, assignment validation,
delegation state, staff recommendations, and clearly labeled emergency depth. The table is the authoritative editing surface;
no decorative field view is required for this screen.

Player order is edited directly in-game by dragging a player row and dropping it at the desired place within the same position
group. This drag-and-drop interaction is the primary reorder control; the screen does not use separate Move Up or Move Down
buttons above the chart. A drop is a validated GameCore reorder request,
not a visual-only shuffle: illegal cross-position drops are refused, the authoritative saved order updates only after validation,
the upper or lower half of a target row inserts before or after that player so rows can move both up and down, and a successful
drop autosaves before the new order is presented as final. Keyboard/controller-accessible reorder actions must
provide equivalent functionality without restoring the discarded top-of-chart step buttons.

Each position group can be locked after the GM sets its order. Auto-Fill preserves the complete saved order of every locked
position while still adding newly eligible players to the bottom of that group; unlocked positions may be regenerated from
availability and staff evaluation. Lock state is visible in the group heading, persists in the franchise save, and can be
toggled from the depth-chart workspace without changing player ratings, eligibility, or assignments in another position.

The September 10, 2026 table-first mockup is accepted as the structural direction, not as a pixel-perfect final layout.
Its exact columns, widths, row density, control placement, wording, and illustrative data remain subject to refinement.

Schemes and playbooks use recognizable real-football concepts while all teams, coaches, playbook names, and presentation remain fictional. Offensive systems can express concepts such as West Coast, spread, power run, and air raid; defensive systems can express fronts and cover-based identities such as 3-4, 4-3, and other supported coverage families. A selected scheme/playbook determines the tactical context and fit shown for players, coaches, packages, and simulation decisions without turning any label into a guaranteed outcome.

The GM may build custom offensive and defensive plays and assemble custom offensive and defensive playbooks for simulation use. The editor takes inspiration from NFL Head Coach 09 while offering a clearer, more flexible 1.0 experience. This is a functional design tool, not a naming or visual-only feature: a custom play carries its formation, required personnel, assignments, routes/coverage or other relevant tactical instructions, and situational use. The GM chooses a supported predetermined option for each player's assignment and has bounded flexibility to position players within the legal structure of the formation. Before the game permits it to enter a playbook or simulation, validation confirms that its formation, personnel, assignment coverage, eligibility, positioning, and rules are legal and that the simulation can resolve it. The UI explains every validation failure and never silently converts an invalid custom play into a different tactic. Custom special-teams design is outside 1.0; special-teams units use the supported depth-chart assignments and staff strategy.

The play builder includes a practice/test simulation. Before adding a custom offensive or defensive play to a live playbook, the GM can run it against a selected opposing defensive or offensive look and inspect the resulting simulated behavior. Testing is a safe sandbox: it does not advance the franchise calendar, affect player health/fatigue/statistics/history, or reveal unavailable hidden ratings. It confirms only that the play functions in the supported simulation context and gives useful tactical feedback rather than promising live-game success.

On live game day, the player is a GM observer rather than a play caller. The GM's influence comes from roster construction, game-day activation, depth packages, staff delegation, schemes, gameplans, and playbooks established before and between games. Coaches call individual offensive, defensive, and special-teams plays according to their assigned/delegated tactical control, preferences, available personnel, and live game state. The game-day interface never offers the GM a direct individual-play call action.

Live games are presented as a 2D pixel-art simulation on a football field. The visual playback is driven by the same authoritative simulation result that produces the score, play-by-play, statistics, game log, injuries, and box score; it is a clear presentation of a resolved play, not a separate rules engine that can disagree with the football simulation. The field view uses readable player markers/sprites, team identity, ball movement, downs/distance, game clock, score, and essential play context while preserving the professional nostalgic pixel-art style.

Player motion uses reusable hand-authored directional animation state sets rather than generating a bespoke video for each
play or rotating one side-facing sprite into invalid orientations. At minimum, supported directions require idle/stance,
start, run, cut, catch, throw, handoff, block-engage, block-shed, tackle, fall, get-up, kick, and celebration/incomplete-play
states where applicable. The authoritative resolved play supplies each actor's timed movement path, facing, state changes,
ball attachment/flight, contacts, and outcome; the presentation samples the matching sprite animation along that path.
Team uniforms, skin tone, body archetype, equipment, and jersey identifiers layer onto the shared motion library so the game
does not need a unique animation set for every player. Integer positions, nearest-neighbor scaling, deterministic event timing,
and persisted playback seeds keep replays visually consistent without changing the underlying result.

Game-day observation uses a Football-Manager-style control model. The player can pause and choose simulation/playback speed through settings. A highlight mode controls which resolved plays receive 2D field presentation: the user can watch every play, only exciting or game-defining plays, or an intermediate level of coverage. The highlight classifier uses explainable football importance—such as scoring, turnovers, major gains/stops, injuries, milestone performances, late-game leverage, and momentum-changing events—and never alters the simulated result to manufacture drama.

While a live game is paused, the GM may make valid depth-chart, personnel-package, delegation, gameplan, scheme, and playbook adjustments; the GM still does not call an individual play. The game validates the changed state against availability, game-day eligibility, roster rules, and supported tactics, then queues the change for the next legal break in play. It clearly identifies pending versus applied adjustments and never retroactively changes an already resolved play.

After a game, the GM reaches a postgame hub with the final score, full box score, play/game log, team and player statistics, injuries, staff recap, notable milestones, and direct routes to affected players. Occasional postgame press conferences occur after genuinely newsworthy circumstances—such as major wins/losses, rivalry games, controversy, injuries, records, playoff games, or unusual performances—rather than after every game. They are part of the media/immersion system and must remain concise, contextual, and skippable. The GM's selected answers have real but bounded consequences for media tone, fan pressure, player morale/relationships, owner perception, and subsequent storylines. They never rig game results, override a player trait, or substitute for actual franchise performance.

Players have ratings, position skills, age, potential, traits, career statistics, and progression history. Development resolves at explicit activity and calendar lifecycle events, including XP changes after games, training, and end-of-day processing; it is not restricted to weekly or seasonal batches. It respects caps and injuries and includes aging, regression, breakouts, stagnation, and retirement. A separate Development screen is outside 1.0 scope; meaningful progression context instead appears on player profiles, roster intelligence, staff reports, news, and other relevant decision surfaces.

Every rating shown to the user is an estimate determined solely by the knowledge their scouts and coaches have accumulated about that player. This uncertainty applies to every player, including the user-controlled roster; it is normally most accurate for the user's own players because the franchise's staff works with and observes them most closely. The UI communicates estimate confidence and avoids presenting scouting guesses as hidden truth. No screen automatically reveals a player's actual ratings merely because that player is a teammate, prospect, free agent, or opponent.

Each player may have zero to three current traits from a roughly 50-trait catalog. Traits may be positive, negative, or mixed; mental, physical, behavioral, or football-specific; and have meaningful, explicitly defined gameplay effects within their applicable systems. A player may hold multiple traits from the same category when they do not directly contradict one another. Trait effects can influence such areas as on-field tendencies, development, morale, injury behavior, leadership, or contract behavior, but never silently bypass a rule or guarantee an outcome. Traits are hidden until the franchise uncovers them through relevant observation and staff knowledge. Rarely, a player can develop or gain a trait over time, subject to the three-trait maximum. Narrative, personality, and storylines emerge naturally from these effects rather than being a disconnected cosmetic trait system.

### Extremely rare two-way players

Two-way eligibility is generated with a player's identity and is extremely rare. It cannot be unlocked by training an
ordinary player into another position. Persist the supported offensive and defensive roles through the college-to-pro
transition, team changes, and save/load without rerolling eligibility. Exact generation frequency and supported role pairs
remain to be defined; do not guarantee a two-way prospect in every draft class.

Offensive and defensive participation contribute to the same player's workload and fatigue. Using the player heavily on
both sides therefore creates a real readiness and health tradeoff rather than granting two independent stamina pools.
Staff and GM personnel decisions must consider combined usage, availability, and medical restrictions. Position-specific
evaluations and performance still reflect the relevant skills; two-way eligibility is permission to fill both supported
roles, not a guarantee of elite ability at either. Exact controls and rating presentation remain for the UI and balance passes.

The GM may convert a two-way player to a one-way role by choosing which of the player's supported positions they will play.
Update active role eligibility and personnel assignments consistently, clearing incompatible assignments on the other side.
Do not reset existing skills, development history, or career statistics as a consequence of the choice. Conversion is
reversible: the GM can restore both originally supported roles for a player generated with two-way ability. Persist that
underlying eligibility separately from current role usage. Restoring both roles does not reset fatigue, rust, or skill
development; normal readiness and workload rules still apply. Ordinary players cannot acquire two-way eligibility this way.

### Injury consequences and recovery estimates

An injured player automatically receives the medically recommended treatment through the health/recovery system. Treatment
selection is not an interactive GM or player negotiation decision. Inform the GM of the injury, selected treatment, and
estimated recovery through medical reports and the player injury view, with subsequent updates when the outlook changes.
The GM manages roster availability and return workload within medical restrictions rather than selecting between treatment
plans. Appropriate basic treatment remains available under the existing medical-staff vacancy rules.

Some injuries require one or two in-game days of evaluation before the exact diagnosis and useful recovery range are known.
Initially report the injury as under evaluation, with any known symptoms, provisional availability, and medical restrictions;
do not invent a precise diagnosis or return date. Initial care and necessary restrictions begin while evaluation is pending.
When evaluation resolves, update the injury record and inform the GM of the diagnosis, recommended treatment, and recovery
outlook through the existing medical/inbox flow. Not every injury requires this delay.

Players medically cleared to participate through a minor injury may play with reduced effectiveness and a risk of aggravating
the injury. Make clearance, performance limitations, and assessed aggravation risk clear before lineup/workload decisions.
Apply effects according to injury type, affected football activity, workload, and recovery state rather than a universal
penalty. Medical ineligibility cannot be overridden by the GM; clearance does not guarantee full effectiveness or safety.
Keep these temporary effects distinct from rust and permanent attribute damage to avoid counting the same impairment twice.

Serious injuries may permanently affect specific relevant attributes, while other players recover fully. Resolve outcomes
from the injury and player context rather than imposing the same permanent loss on every injured player. Keep lasting skill
effects distinct from temporary medical restrictions, fatigue, conditioning, and return-to-play rust; do not apply duplicate
losses for the same consequence. Actual attribute changes follow the existing development rules, and displayed evaluations
reflect them only when staff observe sufficient evidence.

Medical recovery estimates are ranges rather than guaranteed return dates. Update the range as rehabilitation progresses,
setbacks occur, and medical staff reassess the player. Reports explain meaningful changes and distinguish estimated clearance
from full performance readiness. Preserve dated estimates and actual recovery events in injury history so the user can
understand the course of recovery without gaining access to a predetermined hidden return date. Exact injury-to-attribute
effects, recovery modifiers, and setback frequencies remain for the injury catalog and balance pass.

The accepted Injuries presentation direction separates Available/Limited players, injured players on the active roster, and
Injured Reserve into three compact lists. Injured and IR entries receive enough room for diagnosis or evaluation status,
dated recovery ranges, restrictions, clearance and aggravation context, snap-limit routing, IR eligibility, and blocked-action
explanations. The September 10, 2026 mockup establishes this hierarchy only; panel proportions, spacing, wording, actions,
illustrative medical data, and final rules-bound details remain open to refinement.

### Player retirement and rare returns

Unlike GM retirement, player retirement can rarely be reversed. A retired player may announce an intention to return and
become available subject to reinstatement, existing team rights, and league eligibility rules. Preserve the same player
identity, age, career statistics, development DNA, and retirement/return history; returning does not reset ability or health.
An announcement alone does not place the player on an active roster or make them an unrestricted free agent.
The player must initiate the comeback before becoming available for return discussions or signing. Teams cannot initiate
comeback approaches to players who remain retired. Once the player announces a return, teams may act through the applicable
rights, reinstatement, and contract paths.

Returning players carry temporary rust expressed through reduced performance readiness and/or conditioning. Appropriate
training, practice, and a managed return to football activity gradually shed that rust; announcing a comeback or signing a
contract does not remove it instantly. Persist recovery progress and expose understandable readiness context so the GM can
decide how quickly to work the player into a role. Keep rust separate from permanent skill development, age-related decline,
injury, and ordinary fatigue so the same effect is not counted twice. Shedding rust restores current readiness, not the
player's former peak ability.

The same rust/readiness system applies after long injury layoffs and extended periods without a team. Training and practice
can gradually clear rust on their own, but do so more slowly than actual game reps. Game participation accelerates recovery
of football readiness while exposing the team to the player's reduced effectiveness before that recovery is complete.
For players returning from injury, rushing game workload can also increase reinjury risk according to the underlying injury,
medical readiness, and workload; rust alone is not an injury diagnosis. Medical eligibility and workload safeguards still
apply, and game reps cannot bypass an unavailable status. Explain the tradeoff through staff reports and player readiness
context so the GM can choose a gradual return or accept the risks of earlier game exposure. Credit completed activity at
explicit lifecycle events rather than granting an immediate readiness boost merely for assigning snaps. Exact severity,
time-away scaling, recovery rates, and workload/reinjury effects remain to be balanced.

The GM may optionally set a snap limit for a returning player as either a maximum snap count or a percentage of team snaps.
Coaches enforce the configured limit through personnel usage and substitutions, subject to medical eligibility and roster
rules. If no eligible replacement remains, coaches may exceed the workload limit for emergency use without requiring a
separate GM approval. Medical ineligibility remains binding. Clearly flag the emergency use, its reason, and actual workload
in game context and the postgame report rather than silently exceeding the limit. A snap limit controls exposure but does
not guarantee safe participation or eliminate reinjury risk. Exact percentage accounting remains an implementation detail
to define consistently for the player's applicable unit.

Medical and staff reports distinguish medical clearance (whether the player is safe/eligible to participate, with any
restrictions and assessed reinjury risk) from performance readiness (conditioning and remaining rust). Clearance is not a
promise of zero risk, and a medically cleared player may still be well short of normal football effectiveness. Present both
assessments together when setting return workload so the GM can make an informed decision without exposing hidden true ratings.

Distinguish retired players whose contractual rights are retained by a team from retired players without outstanding team
rights. A rights-held player must resolve their return through that team and valid reinstatement, trade, or release paths;
a player without retained rights can enter the appropriate free-agent signing path. Retirement cannot be used to escape a
contract and freely sign elsewhere. Any activation or signing must satisfy roster, contract, cap, medical/availability, and
calendar rules, with the resulting transactions and news recorded once.

Initial NFL reference: the [NFL's Reserve/Retired explanation](https://www.nfl.com/news/cardinals-place-cb-malcolm-butler-on-reserved-retired-list)
confirms that placement preserves team rights. The 2020 CBA's standard Player Contract, paragraph 16, confirms default
retirement tolling unless the contract provides otherwise; its Article 4 also permits certain bonus forfeitures. See the
Rules review findings below. Detailed NFL-rule research remains required before implementation for reinstatement deadlines,
release/waiver treatment, exact salary and signing-bonus accounting, and return eligibility in
each situation. Do not assume calendar passage while retired automatically extinguishes retained rights. Return frequency,
motivations, and permanent time-away effects remain open design decisions.

### Player DNA and career development

#### Functionality

Player DNA includes a hidden, generated career-development trajectory. Regression is not restricted to old age: a young or
prime-age player can enter a genuine decline even while healthy, playing, training, and earning XP. The trajectory supplies
a signed development influence, including negative pressure strong enough to outweigh positive XP-driven development during
an appropriate decline phase. Merely reducing an XP multiplier to zero is insufficient because it cannot produce actual
rating loss. XP remains earned experience; regression acts on underlying skills rather than requiring negative XP.

Career trajectories are not limited to one rise followed by one fall. Generation supports early peaks, plateaus, stalls,
pre-aging declines, temporary downturns, late bloomers, and decline followed by a late resurgence or multiple peaks. Phases
vary in onset, duration, strength, and transition shape; every player need not experience every phase, and resurgence is
possible rather than guaranteed. A rebound may recover only some lost ability, regain a previous level, or reach a new peak
within applicable skill limits. These patterns emerge from generated development data rather than a universal age schedule.

DNA also determines the stability of that underlying trajectory and the player's responsiveness to circumstances. Some
careers closely follow a steady curve; others admit more bounded variation through setbacks, breakthroughs, and changes in
opportunity, training, health, or environment. This is a spectrum across players, not a universal rule that every decline
can be overcome or that every decline is unavoidable. The persistent curve remains the baseline; talent is not independently
rerolled each season. Development variability is distinct from temporary performance volatility.

Training remains beneficial within the player's circumstances, but its effect varies: it may turn a mild decline into net
growth, shorten a slump, or merely slow a severe decline that continues despite excellent support. A late resurgence can
arise from an underlying second upswing, improved circumstances, or their combination. Neither strong training nor a past
slump guarantees a rebound. Exact probabilities and strengths remain balancing decisions for long-run simulation testing.

At each explicit development lifecycle event, resolve the combined change from XP/training gains, the signed DNA influence,
aging, and applicable persistent development effects such as injury consequences and bounded staff/trait modifiers. This is
a conceptual combination, not a finalized numeric formula. Apply each contribution once for the elapsed interval, preserve
fractional progress where needed, and enforce underlying skill bounds after resolving the combined change. A strong negative
DNA phase must be able to produce net regression despite ordinary positive XP gains; management can mitigate the loss but
cannot assume that sufficient routine playing time guarantees continuous improvement.

Physical, technical, and mental skills may develop differently. Aging remains a distinct influence, so a late resurgence
can come from improved technique or decision-making while physical ability continues to decline. Overall remains a derived
summary. Temporary form, morale, fatigue, scheme fit, and a revised scouting estimate must not be mistaken for permanent
development or counted again as permanent rating loss. The DNA trajectory shapes development pressure; actual career outcomes
also depend on opportunity, training, health, and environment.

#### Football experience and training focus

All modeled football-related participation can earn experience, with the amount and type reflecting the activity actually
performed. Ordinary weekly training, workouts, film study, and comparable preparation provide moderate, steady XP. Game
participation provides the greatest XP opportunity but more variable gains, reflecting actual involvement and game context.
Offseason workout-program and minicamp activities provide low skill-development XP and emphasize conditioning/readiness.
Distinguish these conditioning-oriented sessions from the regular weekly training workload rather than applying contradictory
XP rates to a generic "workout" label. Exact rates and game-XP factors remain to be balanced.

The training catalog includes varied football drills and activities that target particular attributes, alongside broader,
less focused training. Define each activity's affected attributes, experience distribution, conditioning/readiness effects,
and workload so selecting a drill has a concrete effect. The GM chooses the specific drill, whose targeted skills and
relative focus are predefined in the catalog; the GM does not select an attribute and have staff choose a drill for it.
Show those predefined skill focuses before selection without exposing XP amounts or true ratings.
Film study and other mental preparation contribute to their relevant
learning areas rather than indiscriminately improving unrelated physical skills. The proposed activity catalog appears in
the review package below; it awaits user review. Focused activities and general work both use the same development system.

Staff build the weekly training schedule by default. The GM can review and change scheduled activities and individual player
focuses, within the same time, workload, availability, and medical constraints used by staff. Show the proposed schedule,
current activities, individual overrides, and expected development/readiness tradeoffs before advancing through the work.
This default scheduling support preserves the GM's existing control over camp focuses and practice-rep decisions.

Heavier training offers greater development opportunity at the cost of additional fatigue and injury risk. Present these
tradeoffs in planning and staff reports without promising exact gains or guaranteed injury outcomes. Resolve actual workload,
conditioning, medical restrictions, and staff support through the shared fatigue/injury/development systems; intensity is
not an unlimited XP multiplier and does not bypass a player's DNA trajectory or skill limits. Supported intensity choices
are light, standard, and heavy, subject to activity, phase, and medical restrictions. Exact effect strengths remain to be balanced.

Practice-squad players continue to earn training XP, but at a reduced rate compared with equivalent active-roster training.
This is a deliberate balance rule to keep practice-squad development useful without allowing players to accumulate excessive
XP by remaining there. Award game XP only for actual eligible game participation. Persist activity and roster-status context
so moving between pools cannot duplicate training credit or collect both rates for the same work. Training choices must fit
available time, workload, and health constraints rather than allowing unlimited repeated activities.

Earned XP contributes to development; it is not a promise of positive rating change. Resolve gains alongside the signed DNA
trajectory, aging, and other applicable effects as defined above, including net regression despite productive football activity.

Actual skill development is resolved at the moment an XP change is applied, such as after a game, completed training, or
end-of-day activity processing. Simulation uses the resulting underlying skills immediately; fractional progress need not
produce an integer rating change at every event. Do not delay actual development until a weekly scouting report. Apply
time-based DNA and aging pressure only for the appropriate elapsed interval, without duplicating it across multiple XP
events on the same day. Calendar-driven decline can still occur without an XP award.

#### Coaching quality and veteran mentoring

Coaching quality affects both the effectiveness of relevant player development and the accuracy with which staff identify
skill changes. These are distinct responsibilities: better development support changes the underlying learning outcome,
while better observation improves the franchise's estimated evaluation. Neither guarantees growth, immediate discovery,
or perfect knowledge. Apply bounded, role-relevant effects through the shared development and scouting systems rather than
allowing staff quality to override DNA or disclose hidden ratings.

Some established veterans can mentor younger teammates, with the benefit depending on traits, relationships, and shared
practice time. Veteran status alone does not confer mentoring ability or willingness. The Mentor trait is not required,
but makes mentoring substantially more effective within bounded development effects. Only veterans are eligible to receive
the Mentor trait, whether through generation or later trait acquisition. Preserve the existing trait limit, compatibility,
and evidence-based discovery rules; the precise experience threshold for veteran eligibility remains to be defined.

The GM can ask a suitable veteran to mentor a younger teammate. The veteran may accept or decline based on personality,
relationship, circumstances, and willingness to teach; selecting a player does not force participation or grant the trait.
Make the response and any disclosed reason clear. An accepted arrangement provides benefits through actual relevant shared
practice and interaction, not a roster-wide bonus simply for employing an older player. Persist the arrangement and apply
its learning effects through the same XP/development system. Exact pairing eligibility, duration, and capacity limits remain
to be defined.

Mentoring primarily pairs players within the same position group, where relevant experience and shared practice provide
the strongest teaching context. Do not treat this as an absolute ban on cross-group mentoring; any exception must have a
relevant learning basis rather than transferring unrelated position skills. A veteran may mentor one younger player or a
small group. Spread finite mentoring attention across active mentees instead of giving every additional player the full
one-to-one benefit. Willingness and capacity can vary by veteran; neither arrangement requires the Mentor trait. Exact
group-size limits and cross-group eligibility remain to be defined.

Mentoring primarily supports technical skills, mental preparation, and scheme knowledge. Shape the learning opportunities
and XP gains around what the mentor actually knows and practices rather than granting the same generic bonus for every
pairing. A mentor's physical attributes are not transferred to the younger player; learning good preparation habits may
instead influence the younger player's own training behavior and resulting development.

Young players may also pick up a mentor's habits and traits through sustained interaction. For example, a veteran's Work
Ethic may encourage similar habits in a rookie, while a mentor who cuts corners can exert a negative influence. Both useful
learning and harmful habit adoption are possible. Reflect the mentor's actual behavior and known domain state, the mentee's
personality and receptiveness, relationship, and shared time; do not copy a trait automatically or generate it from a single
conversation. Trait acquisition remains rare and must respect the three-trait cap, compatibility rules, and eligibility
restrictions, including the veteran-only Mentor trait. When acquisition is ineligible, do not silently replace an existing
trait or bypass the cap. Any trait discovery follows the existing observation rules rather than revealing hidden mentor or
mentee traits through the mentoring screen. Exact influence probabilities and how the Mentor trait affects positive versus
negative habit transmission remain to be defined.

Younger players vary modestly in teachability and receptiveness. These differences affect how readily they learn from a
mentor or adopt habits, alongside relationship and shared experience; they do not make a player incapable of learning or
guarantee that an impressionable player acquires a trait. Use the existing player personality/learning model where applicable
rather than assuming a new visible trait or attribute is required.

Staff may report observed signs that a mentoring arrangement is helping or hurting the younger player, using existing
training reports and player evaluations. Distinguish evidence and uncertain interpretation from confirmed trait discovery;
reports must not expose hidden values or assign every development change to the mentor. The GM can end an arrangement or
request a different pairing through the same acceptance process. Ending mentoring stops its future interaction-based effects,
but does not erase experience already gained or automatically reverse habits or traits already acquired.

#### Generation, persistence, and continuity

Generate and persist the trajectory with the player's identity, including the parameters or stable seed/version needed to
reproduce it and the development progress already applied. Save/load, team changes, and college-to-pro transitions preserve
that identity and do not reroll the curve or restart its clock. Existing saves require a deterministic migration that retains
current ratings and history without retroactively applying new development. Repeating a processed lifecycle event must not
award XP, apply decline, or trigger a resurgence a second time.
Persist stability, responsiveness, and the random state or resolved events governing development variation so reloading does
not reroll setbacks or breakthroughs. A team change may affect development through actual circumstances while preserving DNA.

#### Presentation and player knowledge

XP totals, gains, thresholds, and progress bars are always hidden from the user. Numerical attribute bars and values are
staff estimates, not XP meters or direct access to underlying skills. An actual skill change does not automatically update
those displayed estimates. Scouts or coaches must observe evidence and issue an evaluation/scouting update before the
change becomes reflected in the user's information. Staff notes explain observed development, with the existing confidence
and report-date context; estimates may lag real changes. Persist actual development separately from observed evaluations
so opening a profile, advancing an unrelated screen, or reloading cannot reveal unobserved gains or losses.

The full hidden curve and future turning points are never revealed to the GM. Player profiles, staff reports, roster context,
and news communicate observed development and uncertain outlooks through the existing scouting-knowledge rules. Reports may
describe a player losing ground despite productive training or showing signs of a rebound, but cannot promise the exact year
of recovery or expose hidden DNA values. Actual development history and changes in staff assessment remain distinguishable.
This uses existing decision surfaces; it does not introduce a separate Development screen.

#### Audit status and implementation acceptance

Design requirement recorded September 8, 2026. The current C# annual development foundation uses age bands and does not yet
implement DNA trajectories or XP opposing regression. XP activity categories and relative gains are now defined above;
before implementation, finalize numeric conversion, the training catalog, phase generation
and frequency, skill-group effects, phase time basis, magnitude limits, and distributions for stability and responsiveness.
The accepted direction is a persistent underlying curve with bounded variability and player-specific responsiveness;
resistance to decline is tuned within that model. Acceptance examples must demonstrate healthy young-player net regression despite positive XP, a decline followed by
later recovery, a plateau, physical aging alongside technical resurgence, and identical outcomes across save/load and repeated
event protection. Include both steady and erratic careers, training overcoming a mild decline, and training mitigating a
severe decline without reversing it. Long-run balancing must retain career variety without making every slump predict a comeback.

### Player identity, ratings, profiles, and context

Every player has a persistent identity: 32-bit pixel-art portrait, name, birthday, age, height, weight, hometown/nationality,
college, draft information, jersey number, handedness where relevant, position, football role, team/employment state, contract,
health, fatigue, statistics, career history, and retirement history where applicable. Birthday is a real in-game calendar value;
the player's age updates on that date. Player Profile is the shared authoritative detail screen reached from roster, scouting,
league, transaction, injury, result, and stat-leader views. Its header makes identity, current team/status, contract, health,
morale, and immediate availability clear. Its main content combines football evaluation, staff assessments, current and career
statistics, traits as they become known, role/depth context, scheme fit, development/regression history, injuries,
transactions, awards, and relevant story context. Contextual actions route to valid roster, depth-chart, contract, trade,
waiver, injury/IR, scouting, or draft decisions and explain unavailable actions.

The accepted Player Profile presentation direction uses a compact identity/status/contract header above a dense overview that
keeps estimated position skills, confidence, staff assessments, current statistics, known traits, health/readiness/morale,
role/depth context, and recent history visible together. The September 10, 2026 compact mockup establishes the general idea
and hierarchy only; its exact spacing, proportions, wording, sample content, and generated-image imperfections require
refinement before implementation. Additional tabs may hold contract, game-log, career, and full-history detail without
removing the key overview information.

#### Modular 32-bit character presentation

Players, coaches, executives, owners, and other recurring people use a shared modular character system rather than a unique
manually drawn model for every person. Each person receives persistent, deterministic `AppearanceDNA` when created; it is
stored with that career identity and does not randomly change between portraits, meetings, seasons, or save/load cycles.
Appearance DNA selects compatible hand-authored pixel components including skin tone, head and face shape, facial features,
hair and color, facial hair, age details, body archetype, accessories, and supported expressions. The content library must
cover a credible range of ages, builds, roles, and ethnic appearances for both players and staff.

All components follow the same fixed pixel grid, anchor points, limited palette ramps, and hard-edged 32-bit art rules.
Rendering uses nearest-neighbor filtering and integer placement; production characters are assembled from authored pixel
assets rather than generated as smooth vector art or treated as one-off AI images. The portrait renderer and scene renderer
share the same identity layers so a person's face, hair, coloring, and distinguishing details remain recognizable everywhere.

Scene variety comes from reusable body, pose, expression, and outfit templates instead of redrawing identities. The initial
library should support seated-left, seated-right, standing, speaking, listening, writing, and other common meeting poses;
neutral and conversational expressions; and role-driven clothing for players, coaches, executives, owners, scouts, and
medical staff. Team colors and role determine outfit overlays without changing the underlying person. Compatibility metadata
prevents clipping and invalid layer combinations and supplies deterministic fallbacks when a preferred combination cannot be
used.

At runtime, assembled portraits and scene sprites are cached as textures or atlases for performance. Save data retains the
compact Appearance DNA and regenerates the presentation output instead of storing a separate bitmap for every pose. An
internal combinatorial preview and validation tool must render large batches across all component combinations, poses, and
outfits to expose bad anchors, palette mismatches, gaps, and clipping before content ships. Rare marquee characters may use
custom component or full-sprite overrides while remaining compatible with the same base pipeline.

Underlying football ratings use a 1-100 scale. Simulation uses true ratings, but no user-facing screen reveals true values
directly: all displayed values remain the franchise's current staff-knowledge estimate and carry an appropriate confidence
signal. Compact or quick-glance screens such as Roster and Trade Center may use a star summary derived from the current
estimated overall/potential assessment. Stars are a space-saving evaluation shorthand, not more accurate information than the
underlying estimate; detailed screens retain position-specific 1-100 estimated skills and confidence. Overall and potential
are derived evaluation summaries, not magic attributes.

Morale and team chemistry are visible through broad, readable labels rather than falsely precise numeric meters. Their
underlying context is explainable through player/staff reports, role and playing-time status, winning, coaching fit, contract
situation, health, traits, and team environment. They have modest, bounded effects on retention, development readiness,
staff/player interaction, and storylines; they never arbitrarily decide games or overpower talent and management.

#### Trait catalog, discovery, and compatibility

The initial catalog contains the following 53 unique traits. Every trait has a concrete bounded effect and a human-readable
description in the player interface. Position-gated traits occur only for applicable positions; a trait adjusts relevant
situational behavior and never substitutes for the underlying 1-100 rating system.

- **Mental and personality:** Leader (team chemistry and morale stabilizer); Mentor (veteran-only; substantially improves
  relevant teammate learning through mentoring, but is not required to mentor);
  Work Ethic (training and development readiness); Competitive (resilience and high-leverage response); Composed (lower
  pressure volatility); Resilient (recovers morale after setbacks); Team First (role and team-friendly contract acceptance);
  Loyal (retention preference); Demanding (stronger role, money, and success expectations); Role Sensitive (reacts more to
  depth-chart/playing-time changes); Volatile (more reactive morale and conflict); Self-Interested (prioritizes own role and
  compensation); Media Magnet (creates more public-storyline exposure); Hot-and-Cold (greater week-to-week performance
  volatility); Pressure-Prone (higher pressure volatility); and Short Temper (greater conflict and emotional-penalty exposure
  when frustrated).
- **Health, preparation, and development:** Durable (lower injury/recurrence exposure); Injury Prone (higher injury/recurrence
  exposure); Quick Healer (favorable recovery outcomes); Slow Healer (slower recovery outcomes); Elite Conditioning (better
  fatigue resistance and training readiness); Poor Conditioning (worse fatigue/training readiness); Plays Through Pain
  (greater willingness to remain available, with a clearly communicated health risk); Fast Learner (faster scheme/role
  learning); Slow Learner (slower scheme/role learning); Workout Avoider (less reliable training participation/result); and
  Overtrainer (can gain short-term training benefit but carries fatigue/health risk).
- **Football intelligence and style:** Field General (position-appropriate communication and execution); Scheme Savant
  (stronger fit/learning in a compatible scheme); Adaptable (lower disruption from team/scheme/role change); System Dependent
  (larger disruption away from a preferred system); Disciplined (lower avoidable penalty/assignment-error exposure); Penalty
  Prone (higher avoidable penalty exposure); Ball Secure (lower relevant fumble/interception risk); Turnover Prone (higher
  relevant fumble/interception risk); Aggressive (greater disruptive/high-risk tendency); Conservative (lower-risk tendency);
  Freelancer (can improvise and create plays but takes more assignment risk outside structure); High Motor (sustained effort
  and late-down energy); Takes Plays Off (greater effort/fatigue inconsistency); Big-Game
  Performer (bounded high-leverage composure/consistency benefit); Big-Stage Shaky (greater high-leverage volatility); and
  Film Junkie (improves gameplan preparation and opponent familiarity).
- **Situational and position-specific:** Clutch Kicker (kickers only; bounded high-pressure consistency); Cold-Weather
  Specialist (favorable cold-weather adjustment); Dome Specialist (favorable indoor adjustment); Cold-Weather Struggles
  (greater cold-weather volatility); Playoff Performer (bounded postseason consistency benefit); Playoff Pressure (greater
  postseason volatility); Short-Yardage Specialist (eligible offensive front/seam roles;
  favorable short-yardage execution); Red-Zone Threat (eligible skill positions; favorable red-zone execution); and
  Red-Zone Tightens (greater red-zone volatility); Two-Minute Specialist (eligible quarterbacks, receivers, and relevant
  pass-protection roles; favorable late-clock execution).

Trait compatibility is explicit and validated. Directly contradictory pairs cannot coexist, including Durable/Injury Prone,
Quick Healer/Slow Healer, Elite Conditioning/Poor Conditioning, Fast Learner/Slow Learner,
Work Ethic/Workout Avoider, Team First/Self-Interested, Composed/Pressure-Prone, Disciplined/Penalty Prone,
Ball Secure/Turnover Prone, Aggressive/Conservative, High Motor/Takes Plays Off, Big-Game Performer/Big-Stage Shaky,
Cold-Weather Specialist/Cold-Weather Struggles, Red-Zone Threat/Red-Zone Tightens, and Playoff Performer/Playoff Pressure.
The generation and rare-trait-development systems validate this conflict map and the
three-trait maximum before assigning a trait.

Traits are discovered from evidence, not random reveal timers. Examples include repeated practice observations, game patterns,
medical assessments, contract negotiations, depth-chart reactions, staff interaction, media behavior, and postseason play.
The user sees a concise evidence-backed report when a trait becomes known. A player profile distinguishes no currently known
traits from a confirmed absence only when the franchise has genuinely accumulated enough knowledge to make that conclusion.

The league database includes fictional pro players, coaches, and a complete college-football universe at franchise creation.
Standard rosters recreate this population from the fixed standard seed and generator version; generated rosters create
all three populations from the new franchise's world seed.

### Contracts, transactions, and free agency

Offseason preparation includes a factual roster-planning report covering current roster composition and depth, expiring
contracts, positional needs, and the salary-cap outlook. Provide links to the relevant player, roster, and contract details
so the GM can investigate and plan their own moves. Any player evaluation shown follows existing staff-knowledge and
confidence rules. This is roster information, not a staff recommendation meeting: it does not propose replacement targets,
include recommendation acceptance/dismissal controls, or execute transactions. Introduce the report through an inbox message
when the team's season ends, following the same non-playoff/elimination/championship timing used for team-specific offseason
eligibility. Keep it accessible from the Roster screen as a reference guide to important offseason information and roster-building
planning. The inbox entry links to this same guide. Update its current roster, contract, positional-need, and cap information
as offseason moves occur, and show when it was last updated so the user can distinguish the current guide from archived records.
The GM can add personal notes and planning labels to players, such as "extend," "shop in trade," or "replace through draft."
Persist these annotations with the career and retain them when the guide refreshes. Labels express private planning intent;
they do not submit offers, list a player on the trade market, or otherwise execute a transaction. The guide also displays
applicable upcoming roster deadlines, including expiring offers and tag or option decisions, with dates, current status,
and links to the relevant actions. Use the same authoritative deadlines as calendar reminders and deadline prompts rather
than maintaining a separate schedule in the guide.

Contracts include years, annual salary, guarantees, bonuses, and type. A rules service calculates payroll and cap space, validates roster limits, eligibility, contracts, and phase restrictions, and records every signing, release, waiver, IR move, and trade. Contract presentation distinguishes authoritative current commitments from estimated future cap space, showing year-by-year obligations and dead-money consequences under the supported rules. Label projection assumptions and uncertainty; future projections do not grant current spending authority. Free agents can receive offers and sign when valid; waivers and practice squads provide appropriate roster-management choices.

The accepted Contracts presentation direction is a dense multi-year cap worksheet. League cap, existing commitments, dead
money, roster-adjustment estimates, and projected space align by season above player-level cap-hit columns for the same years.
Guarantees remaining, current release dead cap, and the next contract decision or deadline stay visible. Future caps and roster
adjustments are labeled projections rather than current spending authority. The September 10, 2026 mockup establishes this
structure; exact columns, widths, horizon, controls, and illustrative values remain open for refinement.

The contract and salary-cap system follows recognizable NFL principles while deliberately prioritizing understandable GM decisions over legal/accounting minutiae. It presents the meaningful player-facing concepts—salary, guarantees, signing/roster/workout bonuses where supported, incentives, cap hit, dead money, term, extensions, restructures, releases, trades, and applicable tags/options—in plain football language. Obscure contract clauses and calculations that do not create a distinct 1.0 decision are simplified or omitted, and every such exception is explicitly documented in the game and blueprint rather than hidden behind an inaccurate number.

The broader finance layer does not turn the GM into a merchandise or concessions price manager. Stadium upgrades, renovations, and replacement/rebuild decisions are owner-led rather than direct GM purchases. The owner evaluates relevant franchise conditions such as team revenue, competitive success, fan demand, and owner wealth before pursuing a stadium project; the GM sees the context, decision, and resulting franchise impact through appropriate finance, owner, and news surfaces.

Ticket pricing is a direct GM-controlled offseason-only setting. Before confirming a price change, the Finance screen presents its projected, non-guaranteed tradeoffs for attendance, ticket revenue, fan satisfaction, and owner expectations. Actual outcomes respond to the team's on-field success, market/fan demand, stadium situation, and other persisted franchise context rather than a fixed price formula. The price remains fixed during the season, preventing weekly attendance exploits and making the decision part of annual franchise planning.

Contract accounting keeps cash paid, salary-cap charge, and guaranteed obligation distinct. A signing bonus is cash paid up front but its cap charge is prorated across the supported contract years; releasing or trading the player accelerates any remaining unamortized signing-bonus charge into dead money. Guaranteed future salary similarly remains an exit obligation, while an unguaranteed future roster or workout bonus is avoided when the player is legally released before its trigger. A late-contract release can therefore have zero dead money when no unamortized bonus or guaranteed obligation remains. Contracts, offers, trades, extensions, and restructures present the applicable year-by-year breakdown of cash owed, cap hit, guarantees remaining, and dead-money/cap effect. Player release instead uses a compact OOTP-style confirmation popup over the current roster or Contracts table, showing the essential immediate and future financial consequences before execution rather than navigating to a separate release screen.

GM Negotiation may make a valid offer modestly more attractive or improve counteroffer terms, and Player Management
may modestly affect a player's willingness to stay. These are capped influences inside the contract and happiness
rules, not automatic outcomes or exceptions to salary-cap and roster rules.

Players may decline extension discussions because they want to test free agency. Communicate that intention through their
agent and distinguish unwillingness to negotiate now from rejection of a particular offer. Interest in testing the market
does not itself terminate an existing contract or bypass applicable rights and signing windows.

Where contact is permitted by league rules, the GM can make an informal agent inquiry about a player's interest and approximate
contract demands before submitting a formal offer. Present the response as a current, nonbinding indication rather than a
guaranteed acceptance threshold; interest and demands can change with the market and player circumstances. An inquiry does
not create an offer, reserve the player, or commit team funds. Preserve useful inquiry context in the negotiation view.

Contract negotiations are direct, explainable offer/counteroffer discussions with the player's agent. An agent evaluates the
complete offer and the player's situation: money, years, guarantees, bonus structure, expected role, team outlook, market
demand, player traits, morale, and relevant GM attributes. Agents have distinct patience levels that govern how long they are
willing to entertain negotiations, counter repeatedly, or wait for the market. Patience and urgency are communicated through
clear context and deadlines rather than arbitrary hidden timers. A counteroffer explains its meaningful priorities whenever
the game can do so without revealing protected AI or player information; acceptance and rejection remain subject to all cap,
roster, phase, and contract validation.

The GM can revise or withdraw an outstanding offer before acceptance. Revisions supersede the earlier terms, and withdrawals
close that offer; a later response cannot accept terms that are no longer available. These controls do not undo an executed
contract. Preserve offer history and validate the current offer state when resolving each response.

Worsening or fully withdrawing an offer may upset the player or agent, depending on personality, patience, negotiation history,
and circumstances; it is not an automatic penalty every time. Repeated lowball offers or withdrawals can damage the negotiation
relationship. An upset player or agent may refuse further talks. When they also have significant leverage, such as strong
competing demand or the team's urgent need, a poor relationship may instead lead to substantially higher asking terms.
Explain the stance through agent feedback without exposing hidden thresholds. Improving an offer does not automatically erase
earlier friction or guarantee that talks reopen. Exact reaction strengths and relationship-recovery rules remain to be defined.

Agents have persistent identities and may represent multiple players. Track the GM's relationship with the agent separately
from the relationship with each player. Prior dealings with an agent can affect trust, patience, and bargaining stance when
negotiating for another client, while that client's own preferences, circumstances, and market leverage still matter. One
dispute does not automatically make every client refuse the GM. Preserve representation links and relationship history
across seasons and save/load.

Refusals to negotiate can take different forms: a defined cooling-off period, a firm refusal for the rest of the offseason,
or a stance that may change when the market disappoints the player or agent. Reopening depends on the actual refusal state,
personality, relationship, and changed circumstances rather than repeated clicks or a guaranteed universal reset timer.
An offseason-long refusal remains binding for that offseason. For conditional refusals, weaker competing interest or offers
may prompt the player/agent to reconsider. Communicate the current stance and any disclosed reconsideration conditions through
the negotiation view, and notify the GM when talks reopen without promising a recovery date the agent has not supplied.
Persist refusal scope, applicable expiry or conditions, and reopening events so save/load cannot clear a refusal.

Agents communicate competing interest through broad signals, such as another team offering more guaranteed money, rather
than exposing exact rival bids. Ground these signals in the actual negotiation state and information the agent can disclose;
the negotiation interface must not reveal other teams' private contract terms through a comparison table or hidden-value hint.

#### Retention tools: tags and rookie options

1.0 includes franchise and transition tags as distinct retention tools. Enforce designation eligibility, the shared team
designation limit, applicable tender cost, negotiation/matching rights, and deadlines through versioned league rules. Present
the financial commitment and retained rights before the GM confirms a designation. Track designation, unsigned tender,
signed contract, and any valid withdrawal separately; applying a tag is not player acceptance of a contract. Resolve competing
offer sheets and compensation only through the applicable tag rules, not the ordinary free-agent confirmation flow.
Present tags and restricted/exclusive-rights tenders together in one Contracts worksheet. Its unified eligible-player list
shows available control, cost, retained rights, deadline, and filed/pending status; selecting a player changes the lower filing
worksheet to only the designation or tender choices valid for that case. Decisions are confirmed individually rather than
through a bulk-submit action.
Use [NFL Football Operations' tag guidance](https://operations.nfl.com/calendar-events/nfl-free-agency/franchise-tags)
as a reference. Franchise-tag variants, exact calculations, repeat-tag escalation, and offer-sheet workflows require a focused
rules pass before implementation.

Unhappy tagged players may hold out, seek a trade, or push for a long-term contract. Reactions depend on personality,
relationship, financial security, and leverage; a tag does not automatically provoke a holdout. Make demands and participation
status visible through agent feedback, player context, and relevant news. A demand does not force a trade or override the GM's
transaction authority. Distinguish an unsigned tender from refusal to participate under a signed contract when applying
availability, financial, and eligibility consequences. Exact holdout consequences and resolution rules remain to be defined.

First-round rookie contracts include a fifth-year team option. The GM receives an explicit exercise/decline decision within
the applicable window, with the option salary, guarantees, cap impact, and deadline visible before confirmation. Persist the
decision and resulting obligations; declining the option does not immediately terminate the remaining rookie contract.
Present the decision period as an OOTP-arbitration-style worksheet under Contracts: one list of the user franchise's eligible
players, option figures, deadlines, and filed/pending status, with the selected player's financial impact and exercise/decline
choice below. This borrows the list-based case-management structure, not baseball arbitration offers, demands, or hearings.
Use [NFL Football Operations' rookie-contract guidance](https://operations.nfl.com/calendar-events/nfl-free-agency/contract-language)
as the reference for timing after the third regular season and guarantees upon exercise. Exact salary-tier calculations and
the mapping of real-world selection criteria to this game's All-Star honors remain part of the rules audit. Surface tag and
option decisions in the offseason roster guide, calendar reminders, and deadline prompts.

#### Free-agent evaluation and market preferences

The GM can invite eligible free agents for workouts and medical evaluations before making a contract offer. Schedule the
evaluation and deliver its results to the inbox after a few in-game days, with links to the player and relevant report.
Workouts inform football/readiness assessment; medical evaluations inform health and participation-risk assessment. Reports
use the existing staff-knowledge, confidence, and staffing-capacity rules rather than revealing hidden true ratings or
guaranteeing future health. Preserve the evaluation date and findings. An invitation or pending evaluation does not reserve
the player or prevent a signing elsewhere. Exact duration, capacity, and invitation-acceptance rules remain to be defined.

Older veterans may favor a contender or meaningful playing time over the largest offer, but preferences vary by individual.
Evaluate money, role, team outlook, personality, career circumstances, and agent priorities together; age does not assign every
veteran the same priorities. Agent feedback should communicate relevant preferences without promising an outcome.

Unsigned players may lower their contract demands as the offseason progresses and market opportunities diminish. Adjust
demands in response to actual interest, competing offers, time unsigned, and player/agent patience and circumstances rather
than applying a guaranteed universal price drop. Some players hold their price longer. Updated demands and any willingness
to reopen talks flow through the existing agent and negotiation systems while preserving binding refusal periods.

The free-agent list supports filtering by position, estimated ability, age, and known asking price. Team interest is not a
list filter; learn relevant interest through the existing agent inquiry and negotiation flow. Unknown asking prices remain
clearly unknown rather than fabricated or inferred from hidden demands.

The GM can create multiple shortlists with freely entered, user-written names. Names are not selected from predefined
categories or restricted to game-supplied labels. Support renaming lists and adding or removing players; a player may appear
on more than one list. Persist list names and membership with the career rather than imposing a single fixed shortlist.
Shortlists persist across seasons until the user manually edits or deletes them. Season rollover does not clear names or
membership, and a player's signing or team change does not automatically remove them from a list.

Relevant news about shortlisted players is delivered to the inbox with links to the player, story, or report. Do not create
separate shortlist pop-ups or alert messages. A player appearing on multiple lists does not generate duplicate copies of the
same news item. Inbox delivery alone does not stop time; a separately configured optional stop may still apply to a relevant
event under the existing advancement preferences, without adding a separate shortlist notification overlay.

Shortlisted players can be compared side by side using known rating estimates and confidence, production, health, and known
contract demands. Use aligned rows, consistent units and timeframes, and clear labels so differences are easy to scan without
crowding the screen. Missing information remains visibly unavailable. Keep comparison focused on a readable number of players
at once. The accepted September 14, 2026 direction is a dense worksheet supporting up to four player columns beside a fixed
label column. Flat Overview, Attributes, Production, Contract, and Scouting subheaders change the aligned information group;
the default Overview combines identity, experience, playstyle, key estimates, current and career production, contract summary,
and availability. Comparison does not declare a winner, calculate a synthetic score, or recommend a best fit. It must respect
the same knowledge boundaries as player profiles and must not reveal hidden true ratings or private demands merely to fill an
empty field. The first comparison mockup establishes structure and density only; exact values, spacing, and generated-image
imperfections remain subject to implementation refinement.

#### Owner-controlled facilities and stadium projects

Owner investment is organized into Stadium, Facilities, and Operations, each with its own sub-upgrades. Facilities groups
player-related spaces and resources; Operations groups football-operations resources such as scouting tools and staff
workspaces. The specific sub-upgrade lists remain under review.
Owners make all approval, funding, and project decisions. Potential projects and GM feedback are discussed in owner meetings,
but the GM cannot directly purchase an upgrade or turn a request into guaranteed approval. Apply owner personality,
priorities, actual resources, and franchise circumstances to the decision.

An approved project requires owner funds sufficient for its applicable costs and commitments; owner wealth is a tracked
resource rather than an unlimited source of money. Account for project funding and expenses without charging the same cost
twice across owner and franchise finances. Construction takes an appropriate amount of in-game time, with costs, progress,
completion, and applicable ongoing operating expenses persisted and communicated through owner/finance context. Benefits
begin when the project is completed, not merely announced. Resource upgrades use appropriate acquisition/setup timing where
physical construction is not relevant. Exact costs, durations, effects, and allocation of ongoing expenses remain to be defined.

Facilities age over time and require owner-funded maintenance or renovation. Track facility age and condition separately
from upgrade level so a previously improved facility does not remain in peak condition indefinitely without upkeep. Owners
decide and fund maintenance and renovation under the same resource and project rules, with condition, needs, and decisions
communicated in owner meetings and finance/facility context. Exact deterioration rates, maintenance schedules, renovation
durations, and effect strengths remain to be defined. Poor condition reduces the facility's relevant benefits and can
increase associated costs or risks. Tie consequences to the facility's actual function and condition rather than imposing
an unrelated universal penalty; avoid duplicating an effect already attributed to missing staff or another system.

Facility operating costs, maintenance, owner funding, and project spending appear in Team Finances and Accounting rather than
a standalone Facilities finance screen. Current facility grades, condition context, stadium facts, and owner-project status
appear on Team Info under Team. Distinguish announced, requested, funded/in-progress, completed, and uncommitted projects,
with links to relevant owner decisions and financial records. Team Info is an information and tracking surface; it does not
give the GM direct purchase, maintenance, funding, or project-approval controls.

The September 10, 2026 Team Info mockup is accepted only as a provisional structural reference. Its hierarchy, proportions,
content balance, wording, facility-grade presentation, project treatment, and visual polish require a later refinement pass.

Owners may renovate, expand, or replace the stadium while keeping the franchise in its current city. Apply the same
owner funding, approval, construction-time, and expense rules used by other projects. Track the specific scope and timing
of each project so construction disruption depends on the work actually performed rather than a penalty for every upgrade.

Stadium upgrade ideas under discussion include Renovation/Rebuild, Seating, Screens and Speakers, Field Quality,
Facilities Quality (bathrooms and similar stadium amenities), Parking, VIP Seats/Boxes, and Concessions. This entire list
is brainstorming, not approved categories or committed scope. Keep stadium Facilities Quality distinct from football
training, medical, and scouting facilities. Final categories, benefits, and project options remain to be reviewed.

For example, a major renovation during the season may close seating sections, reducing usable capacity and therefore
potential attendance and ticket revenue while those restrictions apply. Calculate attendance against the temporarily
available seats and existing demand/pricing factors; closed seats do not imply that every game otherwise would have sold
out. Show temporary capacity restrictions, affected dates, and financial effects in Facilities and relevant stadium/finance
context. Restore availability as the affected work completes. Projects without relevant game-day disruption do not incur
an automatic attendance reduction.

#### Financial reporting and projections

Finances presents the yearly budget alongside actual income and expenses, including player payroll, GM/staff compensation,
stadium operations, facility/project expenses, and owner funding. Distinguish owner cash injections from operating revenue
and show budget-to-actual differences with understandable categories. Keep player salary-cap accounting separate from cash
spending, while linking to the relevant contract and cap views. Use recorded transactions and obligations consistently;
do not count an owner-funded project twice as both owner and franchise expenditure in consolidated totals.

Team Finances uses a dense, number-first worksheet rather than a visual dashboard. Small summary figures may remain, but the
main surface prioritizes aligned financial columns such as budget, actual, variance, forecast, prior year, and league rank.
Attendance, ticketing, owner funding, projects, and upcoming decisions appear as compact numeric tables and routes to detail,
not portraits, facility renderings, charts, or large KPI cards. Accounting remains the transaction-level ledger; this overview
is the comparative operational statement.

The September 10, 2026 number-first Team Finances mockup is accepted as the structural reference. Its exact column widths,
line categories, comparison periods, controls, and illustrative values remain open for refinement; production totals must
reconcile to authoritative accounting records and forecast assumptions.

Include future financial projections that clearly distinguish committed contractual expenses from estimated costs and
uncertain revenue. Show the forecast period and key assumptions, including attendance, pricing, construction restrictions,
and known staffing/player commitments where applicable. Label forecasts as estimates, refresh them as decisions and actual
results change, and preserve historical actuals. Pending offers and unapproved funding/project requests remain hypothetical
planning items, not committed income or expenditure.

Accounting is the authoritative category-level and month-by-month financial record, not a transaction-entry ledger. Its
category statement separates revenue, operating expenses, owner funding, and project/capital spending; its monthly table
shows operating result, funding, capital outflow, net cash change, and ending cash. Supported categories may link to their
originating game-system records for verification, but the UI does not expose accounting entry IDs or require the user to
browse every posted transaction.

The September 10, 2026 Accounting mockup without a transaction ledger is accepted as the structural reference. Its exact
category depth, comparison columns, monthly fields, controls, and illustrative values remain open for refinement; category
and monthly totals must reconcile to authoritative financial state.

When franchise cash runs low, the owner may inject available funds or impose spending restrictions according to resources,
personality, and franchise circumstances. Explain the decision and any revised spending limits through owner/finance context.
Funding does not create player salary-cap space, and a restriction does not erase existing contractual obligations.
Normal financial pressure appears as tighter spending budgets, denied or delayed upgrades, and owner demands for improved
financial performance. A few poor seasons or bad contracts must not create a routine bankruptcy spiral or threaten franchise
survival as a normal GM gameplay consequence. Serious ownership financial problems are rare and may contribute to a sale
through the existing ownership-change system; they are not an automatic consequence of an operating deficit. Preserve
meaningful expenses and funding constraints without adding a routine franchise-insolvency management system. Exact budget
thresholds and rare ownership-event frequency remain balancing details.

Financial trouble can influence owner expectations and GM job security, but the assessment must reflect the GM's actual
responsibility and authority. Consider GM-controlled contracts, staffing choices, and other approved spending decisions
separately from owner-directed projects, denied funding, inherited obligations, and external revenue changes. Preserve the
decision history needed to explain the assessment; do not automatically blame the GM for every deficit. Communicate concerns
through the existing feedback and owner-review process, preserving offseason-only dismissal and agreed-expectation rules.

#### Signed-player holdouts and restructures

Players already under contract may hold out over compensation or a desired extension. Resolve these disputes using player
and agent personality, relationship, existing terms, and market leverage. Communicate the demand, participation status, and
applicable consequences clearly; holding out does not void the contract or compel the GM to grant an extension. Distinguish
these disputes from unsigned tag tenders. Exact participation, pay/fine, and eligibility rules require the focused rules pass.

The initial holdout notice is an authored Inbox message from the player's representative, not a standalone holdout dashboard.
It states the request, signed-contract context, participation status, and current team availability, then links to the existing
player profile, contract detail, and contract-discussion workflow. Applicable attendance and financial consequences are tracked
automatically; the GM is not given a decorative punishment button or forced into an immediate one-click resolution.

1.0 supports contract restructures but does not include a standalone request to cut a player's agreed pay. Do not disguise a
pay reduction as a restructure. Before submission, show the proposed contract against its existing terms year by year,
including immediate cap savings, added future cap charges, cash-payment timing, guarantees, and any change in potential dead
money. Make clear that moving cap charges into later years is not eliminating the obligation. The workflow begins from the
player's nested right-click action, **Contract > Restructure**, which sends only an inquiry about willingness to discuss a
restructure. The player's yes-or-no response arrives through an Inbox message after a few in-game days. A positive response
opens the restructure-terms screen; a negative response ends the inquiry without presenting editable terms. The GM then
proposes exact accounting terms through that discussion, with later replies returning through Inbox. Validate each proposal
before submission. If terms are accepted, apply the accepted proposal once and update finance projections and transaction
history once. Exact supported restructure mechanisms and response timing remain to be defined in the contract rules pass.

#### Negotiation presentation and tracking

The negotiation view combines an editable contract offer, a clear year-by-year financial breakdown, and a conversation with
the agent. Agent feedback hints at which parts of the current offer are acceptable and which need work, such as guarantees,
salary, term, or role expectations. Tie feedback to the actual proposal and the client's priorities so it helps the GM revise
the offer; do not substitute generic chatter or expose exact hidden acceptance thresholds. Keep the latest response and the
offer version it addresses clear, with access to earlier discussion and revisions. Financial presentation follows the existing
cash, cap-hit, guarantee, and dead-money breakdown rules.

A central negotiations view tracks active talks, current offers, latest responses, and deadlines across players. Keep it
compact and actionable: show player/agent identity, negotiation status, whose response is pending, relevant timing, and a
direct route to the detailed conversation and offer. Use the same authoritative offer state as player profiles and inbox
messages, without creating a second negotiation workflow or exposing exact private rival bids. Its navigation placement and
detailed layout remain to be reviewed in the UI pass.

Submitting a formal offer does not itself authorize the final signing. Player/agent acceptance moves the proposal into an
"Accepted — awaiting your confirmation" state. Show the agreed terms and current financial impact, then require the GM to
explicitly confirm before executing the contract. Revalidate player availability, the accepted offer version, cash/budget,
salary cap, roster capacity, and applicable rules at that moment. If another signing or changed circumstance makes the deal
invalid, explain what must be resolved rather than silently signing or rewriting its terms. Confirmed signings alone create
contract obligations and roster changes; do not count pending acceptance as a completed transaction.

The GM may maintain multiple offers whose combined cost exceeds current cap space or available funding, provided individual
offers obey their applicable validation rules and each final signing is affordable and legal when confirmed. Pending offers
do not reserve or deduct actual cap space or cash. In the negotiations tracker and signing preview, distinguish actual current
cap space and cash/budget from projected impact of unconfirmed offers. Show the effect of the selected deal and a clearly
labeled "if all pending offers were signed" projection, with awaiting-response versus accepted-awaiting-confirmation status
visible. Hypothetical deficits are planning warnings, not existing cap violations. Use cap hit for cap projections and cash
obligations for funding projections; refresh both after each confirmed transaction. Do not label a hypothetical remainder as
actual available cap.

Use five in-game days from player/agent acceptance as the provisional final-confirmation window, with a seven-day window
retained as a balancing alternative. Display and persist the exact deadline and issue the standard advance reminder and
deadline prompt. This is an in-game design value, not an asserted NFL contract rule. A confirmation window never overrides
an earlier applicable league signing or eligibility deadline; disclose any resulting earlier cutoff when the agreement is made.

An accepted agreement is exclusive while awaiting the GM's confirmation. The player cannot entertain, accept, or sign another
team's offer during that window. Suspend competing negotiations and allow only one accepted agreement per player. This
exclusivity reserves the player's availability, not team cash or cap space. If the deadline expires without a valid confirmed
signing, the agreement lapses and the player can return to the market; never auto-sign on the GM's behalf. Letting an accepted
agreement expire can damage the player/agent relationship, using the same context-dependent reactions as withdrawing an offer.
Consider personality, patience, negotiation history, and circumstances rather than applying an identical penalty every time.
Explain the reaction and any resulting refusal or changed demands through agent feedback. If the GM explicitly
withdraws the accepted agreement, release the player immediately and apply the existing context-dependent relationship
consequences. Revision cannot silently change accepted terms or reset the countdown: changed terms require a new agreement.
Persist the exclusivity state and original deadline so save/load or repeated confirmation attempts cannot extend the hold.

Free agency is a live, non-exclusive market. A player may negotiate with and receive offers from multiple teams at once;
competing offers, bidding wars, agent updates, and market deadlines are surfaced through the negotiation and Inbox systems.
Before accepting an offer, a player considers it over an explainable period. Depending on patience, competing offers, urgency,
and desire to play the market, they may respond the same day, after one or two days, or wait weeks for additional opportunities.
During this pre-acceptance period they may choose another valid offer. Once they accept terms, the exclusive confirmation
window defined above applies (provisionally five days); they cannot accept another team's offer while that agreement remains
active. A user-team signing still requires the GM's final confirmation and current financial/roster validation. The system
warns of known urgent decisions without claiming an exact response time the franchise could not realistically know.

### Draft, scouting, trades, and AI front offices

Each offseason has prospects, draft order, picks, user draft board, selections, rookie contracts, undrafted free agents, and draft history. Scouting provides estimated ratings, confidence, reports, traits, interviews, public combine data, and attendance-based pro-day observations; it does not reveal hidden truth automatically.

The GM may either directly control scouting assignments or delegate them to the scouting department. Direct control supports assigning available scouting capacity to supported prospects, position groups, regions, schools, and priorities; delegation lets staff choose assignments using their role, tendencies, knowledge gaps, and franchise needs. The chosen mode is visible and reversible. In either mode, scouting results remain estimates derived from real accumulated knowledge, respect staff aptitude and capacity, and never reveal hidden ratings or traits automatically.

Each scout has individual accuracy, speed, and role-relevant skill traits. Accuracy and skill determine how closely an evaluation approaches a player's true ratings and the width of any displayed rating-confidence window; speed and workload determine how many players the scout can meaningfully evaluate over the selected scouting time frame. The interface makes scout capacity, assignment duration, estimate confidence, and known limitations clear, so a highly rated report represents better information rather than an unexplained reveal.

Scouting uses a weekly cadence throughout the pro and college seasons and the offseason. Assignments update when the calendar advances a week; longer sustained assignments accumulate deeper knowledge over multiple weeks instead of resolving as instant reveals. Offseason scouting events and deadlines may add their own reports or availability windows, but they use the same persisted knowledge and confidence model.

Pro scouting includes ongoing routine coverage that consistently updates knowledge of professional players without requiring
the GM to assign each player individually. Specific assignments add focused attention to selected players within available
scouting capacity. Routine coverage is not an instant league-wide refresh or guaranteed complete knowledge: reports update
from actual observations, with dates and confidence reflecting staff coverage and workload. An unobserved player retains the
last evaluation until new evidence is obtained. Public statistics continue to update independently, and coaches may report
observed changes in the user's own players under the existing rules.

College scouting always requires an assignment, whether chosen by the GM or created through delegated scouting management.
Assignments may be set to ongoing ("permanent") and remain active until canceled, continuing to consume capacity and produce
observations through the normal scouting cadence. Persist their scope and ongoing status so they do not require repeated
weekly setup. Without an assignment or an explicitly attended scouting event, college prospects receive no automatic private
evaluation refresh; public information remains available under the existing Combine, statistics, and media rules.

Pre-draft evaluation includes public combine information, attendance-based pro-day scouting and selective media coverage, plus GM-selected combined private workout/medical appointments and interviews. Combine participation is not universal: invitations depend on the prospect pool and league process, and an invited prospect may opt out of the combine or individual drills. Missing or partial public results are presented as unavailable information, not as a hidden penalty or an automatic negative assessment. Private evaluations are limited, scheduled decisions that add evidence to the same knowledge model rather than exposing a prospect's true ratings.

The day before the draft, send a simple inbox reminder to review the private Team Draft Board, with a direct link to it.
There is no separate pre-draft review screen, meeting, or roster/scouting briefing. The reminder does not require the GM to
change or approve their board; existing roster-planning and scouting information remains available through its normal surfaces.

#### Combine presentation and prospect interviews

A dedicated Combine screen is available during the offseason. It presents recorded prospect drill results with position
filters and comparable drill measurements, plus links to prospect profiles. Before results are available, show the scheduled
event and pending state; never expose future results. Distinguish nonparticipation or skipped drills from poor performance.
Combine measurements supplement the existing scouting picture rather than revealing true football ratings directly.

Use the Combine prospect list to select interview and combined workout/medical targets with separate checkboxes beside
each prospect. Show the selected count against each independent allowance so the GM can assemble the two lists together.
The same prospect may be selected for both, using the corresponding separate allowances. Checkboxes remain freely editable
until the GM presses Confirm. Before confirmation, selections show projected allowance usage without spending slots.
Confirm books the selected appointments and consumes their corresponding interview/evaluation slots. Persist bookings and
usage together, counting each appointment once; reopening the screen or reading results does not spend additional slots or
restore confirmed slots. Booking does not itself complete an interview, reveal findings, or turn a private evaluation into
a public combine result.
After selecting targets, the GM takes manual interviews one at a time or delegates selected interviews to the Head Coach.
Preserve the selections, delegation choices, and interview progress so the
workflow can be resumed. Keep evaluation selection distinct from its previously specified result-delivery timing.

Prospects attending the Combine do not decline or cancel a selected interview or combined workout/medical evaluation.
Selection of an attending prospect guarantees participation in that appointment, not a particular discovery or favorable
result. Medical restrictions still govern which physical activities can safely be performed; a restricted drill does not
cancel the evaluation or its medical assessment. This participation rule is separate from whether a prospect attends the
Combine at all and does not change the established pro-day attendance or drill opt-out rules.

The accepted starting allowances are 15 prospect interviews and 30 combined workout/medical appointments per team per draft
cycle. Slots are consumed on booking confirmation; delegation does not increase either allowance.

Each franchise receives 15 prospect interview slots per draft cycle, requiring the GM to prioritize
whom to meet. Staff quality or franchise resources do not increase this allowance.
Show total, used, and remaining interview slots before committing to a
prospect, and persist usage for the cycle. A new draft cycle receives its own allocation; reopening screens or reloading does
not restore spent slots.

For each manually interviewed prospect, the user chooses up to five questions in total for that draft cycle from a selectable pool
of roughly 20-25 possible questions. Returning to the interview does not grant another five questions.
Questions explore relevant personality, mental qualities, and football understanding. Responses reflect the particular
prospect. Every question answered reveals something useful about that player: work ethic, a trait, a mental skill, or a
personal value. If a question concerns a specific trait the prospect does not possess, its result explicitly confirms that
the player does not have that trait. This is a recorded negative finding, not an unknown trait or an unexplained empty
result. The information need not always be a newly confirmed named trait; it can be a relevant mental-skill evaluation or
a disclosed preference or preparation habit. Confirmed absence applies only to the specific trait examined, not all traits.
Convey that absence naturally through the prospect's response and staff interpretation, not a literal "does not have this
trait" message or a checklist of missing traits. The underlying scouting knowledge records the specific finding while the
user-facing report describes what was learned in ordinary football language.
Show each question with a broad topic label, such as Preparation or Teamwork, without exposing its hidden-trait mapping.
Apply the existing evidence-based trait-discovery and staff-knowledge rules, distinguishing an impression or clue from a
confirmed trait. Do not expose exact hidden mental ratings or treat every answer as perfectly truthful or conclusive.

The GM may conduct some interviews personally and delegate others, including all selected interviews, to the Head Coach.
Delegated interviews consume the same prospect slots and five-question allowance as manual interviews. The coach selects
varied questions for different prospects, with randomized question selection and discovery outcomes representing those
different conversations. Findings must remain grounded in the prospect's actual personality, traits, and answers under the
existing evidence rules. Every delegated answer also provides a useful finding; randomness determines which information is
uncovered rather than whether the answer reveals anything. Delegation does not generate new traits. The GM receives the resulting
staff notes and discoveries in the same interview/scouting report without having to play through each conversation.
Persist questions, answers, findings, and completion state so save/load, reopening results, or changing delegation cannot
reroll discoveries or grant extra questions. A partially completed interview retains only its remaining question allowance
when handed to the coach. Delegation requires an available Head Coach.

Show the question allowance, questions already asked, and the prospect's responses in a readable conversation. Preserve the
interview record and resulting knowledge so reopening the screen or reloading cannot reset the allowance or reroll answers.
Interview answers update the prospect's scouting report with staff interpretation and supporting notes, presented in a
dedicated Interview Results section or a clearly identified part of the mental evaluation. Preserve the source answers and
distinguish staff interpretation, uncertain evidence, and confirmed discoveries. This is part of the same scouting record,
not a separate competing evaluation. The exact question catalog and final
report-section layout remain to be defined; resuming an interview can use only that prospect's remaining question allowance.

Private prospect workouts and medical evaluations form one combined evaluation appointment. Every team receives the same
allowance of 30 appointments per draft cycle, separate from its interview allowance. One appointment covers both
the workout and medical evaluation for the selected prospect; do not charge two separate allowances. Show total, used,
and remaining appointments and persist usage and results. Results from these combined private prospect evaluations become
available immediately after the last pro day of the cycle ends, delivered through the inbox and added to the corresponding
scouting reports. This timing is distinct from attended pro-day observations, which update after each event, and free-agent
evaluations, which use their own turnaround. The exact booking cutoff remains for the calendar pass.
Combine attendees participate when selected under the rule above; there is no prospect decline/cancellation step for them.

College pro days use a staff-allocation model inspired by NFL Head Coach 09, spread across a provisional two-week offseason
window. This duration is a game-pacing choice rather than a claim about the real NFL circuit; exact length remains tunable.
Multiple colleges hold pro days on the same in-game date. As each event day arrives, return control to the GM to decide which
colleges to cover that day and how much of the day's available staff allowance to allocate to each. Allow the GM to deliberately skip
coverage and continue; do not silently assign staff or advance past the day's allocation opportunity.
Show the event schedule, participating college/prospects, total daily staff allowance, allocated amounts, and remaining
staff together. The GM assigns a number to each college, not named staff members or specific roles. Total allocations cannot
exceed that day's allowance. Each new pro-day date provides its own daily allowance; persist allocations and resolved events
so reopening or reloading cannot create additional coverage. The exact daily allowance remains to be chosen.

Allocating more staff provides more knowledge about players at that college and can improve evaluation accuracy.
Apply bounded gains within the existing scouting-confidence model;
additional observers do not guarantee perfect accuracy or reveal true ratings. Deliver resulting observations into the same
prospect scouting reports, preserving the event date and evaluation source. This daily allocation model replaces individual
role selection, staff travel scheduling, and tracking which coach's other duties are displaced. Exact coverage/accuracy
effects remain for balancing. Pro-day staffing is distinct from the equal interview and combined private-evaluation allowances.

Prospects may choose not to participate in their college's pro day. Participation decisions use explainable football and
draft-market context rather than an arbitrary attendance roll. Consider health and medical restrictions, existing evaluation
evidence, current perceived draft stock, and the expected opportunity to improve that stock by performing. A prospect with
something to prove may attend to strengthen their standing; an injured prospect may refrain, and one satisfied with existing
results may see less benefit in another workout. Player and agent preferences inform the decision without guaranteeing that
attendance improves stock or that absence lowers it. Base perceived stock on the existing evaluation/market model rather
than knowledge of future draft outcomes. Record participation and any known explanation; never award firsthand pro-day
scouting gains for a prospect who did not participate. Surface disclosed absence reasons in event context and relevant media
without exposing undiscovered medical details. Exact participation weights remain a balancing decision.

Prospects may attend while skipping individual drills when supported by drill-specific reasoning. Examples include retaining
a strong valid combine measurement, avoiding a drill affected by a known injury or medical restriction, or focusing on drills
where another showing has a plausible chance to improve their evaluation. Compare prior evidence, preparation/readiness,
relevant health restrictions, and player/agent priorities; do not decide skipped drills by an unexplained independent coin flip.
Mark each drill as completed, planned, skipped, or unavailable as appropriate. Retain prior results with their original source
and date, and apply new scouting evidence only for activities actually observed. A skipped drill is not a zero result or an
automatic rating penalty; staff may still evaluate uncertainty in context.

Before assigning scouts or other staff, the pro-day schedule shows expected participants for each college with current public
draft-stock context next to their names. Use the existing public evaluation/board model, not hidden true ability or guaranteed
draft positions. Show known participation plans and restrictions where available. Expected attendance can change, including
late withdrawals with a supported reason; distinguish the pre-event expected list from the actual attendance retained in the
final report. Staff allocation decisions use the information available at that time rather than foreknowledge of withdrawals.

Update participating prospects' scouting reports after each attended pro-day event resolves, making the new findings available
immediately for subsequent planning and evaluation decisions. The final summary does not delay access to these findings or
apply the same scouting gains again.

After the entire pro-day window concludes, send one consolidated results message to the inbox rather than a separate update
for every college. The report lists which colleges the franchise scouted and, under each, the players who actually participated
in that event, with links to their scouting reports. Preserve attendance and participation as event-time records, independent
of later roster or eligibility changes. Collect the franchise's pro-day findings into this summary while maintaining their
underlying source observations in the existing prospect scouting records. Deliver the consolidated report once per draft
cycle, with persisted delivery state; do not generate per-college result pop-ups or inbox messages. Selective media stories
remain part of the separate league-news system.

Assign staff to the whole college pro-day event, not individual prospects. All participating prospects receive the same
amount of knowledge/scouting coverage from that attendance; there is no per-player priority allocation within the event.
This equal coverage does not make their total accumulated knowledge or final confidence identical, since prior observations
can differ.

Unattended pro days do not automatically supply public drill-result tables or direct scouting observations. Instead, media
may publish broad positive or negative impressions, primarily about higher-ranked prospects who attract coverage. A headline
might praise a standout performance or question a disappointing showing; coverage is selective and not guaranteed for every
prospect or event. Treat it as attributed media opinion, not measured results, confirmed traits, or equivalent to attending.
Keep media impressions visibly distinct from the franchise's firsthand scouting evidence. The public Combine screen remains
a separate source; its availability does not grant access to unattended pro-day measurements.

The draft supports both a live multi-round Draft Room with a real countdown for every selection and a fast-simulation mode that advances AI selections efficiently until the user's next decision point. The GM can change the preferred pace during the event. Fast simulation preserves completed selection history and all user decisions; it never silently makes a user pick, accepts a trade, or bypasses a required user-facing deadline.

Draft selections receive a presentation announcing the selecting team, pick number, player portrait, name, position, and
college. The first ten overall picks receive longer, more elaborate cinematics; subsequent picks use shorter, simpler
announcements to keep the remaining rounds moving. Presentation reflects the committed selection, including the actual
selecting team after any trade, and never changes the draft outcome. Every pick cinematic is skippable, and the user can
enable automatically shortened announcements for subsequent picks. Preserve that presentation preference with the career.
The next selection's clock starts only after the announcement finishes or the user skips it; watching a cinematic never
consumes the next team's decision time. Skipping does not skip a selection, execute a trade, or change a draft outcome.
Exact presentation durations remain to be defined in the UI pass.

Fictional analysts react to notable selections, perceived reaches or steals, and draft-day trades. Commentary uses public
boards, known production, public evaluation context, and team needs rather than hidden true ratings or future career outcomes.
Opinions can differ between analysts and can prove wrong; a "steal" label is a contemporary assessment, not a guaranteed
development result. Keep reactions contextual and varied rather than forcing a dramatic verdict for every pick. Integrate
them into draft presentation and the existing news/ticker context without exposing the GM's private board or scouting knowledge.

Draft-day trades are available through the same validated player/pick package rules as the broader Trade Center. AI teams may trade with one another, send the GM realistic trade-up/trade-down offers, and respond to GM proposals while the user is on the clock. Trade frequency is restrained and context-driven by pick value, prospect availability, roster need, cap/strategy, draft capital, and clock urgency; the draft does not manufacture offers or movement merely to make every round busy.

If the user allows a live draft clock to expire, the game does not pause or automatically select a prospect. The next team on the clock may submit its selection first, jumping the user as in a real missed pick; the user's unsubmitted selection remains available to make afterward, subject to the evolving available-prospect pool. The Draft Room makes this status unmistakable and continues to protect the user from an unintended automatic pick or accepted trade.

The private Team Draft Board is a feature-rich GM workspace available throughout scouting and the draft. It supports manual prospect ranking, drag/reorder controls, named tiers, personal notes, target/avoid tags, position and scouting-confidence filters, and relevant need/fit context. It preserves the GM's private evaluations and never substitutes a public board or hidden true rating for the user's chosen ordering. During a live selection, the board remains immediately available alongside the clock, draft order, known offers, and prospect detail.

After the final pick, the game enters a dedicated Undrafted Free Agent market screen inspired by NFL Head Coach-style post-draft free agency. The GM can open negotiations and sign eligible undrafted players while all teams compete for them. It uses the same offer, agent, and competing-market logic as free agency at an accelerated pace: a player may agree immediately or wait roughly one or two in-game days to test the market. Final signing still requires GM confirmation, and an accepted agreement uses the existing temporary exclusivity and confirmation-deadline rules. Open discussions alone do not grant exclusivity.

Each undrafted player and agent retains individual personality and priorities when evaluating offers. Perceived opportunity
to make the roster and guaranteed money are commonly prominent factors, without assigning every UDFA identical preferences.
Assess roster opportunity from actual positional competition, available roles, and team context rather than promising a roster
place or using hidden future outcomes. Agent feedback communicates the client's priorities through the existing negotiation flow.

In the UDFA market, special player-interest highlighting is reserved for membership in the user's custom shortlists. Use a
small, clearly identified shortlist star beside the name as the initial presentation choice, distinguishable from ability stars.
Do not automatically highlight players merely because they went undrafted or remain on the private draft board. Draft-board
membership alone does not create shortlist membership. Preserve the user's shortlist markers through the draft-to-UDFA transition.

Drafted rookies use a simplified NFL-style slot contract system rather than a full post-selection negotiation. Each pick has a transparent defined contract range derived from draft position and the current league/cap environment, allowing cap planning before and during the draft. The player normally signs through this supported rookie process; any rare failure/exception must be explicitly modeled and explained rather than trapping the user in an unexpected second negotiation flow.

#### Rookie minicamp and tryouts

Rookie minicamp is a simulated offseason event that produces early staff observations and readiness updates for its
participants. Add observations to the existing player evaluation records with their source and date, respecting staff
knowledge and confidence; early camp impressions do not reveal hidden true ratings or guarantee future performance.
Staff run the camp automatically; the GM chooses tryout invitees and reviews the results rather than directing individual
drills or sessions. After camp concludes, deliver one consolidated inbox report covering notable performances, readiness,
and any injuries, with links to affected players and updated evaluations. Use the actual simulated camp outcomes and medical
state; do not invent an injury or standout performance merely to populate the report. Persist report delivery so reloading
does not produce duplicate summaries or apply readiness changes twice.

Teams can invite unsigned players to rookie-minicamp tryouts without offering them a contract. Every team has an allowance
of 10 unsigned tryout invitees per rookie-minicamp cycle, separate from drafted rookies and already signed UDFAs. Detailed
slot-consumption and replacement rules remain to be defined. Show used and remaining
capacity before inviting players and persist invitation and attendance state. Distinguish invitees from signed roster members:
a tryout does not create an employment contract, promise a roster place, or automatically sign a player after camp. Any
subsequent signing follows the existing offer, confirmation, roster, and financial validation rules. Tryout eligibility,
invitation acceptance, and treatment of cancellations remain for the focused rules/design pass.

#### Offseason workouts, mandatory minicamp, and attendance

Offseason team workouts and mandatory minicamp are simulated calendar activities that affect preparation, scheme familiarity,
and conditioning through the existing training/readiness systems. Participation and actual workload determine the relevant
benefits; attendance alone does not guarantee skill improvement. Respect the applicable event's eligibility, medical limits,
and practice/contact restrictions.

Staff run offseason workouts and mandatory minicamp automatically. During these activity periods, provide weekly inbox
reports or updates covering participation, preparation progress, scheme familiarity, conditioning, and relevant health
developments. Consolidate routine activity into the weekly summary rather than sending a separate message for every session.
Mandatory activities explicitly flag attendance/compliance issues, holdouts, applicable fines, and other matters requiring
GM attention. Voluntary nonattendance can appear as factual participation context but is not presented as a mandatory-duty
violation. These updates use the existing inbox and advancement rules: routine reports do not force a stop, while an actual
required decision does. Preserve the separate agreed end-of-rookie-minicamp summary without duplicating its report content.

Players may miss voluntary workouts or hold out from mandatory activities. Voluntary absence is not itself a finable offense
or automatically a contract holdout; account separately for any valid attendance-based workout bonus and missed team-specific
preparation. A player may prepare independently, so voluntary nonattendance does not automatically mean poor conditioning.
Distinguish excused absence, medical restriction, voluntary nonattendance, and an unexcused contractual holdout in reports.

Unexcused absence from mandatory activities can incur the applicable rule-based fines for a player subject to those obligations.
Track the reason, amount, and financial consequence without applying signed-contract penalties to an unsigned tender by default.
Players generally wish to avoid financial loss, but fines are one pressure among several: financial security, expected gains
from a new deal, player/agent resolve, relationship, and leverage influence whether a holdout continues. Do not force every
holdout to end after a fixed fine threshold or assume most players always endure the fines. Reporting back does not necessarily
resolve the underlying contract dispute. Persist attendance, fines, and dispute state with clear agent and staff feedback.

Use the [NFLPA offseason rules](https://nflpa.com/active-players/off-season-rules) and applicable CBA provisions as references.
Exact fine schedules, mandatory versus discretionary treatment, forfeitures, and event-specific exceptions require a focused
rules pass before implementation; do not apply one generic fine to every missed offseason event.

#### Post-draft grades and recap

After the draft, each team receives a fictional media draft grade based on the perceived value of its selections relative
to pick position and how well the class addresses team needs. Use contemporaneous public prospect evaluations and observable
roster needs, not hidden true ability, potential, the GM's private board, or future career results. Explain the main value
and need judgments behind the grade. It is a media assessment that may prove wrong, not an authoritative measure of the
class's eventual success. Exact scoring weights and grade presentation remain to be defined.

The user's draft recap lists every selection with pick number, player, position, college, and a direct player-profile link,
alongside the team's completed draft-day trades and exchanged assets. Include the media grade and its rationale. Build the
recap from committed draft and transaction records, preserve it in season/team history, and retain the original assessment
rather than rewriting it when players later develop. The recap provides context without replacing the existing post-draft
Undrafted Free Agent market flow.

#### College football universe and draft pipeline

The college landscape is a living parallel football universe, not a static prospect generator. It is a fixed, fictional Division-I-style competition with named teams, conferences, player rosters, a season calendar, schedules, results, standings, rankings, postseason projections, awards, statistics, and news. The competition is generated with the franchise world, advances deterministically, and is fully persisted with the franchise; its results must be reproducible from the saved world and state. It is a scouting context, not a second user-controlled franchise mode: recruiting, NIL, college coaching contracts, and user-directed college game management are outside the 1.0 scope. Redshirts and a living transfer portal are simulated college systems that affect player eligibility, team rosters, development, playing opportunity, and draft context, but the pro GM cannot directly operate them.

The college universe is full FBS-sized rather than a condensed feeder league. It uses a fictional but real-scale top-level population of teams, conferences, players, games, and postseason context, generated and simulated efficiently enough to remain credible across long persistent careers. Its fictional identities avoid real-world licensing dependence while preserving the breadth, competitive variety, and scouting depth of a full major-college landscape.

Its postseason mirrors the real-world-style major-college playoff and bowl structure rather than using a simplified fictional tournament. Qualification, seeding, conference-champion treatment, bowl relationships, and postseason advancement are explicit versioned college-league rules, so each career can preserve the structure that governed its historical seasons.

College players retain a college team, class/eligibility state, position, ratings, potential, development history, season and career statistics, awards, and draft outlook. A player exists in exactly one pool. A prospect becomes draft-eligible through the college lifecycle, may declare or remain in school according to deterministic, explainable rules, and moves to the pro draft pool only when eligible. Draft selection transitions the same persistent player into a rookie pro roster state, preserving identity, DNA, college history, and accumulated scouting evidence; undrafted eligible players follow the existing undrafted-free-agent flow. Scouting estimates and public boards never reveal hidden ratings automatically.

College eligibility uses a simple four playable seasons within five college years model. A redshirt preserves a playable season, and complex waiver/exception eligibility rules are outside 1.0. The profile, college roster, and scouting surfaces show the player's class, college year, playable seasons used/remaining, redshirt state, and draft-eligibility context clearly.

Players become draft-eligible after three college years or when they exhaust college eligibility. A player with eligibility remaining may declare for the pro draft or return to school. The simulation resolves this through deterministic, explainable factors such as draft grade/outlook, recent performance, development opportunity, playing role, traits, and college/franchise context; it records the decision and its rationale as appropriate scouting/news information. Declaring moves the player to the eligible pro prospect pool, while returning preserves the player in the college universe for the next lifecycle.

College transfers are immediately eligible at their new school. The portal simulation evaluates transfer decisions and destinations using playing opportunity, team fit, coaching/college context, performance, development outlook, and player traits; it updates college rosters, rankings context, news, and future draft outlook without imposing a sit-out season.

College recruiting is a fully simulated, non-user-controlled annual cycle that creates incoming freshman classes and sustains the full FBS-sized universe. Program prestige, recent success, coaching/program context, roster need, and player fit influence outcomes, but bounded randomness prevents a deterministic rich-get-richer loop: weaker programs can land unexpected talent and stronger programs can miss targets. Recruiting outcomes feed college roster construction, player development opportunity, rankings/news, transfers, and eventual draft quality without becoming a separate playable recruiting mode.

College coaching changes, firings, retirements, and hires are simulated as a lightweight background carousel. They reshape program/coaching context, recruiting, player fit, and college performance but do not expose the pro GM to college contract management or a second deep staff-management layer. The implementation must remain computationally bounded so this immersion does not materially slow weekly or seasonal simulation.

College games are fast background simulations with schedules, scores, box scores, rankings, awards, and news; they do not receive a playable or 2D-viewable game presentation in 1.0. They use the same authoritative core football simulation engine as pro games, configured for the college competition's teams, rules, and context, so results remain mechanically coherent with pro scouting evaluation. Complete college season and career player statistics persist and are available through prospect, college-team, scouting, and historical views.

The college season advances in lockstep with the pro calendar, with weekly competition/reporting lifecycle points. Apply actual player skill changes at the corresponding activity and calendar events under the shared development rules; weekly reporting must not defer those changes or apply them a second time. Simulation produces team and player results, updates standings and rankings, refreshes bowl/playoff projections and award races, applies bounded player development, and emits explainable story hooks such as upsets, breakout performances, injuries, declarations, and draft-stock movement. College standings and rankings are their own systems; they never affect pro standings, schedule validity, or game resolution.

College information is available throughout the season from Scouting. The College Football hub provides Home, League Leaders, Bowl Projections, Full Rankings, Awards, and News. The Scouting Board remains the complete filterable list of currently draft-eligible prospects. The private Team Draft Board supports add/remove and manual ranking. Public Big Boards provide distinct fictional analyst and media-outlet consensus views that can disagree with each other and with private scouting. Before the draft, the Draft Room emphasizes draft order and preparation; once the draft starts, the Draft Stage emphasizes the current selection, countdown, recent-pick/news ticker, and immediate access to draft order, Team Draft Board, and scouting information. Presentation supports decisions and never blocks them.

Trades exchange valid players and picks only after rules validation. AI front offices evaluate needs, age, cap, contracts, picks, strategy, and risk, then propose actions with clear rationale. AI never silently changes a user-controlled roster.

The trade market is active across the entire league: AI front offices may initiate and complete valid trades with one another, and they may send the user GM offers with a clear rationale. The user must explicitly accept any change to the user-controlled roster; no AI proposal, trade demand, or deadline silently executes it. Unhappy players may demand a trade when their morale, role, contract, traits, and franchise context support it. A demand creates understandable consequences and negotiation pressure, but does not force an illegal or unapproved transaction.

1.0 trades support multi-asset packages of valid players and fixed current or future draft picks. Conditional picks are outside 1.0 scope until their triggers, ownership, and outcome can be fully modeled and explained. Trade validation presents all transferred assets, cap/roster implications, pick ownership, and phase/deadline restrictions before a user confirmation or AI-to-AI completion.

#### Trade approval and CPU evaluation

Incoming trade offers and agreed counteroffers require explicit final user confirmation before a user-team transaction
executes. Present every exchanged player and pick, relevant contract obligations, cash and cap effects, and roster impact
for review. Agreement during discussion does not silently complete the trade. Revalidate ownership, player availability,
financial/roster legality, and timing against current state at confirmation; explain invalidated terms rather than partially
executing a deal or silently substituting assets. Record a confirmed trade once through the authoritative transaction system.

CPU front offices evaluate trade value using their own accumulated scouting knowledge, confidence, roster needs, and strategy.
They do not access hidden true player ability or future development to price deals. Different evaluations and priorities can
produce believable mistakes or disagreements without deliberately making irrational offers to manufacture drama. Keep uncertain
football valuation distinct from objective legal facts: contracts, pick ownership, and cap/roster rules still use authoritative
state. Apply the same evaluation model to CPU-to-CPU and CPU-to-user negotiations without granting either special knowledge.

#### CPU franchise direction and review cadence

CPU teams maintain a franchise direction, such as rebuilding, contending, or preserving flexibility, that guides their
transaction decisions rather than evaluating each move without a broader plan. Direction reflects the front office's
scouting-based roster assessment, results, resources, and owner expectations. It can persist across seasons; a review does
not require a change or reset the plan simply because the calendar advances.

Review the appropriate direction at four explicit points: the start of preseason, Week 4, the trade deadline, and the
beginning of that team's offseason. The offseason review follows existing team-specific season-end eligibility. Evaluate
deadline direction in time to inform deadline-day transactions. Exact placement within Week 4 remains a calendar detail.
Persist the current direction, review date, and rationale, and retain it between these scheduled reviews. Transactions may
respond to current opportunities and constraints within the plan without rerolling franchise direction for every offer.
Changes must follow actual evidence and circumstances while respecting the owner's existing commitments and all league rules.
Other teams' internal direction and review rationale are not directly displayed to the user. The GM infers their approach
from public media interpretation, trade-block listings, transactions, and negotiation behavior. These signals can be incomplete
or mistaken; do not expose a definitive rebuilding/contending label or hidden strategy score through another screen.

#### Trade block and negotiation feedback

The GM can place up to eight valid owned players and draft picks into one Trade Block shopping package. The GM may optionally
communicate desired-return filters such as asset type, position, role, playstyle, age, contract preference, draft round, and
priority; leaving those filters open requests the best available market offers. Before the package is submitted, the screen
shows no teams, offers, proposed returns, or results placeholders. **Shop Selected Assets** submits the request; only after it
is processed does Trade Block transition to a distinct results state containing concrete packages from interested teams.
Listings express availability and preferences, not authorization to complete a
deal. The GM may update or remove listings, and listings must reflect current ownership and eligibility after transactions
resolve. Reviewing a returned package opens it in Build a Trade for adjustment or negotiation. There is no separate Active
Offers screen; unresolved negotiations remain reachable from their inbox items and a compact saved/live selector within the
Trades workspace. Other teams use the information when evaluating possible offers without guaranteeing
interest or bypassing the existing trade rules. There is no separate Trade Finder screen.

Trade discussions provide broad, useful explanations of objections and counteroffer priorities, such as needing a better
draft pick, seeking help at another position, or refusing to move a core player. Ground responses in the other front office's
actual needs, valuation, and strategy without exposing exact hidden acceptance scores. An objection does not guarantee that
one specific adjustment will secure acceptance. Trade-block layout and detailed negotiation controls remain for the UI pass.

Being placed on the trade block can upset a player depending on personality, relationship, role, and circumstances. Use the
existing morale, agent, and conversation systems to communicate reactions; listing does not cause an identical penalty for
every player or automatically trigger a public dispute.

There is no separate private player-shopping conversation with another team outside the trade-block/proposal workflows.
The existing ability to submit and negotiate valid trade proposals remains. The GM can instead initiate a conversation
with their own player about potentially trading them, with the player's response reflecting their circumstances and priorities.
Preserve the discussion in relationship context and use the existing promise rules for any explicit commitments. A conversation
does not itself place the player on the block, approve a trade, or create a blanket player veto over otherwise legal trades.
Players can express preferred trade destinations or priorities, such as contention, role, or location, during these
conversations. Preferences inform the GM's decision and potential relationship consequences but do not automatically confer
veto power; any actual contractual trade restriction is handled by the contract rules. Discussing a potential move beforehand
can soften its relationship impact when the conversation goes well, but does not guarantee approval or prevent disappointment.
Evaluate the response against the player's preferences, the conversation, any explicit promises, and the eventual outcome.
Exact dialogue options and effects remain for the conversation-content pass.

### Staff, preseason, and immersion

Staff influence scouting, development, injuries, and strategy. The 1.0 organization includes a Head Coach; offensive, defensive, and special-teams coordinators; position coaches; medical staff; a scouting team; and a strength-and-conditioning coach. Position coaches are organized around the position groups they teach. The scouting department includes the personnel/scouting leadership and the pro and college evaluators needed to build player knowledge. Medical staff own the relevant health, treatment, and recovery expertise; strength and conditioning owns the relevant training, fitness, and physical-preparation expertise. The Staff screen is organized by these departments and shows every supported role, its current holder or vacancy, tendency, role aptitude, profile, history, contract, and explainable effect.

The accepted Staff presentation direction is a full-width, compact organization table grouped by department. It keeps each
supported role or vacancy visible with the assigned person, relevant tendency, role aptitude, contract, salary, satisfaction,
and concise responsibility or vacancy consequence. The September 10, 2026 mockup establishes the overall hierarchy only;
exact spacing, columns, wording, role catalog, illustrative values, and production interactions remain open to refinement.

Staff changes are phase-gated, persist with tenure and retirement history, and use a separate staff market. Effects are bounded, explainable, and relevant to each role: scouting staff build evaluation knowledge and accuracy; coaches affect preparation, activity-based development, and observation of skill changes; medical and conditioning staff affect health, readiness, and recovery. Apply the detailed development, injury, and vacancy rules rather than the older implementation foundation's annual-only bonuses or fixed one-point/one-day caps. Exact effect strengths require simulation balancing. Training camp and preseason create position battles, training reports, roster cuts, and development opportunities. Morale, chemistry, fan pressure, and media add understandable, modest context without overpowering talent or rules.

#### Training-camp position battles and reports

Position battles emerge from closely matched players competing for the same role through actual roster composition, camp
reps, readiness, and performance. The GM does not need to explicitly designate a battle. Staff evaluate the competition using
their accumulated player knowledge, applicable scheme/role fit, and observed evidence rather than revealing hidden true ratings.

Include position-battle assessments and developments in the regular weekly camp report, alongside other camp observations;
do not create a separate recurring battle-report message. Explain the competitors, relevant evidence, current staff assessment,
and any resolved outcome without guaranteeing future performance. Persist the observations and resulting depth assignments
through the existing report and depth-chart systems.

When depth-chart control is delegated, coaching staff decide who wins the battle and apply legal depth-chart changes within
their delegated authority. Make those changes visible in the weekly report, with the existing GM review/override available.
When depth-chart control is retained by the GM, the report informs the decision but does not automatically change assignments;
the GM chooses the final depth chart. Respect the existing personnel-package and availability rules in either mode.

Preseason game performance contributes to position-battle evaluations alongside practice performance. Staff weigh the
available evidence in context, including role, opportunity, and competition, rather than declaring a winner from one raw
stat line. Weekly camp reports incorporate relevant preseason observations into the same ongoing battle assessment.

The GM can set preseason playing-time priorities, such as resting established starters or giving roster-bubble players
more evaluation snaps, or optionally delegate those priorities to coaches. Show the active control mode and priorities
before each preseason game. Coaches translate priorities into legal personnel usage within availability, medical restrictions,
and any configured snap limits; these priorities do not give the GM individual play-calling control. Report actual usage
and relevant evaluation results through the existing game and weekly camp reports. Exact priority controls remain for the UI pass.

#### Opening-week readiness and season outlook

Week 1 arrives through the existing calendar and user-initiated advancement flow. Send an inbox reminder to review the roster,
depth chart, and game plan, with links to the relevant screens. No separate opening-week ceremony or setup wizard is required.

At the applicable roster-compliance point, an illegal roster blocks further advancement. State the specific reason or reasons
it is illegal, show the relevant current count/status and requirement where applicable, and link directly to the Roster screen
to resolve the problem. Revalidate after changes rather than clearing the block merely because the message was read. Weak
but legal positional depth can produce an advisory warning the GM may disregard; it does not block advancement or authorize
staff to change the roster automatically.

Provide a concise preseason/Week 1 season-outlook inbox report combining media predictions, Head Coach notes on team strengths
and weaknesses, and the owner's agreed expectations. Keep these sources distinct: media predictions are outside opinions,
coach notes reflect staff knowledge and observed readiness, and owner expectations are the actual recorded commitments from
the owner meeting. Explain how the current roster supports or challenges those expectations without silently rewriting them.
Predictions and coach assessments do not reveal hidden true ratings or guarantee results. Preserve the dated outlook so it
can be revisited against the season's eventual outcome.

#### Weekly roster availability and game-day administration

Injury-related roster shortages generate contextual inbox messages identifying the affected position or role and linking to
the Depth Chart and Roster screens. Distinguish a legal but thin unit from a rules violation requiring correction. Messages
do not authorize signings, releases, or other automatic roster transactions.

Temporary practice-squad elevation and permanent active-roster signing are separate, clearly labeled actions. Before either
is confirmed, explain duration/status, roster and financial consequences, applicable eligibility or usage limits, and any
scheduled reversion for a temporary elevation. Resolve both through their appropriate validated rules and transaction history;
do not silently convert a temporary elevation into a permanent contract or vice versa. Exact limits follow the focused rules pass.

Game-day inactive selections are manual by default. The GM may explicitly delegate them to staff, with the active control
mode visible and selections available for GM review or override before the applicable deadline. Delegated selections obey
game-day eligibility, medical restrictions, and roster rules and do not confer authority to execute roster transactions.

#### Final roster cut-down

The final cut-down screen presents current roster counts and applicable limits, position depth, player contract and
release/dead-money impact, and practice-squad eligibility together so the GM can assess the consequences of each decision.
Use authoritative roster, contract, and eligibility rules, with links to player details and relevant evaluations. Update
counts, depth, and financial projections as proposed decisions change and after confirmed transactions resolve.
Use checkboxes beside players to build the proposed cut list. Checking or unchecking a player immediately updates projected
roster counts, position depth, cap impact, and dead money, with current values clearly distinguished from the projected result.
Selection alone makes no roster change. A Confirm Cuts button executes the reviewed batch after validation; if changed state
invalidates the proposed batch, explain the issue and return it for review rather than silently executing a partial selection.

Staff may provide optional cut recommendations grounded in their player knowledge, camp/preseason evidence, and roster needs.
Explain the rationale and distinguish recommendations from approved actions. Every release requires explicit user approval;
staff never execute cuts automatically, including when depth charts or preseason playing time are delegated. Validate each
release against current rules and record completed transactions once. Show when a released player must pass through waivers
and distinguish practice-squad eligibility from guaranteed availability to sign there. Deadline reminders and the final
compliance prompt route directly to this screen. Do not generate inbox messages or digests announcing newly waived players;
the waiver market remains available through its management screen. This does not suppress the outcome of the GM's own claims
or separately requested shortlist news.

#### Practice-squad formation

After cuts and applicable waiver processing, the GM fills the practice squad through the existing roster/free-agent screens.
Show eligibility, capacity, and practice-squad contract terms using the same authoritative rules as other roster transactions.
Eligible players consider pay and perceived opportunities when choosing among practice-squad offers, with individual player
and agent preferences rather than automatic acceptance of the first offer. Free Agency exposes one Make Offer action; the
negotiation screen selects the valid contract type, including Active Roster or Practice Squad where eligible. The current
Practice Squad is a Roster-screen variant with squad-specific contract, pay, elevation, and eligibility information.

A releasing team has no special priority or exclusive right to sign that player to its practice squad. Players subject to
waivers must clear the process before any team, including the prior club, can complete a practice-squad signing. Once eligible
and available, the player chooses among valid offers through the negotiation flow. Do not treat an intended practice-squad
return as guaranteed when presenting a cut. Reference the
[NFL personnel calendar](https://www.nfl.com/news/2026-27-national-football-league-important-dates) for the common opening
of practice-squad signing after waiver notifications; precise eligibility and contract rules remain part of the rules audit.

#### Waiver claim confirmation and conditional releases

The GM submits claims for the players they want without ranking their own claims in a preference list. Resolve competing
claims by the league's authoritative waiver order. When the user GM has the winning claim, deliver an Action Required email
from the League Office that provides a second opportunity to finalize or cancel the acquisition. The email shows the player,
inherited contract, roster/cap impact, and any attached conditional release in structured detail below a short natural league
notice. This pending confirmation does not yet transfer the player or execute a release; it is not presented as a modal over
the Waivers screen.

If the GM cancels, pass the opportunity to the next eligible claimant in the original waiver order. Persist claimant order
and each declined opportunity so cancellation or reload cannot restart the queue or give the same team repeated first chances.
Apply the same confirmation/cancellation opportunity to CPU claimants through their decision logic. This extra confirmation
and pass-to-next-claimant flow is a deliberate game-design exception to the NFL-style waiver baseline and must be documented
as such. A winning user claim creates a mandatory advancement stop. Time remains paused until the GM explicitly finalizes
or cancels; there is no confirmation countdown, automatic acceptance, or timeout cancellation. This required stop cannot be
disabled through optional alert preferences. Resume waiver processing from the persisted pending decision once resolved.

The GM may attach a conditional release of a current player to a waiver claim. Execute that release only if the claim is
won and the GM finalizes it. Losing or cancelling the claim leaves the designated player on the roster. Before finalization,
validate the acquisition and conditional release together against current contract, cap, roster, and eligibility rules,
then commit both as one consistent transaction. Show any invalidated plan for correction rather than executing one half.
Multiple pending claims do not reserve cap room, duplicate a release, or bypass revalidation after another acquisition.

#### Offseason staff review and hiring interviews

Staff have no fog of war. Their ratings, qualifications, traits, and personality tendencies are visible from the start,
including candidates in the staff market. Do not require interviews, scouting, employment, or repeated observation to unlock
staff information. Player scouting uncertainty remains separate and unchanged.

The annual owner meeting includes the staff review for a retained GM, using a Football-Manager-style board-meeting discussion
rather than a separate post-meeting review screen. Discuss whom to retain, dismiss, or offer an extension within that
conversation. Present supporting staff context by department with role, season assessment, scheme/philosophy fit where applicable, contract status,
and the financial consequences of available actions. Use the expectations and resources agreed with the owner as planning
context. The meeting proceeds linearly through one topic at a time; there are no agenda tabs or controls for jumping ahead,
and each topic must resolve through dialogue before the next begins. Apply the existing team-specific staffing window: non-playoff teams may act after the regular season, while playoff
teams wait until elimination or the championship concludes. A dismissed GM cannot make staffing decisions for their former team.

Staff hiring includes an interview before contract negotiation and appointment. Interviews discuss the candidate's relevant
scheme, working philosophy, and expected resources; adapt the topics to coaching, scouting, medical, or conditioning roles.
Interviews allow up to five questions from a role-specific list, with no annual staff-interview allowance. They explore
working preferences, requests, and conditions for accepting the job rather than uncovering hidden ratings or traits.
Follow the conversation with a summary of candidate fit, requests, and contract
demands. Make the candidate's responses useful for assessing fit with the roster, existing staff, and franchise direction. The interview
leads into a reviewable contract offer rather than automatically hiring the candidate. Preserve interview context and any
agreed commitments with the hiring record; distinguish requests from resources the franchise actually approved.

The user GM personally approves every staff hire; assistant hiring cannot be delegated to the Head Coach. The Head Coach
may provide candidate recommendations and input on fit, but cannot issue or accept offers or appoint staff on the GM's behalf.
Staff candidates can consider competing offers from other teams and may take several in-game days to decide. An interview
or pending offer does not reserve the candidate. Show pending decisions and notify the GM of acceptance, rejection, or a
candidate taking another job; validate availability before completing any appointment so a candidate cannot hold conflicting jobs.

Staff contracts have negotiable salary and length. Salary is a franchise operating expense outside the player salary cap.
Firing a staff member leaves the team owing their remaining contracted salary, even after a replacement is hired. Voluntary
resignation or retirement forfeits remaining unearned salary while preserving already earned pay. Persist contracts, payments,
and outstanding dismissal obligations in team finances and staff history without duplicate charges. Exact interview choices,
offer deadlines, decision timing, and payout schedules remain to be defined in this audit section.

#### Staff promotions, interview permission, and vacancies

Staff can be promoted internally to roles for which they are eligible, including advancement through the coaching hierarchy.
The GM explicitly approves the role change and any negotiated contract changes. Resolve the previous assignment and resulting
vacancy in the same transaction rather than leaving one person occupying incompatible roles.

During the offseason, other teams may request permission to interview contracted assistants for promotions, such as a
coordinator becoming Head Coach. The user GM can allow or deny each request. Approval permits an interview, not an automatic
departure; an accepted valid promotion offer resolves the staff member's move, contract transition, and resulting vacancy.
Denying a request can damage the relationship with a coach who wanted the promotion and affect satisfaction or willingness
to renew. Explain the coach's response in context; denial does not automatically cause resignation or secretly bypass the
permission decision. Apply the team-specific staffing windows to both teams so active playoff participants do not lose staff
before their season ends. Exact promotion eligibility, departure-contract accounting, and consequence strengths remain to be defined.

Head Coach is the only mandatory staff role. Other positions may remain vacant, but vacancies have functional consequences:
some reduce support or capacity, while others completely suspend the organizational activities that depend on that role.
The Staff screen explains the affected functions before a dismissal or role change and while a vacancy remains open; do not
silently supply a free replacement's expertise. Temporary Head Coach vacancies are permitted during hiring, but the team must
appoint a Head Coach before its next game can be simulated. The exact vacancy effects and fallback responsibilities for each
department remain to be defined before implementation so required league processes can still resolve consistently.

Without a coordinator, the Head Coach covers that unit's coordination duties with reduced effectiveness; the unit can still
play, but does not receive the missing coordinator's expertise. Define the penalty by actual coverage responsibilities and
avoid silently treating a vacancy as a fully staffed unit.

Without scouts, new scout-authored private reports stop. Coaches may still report changes they actually observe in their own players under the existing observation rules; this does not replace missing scouting assignments. Existing reports and public information remain accessible, but private
evaluations retain their last observation date and become stale as players change. They do not receive monthly refreshes
without scouting coverage. Publicly recorded statistics, transactions, and other public information may still update through
their normal league feeds; this does not automatically refresh private rating estimates or reveal traits. Show report age
and declining currency clearly. This vacancy rule applies to both ongoing routine pro coverage and targeted assignments.
College coverage follows explicit assignments, including ongoing assignments, as specified in the scouting section.

Without medical or strength-and-conditioning support, basic recovery continues, but the corresponding specialist treatment
and training benefits are unavailable. Missing coverage increases injury likelihood, the risk of more severe injuries, and
recovery time compared with properly staffed support. Apply effects according to the missing department's responsibilities,
with bounded, explainable penalties rather than a guaranteed injury or every injury becoming severe. Exact magnitudes and
how multiple vacancies combine remain balancing decisions; avoid counting the same missing support twice.

Each coach owns a preferred scheme, playbook, gameplan, and relevant tactical tendencies. By default the staff controls these football decisions. The GM may override a coach and choose schemes or playbooks, but the UI explains the fit and the coach may perform better when trusted to use their own preferred approach. Tactical choices never alter simulation fairness or bypass roster, availability, or rules validation.

### Player relationships and promises

Player conversations can include explicit GM promises about playing time, football role, contract extensions, or handling
a trade request. Show the precise commitment before the GM makes it. Distinguish a promise to pursue an action, such as
opening extension talks or seeking a trade, from a guaranteed result that also requires player agreement or a willing partner.
Promises never bypass contract, roster, medical, or transaction rules and do not automatically execute a move.

Player conversations reuse the established meeting presentation rather than requiring a bespoke screen for each topic.
Participants, office setting, current-context fields, dialogue, and directly clickable responses are populated from the
conversation event. The shared template may adapt its compact context strip to the subject, but its interaction and layout
remain consistent across playing-time, role, contract, trade-request, promise-revision, and dispute conversations.

Persist each promise with the player, agreed conditions, timeframe/review date, current status, and relevant evidence of
fulfillment. Make active commitments accessible from player relationship context and provide appropriate inbox reminders.
Evaluate outcomes against the terms actually agreed rather than changing expectations retroactively. Broken promises can
damage trust and the relationship, with the response reflecting personality and circumstances; keep consequences explainable
and consistent with the existing morale, negotiation, and trade-demand systems. Exact conversation choices, promise catalog,
and response strengths remain to be defined.

The GM can revisit a promise when circumstances change, such as injury, role competition, or another player's performance.
Present the original commitment, changed circumstances, and proposed revision in the conversation. The player may accept or
reject revised terms according to their priorities and relationship. Only an accepted revision changes the tracked agreement;
an attempted renegotiation does not silently erase the original promise or its history. Medical and league rules remain binding
even when a player rejects a revision, with any relationship response evaluated in that context.

Unhappy players can initiate conversations about concerns or unmet commitments, and the GM can approach players proactively.
Route player-initiated requests through the inbox and make proactive conversations available from player context. Use the
same relationship, promise, and conversation records for both paths so initiating a new conversation cannot reset an existing
dispute. Exact topics and timing remain part of the conversation-content pass.

Major player/management disputes can lower team morale beyond the directly involved player. Scale the effect to the dispute's
severity, visibility within the team, and relationship context; minor disagreements do not automatically trigger a roster-wide
penalty. Traits may help some players stabilize teammates and reduce the morale hit. Other traits may lead players to exploit
the disruption for personal advantage, such as pressing their own role or contract demands when they perceive leverage.
These are context-dependent behaviors, not automatic outcomes or authority to bypass negotiations and roster rules.

Use existing morale, trait, and relationship systems to record and explain the consequences through staff reports, player
conversations, and relevant news when publicly known. Apply the established bounded morale effects rather than letting a
dispute dictate game results. Exact trait mappings, influence strengths, and dispute-resolution/recovery timing remain to
be defined in the trait and conversation-content passes.

Most disputes begin privately between the player/agent and franchise. Some players or agents may take a dispute to the media
based on personality, relationship, leverage, and the course of negotiations. Track whether an issue is private or public;
news coverage must not automatically know undisclosed conversations or promises. A public statement may express one party's
perspective rather than reveal the entire underlying dispute.

When a dispute becomes public, the GM can respond publicly, address the player privately, or decline public comment. These
choices can affect player/agent relationships, team morale, and media interpretation according to the wording, history,
personalities, and circumstances. No response category is universally best or guarantees resolution. Record the response
and resulting developments through the existing conversation and news systems; declining comment is an intentional choice,
not an automatic admission or an unfulfilled response requirement.

### Media identities and developing stories

Fictional media outlets and reporters have persistent identities and distinct editorial styles, including analytical,
sensational, optimistic, and critical approaches. Style affects emphasis, wording, and interpretation of available evidence;
it does not give reporters access to hidden ratings, private talks, or future outcomes. Attribute coverage clearly so the
user can recognize whose assessment they are reading. Exact outlet/reporter catalogs remain to be authored.

Major stories can develop across multiple updates as actual events unfold, such as a dispute becoming public, a negotiation
changing direction, or a player returning after a setback. Link related coverage through a persistent story record and retain
the dated sequence of developments. New articles should add a development, response, or distinct substantive perspective
rather than repeat the same headline indefinitely. Allow stories to resolve or fade when no longer relevant; do not manufacture
transactions, injuries, or conflicts merely to continue coverage. Use the existing news and inbox delivery preferences.

The News screen supports filters for the user's team, league-wide coverage, college/draft coverage, and followed players.
Use the existing custom-shortlist membership for followed-player context rather than creating a conflicting follow list.
Articles open in a readable detail view with clear headline, outlet/reporter attribution, publication date, body text,
relevant player/team links, and access to earlier coverage of the same story. Visually separate related coverage from the
main article so it aids navigation without crowding the reading experience.

Article and feed presentation must fit the game's established visual style while maintaining readable typography, spacing,
and information hierarchy. Review layouts visually with representative short and long stories before finalizing them;
functional links alone do not satisfy the presentation requirement. Exact layout, artwork use, and responsive behavior
remain for the screen-by-screen UI pass.

Player and team history retain awards, transactions, milestones, and relevant career/season records, but do not permanently
archive past media articles. Earlier coverage links serve the current developing-story experience rather than an unlimited
historical article library. Exact news retention and cleanup timing remains to be defined; cleanup must not remove the
underlying game records or leave related-story links pointing to unavailable content.
There is no separate News Archive screen. Retained coverage remains searchable and filterable within League News, while
League Transactions remains the authoritative chronological register for completed transaction events.

Media occasionally revisits older draft classes, comparing the original recorded media grade and expectations with actual
career production and outcomes to date. Draw retrospective articles from preserved draft recaps, grades, transactions, and
player history without requiring retention of old articles. Distinguish what was believed at the time from what has since
happened, retain the original grade unchanged, and avoid treating unfinished careers as final verdicts. Exact frequency and
selection of classes remain part of media-content tuning.

### Established league history and career recognition

A new franchise begins in an already established fictional football universe. Generate populated league and team record
books, past seasons and honors, retired legends, and a Hall of Fame before the user takes the job. Starting record-book values
match the exact real NFL record numbers for the corresponding supported categories, attributed to original fictional
identities. Names need not resemble or parody the real record holders. Do not randomize record values around NFL benchmarks
between generated worlds; fictional identities and supporting careers may vary while the reference record values remain fixed.
Use a verified, versioned NFL record dataset with a stated cutoff date, category definitions, and regular-season/postseason
scope. For a new career, use record values through the preceding completed NFL season: a 2026 starting year uses records
through the 2025 season. Persist the adopted dataset and cutoff with the save; later real-world records or game dataset
updates do not overwrite its established history. Subsequent simulated achievements can tie or break those records normally.
The exact generated historical span and supported record catalog remain to be defined. Reference sources include the
[NFL Record & Fact Book resources](https://support.nfl.com/hc/en-us/articles/35869694725268-Stats-Records) and
[Pro Football Hall of Fame history](https://www.profootballhof.com/football-history/nfl-history-and-stats).

Every player present at world creation has a generated career appropriate to age and experience, including season-by-season
production, team/college history, draft or entry context, and earned honors where applicable. Veterans have substantive prior
careers; rookies have college history rather than invented pro seasons, and younger college players have appropriately shorter
histories. Active free agents and historical retired players are included. Reconcile season totals, career totals, team
tenures, records, awards, and eligibility dates across the generated world. Record holders and Hall of Fame members link to
actual historical player records supporting their exact record values. Constrain generated histories so season and career
totals reconcile with those values and no other generated performance silently exceeds a seeded record. Starting contracts, roster membership, age, and player
development context must agree with that history. Do not reveal hidden DNA or true ratings through retrospective screens.

Generation follows the existing world-source rules: standard rosters reproduce the same versioned fictional history, while
generated rosters use their own saved world seed. Persist the completed starting history, then append actual simulated
seasons without rerolling or rewriting the past. New in-save achievements can tie or break generated records through the
same record system. Detailed generation and validation methods remain implementation work, not a requirement to retain
historic media articles or replay every past game on screen.

1.0 includes a fictional Hall of Fame for retired players, with existing inductees at startup and future induction classes
as the league continues. Team-specific honors are limited to jersey retirements, with preexisting
recognition where supported by generated history. Persist the recipient, team where applicable, honor, number if applicable,
and date in player/team/league history. Retired numbers must be represented in jersey-assignment rules. Hall eligibility,
voting, and announcement follow the detailed section below. A team Ring of Honor or other separate team-honor system is outside scope.

A retired jersey number stays unavailable for assignment until explicitly unretired. For the user's team, the GM must
specifically confirm unretirement; roster changes, number requests, or automatic number assignment cannot silently restore
it to use. Persist both retirement and any later unretirement in team history, preserving the original recognition record.

The user GM chooses jersey retirements. After a player's retirement, the owner may suggest
them for recognition based on their franchise career and achievements. An owner suggestion does not automatically confer
an honor or require the GM to accept it. The GM may select eligible candidates independently of a suggestion, review the
proposed recognition and any jersey-number consequences, and explicitly confirm the honor. Record the resulting team and
player history once. This authority applies to jersey retirements, not league Hall of Fame selection.
Detailed candidate eligibility and presentation remain for the recognition/UI pass.

#### Record-book browsing and record chases

The record book supports league-wide and team-specific browsing, with single-game, single-season, and career categories.
Show record value, holder, relevant team, and season/date, with links to the associated player and available historical
context. Clearly distinguish tied holders and the scope of each record, including regular-season versus postseason where
supported. Seeded fictional records and achievements after save creation use the same display and comparison rules.

Approaching a major record can generate occasional media coverage based on actual accumulated statistics and the standing
record. Keep coverage proportional to the significance and proximity of the chase rather than generating repetitive news
for every minor threshold. When a record is tied or broken during a game, include a game-day mention driven by authoritative
statistics and preserve the achievement in the record book and relevant player/team history. Distinguish a tie from a new
record, and do not announce the same achievement repeatedly because of playback speed, skipped highlights, or save/load.
Exact record categories, chase thresholds, and visual treatment remain for the records and UI passes.

#### Hall of Fame eligibility and voting

Use real Pro Football Hall of Fame eligibility and selection rules as the versioned baseline for fictional inductions.
Players must have completed five full seasons without playing before eligibility. A return to playing before induction
requires eligibility to be recalculated from the player's last season played, not the original retirement announcement.
Simulated committee voting evaluates career production, awards, records, and football legacy using historical evidence;
eligibility is not automatic induction, and candidates can wait multiple years. Preserve annual candidacy and voting outcomes
without rerolling them on load or replacing past decisions with hindsight.

The current reference is the [September 4, 2026 reform for the Class of 2027](https://www.profootballhof.com/news/pro-football-hall-of-fame-announces-selection-process-improvements-ahead-of-class-of-2027-election):
one eligible player pool after the waiting period, a 28-person panel with 25 voting members, 13 player finalists, and no
automatic finalist carryover. The [NFL's reform report](https://www.nfl.com/news/pro-football-hall-of-fame-overhauls-selection-process)
confirms the 80% election threshold. These supersede older Modern-Era/Seniors separation for the chosen baseline. Remaining
ballot mechanics, candidate screening, and recognition of equivalent fictional honors require a focused rules pass, including
the category-specific details below; do not silently mix incompatible rules from different versions.
Coaches and staff are eligible for the Hall of Fame as well as players. Use the Coach category for coaching careers and the
Contributor category for qualifying non-coaching staff careers, with evaluations based on role-relevant achievements and
contributions rather than player-stat thresholds or tenure alone. The current real baseline provides one Coach and one
Contributor finalist through separate subcommittees. Coaches have a one-season-out waiting period
([Hall guidance](https://www.profootballhof.com/news/12-coaches-advance-in-selection-process-for-hall-of-fame-class-of-2025));
Contributors do not require retirement
([Hall guidance](https://www.profootballhof.com/news/21-contributors-advance-in-selection-process-for-hall-of-fame-s-class-of-2026)).
Preserve category-specific eligibility and career records; exact staff-role qualification and scoring remain to be defined.

Persist the adopted rule version with each save and historical class. After the league's first preseason game is simulated,
send the new class results to the GM's inbox. There is no Hall of Fame announcement cinematic or watch/skip decision.
This is a league-wide announcement even when the user's team is not playing in that game. Preserve unrevealed selections
until that game resolves and deliver the inbox results once, with persisted delivery state to prevent duplicates on save/load.
The adopted eligibility and voting baseline remains separate from this announcement timing. The Hall of Fame screen presents each inductee's portrait, career
statistics or role-appropriate accomplishments, honors, and a brief career summary drawn from preserved history, with access
to their player or staff record. Keep the
class/year context clear and retain the generated pre-save inductees alongside subsequent classes. Detailed visual layout
remains for the UI pass; a separate enshrinement ceremony has not been specified.

### User interface

Godot includes GM-profile management, character design, main menu/franchise setup with roster-source selection,
saves/settings, a dashboard, team screens (roster, depth chart, injuries, cap, staff, scouting, practice squad, picks),
league screens (standings, schedules, leaders, results, transactions, history), franchise screens (free agency, trades,
scouting, draft), and game screens (quick sim, drive summary, box score, game log). The UI always explains blocked
actions and handles missing data safely.

### Visual audit implementation contract

Recorded September 16, 2026. `Visual References/AUDIT-STATUS.md` identifies the working reference for each screen; the
blueprint remains authoritative for behavior and rules. Mockup values, people, dates, records, and Ironwood-specific content
are illustrative. Every screen binds to the active save and controlled team. A reference can determine hierarchy and interaction
without overriding a domain rule, knowledge boundary, or persistence requirement.

| Surface group | Entry point | Consequence / action boundary | Persistence requirement | Empty or blocked explanation |
| --- | --- | --- | --- | --- |
| Boot, title, careers, profiles, settings | Application launch and title menu | Create, select, duplicate, recover, or delete a career; edit reusable profiles and settings through confirmations | Career index, profile library, global options, per-career settings, backups, migration metadata | Explain no career, unavailable/corrupt save, migration, missing profile, invalid option, or failed recovery without discarding data |
| Dashboard, inbox, navigation, time advance | Loaded career home and persistent shell | Route to every major workstation; open messages; advance only to the next meaningful stop | Dashboard layout, unread/delivery state, current date/phase, configured optional stops, pending mandatory decisions | Name the event or rule preventing advancement and link directly to the required action |
| Roster, practice squad, injuries, availability | Team rail and contextual person actions | Inspect, filter, compare, move, release, claim, elevate, or route to a confirmation; no list action bypasses validation | Ownership, roster designation, availability, injury history, cap impact, transaction record, table preferences | Explain phase locks, capacity, cap, ownership, injury, waiver, or eligibility failure beside the disabled action |
| Player profile, development, contracts, conversations | Player link from any supported table, news item, report, or right-click menu | Read authoritative facts and scoped estimates; initiate only legal negotiations, role talks, mentoring, or roster actions | Career stats, development history, contract, morale, relationships, promises, scouting knowledge, conversation outcomes | Distinguish unknown, not scouted, not eligible, unavailable participant, and phase-locked action from missing data |
| Depth chart and live substitutions | Team workspace and game-day depth control | Edit ordered roles and starters; game-day changes affect subsequent snaps, never rewrite ratings or create eligibility | Saved depth order, user locks, emergency order, availability substitutions, in-game lineup state | Identify unavailable players, invalid positions, minimum-role requirements, or changes that cannot apply to the current snap |
| League standings, schedule, statistics, news, awards, history | League rail, linked entities, inbox, and season events | Read, sort, filter, navigate, and open source records; presentation never owns simulation state | Games, standings, current and archived stats, news delivery, awards, records, champions, Hall classes | Show preseason/no-games, no qualifying leaders, event not reached, or unavailable archive with the governing timeframe |
| Team finances, cap, facilities, accounting | Finances rail, Team Info links, owner discussions, contextual contract actions | Review obligations and propose only validated contracts, restructures, releases, or owner-funded projects | Contracts, cap charges, dead money, cash/owner state, budgets, project approval/progress, monthly summaries | Explain insufficient cap/cash, owner denial, phase restriction, ineligible contract, missing consent response, or unavailable project |
| Scouting and college football | Scouting rail, prospect links, assignments, event inbox stops | Allocate coverage, set filterable instructions, book finite evaluations, interview, and inspect public or private evidence | Assignments, capacity usage, confidence, reports, known facts, estimates, interview answers, college state and archives | Separate public facts from unavailable private observations; explain no staff, no capacity, missed window, or ineligible prospect |
| Draft, UDFA, rookie minicamp | Draft hub, war room, on-clock event, post-draft market | Start the draft, make or trade legal picks, submit selections, invite UDFAs, and move invitees through the normal contract flow | Order and pick ownership, board, selections, evaluation snapshot, rookie contracts, invitations and remaining slots | Explain not on clock, invalid/used pick, unavailable prospect, roster/cap failure, closed market, or full invitation limit |
| Trades, free agency, waivers, transactions | Trade Center, contextual player actions, inbox offers, market phase | Build up to eight assets, submit a shopping package, receive offers afterward, negotiate, accept, reject, claim, or withdraw | Offers and direction, asset ownership, submitted packages, contract state, claims, transaction history, deadlines | Explain phase, cap, roster, ownership, valuation, deadline, or claimed/unavailable asset; a failed proposal mutates nothing |
| Staff, owner meetings, interviews | Staff screen, annual meeting stop, vacancy links, permission email | Move topic by topic through meetings; interview and hire only within authority; record commitments and owner decisions | Staff contracts/roles, vacancies, permissions, requests, commitments, owner expectations, job security | Explain missing authority, denied permission, unavailable candidate, budget, vacancy consequence, or unresolved mandatory topic |
| Game day and postgame | Scheduled game stop, quick-sim choice, live observer, final result | Observe or simulate, adjust legal depth/personnel, resume, and inspect final statistics; UI requests actions from GameCore | Clock, score, possession, play log, lineup changes, injuries, box score, result, fatigue, milestones | Explain paused state, unavailable adjustment, finished game, missing historical play detail, or save failure without inventing events |

Shared UI rules for every surface:

- Domain services own truth and validation. UI models format state, expose action availability, and route typed requests; they do
  not duplicate cap, phase, eligibility, simulation, scouting, or persistence logic.
- Every consequential action has preview/review when needed, an explicit commit point, a success/failure result, and an immediate
  refresh from authoritative state. Disabled controls expose the same validator reason used by the committing service.
- Links to teams, people, games, reports, transactions, and historical subjects use stable IDs. Display names are not identity keys.
- Tables share sorting, filtering, column configuration, selection, keyboard focus, contextual actions, empty-state language, and
  persisted view preferences. Right-click remains a shortcut; visible controls provide the same discoverability.
- Current statistics require no redundant currency label. Historical scope, scouting estimates, projections, and uncertainty are
  labeled where they actually change interpretation.
- Team identity is injected from saved team data: name, abbreviation, palette, and approved logo. The UI never embeds Ironwood or
  another reference identity. Logos use nearest-neighbor scaling and must remain legible at header, row, scorebug, and helmet sizes.
- Loading, saving, no-data, partial-data, blocked, confirmation, recoverable error, and fatal error are distinct states. A blank panel
  is never the only explanation.

#### Approved first vertical slice

The first visual implementation slice is deliberately narrow and uses already functioning GameCore state:

1. **Dashboard:** apply the accepted compact shell, controlled-team branding, standings/news/leaders data, inbox count, and safe
   time-advance stop. Preserve configurable dashboard layout only where its persistence is already reliable.
2. **Roster and Player Profile:** provide the dense sortable roster, shared player selection, contextual actions, and a profile route
   that separates authoritative ratings/stats/contracts from scouting estimates and development history.
3. **Depth Chart:** keep the table-first accepted reference, ordered roles, availability, seasonal context, and starter controls.
   Do not restore the discarded oversized field visual as the primary workspace.
4. **Game Day:** enter from the scheduled-game stop, render the accepted live observer, provide the depth-chart substitution route,
   and preserve pause/resume, play-by-play, score, clock, statistics, injuries, and safe save behavior.
5. **Postgame:** consume the immutable completed result, show the accepted summary/box-score navigation, apply statistics, injuries,
   fatigue and milestones exactly once, then return to the refreshed dashboard.

The slice is complete only when one saved career can traverse all five stages, close and reload safely between stages, use the
controlled team's installed logo at every existing mark location, and receive the same validator explanations before and after
reload. It requires focused unit tests for action availability and state transitions plus one Godot smoke test of the full route.

Deferred from this slice: provisional-screen refinement, global game-name/logo replacement, final copy editing, production portrait
generation, full modular player/coach scene models, sprite animation production, and visual polish for workflows outside the route.
Those tasks remain required by 1.0 where specified, but none should expand the first slice.

## Rules findings and proposed content package

Prepared September 9, 2026. This section is part of the existing blueprint, not a second planning authority. Verified
references below inform the already approved NFL-style direction. All catalog entries and explicitly labeled defaults are
proposals for review, not newly accepted features or implemented behavior. Existing approved decisions take precedence.

### Rules review findings

| Area | Finding and application | Status |
| --- | --- | --- |
| Calendar | The 2026 trade deadline is November 10 at 4 p.m. New York time. Current standings begin determining waiver priority after the third regular-season weekend. Postseason transactions have specific exceptions; a blanket postseason transaction ban is inadequate. | Verified reference; generate equivalent dated windows per saved league year. |
| Rookie contracts | Drafted rookie terms are four years; undrafted rookie terms are three. Renegotiation opens after year three for drafted rookies and year two for undrafted rookies. | Verified reference. |
| Fifth-year option | Exercising guarantees the option and any otherwise unguaranteed fourth-year salary. Salary tiers depend on participation and original-ballot Pro Bowl selections. | Verified reference; fictional All-Star mapping proposed below. |
| Restructure | Salary/roster-bonus conversion to signing bonus moves cap charges forward. Signing-bonus proration is capped at five seasons. | Verified reference; propose conversion within existing contract years, with consent unless an existing clause authorizes it. |
| Tags | Non-exclusive franchise tags permit matching an offer sheet within five days or receiving two first-round picks. Transition tags permit matching without draft compensation. Exclusive tags prevent outside negotiations. | Verified reference; designation is distinct from the player signing a tender. |
| Retired contracts | The standard Player Contract paragraph 16 tolls the contract during retirement unless otherwise specified. Remaining service resumes on return; calendar years away do not simply exhaust it. Article 4 permits certain bonus forfeitures. | Verified default; reinstatement and accounting details still require case-specific rules. |
| Free-agent status | CBA Articles 8–9 distinguish qualifying exclusive-rights tenders below three accrued seasons, restricted free agency at three, and unrestricted free agency at four or more, subject to applicable rights. | Verified framework; do not substitute player age or years since draft for accrued service. |
| Practice squad | The NFL contract explainer contains both two-game and three-game elevation language. | Source conflict: do not implement the contradictory clauses together; current limits and postseason exceptions need amendment-level verification. |
| Coach interviews | The NFL has removed the ability to block bona fide coordinator-promotion interviews. The game's approved allow/deny system is a deliberate exception. | Preserve user decision and label the exception. |
| Hall of Fame | The September 2026 reform confirms the single player pool, 25 voters plus three nonvoting participants, and Coach/Contributor categories. Contributor evaluation may include a mixed playing/coaching/other-contributions career. | Matches chosen Class of 2027 baseline; do not import superseded finalist-carryover rules. |

Sources checked: [NFL calendar](https://operations.nfl.com/calendar-events/nfl-important-dates),
[NFL contract guidance](https://operations.nfl.com/calendar-events/nfl-free-agency/contract-language),
[NFL tag guidance](https://operations.nfl.com/calendar-events/nfl-free-agency/franchise-tags),
[2020 NFL–NFLPA CBA](https://nflpaweb.blob.core.windows.net/website/PDFs/CBA/March-15-2020-NFL-NFLPA-Collective-Bargaining-Agreement-Final-Executed-Copy.pdf),
[NFL coordinator interview policy](https://www.nfl.com/news/nfl-announces-new-policies-designed-to-increase-diversity), and
[Hall's September 2026 reform](https://www.profootballhof.com/news/pro-football-hall-of-fame-announces-selection-process-improvements-ahead-of-class-of-2027-election).
These are reference findings, not a claim that all NFL transaction rules have been fully specified.

Explicitly preserve the game's accepted exceptions: five-day exclusive offer confirmation; winning waiver claim confirmation
with time paused and cancellation passing the opportunity onward; offseason-only team staffing departures; promotion interview
permission; no All-Star game; chosen honors announcement dates; compressed pro days; equal Combine allowances and mandatory
participation for attending selected prospects. NFL research does not silently undo those choices.

Remaining focused verification before transaction implementation:

- Reserve/Retired tolling exceptions, reinstatement windows, exact bonus recovery, and release/waiver treatment for each return path.
- Current practice-squad eligibility, elevation/reversion and postseason exceptions; IR/PUP/NFI return rules and exemptions.
- Complete tag/tender salary formulas, repeated designations, offer-sheet asset requirements, RFA/ERFA rights, and deadlines.
- June 1 accounting, guarantees, consent/contract clauses, and roster/cap checks at each transaction stage.
- Coach-promotion contract settlement and precise category eligibility; retain the game's chosen departure windows.
- Remaining Hall screening, final ballots, tie handling, class limits, and staff qualification under the adopted reform.

Do not infer these details solely from old code or a dated explainer. The current C# phase foundation restricts transactions
more broadly than the target design; implementation progress remains in ROADMAP.md. This research pass changes no runtime code.

### Proposed defaults for the few remaining workflow choices

| Choice | Proposed default for review |
| --- | --- |
| All-Star option-salary mapping | Use the game's original announced All-Star selections wherever the adopted option formula uses original-ballot Pro Bowl honors. All-Pro awards do not substitute. |
| Rookie tryout invitations | Confirmed invitations reserve one of ten slots. An invitee who becomes unavailable before camp releases that reservation. Attendance consumes the slot for the cycle; no automatic signing afterward. This does not change mandatory Combine participation. |
| Combine booking deadline | Confirm bookings before the Combine evaluation/interview window closes, with advance reminder and final prompt. Keep the accepted later delivery of combined evaluation results. |
| Pro-day daily capacity | Tune X against the number of simultaneous colleges; a normal day should force a choice between broad shallow coverage and fewer deeper evaluations. Do not choose X before the event schedule exists. |
| Missing scouting staff | No private scout observations, including pro-day gains, without scouting coverage. Proposed daily pro-day allowance is zero when the entire scouting department is vacant; department size otherwise informs X. |

### Proposed training activity catalog

Activity names and coverage below are draft content. They describe skill families, not a finalized attribute schema or extra
visible ratings. Map them to the authoritative rating catalog during implementation. Staff schedules select appropriate work
by position, phase, health, and workload. General work distributes opportunity broadly; targeted work concentrates it.
Football activity XP, hidden DNA, observation delays, and reduced practice-squad training XP remain as already approved.

| Activity | Primary focus | Workload / readiness context |
| --- | --- | --- |
| General position practice | Balanced relevant position skills | Normal field workload; broad rather than concentrated gains |
| Specific technique drill | Each named drill has predefined supported skill focuses | Select the drill itself; no attribute-first drill assignment |
| Strength session | Strength and contact preparation | Physical fatigue; recovery time competes with field work |
| Speed and acceleration | Running mechanics and explosiveness | High physical demand; medical restrictions matter |
| Agility and footwork | Change of direction, balance, positional footwork | Moderate physical demand |
| Conditioning | Endurance and activity readiness | Fitness emphasis; avoid treating temporary conditioning as permanent skill |
| Recovery and mobility | Readiness and managed recovery | Low load; no large skill-XP reward for resting |
| Film and opponent study | Recognition, decisions, opponent preparation | Low physical load; mental learning and preparation |
| Playbook walkthrough | Assignments, communication, scheme familiarity | Low contact; distinguish familiarity from lasting ability |
| QB mechanics and placement | Throwing mechanics and accuracy | Arm workload; target supported throw types |
| QB pocket and reads | Pocket movement, progression reads, decisions | Controlled pressure; avoid awarding passing accuracy for film alone |
| Ball carrying | Vision, ball protection, running technique | RB/eligible ball-carrier work; contact load varies |
| Routes and releases | Route technique and separation | WR/TE roles; concentration on chosen techniques |
| Catching and contested catches | Hands, tracking, reception technique | Catch-point work may add contact exposure |
| Pass protection | Footwork, leverage, protection recognition | OL and relevant RB/TE duties |
| Run blocking | Leverage, angles, sustained blocks | Relevant blockers; higher contact exposure |
| Pass rush and block shedding | Rush technique and escaping blocks | Defensive front roles; contact exposure |
| Pursuit and tackling | Angles, tackling technique, run fits | Defensive roles; contact level must follow phase rules |
| Coverage and ball skills | Man/zone responsibilities, reaction, playing the ball | DB/LB roles as applicable |
| Kicking and punting | Accuracy, power, placement, operation timing | Specialist repetition and leg workload |
| Special-teams execution | Snapping, holding, returns, coverage, lane discipline | Supported unit duties; no custom special-teams play design |
| Situational team practice | Red zone, short yardage, two minute, third down | Shared execution/preparation; distribute XP only to actual participation |

Approved controls: select a specific drill with predefined skill focuses, and choose light, standard, or heavy intensity
where permitted. The table above describes draft activity families; the selectable catalog must expand them into named
drills with fixed skill mappings rather than an attribute picker. The exact drill list remains proposed content.

Proposed workload accounting: a heavier session consumes more
of the same limited day; it does not create free extra repetitions. Offseason/minicamp versions retain their approved low
skill-XP and conditioning emphasis. A permanent DNA downturn can still outweigh all earned development.

### Proposed prospect interview question pool

Twenty-five selectable questions; at most five per prospect per draft cycle, shared between manual and delegated work.
Evidence columns are authoring guidance, not a user-visible answer key. Approved presentation shows each question and a broad
topic label. Answers vary by actual personality, experiences, football understanding, and values. Every answered question
provides useful player information, whether about work ethic, traits, mental skills, or values. A trait-focused question
about a trait the player lacks explicitly reveals that specific trait's absence. Preserve this negative finding in the
interview/scouting report, distinct from unknown traits, for both manual and delegated interviews. A finding is not necessarily
a newly confirmed named trait: preserve the distinction between evaluated skills, staff interpretation, disclosed values,
and confirmed traits. Mental-skill findings remain scouting estimates rather than a reveal of exact true ratings. Manual
and delegated interviews follow the same guaranteed-information rule. Exact question wording remains draft content.

| # | Question | Possible evidence |
| --- | --- | --- |
| 1 | Walk me through how you prepare for a normal game week. | Work Ethic, Film Junkie, preparation habits |
| 2 | What do you work on when nobody is supervising? | Independent effort, Workout Avoider clues |
| 3 | Tell me about a weakness you have worked to improve. | Self-assessment, learning response |
| 4 | How do you react when a coach corrects you in front of teammates? | Teachability, Short Temper, composure |
| 5 | What do you do after making a costly mistake? | Resilient, Pressure-Prone, recovery habits |
| 6 | Describe a time you lost your starting job. | Role Sensitive, Team First, competitive response |
| 7 | How would you approach spending your rookie year as a backup? | Role expectations, patience |
| 8 | What would you want from a veteran mentor? | Mentoring willingness and compatibility |
| 9 | Have you helped a teammate learn something difficult? | Leadership evidence; cannot grant veteran-only Mentor |
| 10 | How do you handle disagreement inside the locker room? | Leader, Volatile, conflict response |
| 11 | What do you do when your personal goals conflict with the team's plan? | Team First, Self-Interested |
| 12 | Tell me about preparing for your biggest game. | High-pressure habits; not proof of future clutch performance |
| 13 | How do you stay engaged when the season is going badly? | Competitive, Resilient, effort consistency |
| 14 | What changes in your preparation after a great performance? | Consistency, discipline, complacency clues |
| 15 | How do you learn a new playbook? | Fast/Slow Learner clues, learning method |
| 16 | Talk me through your responsibility on this position-specific play. | Football understanding and assignment recognition |
| 17 | What do you do when the opponent shows something you did not expect? | Adaptable, recognition and decision approach |
| 18 | When is it appropriate to improvise outside your assignment? | Freelancer, Disciplined, risk preference |
| 19 | What do you look for first when studying an opponent? | Film habits, position-relevant recognition |
| 20 | Describe a scheme change you had to adjust to. | Scheme Savant, System Dependent, adaptability clues |
| 21 | How do you balance extra training with recovery? | Overtrainer, conditioning and preparation habits |
| 22 | How would you handle being told to limit your workload? | Communication, Plays Through Pain clues; medical rules still bind |
| 23 | How do you respond to criticism from fans or reporters? | Media Magnet, volatility, composure |
| 24 | What matters most to you about your first professional opportunity? | Role, development, compensation, team priorities |
| 25 | What would make you want to stay with one franchise long term? | Loyal, expectations and relationship priorities |

Delegated question variation samples this same pool and uses the same evidence model. It cannot discover a trait the player
does not possess. Preserve staff interpretation separately from the actual response and any confirmed finding.

### Proposed staff interview topics

Approved structure: up to five questions from a role-specific list, with no annual staff-interview allowance. Staff ratings
and traits are fully visible; interviews discuss fit, requests, and working conditions, not hidden information discovery.
The topic wording below remains draft content. These interviews do not consume Combine allowances.

| Topic / prompt | Useful assessment |
| --- | --- |
| What would you change first with this roster or department? | Priorities grounded in available knowledge |
| How would your preferred system fit our personnel? | Scheme flexibility and likely disruption |
| How do you decide a close position battle? | Evaluation approach and opportunity preferences |
| How do you develop a talented player whose progress has stalled? | Teaching, patience, workload philosophy |
| How do you manage a veteran who disagrees with their role? | Communication and conflict approach |
| How do you balance winning now with developing young players? | Compatibility with agreed team goals |
| How would you operate with a vacancy or limited budget? | Coverage plan and resource expectations |
| How do you handle medical restrictions and player readiness? | Role boundaries and risk approach |
| What would you prioritize when scouting information is incomplete? | Uncertainty, assignment planning, pro/college focus |
| What support and authority would you need from me? | Requests, delegation expectations, possible commitments |

Candidates discuss only duties relevant to their role. An interview informs hiring; no response reserves the candidate or
promises funding. Persist material requests and agreed commitments through the existing contracts/owner systems.

### Owner and agent personality catalogs

These are descriptive tendencies with bounded effects, not additional player traits or exact user-visible scores. Opposing
ends describe one dimension rather than contradictory independent bonuses. Avoid forcing every person into a single archetype.
The four owner dimensions below are approved and fully visible from the start. Their exact descriptive labels remain draft
wording. The four agent dimensions below are approved and fully visible from the start. Agent traits require no discovery
or fog-of-war progression; this does not reveal private competing offers or hidden negotiation acceptance thresholds.

| Owner dimension | Possible descriptions | Effect |
| --- | --- | --- |
| Patience | Patient / demanding quick results | Time allowed to meet agreed goals |
| Spending | Willing investor / cost conscious | Funding willingness within actual means |
| Ambition | Championship driven / steady progress | Goals and appetite for competitive risk |
| Involvement | Hands off / closely involved | Frequency and specificity of requests |

| Agent dimension | Possible descriptions | Negotiation effect |
| --- | --- | --- |
| Patience | Quick agreement / willing to wait | Response timing and market testing |
| Aggression | Accommodating / hard pushing | How strongly the agent pushes for better terms |
| Forgiveness | Forgiving / holds a grudge | Recovery after withdrawals or poor treatment |
| Media Approach | Discreet / outspoken | Whether the agent keeps negotiations private or uses media attention to apply pressure |

Player preferences remain distinct from the agent's style. An agent cannot give every client identical priorities; rookie
roster opportunity, veteran role, personal loyalty, and career circumstances still matter. No personality bypasses rules.

### Proposed mentoring and dispute trait mapping

Use existing traits rather than expanding the 53-trait catalog. Its listed groups contain 16 + 11 + 16 + 10 entries.
The three-trait cap and incompatibility map still apply; evidence of a habit is not automatically acquisition of a trait.

| Existing traits / context | Proposed use |
| --- | --- |
| Mentor | Veteran-only multiplier to effective mentoring, not a requirement for participation |
| Work Ethic, Film Junkie, Disciplined | Repeated shared preparation can influence related habits and learning |
| Workout Avoider, Takes Plays Off, Freelancer | May transmit poor preparation or risky habits where actually modeled and observed |
| Fast Learner / Slow Learner | Modest variation in uptake; neither guarantees acceptance or immunity |
| Leader, Team First, Resilient | Can limit the spread or duration of morale disruption |
| Self-Interested, Demanding, Volatile | May pursue opportunity or amplify conflict when personal context supports it |

Do not transfer physical or situational traits merely through conversation: a mentor cannot teach Durable, Quick Healer,
Cold-Weather Specialist, or veteran status. Young players may pick up habits or relevant XP without gaining a named trait.

### Proposed owner project catalog

All approval and funding remain owner-controlled, with time, costs, upkeep, and disruption already required by the blueprint.
Effects describe plausible game functions; exact benefits and build durations require balancing, not promised medical outcomes.

The main categories are Stadium, Facilities, and Operations, each with its own sub-upgrades. Facilities contains player-related
facilities; Operations is a separate category for football-operations resources. The project suggestions below are not finalized
sub-upgrade lists. Candidate Operations upgrades include scouting tools, video/analysis technology, and staff workspaces;
their exact scope and benefits remain proposals, including distinguishing staff analysis tools from player film/meeting rooms.
Operations sub-upgrades are explicitly deferred for a later workshop. The examples above are unapproved ideas, not a working
list accepted by the user; do not require those details to continue the current audit.

Facilities working sub-upgrade list, provisionally accepted for now:

- Practice Fields
- Indoor Practice Facility
- Weight Room and Conditioning
- Medical and Rehabilitation
- Film and Meeting Rooms
- Locker Room and Player Lounge
- Nutrition and Cafeteria

Use this grouping in preference to the earlier individual project suggestions below. It remains open to later revision;
exact benefits, levels, costs, and completion times are not yet approved.

| Project | Intended benefit | Cost/disruption considerations |
| --- | --- | --- |
| Weight-room refurbishment | Supports strength preparation and appropriate training capacity | Equipment cost, installation, upkeep |
| Conditioning equipment | Supports conditioning and workload preparation | Equipment replacement and operating cost |
| Practice-field renovation | Restores practice quality and relevant preparation benefits | Temporary field access constraints |
| Indoor practice facility | More dependable training environment | Major construction and continuing upkeep |
| Film and meeting rooms | Supports teaching and opponent preparation | Installation; no direct reveal of hidden ratings |
| Medical assessment facilities | Supports diagnostic capacity and assessment quality | Qualified staff still required |
| Rehabilitation facilities | Supports medically appropriate recovery work | Construction/equipment costs; no guaranteed recovery date |
| Scouting analysis resources | Supports processing observations and staff efficiency | Does not grant free college scouting or more Combine slots |

Stadium brainstorming replaces the earlier broad stadium suggestions. None of the categories below is finalized;
the list captures ideas for review, with precise effects, upgrade choices, costs, and timing still undecided.

| Stadium category | Scope |
| --- | --- |
| Renovation/Rebuild | Major stadium renovation or rebuild, under the existing same-city rule |
| Seating | Stadium seating improvements and capacity |
| Screens and Speakers | Stadium displays and sound system |
| Field Quality | Stadium playing-field quality |
| Facilities Quality | Bathrooms and similar stadium amenities |
| Parking | Stadium parking capacity and quality |
| VIP Seats/Boxes | Premium seating and private boxes |
| Concessions | Stadium food, drink, and concession facilities |

### Proposed conversations and team honors

| Conversation | Permitted commitment or response |
| --- | --- |
| Player role concern | Discuss intended role or opportunities within GM/delegated authority; no guaranteed statistics |
| Contract concern | Discuss pursuing an extension or restructure; signing still requires confirmed legal terms |
| Potential trade | Explain intent, hear preferences, record any explicit promise; no automatic veto |
| Mentoring request | Ask for one player or a small group; accept refusal and allow later reassignment |
| Public dispute | Private conversation, public response, or no comment; context determines consequences |
| Owner rebuild negotiation | Request time in exchange for financial discipline and/or agreed draft-development goals |
| Owner contention negotiation | Request resources; owner may approve, condition, or deny them |
| Staff resource request | Discuss support and authority; owner-controlled funding stays uncommitted until approved |

Approved team honors are limited to jersey retirements; there is no Ring of Honor. Preserve the recipient, franchise, date,
and career context. Jersey retirement changes number availability; explicit unretirement preserves its historical recognition.
Use a confirmation and history entry rather than adding another ceremony workflow. The owner may suggest retired players;
the user GM decides. Existing league Hall selection remains separate.

### Review and handoff status

The content package is ready for grouped review, beginning with training and prospect interviews. It is not a finished
dialogue script library, tuned simulation specification, verified record dataset, or implemented UI. After content review,
continue to screen layouts and settings. Final handoff must still resolve the focused rules checklist above and map the
accepted behaviors to implementation stages in ROADMAP.md. No additional feature interview is needed merely to tune numbers.

## 1.0 completion standard

1.0 is complete when a fresh user can start a franchise, manage a legal roster and cap, follow a persistent college season and prospect pipeline, simulate a season, complete playoffs, progress players, complete an offseason with draft and free agency, start the next season, and browse persistent history—all without manual file editing or developer intervention.
