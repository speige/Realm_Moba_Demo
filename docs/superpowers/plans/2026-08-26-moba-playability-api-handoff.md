# MOBA Playability + API Handoff Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Realm_Moba_Demo playable (hero, moving three-lane waves, attacking towers), layer optional XP/gold progression on existing MapAPIs, and ship `docs/realm-api-handoff/` prompts for missing core UI APIs.

**Architecture:** Fix unit stats + named coordinates + spawn/path helpers in the map first. Keep `TowerAI` / `MinionSpawner` / `LanePathfinder`. Add a `HeroProgression` adapter that uses **only APIs present in current `lib/Realm.MapAPI.dll`** (gold, level, `OnUnitDied`, camera). Missing select/shop/ability-UI APIs stay documented in the handoff pack until `lib/` is refreshed — do not call methods that are not on the DLL (they will not compile).

**Tech Stack:** C# / `net10.0` / `wasi-wasm` map script, `Realm.MapAPI` (`lib/`), JSON metadata/terrain, Markdown handoff docs.

## Global Constraints

- Priority A (playability) before polishing B.
- Do not edit the core Realm engine in this repo; only handoff docs for core.
- Compile only against APIs that exist in `lib/Realm.MapAPI.dll` / `lib/Realm.MapAPI.xml`.
- Eight `ice_castle_1` buildings are intentional towers/bases.
- One demo hero (Kevin) is enough; Chad remains alternate for future select UI.
- Never path through `(0,0,0)` when a named coordinate is missing.
- Dual-map merge editor and BTD map are out of scope.
- Opt-in MOBA mode only; no hard-coding this map name into core (handoff constraint).
- Prefer small focused files; match existing map-script style.

---

## File Structure

| File | Responsibility |
|---|---|
| `metadata.json` | Combat/move stats for Kevin, Chad, ice_castle_1 |
| `terrain.json` | Named coords (`Mid_Team*_Tower`, tuned `Middle`, keep spawn pads) |
| `terrain_pathing.png` | Walkable lane crossings over water (manual editor fix if needed) |
| `CoordinateResolver.cs` | Safe named-coordinate lookup; never returns origin silently |
| `MinionSpawner.cs` | Wave timing + three-lane spawn using resolver |
| `LanePathfinder.cs` | Waypoint AttackMove + hostile interrupt (keep; only touch if needed) |
| `TowerAI.cs` | Tower aggro (keep; only touch if needed) |
| `MapScript.cs` | Teams, towers, hero spawn/select, wire progression |
| `HeroProgression.cs` | OnUnitDied → gold/XP/level using current APIs; stubs for future UI |
| `docs/realm-api-handoff/*` | Suggested APIs + AI prompts for core |
| `CORE_API_MOBA_PROMPT.md` | Short stub pointing at handoff folder |
| `docs/superpowers/specs/2026-08-26-moba-playability-api-handoff-design.md` | Approved design (reference only) |
| `tools/verify_moba_playability.py` | Pre/post checks for stats + coords |

---

### Task 1: Unit combat stats in metadata

**Files:**
- Modify: `metadata.json` (CustomUnits Kevin/Chad; CustomBuildings ice_castle_1)
- Create: `tools/verify_moba_playability.py`
- Test: run `python3 tools/verify_moba_playability.py`

**Interfaces:**
- Consumes: map schema fields `MaxHp`, `Damage`, `Range`, `Armor`, `Speed`, `AttackCooldown`, `PopCost`, `AttackType`, `ArmorType`, `GoldBounty`, `XpBounty`, `IsHero`
- Produces: units that can move and fight under engine defaults

- [ ] **Step 1: Write the failing verification script**

Create `tools/verify_moba_playability.py`:

```python
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
```

- [ ] **Step 2: Run script to verify it fails**

Run: `python3 tools/verify_moba_playability.py`

Expected: `FAIL:` with missing stats for both warriors and castle, and missing `Mid_Team1_Tower` / `Mid_Team2_Tower`.

