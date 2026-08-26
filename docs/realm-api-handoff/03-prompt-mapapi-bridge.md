# Copy-Paste Prompt: MapAPI, WASM Bridge, and Artifacts

Paste everything below this line into an AI session rooted in the core Realm / MapAPI repository after the core-engine pass.

---

Complete the public `Realm.MapAPI` surface and WASM bridge for the opt-in MOBA capabilities implemented by the core-engine pass. Inspect the actual host implementation, interface/versioning conventions, source generators, serializers, native imports/exports, trimming/AOT annotations, artifact packaging, and test fixtures before editing.

Use `Realm_Moba_Demo/docs/realm-api-handoff/01-suggested-api.md` as the behavioral contract. Adapt names to established repository conventions when necessary, but preserve these required capabilities:

- `RegisterGameMode`;
- `BeginHeroSelection`, `TryGetHeroSelectionResult`, and `CloseHeroSelection`;
- item registration, purchase/sell, available-item query, and shop open/close;
- per-unit XP and level queries plus level-up choice show/poll;
- ability registration, unit-type attachment, per-unit level/upgrade, and validated cast.

## Hard boundary rules

- Never pass `Action<>`, delegates, function pointers to managed closures, or object references across WASM.
- Use integer handles, polling, serialized request/result records, or an engine-owned event queue.
- Every value crossing the boundary must have explicit stable serialization, bounds checking, and ownership/lifetime rules.
- Expected failures must use `bool`, result codes, or serializable result records; do not propagate host exceptions through WASM.
- Changes are additive. Preserve existing public signatures and behavior. If an existing method conflicts with a proposed return type, keep it and add a clearly named result-returning method.
- Do not add demo-specific IDs or map-name checks to `Realm.MapAPI` or the bridge.
- Keep the implementation compatible with `net10.0`, `wasi-wasm`, trimming, and AOT.

## Public contracts

Expose serializable equivalents of:

- `HeroChoice`: ID, display name, unit type ID, description, optional icon.
- `ItemDefinition`: ID/display/description/icon, gold cost, damage/max-health/armor/speed/cooldown modifiers, stackability, maximum stack count.
- `UpgradeChoice`: ID/display/description/icon, choice kind, target ID, value.
- `AbilityDefinition`: ID/display/description/icon, maximum level, per-level mana/cooldown/effect values, targeting mode, range, area radius, command-bar grid position.

Validate string lengths, list sizes, duplicate IDs, enum ranges, numeric finiteness, nonnegative prices/costs/cooldowns/ranges, and per-level array lengths before allocating or mutating host state.

## Bridge behavior

1. Add host imports/exports and generated bindings for every new public operation.
2. Serialize list/model inputs without retaining map-owned managed memory after a call returns.
3. Map `IUnit` arguments to stable unit IDs/handles and validate that the referenced unit is still alive/valid at command execution.
4. For hero selection, return an integer handle. Polling must distinguish pending, completed, invalid, and closed states without throwing. A completed result remains stable until closed.
5. Give level-up polling equivalent exactly-once and timeout/default behavior; use a handle if polling by hero cannot distinguish sequential pending choices safely.
6. Make shop and ability validation outcomes observable enough for the UI/map to distinguish common rejection reasons, even if compatibility methods retain a `bool` result.
7. Ensure player index and affected unit identity are carried in all UI commands/events and revalidated by the host.
8. Bound event queues and define overflow behavior. A consumer retry must not apply a transaction, selection, upgrade, or cast twice.
9. Clean up handles, queued events, and serialized buffers at map/match teardown.

## Capability/version discovery

Provide the repository's standard feature/version query so an older host or older copied DLL can be detected without reflection across WASM. The map needs to gate optional UI features without scattered exception handling. If a capability system already exists, register distinct capabilities for hero selection, shop, level-up choices, and registered abilities.

## Tests

Add native and WASM integration tests that cover:

- model round trips, Unicode/empty/oversized strings, empty and maximum-size lists;
- invalid/closed handles and pending/completed selection states;
- timeout/default results and exactly-once result consumption/application;
- stale/invalid unit handles and wrong-player commands;
- explicit failure results without host exceptions crossing WASM;
- retries/duplicate commands causing only one purchase, upgrade, or cast;
- trimming/AOT preservation of all serialized model members and bridge entry points;
- a smoke map compiling and running under `net10.0` and `wasi-wasm`;
- unchanged behavior for existing APIs and a non-opted-in map.

Also run the complete checklist at `Realm_Moba_Demo/docs/realm-api-handoff/04-acceptance-checklist.md`.

## Generated deliverables

- Updated public `Realm.MapAPI` source and XML documentation.
- Updated WASM imports/exports, serializers, source-generated bindings, and host dispatch.
- Regenerated `Realm.MapAPI.dll` and `Realm.MapAPI.xml` suitable for replacing:
  - `Realm_Moba_Demo/lib/Realm.MapAPI.dll`
  - `Realm_Moba_Demo/lib/Realm.MapAPI.xml`
- Any other generated bridge files required by the core repository, committed according to its policy.
- Build/test commands and their results.
- Migration notes listing final method/model names, capability identifiers, failure semantics, and any deviations from `01-suggested-api.md`.

Verify the artifacts are from the tested build, not stale output. Include exact source paths for the DLL/XML so they can be copied into the map repository, and ensure build tooling correctly quotes workspace paths containing spaces.
