# Trade Center Concept

## Navigation

`Trade Center` is an expandable primary-navigation group:

`Trades | Trade Block | Free Agency | Waivers | League Transactions`

On trade-deadline day, Trade Center opens a temporary deadline workspace within this navigation rather than adding a permanent category.

## Trade Deadline Day

The deadline workspace uses a user-controlled hourly clock. Its header keeps the saved date, current time, cutoff time, and hours remaining visible with one **Advance 1 Hour** action. Time does not pass while the GM browses or negotiates, and advancement cannot pass an unresolved mandatory decision or execute an unapproved trade.

The main view combines three pieces of deadline evidence: a sortable Active Negotiations table, a compact Action Required queue for counters and replies, and a read-only League Trade Activity register. Active Negotiations begins with the partner and uses a plain **Offer** column containing **Received** or **Sent**; do not add a separate direction-arrow column. Opening a negotiation returns to the normal two-team trade builder with its full asset and validation context. The own-team context strip may show record, cap space, open roster spots, and the team's established plan; it must not expose hidden CPU evaluations, trade values, fairness grades, or acceptance odds.

Advancing an hour processes negotiations, responses, and league activity within that interval, then returns control with the updated time and queues. The deadline cutoff is enforced by the simulation even if talks began earlier. Ironwood Thunder and all displayed clubs are reference data only; production identity and deadline state are dynamic.

## Trades: Build a Trade

Trades is a two-team trade-builder workspace with a live negotiation area. Existing negotiations reopen here from their inbox item, Trade Block response, or a compact saved/live negotiation selector within the Trades workspace; they do not require a separate navigation screen.

### Left: User’s team

- The user’s team identity and relevant cap information.
- A list of available trade assets, including supported player and draft-pick assets.

### Right: Trade partner

- A dropdown selects the other team.
- Once selected, that team’s identity, relevant cap information, and available trade assets appear in the same structure as the user’s side.

### Center: Negotiation

- A live negotiation area shows the opposing GM’s current thoughts, comments, and reaction to the proposed offer.
- The feedback should make the AI’s perspective understandable without exposing hidden values or turning negotiation into a black box.
- As assets are added or removed, the negotiation area updates to reflect the current offer.
- Each team has eight visible proposal slots, arranged compactly so mixed player-and-pick packages remain readable without hiding either asset pool. Empty slots remain visible until filled, and occupied slots can be removed individually.

The layout keeps both sides of the transaction visible at once, while the center gives the trade a human front-office feel rather than reducing it to a silent asset exchange.

## Trade Block

Trade Block is the user franchise’s market-request workspace and fully absorbs the former Trade Finder workflow.

- The user selects up to eight owned players and draft picks to shop as one package.
- The user may optionally describe the desired return through filters such as asset type, position, role, playstyle, age range, contract preference, draft round, and priority.
- With no return filters, the department requests the best offers available for the selected outgoing assets.
- **Shop Selected Assets** submits the completed request to the market. Before submission, no teams, offers, proposed returns, or results placeholders are shown.
- After submission and processing, Trade Block changes to a distinct results state. Only then do interested opposing general managers return concrete packages.
- Returned offers identify the responding team and proposed return without exposing a trade value, fairness grade, acceptance probability, or hidden evaluation. Selecting a response shows both sides plus concrete cap and roster-space effects and can open Build a Trade for adjustment or negotiation.

The pre-submission state uses two working columns. **Select Assets to Shop** is the searchable Players/Draft Picks asset pool. The other column combines the visible eight-slot **Outgoing Package**, optional desired-return filters, and **Shop Selected Assets**. The post-submission results state is separate rather than appearing beside the unfinished request. Reviewing a response opens it directly in Trades, and unresolved negotiations remain reachable through inbox items and the Trades workspace's compact saved/live selector.

## Free Agency

Free Agency is a large scrollable list of available free agents and their relevant information.

- Filter controls narrow the pool to the players the user wants to evaluate.
- The list uses a compact, information-rich table treatment appropriate for comparison and search.
- Selecting a player opens their profile and relevant free-agency context.
- When the user chooses to make an offer, the game opens a dedicated contract-negotiation screen for that player.

The list is the discovery/evaluation workspace; contract terms and back-and-forth negotiation happen in the separate negotiation flow.

The default presentation is one full-width deep sortable table with compact search, position, age, overall, expanded-filter, unit, and sort controls. Rows show player, position, age, overall, playstyle, experience, last team, projected depth-chart role, scheme fit, known competing interest, and contract ask. There is no permanent selected-player rail. Right-clicking a row opens **Open Profile**, **Compare Player**, **Add to Shortlist**, and **Make Offer**; Make Offer enters the separate negotiation workflow.

