# Overview and Compatibility Boundary

## Goal

Provide generic engine capabilities and UI/event plumbing that let an opted-in map implement a playable MOBA loop:

- modal hero selection with one deterministic result;
- shop and inventory purchases;
- per-hero XP, levels, gold rewards, and level-up choices;
- registered abilities with cooldowns, costs, targeting, casting, and upgrades;
- UI controls for selection, shop, upgrades, and the ability bar;
- safe operation from a trimmed/AOT `net10.0` `wasi-wasm` map script.

The map remains responsible for its rules, content registration, wave timing, lanes, rewards, and spawn decisions. The shared engine must not contain demo-specific rules.

## Compatibility requirements

- All APIs are additive and backward compatible. Preserve existing signatures, defaults, and semantics.
- Existing maps that do not opt in must receive no MOBA UI, automatic spawning, progression, or changed input behavior.
- Activation is explicit, such as `RegisterGameMode("moba_demo")`, and/or occurs only when a map invokes a corresponding setup API.
- Do not hard-code `Realm_Moba_Demo`, its unit IDs, item IDs, ability IDs, coordinates, or rules into generic engine code.
- Keep gameplay decisions and mutations server-authoritative. Client UI submits requests; the host validates and applies them.
- Use integer handles, polling, serializable commands, or engine-owned event queues across WASM. Never marshal managed delegates such as `Action<>` across the boundary.
- Return explicit success/failure states for expected errors instead of throwing through the WASM boundary.
- Hero selection, shop, level-up, and ability controls must be normal game UI. Chat commands may remain debug-only fallbacks.
- Add an integration test proving that a non-MOBA map retains current behavior while an opted-in fixture receives the new feature set.

## Demo contract

`Realm_Moba_Demo` currently supplies the map-side gameplay:

- Team 1/local player is player `0`; Team 2/enemy is player `1`.
- Ownership and team relationships are authoritative. Do not infer hostility solely from `IUnit.IsEnemy`.
- Hero choices are Kevin (`fantasy_warrior_unit_1`) and Chad (`orc_warrior_7`).
- Towers/bases use `ice_castle_1`.
- Named coordinates include `Base_Team1`, `Base_Team2`, `Spawn_Team1`, `Spawn_Team2`, `Top_Corner`, `Middle`, `Bot_Corner`, `Mid_Team1_Tower`, and `Mid_Team2_Tower`.
- Both teams spawn three units in each of three lanes every 30 seconds: nine units per side per wave.
- The map already owns wave routing, tower targeting, hero spawning fallback, and basic gold/XP bookkeeping.

The desired post-handoff flow is:

1. The map registers `moba_demo`, item definitions, and ability definitions.
2. The map begins hero selection for player `0`.
3. `Update` polls the selection handle; the map spawns the chosen unit exactly once.
4. Death rewards add gold and per-hero XP exactly once.
5. Level thresholds present upgrade choices; the map polls and applies one result.
6. The player opens the shop through normal UI or an explicit map trigger, and the host validates transactions.
7. Registered abilities appear in the existing command/ability bar and all casts are host-validated.

## Existing behavior to preserve

Do not regress unit enumeration/radius queries, spawning and ownership, attack-move and attacks, mutable combat stats, inventory methods, existing ability/cooldown methods, unit lifecycle/combat events, camera/selection, player gold, or named-coordinate APIs. In particular, preserve current behavior of:

- `GetAllUnits`, `GetUnitsInRadius`, `SpawnUnitForPlayer`, `SetUnitOwner`, `IssueAttackMoveOrder`;
- `IUnit.Attack`, `Damage`, `Range`, `Health`, `MaxHealth`, and `Experience`;
- `AddItem`, `RemoveItem`, `HasItem`, and `GetItems`;
- `AddUnitTypeAbility`, `CastAbility`, and cooldown APIs;
- `OnUnitCreated`, `OnUnitDied`, `OnUnitDamaged`, and `OnUnitAttacked`;
- `HasCoordinate`, `GetCoordinate`, and `IsPositionInCoordinate`.

Where a suggested API overlaps an existing one, extend or adapt the existing implementation instead of creating competing state.

## Non-goals

- Implementing or forking the core engine inside this map repository.
- Automatically converting every Realm game into a MOBA.
- Encoding this map's waves, lanes, towers, rewards, or content in shared code.
- Adding multiple fully developed heroes; one functional demo hero is sufficient.
- Replacing the terrain/pathing editor or solving binary map merge workflows.
- Building unrelated game modes or content.
