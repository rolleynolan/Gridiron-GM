# Finances Section Concept

## Navigation

`Finances` is an expandable primary-navigation group with three direct destinations:

`Team Finances | Contracts | Accounting`

## Team Finances

Team Finances follows the OOTP-style financial-page model: a dense, number-first franchise-business worksheet rather than a
visual dashboard or a bare list of ledger entries. It gives the GM the financial health, operating context, and controls needed
to run the team.

- Prominent current financial summaries, with a clear distinction between the most important available funds, payroll/cap context, and operating outcome supported by the game’s systems.
- An attendance and ticketing area: current attendance context, ticket-tier prices, and direct controls for setting those prices.
- Compact revenue/cost context that explains the current business picture without replacing Accounting’s authoritative year-to-date ledger.
- Facility operating costs, maintenance, owner funding, and project spending appear in the applicable financial categories.
- Financial rankings where useful, such as attendance rank and payroll rank, to place the franchise in league context.
- Clear routes into Contracts and Accounting for detailed cap planning and full revenue/expense review.

The page should help the player understand how business choices, fan turnout, and payroll relate without becoming the
transaction-level Accounting ledger. Its visual language is dominated by compact financial tables and aligned numeric
columns—budget, actual, variance, forecast, prior year, and league rank—with strong labels and cautious status color. Avoid
portraits, facility renderings, charts, large KPI cards, and oversized dashboard graphics. Owner funding, projects, ticketing,
and upcoming decisions appear as concise numeric tables with links to their authoritative screens.

## Contracts

Contracts follows the OOTP-style financial-table model: a neat, multi-year salary-cap sheet that displays every player currently under contract and makes upcoming commitments understandable at a glance.

- A compact list of all contracted players, sorted by current cap hit from highest to lowest by default.
- A year-by-year cap-hit column for each contracted player, showing several future seasons side by side.
- A clear available-cap-space summary for every displayed year.
- A clear dead-cap summary for every displayed year.
- Other essential contract/cap context as supported by the game’s rules.

The annual cap-space and dead-cap figures should be aligned with the same year columns as the player cap hits, so the user can understand each future season without switching views. It is a dense, sortable, information-first financial table, optimized for planning rather than presentation. The user should be able to compare future years and quickly identify the contracts driving the team’s cap situation.

### Contract restructure

From a roster or Contracts row, the nested right-click action **Contract > Restructure** sends only an inquiry asking whether the player is open to discussing a restructure. The player's yes-or-no answer arrives through Inbox after a few in-game days. A positive response unlocks the focused terms discussion; a refusal ends the inquiry without opening editable terms.

Once the player agrees to talk, the default **Restructure Summary** compares existing and proposed cap hits by season, shows immediate cap savings and the equal future cap added, and keeps total player cash visibly unchanged. **Year-by-Year**, **Guarantees & Dead Money**, and **History** provide progressive disclosure for deeper accounting. The screen explicitly states that restructuring changes when cap charges are counted; it does not erase the obligation or reduce agreed pay. Submitted terms receive a later response through Inbox. If accepted, the exact agreed terms apply once and update the contract, projections, and transaction history.

### Player release confirmation

Releasing a player follows the compact OOTP interaction model. The GM right-clicks a player from the roster or Contracts table and selects **Release**. A centered confirmation popup appears over the current screen instead of navigating to a dedicated release page.

The popup identifies the player and summarizes only the relevant financial consequences: current cap space, the player's cap hit, dead money created, cap space after release, guaranteed cash still owed, and future-year cap changes. Roster-space and depth-chart effects do not appear in this financial confirmation. **Cancel** closes the popup unchanged; **Release Player** executes the transaction. There are no tabs, negotiation steps, permission checks, or secondary confirmation screen.

### Fifth-year options

Fifth-year option decisions use a dedicated OOTP-arbitration-style worksheet under Contracts rather than individual popups. The top table lists every eligible player controlled by the franchise with draft position, current cap hit, option season, option pay, guarantee treatment, decision status, and deadline. This adapts OOTP's list-based contract-case presentation without importing team offers, player demands, hearings, awards, wins, or losses.

Selecting a row opens that player's compact financial worksheet below the list. It compares projected option-year cap space before and after exercise and offers mutually exclusive **Exercise Fifth-Year Option** and **Decline Fifth-Year Option** choices. Each decision is confirmed individually and persisted; declining does not end the player's current rookie contract.

### Tags and tenders

Tags and tenders use the same list-and-selected-case structure as Fifth-Year Options. One unified table contains every controlled player eligible for a franchise or transition tag, restricted-free-agent tender, or exclusive-rights tender. Columns show contract status, available control, projected cost, retained rights, decision status, and deadline without splitting the small case list into multiple category tabs.

Selecting a row changes the lower worksheet to only the filing choices valid for that player. For a tag-eligible unrestricted free agent, it compares the designation cost and projected cap-space effect and clearly states that filing a tender does not sign the player. Restricted and exclusive-rights cases expose their applicable tender levels and retained rights instead. The shared tag limit remains visible, and decisions are confirmed individually.

## Accounting

Accounting is the authoritative category-level and monthly financial record for the current financial year. It does not
include a transaction-by-transaction ledger.

- Revenue and expense categories remain clearly separated, with payroll through to operating costs such as travel, stadium expenses, and equipment costs represented in the ledger.
- Totals and net result are immediately visible alongside the detailed line items.
- A month-by-month results table shows operating revenue, operating expenses, operating result, owner funding, project/capital
  spending, net cash change, and ending cash.
- The screen supports drilling into supported category details and their originating game-system records when useful, without
  exposing an accounting-entry ledger or entry IDs.
- The game calculates directly trackable categories from recorded franchise data, including scouting expenses, all player/staff contracts, attendance revenue, and other in-game financial events.
- Operational figures that are impractical to track event by event—such as every travel cost, stadium operating expense, equipment cost, or merchandise sale—are modeled behind the scenes from realistic team-based averages and conditions. They appear as normal financial entries; the player is not shown an “estimated” label.

It is the authoritative detailed financial summary, while Team Finances remains the comparative operational overview and
Contracts remains the cap-planning workspace.
