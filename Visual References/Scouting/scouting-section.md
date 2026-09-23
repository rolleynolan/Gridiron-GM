# Scouting Section Concept

Ironwood Thunder is the fictional reference team shown in the mockups, not a fixed or privileged franchise. All team-specific branding, staff, highlights, owned picks, status panels, and draft interactions use whichever of the 32 teams the player currently controls.

## Navigation

`Scouting` is an expandable primary-navigation group focused on the prospect-to-draft pipeline:

`Scouting Board | Draft Board | College Football | Draft`

Prospect Search is not a separate Scouting destination. League-wide player discovery belongs in League > Player Search; Scouting is reserved for evaluating prospects, maintaining draft priorities, following college football, and making selections.

## Scouting Board

Scouting Board is the complete working list of every prospect eligible for the current draft cycle.

- A large scrollable table lists all eligible prospects.
- Sort and filter controls help the GM narrow the class by the information most useful to their draft process.
- FM-style configurable headers expose useful draft/scouting information and allow the user to choose, reorder, and resize the columns they value.
- The default comparison view prioritizes overall and position rank, prospect identity, position, college, class, age, measurements, position-specific playstyle, projected round, estimated scout assessment, confidence, and known traits.
- Assignment state, report date, Combine results, eligibility chips, scouting-control mode, and workload summaries do not permanently occupy this board. Assignment controls remain available contextually after selecting a prospect, while detailed reports and public testing information belong on the prospect's scouting/profile detail and relevant event screens.
- Selecting a prospect opens that player’s full scouting/profile detail.

The screen is data-first and designed for active comparison throughout the current draft year. Playstyle is one concise position-specific classification rather than a second trait list. Tiny portraits use authored 32-bit pixel art with crisp hard-edged clusters rather than smooth miniature avatars.

### Prospect scouting report

The prospect profile includes a dedicated **Scouting Report** subheader. A compact authored 32-bit player portrait sits in the identity header, while the report remains the visual focus. Its main body resembles an authored professional football scouting document rather than another board or dashboard. One formal report sheet identifies the scout, date, confidence, position, school, and projected round, then presents an executive summary, narrative strengths, narrative weaknesses, injury history and medical concerns, and a role projection.

Estimated position-specific skill grades use the game's 1–100 scouting scale and remain explicitly labeled staff estimates. College production and verified public testing support the written evaluation in compact tables. Medical language distinguishes past injuries, games missed, recurrence, completed examinations, current clearance, and any remaining concern. The report never exposes true ability, guarantees an outcome, or turns the scout's conclusion into an automatic draft recommendation.

## Draft Board

Draft Board has two distinct board types, keeping the user’s private priorities separate from public draft conversation.

### Team Draft Board

The Team Draft Board is the user’s private, customizable board.

- The user can add or remove prospects.
- The user can manually rank and re-rank prospects.
- The board is one uninterrupted ranked list; it does not group prospects into tiers or expose a tier filter.
- A compact **Owned Picks** line above the list shows every selection the franchise currently controls in the upcoming draft, including the original team for acquired picks. It is passive informational text separated by simple dots rather than a row of clickable pills; only the adjacent View Draft Order text is a link.
- The default board fields are prospect, position, college, playstyle, projected round, estimated scout assessment, confidence, known traits, and scheme fit. Team Need is not a permanent column.
- It represents the franchise’s own draft priorities and is not required to match public consensus or the scouting-board order.

### Public Big Boards

Public Big Boards present external draft-media perspectives.

- Individual analyst boards provide distinct rankings and viewpoints.
- Media/company boards provide a broader outlet-level ranking.
- Each public big board contains either 50 or 100 ranked prospects, depending on that analyst or outlet's established format. Public boards do not extend beyond 100 players.
- Original fictional analyst personalities and media outlets can occupy the same editorial roles as familiar NFL draft analysts and major football networks, without presenting them as real people or brands.
- Public rankings may conflict with each other and with the Team Draft Board, helping the college/draft world feel active and opinionated.

## College Football

College Football is a mini version of the pro-league hub, representing the living college-football universe that feeds the draft pipeline.

### Home

