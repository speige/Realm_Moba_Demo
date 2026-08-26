# Core/API Handoff Prompt: Realm MOBA Demo

Implement the core engine and `Realm.MapAPI` support required by the map at `Realm_Moba_Demo`.

## Scope and Compatibility Boundary

This work must not change the behavior of unrelated maps or game modes.

- Add generic, backward-compatible API primitives to the shared core only when necessary.
- Keep all MOBA-specific rules, units, items, abilities, rewards, timers, and UI registration in the `Realm_Moba_Demo` map script/configuration.
- Do not automatically show hero selection, shop, level-up, or MOBA ability UI in every game.
- Activate the feature set only when the map explicitly registers a MOBA profile, for example `RegisterGameMode("moba_demo")`, or invokes the corresponding setup APIs.
- Existing maps that do not opt in must retain their current behavior and API defaults.
- Do not add hard-coded references to `Realm_Moba_Demo` in the generic engine. The map should register its own content using public APIs.
- Treat the new API surface as additive. Preserve existing method signatures and semantics.
- Add an opt-in integration test proving that a non-MOBA map receives no MOBA UI or automatic spawning, while `Realm_Moba_Demo` receives the complete feature set.

## Goal

Make this specific map type a functional, playable MOBA demo with:

- Hero selection at game start
- One selected hero for Team 1
- Three-lane minion waves
- Enemy towers that attack hostile units
- Hero XP, levels, and gold rewards
- A shop UI with item purchases
- Three abilities with cooldowns and upgrades
- A level-up choice UI
- WASM-safe map-script integration

The map script must remain responsible for game-specific configuration. The core/API must provide generic engine capabilities and UI/event plumbing.

## Existing Map Contract

The map uses these unit types:

- Hero choices: `fantasy_warrior_unit_1` (Kevin), `orc_warrior_7` (Chad)
- Tower/building: `ice_castle_1`

The map uses these named coordinates:

- `Base_Team1`
- `Base_Team2`
- `Top_Corner`
- `Bot_Corner`
- `Mid_Team1_Tower`
- `Mid_Team2_Tower`

Ownership is represented by `IUnit.Player`:

- `0`: Team 1 / local player
- `1`: Team 2 / enemy

Do not infer hostility only from `IsEnemy`; player ownership and team relationships must be authoritative.

## Required Map API Features

### 1. Hero selection

Add a map-safe API for a modal selection screen:

```csharp
int ShowHeroSelection(
    int playerIndex,
    string title,
    IReadOnlyList<HeroChoice> choices,
    Action<string> onSelected);
void CloseHeroSelection(int handle);
```

`HeroChoice` should contain at least:

- `Id`
- `DisplayName`
- `UnitTypeId`
- `Description`
- Optional portrait/icon ID

Requirements:

- Only the owning player can interact with their selection UI.
- Selection must be deterministic and fire exactly once.
- The callback must be safe from the WASM execution boundary. Prefer an engine event or integer/string selection result if direct delegate callbacks are unsafe.
- Provide a timeout/default selection option.
- Expose a way for the map script to spawn the selected hero after selection.

### 2. Shop and inventory

Add generic item definitions and purchase APIs:

```csharp
void RegisterItem(ItemDefinition definition);
bool PurchaseItem(IUnit buyer, string itemId);
bool SellItem(IUnit seller, string itemId);
IReadOnlyList<ItemDefinition> GetAvailableItems(int playerIndex);
```

`ItemDefinition` should support:

- `Id`
- `DisplayName`
- `Description`
- `CostGold`
- Stat modifiers such as damage, max health, armor, speed, and ability cooldown reduction
- Stackability and maximum stack count

Add shop UI APIs/events:

- Open/close shop for a player
- Show item list, costs, purchase state, and insufficient-gold state
- Notify map scripts of purchase/sell events
- Ensure purchases are server-authoritative and cannot spend negative gold

The first demo item set should be:

- `demo_blade`: increases damage
- `demo_armor`: increases max health and armor
- `demo_boots`: increases movement speed

### 3. Experience and level progression

Add generic hero progression support:

```csharp
int GetUnitLevel(IUnit unit);
float GetUnitExperience(IUnit unit);
void AddUnitExperience(IUnit unit, float amount);
void ConfigureLevelCurve(string curveId, ...);
```

Requirements:

- Award XP for kills and optionally assists according to map configuration.
- Track XP per hero, not per player globally.
- Emit an event when a hero reaches a level threshold.
- Apply configured level stat growth.
- Prevent XP or rewards from being awarded twice for the same death.
- Keep progression server-authoritative.

