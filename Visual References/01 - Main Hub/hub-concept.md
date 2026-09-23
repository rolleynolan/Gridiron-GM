# Franchise Hub Concept

## Role

The franchise hub is the player’s command center. In one glance it should establish the current state of the team and the immediate decisions competing for attention. It should feel like the customizable team pages in OOTP and the tile-driven overview screens in Football Manager.

It is not a replacement for detailed roster, finance, scouting, or league screens. It is a personalized launch point into them.

## Page structure

1. **Persistent primary navigation** — a top bar or left rail containing the main areas of the game.
2. **Contextual secondary navigation** — appears under or beside the active primary area, exposing that area’s relevant subsections.
3. **Franchise status strip** — compact, persistent context: team identity, current date/week/season phase, record, inbox notifications, and the control to advance time.
4. **Customizable tile grid** — the body of the hub, with information tiles chosen and arranged by the player.

### Dashboard-grid model

The Home layout follows the referenced OOTP-style structure: a fixed **left navigation rail**, a slim **top quick-glance/status bar**, and a tile grid filling the remaining workspace. The status bar and navigation never move; only the dashboard grid is customized.

- The grid uses a fixed number of available cells based on the usable screen size, while preserving a readable minimum cell size.
- At the reference desktop size, the default grid is a practical **3-column by 2-row workspace** (six 1×1 cells). The grid may add or remove columns/rows at other supported screen sizes rather than shrinking content until it is unreadable.
- Tiles can occupy adjacent cells in supported shapes: `1×1`, `2×1`, `3×1`, `1×2`, `2×2`, and other shapes only when the current grid has room.
- A player may use six compact `1×1` tiles, one wide `2×1` tile beside a `1×1` tile, a full-width `3×1` tile along the bottom, or another valid arrangement.
- Tiles snap to the grid. In Edit Dashboard mode, a player can drag a tile, change its size, choose its content, remove it, add a module to an empty space, or reset the default layout.
- Tile content adapts to its allocated size: a larger news tile shows more headlines, a wider standings tile can show full divisions, and compact tiles show only the most essential data.
- The system should prevent overlaps and leave the layout in a valid, filled state when resizing or changing screen size; it can reflow tiles predictably and allow the player to fine-tune afterward.

## Navigation model

The navigation should keep the OOTP principle: almost all information is within a few predictable clicks.

| Main area | Example subsections |
| --- | --- |
| Home | Franchise Home |
| Inbox | Email / Messages |
| Team | Roster, Depth Chart, Team Standings, Team History, Staff, Team Stats, Injuries, Development |
| Finances | Team Finances, Contracts, Accounting |
| League | League Standings, League Stats, Scores & Schedule, News, Leaders, Awards, League History, Player Search |
| Scouting | Scouting Board, Draft Board, College Football, Draft |
| Trade Center | Trades, Trade Block, Trade Finder, Active Offers, Free Agency, Waivers, League Transactions |

The exact names can change, but every main area should have a clear home and a stable set of subsections.

### Confirmed primary navigation

`Home | Inbox | Team | Finances | League | Scouting | Trade Center`

This is deliberately compact. The rail carries the seven permanent destinations; detailed screens live in the contextual secondary navigation rather than adding more top-level categories.

### Subsection intent

- **Home** is a single, direct route to the customizable franchise dashboard.
- **Inbox** is a single, direct route to the franchise email/messages page. It is the place for communication and actionable notices, not a second dashboard.
- **Team** contains information and decisions specific to the player’s franchise: its people, performance, identity, and history.
- **Finances** contains all financial and contractual information affecting the franchise, including revenue and cost drivers such as attendance and the stadium.
- **League** contains the league-wide view: results, information, historical context, and player discovery across all teams.
- **Scouting** contains the prospect-evaluation pipeline and every part of preparing for and completing the draft.
- **Trade Center** contains player-movement tools and the record of movement across the league.

### Team navigation behavior

`Team` is an expandable primary-navigation group rather than a standalone landing page. Selecting it in the left rail opens a compact dropdown/flyout of its subsections; selecting a subsection opens that screen directly.

Initial Team submenu:

`Roster | Depth Chart | Team Standings | Team History | Staff | Team Stats | Injuries | Development`

The expanded navigation should stay compact and predictable, with a clear active-screen state. It can collapse when the player opens another primary category or chooses to collapse the rail, preserving workspace for data-heavy screens.

## Tile system

Tiles are reusable modules rather than one-off dashboard widgets. The player can choose which modules appear, arrange them, and ideally vary the layout by screen or saved view.

Useful initial tile categories:

