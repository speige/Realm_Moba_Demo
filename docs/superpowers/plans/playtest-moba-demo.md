# MOBA Demo Playtest

Date: 2026-08-26

## Automated verification

| Check | Result | Notes |
|---|---|---|
| `python3 tools/verify_moba_playability.py` | PASS | Prerequisites present in `metadata.json` and `terrain.json`. |
| `dotnet build MapScript.csproj -v q` | PASS | Build succeeded (2 IL trimming warnings in `WasmEntryPoint.cs`). |

## Manual playtest checklist

> **Human playtest still required.** The Realm game client is not available in this workspace. Items below remain unchecked until someone runs the map in Realm. Pay special attention to **pathing and stream crossings** (Task 6): confirm minions cross the stream on all three lanes and do not freeze at the banks.

- [ ] Castles visible as towers/bases (expected)
- [ ] Kevin spawns at Team 1; selected; camera nearby
- [ ] Minions spawn ~30s both sides
- [ ] Minions move (not frozen at spawn)
- [ ] Three lanes diverge (top/mid/bot)
- [ ] Minions cross stream on each lane (Task 6 pathing)
- [ ] Towers attack hostiles in range
- [ ] Enemy death grants gold (check gold UI / message)
- [ ] Hero level increases after enough XP
- [ ] No shop/select UI yet (expected until core handoff lands)

## Playtest notes

<!-- Record pass/fail, screenshots, or issues here after in-game run. -->
