# Setup and Career Entry Concept

## Title menu

The title menu is a distinct front-end presentation rather than an in-career dashboard. A restrained 32-bit football-operations office overlooking a stadium provides atmosphere while one clean menu remains the functional focus.

The user accepted the composition and visual atmosphere of Title Menu v1. **Gridiron GM**, its shield mark, and all current title branding are placeholders pending a later game-name and logo decision. Preserve the layout direction independently from the eventual brand assets.

The menu contains exactly **Continue Career**, **New Career**, **Load Career**, **GM Profiles**, **Options**, **Credits**, and **Quit Game**. Continue Career is the default focused action when a valid recent career exists. A compact Most Recent Career panel identifies that career's team, GM, year/phase, record, and last-played time without turning the title screen into a career library.

With no valid career, Continue Career remains visible but disabled and states why. Keyboard and mouse focus use the same visible selection treatment. The title screen has no in-career top bar, sidebar, Inbox, dashboard tiles, or franchise Continue control.

Ironwood Thunder and Nolan Price are reference data only. The recent-career panel is populated dynamically.

## Career library

The Career Library is the load-and-manage screen for local careers. It uses one dense, full-width table rather than oversized save cards or a fixed number of save slots. Search by team or general manager and newest-played sorting sit above the table, while **New Career** remains available without competing with **Load Career**.

Each row identifies the team, general manager, current year and phase, record or last result, roster source, creation date, and last-played time. There is no separate custom career-name field or always-visible save-health column. Selecting a row reveals its autosave time, last manual save, rolling-backup count, and exactly the actions **Load Career**, **Duplicate Career**, and **Delete**.

Loading is the primary action. A migration, corruption, or recovery state appears only when relevant and routes through a clear confirmation/recovery flow before the simulation is opened. Delete remains visually restrained and requires its own confirmation. Career count is storage-driven rather than represented as a fixed slot limit.

Ironwood Thunder, Bayview Blizzard, Mesa Scorpions, their general managers, and all displayed career metadata are illustrative reference data only and must be populated dynamically.

### Career recovery behavior

Save corruption and recovery use the standard system-popup component rather than a dedicated screen or bespoke visual reference. The popup plainly explains that the save could not be loaded and, when a verified backup exists, offers recovery from that backup or cancellation. It names the affected career and preserves the damaged file separately. Technical diagnostics remain available to logging rather than being presented as a special management screen.

## New career — GM profile

The main New Career wizard contains exactly four steps: **GM Profile**, **Difficulty**, **Team & Roster**, and **Review**. Appearance and management-attribute allocation belong to the reusable GM-profile creator/editor and are not repeated as separate career steps. Roster is not a standalone step: team selection and roster review are one decision surface.

The first screen presents a compact selectable list of reusable GM profiles. Each row shows a professional shoulders-up 32-bit portrait, name, and concise management style. The selected profile's larger portrait and its four stored management attributes—Negotiation, Player Management, Scouting Judgment, and Leadership—appear alongside the list so the user can verify the identity being copied into the new career.

**New GM Profile** opens the complete profile-creation workflow and returns the newly created profile selected. **Edit Profile** updates the reusable source profile before career creation; it never rewrites an existing career's independent GM snapshot. **Continue** advances to difficulty. Profile count is not represented as a fixed slot limit.

The later **Team & Roster** screen contains the Standard/Generated roster-source choice, the complete team selector, and the selected team's roster/context preview together. Changing teams updates the roster preview immediately; there is no separate roster-selection screen.

All shown GM names, portraits, styles, and ratings are illustrative reference data only.

## New career — difficulty

New Career — Difficulty v2 is provisionally accepted as a workable reference. Its underlying choices and presentation will receive a later workshop pass before implementation is considered final.

The setup version of Difficulty reuses the same rules and terminology as the full Career Settings worksheet but removes change-timing and save-management controls. Four presets—**Relaxed**, **Standard**, **Challenging**, and **Custom**—sit above a compact explanation of the selected preset.

The worksheet exposes exactly five affected areas: AI Front-Office Decisions, Negotiation, Scouting Uncertainty, Owner Pressure, and Financial Pressure. Presets show their values as information; selecting Custom makes the individual rows adjustable. The screen explicitly states that difficulty changes management conditions and AI decision quality, never resolved football results.

The same-simulation, no-rigging, and no-hidden-information rules remain guaranteed by the game and are not presented as visible checked or unchecked options. **Continue** advances to the combined Team & Roster screen.

## New career — team and roster

Team and roster selection share one screen. The Standard/Generated roster-source choice sits at the top of the decision area, while all 32 teams remain visible in a compact grid without search or pagination. Selecting a team updates the entire adjacent detail area immediately; no Continue action or second screen is required merely to inspect another roster.

The selected-team header shows identity, conference, preceding-season record, cap space, owner, and competitive outlook. Beneath it, the selected 53-player roster is browsed through the clickable text subheaders **Offense**, **Defense**, and **Specialists**. The compact roster table shows position, player, age, known roster overall, and contract. **View Full Roster** provides the complete list when the position-group preview is insufficient.

