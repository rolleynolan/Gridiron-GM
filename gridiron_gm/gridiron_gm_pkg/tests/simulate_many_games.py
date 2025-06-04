import matplotlib.pyplot as plt
import numpy as np
from collections import defaultdict
from gridiron_gm.gridiron_gm_pkg.simulation.engine import game_engine
from gridiron_gm.gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm.gridiron_gm_pkg.simulation.entities.player import Player
import csv

# Try to import your real data loader
try:
    from gridiron_gm.gridiron_gm_pkg.simulation.systems.core import data_loader
    from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.team_data import fill_team_rosters_with_dummy_players
    DATA_LOADER_AVAILABLE = True
except ImportError:
    DATA_LOADER_AVAILABLE = False
    fill_team_rosters_with_dummy_players = None

def load_or_build_team(abbr, team_name=None, city=None, conference=None):
    """
    Try to load a real team with full depth chart. If not available, build a full 53-man roster and valid 11-man depth chart.
    """
    if DATA_LOADER_AVAILABLE:
        try:
            team = data_loader.load_team_by_abbr(abbr)
            if team and hasattr(team, "depth_chart") and len(team.depth_chart) > 0:
                return team
        except Exception:
            pass  # Fallback to manual creation

    # Manual fallback: create a full 53-man roster with realistic positions
    positions = (
        ["QB"] * 3 + ["RB"] * 4 + ["FB"] * 1 + ["WR"] * 6 + ["TE"] * 4 +
        ["LT"] * 2 + ["LG"] * 2 + ["C"] * 2 + ["RG"] * 2 + ["RT"] * 2 +
        ["DE"] * 4 + ["DT"] * 4 + ["LB"] * 7 + ["CB"] * 6 + ["S"] * 5 +
        ["K"] * 1 + ["P"] * 1 + ["LS"] * 1
    )
    roster = []
    for i, pos in enumerate(positions):
        player = Player(
            name=f"{pos} {i+1} {abbr}",
            position=pos,
            age=22 + (i % 10),
            dob=f"200{(i%10)}-01-01",
            college="Test U",
            birth_location="Testville, USA",
            jersey_number=(i % 99) + 1,
            overall=70 + (i % 20)
        )
        roster.append(player)

    team = Team(
        team_name=team_name or abbr,
        abbreviation=abbr,
        city=city or f"{abbr} City",
        conference=conference or "Test Conference",
        division="Test Division",
    )
    team.roster = roster

    # Build a valid 11-man starting depth chart for offense and defense
    depth_chart = defaultdict(list)
    # Offense
    for pos in ["QB", "RB", "FB", "WR", "TE", "LT", "LG", "C", "RG", "RT"]:
        depth_chart[pos] = [p for p in roster if p.position == pos][:1]
    # Defense
    for pos in ["DE", "DT", "LB", "CB", "S"]:
        depth_chart[pos] = [p for p in roster if p.position == pos][:1]
    # Special teams
    for pos in ["K", "P", "LS"]:
        depth_chart[pos] = [p for p in roster if p.position == pos][:1]
    team.depth_chart = dict(depth_chart)
    return team

NUM_GAMES = 10000  # Number of games to simulate

stats = {
    "A": defaultdict(list),
    "B": defaultdict(list)
}
results = []

teams = []
for i in range(NUM_GAMES):
    team_a = load_or_build_team("AAA", team_name="Alpha", city="Alpha City", conference="East")
    team_b = load_or_build_team("BBB", team_name="Beta", city="Beta City", conference="West")
    teams.extend([team_a, team_b])

# Ensure all teams have full rosters and depth charts before simulation
if fill_team_rosters_with_dummy_players is not None:
    fill_team_rosters_with_dummy_players(teams)

