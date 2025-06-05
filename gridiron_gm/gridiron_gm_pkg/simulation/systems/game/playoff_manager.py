"""Playoff management utilities"""

from gridiron_gm.gridiron_gm_pkg.simulation.engine.game_engine import simulate_game
from gridiron_gm.gridiron_gm_pkg.simulation.systems.core.data_loader import save_playoff_results


def update_playoff_schedule(schedule_by_week, playoff_results, round_name, next_round_name, conference):
    """Replace placeholders in the next playoff round with winners from the current round."""
    winners = []
    for game in playoff_results.get(round_name, []):
        if conference != "Both" and game.get("conference") != conference:
            continue
        if game["home_score"] > game["away_score"]:
            winners.append(game["home_id"])
        else:
            winners.append(game["away_id"])

    if not winners:
        print(f"[DEBUG] No winners found for {round_name} ({conference})")
        return

    if next_round_name == "Divisional":
        one_seed_id = None
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Divisional" and game.get("conference") == conference and "TBD_LowestSeedWinner" in str(game.get("away_id")):
                    one_seed_id = game.get("home_id")
        if one_seed_id in winners:
            winners.remove(one_seed_id)
        winners_sorted = sorted(winners)
        if len(winners_sorted) < 3:
            print(f"[DEBUG] Not enough winners for Divisional ({conference}): {winners_sorted}")
            return
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Divisional" and game.get("conference") == conference:
                    if "TBD_LowestSeedWinner" in str(game.get("away_id")):
                        game["away_id"] = winners_sorted[0]
                        game["away_abbr"] = None
                    elif "TBD_HighSeedHost" in str(game.get("home_id")):
                        game["home_id"] = winners_sorted[1]
                        game["away_id"] = winners_sorted[2]
                        game["home_abbr"] = None
                        game["away_abbr"] = None
    elif next_round_name == "Conference Championship":
        winners_sorted = sorted(winners)
        if len(winners_sorted) < 2:
            print(f"[DEBUG] Not enough winners for Conference Championship ({conference}): {winners_sorted}")
            return
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Conference Championship" and game.get("conference") == conference:
                    game["home_id"] = winners_sorted[0]
                    game["away_id"] = winners_sorted[1]
                    game["home_abbr"] = None
                    game["away_abbr"] = None
    elif next_round_name == "Gridiron Bowl":
        winners_sorted = sorted(winners)
        if len(winners_sorted) < 2:
            print(f"[DEBUG] Not enough winners for Gridiron Bowl: {winners_sorted}")
            return
        for g in schedule_by_week.values():
            for game in g:
                if game.get("round") == "Gridiron Bowl":
                    game["home_id"] = winners_sorted[0]
                    game["away_id"] = winners_sorted[1]
                    game["home_abbr"] = None
                    game["away_abbr"] = None


class PlayoffManager:
    """Orchestrates playoff simulation across all rounds."""

    def __init__(self, season_manager):
        self.season_manager = season_manager

    def run_playoffs(self):
        sm = self.season_manager
        results = {"Nova": [], "Atlas": [], "Championship": None}
        rounds = [
            ("Wild Card", "Divisional"),
            ("Divisional", "Conference Championship"),
            ("Conference Championship", "Gridiron Bowl"),
            ("Gridiron Bowl", None),
        ]
        weeks = sorted(int(w) for w in sm.schedule_by_week.keys())

        for round_name, next_round in rounds:
            for week in weeks:
                week_str = str(week)
                games = sm.schedule_by_week.get(week_str, [])
                for game in games:
                    if not game.get("playoff") or game.get("round") != round_name:
                        continue
                    home_id = game["home_id"]
                    away_id = game["away_id"]
                    home_team = sm.id_to_team.get(home_id)
                    away_team = sm.id_to_team.get(away_id)
                    sim_home, sim_away = simulate_game(
                        home_team,
                        away_team,
                        week=week,
                        context={"weather": None},
                    )
                    home_score = sim_home.get("points", sim_home.get("score", 0))
                    away_score = sim_away.get("points", sim_away.get("score", 0))
                    game["home_score"] = home_score
                    game["away_score"] = away_score
                    result = {
                        "home_id": home_id,
                        "away_id": away_id,
                        "home_score": home_score,
                        "away_score": away_score,
                        "conference": game.get("conference"),
                        "round": round_name,
                        "final_score": f"{sm.id_to_abbr.get(home_id, home_id)} {home_score} - {sm.id_to_abbr.get(away_id, away_id)} {away_score}",
                        "result_str": f"{sm.id_to_abbr.get(home_id, home_id)} {home_score}, {sm.id_to_abbr.get(away_id, away_id)} {away_score}",
                    }
                    if round_name == "Gridiron Bowl":
                        results["Championship"] = result
                        champ_id = home_id if home_score > away_score else away_id
                        sm.champion = sm.id_to_abbr.get(champ_id, champ_id)
                        sm.runner_up = sm.id_to_abbr.get(away_id if champ_id == home_id else home_id, away_id if champ_id == home_id else home_id)
                    else:
                        conf = game.get("conference")
                        results.setdefault(conf, []).append(result)
            if next_round:
                if next_round == "Gridiron Bowl":
                    update_playoff_schedule(sm.schedule_by_week, results, round_name, next_round, "Both")
                else:
                    for conf in ["Nova", "Atlas"]:
                        update_playoff_schedule(sm.schedule_by_week, results, round_name, next_round, conf)

        save_playoff_results(results, sm.save_name)
        sm.playoff_results = results
        return results

