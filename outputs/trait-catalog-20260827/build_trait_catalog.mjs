import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const outputDir = "C:\\Users\\norma\\Desktop\\GridironGM\\outputs\\trait-catalog-20260827";
const outputPath = `${outputDir}\\Gridiron_GM_Trait_Catalog.xlsx`;

const traits = [
  ["Mental & Personality", "Positive", "Leader", "Stabilizes team chemistry and morale when entrusted with a meaningful role.", "All players", "Practice observations, teammate feedback, captaincy behavior"],
  ["Mental & Personality", "Positive", "Mentor", "Helps relevant teammates learn and settle into roles.", "Veterans / established players", "Position-room reports, rookie development observations"],
  ["Mental & Personality", "Positive", "Work Ethic", "Improves training consistency and development readiness.", "All players", "Practice habits, staff development reports"],
  ["Mental & Personality", "Positive", "Competitive", "Improves resilience after poor performances and response in high-leverage moments.", "All players", "Game behavior, performance after setbacks"],
  ["Mental & Personality", "Positive", "Composed", "Reduces mental-error and performance volatility under pressure.", "All players", "Late-game film, coach observations"],
  ["Mental & Personality", "Positive", "Resilient", "Recovers morale more readily after injury, losing streaks, or role setbacks.", "All players", "Injury recovery, staff interactions, role changes"],
  ["Mental & Personality", "Positive", "Team First", "More accepting of depth-chart changes and team-friendly role or contract outcomes.", "All players", "Role discussions, negotiations, staff reports"],
  ["Mental & Personality", "Positive", "Loyal", "Places additional value on remaining with the current franchise.", "All players", "Contract talks, player communication"],
  ["Mental & Personality", "Neutral / Mixed", "Demanding", "Sets stronger expectations for role, compensation, and team success.", "All players", "Agent talks, role conversations, media reports"],
  ["Mental & Personality", "Neutral / Mixed", "Role Sensitive", "Reacting strongly to playing-time and depth-chart movement can drive either motivation or frustration.", "All players", "Depth-chart changes, playing-time reports"],
  ["Mental & Personality", "Neutral / Mixed", "Volatile", "More reactive to conflict, coaching fit, losing, and role changes; can bring emotional intensity.", "All players", "Staff interaction, morale swings, media behavior"],
  ["Mental & Personality", "Neutral / Mixed", "Media Magnet", "Creates more public attention and storyline exposure around notable events.", "All players", "Press coverage, media events"],
  ["Mental & Personality", "Neutral / Mixed", "Hot-and-Cold", "Creates greater week-to-week performance volatility outside pressure-specific situations.", "All players", "Performance streaks, coach observations"],
  ["Mental & Personality", "Negative", "Self-Interested", "Prioritizes personal role and compensation over team-friendly outcomes.", "All players", "Negotiations, role disputes, staff reports"],
  ["Mental & Personality", "Negative", "Pressure-Prone", "Increases performance volatility in high-pressure situations.", "All players", "Late-game and postseason film"],
  ["Mental & Personality", "Negative", "Short Temper", "Raises conflict and emotional-penalty exposure when frustrated.", "All players", "Penalty patterns, staff interactions, media behavior"],

  ["Health, Preparation & Development", "Positive", "Durable", "Lowers injury and recurrence exposure within normal football risk.", "All players", "Medical evaluations, availability history"],
  ["Health, Preparation & Development", "Positive", "Quick Healer", "Favors recovery outcomes within medically plausible limits.", "All players", "Medical recovery history"],
  ["Health, Preparation & Development", "Positive", "Elite Conditioning", "Improves fatigue resistance and training readiness across the season.", "All players", "Conditioning tests, practice load"],
  ["Health, Preparation & Development", "Positive", "Plays Through Pain", "More willing to remain available while injured, with a visible health-risk tradeoff.", "All players", "Medical and game availability decisions"],
  ["Health, Preparation & Development", "Positive", "Fast Learner", "Learns a new role, scheme, or playbook more quickly.", "All players", "Installation reports, coaching observations"],
  ["Health, Preparation & Development", "Negative", "Injury Prone", "Raises injury and recurrence exposure; medical care can mitigate but not erase it.", "All players", "Medical history, recurring injuries"],
  ["Health, Preparation & Development", "Negative", "Slow Healer", "Makes recovery outcomes slower within medically plausible limits.", "All players", "Medical recovery history"],
  ["Health, Preparation & Development", "Negative", "Poor Conditioning", "Worsens fatigue resilience and training readiness.", "All players", "Conditioning tests, practice load"],
  ["Health, Preparation & Development", "Negative", "Slow Learner", "Takes longer to absorb a new role, scheme, or playbook.", "All players", "Installation reports, coaching observations"],
  ["Health, Preparation & Development", "Negative", "Workout Avoider", "Makes training participation and gains less reliable.", "All players", "Practice habits, training reports"],
  ["Health, Preparation & Development", "Neutral / Mixed", "Overtrainer", "Can produce short-term training benefit but raises fatigue and health-management risk.", "All players", "Practice load, medical and conditioning reports"],

  ["Football Intelligence & Style", "Positive", "Field General", "Improves position-appropriate communication and execution.", "Quarterbacks, centers, defensive signal callers", "Film, coach observations, teammates"],
  ["Football Intelligence & Style", "Positive", "Scheme Savant", "Learns and performs in a compatible scheme or playbook more effectively.", "All players", "Installation performance, coach reports"],
  ["Football Intelligence & Style", "Positive", "Adaptable", "Reduces disruption from a team, scheme, role, or position change.", "All players", "Transitions, practice roles, prior team history"],
  ["Football Intelligence & Style", "Positive", "Disciplined", "Lowers avoidable penalty and assignment-error exposure.", "All players", "Game film, penalty patterns, coach reports"],
  ["Football Intelligence & Style", "Positive", "Ball Secure", "Lowers relevant fumble and interception risk.", "Ball carriers, receivers, quarterbacks", "Game film, turnover history"],
  ["Football Intelligence & Style", "Positive", "High Motor", "Supports sustained effort and late-down energy.", "All players", "Film, snap-to-snap effort observations"],
  ["Football Intelligence & Style", "Positive", "Big-Game Performer", "Provides a bounded composure and consistency benefit in high-leverage games.", "All players", "High-leverage and postseason performance"],
  ["Football Intelligence & Style", "Positive", "Film Junkie", "Improves game-plan preparation and opponent familiarity.", "All players", "Meeting reports, game-plan performance"],
  ["Football Intelligence & Style", "Neutral / Mixed", "Aggressive", "Creates more disruptive, high-risk tendencies where relevant.", "All players", "Film, penalty and decision patterns"],
  ["Football Intelligence & Style", "Neutral / Mixed", "Conservative", "Favors lower-risk decisions and steadier assignment choices.", "All players", "Film, decision patterns"],
  ["Football Intelligence & Style", "Neutral / Mixed", "Freelancer", "Can improvise and create plays but takes more assignment risk outside structure.", "All players", "Film, coach observations, assignment reports"],
  ["Football Intelligence & Style", "Negative", "System Dependent", "Suffers greater disruption outside a preferred system or role.", "All players", "Scheme changes, performance splits"],
  ["Football Intelligence & Style", "Negative", "Penalty Prone", "Raises avoidable penalty exposure.", "All players", "Penalty patterns, game film"],
  ["Football Intelligence & Style", "Negative", "Turnover Prone", "Raises relevant fumble and interception risk.", "Ball carriers, receivers, quarterbacks", "Game film, turnover history"],
  ["Football Intelligence & Style", "Negative", "Takes Plays Off", "Creates greater effort and fatigue inconsistency.", "All players", "Film, coaching reports"],
  ["Football Intelligence & Style", "Negative", "Big-Stage Shaky", "Raises high-leverage performance volatility.", "All players", "Late-game and postseason performance"],

  ["Situational & Position-Specific", "Positive", "Clutch Kicker", "Gives kickers a bounded consistency benefit on high-pressure attempts.", "Kickers", "Late-game field-goal history, coach observations"],
  ["Situational & Position-Specific", "Positive", "Cold-Weather Specialist", "Provides a favorable bounded adjustment in cold-weather games.", "All players", "Cold-weather performance history"],
  ["Situational & Position-Specific", "Positive", "Dome Specialist", "Provides a favorable bounded adjustment in indoor games.", "All players", "Indoor performance history"],
  ["Situational & Position-Specific", "Positive", "Playoff Performer", "Provides a bounded postseason consistency benefit.", "All players", "Postseason performance history"],
  ["Situational & Position-Specific", "Positive", "Short-Yardage Specialist", "Provides a favorable bounded adjustment in short-yardage execution.", "Eligible offensive line, tight end, fullback, running back roles", "Goal-line and short-yardage film"],
  ["Situational & Position-Specific", "Positive", "Red-Zone Threat", "Provides a favorable bounded adjustment in red-zone execution.", "Eligible quarterbacks, receivers, tight ends, running backs", "Red-zone film and production"],
  ["Situational & Position-Specific", "Positive", "Two-Minute Specialist", "Provides a favorable bounded adjustment in late-clock execution.", "Eligible quarterbacks, receivers, pass-protection roles", "Two-minute drill film"],
  ["Situational & Position-Specific", "Negative", "Cold-Weather Struggles", "Raises performance volatility in cold-weather games.", "All players", "Cold-weather performance history"],
  ["Situational & Position-Specific", "Negative", "Red-Zone Tightens", "Raises execution volatility in red-zone situations.", "Eligible quarterbacks, receivers, tight ends, running backs", "Red-zone film and production"],
  ["Situational & Position-Specific", "Negative", "Playoff Pressure", "Raises postseason performance volatility.", "All players", "Postseason performance history"],
];

