using System.Collections.Generic;
using Extensions; // NetworkSingleton<T>
using HarmonyLib;
using UnityEngine;

namespace SandboxTweaks
{
    /// <summary>Shared elevator-button logic for the unlock-all-floors tweak.</summary>
    internal static class Elevator
    {
        /// <summary>Boss elevator stop = the last button in the list. -1 if unknown.</summary>
        public static int BossIndex(ElevatorManager elevator)
        {
            var buttons = GetButtons(elevator);
            return (buttons != null && buttons.Count > 0) ? buttons.Count - 1 : -1;
        }

        /// <summary>
        /// Enables every casino-floor button. The boss button (last entry) is left
        /// to normal progression — it only lights once currentFloor has legitimately
        /// reached the top casino floor, so unlock-all-floors can't skip to the boss.
        /// </summary>
        public static void ApplyButtons(ElevatorManager elevator)
        {
            var buttons = GetButtons(elevator);
            if (buttons == null || buttons.Count == 0) return;

            int bossIndex = buttons.Count - 1;
            bool bossUnlocked = CurrentFloor() + 1 >= bossIndex;

            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] == null) continue;
                bool active = (i < bossIndex) || bossUnlocked;
                buttons[i].gameObject.SetActive(active);
            }
        }

        /// <summary>True once the player has legitimately progressed to (or past) the boss stop.</summary>
        public static bool BossReachedLegitimately(ElevatorManager elevator)
        {
            int bossIndex = BossIndex(elevator);
            return bossIndex < 0 || CurrentFloor() + 1 >= bossIndex;
        }

        private static int CurrentFloor() =>
            NetworkSingleton<GameManager>.Instance != null
                ? NetworkSingleton<GameManager>.Instance.currentFloor
                : 0;

        private static List<Transform> GetButtons(ElevatorManager elevator)
        {
            if (elevator == null) return null;
            return Traverse.Create(elevator).Field("buttonList").GetValue<List<Transform>>();
        }
    }
}
