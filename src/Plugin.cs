using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>
    /// Splits SandboxMode's all-or-nothing sandbox into four per-save tweaks the
    /// host picks from a checkbox dialog when creating a new save.
    /// </summary>
    [BepInPlugin(Guid, "Sandbox Tweaks", Version)]
    [BepInProcess("Gamble With Your Friends.exe")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.khirsah.sandboxtweaks";
        public const string Version = "0.2.1";

        internal static ManualLogSource Log;

        // Default checkbox states shown in the new-save dialog.
        internal static ConfigEntry<bool> DefUnlockFloors;
        internal static ConfigEntry<bool> DefBigMoney;
        internal static ConfigEntry<bool> DefLongDays;
        internal static ConfigEntry<bool> DefPinQuota;

        // Default numeric values for the enabled tweaks.
        internal static ConfigEntry<long> StartingMoney;
        internal static ConfigEntry<float> DayDurationSeconds;
        internal static ConfigEntry<long> BetQuota;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            DefUnlockFloors = Config.Bind("Defaults", "UnlockAllFloors", true,
                "Default state of the 'Unlock all floors' checkbox in the new-save dialog.");
            DefBigMoney = Config.Bind("Defaults", "BigMoney", true,
                "Default state of the 'Big starting money' checkbox.");
            DefLongDays = Config.Bind("Defaults", "LongDays", true,
                "Default state of the 'Long day timer' checkbox.");
            DefPinQuota = Config.Bind("Defaults", "PinQuota", true,
                "Default state of the 'Pin quota' checkbox.");

            StartingMoney = Config.Bind("Values", "StartingMoney", 1000000000000L,
                "Money a save starts with when 'Big starting money' is enabled. Default 1,000,000,000,000 ($1T).");
            DayDurationSeconds = Config.Bind("Values", "DayDurationSeconds", 3600f,
                "Day length in seconds when 'Long day timer' is enabled. Vanilla is 300 (5 min).");
            BetQuota = Config.Bind("Values", "BetQuota", 5000L,
                "Pinned quota used by the casino MinBet/MaxBet formulas when 'Pin quota' is enabled. " +
                "Higher = larger bet ranges; also stops the lose-state from triggering.");

            _harmony = new Harmony(Guid);
            _harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Host object for the IMGUI dialog, the on-screen badge, and the
            // scene-load watcher that re-applies tweaks after a lost run.
            var go = new GameObject("SandboxTweaks.UI");
            DontDestroyOnLoad(go);
            go.AddComponent<ToggleDialog>();
            go.AddComponent<SandboxBadge>();
            go.AddComponent<SandboxRuntime>();

            Log.LogInfo("Sandbox Tweaks " + Version + " loaded.");
        }

        private void OnDestroy() => _harmony?.UnpatchSelf();
    }
}
