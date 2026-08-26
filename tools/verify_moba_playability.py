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
    "Side_Team1_Tower", "Side_Team2_Tower", "Bot_Team1_Tower", "Top_Team2_Tower",
)
MINION_STATS = {
    "MaxHp": 280,
    "Damage": 18,
    "Range": 1.5,
    "Armor": 1,
    "Speed": 3.8,
    "AttackCooldown": 1.2,
    "GoldBounty": 25,
    "XpBounty": 40,
}
HERO_STATS = {
    "MaxHp": 600,
    "Damage": 35,
    "Range": 1.5,
    "Armor": 3,
    "Speed": 4.5,
    "AttackCooldown": 1.1,
}


def main() -> int:
    meta = json.loads((ROOT / "metadata.json").read_text())
    terrain = json.loads((ROOT / "terrain.json").read_text())
    errors = []

    units = {u["UnitId"]: u for u in meta.get("CustomUnits", [])}
    buildings = {b["UnitId"]: b for b in meta.get("CustomBuildings", [])}

    for uid, model in (
        ("fantasy_warrior_unit_1", "fantasy_warrior_unit_1.glb"),
        ("orc_warrior_7", "orc_warrior_7.glb"),
    ):
        u = units.get(uid)
        if not u:
            errors.append(f"missing hero unit {uid}")
            continue
        if u.get("ModelPath") != model:
            errors.append(f"{uid} ModelPath must be {model}")
        if not u.get("IsHero"):
            errors.append(f"{uid} must be IsHero true")
        for key, expected in HERO_STATS.items():
            if u.get(key) != expected:
                errors.append(f"{uid} {key} must be {expected}")

    for uid, model in (
        ("moba_minion_team1", "fantasy_warrior_unit_1.glb"),
        ("moba_minion_team2", "orc_warrior_7.glb"),
    ):
        u = units.get(uid)
        if not u:
            errors.append(f"missing unit {uid}")
            continue
        if u.get("ModelPath") != model:
            errors.append(f"{uid} must reuse {model}")
        if u.get("IsHero", False):
            errors.append(f"{uid} must not be a hero")
        for key, expected in MINION_STATS.items():
            if u.get(key) != expected:
                errors.append(f"{uid} {key} must be {expected}")

    castle = buildings.get("ice_castle_1")
    if not castle:
        errors.append("missing ice_castle_1")
    else:
        for key in ("MaxHp", "Damage", "Range", "Armor"):
            if key not in castle or castle[key] in (None, 0):
                errors.append(f"ice_castle_1 missing/zero {key}")

    coords = {c["Name"]: c for c in terrain.get("Coordinates", [])}
    for name in REQUIRED_COORDS:
        if name not in coords:
            errors.append(f"missing coordinate {name}")

    castles = [u for u in terrain.get("Units", []) if u.get("UnitId") == "ice_castle_1"]
    if len(castles) < 9:
        errors.append(f"expected at least 9 authored castles, found {len(castles)}")
    team1_towers = [c for c in castles if c.get("Player") == 0]
    if not any(
        abs(c["PosX"] + 39.76) < 0.05 and abs(c["PosZ"] - 45.52) < 0.05
        for c in team1_towers
    ):
        errors.append("missing Team 1 tower near (-39.76, 45.52)")
    if any(castle.get("Player") not in (0, 1) for castle in castles):
        errors.append("all authored castles must have player 0 or 1 ownership")
    if any(bool(castle.get("IsEnemy")) != (castle.get("Player") == 1) for castle in castles):
        errors.append("castle IsEnemy must match Player (P1 => enemy)")

    def in_box(x: float, z: float, name: str) -> bool:
        c = coords[name]
        return c["MinX"] <= x <= c["MaxX"] and c["MinZ"] <= z <= c["MaxZ"]

    ownership_zones = [
        "Base_Team1", "Base_Team2", "Mid_Team1_Tower", "Mid_Team2_Tower",
        "Side_Team1_Tower", "Side_Team2_Tower", "Bot_Team1_Tower", "Top_Team2_Tower",
    ]
    for castle in castles:
        x, z = castle["PosX"], castle["PosZ"]
        if not any(in_box(x, z, name) for name in ownership_zones if name in coords):
            errors.append(f"castle at ({x:.1f},{z:.1f}) has no ownership zone")

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
    if '"moba_minion_team2"' not in sources["MinionSpawner.cs"]:
        errors.append("Team 2 waves must spawn moba_minion_team2")
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