Roster-source version is visible alongside Standard/Generated so the user knows which data context is being selected. Choosing Generated starts a one-time seeded world build before team choice can be finalized. It creates and validates all 32 rosters, contracts, cap positions, owners, starting histories, and other required populations, then derives team outlooks and populates these previews. The interface shows a clear generation state rather than temporary or fabricated values. Team identities and league rules remain fixed, and the completed generated world is preserved into Review and the career without rerolling. All team identities, logos, players, ratings, contracts, owner names, financial values, and outlooks shown in the reference are illustrative and dynamic.

The 32 logos in the mockup are layout placeholders only. Production uses the separately planned cohesive 32-logo hard-pixel pass.

New Career — Team & Roster v2 removes the unnecessary search field and uses that space to enlarge the always-visible 32-team selector.

## New career — review and create

New Career — Review v1 is accepted as the working final-step reference.

The final Review screen is a concise factual summary rather than another settings page. It shows the selected GM portrait, identity, management style, and four attributes; the selected team, roster source/version, roster count, preceding-season record, cap space, owner, and outlook; the difficulty preset and its five values; and the saved starting year, phase, and calendar date.

Each editable decision group has a restrained **Change** link that returns to its corresponding wizard step without discarding other selections. Career-start values are informational on this screen. There is no custom career/save name, seed display, save-location field, consent checkbox, or redundant confirmation control.

**Create Career** persists the reviewed choices and enters the world-creation/loading flow. With Standard, it copies the versioned standard world; with Generated, it uses the already completed and validated world from Team & Roster rather than rerolling it. The completed career opens into the controlled franchise and triggers the owner's welcome email through the normal inbox flow.

## Reusable GM profiles library

The standalone GM Profiles screen manages reusable identities outside any individual career. A compact list shows each profile's professional shoulders-up portrait, name, and management style. Selecting a profile opens its larger portrait, four management attributes and effects, point allocation, creation date, and last-updated date in the adjacent detail pane.

The screen supports **New GM Profile**, **Edit Profile**, and **Delete**. Profile duplication is unnecessary and is not offered. It does not contain Select, Use Profile, Continue, team assignment, or career results because those actions belong to New Career or an active career. Deleting a reusable profile requires confirmation and never deletes or rewrites any career that already copied it.

Profiles are not represented as fixed slots, levels, unlocks, or progression accounts. Nolan Price, Maya Torres, Andre Cole, their portraits, styles, ratings, and dates are illustrative reference data only.

## GM profile editor — identity and portrait

GM Profile Editor — Identity & Portrait v1 is accepted as the working identity-editor reference.

The reusable profile editor has two clickable subheaders: **Identity & Portrait** and **Management Attributes**. Identity & Portrait shows one professional shoulders-up preview and a compact modular control form. Name and age define the identity; numbered step controls select face shape, skin tone, hair style, hair color, facial hair, eye style, suit style, and tie color. Relevant palette swatches accompany their numbered values so color is never the only indicator.

Every control updates the same portrait immediately. **Randomize Appearance** selects a valid combination from the same curated component library; it does not create a separate art style or invoke an unconstrained portrait generator. The saved profile stores component IDs and palette choices, allowing the same portrait to render consistently on profile, meeting, inbox, and career surfaces.

The editor is shoulders-up only. It does not offer full-body modeling, photo upload, paid cosmetics, unlocks, XP, or career/team data. **Save Profile** commits both editor tabs, while Cancel and Reset Changes are non-destructive.

## GM profile editor — management attributes

GM Profile Editor — Management Attributes v2 is provisionally accepted as the working reference, with later balance and wording refinement still allowed.

Management Attributes uses the same editor shell and keeps the selected GM portrait and identity visible. The user distributes exactly 16 points across Negotiation, Player Management, Scouting Judgment, and Leadership on the 1–10 scale using compact minus/plus controls. Allocated and remaining totals update immediately, and Save Profile is unavailable while the allocation is invalid.

Each row uses one **What It Does** description to identify the systems influenced by that attribute; the numeric rating communicates its strength. Exact balance curves or capped values can be exposed contextually when useful without duplicating two effect columns in the allocation table. Effects remain modest at low and middle ratings and never guarantee outcomes, alter player talent, reveal true ratings, or change game results. The derived management-style label updates from the allocation but adds no separate hidden modifier.

There are no skill trees, perks, XP, levels, badges, unlocks, sliders, percentages, or extra attribute categories.

## Generated-world loading behavior

Generated world creation does not use a separate screen. Team & Roster remains visible with its selection controls temporarily disabled and a compact loading treatment while the full seeded world is built and validated. When generation succeeds, the same screen populates all team and roster previews; failure offers Retry or a return to Standard without accepting partial data. Do not expose a stage dashboard, invented percentage, or ETA.
