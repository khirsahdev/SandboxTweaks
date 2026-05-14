using System;
using System.IO;
using Extensions; // game's MonoSingleton<T>
using HarmonyLib;
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

                    if (pending.unlockAllFloors)
                    {
                        // currentFloor = top floor → ElevatorManager.SetButtons enables every
                        // elevator button (it activates buttons up to currentFloor + 1).
                        var gs = Resources.Load<GameSettings>("GameSettings");
                        int topFloor = (gs != null && gs.floorData != null && gs.floorData.Count > 0)
                            ? gs.floorData.Count - 1
                            : 4;
                        data.currentFloor = topFloor;
                        // Never advance/lose a floor: the next-floor quota gate is unreachable.
                        data.requiredQuotaToNextFloor = long.MaxValue;
                    }

                    if (pending.bigMoney)
                        data.money = pending.startingMoney;

                    if (pending.pinQuota)
                    {
                        data.currentQuota = pending.betQuota;
                        data.successfulQuota = 0;
                    }

                    File.WriteAllText(path, JsonUtility.ToJson(data, true));
                    // The game also caches the freshly-created save in PlayerPrefs.
                    PlayerPrefs.SetString("SelectedSaveData", JsonUtility.ToJson(data));
                    PlayerPrefs.Save();

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
    }
}
