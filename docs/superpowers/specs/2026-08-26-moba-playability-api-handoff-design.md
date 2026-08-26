# MOBA Playability + API Handoff Design

**Date:** 2026-08-26  
**Repo:** `Realm_Moba_Demo`  
**Status:** Approved for implementation planning  

## Problem

The MOBA demo map has scaffolding (`MapScript`, `MinionSpawner`, `LanePathfinder`, `TowerAI`) but is not playable end-to-end:

- Enemy/friendly castles are visible (expected as towers/bases), but the local hero is hard to find.
- Minion waves spawn near bases, then stand still instead of walking three lanes.
- Supervisor wants a polished MOBA demo: towers that attack, rhythmic waves, hero select, XP/gold, shop, and upgradable abilities — while discovering missing core MapAPIs.
- Core Realm / MapAPI code is **not** in this repo. Engine work must be delivered as an in-repo handoff pack (suggested APIs + AI prompts), not as direct engine edits here.

## Goals

1. **Playability (priority A):** Controllable Team 1 hero, moving three-lane minion waves, towers that aggro hostiles.
2. **MOBA loop (B, best-effort):** Hero select → XP/levels → gold/shop → abilities, scripted against proposed APIs with graceful fallbacks when `lib/` lacks them.
3. **API handoff:** `docs/realm-api-handoff/` with concrete suggested edits and copy-paste prompts for an AI working in the core Realm/MapAPI repo.

## Non-goals

- Dual-map editor / merge-conflict UX for terrain images (separate brainstorm later).
- Balloon TD / new map content.
- Implementing or forking the core engine inside this repo.
- Hard-coding `Realm_Moba_Demo` into the generic engine (maps must opt in).
- Multiple heroes beyond one fully functional demo hero (Kevin); Chad remains an alternate when select UI exists.

## Approach

**Map-first playability, then layered MOBA loop** with capability-gated APIs and an in-repo handoff pack.

## Architecture

```
Initialize
  → register MOBA mode if API exists (else skip)
  → claim towers (ice_castle_1) by lane/base coords / Player
  → hero select UI if API exists, else spawn Kevin at Team 1 spawn + SelectUnit

Update @ 30Hz
  → TowerAI (range + cooldown attacks + VFX)
  → MinionSpawner (30s waves × 3 lanes × both teams)
  → LanePathfinder (waypoint AttackMove; interrupt to attack hostiles)
  → HeroProgression adapter (XP/gold/shop/abilities if APIs exist; else no-op)
```

Two deliverables:

| Deliverable | Location |
|---|---|
| Map playability | `metadata.json`, `terrain.json` / named coords, `terrain_pathing.png`, map scripts |
| API handoff | `docs/realm-api-handoff/` (expand/refine `CORE_API_MOBA_PROMPT.md`) |

## Playability fixes (A)

### Castles / towers

Seeing eight `ice_castle_1` buildings is **correct** for this demo: bases + lane towers. Ownership uses `IUnit.Player` and named coordinates (`Base_Team1`, `Base_Team2`, mid-tower regions when present). `TowerAI` continues to drive range/cooldown attacks.

### Standing minions (primary hypothesis)

`fantasy_warrior_unit_1` and `orc_warrior_7` currently lack combat/move stats in `metadata.json` (`MaxHp`, `Damage`, `Range`, `Speed`, etc.). Add demo-appropriate stats for hero, minion, and castle unit types so `AttackMove` and combat can function.

### Hero visibility

Root cause is unknown (never spawned vs wrong place vs camera). Keep spawn near Team 1 base; prefer `Spawn_Team1` when that pad is the intended spawn. Always `SetUnitOwner(0)` and `SelectUnit`. After stats land, re-verify spawn vs camera vs height. Use camera-focus API only if already available in `lib/`.

### Lane movement

- Waypoints: top via `Top_Corner`, mid via `Middle`, bot via `Bot_Corner`, between `Base_Team1` and `Base_Team2`.
- `LanePathfinder`: `AttackMove` to next waypoint; if hostile in scan radius, attack until clear, then resume.
- If units still freeze after stats: repaint `terrain_pathing.png` so lanes cross the stream. Image merges are manual (git cannot merge pathing images cleanly).
- Align named coords with tower positions; add/fix `Mid_Team1_Tower` / `Mid_Team2_Tower` if ownership zones are wrong.
- Never fall back to `(0,0,0)` when a coordinate is missing — skip that lane or synthesize a mid point between bases.

### Success criteria for A