const lastDataRow = traits.length + 4;

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("Traits");
const summary = workbook.worksheets.add("Summary");

sheet.showGridLines = false;
sheet.getRange("A1:F1").merge();
sheet.getRange("A1").values = [["Gridiron GM — Trait Catalog"]];
sheet.getRange("A2:F2").merge();
sheet.getRange("A2").values = [[`${traits.length} unique traits, sorted by category and impact. Effects are bounded modifiers, not rating replacements.`]];
sheet.getRange("A4:F4").values = [["Category", "Impact", "Trait", "Gameplay Effect", "Position / Scope", "Typical Discovery Evidence"]];
sheet.getRange(`A5:F${lastDataRow}`).values = traits;

sheet.getRange("A1:F1").format = { fill: "#1E2A44", font: { bold: true, color: "#FFFFFF", size: 16 }, horizontalAlignment: "left", verticalAlignment: "center" };
sheet.getRange("A2:F2").format = { fill: "#E8EEF8", font: { italic: true, color: "#344055" }, horizontalAlignment: "left", verticalAlignment: "center", wrapText: true };
sheet.getRange("A4:F4").format = { fill: "#2F5D50", font: { bold: true, color: "#FFFFFF" }, horizontalAlignment: "center", verticalAlignment: "center", wrapText: true, borders: { preset: "outside", style: "medium", color: "#1E3E35" } };
sheet.getRange(`A5:F${lastDataRow}`).format = { verticalAlignment: "top", wrapText: true, borders: { insideHorizontal: { style: "thin", color: "#D9E0E8" } } };
sheet.getRange(`B5:B${lastDataRow}`).conditionalFormats.add("containsText", { text: "Positive", format: { fill: "#D9EAD3", font: { color: "#245B2A", bold: true } } });
sheet.getRange(`B5:B${lastDataRow}`).conditionalFormats.add("containsText", { text: "Negative", format: { fill: "#F4CCCC", font: { color: "#8A1C1C", bold: true } } });
sheet.getRange(`B5:B${lastDataRow}`).conditionalFormats.add("containsText", { text: "Neutral / Mixed", format: { fill: "#FFF2CC", font: { color: "#7A5D00", bold: true } } });
sheet.getRange(`A5:A${lastDataRow}`).format = { fill: "#F2F5F8", font: { bold: true, color: "#2F4054" }, verticalAlignment: "top", wrapText: true, borders: { insideHorizontal: { style: "thin", color: "#D9E0E8" } } };
sheet.getRange(`C5:C${lastDataRow}`).format.font = { bold: true, color: "#1E2A44" };
sheet.getRange("A1:F1").format.rowHeight = 28;
sheet.getRange("A2:F2").format.rowHeight = 32;
sheet.getRange("A4:F4").format.rowHeight = 26;
sheet.getRange(`A5:F${lastDataRow}`).format.rowHeight = 40;
sheet.getRange("A:A").format.columnWidth = 27;
sheet.getRange("B:B").format.columnWidth = 16;
sheet.getRange("C:C").format.columnWidth = 23;
sheet.getRange("D:D").format.columnWidth = 56;
sheet.getRange("E:E").format.columnWidth = 38;
sheet.getRange("F:F").format.columnWidth = 42;
sheet.freezePanes.freezeRows(4);
sheet.freezePanes.freezeColumns(2);
sheet.tables.add(`A4:F${lastDataRow}`, true, "TraitCatalogTable");

