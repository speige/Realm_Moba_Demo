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
TEAM1_MINION_STATS = {
    "MaxHp": 280,
    "Damage": 18,
    "Range": 1.5,
    "Armor": 1,
    "Speed": 3.8,
    "AttackCooldown": 1.2,
    "GoldBounty": 25,
    "XpBounty": 40,
}

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

    team1_minion = units.get("moba_minion_team1")
    if not team1_minion:
        errors.append("missing unit moba_minion_team1")
    else:
        if team1_minion.get("ModelPath") != "fantasy_warrior_unit_1.glb":
            errors.append("moba_minion_team1 must reuse fantasy warrior model")
        if team1_minion.get("IsHero", False):
            errors.append("moba_minion_team1 must not be a hero")
        for key, expected in TEAM1_MINION_STATS.items():
            if team1_minion.get(key) != expected:
                errors.append(f"moba_minion_team1 {key} must be {expected}")

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

    castles = [u for u in terrain.get("Units", []) if u.get("UnitId") == "ice_castle_1"]
    if len(castles) != 8:
        errors.append(f"expected 8 authored castles, found {len(castles)}")
    if any(castle.get("Player") not in (0, 1) for castle in castles):
        errors.append("all authored castles must have player 0 or 1 ownership")
    east_tower = min(
        castles,
        key=lambda castle: (castle["PosX"] - 90.15632) ** 2 + (castle["PosZ"] - 0.61254096) ** 2,
        default=None,
    )
    if east_tower is None or east_tower.get("Player") != 1:
        errors.append("east tower near (90.16, 0.61) must remain player 1")

    sources = {
        name: (ROOT / name).read_text()
        for name in ("MapScript.cs", "MinionSpawner.cs", "LanePathfinder.cs")
    }
    if "GetTowerOwner(unit, api)" not in sources["MapScript.cs"]:
        errors.append("tower claiming must consider authored unit ownership")
    if '"moba_minion_team1"' not in sources["MinionSpawner.cs"]:
        errors.append("Team 1 waves must spawn moba_minion_team1")
    if "HorizontalDistanceSquared" not in sources["LanePathfinder.cs"]:
        errors.append("waypoint arrival must use horizontal distance")

    if errors:
        print("FAIL:")
        for e in errors:
            print(f"  - {e}")
        return 1
    print("PASS: playability prerequisites present")
    return 0

if __name__ == "__main__":
    sys.exit(main())
