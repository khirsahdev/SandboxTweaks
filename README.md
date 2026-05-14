# SandboxTweaks — dev repo

BepInEx 5 mod for **Gamble With Your Friends**. Splits `saltedbyte`'s all-or-nothing
**SandboxMode** into four independent per-save tweaks the host picks from a checkbox
dialog when creating a save:

| Tweak | What it changes | How |
|---|---|---|
| Unlock all floors | elevator buttons only — `currentFloor` left alone | runtime: `ElevatorManager.RpcEnableAllButtons` (host) + `SetButtons` fallback |
| Big starting money | `money` | written into the new save's `.json` |
| Long day timer | — | runtime `GameSettings.dayDuration` override on load |
| Pin quota | `currentQuota` | written into `.json` + `GameSettings.GetQuota` prefix |

Each save gets a `.tweaks` JSON sidecar recording its choices. Normal saves are untouched.

"Unlock all floors" intentionally does **not** pin `currentFloor` — that would make
the game treat you as end-game (`GetCurrentFloorData()` reads `currentFloor`, so
reroll cost, challenge difficulty and prices all scale up). Unlocking just the
elevator buttons keeps floor-keyed difficulty on normal progression.

## Layout

```
SandboxTweaks.csproj      build (netstandard2.1; paths to game + TMM profile at top — EDIT IF YOURS DIFFER)
src/                      mod sources
  Plugin.cs               BepInPlugin, config, Harmony bootstrap
  SandboxMarker.cs        per-save marker model + .tweaks file IO
  SandboxState.cs         runtime state + post-load refresh
  Patches.cs              6 Harmony patches
  ToggleDialog.cs         IMGUI checkbox dialog (replaces the Normal/Sandbox prompt)
  SandboxBadge.cs         on-screen "SANDBOX" badge
thunderstore/             manifest.json, README.md, CHANGELOG.md, icon.png, make_icon.py
reference/                decompiled Assembly-CSharp + SandboxMode (lookup only, not compiled)
build-package.ps1         build dll + assemble dist/SandboxTweaks-<ver>.zip
verify-load.ps1           launch game w/ profile BepInEx, check log for clean load
dist/                     packaged zip output
```

## Build

```powershell
pwsh -ExecutionPolicy Bypass -File build-package.ps1
```

Compiles `SandboxTweaks.dll`, deploys it into the active TMM profile's
`BepInEx/plugins/SandboxTweaks/`, and produces `dist/SandboxTweaks-0.1.0.zip`
ready to upload to Thunderstore.

## Notes

- The .NET 7 SDK here can't target net8; the project targets `netstandard2.1`
  (the game's Unity DLLs reference netstandard 2.1).
- Game's `MonoSingleton<T>` lives in the `Extensions` namespace.
- The game folder has no `BepInEx/core` — BepInEx is supplied by the TMM profile;
  the game must be launched through Thunderstore Mod Manager (or `verify-load.ps1`).
- Conflicts with **SandboxMode** (both hook the new-save flow). Disable one.

## Credits & license

Sandbox mechanics are **derived from `SaltedByte`'s [SandboxMode](https://github.com/SaltedByte/sandboxmode)**,
which is MIT-licensed. SandboxTweaks studies those mechanics from decompilation,
reimplements them in original code, and splits them into independent per-save
toggles. The decompiled game/third-party sources under `reference/` are kept
local only (`.gitignore`d) and never redistributed.

Released under the **MIT License** — see [`LICENSE`](LICENSE), which carries both
SaltedByte's original copyright and the modification copyright.
