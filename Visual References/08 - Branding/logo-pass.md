# 32-Team Logo Pass

The production franchise list is locked to `Godot/Assets/data_seed/teams.json`. Mockup-only clubs, including Ironwood Thunder, are not members of the production league. The older fallback team list in `LeagueBootstrapService.cs` is not a branding source.

Logos are reviewed four at a time. Pilot images remain visual-review candidates until accepted; they do not replace the live files in `Godot/Assets/team_logos`.

## Shared direction under review

- Apply two mandatory first-look tests before approving any mark: **would it look good on a football helmet**, and **does it use the visual language of a credible professional-team logo** without copying a real identity?
- Design the helmet-ready sports mark first and translate it into the game's 32-bit treatment second. Pixel styling cannot rescue an icon, illustration, or badge that lacks a professional sports silhouette.
- Symbol-only transparent emblems with no city or team wording.
- Authentic late-1990s 32-bit sports pixel art built on a small logical grid.
- Hard square pixels, stepped curves and diagonals, deliberate clusters, and flat color regions.
- Compact silhouettes that remain recognizable at 32 pixels.
- Use one dominant idea, broad shapes, minimal interior detail, and a clear directional profile where appropriate.
- A maximum of four team colors plus transparency.
- No glow, blur, gradients, antialiasing, bevels, shadows, glossy 3D, generic shields, footballs, or resemblance to real sports marks.
- The set may mix abstract, creature, object, and regional symbols if the same construction and complexity rules make it feel like one league.

## Review batches

1. Chicago Cyclones, Miami Neon, Detroit Forge, Portland Stags — all four identities locked; Chicago v2, Forge v2, and Stags v2 approved; Neon v2 awaits logo review.
2. Dallas Outlaws, New York Empire, San Francisco Surge, Tennessee Copperheads — batch approved.
3. Las Vegas Spades, Atlanta Flight, Boston Redcoats, Houston Stampede — batch approved.
4. Los Angeles Paladins, Philadelphia Liberty, Phoenix Inferno, Seattle Orcas — batch approved.
5. Denver Bighorns, Cleveland Vanguards, Orlando Stingrays, Minnesota Mammoths.
6. Baltimore Knights, New Orleans Specters, Indianapolis Racers, Cincinnati Sabers.
7. Kansas City Kings, Charlotte Stingers, Tampa Bay Sharks, Pittsburgh Ironmen.
8. Washington Generals, Green Bay Lumberjacks, Buffalo Blizzard, San Diego Armada.

## Batch 1 candidates

- `chi-cyclones-logo-pilot-v2.png` — approved; broad forward-sweeping cyclone/C silhouette and the current batch quality benchmark.
- `mia-neon-logo-pilot-v2.png` — hard-pixel M monogram in hot pink, electric cyan, deep violet, and near-black Miami-night colors.
- `det-forge-logo-pilot-v2.png` — approved identity and logo direction; preserves the forged D/flame core while adding a restrained anvil-like top profile and fuller flame.
- `por-stags-logo-pilot-v2.png` — approved identity and logo direction; compact front-facing stag built with strict hard-grid pixel clusters.

All earlier files remain only as rejected comparisons except approved Chicago v2.

## Batch 2 candidates

- `dal-outlaws-logo-pilot-v1.png` — approved; masked side-profile outlaw with a broad-brim hat, built as a lateral helmet mark in charcoal, crimson, sand, and brass.
- `ny-empire-logo-pilot-v6.png` — approved; front-facing crowned lion with unmistakable feline anatomy, a compact symmetrical mane, and a charcoal, imperial-purple, antique-gold, and ivory palette. It replaces the rejected side-profile iterations.
- `sf-surge-logo-pilot-v5.png` — approved; three thick interwoven charge bands collectively form one forward-moving electrical S rather than another single-bolt composition.
- `ten-rivermen-logo-pilot-v1.png` — rejected identity and logo direction; retained only as an audit artifact.

Dallas, New York, San Francisco, and Tennessee are approved. Tennessee Copperheads replaces Tennessee Renegades. Las Vegas Vipers now requires a replacement identity. Review files do not replace live runtime logos or team data unless approved.