- [ ] **Step 3: Add combat stats to metadata**

On `fantasy_warrior_unit_1` (Kevin) add (keep existing Animation/Model fields):

```json
"MaxHp": 600,
"Damage": 35,
"Range": 1.5,
"Armor": 3,
"Speed": 4.5,
"AttackCooldown": 1.1,
"PopCost": 1,
"AttackType": "melee",
"ArmorType": "light",
"IsHero": true,
"GoldBounty": 200,
"XpBounty": 100
```

On `orc_warrior_7` (Chad / enemy minion model) add:

```json
"MaxHp": 280,
"Damage": 18,
"Range": 1.5,
"Armor": 1,
"Speed": 3.8,
"AttackCooldown": 1.2,
"PopCost": 1,
"AttackType": "melee",
"ArmorType": "light",
"GoldBounty": 25,
"XpBounty": 40
```

On `ice_castle_1` add:

```json
"MaxHp": 2500,
"Damage": 45,
"Range": 12,
"Armor": 8,
"Speed": 0,
"AttackCooldown": 1.25,
"AttackType": "ranged",
"ArmorType": "building",
"GoldBounty": 150,
"XpBounty": 120
```

Note: Kevin and Chad currently share minion/hero models; that is acceptable for the demo. Do not invent separate minion unit types in this task.

- [ ] **Step 4: Re-run script**

Run: `python3 tools/verify_moba_playability.py`

Expected: still `FAIL` only for missing `Mid_Team*_Tower` coords (stats pass). That is OK for this task — Task 2 clears coords.

- [ ] **Step 5: Commit**

```bash
git add metadata.json tools/verify_moba_playability.py
git commit -F- <<'EOF'
Add combat stats so MOBA units can move and fight.

Warriors and castles had visuals only; without Speed/Damage/HP waves cannot path or trade.
EOF
```

---

### Task 2: Named coordinates for mid towers and mid lane

**Files:**
- Modify: `terrain.json` (`Coordinates` array only — do not reshuffle Units unless required)
- Test: `python3 tools/verify_moba_playability.py`

**Interfaces:**
- Consumes: existing tower positions (mid-lane towers ≈ Team1 `(-42.3, 52.1)`, Team2 `(42.4, -45.4)`; side mid towers at `z≈0`)
- Produces: `Mid_Team1_Tower`, `Mid_Team2_Tower`; `Middle` centered between bases

- [ ] **Step 1: Confirm current coords fail mid-tower checks**

Run: `python3 tools/verify_moba_playability.py`

Expected: FAIL mentioning `Mid_Team1_Tower` and/or `Mid_Team2_Tower`.

- [ ] **Step 2: Update `Middle` and add mid-tower regions**

Parse `terrain.json` with Python (file is one line). Update the `Coordinates` list:

1. Set `Middle` to a box around map mid between bases, e.g.:

```json
{"Name": "Middle", "MinX": -8, "MinZ": -8, "MaxX": 8, "MaxZ": 8}
```

2. Add:

```json
{"Name": "Mid_Team1_Tower", "MinX": -50, "MinZ": 44, "MaxX": -34, "MaxZ": 60}
{"Name": "Mid_Team2_Tower", "MinX": 34, "MinZ": -54, "MaxX": 50, "MaxZ": -38}
```

Keep existing `Base_Team*`, `Top_Corner`, `Bot_Corner`, `Spawn_Team*`. Prefer editing via a short Python snippet so JSON stays valid:

```python
import json
from pathlib import Path
p = Path("terrain.json")
d = json.loads(p.read_text())
coords = {c["Name"]: c for c in d["Coordinates"]}
coords["Middle"] = {"Name": "Middle", "MinX": -8, "MinZ": -8, "MaxX": 8, "MaxZ": 8}
coords["Mid_Team1_Tower"] = {"Name": "Mid_Team1_Tower", "MinX": -50, "MinZ": 44, "MaxX": -34, "MaxZ": 60}
coords["Mid_Team2_Tower"] = {"Name": "Mid_Team2_Tower", "MinX": 34, "MinZ": -54, "MaxX": 50, "MaxZ": -38}
d["Coordinates"] = list(coords.values())
p.write_text(json.dumps(d, separators=(",", ":")))
```

