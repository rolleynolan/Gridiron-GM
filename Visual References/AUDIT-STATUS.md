# Visual Audit Status

Status reconciled September 16, 2026. This file is the implementation-facing index for the visual-reference audit. It supersedes stale `Pending review` wording inside the chronological generation log; that log remains the record of how each concept evolved.

## Audit outcome

The first-pass audit now covers every major 1.0 workflow and application area. No additional unique full-screen mockup is required before implementation planning. Screens described as provisional below are deliberately deferred for a later refinement workshop and must not hold up the next audit phase.

Mockups establish composition, information hierarchy, interaction direction, and the hard-pixel visual target. Names, dates, values, people, team identities, and Ironwood-specific content are illustrative and must be populated from the active save.

## Current working references

### Application shell and entry

- `07 - Setup & Utility/title-menu-v1.png`
- `07 - Setup & Utility/career-library-v2.png`
- `07 - Setup & Utility/new-career-gm-profile-v2.png`
- `07 - Setup & Utility/new-career-difficulty-v2.png` — provisional
- `07 - Setup & Utility/new-career-team-roster-v2.png`
- `07 - Setup & Utility/new-career-review-v1.png`
- `07 - Setup & Utility/gm-profiles-library-v2.png`
- `07 - Setup & Utility/gm-profile-editor-identity-v1.png`
- `07 - Setup & Utility/gm-profile-editor-attributes-v2.png` — provisional
- `07 - Setup & Utility/career-settings-difficulty-v1.png` is the reusable settings-screen shell; separate mockups are not required for every settings category.

### Home, team, and roster management

- `01 - Main Hub/home-dashboard-compact-v2.png`
- `01 - Main Hub/player-holdout-inbox-v2.png`
- `02 - Team & Roster/roster-compact-v2.png`
- `02 - Team & Roster/depth-chart-table-v3.png`
- `02 - Team & Roster/staff-compact-v1.png`
- `02 - Team & Roster/injuries-compact-v1.png`
- `02 - Team & Roster/team-stats-compact-v1.png`
- `02 - Team & Roster/team-history-encyclopedia-v3.png`
- `02 - Team & Roster/team-info-compact-v1.png` — provisional
- `02 - Team & Roster/weekly-roster-availability-v1.png`
- `02 - Team & Roster/training-camp-player-focus-v2.png`
- `02 - Team & Roster/training-camp-weekly-report-v1.png`
- `02 - Team & Roster/final-roster-cutdown-v1.png`
- Practice Squad reuses the Roster screen structure with practice-squad-specific status and actions.

### League and history

- `League/league-standings-no-panel-v2.png`
- `League/league-stats-clickable-groups-v4.png`
- `League/schedule-results-single-list-v2.png`
- `League/league-news-32bit-v2.png`
- `League/league-news-article-v2.png`
- `League/player-search-compact-v1.png`
- `League/league-history-broad-categories-v4.png`
- `League/league-history-encyclopedia-v1.png` supplies the detailed subject-page direction opened from the broad history directory.
- `League/league-awards-race-v1.png`
- `League/league-awards-premidseason-v1.png`
- `League/hall-of-fame-class-results-v1.png`
- `Trade Center/official-transaction-register-v2.png` is the single league-transactions record.

### Players, scouting, college football, and draft

- `Player Profile/player-profile-compact-v1.png`
- `Player Profile/player-comparison-v1.png`
- Player and staff conversations reuse the accepted meeting template with data-driven participants and text.
- `Scouting/scouting-board-playstyle-v3.png`
- `Scouting/team-draft-board-pick-summary-v3.png`
- `Scouting/public-big-boards-v1.png`; public big boards contain 50 or 100 players.
- `Scouting/prospect-scout-report-v2.png`
- `Scouting/combine-results-bookings-v1.png`
- `Scouting/prospect-interview-focused-v3.png`
- `Scouting/pro-day-allocation-clean-v3.png`
- `Scouting/scouting-focus-instructions-v2.png` — provisional
- `Scouting/college-football-home-human-prospect-v2.png`
- `Scouting/college-league-leaders-v1.png`
- `Scouting/college-bowl-projections-v1.png`
- `Scouting/college-full-rankings-v1.png`
- `Scouting/college-awards-race-v1.png`
- `Scouting/college-news-editorial-v2.png`
- `Scouting/draft-war-room-pre-draft-v2.png`
- `Scouting/draft-stage-live-v1.png`
- `Scouting/draft-on-clock-selection-v1.png`
- `Scouting/udfa-market-v2.png`
- `Scouting/rookie-minicamp-invites-v1.png`

