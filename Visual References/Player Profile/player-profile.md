# Player Profile Concept

## Role

Player Profile is the shared detail screen reached from roster, scouting, league, transaction, injury, and stat-leader views. It combines the player’s human identity with the information needed to evaluate them.

For draft prospects, the shared profile substitutes prospect-relevant subheaders and includes a dedicated Scouting Report. That view is an authored report document containing strengths, weaknesses, injury history and medical context, college statistics, verified testing, and estimated position-specific skill grades.

## Header

The header gives the player an immediate identity and contract/status snapshot:

- Player face/portrait.
- Basic bio information.
- Contract information.
- Current morale.

The player’s current team and key football identity should be immediately clear alongside this information.

## Main detail area

Below the header, one large tile is divided into four clear sections:

1. **Attributes** — player ratings/attributes.
2. **Scout and Coach Assessments** — the relevant evaluations and observations from the franchise’s staff.
3. **Current-Year Stats** — the player’s statistics for the current season.
4. **Traits** — the player’s traits and their relevant context.

The sections should be visually organized for quick scanning while allowing a deeper drill-down where the underlying data supports it. Player Profile is information-rich but not a collection of unrelated tabs—the key facts remain visible together in the main view.

## Visual direction

The portrait and header give the profile personality, while the large lower area preserves the OOTP/FM-style data density. This is the primary place where players should feel like individual people within the football world, not only entries in a table.

## Player comparison

Player Comparison is a dense side-by-side worksheet derived from the profile rather than a separate grading system. It supports up to four players at once and keeps a fixed label column beside equal-width player columns so every fact shares a row, unit, and timeframe.

The default **Overview** combines identity, experience, playstyle, key estimated ratings, current-season production, career production, contract summary, and availability. Flat clickable subheaders switch the worksheet among **Overview**, **Attributes**, **Production**, **Contract**, and **Scouting** without using dropdowns or opening permanent side panels. Each filled column links to the full profile and can be removed; an unused column remains a quiet, table-aligned **Add Player** slot.

Comparison never declares a winner, calculates a synthetic comparison score, or supplies a best-fit recommendation. It shows known information only, leaves unavailable information visibly blank, and follows the same scouting-confidence and knowledge boundaries as Player Profile. Portraits remain small strict 32-bit identifiers; aligned numbers and text are the focus.

## Player conversations and promises

Talk to Player reuses the established meeting-style conversation template with the player and an agent where applicable; it is not a separate screen design for every discussion topic. A compact data-driven header keeps the context relevant to the current conversation visible, such as role, usage, morale, contract, and existing promises. There is no relationship score, approval probability, recommended response, or persistent topic list beside the meeting.

Responses are directly clickable and continue the conversation without an extra Ask or Confirm button. Any response that creates a promise displays its exact tracked terms inside the response choice before selection, including the measurable requirement, review period, and valid exceptions. A promise to deliver a defined result is distinguished from a commitment merely to review, negotiate, or pursue an opportunity. Declining to promise remains an explicit valid response.

Accepted promises persist on the player record with their evidence, deadline, and status. Changed circumstances are handled through a later conversation that shows the original terms and proposed revision; only player acceptance changes the tracked agreement.
