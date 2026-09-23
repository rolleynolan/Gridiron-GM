# Visual Direction: OOTP depth, NFL Head Coach energy

## Core intent

GridironGM should feel like a serious football front office: information-rich, quick to navigate, and satisfying to study. The primary model is **Out of the Park Baseball**—a dense management workspace where nearly every important detail is visible or only a few clicks away. The visual layer should add selective hints of **NFL Head Coach**: football atmosphere, team identity, and moments with enough polish and focus to remind the player that they are running a living franchise, not browsing a database.

## Design balance

| Prefer | Avoid |
| --- | --- |
| Compact, readable tables and clear data hierarchy | Large empty dashboards that hide key information |
| Persistent navigation and predictable screen layouts | Deep, opaque navigation paths |
| Team colors and football imagery used as accents | Decoration that competes with decisions or data |
| Rich visual focus for major moments—game day, draft, signing, trade | Applying the same visual intensity to every utility screen |
| Quick drill-down from summaries to player/team detail | Information that requires repeated modal windows or extra clicks |

## Proposed visual language

- **Foundation:** dark or neutral application frame, dense panels, tabular views, compact typography, and strong headers—built for long management sessions.
- **Data hierarchy:** important values get weight through placement, alignment, color status, and modest type contrast; raw numbers remain easy to scan in rows and columns.
- **Football identity:** team-color bands, subtle stadium/playbook/gridiron textures, player imagery or silhouettes where valuable, and branded presentation around major franchise events.
- **Interaction:** a player, team, stat, or contract should lead naturally to a deeper detail view. Tables should support sorting, filtering, comparison, and easy cross-navigation.
- **Pacing:** ordinary management is utilitarian and calm. Key events can briefly become more cinematic without breaking the underlying UI system.
- **No mockup commentary:** do not place explanatory comments, design disclaimers, helper notes, instructional hints, or similar commentary in footer strips on image references. If information is not part of the actual production interface, record it in the written concept notes rather than drawing it into the screen.

## Screen types

1. **Operational screens** — roster, depth chart, stats, contracts, scouting lists. These are spreadsheet-first and optimize density, scanning, filtering, and comparison.
2. **Overview screens** — team hub, league home, player profile. These combine a visual focal point with immediately actionable summaries.
3. **Event screens** — draft, trade completion, free-agent signing, game day. These earn more visual presentation, while keeping outcomes and next actions clear.

## First UX principle

Every screen should answer: *What can I decide here, what evidence supports that decision, and where do I go next?*