- [ ] **Step 3: Run verification**

Run: `python3 tools/verify_moba_playability.py`

Expected: `PASS: playability prerequisites present`

- [ ] **Step 4: Commit**

```bash
git add terrain.json
git commit -F- <<'EOF'
Add mid-tower coordinates and center the mid-lane waypoint.

Ownership and lane pathing need Mid_Team regions and a true map-center Middle.
EOF
```

---

### Task 3: Safe coordinate resolver + minion spawn fix

**Files:**
- Create: `CoordinateResolver.cs`
- Modify: `MinionSpawner.cs`
- Modify: `MapScript.cs` (only if it still uses raw `GetCoordinate` without checks — hero spawn moves to Task 4)

**Interfaces:**
- Consumes: `IGameAPI.HasCoordinate`, `GetCoordinate`
- Produces:

```csharp
public static class CoordinateResolver
{
    public static bool TryGetCenter(IGameAPI api, string name, out Vector3 center);
    public static Vector3 RequireCenterOrFallback(IGameAPI api, string name, Vector3 fallback);
    public static bool TryBuildLaneWaypoints(
        IGameAPI api,
        Vector3 start,
        string cornerName,
        Vector3 destination,
        out IReadOnlyList<Vector3> waypoints);
}
```

- [ ] **Step 1: Add `CoordinateResolver.cs`**

```csharp
namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public static class CoordinateResolver
{
    public static bool TryGetCenter(IGameAPI api, string name, out Vector3 center)
    {
        if (api.HasCoordinate(name))
        {
            center = api.GetCoordinate(name).Center;
            return true;
        }

        center = default;
        return false;
    }

    public static Vector3 RequireCenterOrFallback(IGameAPI api, string name, Vector3 fallback)
    {
        return TryGetCenter(api, name, out var center) ? center : fallback;
    }

    public static bool TryBuildLaneWaypoints(
        IGameAPI api,
        Vector3 start,
        string cornerName,
        Vector3 destination,
        out IReadOnlyList<Vector3> waypoints)
    {
        if (!TryGetCenter(api, cornerName, out var corner))
        {
            waypoints = Array.Empty<Vector3>();
            return false;
        }

        waypoints = new[] { start, corner, destination };
        return true;
    }
}
```

- [ ] **Step 2: Rewrite `MinionSpawner` to skip bad lanes**

Replace `GetCenter` usage so missing coords do not become `Vector3.Zero`. Full file:

