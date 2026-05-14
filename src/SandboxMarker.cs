using System;
using System.IO;
using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>
    /// Per-save record of which tweaks were chosen and their values.
    /// Serialized as JSON into a "&lt;saveName&gt;.tweaks" sidecar file next to the
    /// save JSON, so normal saves stay completely untouched.
    /// </summary>
    [Serializable]
    public class SandboxMarker
    {
        public bool unlockAllFloors;
        public bool bigMoney;
        public bool longDays;
        public bool pinQuota;
        public bool allGamesFloorOne;

        public long startingMoney = 1000000000000L;
        public float dayDuration = 3600f;
        public long betQuota = 5000L;

        public bool AnyEnabled =>
            unlockAllFloors || bigMoney || longDays || pinQuota || allGamesFloorOne;
    }

    /// <summary>Reads/writes the ".tweaks" sidecar files in the game's Saves folder.</summary>
    internal static class Marker
    {
        private const string Ext = ".tweaks";

        public static string SavesDir => Path.Combine(Application.persistentDataPath, "Saves");

        private static string PathFor(string saveName) => Path.Combine(SavesDir, saveName + Ext);

        public static SandboxMarker Read(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return null;
            try
            {
                string path = PathFor(saveName);
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<SandboxMarker>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SandboxTweaks] marker read failed for '" + saveName + "': " + e.Message);
                return null;
            }
        }

        public static void Write(string saveName, SandboxMarker marker)
        {
            if (string.IsNullOrEmpty(saveName)) return;
            try
            {
                Directory.CreateDirectory(SavesDir);
                File.WriteAllText(PathFor(saveName), JsonUtility.ToJson(marker, true));
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SandboxTweaks] marker write failed for '" + saveName + "': " + e.Message);
            }
        }

        public static void Delete(string saveName)
        {
            if (string.IsNullOrEmpty(saveName)) return;
            try
            {
                string path = PathFor(saveName);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SandboxTweaks] marker delete failed for '" + saveName + "': " + e.Message);
            }
        }
    }
}
