# Changelog

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
