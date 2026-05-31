using UnityEngine;

namespace VirtualControllers
{
    /// <summary>
    /// Holds the full input state of a single virtual PS4-style controller.
    /// One instance per emulated player. Safe to read/write from any thread (plain fields).
    /// </summary>
    [System.Serializable]
    public class VirtualControllerState
    {
        // ── Identity ──────────────────────────────────────────────────
        public int  playerId   = 0;
        public bool isActive   = true;

        // ── Face buttons ──────────────────────────────────────────────
        public bool cross,    circle,  square,   triangle;

        // ── Shoulder / trigger ────────────────────────────────────────
        public bool l1,       r1;
        public float l2       = 0f;   // 0..1
        public float r2       = 0f;   // 0..1

        // ── Sticks (axis values -1..1) ────────────────────────────────
        public float leftStickX  = 0f;
        public float leftStickY  = 0f;
        public bool  l3;

        public float rightStickX = 0f;
        public float rightStickY = 0f;
        public bool  r3;

        // ── D-Pad ─────────────────────────────────────────────────────
        public bool dpadUp, dpadDown, dpadLeft, dpadRight;

        // ── System buttons ────────────────────────────────────────────
        public bool options,  share,   touchpad,  ps;

        // ── Helpers ───────────────────────────────────────────────────
        public void Reset()
        {
            cross = circle = square = triangle = false;
            l1    = r1    = l3     = r3       = false;
            l2    = r2    = 0f;
            leftStickX  = leftStickY  = 0f;
            rightStickX = rightStickY = 0f;
            dpadUp = dpadDown = dpadLeft = dpadRight = false;
            options = share = touchpad = ps = false;
        }

        /// <summary>Returns a clone snapshot (useful for frame-diff checks).</summary>
        public VirtualControllerState Clone()
        {
            return (VirtualControllerState)MemberwiseClone();
        }
    }
}