```csharp
namespace Realm.Maps;

using System.Numerics;
using Realm.MapAPI;

public sealed class MinionSpawner
{
    private const float WaveInterval = 30f;
    private const float SpawnOffset = 1.5f;
    private readonly List<LanePathfinder> _minions = [];
    private float _elapsed = WaveInterval;

    public void Update(IGameAPI api, float delta)
    {
        _elapsed += delta;
        if (_elapsed >= WaveInterval)
        {
            _elapsed -= WaveInterval;
            SpawnWave(api);
        }

        foreach (var minion in _minions.ToArray())
            minion.Update(api, delta);

        _minions.RemoveAll(minion => !minion.IsAlive);
    }

    private void SpawnWave(IGameAPI api)
    {
        if (!CoordinateResolver.TryGetCenter(api, "Base_Team1", out var baseTeam1) ||
            !CoordinateResolver.TryGetCenter(api, "Base_Team2", out var baseTeam2))
        {
            api.BroadcastMessage("MinionSpawner: Base_Team1/Base_Team2 missing; skipping wave");
            return;
        }

        SpawnTeam(api, 0, baseTeam1, baseTeam2);
        SpawnTeam(api, 1, baseTeam2, baseTeam1);
    }

    private void SpawnTeam(IGameAPI api, int player, Vector3 start, Vector3 destination)
    {
        var spawnPad = player == 0 ? "Spawn_Team1" : "Spawn_Team2";
        var spawnStart = CoordinateResolver.RequireCenterOrFallback(
            api,
            spawnPad,
            start + (player == 0 ? new Vector3(5f, 0f, -5f) : new Vector3(-5f, 0f, 5f)));

        var laneCorners = new[] { "Top_Corner", "Middle", "Bot_Corner" };
        for (var lane = 0; lane < laneCorners.Length; lane++)
        {
            if (!CoordinateResolver.TryBuildLaneWaypoints(
                    api, start, laneCorners[lane], destination, out var waypoints))
            {
                api.BroadcastMessage($"MinionSpawner: skipping lane {laneCorners[lane]}");
                continue;
            }

            for (var member = 0; member < 3; member++)
            {
                var offset = new Vector3((member - 1) * SpawnOffset, 0, (lane - 1) * SpawnOffset);
                var unit = api.SpawnUnit(
                    player == 0 ? "fantasy_warrior_unit_1" : "orc_warrior_7",
                    spawnStart + offset,
                    player == 1,
                    true);
                if (unit == null)
                    continue;

                api.SetUnitOwner(unit, player);
                _minions.Add(new LanePathfinder(unit, waypoints));
            }
        }
    }
}
```

- [ ] **Step 3: Build the map project**

Run: `dotnet build MapScript.csproj -v q`

Expected: build succeeds (or existing environment WASM tooling errors unrelated to these files — if compile errors in new code, fix before continuing).

- [ ] **Step 4: Commit**

```bash
git add CoordinateResolver.cs MinionSpawner.cs
git commit -F- <<'EOF'
Route minion waves through safe lane coordinates.

Avoid silent origin fallbacks that pin waves in the wrong place or freeze pathing.
EOF
```

---

### Task 4: Hero spawn, select, and camera

**Files:**
- Modify: `MapScript.cs`

**Interfaces:**
- Consumes: `SpawnUnit` / `SpawnUnitForPlayer`, `SetUnitOwner`, `SelectUnit`, `PanCameraTo`, `CoordinateResolver`, `BroadcastMessage` / `SendMessageToPlayer`
- Produces: Team 1 Kevin at `Spawn_Team1` (fallback `Base_Team1`), selected, camera panned

- [ ] **Step 1: Replace hero init block in `MapScript.Initialize`**

Keep team setup and tower claiming. Replace the hero spawn block with:

```csharp
SpawnPlayerHero(api);
```

Add private methods:

```csharp
private void SpawnPlayerHero(IGameAPI api)
{
    if (!CoordinateResolver.TryGetCenter(api, "Spawn_Team1", out var spawn) &&
        !CoordinateResolver.TryGetCenter(api, "Base_Team1", out spawn))
    {
        api.BroadcastMessage("Hero spawn failed: Spawn_Team1/Base_Team1 missing");
        return;
    }

    // Prefer player-aware spawn when available.
    _hero = api.SpawnUnitForPlayer("fantasy_warrior_unit_1", spawn, 0)
            ?? api.SpawnUnit("fantasy_warrior_unit_1", spawn, false, true);

    if (_hero == null)
    {
        api.BroadcastMessage("Hero spawn returned null for fantasy_warrior_unit_1");
        return;
    }

    api.SetUnitOwner(_hero, 0);
    api.SelectUnit(_hero);
    api.PanCameraTo(_hero.Position, 0.35f);
    api.SendMessageToPlayer(0, "Kevin ready. Waves every 30s.");
}
```

Ensure `GetTowerOwner` still checks `Mid_Team1_Tower` / `Mid_Team2_Tower` (already present).

- [ ] **Step 2: Build**

Run: `dotnet build MapScript.csproj -v q`

Expected: success (same tooling caveat as Task 3).

- [ ] **Step 3: Commit**

