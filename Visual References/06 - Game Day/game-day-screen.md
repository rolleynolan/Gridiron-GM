# Live Game Screen Concept

## Role

Live Game is a Football Manager-style observation screen driven by the authoritative football simulation. The GM watches resolved plays, reviews play-by-play and statistics, controls playback coverage and speed, and may pause to make valid management-level adjustments. The GM never calls an individual offensive, defensive, or special-teams play.

## Layout

- A dedicated scoreboard and situation strip keep score, quarter, clock, possession, down and distance, field position, and play clock visible.
- The left side presents a horizontal top-down 2D pixel field with readable team sprites, ball position, line of scrimmage, first-down marker, and the previous resolved-play caption.
- The right side uses broad Play-by-Play, Drive Summary, Game Stats, and Injuries subheaders. Play-by-Play aligns game clock, field position, event, down/distance, and yards, with a compact team comparison beneath it.
- A thin live-context strip identifies the active personnel packages, delegated play caller, and whether any GM adjustments are pending.
- The bottom bar controls pause/resume, playback speed, highlight coverage, Box Score, and Game Log.

## Interaction and authority

Highlight modes determine which already resolved plays receive field presentation; they never change results to manufacture drama. Pausing permits legal depth-chart, personnel-package, delegation, gameplan, scheme, and playbook adjustments through Live Adjustments, but supplies no direct play-call menu. Every change is validated and clearly marked pending until the next legal break in play.

The field animation, play-by-play, drive state, score, statistics, injuries, and final game log must all consume the same simulation result. The presentation layer cannot independently resolve or reinterpret a play.

## Live Depth Chart and substitutions

The primary paused-game adjustment workspace is a compact live clone of the accepted Depth Chart screen beneath the persistent scoreboard. Offense, Defense, and Special Teams subheaders, package controls, position groups, ordering controls, and manual/delegated control behave consistently with the normal depth chart so the GM can make substitutions without learning a second interaction model. Coaches continue calling individual plays.

The live table replaces season-oriented columns with health, fatigue, game snaps, and current game production. Moving a player up or down creates a clearly highlighted pending substitution and identifies both affected roles. Queue Substitution submits the reviewed change for the next legal break; it cannot alter an already resolved play. Invalid availability, game-day eligibility, or package assignments remain blocked with specific explanations. Discarding or removing a pending swap leaves the applied game depth chart unchanged.

## Sprite animation pipeline

On-field movement combines two independent sources: the authoritative play supplies timed paths, facing, contacts, ball flight, and state changes; reusable sprite libraries supply the visual frames for each state. Do not render every play as a unique video or rotate a side-facing run cycle to fake another direction. Author matching directional cycles for stance, start, run, cut, catch, throw, handoff, blocking, shedding, tackling, falling, getting up, kicking, and short reactions where applicable.

Uniform, equipment, skin tone, body archetype, and jersey layers are assembled over shared motion templates. A player's identity therefore remains consistent without requiring bespoke animations. Playback uses integer coordinates, nearest-neighbor scaling, deterministic timing, and a persisted presentation seed so pausing, speed changes, highlights, and replay do not alter the underlying event.

## Visual direction

The screen balances a strict 32-bit top-down field with dense professional tables. Sprites, field marks, icons, and team identifiers use hard pixel clusters, limited ramps, nearest-neighbor scaling, and integer placement. Avoid broadcast-style 3D presentation, giant players, tactical arrows, probability graphics, momentum meters, ratings, recommendations, or decorative crowd spectacle.

## Postgame Hub

The final whistle routes to a number-first Postgame Hub rather than a celebration splash. A compact final scoreboard and quarter-scoring table lead into broad Summary, Box Score, Team Stats, Player Stats, and Game Log subheaders.

Summary uses stacked authoritative sections for team comparison, game leaders, and injuries and milestones. Injury rows distinguish known game status from diagnoses or recovery estimates that remain pending. Milestones link to the preserved player or career record. Do not add a dedicated Staff Recap block; any necessary coach or medical follow-up belongs in the Inbox or an appropriate contextual report.

Return to Dashboard leaves the completed game; Box Score and Game Log remain directly accessible. Occasional qualifying press conferences use the established conversation template and are triggered separately by actual newsworthy context, so the Postgame Hub does not need a permanent press-conference panel or another bespoke layout.
