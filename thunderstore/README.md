# Sandbox Tweaks

A BepInEx 5 mod for **Gamble With Your Friends** that lets the host pick exactly
which sandbox cheats a save uses — instead of one all-or-nothing "sandbox" mode.

When you click an empty save slot, a **checkbox dialog** opens. Tick the tweaks
you want, set their values, and press **Create Save**. Each choice is baked into
that save only; normal saves are never touched.

## Tweaks

| Tweak | Effect |
|---|---|
| **Unlock all floors** | Every elevator button works from day one — ride to any floor freely. Floor-keyed difficulty (challenge pool, reroll cost, shredding prices) still follows your **normal progression** — it does not jump to end-game. |
| **Big starting money** | Save starts with a configurable pile of cash (default **$1,000,000,000,000**). |
| **Long day timer** | Each casino day lasts a configurable length (default **3600s / 1 hour**, vanilla is 300s). |
| **Pin quota** | The quota used by the casino MinBet/MaxBet formulas is pinned to a fixed value, so bet ranges stay stable across days and the lose-state never triggers. |

Leave every box unchecked and you get a perfectly normal save.

A small **`SANDBOX`** badge in the top-left lists which tweaks are active while
you play, so you always know a save is modified.

## Configuration

After the first launch, edit `BepInEx/config/com.khirsah.sandboxtweaks.cfg`:

- **`[Defaults]`** — which checkboxes start ticked in the dialog.
- **`[Values]`** — the default money / day-length / quota numbers (you can still
  override them per-save in the dialog).

## Multiplayer

Only the **host** needs this mod. Money, floors, day length and quota are applied
to the host's save and replicate to every client via the game's networking. The
checkbox dialog and the on-screen badge are host-side only.

## Installation

**Mod manager (recommended):** install through Thunderstore Mod Manager / r2modman
— BepInEx and the DLL are placed for you.

**Manual:** install [BepInEx 5.4.23.x (win x64)](https://github.com/BepInEx/BepInEx/releases),
launch once to generate `BepInEx/config/`, then drop `SandboxTweaks.dll` into
`BepInEx/plugins/SandboxTweaks/`. Check `BepInEx/LogOutput.log` for
`Sandbox Tweaks 0.1.0 loaded.`

## Known limitations

- The checkbox dialog is drawn with IMGUI and is **not** a true modal — clicking
  the menu *behind* it still registers. Keep interaction on the dialog itself.
- Do not run this alongside **SandboxMode** — both hook the new-save flow and will
  fight over it. Sandbox Tweaks is a superset; pick one.

## Credits

Mechanics derived from `saltedbyte`'s **SandboxMode**, split into independent
per-save toggles. Built on BepInEx 5 and HarmonyX.

*Built with AI assistance (Claude).*
