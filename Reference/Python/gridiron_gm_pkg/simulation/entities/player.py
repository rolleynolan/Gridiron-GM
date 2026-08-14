import datetime
import random
from uuid import uuid4
from dataclasses import dataclass, field, asdict, is_dataclass
from typing import List, Dict, Optional
from gridiron_gm_pkg.simulation.systems.player.player_dna import PlayerDNA

# Generic attributes shared by all players
CORE_ATTRIBUTES = [
    "speed",
    "acceleration",
    "agility",
    "strength",
    "awareness",
    "iq",
    "stamina",
    "toughness",
    "balance",
    "discipline",
    "consistency",
]


def generate_pot(
    position: str,
    age: int,
    league_level: str = "pro",
    rng: random.Random | None = None,
) -> int:
    rng = rng or random
    pos = (position or "").upper()
    if pos in {"QB"}:
        weights = [0.1, 0.4, 0.35, 0.15]
    elif pos in {"K", "P"}:
        weights = [0.3, 0.45, 0.2, 0.05]
    elif pos in {"RB", "WR", "TE", "CB", "S"}:
        weights = [0.12, 0.45, 0.3, 0.13]
    else:
        weights = [0.15, 0.5, 0.25, 0.1]

    if league_level == "college":
        weights = [max(0.05, w - 0.02) for w in weights]
        weights[-1] += 0.06
        weights[-2] += 0.02

    if age <= 21:
        weights[0] = max(0.02, weights[0] - 0.05)
        weights[1] = max(0.05, weights[1] - 0.05)
        weights[2] += 0.05
        weights[3] += 0.05
    elif age >= 23:
        weights[0] += 0.05
        weights[1] += 0.05
        weights[2] = max(0.05, weights[2] - 0.05)
        weights[3] = max(0.02, weights[3] - 0.05)

    total = sum(weights)
    weights = [w / total for w in weights]
    roll = rng.random()
    thresholds = [weights[0], weights[0] + weights[1], weights[0] + weights[1] + weights[2]]
    if roll < thresholds[0]:
        low, high = 60, 69
    elif roll < thresholds[1]:
        low, high = 70, 79
    elif roll < thresholds[2]:
        low, high = 80, 89
    else:
        low, high = 90, 99
    return int(rng.randint(low, high))


def ensure_pot(player, league_level: str = "pro") -> bool:
    pot = getattr(player, "pot", None)
    if pot is not None:
        return False
    pot = generate_pot(getattr(player, "position", ""), getattr(player, "age", 22), league_level)
    overall = getattr(player, "overall", 0)
    pot = max(int(round(pot)), int(round(overall)))
    pot = min(pot, 99)
    setattr(player, "pot", pot)
    return True


@dataclass
class DevArc:
    type: str
    current_progress: float
    milestones: List[str] = field(default_factory=list)


@dataclass
class AttributeSet:
    core: Dict[str, int] = field(default_factory=dict)
    position_specific: Dict[str, int] = field(default_factory=dict)


@dataclass
class Contract:
    years: int
    salary_per_year: int
    bonuses: Dict[str, int] = field(default_factory=dict)


