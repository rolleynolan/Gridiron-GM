import datetime
from uuid import uuid4
from dataclasses import dataclass, field
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
    "tackling",
    "catching",
    "return_skill",
]


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
    def consistency(self) -> Optional[int]:
        return self._get_core_attr("consistency")

    @consistency.setter
    def consistency(self, value: int) -> None:
        self._set_core_attr("consistency", value)

    @property
    def return_skill(self) -> Optional[int]:
        return self._get_core_attr("return_skill")

    @return_skill.setter
    def return_skill(self, value: int) -> None:
        self._set_core_attr("return_skill", value)
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
        self.potential = None

        # Initialize attribute containers
        core_attrs = self.init_core_attributes()
        pos_attrs = self.init_position_attributes()

        self.position_specific = pos_attrs
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
        self.weeks_out = 0
        self.retired_due_to_injury = False
        self.retired = False
        self.on_injured_reserve = False
        self.is_injured = False
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
        self.dna = PlayerDNA.generate_random_dna(self.position)
        self.mutations = [m.name.lower() for m in self.dna.mutations]

        self.generate_caps()

    def init_core_attributes(self):
        """Return baseline attribute mapping common to all players."""
        core = {attr: None for attr in CORE_ATTRIBUTES}
        core["stamina"] = 80
        core["tackling"] = 40
        core["catching"] = 40
        core["return_skill"] = 20
        return core

    def init_position_attributes(self):
        position = self.position.upper()
        attrs = []

        if position in ["QB"]:
            attrs = [
                "throw_power",
                "throw_velocity",
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
                "route_running_short",
                "throw_power",
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
                "carry_security",
                "elusiveness",
                "break_tackle",
                "trucking",

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
                "block_footwork",
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
        attrs.update(self.attributes.core)
        attrs.update(self.attributes.position_specific)
        return attrs

    def get_relevant_attribute_names(self) -> List[str]:
        """Return list of attribute names relevant to this player's position."""
        names = list(self.attributes.core.keys()) + list(self.position_specific.keys())
        for base in ["tackling", "catching"]:
            if base not in names:
                names.append(base)
        return names

    def generate_caps(self) -> None:
        """Initialize hidden and scouted caps using the player's DNA."""
        relevant = self.get_relevant_attribute_names()
        self.hidden_caps = {}
        self.scouted_potential = {}
        for attr in relevant:
            cap_info = self.dna.attribute_caps.get(attr, {}) if hasattr(self, "dna") else {}
            cur = cap_info.get("current", 20)
            hard_cap = cap_info.get("hard_cap", 20)
            self.hidden_caps[attr] = hard_cap
            self.scouted_potential[attr] = self.dna.scouted_caps.get(attr, hard_cap) if hasattr(self, "dna") else hard_cap
            if attr in self.attributes.core:
                self.attributes.core[attr] = cur
            else:
                self.attributes.position_specific[attr] = cur

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
        base *= (100 - self.stamina) / 100

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
        self.injuries.append(injury)
        self.injury_history.append(injury)
        self.weeks_out = injury.weeks_out
        self.is_injured = True

    def recover_one_week(self):
        if self.weeks_out > 0:
            if "Quick Recovery" in self.traits.get("physical", []):
                self.weeks_out = max(1, int((self.weeks_out - 1) * 0.90))
            else:
                self.weeks_out -= 1
            if self.weeks_out == 0:
                self.injuries.clear()
                self.is_injured = False

    def get_effective_attribute(self, attr: str):
        """Return attribute value adjusted for any active injury effects."""
        base = None
        if attr in self.position_specific:
            base = self.position_specific.get(attr)
        elif hasattr(self, attr):
            base = getattr(self, attr)
        impact = self.active_injury_effects.get(attr, 0)
        if base is None:
            return None
        return base + impact

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
        # Ensure overall remains within valid bounds
        self.overall = max(0, min(self.overall, 100))

    def get_effective_attribute(self, attr: str):
        """Return the attribute value factoring in active injury penalties."""
        if attr in self.attributes.core:
            base = self.attributes.core.get(attr)
        elif attr in self.attributes.position_specific:
            base = self.attributes.position_specific.get(attr)
        else:
            base = getattr(self, attr, None)

        if base is None:
            return None

        effects = getattr(self, "active_injury_effects", {})
        penalty = 0
        if isinstance(effects, dict):
            penalty = effects.get(attr, 0)
        else:
            for eff in effects:
                if isinstance(eff, dict) and eff.get("attribute") == attr:
                    penalty += eff.get("change", 0)

        return base + penalty

    def to_dict(self):
        return {
            "id": self.id,
            "name": self.name,
            "position": self.position,
            "age": self.age,
            "dob": self.dob.isoformat() if hasattr(self.dob, "isoformat") else self.dob,
            "college": self.college,
            "birth_location": self.birth_location,
            "jersey_number": self.jersey_number,
            "overall": self.overall,
            "potential": self.potential,
            "fatigue": self.fatigue,
            "skills": self.skills,
            "traits": self.traits,
            "notes": self.notes,
            "contract": self.contract,
            "experience": self.experience,
            "injuries": [i for i in self.injuries],
            "weeks_out": self.weeks_out,
            "retired_due_to_injury": self.retired_due_to_injury,
            "retired": self.retired,
            "morale": self.morale,
            "playtime_history": self.playtime_history,
            "career_stats": self.career_stats,
            "season_stats": self.season_stats,
            "on_injured_reserve": self.on_injured_reserve,
            "is_injured": self.is_injured,
            "snaps": self.snaps,
            "snap_counts": self.snap_counts,
            "milestones_hit": list(self.milestones_hit),
            "motivation": self.motivation,
            "loyalty": self.loyalty,
            "ambition": self.ambition,
            "greed": self.greed,
            "passion": self.passion,
            "resilience": self.resilience,
            "position_specific": self.position_specific,
            "attributes": {
                "core": self.attributes.core,
                "position_specific": self.attributes.position_specific,
            },
            "active_injury_effects": self.active_injury_effects,
            "rookie_year": self.rookie_year,
            "drafted_by": self.drafted_by,
            "draft_round": self.draft_round,
            "draft_pick": self.draft_pick,
            "hidden_caps": self.hidden_caps,
            "scouted_potential": self.scouted_potential,
            "last_attribute_values": self.last_attribute_values,
            "no_growth_years": self.no_growth_years,
            "progress_history": self.progress_history,
            "dna": self.dna.to_dict() if hasattr(self, "dna") else None,
            "mutations": self.mutations,
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
        player.skills = data.get("skills", {})
        player.traits = data.get(
            "traits", {"training": [], "mental": [], "gameday": [], "media": []}
        )
        player.notes = data.get("notes", [])
        player.contract = data.get("contract", None)
        player.experience = data.get("experience", 0)
        player.injuries = data.get("injuries", [])
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
        player.position_specific = data.get(
            "position_specific", player.position_specific
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
            player.position_specific = pos
        else:
            player.position_specific = data.get(
                "position_specific", player.position_specific
            )
            player.attributes = AttributeSet(
                core=player.init_core_attributes(),
                position_specific=player.position_specific,
            )

        dna_data = data.get("dna")
        if dna_data:
            player.dna = PlayerDNA.from_dict(dna_data)
        else:
            player.dna = PlayerDNA.generate_random_dna(player.position)
        player.mutations = data.get(
            "mutations",
            [m.name.lower() for m in getattr(player.dna, "mutations", [])],
        )
        if not player.hidden_caps or not player.scouted_potential:
            player.generate_caps()
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
    team.roster = new_roster
