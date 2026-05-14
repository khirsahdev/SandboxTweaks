using System;
using System.Globalization;
using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>
    /// IMGUI checkbox dialog shown when an empty save slot is clicked. Replaces
    /// SandboxMode's two-button Normal/Sandbox prompt with per-feature toggles.
    /// The game's own ConfirmationDialog only supports a question + two buttons,
    /// so a small IMGUI window is used instead.
    /// </summary>
    internal class ToggleDialog : MonoBehaviour
    {
        public static ToggleDialog Instance;

        private const int WinId = 0x5A4D; // 'ZM'
        private const float WinWidth = 360f;

        private bool _open;
        private Rect _win;
        private SaveSlotUI _slot;
        private LocalSaveManager _manager;

        // Working copy of the toggles + value fields.
        private bool _floors, _money, _days, _quota;
        private string _moneyStr, _daysStr, _quotaStr;

        private GUIStyle _dimStyle;

        private void Awake() => Instance = this;

        public void Open(SaveSlotUI slot, LocalSaveManager manager)
        {
            _slot = slot;
            _manager = manager;

            _floors = Plugin.DefUnlockFloors.Value;
            _money = Plugin.DefBigMoney.Value;
            _days = Plugin.DefLongDays.Value;
            _quota = Plugin.DefPinQuota.Value;

            _moneyStr = Plugin.StartingMoney.Value.ToString(CultureInfo.InvariantCulture);
            _daysStr = Plugin.DayDurationSeconds.Value.ToString("0", CultureInfo.InvariantCulture);
            _quotaStr = Plugin.BetQuota.Value.ToString(CultureInfo.InvariantCulture);

            _win = new Rect((Screen.width - WinWidth) / 2f, Screen.height * 0.22f, WinWidth, 10f);
            _open = true;
        }

        private void OnGUI()
        {
            if (!_open) return;

            // Dim the screen. The fullscreen Button eats clicks at the IMGUI layer;
            // note it does not block the underlying uGUI menu (see README caveat).
            if (_dimStyle == null) _dimStyle = MakeDimStyle();
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _dimStyle);
            if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _dimStyle)) { /* swallow */ }

            _win = GUILayout.Window(WinId, _win, DrawWindow, "New Save — Sandbox Tweaks");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Space(6);
            GUILayout.Label("Pick which tweaks to bake into this save.\nLeave them all off for a normal save.");
            GUILayout.Space(6);

            _floors = GUILayout.Toggle(_floors, "  Unlock all floors");

            _money = GUILayout.Toggle(_money, "  Big starting money");
            ValueRow("      amount  $", ref _moneyStr, _money);

            _days = GUILayout.Toggle(_days, "  Long day timer");
            ValueRow("      seconds", ref _daysStr, _days);

            _quota = GUILayout.Toggle(_quota, "  Pin quota (stable bets)");
            ValueRow("      quota", ref _quotaStr, _quota);

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Save", GUILayout.Height(30))) Confirm();
            GUILayout.Space(6);
            if (GUILayout.Button("Cancel", GUILayout.Height(30))) _open = false;
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUI.DragWindow(new Rect(0, 0, 100000, 22));
        }

        private static void ValueRow(string label, ref string value, bool enabled)
        {
            GUILayout.BeginHorizontal();
            GUI.enabled = enabled;
            GUILayout.Label(label, GUILayout.Width(110));
            value = GUILayout.TextField(value ?? "", GUILayout.Width(200));
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void Confirm()
        {
            var marker = new SandboxMarker
            {
                unlockAllFloors = _floors,
                bigMoney = _money,
                longDays = _days,
                pinQuota = _quota,
            };

            if (!long.TryParse(_moneyStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out marker.startingMoney)
                || marker.startingMoney <= 0)
                marker.startingMoney = Plugin.StartingMoney.Value;

            if (!float.TryParse(_daysStr, NumberStyles.Float, CultureInfo.InvariantCulture, out marker.dayDuration)
                || marker.dayDuration <= 0f)
                marker.dayDuration = Plugin.DayDurationSeconds.Value;

            if (!long.TryParse(_quotaStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out marker.betQuota)
                || marker.betQuota <= 0)
                marker.betQuota = Plugin.BetQuota.Value;

            _open = false;

            try
            {
                // Only hand a marker to the CreateNewSave postfix if something is enabled;
                // otherwise this is just a normal save and nothing should touch it.
                SandboxState.Pending = marker.AnyEnabled ? marker : null;

                string saveName = "Save_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                _manager.CreateNewSave(saveName);
                _slot.OnSlotSelected?.Invoke(saveName);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("[SandboxTweaks] create save failed: " + e);
                SandboxState.Pending = null;
            }
        }

        private static GUIStyle MakeDimStyle()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.55f));
            tex.Apply();
            return new GUIStyle { normal = { background = tex } };
        }
    }
}
