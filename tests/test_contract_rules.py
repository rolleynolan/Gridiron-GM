import datetime

from gridiron_gm_pkg.simulation.entities.player import Contract, Player
from gridiron_gm_pkg.simulation.entities.team import Team
from gridiron_gm_pkg.simulation.rules.contract_rules import (
    cap_summary,
    validate_contract_offer,
    validate_team_finances,
)


def _player(name: str, salary: int) -> Player:
    player = Player(name, "QB", 25, datetime.date(2000, 1, 1), "U", "USA", 1, 70)
    player.contract = Contract(years=2, salary_per_year=salary, guaranteed=salary)
    return player


def test_cap_summary_counts_each_player_once_across_roster_locations():
    team = Team("Cap", "Test", "CAP")
    starter = _player("Starter", 100)
    reserve = _player("Reserve", 50)
    team.roster.append(starter)
    team.ir_list.append(starter)
    team.practice_squad.append(reserve)
    team.salary_cap = 200

    assert cap_summary(team) == {"salary_cap": 200, "payroll": 150, "cap_space": 50}


def test_contract_offer_rejects_cap_overage_without_mutating_player():
    team = Team("Cap", "Test", "CAP")
    player = _player("Starter", 100)
    team.roster.append(player)
    team.salary_cap = 150

    result = validate_contract_offer(team, player, Contract(years=3, salary_per_year=175))

    assert result["ok"] is False
    assert {error["code"] for error in result["errors"]} == {"salary_cap_exceeded"}
    assert player.contract.salary_per_year == 100


def test_team_finance_validation_reports_bad_contracts_and_cap_overage():
    team = Team("Cap", "Test", "CAP")
    team.salary_cap = 100
    player = _player("Starter", 150)
    player.contract.guaranteed = 999
    team.roster.append(player)

    codes = {error["code"] for error in validate_team_finances(team)}

    assert {"guarantee_exceeds_value", "salary_cap_exceeded"}.issubset(codes)


def test_team_cap_configuration_survives_serialization():
    team = Team("Cap", "Test", "CAP")
    team.salary_cap = 123
    team.payroll = 45

    restored = Team.from_dict(team.to_dict())

    assert restored.salary_cap == 123
    assert restored.payroll == 45
