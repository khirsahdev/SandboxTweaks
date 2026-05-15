using System;
using System.IO;
using Extensions;          // game's MonoSingleton<T>
using HarmonyLib;
using Mirror;              // NetworkServer
using MoreMountains.Tools; // MMLootTableGameObjectSO
using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>
    /// Harmony patches. Targets verified against the decompiled Assembly-CSharp.dll
    /// (see reference/Assembly-CSharp.decompiled.cs) and SandboxMode v0.3.1.
    /// </summary>
    internal static class Patches
    {
        // ── Capture the vanilla day length once, before anything overwrites it. ──
        [HarmonyPatch(typeof(GameManager), "OnAwake")]
        internal static class GameManager_CaptureDayDuration
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                if (SandboxState.DayDurationCaptured) return;
                var gs = Resources.Load<GameSettings>("GameSettings");
                if (gs == null) return;
                SandboxState.OriginalDayDuration = gs.dayDuration;
                SandboxState.DayDurationCaptured = true;
                Plugin.Log.LogInfo("[SandboxTweaks] captured original dayDuration = " + gs.dayDuration + "s");
            }
        }

        // ── After a save loads, read its marker and apply runtime-only tweaks. ──
        [HarmonyPatch(typeof(SaveManager), "LoadGame")]
        internal static class SaveManager_RefreshState
        {
            [HarmonyPostfix]
            private static void Postfix() => SandboxState.RefreshFromCurrentSave();
        }

        // ── PinQuota: GameSettings.GetQuota always returns the pinned value. ──
        [HarmonyPatch(typeof(GameSettings), "GetQuota")]
        internal static class GameSettings_PinQuota
        {
            [HarmonyPrefix]
            private static bool Prefix(ref long __result)
            {
                if (!SandboxState.Current.pinQuota) return true; // run vanilla formula
                __result = SandboxState.Current.betQuota;
                return false; // skip original
            }
        }

        // ── Keep the sidecar marker in sync when a save is deleted. ──
        [HarmonyPatch(typeof(LocalSaveManager), "DeleteSave")]
        internal static class LocalSaveManager_DeleteSave
        {
            [HarmonyPostfix]
            private static void Postfix(string saveName) => Marker.Delete(saveName);
        }

        // ── After the game writes a fresh save JSON, bake in the chosen tweaks. ──
        [HarmonyPatch(typeof(LocalSaveManager), "CreateNewSave")]
        internal static class LocalSaveManager_CreateNewSave
        {
            [HarmonyPostfix]
            private static void Postfix(string saveName)
            {
                SandboxMarker pending = SandboxState.Pending;
                SandboxState.Pending = null; // consume — never leak into a later save

                if (pending == null || !pending.AnyEnabled || string.IsNullOrEmpty(saveName))
                    return;

                try
                {
                    string path = Path.Combine(Marker.SavesDir, saveName + ".json");
                    if (!File.Exists(path))
                    {
                        Plugin.Log.LogWarning("[SandboxTweaks] save file missing after CreateNewSave: " + path);
                        return;
                    }

                    var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                    if (data == null)
                    {
                        Plugin.Log.LogWarning("[SandboxTweaks] failed to deserialize save: " + path);
                        return;
                    }

                    bool dataChanged = false;

                    // NOTE: unlockAllFloors deliberately does NOT touch save data.
                    // Pinning currentFloor makes the game treat you as end-game —
                    // GetCurrentFloorData() reads GameManager.currentFloor, so reroll
                    // cost, challenge difficulty and shredding prices all scale up.
                    // Floors are instead unlocked at runtime via ElevatorManager
                    // (see ElevatorManager_UnlockAllFloors below).

                    if (pending.bigMoney)
                    {
                        data.money = pending.startingMoney;
                        dataChanged = true;
                    }

                    if (pending.pinQuota)
                    {
                        data.currentQuota = pending.betQuota;
                        data.successfulQuota = 0;
                        dataChanged = true;
                    }

                    if (dataChanged)
                    {
                        File.WriteAllText(path, JsonUtility.ToJson(data, true));
                        // The game also caches the freshly-created save in PlayerPrefs.
                        PlayerPrefs.SetString("SelectedSaveData", JsonUtility.ToJson(data));
                        PlayerPrefs.Save();
                    }

                    Marker.Write(saveName, pending);
                    Plugin.Log.LogInfo("[SandboxTweaks] save '" + saveName + "' created — floors=" +
                                       pending.unlockAllFloors + " money=" + pending.bigMoney +
                                       " days=" + pending.longDays + " quota=" + pending.pinQuota);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("[SandboxTweaks] failed to apply tweaks to '" + saveName + "': " + e);
                }
            }
        }

        // ── Empty save slot clicked → show our checkbox dialog instead of vanilla create. ──
        [HarmonyPatch(typeof(SaveSlotUI), "OnSlotClicked")]
        internal static class SaveSlotUI_ToggleDialog
        {
            [HarmonyPrefix]
            private static bool Prefix(SaveSlotUI __instance, ref bool ___isEmpty)
            {
                if (!___isEmpty) return true; // existing save → vanilla select

                var manager = MonoSingleton<LocalSaveManager>.Instance;
                if (manager == null)
                {
                    Plugin.Log.LogWarning("[SandboxTweaks] LocalSaveManager.Instance null — vanilla create flow");
                    return true;
                }
                if (ToggleDialog.Instance == null)
                {
                    Plugin.Log.LogWarning("[SandboxTweaks] ToggleDialog not ready — vanilla create flow");
                    return true;
                }

                ToggleDialog.Instance.Open(__instance, manager);
                return false; // suppress the vanilla "create save immediately" behaviour
            }
        }

        // ── UnlockAllFloors: enable the casino-floor buttons, host-authoritative. ──
        // ElevatorManager.Initialize already sends a ClientRpc, so by the time it
        // runs the object is network-spawned. RpcEnableAllButtons replicates to
        // every client — modded or not — so only the host needs this mod. It lights
        // every button including the boss, so we re-gate the boss button after.
        [HarmonyPatch(typeof(ElevatorManager), "Initialize")]
        internal static class ElevatorManager_UnlockAllFloors
        {
            [HarmonyPostfix]
            private static void Postfix(ElevatorManager __instance)
            {
                if (!SandboxState.UnlockFloors) return;
                if (NetworkServer.active)
                    __instance.RpcEnableAllButtons(); // a ClientRpc must originate on the server/host
                Elevator.ApplyButtons(__instance);    // re-gate the boss button to real progression
                Plugin.Log.LogInfo("[SandboxTweaks] unlock-all-floors applied via Initialize");
            }
        }

        // ── Host-local fallback: SetButtons runs in ElevatorManager.Start, before
        //    the RPC above. Apply the same button gating regardless of timing. ──
        [HarmonyPatch(typeof(ElevatorManager), "SetButtons")]
        internal static class ElevatorManager_SetButtonsFallback
        {
            [HarmonyPostfix]
            private static void Postfix(ElevatorManager __instance)
            {
                if (!SandboxState.UnlockFloors) return;
                Elevator.ApplyButtons(__instance);
            }
        }

        // ── Cap the unlock at the last casino floor: the boss stop stays gated
        //    behind real progression, so unlock-all-floors can't elevator-skip to
        //    the ending. Enforced host-side on both teleport entry points. ──
        [HarmonyPatch(typeof(ElevatorManager), "ServerTryTeleportPlayers")]
        internal static class ElevatorManager_BlockBossShortcut
        {
            [HarmonyPrefix]
            private static bool Prefix(ElevatorManager __instance, int toIndex)
            {
                if (!SandboxState.UnlockFloors) return true;
                int boss = Elevator.BossIndex(__instance);
                if (boss < 0 || toIndex < boss) return true; // not the boss stop
                if (Elevator.BossReachedLegitimately(__instance)) return true;
                Plugin.Log.LogInfo("[SandboxTweaks] blocked elevator shortcut to boss (not progressed)");
                return false;
            }
        }
        [HarmonyPatch(typeof(ElevatorManager), "ServerForceTeleportPlayers")]
        internal static class ElevatorManager_BlockBossForce
        {
            [HarmonyPrefix]
            private static bool Prefix(ElevatorManager __instance, int toIndex)
            {
                if (!SandboxState.UnlockFloors) return true;
                int boss = Elevator.BossIndex(__instance);
                if (boss < 0 || toIndex < boss) return true;
                return Elevator.BossReachedLegitimately(__instance);
            }
        }

        // Bet formulas (GameBase.MinBet / GameBase.MaxBet) are intentionally left
        // as vanilla — no custom postfixes. Mod-induced situations (floor-4 game
        // visited at low progression, high-baseMinBet game imported onto floor 1)
        // keep the game's own pricing.

        // ── Persist tweaks across a lost run. ──
        // On a loss the game calls SaveManager.ResetCurrentSaveToDefaults, which
        // rebuilds currentSaveData to vanilla values (money/quota wiped). Re-bake
        // the per-save tweaks into the reset data so they survive. (Floors are
        // runtime-only now, handled by SandboxRuntime on scene load.)
        [HarmonyPatch(typeof(SaveManager), "ResetCurrentSaveToDefaults")]
        internal static class SaveManager_ReapplyAfterReset
        {
            [HarmonyPostfix]
            private static void Postfix(SaveManager __instance)
            {
                string saveName = PlayerPrefs.GetString("SelectedSaveName", "");
                var marker = Marker.Read(saveName);
                if (marker == null || !marker.AnyEnabled) return;
                SandboxState.Current = marker;

                try
                {
                    var data = Traverse.Create(__instance).Field("currentSaveData").GetValue<SaveData>();
                    if (data == null) return;

                    bool changed = false;
                    if (marker.bigMoney)
                    {
                        data.money = marker.startingMoney;
                        changed = true;
                    }
                    if (marker.pinQuota)
                    {
                        data.currentQuota = marker.betQuota;
                        data.successfulQuota = 0;
                        changed = true;
                    }
                    if (!changed) return;

                    string path = Path.Combine(Marker.SavesDir, saveName + ".json");
                    File.WriteAllText(path, JsonUtility.ToJson(data, true));
                    PlayerPrefs.SetString("SelectedSaveData", JsonUtility.ToJson(data));
                    PlayerPrefs.Save();
                    Plugin.Log.LogInfo("[SandboxTweaks] re-applied tweaks after run reset: " + saveName);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("[SandboxTweaks] re-apply after reset failed: " + e);
                }
            }
        }

        // ── All games on floor 1. ──
        // StampManager.GetLootTableForFloor returns the per-floor game pool.
        // When the tweak is on, swap the Floor 1 table for a runtime union of
        // floors 1-4 so any game type can spawn there. Recognised by SO identity
        // so there is no floor-index guessing. NextCasinoPredicter loads tables
        // via Resources.Load directly, so challenge filtering stays floor-1 normal.
        [HarmonyPatch(typeof(StampManager), "GetLootTableForFloor")]
        internal static class StampManager_MixedFirstFloor
        {
            [HarmonyPostfix]
            private static void Postfix(ref MMLootTableGameObjectSO __result)
            {
                if (!SandboxState.MixedFirstFloor) return;
                if (__result == null || __result != LootPool.Floor1Table) return;

                var combined = LootPool.Combined;
                if (combined != null) __result = combined;
            }
        }
    }
}
