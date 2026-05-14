# Changelog

## 0.1.0

- Initial release.
- Splits SandboxMode's all-or-nothing sandbox into four independent per-save tweaks:
  unlock all floors, big starting money, long day timer, pinned quota.
- Empty save slot opens an IMGUI checkbox dialog to pick tweaks + values per save.
- Per-save `.tweaks` sidecar marker; normal saves untouched.
- BepInEx config for default checkbox states and default values.
- Top-left `SANDBOX` badge listing active tweaks.
