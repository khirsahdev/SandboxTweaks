using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>Runtime state: the tweaks active on the loaded save, plus captured vanilla values.</summary>
    internal static class SandboxState
    {
        /// <summary>Tweaks baked into the currently-loaded save (empty marker = vanilla save).</summary>
        public static SandboxMarker Current = new SandboxMarker();

        /// <summary>Chosen in the dialog, consumed once by the CreateNewSave postfix.</summary>
        public static SandboxMarker Pending;

        /// <summary>Vanilla GameSettings.dayDuration, captured before we ever overwrite it.</summary>
        public static float OriginalDayDuration = 300f;
        public static bool DayDurationCaptured;

        private static GameSettings _gameSettings;

        /// <summary>Cached GameSettings ScriptableObject (Resources.Load is cached, but this avoids the per-call lookup).</summary>
        public static GameSettings GameSettings =>
            _gameSettings != null ? _gameSettings : (_gameSettings = Resources.Load<GameSettings>("GameSettings"));

        /// <summary>
        /// Whether the loaded save has the unlock-all-floors tweak. Used by the
        /// ElevatorManager patches, which may run before SaveManager.LoadGame has
        /// refreshed <see cref="Current"/> — so fall back to reading the marker.
        /// </summary>
        public static bool UnlockFloors
        {
            get
            {
                if (Current != null && Current.AnyEnabled)
                    return Current.unlockAllFloors;

                var marker = Marker.Read(PlayerPrefs.GetString("SelectedSaveName", ""));
                if (marker != null) Current = marker;
                return Current != null && Current.unlockAllFloors;
            }
        }

        /// <summary>
        /// Called after SaveManager.LoadGame. Loads the marker for the active save and
        /// applies the runtime-only tweaks (day length). Restores vanilla day length for
        /// normal saves so a sandbox save never leaks its settings into another save.
        /// </summary>
        public static void RefreshFromCurrentSave()
        {
            string saveName = PlayerPrefs.GetString("SelectedSaveName", "");
            Current = Marker.Read(saveName) ?? new SandboxMarker();

            var gs = GameSettings;
            if (gs == null)
            {
                Plugin.Log.LogWarning("[SandboxTweaks] GameSettings not found — cannot adjust dayDuration");
                return;
            }

            if (!DayDurationCaptured)
            {
                OriginalDayDuration = gs.dayDuration;
                DayDurationCaptured = true;
            }

            float target = Current.longDays ? Current.dayDuration : OriginalDayDuration;
            if (!Mathf.Approximately(gs.dayDuration, target))
            {
                Plugin.Log.LogInfo("[SandboxTweaks] save '" + saveName + "' longDays=" + Current.longDays +
                                   " — dayDuration " + gs.dayDuration + "s -> " + target + "s");
                gs.dayDuration = target;
            }
        }
    }
}
