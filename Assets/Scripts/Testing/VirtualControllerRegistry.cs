using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualControllers
{
    /// <summary>
    /// Central registry for all virtual controllers.
    /// Survives play-mode transitions via DontDestroyOnLoad.
    /// Access via VirtualControllerRegistry.Instance.
    /// </summary>
    public class VirtualControllerRegistry : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────
        private static VirtualControllerRegistry _instance;
        public  static VirtualControllerRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[VirtualControllerRegistry]");
                    _instance = go.AddComponent<VirtualControllerRegistry>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // ── State ─────────────────────────────────────────────────────
        public const int MaxPlayers = 16;

        private readonly VirtualControllerState[] _states =
            new VirtualControllerState[MaxPlayers];

        public IReadOnlyList<VirtualControllerState> States => _states;

        // ── Events ────────────────────────────────────────────────────
        /// <summary>Fired whenever a controller is added or removed (Editor-safe).</summary>
        public static event Action<int, bool> OnControllerActiveChanged;

        // ── Lifecycle ─────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Adds (activates) a virtual controller slot for the given player index (0-based).</summary>
        public VirtualControllerState AddController(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers)
                throw new ArgumentOutOfRangeException(nameof(playerIndex));

            if (_states[playerIndex] == null)
                _states[playerIndex] = new VirtualControllerState { playerId = playerIndex };

            _states[playerIndex].isActive = true;
            OnControllerActiveChanged?.Invoke(playerIndex, true);
            Debug.Log($"[VirtualControllers] Controller added for player {playerIndex + 1}.");
            return _states[playerIndex];
        }

        /// <summary>Removes (deactivates) a virtual controller slot.</summary>
        public void RemoveController(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers) return;
            if (_states[playerIndex] == null) return;

            _states[playerIndex].isActive = false;
            _states[playerIndex].Reset();
            OnControllerActiveChanged?.Invoke(playerIndex, false);
            Debug.Log($"[VirtualControllers] Controller removed for player {playerIndex + 1}.");
        }

        /// <summary>Returns the state for a player, or null if not active.</summary>
        public VirtualControllerState GetState(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= MaxPlayers) return null;
            var s = _states[playerIndex];
            return (s != null && s.isActive) ? s : null;
        }

        /// <summary>How many slots are currently active.</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxPlayers; i++)
                    if (_states[i] != null && _states[i].isActive) count++;
                return count;
            }
        }

        /// <summary>Deactivates all controllers and resets all state.</summary>
        public void Clear()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                if (_states[i] != null)
                {
                    _states[i].isActive = false;
                    _states[i].Reset();
                    OnControllerActiveChanged?.Invoke(i, false);
                }
            }
        }
    }
}
