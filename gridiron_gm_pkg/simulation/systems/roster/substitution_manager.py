from typing import Dict, List, Tuple, Any

class SubstitutionManagerV2:
    """
    Handles active lineup selection and substitution based on fatigue and depth chart.
    """
    def __init__(self, depth_chart: Dict[str, List[Any]]):
        self.depth_chart = depth_chart

    def get_active_lineup_with_bench_log(
        self,
        formation: Dict[str, int],
        offense: Any,
        fatigue_log: List[str],
        scheme: Dict[str, int]
    ) -> Tuple[Dict[str, Any], List[str]]:
        """
        Build an active lineup for the given formation and offense, considering fatigue.
        Records any substitutions in fatigue_log and returns a bench_log.
        Returns:
            Tuple of (lineup dict, bench_log list)
        """
        bench_log: List[str] = []
        offense_lineup: List[Any] = []
        defense_lineup: List[Any] = []
        lineup: Dict[str, Any] = {}

        # Defensive: Check for valid depth chart
        if not hasattr(self, "depth_chart") or not isinstance(self.depth_chart, dict):
            team_name = getattr(offense, "name", "Unknown")
            print(f"[ERROR] SubstitutionManagerV2: Missing or malformed depth_chart for team {team_name}")
            return {"offense": [], "defense": []}, bench_log

        # Build offense lineup
        for position, count in scheme.items():
            depth_list = self.depth_chart.get(position, [])
            chosen = []
            for i in range(count):
                if i < len(depth_list):
                    player = depth_list[i]
                    fatigue_threshold = 0.9
                    if hasattr(player, "fatigue") and player.fatigue >= fatigue_threshold and len(depth_list) > i + 1:
                        backup = depth_list[i + 1]
                        if hasattr(backup, "fatigue") and backup.fatigue < fatigue_threshold:
                            chosen.append(backup)
                            bench_log.append(f"{position}: {player.name} → {backup.name}")
                            fatigue_log.append(f"[SUB] {position}: {player.name} → {backup.name} (fatigue {getattr(player, 'fatigue', 0):.2f} → {getattr(backup, 'fatigue', 0):.2f})")
                        else:
                            chosen.append(player)
                    else:
                        chosen.append(player)
                else:
                    # Not enough players for this position
                    team_name = getattr(offense, "name", "Unknown")
                    print(f"[ERROR] SubstitutionManagerV2: Not enough players for {position} on team {team_name} (needed {count}, got {len(depth_list)})")
            offense_lineup.extend(chosen)

        # Build standard 11-man defense: 2 DE, 2 DT, 3 LB, 2 CB, 2 S
        defense_scheme = {"DE": 2, "DT": 2, "LB": 3, "CB": 2, "S": 2}
        for position, count in defense_scheme.items():
            depth_list = self.depth_chart.get(position, [])
            chosen = []
            for i in range(count):
                if i < len(depth_list):
                    player = depth_list[i]
                    chosen.append(player)
                else:
                    team_name = getattr(offense, "name", "Unknown")
                    print(f"[ERROR] SubstitutionManagerV2: Not enough defensive players for {position} on team {team_name} (needed {count}, got {len(depth_list)})")
            defense_lineup.extend(chosen)

        # Always return dict with offense and defense keys, each mapping to a list (even if empty)
        return {"offense": offense_lineup, "defense": defense_lineup}, bench_log