class Player:
    """Represents a football player."""

    # ------------------------------------------------------------------
    # Core attribute property helpers
    def _get_core_attr(self, name: str) -> Optional[int]:
        return getattr(self, "attributes", AttributeSet()).core.get(name)

    def _set_core_attr(self, name: str, value: int) -> None:
        if not hasattr(self, "attributes"):
            self.attributes = AttributeSet(core={}, position_specific={})
        self.attributes.core[name] = value

    # Dynamically expose common core attributes for backward compatibility
    @property
    def speed(self) -> Optional[int]:
        return self._get_core_attr("speed")

    @speed.setter
    def speed(self, value: int) -> None:
        self._set_core_attr("speed", value)

    @property
    def acceleration(self) -> Optional[int]:
        return self._get_core_attr("acceleration")

    @acceleration.setter
    def acceleration(self, value: int) -> None:
        self._set_core_attr("acceleration", value)

    @property
    def agility(self) -> Optional[int]:
        return self._get_core_attr("agility")

    @agility.setter
    def agility(self, value: int) -> None:
        self._set_core_attr("agility", value)

    @property
    def strength(self) -> Optional[int]:
        return self._get_core_attr("strength")

    @strength.setter
    def strength(self, value: int) -> None:
        self._set_core_attr("strength", value)

    @property
    def awareness(self) -> Optional[int]:
        return self._get_core_attr("awareness")

    @awareness.setter
    def awareness(self, value: int) -> None:
        self._set_core_attr("awareness", value)

    @property
    def iq(self) -> Optional[int]:
        return self._get_core_attr("iq")

    @iq.setter
    def iq(self, value: int) -> None:
        self._set_core_attr("iq", value)

    @property
    def stamina(self) -> Optional[int]:
        return self._get_core_attr("stamina")

    @stamina.setter
    def stamina(self, value: int) -> None:
        self._set_core_attr("stamina", value)

    @property
    def toughness(self) -> Optional[int]:
        return self._get_core_attr("toughness")

    @toughness.setter
    def toughness(self, value: int) -> None:
        self._set_core_attr("toughness", value)

    @property
    def balance(self) -> Optional[int]:
        return self._get_core_attr("balance")

    @balance.setter
    def balance(self, value: int) -> None:
        self._set_core_attr("balance", value)

    @property
    def discipline(self) -> Optional[int]:
        return self._get_core_attr("discipline")

    @discipline.setter
    def discipline(self, value: int) -> None:
        self._set_core_attr("discipline", value)

    @property
    def discipline_rating(self) -> int:
        return self.resolve_discipline_rating()

    @discipline_rating.setter
    def discipline_rating(self, value: int) -> None:
        self.discipline = value

    @property
    def consistency(self) -> Optional[int]:
        return self._get_core_attr("consistency")

    @consistency.setter
    def consistency(self, value: int) -> None:
        self._set_core_attr("consistency", value)

    @property
    def position_specific(self) -> Dict[str, int]:
        return getattr(self, "attributes", AttributeSet()).position_specific

    def __init__(
        self,
        name,
        position,
        age,
        dob,
        college,
        birth_location,
        jersey_number,
        overall,
        potential=None,
        is_college: bool = False,
    ):
        self.id = str(uuid4())
        self.name = name
        self.position = position
        self.age = age
        self.dob = dob
        self.college = college
        self.birth_location = birth_location
        self.jersey_number = jersey_number
        self.overall = overall
        self.potential = overall if potential is None else potential
        if potential is None:
            pot_value = generate_pot(self.position, self.age)
        else:
            pot_value = potential
        pot_value = max(int(round(pot_value)), int(round(self.overall)))
        self.pot = min(pot_value, 99)

        # Initialize attribute containers
        core_attrs = self.init_core_attributes()
        pos_attrs = self.init_position_attributes()

        self.attributes = AttributeSet(core=core_attrs, position_specific=pos_attrs)

        # --- Off-field attributes
        self.motivation = None
        self.loyalty = None
        self.ambition = None
        self.greed = None
        self.passion = None
        self.resilience = None
        self.dev_arc = DevArc("standard", 0.0)
        self.contract = None
        self.morale = 100
        self.fatigue = 0.0
        self.snaps = 0
        self.sub_cooldown = 0

        self.current_team = None
        self.rookie_year = None
        self.drafted_by = None
        self.draft_round = None
        self.draft_pick = None

        self.injuries = []
        self.injury_history = []
        # Legacy fields (ignored for gameplay logic; kept for backward compatibility)
        self.weeks_out = 0
        self.retired_due_to_injury = False
        self.retired = False
        self.on_injured_reserve = False
        self.is_injured = False
        self.injury_status = "healthy"
        self.injury_name = None
        self.injury_start_date = None
        self.injury_end_date = None
        self.injury_severity = None
        # Track active temporary penalties from injuries
        self.active_injury_effects = {}

        self.traits = {
            "training": [],
            "gameday": [],
            "physical": [],
            "mental": [],
            "media": [],
        }

        self.skills = {}
        self.experience = 0
        self.notes = []
        self.playtime_history = []

        self.career_stats = {}
        self.stats_by_year = {}
        self.season_stats = {}
        self.snap_counts = {}
        self.milestones_hit = set()

        # --- Scouting related fields
        # hidden_caps: the true ceiling for each attribute (not visible to the user)
        # scouted_potential: what scouts currently believe the ceiling to be
        # last_attribute_values: last recorded attribute values for year-over-year comparison
        # no_growth_years: consecutive years with no growth for an attribute
        self.hidden_caps = {}
        self.scouted_potential = {}
        self.last_attribute_values = {}
        self.no_growth_years = {}
        # Track periodic snapshots of attributes
        self.progress_history = {}

        # --- Procedural DNA profile ---
        self.dna = PlayerDNA.generate_random_dna(self.position, is_college=is_college)

        relevant = self.get_relevant_attribute_names()
        self.hidden_caps = {}
        self.scouted_potential = {}
        for attr in relevant:
            cap_info = self.dna.attribute_caps.get(attr)
            if cap_info:
                cur = cap_info.get("current", 20)
                hard_cap = cap_info.get("hard_cap", 20)
            else:
                cur = 20
                hard_cap = 20
            self.hidden_caps[attr] = hard_cap
            self.scouted_potential[attr] = self.dna.scouted_caps.get(attr, hard_cap)
            if attr in self.attributes.core:
                self.attributes.core[attr] = cur
            else:
                self.attributes.position_specific[attr] = cur
        self.normalize_ratings()
        self.attribute_xp = {}
        from gridiron_gm_pkg.simulation.systems.player.attribute_xp import sync_xp_from_rating

        sync_xp_from_rating(self)

    def _compute_overall(self) -> int:
        attrs = getattr(self, "attributes", None)
        if attrs is None:
            return int(round(getattr(self, "overall", 0)))
        values = []
        for container in (attrs.core, attrs.position_specific):
            for val in container.values():
                if isinstance(val, (int, float)):
                    values.append(val)
        if not values:
            return int(round(getattr(self, "overall", 0)))
        return int(round(sum(values) / len(values)))

    def normalize_ratings(self) -> None:
        attrs = getattr(self, "attributes", None)
        if attrs is not None:
            for container in (attrs.core, attrs.position_specific):
                for key, val in list(container.items()):
                    if isinstance(val, (int, float)):
                        container[key] = max(0, min(99, int(round(val))))
        self.overall = max(0, min(99, self._compute_overall()))
        pot_value = getattr(self, "pot", None)
        if not isinstance(pot_value, (int, float)):
            pot_value = self.overall
        pot_value = max(0, min(99, int(round(pot_value))))
        self.pot = max(pot_value, self.overall)

    def init_core_attributes(self):
        """Return baseline attribute mapping common to all players."""
        core = {attr: None for attr in CORE_ATTRIBUTES}
        core["stamina"] = 80
        return core

    def init_position_attributes(self):
        position = self.position.upper()
        attrs = []

        if position in ["QB"]:
            attrs = [
                "throw_power",
                "throw_accuracy_short",
                "throw_accuracy_mid",
                "throw_accuracy_deep",
                "throw_on_run",
                "pocket_presence",
                "release_time",
                "read_progression",
                "scramble_tendency",
                "throwing_footwork",
                "throw_under_pressure",
            ]
        elif position in ["RB"]:
            attrs = [
                "ball_carrier_vision",
                "elusiveness",
                "break_tackle",
                "trucking",
                "carry_security",
                "pass_block",
                "route_running",
                "catching",
            ]
        elif position in ["WR"]:
            attrs = [
                "catching",
                "catch_in_traffic",
                "spectacular_catch",
                "release",
                "route_running_short",
                "route_running_mid",
                "route_running_deep",
                "separation",
                "run_blocking",
            ]
        elif position in ["TE"]:
            attrs = [
                "catching",
                "catch_in_traffic",
                "release",
                "route_running_short",
                "route_running_mid",
                "route_running_deep",
                "separation",
                "run_blocking",
                "pass_block",
                "lead_blocking",
            ]
        elif position in ["LT", "LG", "C", "RG", "RT", "OL"]:
            attrs = [
                "pass_block",
                "run_block",
                "impact_blocking",
                "block_shed_resistance",
                "footwork_ol",
                "lead_blocking",
            ]
        elif position in ["EDGE", "DE"]:
            attrs = [
                "pass_rush_power",
                "pass_rush_finesse",
                "block_shedding",
                "run_defense",
                "pursuit_dl",
                "tackle_dl",
                "play_recognition",
                "hands",
                "hit_power",
                "strip_ball",
            ]
        elif position in ["DT"]:
            attrs = [
                "block_shedding",
                "run_defense",
                "pass_rush_power",
                "pass_rush_finesse",
                "tackle_dl",
                "pursuit_dl",
                "play_recognition",
                "hands",
                "hit_power",
                "strip_ball",
            ]
        elif position in ["MLB", "OLB", "LB"]:
            attrs = [
                "tackle_lb",
                "block_shedding",
                "zone_coverage_lb",
                "man_coverage_lb",
                "pass_rush_lb",
                "pursuit_lb",
                "play_recognition_lb",
                "catching",
                "hit_power",
                "strip_ball",
            ]
        elif position in ["CB"]:
            attrs = [
                "man_coverage",
                "zone_coverage",
                "press",
                "play_recognition_cb",
                "catching_cb",
                "tackle_cb",
                "pursuit_cb",
                "hit_power",
                "strip_ball",
            ]
        elif position in ["FS", "SS", "S"]:
            attrs = [
                "zone_coverage_s",
                "man_coverage_s",
                "tackle_s",
                "hit_power",
                "catching_s",
                "run_support",
                "play_recognition_s",
                "strip_ball",
            ]
        elif position in ["K"]:
            attrs = [
                "kick_power",
                "kick_accuracy",
                "kick_consistency",
                "kick_clutch",
                "onside_kick_skill",
            ]
        elif position in ["P"]:
            attrs = ["kick_power", "kick_accuracy", "hang_time", "kick_consistency"]

        return {attr: None for attr in attrs}

    def get_all_attributes(self) -> Dict[str, int]:
        """Return combined core and position-specific attribute mapping."""
        attrs = {}
        attrs.update(getattr(self, "attributes", AttributeSet()).core)
        attrs.update(getattr(self, "attributes", AttributeSet()).position_specific)
        return attrs

    def get_relevant_attribute_names(self) -> List[str]:
        """Return list of all attribute names used for this player."""
        return list(self.get_all_attributes().keys())

    def add_trait(self, category, trait):
        if category in self.traits:
            self.traits[category].append(trait)

    def get_fatigue_rate(self):
        # Base fatigue rate for all players
        base = 0.1

        # Increase fatigue rate for positions requiring high physical exertion
        if self.position in ["WR", "CB", "RB", "LB"]:
            base += 0.05

        # Adjust fatigue rate based on stamina (lower stamina increases fatigue)
        stamina = self.stamina if self.stamina is not None else 80
        base *= (100 - stamina) / 100
        return max(0.01, float(base))

    def fatigue_threshold(self):
        """Return the fatigue level at which the player is considered tired."""

        # Base threshold for fatigue varies by position:
        # - RB, WR, DL: More demanding positions have a lower threshold (0.6).
        # - QB, OL, K, P: Less demanding positions have a higher threshold (0.9).
        # - Others default to 0.7.
        base = 0.7
        if self.position in ["RB", "WR", "DL"]:
            base = 0.6
        elif self.position in ["QB", "OL", "K", "P"]:
            base = 0.9

        # Adjust the base threshold slightly based on stamina.
        base += (self.stamina - 80) * 0.002
        return base

    def is_fatigued(self):
        return self.fatigue >= self.fatigue_threshold()

    def play_snap(self, intensity=1.0):
        self.snaps += 1
        self.fatigue += self.get_fatigue_rate() * intensity
        self.fatigue = min(self.fatigue, 1.0)
        if self.sub_cooldown > 0:
            self.sub_cooldown -= 1

    def add_injury(self, injury):
        """Legacy hook for old injury objects; converts to status-based fields."""
        from gridiron_gm_pkg.simulation.systems.player.injury_status import apply_simple_injury

        name = getattr(injury, "name", None) or str(injury)
        weeks_out = getattr(injury, "weeks_out", None)
        severity = getattr(injury, "severity", None)
        if weeks_out:
            duration_days = max(1, int(weeks_out) * 7)
            apply_simple_injury(self, datetime.date.today(), duration_days, name, severity=severity)
        else:
            self.injury_status = "out"
            self.injury_name = name
            self.injury_start_date = datetime.date.today()
            self.injury_end_date = None
            self.injury_severity = None
        if isinstance(self.injuries, list):
            self.injuries.append(name)
        if isinstance(self.injury_history, list):
            self.injury_history.append(name)
        self.weeks_out = 0
        self.is_injured = False

    def recover_one_week(self):
        """Legacy no-op for the deprecated weeks_out injury model."""
        if self.weeks_out > 0:
            if "Quick Recovery" in self.traits.get("physical", []):
                self.weeks_out = max(1, int((self.weeks_out - 1) * 0.90))
            else:
                self.weeks_out -= 1
            if self.weeks_out == 0:
                self.injuries.clear()
                self.is_injured = False

    def get_effective_attribute(self, attr: str):
        """Return the attribute value factoring in active injury penalties."""
        attrs = getattr(self, "attributes", None)
        base = None
        if attrs is not None and attr in attrs.core:
            base = attrs.core.get(attr)
        elif attrs is not None and attr in attrs.position_specific:
            base = attrs.position_specific.get(attr)
        else:
            base = getattr(self, attr, None)

        if base is None:
            base = 0

        effects = getattr(self, "active_injury_effects", {})
        penalty = 0
        if isinstance(effects, dict):
            penalty = effects.get(attr, 0)
        else:
            for eff in effects:
                if isinstance(eff, dict) and eff.get("attribute") == attr:
                    penalty += eff.get("change", 0)

        return base + penalty

    def resolve_discipline_rating(self, default: int = 50) -> int:
        """Return a backward-compatible discipline rating for mixed save formats."""
        candidates = []
        direct = self.__dict__.get("discipline_rating")
        if direct is not None:
            candidates.append(direct)
        discipline = self.__dict__.get("discipline")
        if discipline is not None:
            candidates.append(discipline)

        attrs = getattr(self, "attributes", None)
        if attrs is not None:
            core = getattr(attrs, "core", None)
            if isinstance(core, dict):
                candidates.append(core.get("discipline"))
            ratings = getattr(attrs, "ratings", None)
            if isinstance(ratings, dict):
                candidates.append(ratings.get("discipline"))

        raw_attrs = self.__dict__.get("attributes")
        if isinstance(raw_attrs, dict):
            core = raw_attrs.get("core")
            if isinstance(core, dict):
                candidates.append(core.get("discipline"))
            ratings = raw_attrs.get("ratings")
            if isinstance(ratings, dict):
                candidates.append(ratings.get("discipline"))

        ratings = getattr(self, "ratings", None)
        if isinstance(ratings, dict):
            candidates.append(ratings.get("discipline"))

        for value in candidates:
            if value is None:
                continue
            try:
                return max(0, min(99, int(round(float(value)))))
            except (TypeError, ValueError):
                continue
        return max(0, min(99, int(default)))

    def update_career_stats_from_season(self, year, game_world=None) -> List[str]:
        """Aggregate a season's totals into ``career_stats`` and check milestones.

        Parameters
        ----------
        year : int | str
            The season year to aggregate.
        game_world : dict | None
            Optional game world to update career record tracking.
        """
        year_key = str(year)
        data = self.season_stats.get(year_key)
        if not data or data.get("career_added"):
            return []

        from gridiron_gm_pkg.stats.player_stat_manager import (
            update_career_stats,
        )

        totals = data.get("season_totals", {})
        update_career_stats(self, totals)
        data["career_added"] = True

        if game_world is not None:
            from gridiron_gm_pkg.stats.record_book import (
                update_career_record,
                update_career_leaderboard,
            )

            for stat, val in totals.items():
                if stat == "snap_counts" or not isinstance(val, (int, float)):
                    continue
                current = self.career_stats.get(stat, 0)
                update_career_record(game_world, self.id, stat, current)
                update_career_leaderboard(game_world, stat, self.id, current)

        return self.check_for_new_milestones()

    def check_for_new_milestones(self) -> List[str]:
        """Check career stats for milestone thresholds.

        Returns
        -------
        List[str]
            Milestone identifiers reached during this check.
        """
        from gridiron_gm_pkg.stats.milestone_definitions import (
            MILESTONES,
        )

        new = []
        for stat, thresholds in MILESTONES.items():
            total = self.career_stats.get(stat, 0)
            for threshold in thresholds:
                key = f"{stat}_{threshold}"
                if total >= threshold and key not in self.milestones_hit:
                    self.milestones_hit.add(key)
                    new.append(key)
        return new

    def update_player_stats(self, stat_type, value):
        if stat_type in self.career_stats:
            self.career_stats[stat_type] += value

    def update_performance_due_to_traits(self):
        if "Clutch Performer" in self.traits["mental"]:
            self.overall += 2
        if "Lazy" in self.traits["training"]:
            self.overall -= 1
        self.normalize_ratings()

    def _serialize_value(self, value):
        if value is None or isinstance(value, (str, int, float, bool)):
            return value
        if isinstance(value, (datetime.date, datetime.datetime)):
            return value.isoformat()
        if isinstance(value, dict):
            serialized = {}
            for key, item in value.items():
                if isinstance(key, (str, int, float, bool)) or key is None:
                    key_out = key
                else:
                    key_out = str(key)
                serialized[key_out] = self._serialize_value(item)
            return serialized
        if isinstance(value, (list, tuple, set)):
            return [self._serialize_value(item) for item in value]
        if hasattr(value, "to_dict"):
            try:
                payload = value.to_dict()
            except Exception:
                payload = None
            if payload is not None:
                return self._serialize_value(payload)
        if is_dataclass(value) and not isinstance(value, type):
            try:
                payload = asdict(value)
            except Exception:
                payload = None
            if payload is not None:
                return self._serialize_value(payload)
        try:
            return str(value)
        except Exception:
            return "<unprintable>"

    def to_dict(self):
        contract_payload = None
        contract = getattr(self, "contract", None)
        if contract is not None:
            if hasattr(contract, "to_dict"):
                try:
                    contract_payload = contract.to_dict()
                except Exception:
                    contract_payload = None
            if contract_payload is None and is_dataclass(contract) and not isinstance(contract, type):
                try:
                    contract_payload = asdict(contract)
                except Exception:
                    contract_payload = None
            if contract_payload is None:
                contract_payload = str(contract)
        return {
            "id": self.id,
            "name": self.name,
            "position": self.position,
            "age": self.age,
            "dob": self._serialize_value(self.dob),
            "college": self.college,
            "birth_location": self.birth_location,
            "jersey_number": self.jersey_number,
            "overall": self.overall,
            "potential": self.potential,
            "pot": self.pot,
            "fatigue": self.fatigue,
            "skills": self._serialize_value(self.skills),
            "traits": self._serialize_value(self.traits),
            "notes": self._serialize_value(self.notes),
            "contract": self._serialize_value(contract_payload),
            "experience": self.experience,
            "injuries": self._serialize_value(self.injuries),
            "injury_status": getattr(self, "injury_status", "healthy"),
            "injury_name": self._serialize_value(getattr(self, "injury_name", None)),
            "injury_start_date": self._serialize_value(getattr(self, "injury_start_date", None)),
            "injury_end_date": self._serialize_value(getattr(self, "injury_end_date", None)),
            "retired_due_to_injury": self.retired_due_to_injury,
            "retired": self.retired,
            "morale": self.morale,
            "playtime_history": self._serialize_value(self.playtime_history),
            "career_stats": self._serialize_value(self.career_stats),
            "season_stats": self._serialize_value(self.season_stats),
            "on_injured_reserve": self.on_injured_reserve,
            "snaps": self.snaps,
            "snap_counts": self._serialize_value(self.snap_counts),
            "milestones_hit": self._serialize_value(list(self.milestones_hit)),
            "motivation": self.motivation,
            "loyalty": self.loyalty,
            "ambition": self.ambition,
            "greed": self.greed,
            "passion": self.passion,
            "resilience": self.resilience,
            "position_specific": self._serialize_value(self.position_specific),
            "attributes": self._serialize_value(
                {
                    "core": self.attributes.core,
                    "position_specific": self.attributes.position_specific,
                }
            ),
            "attribute_xp": self._serialize_value(getattr(self, "attribute_xp", {})),
            "active_injury_effects": self._serialize_value(self.active_injury_effects),
            "injury_severity": self._serialize_value(getattr(self, "injury_severity", None)),
            "rookie_year": self.rookie_year,
            "drafted_by": self.drafted_by,
            "draft_round": self.draft_round,
            "draft_pick": self.draft_pick,
            "hidden_caps": self._serialize_value(self.hidden_caps),
            "scouted_potential": self._serialize_value(self.scouted_potential),
            "last_attribute_values": self._serialize_value(self.last_attribute_values),
            "no_growth_years": self._serialize_value(self.no_growth_years),
            "progress_history": self._serialize_value(self.progress_history),
            "dna": self._serialize_value(self.dna.to_dict() if hasattr(self, "dna") else None),
        }

    @staticmethod
    def from_dict(data):
        player = Player(
            name=data["name"],
            position=data["position"],
            age=data.get("age", 22),
            dob=(
                datetime.datetime.fromisoformat(data["dob"])
                if isinstance(data["dob"], str)
                else data["dob"]
            ),
            college=data["college"],
            birth_location=data["birth_location"],
            jersey_number=data["jersey_number"],
            overall=data["overall"],
            potential=data.get("potential"),
        )
        player.fatigue = data.get("fatigue", 0)
        pot_value = data.get("pot")
        if pot_value is None:
            pot_value = data.get("potential")
        if pot_value is None:
            pot_value = data.get("pot_rating")
        player.skills = data.get("skills", {})
        player.traits = data.get(
            "traits", {"training": [], "mental": [], "gameday": [], "media": []}
        )
        player.notes = data.get("notes", [])
        player.contract = data.get("contract", None)
        player.experience = data.get("experience", 0)
        player.injuries = data.get("injuries", [])
        from gridiron_gm_pkg.simulation.systems.player.injury_status import normalize_injury_status

        status = data.get("injury_status", "healthy")
        player.injury_status = normalize_injury_status(status)
        player.injury_name = data.get("injury_name")
        injury_start_date = data.get("injury_start_date")
        if isinstance(injury_start_date, str):
            try:
                injury_start_date = datetime.date.fromisoformat(injury_start_date)
            except ValueError:
                injury_start_date = None
        elif isinstance(injury_start_date, datetime.datetime):
            injury_start_date = injury_start_date.date()
        player.injury_start_date = injury_start_date
        injury_end_date = data.get("injury_end_date")
        if isinstance(injury_end_date, str):
            try:
                injury_end_date = datetime.date.fromisoformat(injury_end_date)
            except ValueError:
                injury_end_date = None
        elif isinstance(injury_end_date, datetime.datetime):
            injury_end_date = injury_end_date.date()
        player.injury_end_date = injury_end_date
        injury_severity = data.get("injury_severity")
        if injury_severity is not None:
            try:
                injury_severity = int(injury_severity)
            except (TypeError, ValueError):
                injury_severity = None
        player.injury_severity = injury_severity
        player.weeks_out = data.get("weeks_out", 0)
        player.retired_due_to_injury = data.get("retired_due_to_injury", False)
        player.retired = data.get("retired", False)
        player.morale = data.get("morale", 100)
        player.playtime_history = data.get("playtime_history", [])
        player.career_stats = data.get("career_stats", {})
        player.season_stats = data.get("season_stats", {})
        player.on_injured_reserve = data.get("on_injured_reserve", False)
        player.is_injured = data.get("is_injured", False)
        player.snap_counts = data.get("snap_counts", {})
        player.milestones_hit = set(data.get("milestones_hit", []))
        player.active_injury_effects = data.get("active_injury_effects", [])
        for attr in CORE_ATTRIBUTES:
            val = data.get(attr)
            if val is not None:
                player.attributes.core[attr] = val
        player.motivation = data.get("motivation")
        player.loyalty = data.get("loyalty")
        player.ambition = data.get("ambition")
        player.greed = data.get("greed")
        player.passion = data.get("passion")
        player.resilience = data.get("resilience")
        player.attributes.position_specific = data.get(
            "position_specific", player.attributes.position_specific
        )
        player.active_injury_effects = data.get("active_injury_effects", {})
        player.hidden_caps = data.get("hidden_caps", {})
        player.scouted_potential = data.get("scouted_potential", {})
        player.last_attribute_values = data.get("last_attribute_values", {})
        player.no_growth_years = data.get("no_growth_years", {})
        player.progress_history = data.get("progress_history", {})
        attrs_data = data.get("attributes")
        if attrs_data:
            core = attrs_data.get("core", {})
            pos = attrs_data.get("position_specific", {})
            player.attributes = AttributeSet(core=core, position_specific=pos)
        else:
            player.attributes.position_specific = data.get(
                "position_specific", player.attributes.position_specific
            )
            player.attributes = AttributeSet(
                core=player.init_core_attributes(),
                position_specific=player.attributes.position_specific,
            )

        dna_data = data.get("dna")
        if dna_data:
            player.dna = PlayerDNA.from_dict(dna_data)
        else:
            player.dna = PlayerDNA.generate_random_dna(player.position)
        if pot_value is None:
            if hasattr(player.dna, "potential"):
                pot_value = getattr(player.dna, "potential")
            elif hasattr(player.dna, "pot"):
                pot_value = getattr(player.dna, "pot")
        if pot_value is None:
            pot_value = generate_pot(player.position, player.age)
        pot_value = max(int(round(pot_value)), int(round(getattr(player, "overall", 0))))
        player.pot = min(pot_value, 99)
        if not player.hidden_caps:
            player.hidden_caps = {}
            for attr in player.get_relevant_attribute_names():
                info = player.dna.attribute_caps.get(attr)
                if info:
                    player.hidden_caps[attr] = info.get("hard_cap", 20)
                else:
                    player.hidden_caps[attr] = 20
        if not player.scouted_potential:
            player.scouted_potential = {
                attr: player.dna.scouted_caps.get(attr, player.hidden_caps.get(attr, 20))
                for attr in player.get_relevant_attribute_names()
            }
        player.normalize_ratings()
        xp_payload = data.get("attribute_xp", {})
        xp_map = {}
        if isinstance(xp_payload, dict):
            for key, val in xp_payload.items():
                try:
                    xp_map[str(key)] = int(round(val))
                except (TypeError, ValueError):
                    continue
        player.attribute_xp = xp_map
        from gridiron_gm_pkg.simulation.systems.player.attribute_xp import (
            apply_xp_to_player,
            sync_xp_from_rating,
        )

        sync_xp_from_rating(player)
        apply_xp_to_player(player)
        return player


def ensure_player_objects(team):
    from gridiron_gm_pkg.simulation.entities.player import (
        Player,
    )  # adjust import as needed

    new_roster = []
    for p in team.roster:
        if isinstance(p, dict):
            new_roster.append(Player.from_dict(p))
        else:
            new_roster.append(p)
    team.roster[:] = new_roster
