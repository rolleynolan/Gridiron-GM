# Gridiron GM 1.0 Blueprint

## Vision

Gridiron GM is a single-player American-football franchise simulation. At 1.0, a user can take control of a team, make meaningful football and business decisions, simulate seasons, and build a persistent multi-year league.

Godot and C# are the complete runtime. Simulation, rules, saves, and interface live in one application. Python files in this repository are historical reference only and are never part of the shipped game.

## Player journey

1. Start or load a franchise and choose a team.
2. Use the dashboard, roster, depth chart, finances, schedule, inbox, and league views to make decisions.
3. Advance through preseason, regular season, and playoffs while managing games, injuries, and roster needs.
4. Complete the offseason: development, retirements, contracts, free agency, scouting, draft, and roster construction.
5. Repeat across seasons with complete history, records, and consequences.

## GM identity and franchise starts

Players create and maintain a reusable GM profile outside individual franchise saves. The profile records the GM's
name, selected management attributes, and character-design choices. Starting a new franchise selects an existing
profile or creates one; the profile is copied into the franchise as that save's GM record, so later changes to the
reusable profile do not rewrite an existing career.

GM attributes add modest, Football-Manager-style management advantages without becoming a path to guaranteed success.
The initial attributes are **Negotiation** (small contract-offer and counteroffer advantages), **Player Management**
(small retention and happiness advantages), **Scouting Judgment** (clearer player and prospect information), and
**Leadership** (small staff and team-culture advantages). Ratings, contracts, player personalities, team situation,
cap space, and gameplay results remain the primary drivers. Attributes never alter a player's underlying talent or
directly determine game results. Every modifier is capped, shown when relevant, and designed so a low-rated GM can
still succeed through sound decisions while a high-rated GM can still fail through poor decisions.

Every new franchise must choose a roster source before team selection:

- **Standard roster:** a fixed, versioned world-generation seed shipped with the game. The deterministic world
  generator creates the same fictional pro-player, coach, and college-player database, including ratings, contracts,
  histories, and draft classes, for every new standard-roster franchise. Each save receives its own generated copy.
  Two saves started with the same standard seed and generator version are identical at kickoff. Their state diverges
  only through decisions and simulation after the save begins; any newly introduced players are generated for that
  save.
- **Generated roster:** a new world seed creates the full fictional pro-player, college-player, and coach database when the save is
  started. Each generated franchise stores its seed and generated data, and is expected to differ from other generated
  franchises while remaining reproducible from its own saved state.

The save metadata records GM-profile identity, roster source, standard-world seed, world-generator version, and world
seed. The UI explains the choice and never modifies the Standard Roster definition during play.

## Architecture

- **Domain:** deterministic C# models and services own the league, teams, players, rules, schedules, games, and transactions.
- **Presentation:** Godot screens render state and request actions; they never duplicate rules or directly mutate domain state.
- **Persistence:** versioned JSON-safe saves preserve complete league, GM-profile snapshot, roster-source metadata,
  standard-world seed, world-generator version, and random-seed state, with migration for supported old saves.
  Reusable GM profiles and the immutable Standard Roster definition are persisted separately from franchise saves.
- **Events:** typed events such as `GameEnded`, `WeekAdvanced`, `SeasonEnded`, `PlayerInjured`, `ContractSigned`, and `DraftPickMade` connect systems without hidden dependencies.
- **Validation:** rules approve every mutation before it commits. No UI or AI system bypasses cap, roster, eligibility, or phase rules.

## 1.0 systems

### League, calendar, simulation, and history

The league owns all teams, pools, standings, schedule, current phase, results, settings, transactions, and historical archive. The calendar supports preseason, regular season, playoffs, offseason, draft, free agency, and preseason handoff. Game simulation resolves games from ratings, lineups, schemes, fatigue, injuries, home field, and controlled randomness, producing final scores, box scores, player statistics, and key events.

Schedules enforce valid weekly matchups and byes. Standings use only regular-season results, apply tiebreakers, seed playoffs, and advance a bracket to one champion. History records careers, team seasons, awards, records, playoffs, championships, transactions, and retirements without changing current gameplay state.

### Roster, game-day, injuries, and development

Teams own active rosters, injured reserve, practice squad, depth chart, and position requirements. Availability, fatigue, and substitutions affect game-day lineups. Injuries carry status, recovery timing, availability, history, and roster decisions.

Players have ratings, position skills, age, potential, traits, development profile, career statistics, and progression history. Development runs only at explicit weekly or seasonal lifecycle events, respects caps and injuries, and includes aging, regression, breakouts, stagnation, and retirement.

The league database includes fictional pro players, coaches, and college players/prospects at franchise creation.
Standard rosters recreate this population from the fixed standard seed and generator version; generated rosters create
all three populations from the new franchise's world seed.

### Contracts, transactions, and free agency

Contracts include years, annual salary, guarantees, bonuses, and type. A rules service calculates payroll and cap space, validates roster limits, eligibility, contracts, and phase restrictions, and records every signing, release, waiver, IR move, and trade. Free agents can receive offers and sign when valid; waivers and practice squads provide appropriate roster-management choices.

GM Negotiation may make a valid offer modestly more attractive or improve counteroffer terms, and Player Management
may modestly affect a player's willingness to stay. These are capped influences inside the contract and happiness
rules, not automatic outcomes or exceptions to salary-cap and roster rules.

### Draft, scouting, trades, and AI front offices

Each offseason has prospects, draft order, picks, user draft board, selections, rookie contracts, undrafted free agents, and draft history. Scouting provides estimated ratings, confidence, reports, traits, interviews, and public combine/pro-day data; it does not reveal hidden truth automatically.

Trades exchange valid players and picks only after rules validation. AI front offices evaluate needs, age, cap, contracts, picks, strategy, and risk, then propose actions with clear rationale. AI never silently changes a user-controlled roster.

### Staff, preseason, and immersion

Staff influence scouting, development, injuries, and strategy. Training camp and preseason create position battles, training reports, roster cuts, and development opportunities. Traits, morale, chemistry, fan pressure, and media add understandable, modest context without overpowering talent or rules.

### User interface

Godot includes GM-profile management, character design, main menu/franchise setup with roster-source selection,
saves/settings, a dashboard, team screens (roster, depth chart, injuries, cap, staff, scouting, practice squad, picks),
league screens (standings, schedules, leaders, results, transactions, history), franchise screens (free agency, trades,
scouting, draft), and game screens (quick sim, drive summary, box score, game log). The UI always explains blocked
actions and handles missing data safely.

## 1.0 completion standard

1.0 is complete when a fresh user can start a franchise, manage a legal roster and cap, simulate a season, complete playoffs, progress players, complete an offseason with draft and free agency, start the next season, and browse persistent history—all without manual file editing or developer intervention.
