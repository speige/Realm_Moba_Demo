# Map Integration Notes

Apply these changes only after the refreshed `Realm.MapAPI.dll` and `Realm.MapAPI.xml` are copied into `lib/`. Compile against the final generated XML/signatures; use the core migration notes if names differ from [01-suggested-api.md](01-suggested-api.md).

## 1. Refresh and verify `lib/`

1. Replace `lib/Realm.MapAPI.dll` and `lib/Realm.MapAPI.xml` together from the same tested core build.
2. Confirm capability/version identifiers and final model constructors in the XML.
3. Run:

   ```sh
   dotnet build MapScript.csproj -v q
   python3 tools/verify_moba_playability.py
   ```

Do not leave a new DLL paired with stale XML. Do not copy generated files from this map's `bin/` or `obj/` back into `lib/`.

## 2. Add one capability adapter

Add a small map-side adapter that queries the host's explicit capability/version API once during initialization. It should expose booleans for hero selection, shop, level-up choices, and registered abilities. Do not use reflection, dynamic invocation, or broad exception handling across WASM.

If an older host lacks a capability:

- hero selection: auto-spawn Kevin as the current code does;
- shop: keep the shop unavailable;
- level-up choices: preserve current automatic level bookkeeping;
- abilities: preserve auto-attack-only gameplay.

Keep these gates in the adapter so `MapScript` and `HeroProgression` do not accumulate separate compatibility branches.

## 3. Register the mode and begin hero selection

In `MapScript.Initialize`, after team setup and before spawning the hero:

```csharp
api.RegisterGameMode("moba_demo");
_heroSelectionHandle = api.BeginHeroSelection(
    playerIndex: 0,
    title: "Choose your hero",
    choices:
    [
        new HeroChoice(
            "kevin", "Kevin", "fantasy_warrior_unit_1",
            "Durable melee fighter", "fantasy_warrior_unit_1"),
        new HeroChoice(
            "chad", "Chad", "orc_warrior_7",
            "Aggressive melee fighter", "orc_warrior_7")
    ],
    defaultChoiceId: "kevin",
    timeoutSeconds: 20f);
```

Store the handle in `MapScript`. In `Update`, poll only while no hero has been spawned:

```csharp
if (_hero == null &&
    _heroSelectionHandle >= 0 &&
    api.TryGetHeroSelectionResult(_heroSelectionHandle, out var choiceId))
{
    api.CloseHeroSelection(_heroSelectionHandle);
    _heroSelectionHandle = -1;
    SpawnPlayerHero(api, choiceId == "chad"
        ? "orc_warrior_7"
        : "fantasy_warrior_unit_1");
}
```

Refactor `SpawnPlayerHero` to accept a unit type ID while retaining:

- `Spawn_Team1`, then `Base_Team1`, as coordinate fallback;
- `SpawnUnitForPlayer(..., 0)`, then the existing spawn fallback;
- `SetUnitOwner(hero, 0)`, `SelectUnit`, and `PanCameraTo`;
- exactly one `HeroProgression` attachment after a successful spawn.

Guard spawning with explicit state so repeated polling or a late result cannot create a second hero. If hero selection is unsupported or cannot begin, call `SpawnPlayerHero(api, "fantasy_warrior_unit_1")` once.

## 4. Register items in `HeroProgression.Attach`

Register definitions once before opening the shop. Suggested demo tuning:

```csharp
api.RegisterItem(new ItemDefinition(
    Id: "demo_blade",
    DisplayName: "Demo Blade",
    Description: "+12 damage",
    CostGold: 150,
    DamageBonus: 12));

api.RegisterItem(new ItemDefinition(
    Id: "demo_armor",
    DisplayName: "Demo Armor",
    Description: "+120 max health, +3 armor",
    CostGold: 175,
    MaxHealthBonus: 120,
    ArmorBonus: 3));

api.RegisterItem(new ItemDefinition(
    Id: "demo_boots",
    DisplayName: "Demo Boots",
    Description: "+0.75 movement speed",
    CostGold: 125,
    SpeedBonus: 0.75f));
```

Treat these values as map configuration. The core engine must not define them. Keep existing starting gold initialization unless game design changes it.

The preferred client has its own shop hotkey/button. Call `OpenShop(0)` from map code only if the final UI contract requires an explicit trigger. In that case, bind a supported map input/hotkey event; a chat command may be retained only as a development fallback. Never implement purchase arithmetic in chat handlers—always call host-authoritative `PurchaseItem`/`SellItem`.

## 5. Register abilities in `HeroProgression.Attach`

Register once, then attach the IDs to the selected hero's unit type:

- `demo_power_strike`: unit-target hostile damage, maximum level 3, range 2.5, mana `[20, 25, 30]`, cooldown `[7, 6, 5]`, damage `[60, 90, 120]`.
- `demo_arc_burst`: point/area hostile damage, maximum level 3, range 8, radius 3, mana `[35, 40, 45]`, cooldown `[10, 9, 8]`, damage `[45, 70, 95]`.
- `demo_rally`: self/nearby-friendly utility buff, maximum level 3, radius 5, mana `[30, 35, 40]`, cooldown `[14, 12, 10]`, effect values `[0.10, 0.15, 0.20]`.

Assign stable command-bar grid positions. Construct `AbilityDefinition` instances using the final generated model, call `RegisterAbility` for each, and call `AddUnitTypeAbility(selectedUnitTypeId, abilityId)`. Do not register on every simulation tick.

Casting should flow through the engine ability bar and validated host cast API. Map code may consume serializable cast events for custom VFX, but must not duplicate damage/effects already applied by the host.

## 6. Replace manual XP with host progression

Keep `_rewardedDeaths` (or use the host's unique reward/death token) to guarantee exactly-once rewards. In `HandleUnitDied`:

```csharp
_api.SetPlayerGold(0, _api.GetPlayerGold(0) + KillGold);
_api.AddUnitExperience(_hero, KillXp);
```

Remove `_trackedXp`, direct `_hero.Experience` assignment, and manual `SetUnitLevel` only after confirming the host owns threshold/level updates. Reward only valid enemy deaths according to authoritative player/team relationships.

When a level transition makes an upgrade available, show choices for:

- the three abilities while below their maximum level;
- a deterministic stat fallback if every ability is maxed.

Poll `TryGetLevelUpChoice` in `HeroProgression.Update`, apply the selected upgrade once with `UpgradeAbility`, and clear/close the pending choice state according to the final API. Never show a second choice for the same level transition.

## 7. Regression checks

After wiring:

- hero selection produces exactly one owned, selected hero at Team 1;
- fallback still spawns Kevin on an older host;
- nine minions per side still spawn every 30 seconds and follow all three lanes;
- towers still target hostile units;
- repeated death events do not duplicate gold or XP;
- shop transactions and ability casts are host-authoritative;
- unavailable optional capabilities do not break map initialization or update;
- no polling path performs registration, spawning, rewards, upgrades, or effects twice.

Run the full [acceptance checklist](04-acceptance-checklist.md) before committing refreshed artifacts and map wiring.
