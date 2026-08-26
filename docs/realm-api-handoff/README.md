# Realm MOBA API Handoff

This folder is the authoritative handoff for adding generic, opt-in MOBA UI and gameplay plumbing to the core Realm engine and `Realm.MapAPI`. The core code is not in this map repository.

## Workflow

1. Open the core Realm / MapAPI repository, not `Realm_Moba_Demo`.
2. Read [00-overview.md](00-overview.md) and [01-suggested-api.md](01-suggested-api.md).
3. Paste [02-prompt-core-engine.md](02-prompt-core-engine.md) into an AI session rooted in the core repository. Review and test the host gameplay and UI implementation.
4. Then paste [03-prompt-mapapi-bridge.md](03-prompt-mapapi-bridge.md) into an AI session in the same repository. Review the public API, WASM bridge, serialization, and regenerated artifacts.
5. Copy the resulting `Realm.MapAPI.dll` and `Realm.MapAPI.xml` into `Realm_Moba_Demo/lib/`, replacing the matching old artifacts.
6. Run [04-acceptance-checklist.md](04-acceptance-checklist.md) in the core repository and against the demo map.
7. Wire hero selection, shop, level-up, and ability calls into this map using [05-map-integration-notes.md](05-map-integration-notes.md).
8. Build this map for `net10.0` / `wasi-wasm` and playtest it before distributing the refreshed `lib/`.

## Document map

- [00-overview.md](00-overview.md): scope, compatibility boundary, gameplay contract, and non-goals.
- [01-suggested-api.md](01-suggested-api.md): concrete WASM-safe interfaces and data models.
- [02-prompt-core-engine.md](02-prompt-core-engine.md): copy-paste implementation prompt for host gameplay and UI.
- [03-prompt-mapapi-bridge.md](03-prompt-mapapi-bridge.md): copy-paste implementation prompt for MapAPI, WASM, and artifact generation.
- [04-acceptance-checklist.md](04-acceptance-checklist.md): automated and manual acceptance criteria.
- [05-map-integration-notes.md](05-map-integration-notes.md): exact follow-up work in `Realm_Moba_Demo`.

The numbered prompts intentionally separate host/UI work from public API and bridge work so each layer can be reviewed independently. If the core repository combines those layers, complete both prompts before copying artifacts.