for i in range(NUM_GAMES):
    team_a = teams[2 * i]
    team_b = teams[2 * i + 1]
    home_stats, away_stats = game_engine.simulate_game(team_a, team_b, week=1)
    results.append({"home_stats": home_stats, "away_stats": away_stats})
    print(f"Finished game {i + 1}")

    for t, team_stats in [("A", home_stats), ("B", away_stats)]:
        stats[t]["points"].append(team_stats["points"])
        stats[t]["rush_yards"].append(team_stats["rush_yards"])
        stats[t]["pass_yards"].append(team_stats["pass_yards"])
        # Yards per play
        rush_plays = team_stats.get("rush_td", 0) + max(1, team_stats.get("rush_yards", 0) // 4)  # crude est.
        pass_plays = team_stats.get("pass_attempts", 0)
        stats[t]["yards_per_rush"].append(team_stats["rush_yards"] / rush_plays if rush_plays > 0 else 0)
        stats[t]["yards_per_pass"].append(team_stats["pass_yards"] / pass_plays if pass_plays > 0 else 0)
        stats[t]["completion_pct"].append(team_stats["completion_pct"])
        stats[t]["turnovers"].append(team_stats["turnovers"])
        stats[t]["penalties"].append(team_stats["penalties"])
        stats[t]["penalty_yards"].append(team_stats["penalty_yards"])
        stats[t]["rush_td"].append(team_stats["rush_td"])
        stats[t]["pass_td"].append(team_stats["pass_td"])
        stats[t]["fg"].append(team_stats["fg"])
        stats[t]["safety"].append(team_stats["safety"])
        stats[t]["def_td"].append(team_stats["def_td"])
        stats[t]["ret_td"].append(team_stats["ret_td"])

# Print first 5 simulated games' box scores and stat summaries
print("Sample simulation results:")
for r in results[:5]:
    print("Home:", r["home_stats"])
    print("Away:", r["away_stats"])
    print("---")

fig, axs = plt.subplots(4, 2, figsize=(18, 20))
fig.suptitle("NFL Sim Engine: 1000 Game Statistical Distributions", fontsize=18)

# Points scored: Histogram
axs[0, 0].hist(stats["A"]["points"], bins=range(0, 70, 3), alpha=0.7, label="Team A")
axs[0, 0].hist(stats["B"]["points"], bins=range(0, 70, 3), alpha=0.7, label="Team B")
axs[0, 0].set_title("Points Scored")
axs[0, 0].set_xlabel("Points")
axs[0, 0].set_ylabel("Games")
axs[0, 0].legend()
axs[0, 0].axvline(np.mean(stats["A"]["points"]), color='blue', linestyle='dashed', linewidth=1)
axs[0, 0].axvline(np.mean(stats["B"]["points"]), color='orange', linestyle='dashed', linewidth=1)

# Yards per rush: Boxplot
axs[0, 1].boxplot([stats["A"]["yards_per_rush"], stats["B"]["yards_per_rush"]], labels=["Team A", "Team B"])
axs[0, 1].set_title("Yards per Rush (Boxplot)")
axs[0, 1].set_ylabel("Yards per Rush")

# Yards per pass: Boxplot
axs[1, 0].boxplot([stats["A"]["yards_per_pass"], stats["B"]["yards_per_pass"]], labels=["Team A", "Team B"])
axs[1, 0].set_title("Yards per Pass (Boxplot)")
axs[1, 0].set_ylabel("Yards per Pass")

# Completion %: Violin plot
axs[1, 1].violinplot([stats["A"]["completion_pct"], stats["B"]["completion_pct"]], showmeans=True)
axs[1, 1].set_title("Completion Percentage (Violin Plot)")
axs[1, 1].set_ylabel("Completion %")
axs[1, 1].set_xticks([1, 2])
axs[1, 1].set_xticklabels(["Team A", "Team B"])

# Turnovers: Histogram
axs[2, 0].hist(stats["A"]["turnovers"], bins=range(0, 8), alpha=0.7, label="Team A")
axs[2, 0].hist(stats["B"]["turnovers"], bins=range(0, 8), alpha=0.7, label="Team B")
axs[2, 0].set_title("Turnovers")
axs[2, 0].set_xlabel("Turnovers")
axs[2, 0].set_ylabel("Games")
axs[2, 0].legend()

# Penalties (count): Side-by-side bar chart
pen_bins = np.arange(0, max(max(stats["A"]["penalties"]), max(stats["B"]["penalties"])) + 1)
pen_A_hist, _ = np.histogram(stats["A"]["penalties"], bins=pen_bins)
pen_B_hist, _ = np.histogram(stats["B"]["penalties"], bins=pen_bins)
bar_width = 0.4
axs[2, 1].bar(pen_bins[:-1] - bar_width/2, pen_A_hist, width=bar_width, label="Team A")
axs[2, 1].bar(pen_bins[:-1] + bar_width/2, pen_B_hist, width=bar_width, label="Team B")
axs[2, 1].set_title("Penalties (Count)")
axs[2, 1].set_xlabel("Penalties")
axs[2, 1].set_ylabel("Games")
axs[2, 1].legend()

# Penalty yards: Side-by-side bar chart
py_bins = np.linspace(0, max(max(stats["A"]["penalty_yards"]), max(stats["B"]["penalty_yards"])), 20)
py_A_hist, _ = np.histogram(stats["A"]["penalty_yards"], bins=py_bins)
py_B_hist, _ = np.histogram(stats["B"]["penalty_yards"], bins=py_bins)
center = (py_bins[:-1] + py_bins[1:]) / 2
axs[3, 0].bar(center - bar_width/2, py_A_hist, width=bar_width, label="Team A")
axs[3, 0].bar(center + bar_width/2, py_B_hist, width=bar_width, label="Team B")
axs[3, 0].set_title("Penalty Yards")
axs[3, 0].set_xlabel("Penalty Yards")
axs[3, 0].set_ylabel("Games")
axs[3, 0].legend()

# Score types: Grouped bar chart
score_labels = ["rush_td", "pass_td", "fg", "safety", "def_td", "ret_td"]
score_A = [sum(stats["A"][k]) for k in score_labels]
score_B = [sum(stats["B"][k]) for k in score_labels]
bar_x = np.arange(len(score_labels))
width = 0.35
axs[3, 1].bar(bar_x - width/2, score_A, width, label="Team A")
axs[3, 1].bar(bar_x + width/2, score_B, width, label="Team B")
axs[3, 1].set_xticks(bar_x)
axs[3, 1].set_xticklabels(["Rush TD", "Pass TD", "FG", "Safety", "Def TD", "Ret TD"])
axs[3, 1].set_title("Score Types (Total in 1000 Games)")
axs[3, 1].set_ylabel("Total")
axs[3, 1].legend()

plt.tight_layout(rect=[0, 0.03, 1, 0.97])
plt.show()

# --- Export simulation results as CSV ---
csv_filename = "simulation_results.csv"

# Flatten the first result to get all stat keys
first_home = results[0]["home_stats"]
first_away = results[0]["away_stats"]
home_keys = sorted(first_home.keys())
away_keys = sorted(first_away.keys())

# Prefix keys
home_keys_prefixed = [f"home_{k}" for k in home_keys]
away_keys_prefixed = [f"away_{k}" for k in away_keys]
header = home_keys_prefixed + away_keys_prefixed

print("About to export simulation results...")

try:
    with open(csv_filename, "w", newline='', encoding="utf-8") as csvfile:
        writer = csv.writer(csvfile)
        writer.writerow(header)
        for r in results:
            row = []
            # Use .get to handle missing keys if new stats are added later
            row.extend([r["home_stats"].get(k, "") for k in home_keys])
            row.extend([r["away_stats"].get(k, "") for k in away_keys])
            writer.writerow(row)
    print(f"Simulation results exported to {csv_filename}")
except Exception as e:
    print("CSV export failed:", e)