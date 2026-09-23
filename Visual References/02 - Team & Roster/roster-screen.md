# Roster Screen Concept

## Role

Roster is a Football Manager-style front-office table: one large, information-dense player list occupying the usable workspace. It is designed for scanning, comparing, sorting, and quickly opening a player’s deeper detail—not for decorative dashboard tiles.

## Layout

- The persistent left navigation and quick-glance bar remain in place.
- A compact screen header identifies `Team > Roster` and holds high-value table controls.
- The roster table fills the rest of the available screen space.
- The player name/identity column remains anchored when the player scrolls horizontally through additional information.

## Core table columns

The initial visible table columns are:

| Column | Purpose |
| --- | --- |
| Player | Name and compact player identity, with a route to the player profile |
| Age | Current age |
| Position | Primary position, with optional depth/role context |
| Contract | Contract status and key contractual context |
| Health | Current injury/availability status |
| Morale | Current morale/status indicator |
| Scout Overall | The user’s scouting department’s current overall assessment, with uncertainty retained where appropriate |

Column headers are sortable. This gives the player an FM-style working table: click a heading to order the roster by age, position, contract status, health, morale, or the scouting department’s opinion.

## Customizable headers and columns

The table uses Football Manager-style configurable headers rather than a permanently fixed column set.

- The user can replace a visible column with another available player field (for example, age can be swapped for experience, salary, potential, a position-specific rating, or a statistical category).
- Columns can be shown, hidden, and reordered.
- Each column’s width is manually adjustable by dragging its header divider; the selected width persists with the user’s roster view.
- The base view supplies the core columns listed above, while the player can create a lean contract-focused, scouting-focused, performance-focused, or position-specific view from the same table.
- The anchored Player column remains protected as the roster’s identity reference, even when other headers are customized.

Column customization must remain fast and discoverable from the header area, without forcing users into a separate settings screen for routine changes.

## Essential interaction

- Select a player to open the full player profile.
- Search, position filters, and compact roster-status filters make large rosters easy to work through.
- The user can customize visible columns, their order, and their widths directly from the header area to create a preferred working view.
- Health, morale, and scouting information should use restrained status cues alongside readable text; color alone must never carry the meaning.
- The table supports wide displays by allowing additional columns, but the initial view keeps the core decision information visible without horizontal scrolling.

## Visual direction

The roster is deliberately spreadsheet-first. Rows, columns, subtle alternating backgrounds, precise alignment, and compact typography provide the OOTP/FM working feel. Team branding may appear in the header and selected-row states, but should not compete with player comparison.

## Final roster cut-down

Final Cut-Down is a batch worksheet reached from roster-compliance reminders and the Roster screen. It shows the current limit, checked proposed releases, projected roster count, cap-space change, added dead money, and projected position compliance in one compact summary strip.

The full-width player table uses checkboxes to build the proposed cut list and keeps player, position, age, experience, OVR, current depth, health, contract, cap savings, dead money, waiver requirement, and practice-squad eligibility aligned. Checking a row recalculates projections but performs no transaction. Practice-squad eligibility never implies availability or automatic placement.

Confirmation validates and executes only the reviewed checked releases. Every release still requires explicit GM approval, staff never cut players automatically, and changed roster or contract state returns an invalid batch for correction rather than partially executing it.

## Practice Squad view

Practice Squad is a direct Roster view, selected through the roster-status filter and reflected in the breadcrumb and capacity counters. It preserves the same searchable, sortable table and column controls while replacing active-roster fields with practice-squad contract, weekly pay, elevation usage, and eligibility basis.

The contextual player menu separates temporary elevation from permanent signing to the active roster and retains profile, comparison, shortlist, contract, and release routes. This screen manages players already under practice-squad contract; free-agent acquisition still begins with the single Make Offer action, with contract type chosen inside negotiation.

## Weekly availability and game-day roster

Weekly Availability is a dense pregame administration view reached from Roster and contextual inbox reminders. A factual status strip keeps active-roster count, inactive-selection progress, temporary elevations, validation state, and the current GM/staff control mode visible without turning them into dashboard cards.

The main Game-Day Roster table uses checkboxes to select inactives and aligns player, position, depth, OVR, health, weekly practice participation, and game availability. Selection is manual by default; the GM may explicitly delegate it to staff and can review or override delegated choices before confirmation. Injured, questionable, and healthy depth players remain visible together so the decision has its actual roster context.

Temporary elevation remains a separate stacked section and transaction. It shows the selected practice-squad player, OVR, health, prior elevation use, and scheduled reversion, with dedicated change and detail-review actions. It must never be presented as the same operation as choosing an inactive or permanently signing a player. Confirmation validates the complete game-day roster and reports specific blocking violations rather than silently changing personnel.
