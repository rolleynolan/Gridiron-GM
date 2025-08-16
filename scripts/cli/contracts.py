from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys

ROOT_DIR = Path(__file__).resolve().parents[2]
if str(ROOT_DIR) not in sys.path:
    sys.path.insert(0, str(ROOT_DIR))

from engine.cap.calc import team_cap_sheet

DATA_DIR = ROOT_DIR / "data"
CAP_DIR = DATA_DIR / "cap"


def cmd_cap_sheet(args: argparse.Namespace) -> None:
    sheet = team_cap_sheet(args.team, args.year)
    CAP_DIR.mkdir(parents=True, exist_ok=True)
    out_path = CAP_DIR / f"capsheet_{args.year}.json"
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(sheet, f, indent=2)
    print(out_path)


def main() -> None:
    parser = argparse.ArgumentParser(prog="gg")
    sub = parser.add_subparsers(dest="cmd", required=True)

    cap_sheet = sub.add_parser("cap-sheet")
    cap_sheet.add_argument("--team", required=True)
    cap_sheet.add_argument("--year", type=int, required=True)
    cap_sheet.set_defaults(func=cmd_cap_sheet)

    for name in ["sign", "cut", "trade"]:
        p = sub.add_parser(name)
        p.set_defaults(func=lambda args, n=name: print(f"{n} command not implemented"))

    args = parser.parse_args()
    args.func(args)


if __name__ == "__main__":
    main()
