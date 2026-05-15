# Changelog

## 0.4.4

- Challenges now match the spawned games under **All games on floor 1**. The
  challenge booth was pulling from floor-1-only challenges and the floor-1
  game-type filter, even though floor 1 was spawning floor-2/3/4 games. On the
  first casino floor the mod now expands both `ChallengeManager.
  GetChallengesByFloorIndex` and `NextCasinoPredicter.GetAvailableGameTypesForFloor`
  to the union of floors 1-4, so quests can target any of the games actually
  present.

## 0.4.3

- Removed the custom MinBet/MaxBet scaling entirely. Bet formulas are now pure
  vanilla in every mode. A floor-4 game visited at low progression under
  unlock-all-floors will use the vanilla `2^(casinoLevel - currentFloor - 1)`
  scaling (i.e. expensive); a high-baseMinBet game imported onto floor 1 under
  the mixed-pool tweak will use the vanilla floor-1 scaling with its baked-in
  base bets.

## 0.4.2

- The progression-based bet scaling now also applies to **All games on floor 1**.
  High-stakes games imported onto floor 1 carry their large baked-in base bets;
  they're now scaled by your real progression like the unlock-all-floors case,
  instead of using the raw vanilla amount.

## 0.4.1

- Unlock-all-floors bet scaling now follows your real progression instead of the
  physical floor. The `MinBet`/`MaxBet` gap term swaps `casinoLevel` for
  `currentFloor`: a floor-4 game (high base bet) is cheap at low progression and
  grows to vanilla pricing as you climb. Fixes floor-4 minimum bets sitting far
  above your quota when visited early.

## 0.4.0

- New tweak: **All games on floor 1**. Floor 1's game pool becomes the
  de-duplicated union of floors 1-4, so any of the 17 game types can spawn on
  the first floor while floor progression, challenges and bets stay normal.
- Implemented as a `StampManager.GetLootTableForFloor` postfix that swaps in a
  runtime copy of the Floor 1 loot table — original tables and the challenge
  predictor are untouched.
- **All games on floor 1** and **Unlock all floors** are mutually exclusive in
  the new-save dialog — checking one clears the other.

## 0.3.0

- Unlock all floors now caps at the last casino floor. The boss stop stays gated
  behind real progression, so you can't elevator-skip straight to the ending —
  enforced host-side on `ServerTryTeleportPlayers` / `ServerForceTeleportPlayers`.
- Restored the original (0.1.0) bet scaling. With floors unlocked, the real
  `currentFloor` stays at normal progression (so challenge pool and reroll cost
  stay normal), but `GameBase.MinBet` / `MaxBet` are recomputed as if you were
  progressed to the top casino floor — high-floor bets stay tame instead of
  scaling `2^(floor gap)` above your balance.

## 0.2.1

- Tweaks now persist across a lost run. A loss calls
  `SaveManager.ResetCurrentSaveToDefaults`, which rebuilds the save to vanilla
  values — big money and pinned quota were wiped, and the floor unlock did not
  reliably re-apply.
- Big money / pinned quota are re-baked into the save by a
  `ResetCurrentSaveToDefaults` postfix.
- Floor unlock + day length now re-apply on every `CasinoScene` / `HomeScene`
  load via a `sceneLoaded` watcher, instead of relying on a single Harmony hook.

## 0.2.0

- **Unlock all floors** now unlocks the elevator buttons directly
  (`ElevatorManager.RpcEnableAllButtons`, host-authoritative) instead of pinning
  the save's `currentFloor` to the top.
- Fixes the side effects of the old approach: pinning `currentFloor` made the
  game treat you as end-game — harder challenge pool, reroll cost 5 instead of 2,
  higher shredding prices. Floor-keyed difficulty now follows normal progression.
- `currentFloor` / `requiredQuotaToNextFloor` are no longer written to the save.
- Saves created with 0.1.0 keep their pinned `currentFloor`; remake the save to
  get the new behaviour.

## 0.1.0

- Initial release.
- Splits SandboxMode's all-or-nothing sandbox into four independent per-save tweaks:
  unlock all floors, big starting money, long day timer, pinned quota.
- Empty save slot opens an IMGUI checkbox dialog to pick tweaks + values per save.
- Per-save `.tweaks` sidecar marker; normal saves untouched.
- BepInEx config for default checkbox states and default values.
- Top-left `SANDBOX` badge listing active tweaks.
