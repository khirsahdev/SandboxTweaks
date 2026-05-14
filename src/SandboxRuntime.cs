using System.Collections;
using Extensions; // NetworkSingleton<T>
using Mirror;     // NetworkServer
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandboxTweaks
{
    /// <summary>
    /// Re-applies the runtime tweaks every time a playable scene loads.
    /// A lost run resets the save to vanilla and bounces through LoseStateScene →
    /// HomeScene → CasinoScene; relying on a single Harmony hook to re-apply the
    /// floor unlock is timing-fragile. Hooking sceneLoaded makes it bulletproof.
    /// </summary>
    internal class SandboxRuntime : MonoBehaviour
    {
        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "CasinoScene" && scene.name != "HomeScene")
                return;

            // Re-read the marker for the active save and re-apply day length.
            SandboxState.RefreshFromCurrentSave();

            if (scene.name == "CasinoScene" && SandboxState.UnlockFloors)
                StartCoroutine(ApplyFloorUnlock());
        }

        /// <summary>
        /// Waits for the ElevatorManager to spawn, then unlocks the casino-floor
        /// buttons — host-authoritative via RpcEnableAllButtons (reaches non-modded
        /// clients) followed by a local pass that re-gates the boss button behind
        /// real progression (the RPC lights every button, including the boss).
        /// </summary>
        private IEnumerator ApplyFloorUnlock()
        {
            ElevatorManager elevator = null;
            for (int i = 0; i < 900; i++) // ~15s at 60fps, plenty for scene init
            {
                elevator = NetworkSingleton<ElevatorManager>.Instance;
                if (elevator != null) break;
                yield return null;
            }

            if (elevator == null)
            {
                Plugin.Log.LogWarning("[SandboxTweaks] ElevatorManager never appeared — floors not unlocked");
                yield break;
            }

            // Let the elevator finish its own Start/Initialize first.
            yield return null;
            yield return null;

            if (NetworkServer.active)
                elevator.RpcEnableAllButtons();

            // RpcEnableAllButtons lights every button incl. the boss; re-gate it.
            yield return null;
            Elevator.ApplyButtons(elevator);

            Plugin.Log.LogInfo("[SandboxTweaks] floor unlock applied on CasinoScene load");
        }
    }
}
