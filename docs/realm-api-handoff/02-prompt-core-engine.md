# Copy-Paste Prompt: Core Engine and UI

Paste everything below this line into an AI session rooted in the core Realm repository.

---

Implement the host-side gameplay services and normal game-client UI needed by the `Realm_Moba_Demo` API handoff. First inspect this repository's architecture, existing MapAPI implementations, command/event transport, UI framework, unit/inventory/ability systems, generated-code workflow, and tests. Reuse existing systems and conventions instead of creating parallel state.

The companion API proposal is `Realm_Moba_Demo/docs/realm-api-handoff/01-suggested-api.md`. If that file is not available in this checkout, use these capability groups:

- explicit opt-in game-mode registration;
- handle-based, polling-safe hero selection;
- registered items plus host-authoritative purchase/sell and shop UI;
- per-unit XP/levels plus level-up choice UI;
- registered abilities, per-unit upgrades, validated casting, and ability-bar UI.

## Compatibility boundary

- Make all changes additive and backward compatible.
- Do not hard-code `Realm_Moba_Demo`, `moba_demo`, its unit/content IDs, coordinates, lane rules, rewards, or wave logic into generic engine behavior.
- A map that does not register a mode or invoke these setup APIs must have exactly its current UI, spawning, input, and gameplay behavior.
- Preserve existing API signatures and semantics. Extend existing inventory, gold, progression, ability, command, and event systems where they overlap.
- Keep all state changes server-authoritative. The client displays state and submits commands; the host validates and mutates.
- Do not use chat as the shipped hero-select, shop, level-up, or ability interface. Chat may remain a debug fallback.

## Implement host services

1. Hero selection
   - Accept player, title, choices, a valid default choice, and timeout.
   - Return a stable integer handle.
   - Permit interaction only from the owning player.
   - Complete exactly once by player choice or timeout, expose one immutable result, reject a second submission, and support idempotent close.
   - Do not spawn a hero automatically; the map consumes the result and decides what to spawn.

2. Items and shop
   - Register map-defined item data: identity, display fields, price, icon, stat modifiers, stackability, and stack limit.
   - Open/close the shop for one player and show item prices, affordability, inventory/stack state, and failure feedback.
   - Validate buyer ownership, registration/availability, nonnegative sufficient gold, inventory capacity, and stack limits.
   - Apply purchase atomically, apply each modifier once, and reverse it correctly on sell. Failed transactions must have no partial effects.
   - Expose purchase/sell outcomes through the repository's serializable command/event mechanism.

3. Unit progression and level-up choices
   - Store XP and level per unit and integrate with existing unit state rather than a second source of truth.
   - Process configured level thresholds and stat growth on the host.
   - Emit each crossed level transition once, including multiple thresholds crossed by one XP grant.
   - Present choices only to the hero's owning player; include timeout/default behavior.
   - Accept and apply one valid choice exactly once, rejecting duplicate or stale submissions.
   - Make unique death identity available so maps can prevent duplicate XP/gold rewards.

4. Abilities
   - Register map-defined ability data: ID/display/icon, max level, per-level mana/cooldown/effect values, target mode, range/radius, and command-bar grid position.
   - Attach registered abilities to unit types and track learned level per unit.
   - Render abilities in the existing command/ability bar with level, mana, cooldown, key/grid placement, and locked/disabled state.
   - Validate on the host: player/caster ownership, alive state, learned level, target mode, target ownership/hostility, range, mana, and cooldown.
   - On rejection, spend no mana and start no cooldown. On success, apply the effect once and emit events usable by VFX/audio/map logic.
   - Support a deterministic unit target representation; do not guess a target from a point when multiple units overlap.

## Implement client UI

- A modal hero picker with title, description/icon when supplied, timeout indication, and one-click/confirm submission.
- A shop panel with item details, purchase/sell controls, affordability and capacity feedback.
- A level-up choice panel tied to the owned hero.
- Ability-bar entries integrated into existing input and command UI.
- Ensure UI closes or becomes noninteractive after terminal results, unit invalidation, mode teardown, or disconnect.
- Keep UI state derived from authoritative host state and command results.

## Threading, lifecycle, and errors

- Follow existing simulation-thread and UI-thread boundaries.
- Clean up pending handles and player UI when a match ends or owner disconnects.
- Reject malformed IDs, duplicate registrations, invalid handles, NaN/infinite numeric values, and invalid per-level arrays deterministically.
- Expected validation failures should return explicit results, not crash the simulation.
- Keep selection and transaction ordering deterministic.

## Tests

Add focused automated tests for:

- one hero result per handle, second submission rejected, timeout chooses default, only owner can select, and close is idempotent;
- item ownership, cost, inventory/stack capacity, no negative gold, atomic failure, modifier apply/remove, and duplicate request handling;
- per-unit XP, exactly-once threshold events, multiple crossed levels, and duplicate level-up application rejection;
- ability ownership, learned level, mana, cooldown, range, target type/hostility, success effects, and no mutation on failure;
- UI activation only for the intended player and teardown after completion;
- an opted-in fixture receives the new features while a non-opted-in map retains current behavior and no MOBA UI/automatic spawning appears.

The complete cross-repository acceptance list is in `Realm_Moba_Demo/docs/realm-api-handoff/04-acceptance-checklist.md`.

## Deliverables

- Host gameplay/services and client UI implementation.
- Automated tests with results.
- A concise file/change summary and any public API assumptions required by the MapAPI/bridge pass.
- No map-repository edits and no demo-specific constants in the shared engine.

After this pass, run the separate `03-prompt-mapapi-bridge.md` prompt to finalize public interfaces, WASM transport, generated XML/DLL artifacts, and copy instructions.