```bash
git add MapScript.cs
git commit -F- <<'EOF'
Spawn and focus the Team 1 hero at the spawn pad.

Select + camera pan make Kevin findable when the model actually appears.
EOF
```

---

### Task 5: Hero progression adapter (current APIs only)

**Files:**
- Create: `HeroProgression.cs`
- Modify: `MapScript.cs`

**Interfaces:**
- Consumes: `IGameAPI.OnUnitDied`, `GetPlayerGold`, `SetPlayerGold`, `SetUnitLevel`, `IUnit.Experience`, `GetUnitById`
- Produces:

```csharp
public sealed class HeroProgression
{
    public HeroProgression(IUnit hero);
    public void Attach(IGameAPI api);
    public void Update(IGameAPI api, float delta); // reserved; may no-op
}
```

Future UI APIs (`ShowHeroSelection`, shop modal, level-up choices) are **not** called here until present in `lib/`.

- [ ] **Step 1: Match `OnUnitDied` signature from XML, then implement `HeroProgression.cs`**

Read `lib/Realm.MapAPI.xml` for `E:Realm.MapAPI.IGameAPI.OnUnitDied` and `IUnit.Experience` / `Id` property names. Adapt the handler to the real delegate. Use this shape when the event is `(IUnit dead, IUnit? killer)`:

```csharp
namespace Realm.Maps;

using Realm.MapAPI;

public sealed class HeroProgression
{
    private const float XpPerLevel = 100f;
    private const float KillGold = 25f;
    private const float KillXp = 40f;

    private readonly IUnit _hero;
    private readonly HashSet<int> _rewardedDeaths = [];
    private IGameAPI? _api;
    private float _trackedXp;
    private bool _attached;

    public HeroProgression(IUnit hero)
    {
        _hero = hero;
    }

    public void Attach(IGameAPI api)
    {
        if (_attached)
            return;

        _attached = true;
        _api = api;
        api.SetPlayerGold(0, MathF.Max(api.GetPlayerGold(0), 300f));
        api.OnUnitDied += HandleUnitDied;
    }

    public void Update(IGameAPI api, float delta)
    {
        // Reserved for polling-based UI results after core handoff lands.
    }

    private void HandleUnitDied(IUnit dead, IUnit? killer)
    {
        if (_api == null || dead == null || !_rewardedDeaths.Add(dead.Id))
            return;
        if (dead.Player == _hero.Player || _hero.IsDead)
            return;

        // Demo rule: reward Team 1 hero for any enemy death (minion/tower).
        _api.SetPlayerGold(0, _api.GetPlayerGold(0) + KillGold);
        _trackedXp += KillXp;
        var level = 1 + (int)(_trackedXp / XpPerLevel);
        _api.SetUnitLevel(_hero, level);
    }
}
```

If the event uses unit ids instead of `IUnit`, resolve with `_api.GetUnitById` and keep the same reward rules. Keep a private `_trackedXp` field even if `Experience` is writable, so level math does not depend on host XP semantics.

- [ ] **Step 2: Wire into `MapScript`**

```csharp
private HeroProgression? _progression;

// after successful hero spawn:
_progression = new HeroProgression(_hero);
_progression.Attach(api);

// in Update:
_progression?.Update(api, delta);
```

- [ ] **Step 3: Build**

Run: `dotnet build MapScript.csproj -v q`

Expected: success. Fix signature mismatches against XML before committing.

- [ ] **Step 4: Commit**

```bash
git add HeroProgression.cs MapScript.cs
git commit -F- <<'EOF'
Add hero gold/XP progression using existing MapAPIs.

Gives a playable reward loop before shop/select UI exists in core.
EOF
```

---

### Task 6: Pathing verification (stream crossings)

**Files:**
- Possibly modify: `terrain_pathing.png` (via Realm pathing editor — binary)
- Optional note in: `docs/realm-api-handoff/05-map-integration-notes.md` (created in Task 7)

**Interfaces:**
- Consumes: playable build from Tasks 1–5
- Produces: walkable top/mid/bot routes across water