- **Immediate action:** unread inbox items, decisions due, pending offers, injured players, roster violations.
- **Team snapshot:** record, recent form, next opponent, team ratings, cap space, morale/chemistry.
- **Roster intelligence:** position strengths/weaknesses, depth concerns, development watchlist, expiring contracts.
- **League context:** division standings, league news, stat leaders, playoff picture.
- **Season flow:** calendar, upcoming events, recent results, draft/free-agency countdowns.

## Tile behavior

- Each tile should have a clear title, a compact summary, and one obvious click-through destination.
- A tile may offer light controls—such as a timeframe, position group, or team selector—without becoming a mini full screen.
- Tiles should support a small number of widths/heights so grids stay ordered and data remains legible.
- Customization belongs in an unobtrusive “Edit dashboard” mode: add, remove, rearrange, reset, and save a layout.
- The default layout must be opinionated and helpful; customization enhances it rather than becoming required setup.

## Visual balance

The grid is practical and data-first. Football flavor should frame the experience: the team’s color and identity in the status/header area, subtle treatment around matchup and milestone tiles, and more visual weight for the upcoming-game tile. Ordinary status modules remain calm, compact, and easy to scan.

## Default hub draft

The default Home page is a quick league refresher rather than a task list. It answers: *Where do we stand, what is happening around the league, and what storylines should I follow?*

1. **Standings** — the largest and highest-priority tile. Show the player’s conference by default, organized into its divisions, with the other conference one arrow click away.
2. **GM Profile / Career Stats** — a compact personal legacy snapshot: GM portrait, age, current team, career record, seasons, playoff results, championships, reputation or milestones, and job security.
3. **Top League News** — current league headlines and storylines, with a clear path to the full news feed.
4. **Top Prospects / Draft Board** — the most notable available prospects or the user’s pinned draft targets, appropriate to the current point in the season.
5. **Team Stat Leaders** — leaders from the user’s team for the most relevant offensive, defensive, and special-teams categories.

Useful secondary or optional tiles: recent results, upcoming game, calendar, injury report, cap snapshot, position needs, development watchlist, inbox summary, and league leaders.

The header/status strip remains compact: team identity, season phase, current date/week, record, inbox notifications, and the Advance control. Actionable items should be visible there or in Inbox; they should not displace the Home page’s league-overview purpose.

### Persistent quick-glance bar

The top bar is intentionally limited to the information needed across every screen:

- **Team** — franchise identity, with an optional route back to Home.
- **Current date** — including the relevant week and season phase.
- **Record** — the user’s current regular-season record.
- **Inbox notification badge** — an unmistakable unread/action-needed count, linking to Inbox.
- **Advance** — the primary time-progression control, clearly labeled with the next advance point when helpful (for example, `Advance to Week 4`).

Salary-cap data and GM-profile access do not belong in this persistent strip. They remain available through Finances and the Home GM tile, preserving space and keeping the bar focused.

## Inbox page concept

Inbox uses a familiar two-pane email layout:

- **Left pane:** a scrollable message list with sender, subject, compact preview, date/time, and an unread or action-needed indicator.
- **Right pane:** the selected email’s complete content, relevant context, and any available response/action controls.

The page is organized into two top-level categories.

### Action Required

These messages represent decisions that must be resolved before the player can continue simulation past the relevant point. They are visually prominent, remain unread/actionable until handled, and drive the inbox notification badge.

- Trade offers and time-sensitive counteroffers.
- Roster issues that make the team illegal or require a decision.
- Salary-cap, contract, budget, attendance, stadium, or other financial issues requiring prompt GM action.
- Other urgent front-office events a real GM would need to address immediately.

The Advance control should explain when an unresolved Action Required message is preventing progression and route the player directly to the relevant message.

### Updates

This category contains non-blocking information that keeps the GM connected to the franchise and league world. These messages can be read, archived, or left for later without stopping the simulation.

- Team and player news.
- Reminders and routine staff communication.
- Game results and notable team performance.
- Scouting reports and prospect updates.
- Other reports sent by coaches, scouts, finance staff, ownership, or league personnel.

Emails should feel authored by their sender and lead naturally to their underlying context when useful—for example, opening a player profile, a trade workspace, scouting report, or game result. The layout remains straightforward and data-friendly, while the writing and sender identity give the front office a sense of people working around the GM.

Player holdouts arrive as authored messages from the player's representative rather than opening a permanent holdout-management screen. The email body should sound like that specific person communicating the player's position, while the structured situation panel carries the factual demand, signed-contract status, participation status, and team availability without repeating the same information in system language. It links to the player profile, current contract, and existing contract-discussion workflow. Attendance and applicable financial consequences are tracked automatically rather than presented as a manual punishment button.

### Standings tile interaction

The large standings space is a **two-in-one conference carousel**, not several competing dashboard tiles.