Practice-squad-eligible free agents remain discoverable through ordinary Free Agency filters. Right-clicking any free agent exposes one **Make Offer** action. The negotiation screen contains the contract-type choice—such as Active Roster or Practice Squad—rather than duplicating contract types in the context menu. Players still respond through the existing agent and confirmation flow.

### Contract negotiation

The dedicated contract-negotiation screen is a direct, explainable discussion with the player's persistent agent. Its default state uses a calm two-column layout: compact player-and-agent context with the latest response and disclosed priorities on the left, and the offer builder on the right. Rival teams' exact bids remain private.

The default **Offer Summary** exposes only the decision-driving fields: term, total value, guarantees, signing bonus, expected role, headline totals, a compact annual cash/cap/guarantee summary, and team cap validation. Clickable subheaders open **Year-by-Year**, **Advanced Terms**, and **Offer History**. Detailed base salary, proration, roster and workout bonuses, incentives, dead-money consequences, and revision history belong in those views instead of competing for attention simultaneously.

The offer builder begins with a **Contract Type** field. Its valid choices depend on the player's eligibility and transaction context; for an eligible free agent this can distinguish Active Roster from Practice Squad terms without creating separate Make Offer actions.

The agent's current response explains disclosed priorities in plain language, while talks state and reply deadline remain visible. There is no acceptance probability, satisfaction meter, hidden threshold, exact rival offer, or permanently exposed history panel. **Withdraw Offer**, **Save Draft**, and **Submit Revised Offer** are explicit actions; revisions supersede earlier terms but do not erase their history.

### Accepted agreement confirmation

Agent acceptance does not execute the signing automatically. It opens a restrained final review with the agreed terms locked, an exclusive-confirmation deadline, and current cap, cash, and roster validation. The default **Contract Summary** remains concise; **Year-by-Year**, **Advanced Terms**, and **Offer History** stay available through the same progressive-disclosure subheaders used during negotiation.

The review distinguishes an accepted agreement from a completed transaction. **Confirm Signing** revalidates and executes the contract, adds the player to the roster, and files the signing once. **Withdraw Agreement** releases the exclusive agreement without silently modifying its terms. The screen avoids celebration art, probabilities, editable fields, and duplicate deadlines; it presents only the facts needed for the final decision.

## Waivers

Waivers uses an OOTP-style full-width utility table rather than a team-grouped list or card layout. Compact category subheaders cover All Players, Offense, Defense, Special Teams, and Expiring Soon, followed by search, position, waived-by team, deadline, expanded-filter, and time-left sort controls.

### Winning-claim confirmation

A winning waiver claim pauses league processing and arrives as a mandatory Action Required email from the League Office. The natural, concise message sits above structured claim detail identifying the awarded player and inherited contract, then compares current roster, cap, and position counts against the result if finalized. No acquisition has occurred at this stage. A separate modal over the Waivers screen is not used.

If the original claim included a conditional release, that player and the release's cap savings and dead money appear in a separate labeled section. The release executes only if the GM finalizes the winning claim; cancelling leaves the current player under contract and passes the award to the next eligible claimant. The acquisition and conditional release are revalidated and committed together rather than as two independent transactions. There is no timer, automatic acceptance, recommendation, probability, or scheme-fit summary.

The sortable table shows player, position, age, overall, playstyle, waiving team, inherited contract, cap hit, remaining years, claim deadline, and time left. The header keeps the total player count, the controlled team's current claim priority, cap space, and open roster spots visible together. Other clubs' sealed claims remain hidden.

Selecting a row opens only a shallow action strip attached to the table, showing the selected player, contract if awarded, projected role, scheme fit, **View Profile**, and **Submit Claim**. Submit Claim enters the existing confirmation workflow; there is no permanent side rail, oversized portrait, probability, or hidden-priority display.

## League Transactions

League Transactions is a chronological, filterable league-wide transaction log.

- It records signings, releases, waiver claims, trades, retirements, contract events, and other supported player-movement activity.
- Filters narrow the log by transaction type, team, player, or other useful criteria.
- Entries link to the involved team and player, and to deeper transaction context when available.

The OOTP-style presentation is framed as the **Official Transaction Register**, with compact league-office bulletin metadata, category subheaders for All, Trades, Signings, Releases & Waivers, Contracts, and Retirements, and one shallow filter toolbar. The register is grouped under formal full-date headings and uses only **Filed**, **Notice**, and **Official Transaction** columns. Each entry is a complete formal sentence, with team and player names acting as restrained text links inside it.

There are no row logos, portraits, transaction icons, glowing status colors, isolated team/player buckets, or permanent action buttons. Quiet record counts and pagination preserve the archive function. The register is read-only and should feel like an authoritative league-office filing rather than news, social media, a colorful activity feed, or an accounting ledger.
