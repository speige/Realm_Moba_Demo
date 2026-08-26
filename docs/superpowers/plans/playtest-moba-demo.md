# MOBA Demo Playtest

Date: 2026-08-26

## Automated verification

| Check | Result | Notes |
|---|---|---|
| `python3 tools/verify_moba_playability.py` | PASS | Prerequisites present in `metadata.json` and `terrain.json`. |
| `dotnet build MapScript.csproj -v q` | PASS | Build succeeded (2 IL trimming warnings in `WasmEntryPoint.cs`). |

## Manual playtest checklist

- [x] Castles visible as towers/bases (expected)
- [x] Kevin spawns at Team 1; selected; camera nearby
- [x] Minions spawn ~30s both sides
- [x] Minions move (not frozen at spawn)
- [x] Three lanes diverge (top/mid/bot)
- [x] Minions cross stream on each lane (Task 6 pathing)
- [x] Towers attack hostiles in range
- [ ] Enemy death grants gold (look for chat `+25 gold` and MOBA leaderboard)
- [ ] Hero level increases after enough XP
- [ ] No shop/select UI yet (expected until core handoff lands)
- [ ] Minions trade damage, then **resume push** toward enemy base
- [ ] Only **one** Team 1 mid tower at (-39.76, 45.52)
- [ ] Fog unveils as hero/minions move (`ExplorationShroud`)

## Playtest notes / answers

### Gold
Gold was being awarded but easy to miss. Now: starting gold chat, `+25 gold (total …)` on enemy deaths, and a MOBA leaderboard Gold/Level rows.

### Minions “forget” the lane after a fight
They were chase-attack-moving onto enemies and dropping the lane order. They now only peel for enemies in melee range, keep attack-moving along waypoints, and **re-issue the lane push** when the fight clears so they keep going toward the main tower.

### Two mid towers / can’t see mine at start
- Removed duplicate at (-42.29, 52.08); kept (-39.76, 45.52).
- Mid tower is far from spawn — with fog you **won’t see it at game start** until units walk near it. That’s expected with `ExplorationShroud`.
- If you want the whole map visible for layout debugging, set `MapProperties.ShroudType` to `"visible"` in `metadata.json`.

### Map unveil
`MapProperties.ShroudType` is now `ExplorationShroud`, and unit/tower `ScanRadius` was raised so movement should explore the shroud. If unveil still fails, it’s likely an engine vision hook we don’t have an API for — ask supervisor.

### Current Team 1 towers (Player 0)
- Base (-83.4, 92.2)
- Bot (98.1, 90.7)
- Side (-93.9, 0.4)
- Mid (-39.8, 45.5)