### Contracts, finances, staffing, and transactions

- `Finances/team-finances-number-first-v2.png`
- `Finances/accounting-monthly-v2.png`
- `Finances/contracts-cap-sheet-v1.png`
- `Finances/contract-restructure-v3.png`
- `Finances/player-release-popup-v2.png`
- `Finances/fifth-year-options-arbitration-style-v1.png`
- `Finances/tags-and-tenders-v1.png`
- `Finances/facilities-compact-v1.png` — provisional; facility costs ultimately appear in Team Finances and grades may appear in Team Info.
- `05 - Management/annual-owner-meeting-staff-review-v2.png`
- `05 - Management/staff-candidate-interview-v2.png`
- `05 - Management/staff-interview-permission-v1.png`; its copy requires a natural-language pass.
- `Trade Center/build-a-trade-eight-slots-v2.png`
- `Trade Center/trade-block-pre-submit-v4.png`; this is the trade finder/market workflow. Offers appear only after the outgoing package is submitted.
- `Trade Center/free-agency-full-table-v2.png`
- `Trade Center/contract-negotiation-streamlined-v3.png`
- `Trade Center/waiver-wire-ootp-v1.png`
- `Trade Center/waiver-claim-email-v2.png`
- `Trade Center/trade-deadline-day-v2.png`

### Game day

- `06 - Game Day/live-game-observer-v1.png`
- `06 - Game Day/live-depth-chart-v2.png` — provisional
- `06 - Game Day/postgame-hub-v2.png`
- The sprite storyboard and run-cycle images are technical experiments, not production-quality art references. The production player/coach sprite system, component library, directions, poses, and animation set remain a separate art and implementation workstream.

## Removed or shared instead of separate screens

- No standalone accounting ledger; use the monthly Team Finances/accounting view.
- No separate Practice Squad design; clone the Roster structure.
- No separate player-release page; use the contextual confirmation popup.
- No separate staff/player meeting layouts for each conversation type; reuse the meeting template.
- No separate post-draft invitation screen outside the UDFA-to-minicamp workflow.
- No permanent right-side free-agent offer panel; right-click a player and choose Make Offer.
- No separate trade finder and trade block; the trade-block workflow is the finder.
- No separate Active Offers screen; unresolved submitted packages and responses remain inside Trade Block / Finder.
- No separate news archive duplicating the transaction register.
- No standalone generated-world progress screen; show a blocking state inside Team & Roster.
- No bespoke career-corruption screen; use the standard system popup.
- No dedicated world-generation settings reference.
- No duplicate league-transaction screen.

## Deferred visual work

- Revisit all references marked provisional only when the user starts the later refinement workshop.
- Replace the placeholder game name, game logo, executable identity, and connected branding in one coordinated rebrand pass.
- Continue production QA of the completed 32-team hard-pixel logo set. All 32 identities have accepted runtime marks; CanvasItem textures now default to nearest-neighbor filtering, every current logo control preserves nearest filtering, and automated asset smoke verifies all expected textures load. Interactive alpha/compact-size review and helmet placement remain with the uniform/production-art pass.
- Build and validate the modular human portrait, coach/player model, uniform, pose, and animation pipeline.
- Perform a dedicated copy-editing pass for generated emails, interviews, reports, and conversation text.

## Shared implementation rules

- Use strict hard-edged 32-bit pixel art with deliberate clusters and stepped silhouettes; avoid smooth vector, photographic, glossy 3D, chibi, mascot-like, or emoji rendering.
- Do not place design commentary, helper disclaimers, coordinate notes, or prompt text inside production UI.
- Statistics are current unless a historical context is explicitly selected; do not add redundant data-currency labels.
- Estimated grades belong only to genuinely uncertain scouting information. Known roster ratings use their normal labels.
- Prefer dense tables, text, and numbers. Visual scenes are reserved for moments where presentation materially supports the experience.
- Hyperlink relevant teams, players, games, reports, and historical subjects directly in text where appropriate.
- Ironwood Thunder is a reference team only. Every franchise-dependent screen uses the controlled team's live identity, colors, personnel, picks, finances, and data.

## Next audit phase

Cross-system presentation reconciliation is recorded in `BLUEPRINT.md` under **Visual audit implementation contract**. Proceed with the approved vertical slice: Dashboard → Roster/Player Profile → Depth Chart → Game Day → Postgame. Provisional visual refinement, the game-level rebrand, copy editing, and the modular portrait/animation pipeline remain separately scoped work and must not block this slice.