summary.showGridLines = false;
summary.getRange("A1:E1").merge();
summary.getRange("A1").values = [["Trait Catalog Summary"]];
summary.getRange("A3:E3").values = [["Category", "Positive", "Neutral / Mixed", "Negative", "Total"]];
const categories = ["Mental & Personality", "Health, Preparation & Development", "Football Intelligence & Style", "Situational & Position-Specific"];
summary.getRange("A4:A7").values = categories.map((category) => [category]);
summary.getRange("B4:D7").formulas = categories.map((_, index) => {
  const row = index + 4;
  return [
    `=COUNTIFS('Traits'!$A$5:$A$${lastDataRow},$A${row},'Traits'!$B$5:$B$${lastDataRow},B$3)`,
    `=COUNTIFS('Traits'!$A$5:$A$${lastDataRow},$A${row},'Traits'!$B$5:$B$${lastDataRow},C$3)`,
    `=COUNTIFS('Traits'!$A$5:$A$${lastDataRow},$A${row},'Traits'!$B$5:$B$${lastDataRow},D$3)`,
  ];
});
summary.getRange("E4").formulas = [["=SUM(B4:D4)"]];
summary.getRange("E4:E7").fillDown();
summary.getRange("A9:D9").values = [["Catalog total", null, "Design target", "About 50"]];
summary.getRange("B9").formulas = [[`=COUNTA('Traits'!$C$5:$C$${lastDataRow})`]];
summary.getRange("A1:E1").format = { fill: "#1E2A44", font: { bold: true, color: "#FFFFFF", size: 16 }, horizontalAlignment: "left" };
summary.getRange("A3:E3").format = { fill: "#2F5D50", font: { bold: true, color: "#FFFFFF" }, horizontalAlignment: "center", borders: { preset: "outside", style: "medium", color: "#1E3E35" } };
summary.getRange("A4:E7").format = { borders: { preset: "inside", style: "thin", color: "#D9E0E8" }, verticalAlignment: "center" };
summary.getRange("A4:A7").format = { fill: "#F2F5F8", font: { bold: true, color: "#2F4054" } };
summary.getRange("B4:B7").format = { fill: "#D9EAD3", horizontalAlignment: "center" };
summary.getRange("C4:C7").format = { fill: "#FFF2CC", horizontalAlignment: "center" };
summary.getRange("D4:D7").format = { fill: "#F4CCCC", horizontalAlignment: "center" };
summary.getRange("E4:E7").format = { fill: "#E8EEF8", font: { bold: true }, horizontalAlignment: "center" };
summary.getRange("A9:D9").format = { fill: "#E8EEF8", font: { bold: true, color: "#1E2A44" }, borders: { preset: "outside", style: "medium", color: "#1E2A44" } };
summary.getRange("A:A").format.columnWidth = 34;
summary.getRange("B:E").format.columnWidth = 16;
summary.getRange("A1:E1").format.rowHeight = 28;
summary.getRange("A3:E3").format.rowHeight = 24;
summary.freezePanes.freezeRows(3);

const inspection = await workbook.inspect({ kind: "table", range: "Traits!A1:F12", include: "values,formulas", tableMaxRows: 12, tableMaxCols: 6 });
console.log(inspection.ndjson);
const summaryInspection = await workbook.inspect({ kind: "table", range: "Summary!A1:E9", include: "values,formulas", tableMaxRows: 9, tableMaxCols: 5 });
console.log(summaryInspection.ndjson);
const errorScan = await workbook.inspect({ kind: "match", searchTerm: "#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A", options: { useRegex: true, maxResults: 100 }, summary: "formula error scan" });
console.log(errorScan.ndjson);

const preview = await workbook.render({ sheetName: "Traits", range: "A1:F18", scale: 1.2, format: "png" });
await fs.writeFile(`${outputDir}\\trait_catalog_preview.png`, new Uint8Array(await preview.arrayBuffer()));
const summaryPreview = await workbook.render({ sheetName: "Summary", range: "A1:E9", scale: 1.5, format: "png" });
await fs.writeFile(`${outputDir}\\trait_catalog_summary_preview.png`, new Uint8Array(await summaryPreview.arrayBuffer()));
const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(outputPath);
console.log(outputPath);
