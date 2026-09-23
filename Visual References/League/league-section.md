# League Section Concept

## Navigation

`League` is an expandable primary-navigation group:

`Standings | Stats | Schedule & Results | News | Player Search | History | Awards`

## Standings

League Standings is the complete league-wide reference screen. It shows the full standings and the current playoff picture. The presentation is dense, clear, and table-first; it does not need additional dashboard complexity.

Tiebreaker explanations are contextual rather than permanently occupying the bottom of the screen. A tied placement's info
control opens the ordered criteria and exact deciding reason in a compact popover or detail view; the Tiebreaker Rules action
opens the complete rules. The default screen uses that space for larger tables and useful division/conference records.

## Stats

League Stats uses a two-level selector above an OOTP-style grid of compact leaderboards.

- The primary header switches between **Individual Stats** and **Team Stats**. Only one mode is shown at a time.
- Three clickable subheaders directly beneath the primary header switch the complete grid among **Offense**, **Defense**, and **Special Teams**. The selected group uses the active accent; the other two remain visibly inactive but clickable. Do not use a dropdown for this navigation.
- The selected group fills the workspace with a dense, even grid of small category leaderboards. Each panel shows its category, the leading player or team with the leading value, and a compact ranked list of the next leaders.
- The Individual Stats mode uses player leaderboards; Team Stats swaps the entire grid to team-level statistical categories.
- Statistics always reflect the current season through the latest completed games. Do not include season, through-week, or minimum-qualifier controls.
- Conference filtering remains available where relevant, along with limited layout customization.
- A **View Sortable Stats** action opens the full-width, category-specific table for deeper comparison, sorting, column configuration, and export.
- Keep the leader panels compact and number-focused. Portraits and team marks are small identifiers; the ranked data remains dominant.

## Schedule & Results

Schedule & Results is a week-driven league schedule view.

- A Week selector controls the main display.
- The main workspace is one uninterrupted, full-width chronological list with one matchup per row. Do not split games into side-by-side columns or separate day sections.
- Each row reads as `Away Team score @ Home Team score`. Completed games end with **Final**; unplayed games omit scores and end with the weekday, date, and kickoff time.
- Team marks and entering records remain compact supporting identifiers. Winning scores may receive restrained emphasis.
- Selecting a completed row opens its box score; selecting an upcoming row opens its matchup preview. Do not keep a persistent game-detail panel beneath the list.
- The page supports browsing weeks directly rather than forcing a full-season schedule into one long list.
- A separate **Playoff Tree** tab shows the postseason bracket and its progression.

## News

League News is styled like an ESPN or NFL Network front page: a visually compelling editorial tile layout that covers the workspace with current league stories.

- Story tiles use strong headlines, relevant photos/imagery, and a concise contextual summary of what the article covers.
- All editorial scenes, player imagery, uniforms, helmets, and team marks use the project's cohesive high-detail 32-bit pixel-art treatment. Do not mix photorealistic sports photography into the feed.
- The top story earns the most visual weight; supporting stories form a dense, attractive grid around it.
- Trade rumors, player narratives, transactions, game results, injuries, and other current league events all belong in the feed.
- Selecting a story opens the full article and routes naturally to its associated player, team, game, transaction, or draft context.

The full article reader uses the entire available workspace without a contextual rail. Headline, subheadline, persistent outlet/reporter attribution, publication time, one restrained 32-bit story image when relevant, and readable body copy form the article. Relevant players, teams, games, transactions, and earlier coverage appear as restrained hyperlinks directly in the prose. A compact factual strip may link to the underlying box score or event record. Previous Story, Next Story, and Back to News preserve browsing without social-media controls or engagement metrics.

Do not add a separate News Archive screen. Search, category, team, and any retained-season browsing belong inside the existing editorial News screen. League Transactions remains the dedicated chronological register for completed player-movement and contract events.

News retains only the configured recent-media window. When an article expires, any related-story link must route to the preserved underlying game, player, team, transaction, award, milestone, or historical record rather than becoming broken. The exact retention window remains pending review.

This is intentionally flashier than the standard management screens and should grab the player’s attention, while remaining an informative in-game news source rather than empty decoration.

## Player Search

Player Search is the league-wide active-player database.

- A paged, scrollable list presents every active player in the league.
- FM-style configurable headers support sorting and preferred columns for player comparison.
- A search bar finds players by name.
- Filters narrow the database by supported player traits and other useful criteria.
- Selecting a player opens the full player profile.

The screen prioritizes fast discovery and comparison over visual presentation.

## History

League History opens as an encyclopedia-style subject directory rather than a selected-season dashboard or season-by-season table.

- The landing page is one uninterrupted list containing only six broad clickable destinations: **Championship History**, **Award Winners**, **League Records**, **Historical Standings**, **Statistical Leaders**, and **Draft History**.
- Do not expose child categories on the landing screen. Details such as championship games, playoff results, individual awards, career and single-season records, divisions, statistical categories, and draft classes appear only after opening the relevant broad destination.
- Every link opens a complete chronological database for that subject. Year, team, player, award, statistic, and other relevant filters belong inside the destination page rather than crowding the directory.
- Search helps locate a historical database directly. Recently viewed links may provide quick return paths without becoming a dashboard.
- The detailed season encyclopedia remains available through contextual links inside Championship History, Historical Standings, award results, and the other chronological databases.
- Do not organize history into eras, decade timelines, nostalgia panels, oversized visual cards, or a default season catalog.

The directory should feel like the high-level contents page of a living football encyclopedia, with every result generated from persisted league records.

## Hall of Fame

Hall of Fame is an official encyclopedia and committee-results archive; the GM does not cast votes. Three broad subheaders cover Members, Class Results, and Eligible Candidates. Class and category filters expose the preserved history without dividing players into deprecated era groupings.

Class Results uses one dense finalist table with candidate, position, career span, first-eligible year, ballot year, career production, major honors, official vote result, and election result. A slim process line identifies the rules version, voting membership, threshold, and final status for the selected class. Selecting a row opens a compact record below the table with a small 32-bit portrait, career statistics or role-appropriate achievements, a brief history-derived summary, and links to the complete career and voting record.

The screen never asks the GM to vote and does not simulate a live reveal. There are no projections, recommendations, candidate scores, tiers, ceremonies, era labels, or automatic finalist carryover. Coaches and contributors use the same archive with category-appropriate accomplishments rather than player-stat columns.

## Awards

Awards is a tile-based current-season awards-race screen.

- Each award receives a tile showing the current projected leading candidates/runners.
- Projections do not begin at season kickoff; the screen becomes active around the midseason point (target: approximately Week 9) once there is enough season context to make the race meaningful.
- Before projections begin, the screen states the current week and first projection week rather than inventing early rankings. The workspace shows an official table of the previous season's recipients, results, and history links so the screen remains useful without implying a current race.
- Award tiles can link to the candidate players and their supporting current-season context.