## Identity-change shortlist

These are creative candidates, not approved renames and not legal/trademark clearance.

- **Detroit Forge** is approved and replaces Detroit Mechanics.
- **Portland Stags** is approved and replaces Portland Pioneers.
- **Miami Neon** is approved as the replacement identity for Miami Tritons. Tritons remains only in rejected audit artifacts.
- **Tennessee Copperheads** is approved and replaces Tennessee Renegades. **Tennessee Hounds** is rejected because its orange-and-T direction felt too close to University of Tennessee Knoxville branding. **Tennessee Rivermen**, **Tennessee Tuners**, and **Tennessee Pioneers** are also rejected.
- Current Tennessee pitch shortlist: **Hellbenders** (distinct Appalachian wildlife identity), **Copperheads** (strongest conventional pro-sports identity but overlaps the league's Vipers snake category), **Sound** (modern music/current identity), **Railmen** (regional transport and industry), and **Mockingbirds** (state-bird identity with a clean wing mark).
- The first Tennessee pitch shortlist was rejected. Second-round major-league-style candidates: **Cavalry**, **Sentinels**, **Marauders**, **Longhunters**, **Triad**, and **Redtails**.
- **Tennessee Copperheads** is selected. **Las Vegas Vipers** now requires a new identity so the 32-team set does not carry two snake brands.
- **Las Vegas Spades** is approved and replaces both Las Vegas Vipers and the short-lived Scorpions working identity. Both canonical team lists are updated. Jacks and Scorpions remain rejected audit directions.

## Tennessee finalist logo candidates

- `ten-tuners-logo-pilot-v2.png` — music-first revision; two connected beamed notes, a forward-sweeping shared beam, and one performance waveform in midnight navy, turquoise, copper, and cream. The tuning-fork v1 is rejected.
- `ten-pioneers-logo-pilot-v2.png` — literal Tennessee-frontier revision; a determined scout profile with a striped coonskin cap and swept tail in charcoal, forest green, ochre, and cream. The abstract compass/P v1 is rejected.
- `ten-copperheads-logo-pilot-v1.png` — approved; broad triangular copperhead emerging from a connected C-shaped patterned coil in charcoal, copper, pine green, and cream.

Tennessee Copperheads is approved. Tuners and Pioneers remain rejected comparison artifacts.

## Batch 3 candidates

- `lv-spades-logo-pilot-v2.png` — approved; black-dominant Art Deco spade with gold structure, crimson facets, and a compact interlocked gold LV monogram.
- `atl-flight-logo-pilot-v2.png` — accepted direction; replaces the rejected abstract wing/A with the small red vintage propeller plane from the original Flight identity, isolated as a helmet-ready symbol without its old wordmark or background.
- `bos-redcoats-logo-pilot-v3.png` — approved direction; replaces the rejected coat-only emblem with a compact three-quarter redcoat soldier in a tricorn, crimson uniform, cream cross-belt, and gold button.
- `hou-stampede-logo-pilot-v4.png` — approved direction; removes the body and shoulders from v3, leaving only the lowered front-facing bull head, compact jaw, and wide horns in charcoal, oxblood, sand, and cream.

Las Vegas Spades v2, Atlanta Flight v2, Boston Redcoats v3, and Houston Stampede v4 are approved. Both canonical team lists use Las Vegas Spades. Earlier Batch 3 files remain rejected comparison artifacts.

The four approved Batch 3 concepts now supply the live `LV.png`, `ATL.png`, `BOS.png`, and `HOU.png` runtime assets. Each previous runtime file is preserved in this audit folder with a `runtime-pre-...` filename.

## Batch 4 candidates

- `la-paladins-logo-pilot-v2.png` — approved direction; preserves the right-facing closed helmet and swept crest while replacing the overused gold with a purple, pale-silver, cool-white, and charcoal uniform palette.
- `phi-liberty-logo-pilot-v4.png` — radically simplified upright Liberty Bell using only a small top loop, broad body, heavy lip, visible clapper, and one bold forked crack in copper, verdigris, navy, and mint.
- `phx-inferno-logo-pilot-v3.png` — selected long-beak firebird draft; the more aggressive hooked profile is preferred over the subsequent short-beak cleanup.
- `sea-eclipse-logo-pilot-v1.png` — rejected along with the Eclipse identity; retained only as an audit artifact while a new Seattle name is selected.
- `sea-orcas-logo-pilot-v2.png` — approved Seattle replacement identity; full-body arcing orca in charcoal, white, Pacific teal, and ice blue. This is the user's selected second Orca generation.

Los Angeles v2, Philadelphia v4, Phoenix v3, and Seattle Orcas v2 are approved. All four supply the live `LA.png`, `PHI.png`, `PHX.png`, and `SEA.png` runtime assets. Previous runtime logos are preserved in this audit folder. Both canonical team lists use Seattle Orcas.

## Batch 5 candidates

- `den-bighorns-logo-pilot-v1.png` — approved; right-facing three-quarter bighorn head dominated by one massive curled horn in charcoal, alpine blue, rust brown, and pale bone.
- `cle-vanguards-logo-pilot-v1.png` — rejected along with the Vanguards identity; retained only as an audit artifact while Cleveland is renamed.
- `cle-rhinos-logo-pilot-v1.png` — approved; aggressive right-facing rhino head with a dominant forward horn in charcoal, steel silver, scarlet, and white.
- `orl-stingrays-logo-pilot-v1.png` — approved; forward-right stingray with connected swept tail, broad teal wings, aqua planes, navy structure, and small coral eye accents.
- `min-mammoths-logo-pilot-v1.png` — approved; broad three-quarter woolly mammoth head with an upward-curling trunk and two ivory tusks in navy, slate blue, ice blue, and ivory.

Denver, Orlando, Minnesota, and Cleveland Rhinos are approved and now supply `DEN.png`, `ORL.png`, `MIN.png`, and `CLE.png`; previous runtime files are preserved in this audit folder. Cleveland Vanguards is rejected.

Cleveland Rhinos is approved. The v1 mark supplies the live `CLE.png`, and both canonical team lists now use the Rhinos identity.

### Cleveland replacement shortlist

- **Cleveland Rockers** — Rock & Roll Hall of Fame connection; compact angular soundwave or guitar-pick mark.
- **Cleveland Sentinels** — disciplined defensive identity; watchtower, beacon, or abstract vigilant-eye mark rather than another knight.
- **Cleveland Lakehawks** — Lake Erie regional identity; fast lateral raptor mark distinct from the league's other birds.
- **Cleveland Rhinos** — conventional major-league power identity; heavy side-profile horn-and-head mark.
- **Cleveland Riffs** — more original music identity than Rockers; interlocking rhythmic bars or a forward C-shaped waveform.

## Batch 6 candidates

- `bal-knights-logo-pilot-v1.png` — rejected; the generic armored helmet was too weak and overlapped too heavily with the Los Angeles Paladins.
- `bal-knights-logo-pilot-v2.png` — active revision; Baltimore retains Knights and now owns the intimidating helmet direction, using a low steel great helm with a thin crimson T-visor and short rear ridges.
- `bal-knights-logo-pilot-v3.png` — active crusader revision; a flat-topped medieval great helm with a cross-shaped crimson visor, squared cheek plates, and torn rear cloth in navy, steel, crimson, and silver.
- `bal-knights-logo-pilot-v4.png` — active reference-informed revision; an original centered crusader bust with narrow great helm, crimson crest, angular shoulders, and two short crossed swords behind it. It follows the broad arrangement of the user's visual reference without copying its exact artwork.
- `bal-knights-logo-pilot-v5.png` — active sketch-driven revision; the user's rounded helmet geometry now dominates the centered bust, including the sharp peaked brow, central nasal slit, vented lower faceplate, pointed chin, compact shoulders, and crossed swords.
- `bal-knights-logo-pilot-v6.png` — full reset and current candidate; strips away the bust, shoulders, and weapons for one centered spearhead-shaped helmet with a high crown point, severe V visor, narrow nasal blade, crimson inset planes, vented jaw, and long pointed chin.
- `bal-knights-logo-pilot-v7.png` — approved darker recolor of the v6 structure; preserves the exact helmet geometry while replacing most bright steel with blackened gunmetal and midnight navy, deepening the blood-red inset planes, and restricting silver to narrow edge highlights. It now supplies the live `BAL.png` asset.
- `nor-specters-logo-pilot-v1.png` — approved; hooded spectral profile in deep plum, violet, bone, and chartreuse, with a single eye and two broad trailing vapor hooks.
- `ind-racers-logo-pilot-v1.png` — approved; horizontal motorsport mark combining one perspective racing wheel with three integrated speed vanes in navy, racing blue, orange, and white.
- `cin-sabers-logo-pilot-v1.png` — approved; right-facing saber-toothed cat head in espresso, burnt orange, cream, and turquoise, defined by two long downward fangs.

New Orleans, Indianapolis, and Cincinnati are approved and now supply `NOR.png`, `IND.png`, and `CIN.png`; their previous runtime files are preserved in this audit folder. Baltimore retains the Knights identity, but its v1 mark is rejected and v2 remains under review.

### Knights and Paladins separation

- Baltimore retains the Knights name and takes ownership of the intimidating armored-helmet identity with `bal-knights-logo-pilot-v2.png`.
- Los Angeles retains the Paladins name but moves away from helmets with `la-paladins-logo-pilot-v3.png`, a purple guardian shield centered on a large white four-point beacon.
- Baltimore v3 and the Los Angeles identity both remain under review; neither live runtime logo has been replaced yet. The Paladins name may be retired to eliminate the remaining conceptual overlap.

### Los Angeles replacement shortlist

- **Los Angeles Aftershock** — unmistakably Californian; fractured seismic wave or offset fault-line mark.
- **Los Angeles Stars** — direct entertainment-city connection; sharp asymmetric star rather than another shield.
- **Los Angeles Condors** — regional, powerful, and visually distinct; broad black-wing or angular condor-head mark.
- **Los Angeles Legends** — Hollywood and sports prestige; bold monogram or spotlight-inspired mark, though less tangible as a mascot.
- **Los Angeles Guardians** — preserves the protective shield concept, but is more conventional than the other options.

`la-stars-logo-pilot-v1.png` is now the active Los Angeles identity trial: one asymmetric purple five-point star driven upward-right by broad coral and silver trails. Los Angeles remains Paladins in canonical data and the live `LA.png` is unchanged pending approval.

`la-stars-logo-pilot-v2.png` supersedes the overly plain v1 trial. It layers a large purple star, two supporting stars, stepped purple/silver marquee trails, and a coral Art Deco sunrise into a richer Hollywood sports identity. It remains under review; canonical data and `LA.png` are unchanged.

The Stars identity is now rejected. `la-condors-logo-pilot-v1.png` is the active Los Angeles replacement trial: a right-facing California condor with a bald coral head, massive pale hooked beak, and swept charcoal/purple neck feathers. Los Angeles remains Paladins in canonical data and `LA.png` remains unchanged pending approval.

`la-condors-logo-pilot-v1.png` is rejected. `la-condors-logo-pilot-v2.png` restarts the mark as a full California condor in a steep attacking glide, with enormous purple/charcoal wings, broad silver feather tiers, a compact head, and limited coral accents. It remains under review; Los Angeles is still Paladins in canonical data and `LA.png` is unchanged.

`la-condors-logo-pilot-v3.png` preserves the improved v2 full-bird geometry but replaces the overused purple with California-poppy orange, warm bone flight feathers, charcoal structure, and a small dark copper-red neck patch. It is approved, now supplies the live `LA.png`, and both canonical team lists use Los Angeles Condors.

## Batch 7 candidates

- `kc-kings-logo-pilot-v1.png` — low-angle armored crown with three broad points, an arched steel band, and one ice-blue central jewel in midnight blue, crimson, steel, and ice.
- `cha-stingers-logo-pilot-v1.png` — unmistakable right-flying hornet with antennae, mandibles, four visible wing planes, six tucked legs, banded abdomen, and one straight rear stinger in charcoal, yellow-orange, turquoise, and mint.
- `tb-sharks-logo-pilot-v1.png` — full shortfin mako driving right with a pointed snout, angular jaw, swept dorsal fin, gill cuts, and compact crescent tail in navy, marine blue, seafoam, and sand.
- `pit-ironmen-logo-pilot-v1.png` — upward-driving clenched fist constructed from interlocking graphite structural plates, safety-yellow edges, steel highlights, black channels, and two large rivets.

Review update:

- `kc-kings-logo-pilot-v1.png` is rejected. `kc-kings-logo-pilot-v2.png` is a full reset using a straight-on heavy royal-blue and steel crown, three broad upright points, and a large crimson central jewel.
- `cha-stingers-logo-pilot-v2.png` replaces the static side profile with a front-right airborne attack pose, four wings forming a broad X, tucked legs, visible mandibles, a banded abdomen, and a rear stinger.
- `tb-sharks-logo-pilot-v2.png` replaces the mako with a broad great-white head-and-body wedge, large triangular dorsal fin, white underside, three gill cuts, and a compact open jaw.
- `pit-ironmen-logo-pilot-v1.png` is approved and now supplies the live `PIT.png`; the previous runtime logo is preserved as `pit-runtime-pre-ironmen-v1.png`.
- `cha-stingers-logo-pilot-v2.png` is approved and now supplies the live `CHA.png`; the previous runtime logo is preserved as `cha-runtime-pre-stingers-v2.png`.
- `kc-kings-logo-pilot-v2.png` is rejected. `kc-kings-logo-pilot-v3.png` abandons crowns for a heavy chess-king piece in an entirely new forest-green, copper-orange, cream, and charcoal palette.
- The first banking great-white attempt was discarded for an overlapping silhouette. `tb-sharks-logo-pilot-v4.png` is the active revision: one continuous great white breaching diagonally from lower-left tail to upper-right head.
- `tb-sharks-logo-pilot-v4.png` is approved and now supplies the live `TB.png`; the previous runtime logo is preserved as `tb-runtime-pre-sharks-v4.png`.
- Kansas City Kings and all three Kings logo trials are rejected. Both canonical team lists now use **Kansas City Wolves**.
- `kc-wolves-logo-pilot-v1.png` is the active replacement trial: an aggressive right-facing wolf head in a navy-and-white-only family, with swept ears, heavy brow, angular cheek tufts, squared muzzle, and two visible fangs.
- `kc-wolves-logo-pilot-v2.png` supersedes v1 with a surgical mouth correction: the malformed lower projections are removed and exactly two clean upper canine fangs point downward inside the dark mouth. All other geometry and colors are preserved.
- `kc-wolves-logo-pilot-v3.png` supersedes v2 by correcting the underlying jaw problem: the oversized mouth cavity is closed, the lower muzzle reconnects beneath the upper muzzle, the mouth becomes a narrow snarl line, and only two short fang tips remain visible.
- `kc-wolves-logo-pilot-v4.png` replaces the paired fang treatment with one short near-side upper canine integrated into the closed lip; the far canine is hidden by the three-quarter perspective.
- `kc-wolves-logo-pilot-v5.png` supersedes the closed-mouth trials with a clearly open snarling jaw, curled upper lip, connected dark mouth cavity, and a restrained row of short teeth that read as part of the jaw rather than floating fangs.

Kansas City Wolves v5 is approved and now supplies the live `KC.png`; the previous runtime logo is preserved as `kc-runtime-pre-wolves-v5.png`. Tampa Bay v4, Charlotte v2, and Pittsburgh v1 are also approved and installed. Kansas City's canonical identity is Wolves.

## Batch 8 candidates — final batch

- `was-generals-logo-pilot-v1.png` — right-facing Revolutionary-era commanding general profile with a broad tricorne, officer star, stern simplified face, and trailing collar in navy, burgundy, cream, and antique brass.
- `gb-lumberjacks-logo-pilot-v1.png` — one oversized double-bit logging axe striking diagonally through a simplified evergreen in forest green, charcoal, cream, and burnt orange-brown.
- `buf-blizzard-logo-pilot-v1.png` — charging white bison with lowered head, curved horn, and rear body dissolving into broad ice-blue whiteout streaks.
- `sd-armada-logo-pilot-v1.png` — compact right-driving naval flagship with three wind-filled sails, strong navy hull, coral pennant, and one angular teal wave.

All four Batch 8 marks remain under review. The live `WAS.png`, `GB.png`, `BUF.png`, and `SD.png` assets are unchanged pending approval.

Batch 8 review update:

- `buf-blizzard-logo-pilot-v1.png` is approved and now supplies the live `BUF.png`; the previous runtime logo is preserved as `buf-runtime-pre-blizzard-v1.png`.
- Washington Generals and San Diego Armada are rejected as identities and will be renamed before further logo work. Their canonical names and live runtime logos remain unchanged until replacements are selected.
- `gb-lumberjacks-logo-pilot-v1.png` is rejected. `gb-lumberjacks-logo-pilot-v2.png` resets the mark to one rugged right-facing lumberjack head with a low knit cap, squared brow, blunt nose, and large angular burnt-orange beard.

### Washington replacement shortlist

- **Washington Wardens** — serious institutional identity with a vigilant eagle, key, or watchtower direction.
- **Washington Monuments** — unmistakably tied to the capital, with a severe obelisk or geometric landmark mark.
- **Washington Diplomats** — unique political identity, best expressed through a strong seal-inspired eagle or linked-column motif without becoming a government badge.
- **Washington Potomacs** — regional rather than political, with a river-current or predatory river-bird direction.
- **Washington Legion** — disciplined and forceful, though the logo must avoid overlapping with Baltimore's medieval military identity.

Washington **Wardens** is selected. Both canonical team lists now use the Wardens identity; the rejected Generals logo remains only as an audit artifact, and live `WAS.png` is unchanged until a Wardens mark is approved.

`was-wardens-logo-pilot-v1.png` is the approved Wardens mark: a compact right-facing vigilant bald-eagle head with a heavy lowered brow, sharply focused eye, broad hooked burgundy beak, and three swept neck-feather plates in federal navy, burgundy, silver, and white. It now supplies live `WAS.png`; the previous runtime mark is preserved as `was-runtime-pre-wardens-v1.png`.

### San Diego replacement shortlist

- **San Diego Tritons** — powerful coastal identity with a trident or stylized sea-warrior mark; Miami no longer uses the trident concept.
- **San Diego Breakers** — energetic surf identity built around one collapsing wave, distinct from Orlando Stingrays and Seattle Orcas.
- **San Diego Barracudas** — aggressive local-water predator with a narrow, fast silhouette distinct from Tampa Bay Sharks.
- **San Diego Harpoons** — sharp maritime identity centered on a single dynamic harpoon mark rather than a ship.
- **San Diego Toros** — strong border-region identity with a charging bull mark, but it risks overlap with Houston Stampede.

The initial San Diego shortlist remains unselected. A second, broader set of directions is under consideration: **Voyagers**, **Ospreys**, **Riptide**, **Coyotes**, **Cutters**, and **Sol**.

San Diego **Riptide** is selected. Both canonical team lists now use the Riptide identity. `sd-riptide-logo-pilot-v1.png` is approved: one large right-driving angular wave with three crest hooks and an opposing coral undertow shape inside the curl. It now supplies live `SD.png`; the previous runtime mark is preserved as `sd-runtime-pre-riptide-v1.png`, and the rejected Armada mark remains only as an audit artifact.

`gb-lumberjacks-logo-pilot-v3.png` preserves the v2 lumberjack design and adds a compact dark square-pixel pupil with a small cream eye highlight, giving the character a clear focused gaze. It is approved and now supplies live `GB.png`; the previous runtime mark is preserved as `gb-runtime-pre-lumberjacks-v3.png`.