The College Football home screen centers on current college news and rankings, giving the GM a quick pulse of the college season.
Person-specific news uses a clearly human 32-bit portrait; program-level stories may use the applicable school mark. Do not use an animal mascot mark where it could imply that a named player or coach is an animal.

### Sub-tabs

`League Leaders | Bowl Projections | Full Rankings | Awards | News`

- **League Leaders** — college player statistical leaders.
- **Bowl Projections** — the current projected postseason/bowl picture.
- **Full Rankings** — the complete college-team rankings.
- **Awards** — current college award races and results as the season progresses.
- **News** — a college version of the professional League News editorial screen: one lead story, compact secondary stories, dense matchup/score/prospect/wire modules, clickable category headers, and a breaking-news ticker.

The college hub is intentionally smaller in scope than the professional League area, but it should feel like a real ongoing football world rather than a static source of draft prospects.

## Scouting Combine

The dedicated Combine workspace is a numbers-first table for comparing recorded public measurements within a position group. Sortable columns cover physical measurements and relevant drills, with direct routes to the prospect profile, Draft Board, and comparison tools. DNP, a player-skipped drill, and a medical restriction are separate labeled states and never appear as zeroes or poor performances. Public results supplement the scouting picture without revealing true football ratings or a synthetic overall score.

The same prospect list also assembles the team's private appointment targets. Independent **Interview** and **Evaluation** checkboxes use separate allowances: 15 interviews and 30 combined workout/medical appointments per draft cycle. Header counters show projected selected usage before commitment. The same prospect may receive both selections. Checkboxes remain editable until **Confirm Bookings** is pressed; confirmation consumes the slots once and persists the bookings. Before public results exist, the same screen shows the scheduled event and pending state rather than future measurements.

### Prospect interviews

The Interviews area has a separate booked-prospect queue showing ready, in-progress, delegated, and completed states. Once a meeting begins, that queue disappears entirely so the active interview can use the space. A small **Back to Interview Queue** action returns to it.

The active view presents a genuine private Combine meeting without becoming a visual novel. The prospect, player-controlled GM, and Head Coach sit around a modest conference table; the current response appears beneath the room, and subsequent staff interpretation remains in a separate notes strip. A right-side chooser lists questions under broad topics without revealing their hidden evidence mapping. Each question row is directly clickable and immediately asks that question—there is no separate Ask Question or confirmation control. The room's colors, branding, GM, and Head Coach follow the controlled franchise.

Each prospect has five questions total for the draft cycle, independent from the team's 15 booked-prospect allowance. Counts, completed answers, and findings persist through closing, reloading, or delegation. **Delegate Remaining** requires an available Head Coach and transfers only the prospect's unused questions; it does not create additional questions or interview slots. Findings use ordinary football language and remain scouting evidence rather than exact hidden mental ratings or a backend trait answer key.

### Pro Day circuit

Each Pro Day date stops progression at a daily allocation screen when one or more events are available. A single table shows every college holding an event that day, its location, total attending prospects, the numerical count already on the user's board, and the number of staff assigned. Specific prospect identities appear only in the selected-event panel, avoiding redundant columns. Allocation uses numerical minus/plus controls rather than named staff, travel schedules, role assignments, or qualitative coverage grades. Zero is a valid deliberate choice.

Available, allocated, and remaining staff totals remain visible together. Selecting an event opens its attending board prospects with board rank and current confidence. **Confirm Today's Allocations** persists and resolves chosen coverage; **Skip Uncovered Events** explicitly closes zero-allocation events. The game never silently assigns staff or advances past an unresolved daily opportunity. More coverage can improve the breadth or reliability of observations within the existing confidence system, but never guarantees perfect knowledge or exposes true ratings. The exact daily allowance remains tuning work and becomes zero when the entire scouting department is vacant.

### Scouting assignments

Scouting assignments open in a dedicated contextual screen rather than adding permanent assignment columns to the main Scouting Board. The screen follows a Football Manager-style focus model: the user assigns a scout a persistent, filterable brief describing the kind of players to find. Direct Control and Delegate to Department remain visible, reversible modes, and changing mode preserves existing knowledge, saved focuses, and assignment history.

