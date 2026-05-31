using UnityEngine;
using VirtualControllers;

namespace VirtualControllers
{
    /// <summary>
    /// Drop this on any GameObject to verify virtual controller states without Rewired.
    /// Reads the registry directly and logs state changes to the Console.
    /// Useful for smoke-testing before wiring up Rewired.
    /// </summary>
    public class VirtualControllerDebugReader : MonoBehaviour
    {
        [Header("Which player to monitor (0-based)")]
        public int playerIndex = 0;

        [Header("Log every N seconds (0 = every frame)")]
        public float logIntervalSeconds = 1f;

        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (logIntervalSeconds > 0f && _timer < logIntervalSeconds) return;
            _timer = 0f;

            var state = VirtualControllerRegistry.Instance?.GetState(playerIndex);
            if (state == null)
            {
                Debug.Log($"[VCDebug] Player {playerIndex + 1}: No active controller.");
                return;
            }

            Debug.Log(
                $"[VCDebug] P{playerIndex + 1} | " +
                $"LS({state.leftStickX:F2},{state.leftStickY:F2}) " +
                $"RS({state.rightStickX:F2},{state.rightStickY:F2}) " +
                $"L2={state.l2:F2} R2={state.r2:F2} | " +
                $"Cross={state.cross} Circle={state.circle} Sq={state.square} Tri={state.triangle} | " +
                $"L1={state.l1} R1={state.r1} | " +
                $"Dpad U{state.dpadUp} D{state.dpadDown} L{state.dpadLeft} R{state.dpadRight}"
            );
        }
    }
}
