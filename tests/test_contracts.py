from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT_DIR = Path(__file__).resolve().parents[1]
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

from engine.models.contracts import ContractDTO
from engine.cap.calc import cap_hit, dead_cap_on_cut, team_cap_sheet


def load_sample() -> ContractDTO:
    sample_path = Path(__file__).parent / "golden" / "contracts" / "sample.json"
    data = json.loads(sample_path.read_text())
    return ContractDTO(**data)


def test_cap_hit_and_dead_cap() -> None:
    contract = load_sample()
    data_round = contract.model_dump()
    assert data_round.get("voidable") is True
    hits = [cap_hit(t) for t in contract.terms]
    assert hits == [10_000_000, 11_000_000, 12_000_000, 13_000_000]
    assert dead_cap_on_cut(contract, 2025) == 15_000_000


def test_team_cap_sheet_under_cap(tmp_path) -> None:
    data_dir = ROOT_DIR / "data"
    data_dir.mkdir(exist_ok=True)
    league_path = data_dir / "league_state.json"
    sample = load_sample()
    league = {
        "league_settings": {"cap": 200_000_000},
        "teams": [
            {
                "abbr": "DAL",
                "players": [
                    {"name": "Player One", "contract": sample.model_dump()}
                ],
            }
        ],
    }
    league_path.write_text(json.dumps(league))
    sheet = team_cap_sheet("DAL", 2024)
    assert sheet["total"] <= league["league_settings"]["cap"]