- [ ] **Step 1: Playtest movement**

Launch the map in Realm. Checklist:

1. Kevin visible near Team 1 spawn; selected; camera near him.
2. ~30s later, both teams spawn minions.
3. Minions leave the base (not frozen).
4. Top / mid / bot groups diverge toward corners/middle then enemy base.

- [ ] **Step 2: If minions still freeze at water**

Open the pathing editor and paint ground-walkable pathing across the three lane stream crossings. Save `terrain_pathing.png`. Git cannot merge this image — treat as full-file replace.

- [ ] **Step 3: Commit only if pathing changed**

```bash
git add terrain_pathing.png
git commit -F- <<'EOF'
Open pathing across MOBA lane stream crossings.

Waves were spawning but could not leave the river banks.
EOF
```

If no change needed, skip commit and record “pathing OK” in the playtest notes of Task 8.

---

### Task 7: API handoff pack

**Files:**
- Create: `docs/realm-api-handoff/README.md`
- Create: `docs/realm-api-handoff/00-overview.md`
- Create: `docs/realm-api-handoff/01-suggested-api.md`
- Create: `docs/realm-api-handoff/02-prompt-core-engine.md`
- Create: `docs/realm-api-handoff/03-prompt-mapapi-bridge.md`
- Create: `docs/realm-api-handoff/04-acceptance-checklist.md`
- Create: `docs/realm-api-handoff/05-map-integration-notes.md`
- Modify: `CORE_API_MOBA_PROMPT.md` → short stub linking here

**Interfaces:**
- Consumes: content of current `CORE_API_MOBA_PROMPT.md` + design spec
- Produces: single entry point `docs/realm-api-handoff/README.md`

- [ ] **Step 1: Write `README.md`**

Include:

1. Open the core Realm / MapAPI repository (not this map repo).
2. Paste `02-prompt-core-engine.md` into an AI session in that repo.
3. Then paste `03-prompt-mapapi-bridge.md` for WASM/bridge/`lib` regeneration.
4. Copy updated `Realm.MapAPI.dll` + `.xml` into this map’s `lib/`.
5. Run checklist in `04-acceptance-checklist.md`.
6. Wire new calls per `05-map-integration-notes.md` (hero select UI, shop UI, ability UI).

- [ ] **Step 2: Write `00-overview.md`**

Fold scope/compatibility/non-goals from existing `CORE_API_MOBA_PROMPT.md` (opt-in mode, no hard-coded map name, additive APIs, WASM-safe events).

- [ ] **Step 3: Write `01-suggested-api.md`**

Include concrete signatures (adapt delegates to WASM-safe polling/events):

```csharp
// Prefer event/poll form over Action<> callbacks across WASM:
int BeginHeroSelection(int playerIndex, string title, IReadOnlyList<HeroChoice> choices, string defaultChoiceId, float timeoutSeconds);
bool TryGetHeroSelectionResult(int handle, out string selectedChoiceId);
void CloseHeroSelection(int handle);

void RegisterGameMode(string modeId);
void RegisterItem(ItemDefinition definition);
bool PurchaseItem(IUnit buyer, string itemId);
bool SellItem(IUnit seller, string itemId);
void OpenShop(int playerIndex);
void CloseShop(int playerIndex);

void AddUnitExperience(IUnit unit, float amount);
int GetUnitLevel(IUnit unit);
void ShowLevelUpChoices(IUnit hero, IReadOnlyList<UpgradeChoice> choices);
bool TryGetLevelUpChoice(IUnit hero, out string choiceId);

void RegisterAbility(AbilityDefinition definition);
bool UpgradeAbility(IUnit unit, string abilityId);
```

Include demo content ids: items `demo_blade` / `demo_armor` / `demo_boots`; abilities `demo_power_strike` / `demo_arc_burst` / `demo_rally`.

- [ ] **Step 4: Write prompts `02` and `03`**

