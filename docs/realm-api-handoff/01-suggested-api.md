# Suggested Public API

These signatures describe the required behavior, not a demand to duplicate equivalent APIs already present in the core repository. Reuse established naming, serialization, and result conventions where possible. The critical constraint is that the public surface and WASM bridge use serializable values, handles, polling, or engine-owned queues rather than managed callbacks.

## `IGameAPI` surface

```csharp
// Explicit opt-in; has no effect on maps that do not call it.
void RegisterGameMode(string modeId);

// Hero selection: one terminal result per handle.
int BeginHeroSelection(
    int playerIndex,
    string title,
    IReadOnlyList<HeroChoice> choices,
    string defaultChoiceId,
    float timeoutSeconds);
bool TryGetHeroSelectionResult(int handle, out string selectedChoiceId);
void CloseHeroSelection(int handle);

// Items and shop.
void RegisterItem(ItemDefinition definition);
bool PurchaseItem(IUnit buyer, string itemId);
bool SellItem(IUnit seller, string itemId);
IReadOnlyList<ItemDefinition> GetAvailableItems(int playerIndex);
void OpenShop(int playerIndex);
void CloseShop(int playerIndex);

// Per-unit progression and level-up UI.
void AddUnitExperience(IUnit unit, float amount);
float GetUnitExperience(IUnit unit);
int GetUnitLevel(IUnit unit);
void ShowLevelUpChoices(IUnit hero, IReadOnlyList<UpgradeChoice> choices);
bool TryGetLevelUpChoice(IUnit hero, out string choiceId);

// Registered abilities and per-unit levels.
void RegisterAbility(AbilityDefinition definition);
void AddUnitTypeAbility(string unitTypeId, string abilityId);
int GetAbilityLevel(IUnit unit, string abilityId);
bool UpgradeAbility(IUnit unit, string abilityId);
bool CastAbility(IUnit caster, string abilityId, Vector3 targetPosition);
```

If the existing API already exposes a `void CastAbility`, preserve it and add a distinct `TryCastAbility` or result-returning command rather than changing the existing signature. Likewise, preserve current `SetUnitLevel`, inventory, gold, and cooldown behavior.

## Serializable models

The exact representation may follow repository conventions, but every field crossing WASM must have stable serialization and validation.

```csharp
public sealed record HeroChoice(
    string Id,
    string DisplayName,
    string UnitTypeId,
    string Description,
    string? IconId = null);

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string Description,
    float CostGold,
    float DamageBonus = 0,
    float MaxHealthBonus = 0,
    float ArmorBonus = 0,
    float SpeedBonus = 0,
    float CooldownReduction = 0,
    bool Stackable = false,
    int MaxStackCount = 1,
    string? IconId = null);

public enum UpgradeChoiceKind
{
    Ability,
    Stat
}

public sealed record UpgradeChoice(
    string Id,
    string DisplayName,
    string Description,
    UpgradeChoiceKind Kind,
    string TargetId,
    float Value = 0,
    string? IconId = null);

public enum AbilityTargetingMode
{
    Unit,
    Point,
    Self,
    Area
}

public sealed record AbilityDefinition(
    string Id,
    string DisplayName,
    string Description,
    string? IconId,
    int MaxLevel,
    IReadOnlyList<float> ManaCostByLevel,
    IReadOnlyList<float> CooldownByLevel,
    IReadOnlyList<float> EffectValueByLevel,
    AbilityTargetingMode TargetingMode,
    float Range,
    float AreaRadius,
    int GridX,
    int GridY);
```

If unit-target casting needs a unit identifier in addition to a position, add a serializable cast request model or a unit-target overload. Do not resolve a unit target from an arbitrary point if that would make selection nondeterministic.

## Required semantics

### Selection and level-up polling

- Validate that choice IDs are non-empty and unique and that the default exists.
- Scope UI and input to the owning player.
- A selection becomes terminal exactly once, whether chosen by input or timeout.
- `TryGetHeroSelectionResult` returns `false` while pending and `true` with the same immutable result after completion until closed.
- `CloseHeroSelection` is idempotent and prevents further interaction.
- Invalid handles fail safely without throwing across WASM.
- Level-up choices require the same ownership, exactly-once, timeout/default, and idempotent-consumption guarantees. If the two-method level-up shape cannot carry timeout/default configuration, use an integer handle analogous to hero selection.

### Shop

- Registration rejects duplicate/invalid item IDs deterministically.
- Purchase and sell requests validate player ownership, item availability, price, current gold, inventory capacity, stack limits, and sale ownership on the host.
- Gold never becomes negative. A failed request causes no partial mutation.
- Stat modifiers apply once on purchase and are fully removed on sell.
- The UI shows price, affordability, stack/inventory state, and explicit failure feedback.
- Purchase/sell observations exposed to map scripts use serializable event records or polling, not delegates across WASM.

### Progression

- XP belongs to a unit, not globally to a player.
- Level transitions occur on the host and emit at most one transition per crossed threshold.
- A large XP grant may cross multiple thresholds without losing transitions.
- Applying a level-up choice is exactly once and rejects duplicate submissions.
- Map-side death processing can identify a death uniquely so gold and XP are not awarded twice.

### Abilities

- Ability levels are per unit and bounded by `MaxLevel`.
- The ability bar shows icon, key/grid position, mana cost, cooldown, level, and locked/disabled state.
- The host validates caster ownership, alive state, learned level, target type and hostility, range, mana, and cooldown.
- A rejected cast spends no mana and starts no cooldown.
- A successful cast applies its effect once and emits serializable cast/damage observations for UI, VFX, audio, and map logic.

## Demo content IDs

The engine must treat these as map-registered data, never built-ins:

- Items: `demo_blade` (damage), `demo_armor` (max health and armor), `demo_boots` (movement speed).
- Abilities: `demo_power_strike` (targeted damage), `demo_arc_burst` (point/area damage), `demo_rally` (self or nearby-friendly utility buff).

Exact demo tuning belongs in `Realm_Moba_Demo`; tests may register small fixture definitions using these IDs.
