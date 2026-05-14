# Changelog

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
