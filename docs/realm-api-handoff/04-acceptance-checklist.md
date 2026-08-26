# Acceptance Checklist

Record the core commit, generated artifact version/hash, map commit, platform, and test output when running this checklist. Items marked automated should be covered by repeatable tests in the core repository where its harness permits.

## Compatibility and build

- [ ] Automated: a map that does not opt in sees no hero picker, shop, level-up panel, ability-bar additions, automatic spawning, or changed gameplay defaults.
- [ ] Automated: the opted-in fixture receives only the capabilities it registers or invokes.
- [ ] Automated: existing MapAPI tests and maps continue to compile and pass without source changes.
- [ ] Automated: no shared engine/MapAPI code contains `Realm_Moba_Demo` or demo content/coordinate IDs.
- [ ] Automated: the public API and bridge build for `net10.0`, `wasi-wasm`, trimming, and AOT.
- [ ] Automated: build/generation tasks work when the checkout path contains spaces.
- [ ] Verified: regenerated `Realm.MapAPI.dll` and `.xml` are from the tested build and copied into `Realm_Moba_Demo/lib/`.
- [ ] Verified: the map project compiles against the copied artifacts.

## Hero selection

- [ ] Automated: only the owning player can interact with a selection.
- [ ] Automated: one valid submission produces one immutable result.
- [ ] Automated: a second or replayed submission is rejected and cannot spawn/apply twice.
- [ ] Automated: timeout produces the configured valid default exactly once.
- [ ] Automated: pending, completed, invalid, and closed handle states are distinguishable without cross-boundary exceptions.
- [ ] Automated: closing is idempotent and terminal UI cannot submit.
- [ ] Integration: player `0` selects Kevin or Chad and exactly one selected hero spawns at `Spawn_Team1` or the `Base_Team1` fallback.

## Waves, lanes, and towers

- [ ] Integration: both teams spawn one wave about every 30 seconds.
- [ ] Integration: every complete wave contains three units on each of three lanes: nine units per side.
- [ ] Integration: top, middle, and bottom groups follow distinct waypoint routes in both directions.
- [ ] Integration: waves do not overlap because of duplicate timer execution or replay.
- [ ] Integration: hostile units interrupt minion movement, are attacked until clear, and movement then resumes.
- [ ] Integration: each tower chooses the closest valid hostile in range, respects cooldown, and does not attack allies or dead units.
- [ ] Integration: ownership/team relationships, not `IsEnemy` alone, determine hostility.

## XP, levels, and rewards

- [ ] Automated: XP is stored per hero/unit rather than globally per player.
- [ ] Automated: one death identity grants its configured XP and gold no more than once, including replay/retry.
- [ ] Automated: crossing one threshold emits one level transition.
- [ ] Automated: one large XP grant handles every crossed threshold once.
- [ ] Automated: only the owning player can choose an upgrade.
- [ ] Automated: one valid level-up choice applies once; duplicate/stale submissions are rejected.
- [ ] Automated: level-up timeout applies the configured default once.
- [ ] Integration: a hero kill/death reward updates visible gold and XP/level state correctly.

## Shop and items

- [ ] Automated: only the owning player can purchase or sell for a buyer/seller unit.
- [ ] Automated: unknown items, invalid units, insufficient gold, full inventory, and stack-limit violations fail without partial mutation.
- [ ] Automated: gold cannot become negative and duplicate requests cannot charge twice.
- [ ] Automated: item modifiers apply once on purchase and are completely reversed on sell.
- [ ] Automated: `demo_blade`, `demo_armor`, and `demo_boots` can be registered as map content without engine constants.
- [ ] UI: the shop displays price, affordability, inventory/stack state, and useful failure feedback.
- [ ] UI: opening/closing the shop affects only the requested player.

## Abilities and upgrade UI

- [ ] Automated: `demo_power_strike`, `demo_arc_burst`, and `demo_rally` can be registered as map content without engine constants.
- [ ] Automated: upgrade enforces ownership, learned/maximum levels, and exactly-once application.
- [ ] Automated: cast validates caster ownership/alive state, learned level, target mode, target relationship, range, mana, and cooldown.
- [ ] Automated: rejected casts spend no mana, start no cooldown, and apply no effect.
- [ ] Automated: successful casts spend resources and apply/emit their effect exactly once.
- [ ] UI: the ability bar shows icon/name, command position/key, current level, mana cost, cooldown, and locked/disabled state.
- [ ] UI: unit, point/area, and self/friendly targeting operate deterministically.
- [ ] Integration: a level-up choice can upgrade an ability and the bar immediately reflects the new level.

## WASM transport and lifecycle

- [ ] Automated: no managed delegate crosses the WASM boundary.
- [ ] Automated: all request/result models round-trip with stable serialization and bounded inputs.
- [ ] Automated: malformed input, stale handles, stale units, and wrong-player commands return explicit failures rather than host exceptions.
- [ ] Automated: command retries and event delivery cannot duplicate selections, transactions, upgrades, rewards, or casts.
- [ ] Automated: pending handles, UI state, queues, and buffers are released on close, disconnect, match end, and map teardown.
- [ ] Automated: capability/version discovery lets the map gate unsupported optional APIs without reflection.

## Final playtest

- [ ] Hero selection is normal client UI, not a chat-only flow.
- [ ] Shop and level-up choices are normal client UI.
- [ ] Kevin is controllable and camera/selection behavior still works.
- [ ] Three-lane waves and towers continue working after the refreshed library is installed.
- [ ] Gold, XP, item stats, abilities, cooldowns, and upgrades remain correct through several levels and purchases.
- [ ] Loading and playing a representative non-MOBA map shows no regression.
