using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandboxTweaks
{
    /// <summary>
    /// Top-left badge listing the tweaks active on the loaded save, so you always
    /// know a save is modified. Host-side only; cosmetic.
    /// </summary>
    internal class SandboxBadge : MonoBehaviour
    {
        private static readonly string[] VisibleScenes = { "HomeScene", "CasinoScene" };

        private GUIStyle _style;

        private void OnGUI()
        {
            var c = SandboxState.Current;
            if (c == null || !c.AnyEnabled) return;

            string scene = SceneManager.GetActiveScene().name;
            bool visible = false;
            for (int i = 0; i < VisibleScenes.Length; i++)
                if (scene == VisibleScenes[i]) { visible = true; break; }
            if (!visible) return;

            EnsureStyle();

            string text = "SANDBOX";
            if (c.unlockAllFloors) text += " · FLOORS";
            if (c.bigMoney) text += " · $$";
            if (c.longDays) text += " · LONG DAY";
            if (c.pinQuota) text += " · QUOTA";

            float width = 24f + text.Length * 8.4f;
            GUI.Box(new Rect(14f, 14f, width, 30f), text, _style);
        }

        private void EnsureStyle()
        {
            if (_style != null) return;

            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
            bg.Apply();

            _style = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 4, 4),
            };
            _style.normal.textColor = new Color(1f, 0.78f, 0.2f, 1f);
            _style.normal.background = bg;
        }
    }
}