`02-prompt-core-engine.md`: full copy-paste prompt — implement host gameplay + UI for select/shop/level-up/abilities; cite `01-suggested-api.md`; require non-MOBA maps unchanged.

`03-prompt-mapapi-bridge.md`: full copy-paste prompt — public MapAPI surface, WASM imports/exports, regenerate XML/DLL, no managed delegates across boundary, deliverables to copy into `Realm_Moba_Demo/lib`.

- [ ] **Step 5: Write `04-acceptance-checklist.md` and `05-map-integration-notes.md`**

Checklist items from design/spec (select once, waves 9/side, lanes, XP once, shop validation, abilities, non-MOBA unchanged).

Integration notes: after `lib/` refresh, change `MapScript` to call `BeginHeroSelection`, register items/abilities in `HeroProgression.Attach`, open shop on chat or hotkey only if UI API requires map trigger.

- [ ] **Step 6: Replace root `CORE_API_MOBA_PROMPT.md` with stub**

```markdown
# Core/API Handoff

The authoritative MOBA API handoff lives in [`docs/realm-api-handoff/`](docs/realm-api-handoff/README.md).

Start at the README, then use the numbered prompts in that folder when working in the core Realm / MapAPI repository.
```

- [ ] **Step 7: Commit**

```bash
git add docs/realm-api-handoff CORE_API_MOBA_PROMPT.md
git commit -F- <<'EOF'
Add in-repo Realm API handoff prompts for MOBA UI features.

Core engine is not in this repo; prompts carry suggested APIs and acceptance checks.
EOF
```

---

### Task 8: Playtest checklist + final verification

**Files:**
- Create: `docs/superpowers/plans/playtest-moba-demo.md` (short checklist results template)

**Interfaces:**
- Consumes: Tasks 1–7 complete
- Produces: recorded pass/fail for A; notes for B fallbacks

- [ ] **Step 1: Run automated verify**

Run: `python3 tools/verify_moba_playability.py`

Expected: `PASS`

- [ ] **Step 2: Build**

Run: `dotnet build MapScript.csproj -v q`

Expected: success or documented tooling-only failure.

- [ ] **Step 3: Manual playtest and fill checklist**

Create `docs/superpowers/plans/playtest-moba-demo.md`:

```markdown
# MOBA Demo Playtest

Date:

- [ ] Castles visible as towers/bases (expected)
- [ ] Kevin spawns at Team 1; selected; camera nearby
- [ ] Minions spawn ~30s both sides
- [ ] Minions move (not frozen at spawn)
- [ ] Three lanes diverge (top/mid/bot)
- [ ] Towers attack hostiles in range
- [ ] Enemy death grants gold (check gold UI / message)
- [ ] Hero level increases after enough XP
- [ ] No shop/select UI yet (expected until core handoff lands)
```

Mark each item during playtest.

- [ ] **Step 4: Commit checklist**

```bash
git add docs/superpowers/plans/playtest-moba-demo.md
git commit -F- <<'EOF'
Record MOBA demo playtest checklist for playability sign-off.
EOF
```

---

## Spec coverage (self-review)

| Spec requirement | Task |
|---|---|
| Combat stats / standing minions | 1, 3, 6 |
| Hero visible / selectable | 4 |
| Castles are towers (expected) | documented; TowerAI kept |
| Three-lane waves | 2, 3, 6 |
| Tower attacks | existing TowerAI + Task 1 castle stats |
| Graceful B (gold/XP now; UI later) | 5, 7 |
| `docs/realm-api-handoff/` | 7 |
| No origin fallback | 3 |
| Pathing/stream | 6 |
| Out of scope merge editor / BTD | omitted |

## Placeholder / consistency notes

- `OnUnitDied` handler signature must be matched to `lib/Realm.MapAPI.xml` at implementation time (Task 5).
- Do not call `ShowHeroSelection` / `RegisterGameMode` until those methods exist on the referenced DLL.
- Kevin currently also used as Team 1 minion model — acceptable for demo; separate minion types are YAGNI unless playtest demands it.
