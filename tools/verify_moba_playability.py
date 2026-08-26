#!/usr/bin/env python3
"""Fail if MOBA playability prerequisites are missing."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
REQUIRED_STATS = ("MaxHp", "Damage", "Range", "Speed", "AttackCooldown")
REQUIRED_COORDS = (
    "Base_Team1", "Base_Team2", "Top_Corner", "Bot_Corner", "Middle",
    "Spawn_Team1", "Spawn_Team2", "Mid_Team1_Tower", "Mid_Team2_Tower",
)

def main() -> int:
    meta = json.loads((ROOT / "metadata.json").read_text())
    terrain = json.loads((ROOT / "terrain.json").read_text())
    errors = []

    units = {u["UnitId"]: u for u in meta.get("CustomUnits", [])}
    buildings = {b["UnitId"]: b for b in meta.get("CustomBuildings", [])}

    for uid in ("fantasy_warrior_unit_1", "orc_warrior_7"):
        u = units.get(uid)
        if not u:
            errors.append(f"missing unit {uid}")
            continue
        for key in REQUIRED_STATS:
            if key not in u or u[key] in (None, 0):
                errors.append(f"{uid} missing/zero {key}")

    castle = buildings.get("ice_castle_1")
    if not castle:
        errors.append("missing ice_castle_1")
    else:
        for key in ("MaxHp", "Damage", "Range", "Armor"):
            if key not in castle or castle[key] in (None, 0):
                errors.append(f"ice_castle_1 missing/zero {key}")

    names = {c["Name"] for c in terrain.get("Coordinates", [])}
    for name in REQUIRED_COORDS:
        if name not in names:
            errors.append(f"missing coordinate {name}")

    if errors:
        print("FAIL:")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("PASS: playability prerequisites present")
    return 0

if __name__ == "__main__":
    sys.exit(main())