Each saved Scouting Focus can filter by search area, position, college class or age, projected round, playstyle, scheme fit, priority, duration, and weekly load. The selected focus shows its assigned scout's relevant accuracy, speed, specialty, and current workload beside the instruction builder. A Current Matches panel previews players who satisfy the brief using the team's present public and private knowledge; it is a changing candidate pool, not a guarantee that every qualifying player has been discovered or correctly evaluated.

Focuses resolve through the weekly scouting cadence rather than instantly and continue until paused or deleted. Ongoing focuses consume capacity; paused focuses consume none. Delegation creates and manages the same focus structure using staff roles, tendencies, knowledge gaps, and franchise needs. Individual-prospect scouting may remain as a narrow contextual action from a player profile or board row, but reusable briefs are the primary assignment workflow. Professional players receive routine coverage plus optional focused work, while college prospects require a focus, individual assignment, or attended event for private evaluation updates.

## Draft

Draft is a dedicated event space with two clearly different states.

### Before the draft: Draft Room

Until the draft begins, the Draft screen places the GM inside the team's operational war room. It remains a calm preparation space, but the room itself supplies the presentation: staff work around a conference table while large wall displays keep the Draft Order and the top of the user's Draft Board visible together.

The Draft Order display highlights the user's next selection. The board display shows the top five currently ranked prospects with position, college, playstyle, and scout assessment, while a narrow informational strip summarizes every owned pick. A prominent **Start Draft** button launches the draft-opening animation or presentation and then transitions the screen into the live Draft Stage; **Review Draft Board** is the secondary pre-draft action.

The War Room is one reusable interface scene shared by all 32 franchises. Team-specific theme data swaps the redesigned 32-bit logo, colors, wordmark, motto, room archetype, furniture accents, and regional decorations, while the team's current staff and draft data populate the common functional displays.

### During the draft: Draft Stage

When the draft begins, the screen transforms into a dedicated stage/area with much greater presentation emphasis than ordinary management pages.

- A draft-opening mini cutscene or presentation sequence can mark the event beginning.
- The visual design should feel like a live league event, with the current selection and draft progression receiving strong focus.
- Persistent application navigation remains available, so the player can return to other game screens when necessary.
- Draft Order, the user’s Draft Board, and Scouting information stay immediately accessible throughout the event.
- A live bottom ticker shows time remaining for the current selection alongside breaking draft news and recent-pick updates.
- Presentation must enhance the decision, not prevent it: the stage is a focal point, not a screen that crowds out the GM’s working information.
- When the player-controlled team is on the clock, the stage contracts to a compact live feed and the top of the team's remaining Draft Board becomes the primary workspace. The countdown, nearby pick tracker, prospect details, profile and comparison tools, Trade Pick, and Select Player actions remain visible together.
- **Select Player** opens a final confirmation before the choice is submitted. Confirmation triggers the pick-announcement presentation and returns the interface to the standard live-stage state.

The draft experience should feel like one of the franchise’s major annual moments, distinct from the spreadsheet-first Scouting screens that lead into it.

### After the draft: Undrafted Free Agent market

The final draft pick transitions into a dedicated accelerated UDFA market that reuses the familiar free-agency table rather than extending the live Draft Stage. Its deliberately compact rookie columns show player, position, college, age, estimated overall and scouting confidence, playstyle, and contract ask. Search, position, college, confidence, shortlist, and broader filters keep the full pool manageable. Projected role, roster opportunity, competing-team interest, and status are not permanent table columns.

Only membership in a custom shortlist receives a special gold star; former Draft Board membership does not highlight a player. A contextual row menu opens the profile, adds the player to comparison or a shortlist, begins negotiation, or uses one of the separate rookie-minicamp tryout invitations. Beginning talks does not reserve the player, guarantee a roster place, or stop competing clubs.

### Rookie minicamp flow

Rookie-minicamp tryout invitations are sent contextually from the UDFA market rather than through a redundant standalone setup screen. The UDFA screen shows used and remaining capacity for the 10 unsigned tryout invitations and provides the invitation action from an eligible player's row. Drafted rookies and signed UDFAs attend automatically.

Staff conduct the camp automatically; there are no drill or schedule controls. The consolidated camp report arrives through Inbox after the event and links to affected players and updated evaluations. A tryout invitation does not sign the player, consume a roster place, promise employment, or expose a hidden outcome.
