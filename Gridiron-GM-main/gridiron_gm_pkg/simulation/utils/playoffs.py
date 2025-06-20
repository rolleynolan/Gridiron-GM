from gridiron_gm_pkg.simulation.engine.game_engine import simulate_game
from gridiron_gm_pkg.simulation.systems.game.tiebreakers import StandingsManager as TiebreakerManager


def generate_playoff_seeds(season_manager, num_seeds=8):
    """Return top seeded team ids per conference using tiebreaker logic."""
    league = season_manager.league
    standings_mgr = season_manager.standings_manager
    calendar = season_manager.calendar
    results_by_week = season_manager.results_by_week

    league_data = [
        {
            "abbreviation": t.abbreviation,
            "conference": getattr(t, "conference", "Unknown"),
            "division": getattr(t, "division", "Unknown"),
        }
        for t in league.teams
    ]
    tb = TiebreakerManager(calendar, league_data, season_manager.save_name, results_by_week)
    tb.standings = {}
    for tid, rec in standings_mgr.standings.items():
        abbr = season_manager.id_to_abbr.get(tid, tid)
        tb.standings[abbr] = {
            "W": rec.get("W", 0),
            "L": rec.get("L", 0),
            "T": rec.get("T", 0),
            "PF": rec.get("PF", 0),
            "PA": rec.get("PA", 0),
            "conference": rec.get("conference", "Unknown"),
            "division": rec.get("division", "Unknown"),
            "opponents": {},
        }
    for week in results_by_week.values():
        for res in week:
            h = res["home"]
            a = res["away"]
            hs = res["home_score"]
            as_ = res["away_score"]
            h_abbr = season_manager.id_to_abbr.get(h, h)
            a_abbr = season_manager.id_to_abbr.get(a, a)
            for t_abbr, o_abbr, ts, os in [
                (h_abbr, a_abbr, hs, as_),
                (a_abbr, h_abbr, as_, hs),
            ]:
                if t_abbr not in tb.standings:
                    continue
                opps = tb.standings[t_abbr].setdefault("opponents", {})
                opps.setdefault(o_abbr, {"W": 0, "L": 0, "T": 0})
                if ts > os:
                    opps[o_abbr]["W"] += 1
                elif ts < os:
                    opps[o_abbr]["L"] += 1
                else:
                    opps[o_abbr]["T"] += 1

    def rank(team_list):
        abbrs = [t.abbreviation for t in team_list]
        ranked_abbrs = tb.break_ties(abbrs)
        return [season_manager.abbr_to_team[a] for a in ranked_abbrs if a in season_manager.abbr_to_team]

    def get_conference_teams(conf):
        return [t for t in league.teams if getattr(t, "conference", None) == conf]

    def get_division_teams(conf, div):
        return [t for t in league.teams if getattr(t, "conference", None) == conf and getattr(t, "division", None) == div]

    def get_divisions(conf):
        return sorted(set(getattr(t, "division", None) for t in league.teams if getattr(t, "conference", None) == conf))

    playoff_seeds = {}
    for conf in ["Nova", "Atlas"]:
        divisions = get_divisions(conf)
        champs = []
        for div in divisions:
            champs.append(rank(get_division_teams(conf, div))[0])
        champs = rank(champs)
        all_conf = get_conference_teams(conf)
        wild_cards = [t for t in rank([t for t in all_conf if t not in champs])[: num_seeds - len(champs)]]
        seeds = champs + wild_cards
        playoff_seeds[conf] = [t.id for t in seeds]

    return playoff_seeds


def simulate_playoff_round(games, id_to_team, week, id_to_abbr):
    """Simulate playoff games for a single round."""
    results = []
    for g in games:
        home_team = id_to_team[g["home_id"]]
        away_team = id_to_team[g["away_id"]]
        sim_home, sim_away = simulate_game(home_team, away_team, week=week, context={"weather": None})
        home_score = sim_home.get("points", sim_home.get("score", 0))
        away_score = sim_away.get("points", sim_away.get("score", 0))
        results.append({
            "home_id": g["home_id"],
            "away_id": g["away_id"],
            "home_score": home_score,
            "away_score": away_score,
            "winner": g["home_id"] if home_score > away_score else g["away_id"],
            "conference": g.get("conference", "Both"),
            "final_score": f"{id_to_abbr.get(g['home_id'], g['home_id'])} {home_score} - {id_to_abbr.get(g['away_id'], g['away_id'])} {away_score}",
        })
    return results
