# MOBA Demo Playtest

Date: 2026-08-26

## Automated verification

| Check | Result | Notes |
|---|---|---|
| `python3 tools/verify_moba_playability.py` | PASS | Prerequisites present in `metadata.json` and `terrain.json`. |
| `dotnet build MapScript.csproj -v q` | PASS | Build succeeded (2 IL trimming warnings in `WasmEntryPoint.cs`). |

## Manual playtest checklist

> Pay special attention to **pathing and stream crossings**: confirm minions cross the stream on all three lanes and do not freeze at the banks.

- [x] Castles visible as towers/bases (expected)
- [x] Kevin spawns at Team 1; selected; camera nearby
- [x] Minions spawn ~30s both sides
- [x] Minions move (not frozen at spawn)
- [x] Three lanes diverge (top/mid/bot)
- [x] Minions cross stream on each lane (Task 6 pathing)
- [x] Towers attack hostiles in range
- [ ] Enemy death grants gold (check gold UI / message)
- [ ] Hero level increases after enough XP
- [ ] No shop/select UI yet (expected until core handoff lands)
- [ ] Minions trade damage with enemy minions / towers (retest after melee-range fix)
- [ ] New Team 1 tower at (-39.76, 45.52) is visible and friendly

## Playtest notes

### Observed
- Minions weren't fighting / HP didn't drop between waves.
- Towers attacked and killed minions.
- No hero-select UI.
- Unclear which towers belong to which team; want ~3 lane towers per side.

### Answers / fixes
- **Hero select UI:** not in the current game API yet. Expected missing until core implements the handoff in `docs/realm-api-handoff/`. Map auto-picks Kevin for now.
- **Seeing enemy towers is correct.** Team 2 castles should be visible. Ownership is `Player` 0 = Team 1, `Player` 1 = Team 2.
- **Minion combat bug:** likely they scanned enemies at 8m but only melee at ~1.5m, then `Stop()` + `Attack()` out of range so they never walked in. `LanePathfinder` now walks into range and applies `DealDamage` on cooldown.
- **Tower layout goal:** 1 base + 1 tower per lane per team. Added Team 1 tower at `(-39.76, 0, 45.52)`. Current Team 1 (Player 0) still has extra side/bot towers from earlier map work — tell me if you want those removed/moved so each team has exactly 3 lane towers + 1 base.

### Still to verify in-game
- Gold / XP after kills
- Minions actually chip HP now
- New mid-lane Team 1 tower placement feels right