- It displays one conference’s complete standings at a time, organized by division.
- Compact left/right arrows in the tile header switch to the other conference, sliding the tile’s contents horizontally like a small slideshow.
- The default conference should be the one containing the player’s team; the player’s row is visually distinct but not distracting.
- A clear conference label and small position indicator make it obvious which view is currently active (for example, `AFC  •  1 of 2`).
- Clicking the tile outside of its arrows opens the full League Standings screen.

This retains the OOTP-style quick reference: both conferences are available from Home without crowding out news, prospects, and team leaders.

### GM Profile tile and profile page

The GM profile represents the user as a person within the league world. It should have the prominence and personality of an OOTP or Football Manager manager profile, rather than reading as a generic user account.

#### Home tile

- A portrait/avatar, GM name, age, and current franchise identity.
- A concise career line: seasons managed, regular-season record, playoff record, and championships.
- **Job security** presented as a clear, at-a-glance status (for example, Secure, Stable, Under Pressure, or Critical), using restrained status color and an optional meter.
- One short current-context note, such as the owner’s expectation, a recent milestone, or the season’s stated objective.
- Clicking the tile opens the full GM profile.

#### Full profile

- Portrait, name, age, and career timeline.
- Career totals and season-by-season record.
- Playoff appearances, championships, awards, notable milestones, and previous teams if the career model supports them.
- Current job-security detail: owner expectation, relevant performance target, and the factors affecting standing with the franchise.
- Optional personality/management attributes only if they have actual game consequences; the profile should not add decorative ratings with no meaning.

#### Visual direction

This is one of the places where the game can feel more personal and presentation-driven. Use the portrait, team identity, and career story to give the card warmth, but preserve compact numeric totals and clear status information so it remains useful on every visit.

### Top League News

The news module should read like the front page of a current football news outlet—an active mix of credible reports, current narratives, and league conversation—not merely a chronological simulation log.

#### Content types

- Trade rumors and reports of teams shopping or pursuing players.
- Player skepticism or praise: performance narratives, breakout stories, declining veterans, chemistry concerns, and coaching pressure.
- Transactions: trades, signings, releases, waiver claims, retirements, and major contract events.
- Recent game results: marquee outcomes, upset results, winning/losing streaks, and record performances.
- Major league events: injuries, awards, playoff-clinching scenarios, draft developments, and personnel changes.

#### Home tile

- Show a small, curated set of the most relevant/current headlines rather than a dense unfiltered list.
- Give the top headline more visual hierarchy; subsequent headlines stay compact and scannable.
- Each item shows a timestamp or relative recency, a concise headline, and optionally its team/player association.
- Selecting a headline opens the full story or its relevant detail screen (player, team, game, transaction, or rumor context).
- A `View All News` action opens the dedicated League News screen.

The dedicated news screen belongs under **League** and supports browsing the larger feed; the Home tile is its high-signal editorial snapshot.

### Team Stat Leaders

This tile is a quick production snapshot for the user’s franchise, showing the players currently driving the team rather than a full statistical table.

- **Passing:** leader in passing yards.
- **Rushing:** leader in rushing yards.
- **Receiving:** leader in receiving yards.
- **Defense:** tackle leader, sack leader, and interception leader.
- **Kicking:** include a concise kicking line—such as field goals made/attempted or field-goal percentage—when the selected tile size has room.

Each entry should use the player’s name, a small positional cue, and the leading value; selecting it opens that player’s profile or detailed team-stat view. The tile should preserve the hierarchy of the core six categories before adding any secondary stats, and it should be able to expand/resize if a user wants a richer breakdown.

### Prospects / college tile interaction

The prospect space is another multi-view tile: it provides a compact look at the upcoming talent pool and the college world without trying to reproduce the full scouting screen.

- The opening view shows **5–10 notable top prospects**, using a concise list with position, school, broad prospect status/ranking, and only the most useful quick context.
- Header arrows slide through additional views within the same tile footprint:
  1. **Notable Prospects** — the current top 5–10 prospects.
  2. **Analyst Draft Board** — an external/league-media top 10, distinct from the user’s private scouting board.
  3. **College Rankings** — a top-ranked college-team list and its current-season context.
- A compact view indicator makes the current panel explicit; each panel has a clear click-through to its full screen in Scouting or the future college/league area.
- The player’s personally scouted or pinned prospects should remain private; the analyst board represents public consensus and may reasonably differ from the user’s evaluation.


## Open design decisions

- Should primary navigation be a horizontal top bar, a collapsible left rail, or a hybrid?
- Should the hub allow several saved layouts (for example, in-season versus offseason)?
- Which items should be true blocking alerts versus passive tiles?
- How much team imagery belongs on the hub before it reduces usable information density?
