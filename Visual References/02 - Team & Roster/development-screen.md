# Development Screen Concept

## Role

Development is a scrollable roster-progress view that answers: *How has each player changed over the last calendar year from the current in-game date?*

## Layout and information

- A single large, scrollable roster table fills the usable workspace.
- Each row represents one player and summarizes their development across the trailing 12 months.
- The primary information is:
  - **Overall trend** — clear direction of development over the period.
  - **Attribute movement** — meaningful gains and losses in player attributes/ratings.
  - **Coach or scout notes** — short contextual observations explaining or qualifying the player’s progress.
- The player/name column stays anchored; position and other useful roster context can be available through the established configurable-header system.

## Interaction

- Users can sort and filter to find breakouts, regressions, positional trends, or players with meaningful recent change.
- Selecting a player opens the player profile and deeper progression history when available.
- The default trailing-12-month period is tied to the current in-game date, so it remains meaningful in every season phase.

## Visual direction

This is another compact FM/OOTP-style working table. Trend direction and rating movement use readable text and restrained status treatment; color reinforces the meaning but never carries it alone. Notes provide the human front-office perspective without hiding the underlying progression data.