Add a level-up choice UI API/event:

```csharp
void ShowLevelUpChoices(
    IUnit hero,
    IReadOnlyList<UpgradeChoice> choices);
```

The map must receive the selected upgrade and apply it exactly once. Include a timeout/default choice.

### 4. Ability progression

Support map-configured abilities on a unit type and per-hero ability levels:

```csharp
void RegisterAbility(AbilityDefinition definition);
void AddUnitTypeAbility(string unitTypeId, string abilityId);
int GetAbilityLevel(IUnit unit, string abilityId);
bool UpgradeAbility(IUnit unit, string abilityId);
void CastAbility(IUnit caster, string abilityId, Vector3 targetPosition);
```

Each ability definition should support:

- ID and display name
- Description and icon ID
- Maximum level
- Mana cost by level
- Cooldown by level
- Damage/effect values by level
- Targeting mode: unit, point, self, or area
- Range and area radius where applicable

The first hero should expose three demo abilities:

- `demo_power_strike`: targeted damage
- `demo_arc_burst`: area damage at a point
- `demo_rally`: self/nearby friendly utility buff

Requirements:

- Put abilities in the existing command/ability bar using grid positions.
- Show cooldown, mana cost, current level, and disabled/locked state.
- Upgrade abilities through level-up choices.
- Validate caster ownership, target hostility, range, mana, cooldown, and ability level on the server.
- Emit cast and damage events for VFX/audio and map logic.

### 5. Player input and UI events

Expose safe events or commands for:

- Hero selection
- Item purchase/sell
- Level-up choice
- Ability cast
- Ability upgrade

All events must include the player index and validate that the player owns the affected hero/unit.

Use existing chat/leaderboard APIs only as compatibility fallbacks; the target implementation needs actual UI controls.

### 6. WASM bridge

For every new API:

- Add the corresponding host implementation.
- Add WASM imports/exports and serialization in the generated bridge.
- Keep signatures compatible with `net10.0`, `wasi-wasm`, trimming, and AOT.
- Avoid passing managed delegates across the unmanaged boundary.
- Use handles, polling, or engine-owned event queues where callbacks cannot be marshalled safely.
- Return explicit failure states rather than throwing across the WASM boundary.

### 7. Existing gameplay APIs to preserve

Do not regress these existing APIs:

- `GetAllUnits`
- `GetUnitsInRadius`
- `SpawnUnitForPlayer`
- `SetUnitOwner`
- `IssueAttackMoveOrder`
- `IUnit.Attack`
- `IUnit.Damage`, `Range`, `Health`, `MaxHealth`, `Experience`
- `AddItem`, `RemoveItem`, `HasItem`, `GetItems`
- `AddUnitTypeAbility`, `CastAbility`, cooldown APIs
- `OnUnitCreated`, `OnUnitDied`, `OnUnitDamaged`, `OnUnitAttacked`
- Coordinate APIs such as `HasCoordinate`, `GetCoordinate`, and `IsPositionInCoordinate`

## Acceptance Tests

Add automated tests for:

1. A player can select exactly one hero and the hero spawns at `Base_Team1`.
2. A second selection attempt is rejected after selection is complete.
3. A tower targets the closest hostile unit and respects range and cooldown.
4. Waves spawn nine units per team every 30 seconds without overlap.
5. Minions follow top, mid, and bottom waypoint routes in both directions.
6. Hostile units stop minion movement and are attacked until destroyed.
7. A hero kill awards XP and gold exactly once.
8. XP crossing a threshold emits one level-up event.
9. A level-up choice applies one upgrade and rejects duplicate application.
10. Shop purchases validate ownership, cost, inventory capacity, and gold.
11. Item stat modifiers apply and are removed correctly on sell.
12. Abilities validate level, mana, cooldown, range, and target type.
13. The full map script builds with `net10.0`, `wasi-wasm`, AOT, and trimming enabled.

## Deliverables

- Core engine implementation
- Public `Realm.MapAPI` interfaces and models
- WASM bridge changes
- UI implementation for hero selection, shop, level-up choices, and ability bar
- Automated tests
- Updated API XML/generated artifacts
- A corrected map build task that handles workspace paths containing spaces
- Brief migration notes showing the exact map-script calls and any generated files that must be copied into `Realm_Moba_Demo/lib`

Do not silently implement these as chat commands. Chat can remain a fallback for debugging, but the requested behavior for this game type is UI-driven and must be available through the normal game client.