- Player can select/control a hero near Team 1 base.
- Every ~30s both teams spawn 3 lanes × 3 minions that walk toward the enemy base along distinct routes.
- Towers attack hostiles in range.

## MOBA loop (B) + graceful fallbacks

Map script targets APIs described in the handoff / `CORE_API_MOBA_PROMPT.md`, but must not hard-crash if `lib/` is older.

| Feature | When API present | Fallback |
|---|---|---|
| Hero select | Modal → spawn chosen unit at Team 1 | Auto-spawn Kevin (`fantasy_warrior_unit_1`) |
| XP / levels | Kill XP → level events → upgrade choices | No level UI; hero still fights |
| Gold / shop | Kill gold + shop UI (`demo_blade`, `demo_armor`, `demo_boots`) | No shop |
| Abilities | Three demo abilities + bar + upgrades | Auto-attack only |
| Mode register | Opt-in e.g. `RegisterGameMode("moba_demo")` | Skip; RTS defaults unchanged |

### Adapter

Route optional features through a small capability adapter so missing methods are not scattered try/catch soup. Exact WASM-safe detection (feature flags, version query, or optional host exports) is specified in the handoff prompts — map code must not assume reflection across the WASM boundary.

### Data flow (when APIs exist)

`OnUnitDied` → award XP/gold once to nearby allied hero → level threshold → show upgrade choices → apply ability level or stat bump. Shop purchase → validate gold/ownership → apply item modifiers.

Demo hero abilities (when core lands): `demo_power_strike`, `demo_arc_burst`, `demo_rally`.

## API handoff pack

**Path:** `docs/realm-api-handoff/`

| File | Purpose |
|---|---|
| `README.md` | How to use: open core repo → paste prompt → copy updated `lib/` back |
| `00-overview.md` | Goals, opt-in MOBA mode, non-goals |
| `01-suggested-api.md` | Concrete C# signatures + models |
| `02-prompt-core-engine.md` | Copy-paste prompt for core engine AI |
| `03-prompt-mapapi-bridge.md` | MapAPI + WASM bridge + regenerate XML/`lib` |
| `04-acceptance-checklist.md` | Engine tests (select once, waves, towers, XP once, shop, abilities, non-MOBA unchanged) |
| `05-map-integration-notes.md` | Exact map-script calls after core lands; refresh `lib/` |

`docs/realm-api-handoff/README.md` is the authoritative handoff entry point. Keep root `CORE_API_MOBA_PROMPT.md` as a short stub that links into that folder (content is folded into `00-overview.md` / `01-suggested-api.md` / prompts so it is not duplicated long-term).

Constraints carried into every prompt:

- Additive, backward-compatible MapAPI.
- No hard-coded references to this map in the engine.
- WASM-safe events (no managed delegates across the boundary).
- UI-driven select/shop/level-up (chat may be debug fallback only).

## Error handling

- Null spawn → log/chat once; do not repeatedly spam.
- Missing coordinate → do not path through origin; skip or synthesize.
- Dead towers/minions removed from update lists.
- Rewards applied once per death id when progression APIs exist.

## Testing / verification

**This repo:**

- Manual playtest checklist for A (hero, waves, towers).
- Map project builds against current `lib/`.
- Handoff docs are complete and reviewable without the core repo.

**Core repo (via handoff checklist):** automated acceptance tests from `CORE_API_MOBA_PROMPT.md` (hero select once, wave composition, lane routes, tower targeting, XP/gold once, shop validation, ability validation, non-MOBA maps unchanged).

## Editor notes

Only fix editor awkwardness that blocks A (pathing/coords). Dual-map simultaneous open for merge copy/paste is explicitly out of scope.

## Components (map repo)

| Component | Responsibility |
|---|---|
| `MapScript` | Init teams, towers, hero (select or fallback), tick subsystems |
| `TowerAI` | Tower targeting / cooldown / attack + VFX |
| `MinionSpawner` | Wave timing and per-lane spawn |
| `LanePathfinder` | Waypoint follow + combat interrupt |
| `HeroProgression` (new adapter) | Optional XP/gold/shop/ability wiring |
| `metadata.json` | Unit combat/move stats and models |
| Terrain assets | Coords, pathing for stream crossings |
| `docs/realm-api-handoff/` | Suggested APIs + AI prompts for core |

## Open follow-ups (not this design)

- Merge/push workflow with `speige/Realm_Moba_Demo` / Isabel map updates (pathing image likely needs manual redo).
- Dual-map editor for easier terrain merge conflicts.
- BTD map after MOBA demo is polished.
