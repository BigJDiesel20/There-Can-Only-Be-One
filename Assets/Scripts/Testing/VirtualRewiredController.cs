using System;
using Rewired;
using UnityEngine;
using VirtualControllers;

namespace VirtualControllers.Rewired
{
    /// <summary>
    /// Bridges a single VirtualControllerState into Rewired's ICustomController interface.
    /// 
    /// HOW IT WORKS
    /// ────────────
    /// Rewired supports "Custom Controllers" — hardware-agnostic input sources you register
    /// at runtime. Each VirtualRewiredController wraps one VirtualControllerState and feeds
    /// its values to Rewired every frame via ICustomController.SetAxisValue / SetButtonValue.
    ///
    /// SETUP (one-time, in your game bootstrap):
    ///   1. In the Rewired Input Manager, create a "Custom Controller" template with the
    ///      axes/buttons listed in the AxisId / ButtonId enums below, matching their indices.
    ///   2. Call VirtualRewiredBridge.SpawnBridge(playerIndex) for each virtual player.
    ///   3. Assign the resulting CustomController to the matching Rewired Player.
    ///
    /// See VirtualRewiredBridge (the MonoBehaviour driver) for a turnkey setup.
    /// </summary>
    public class VirtualRewiredController
    {
        // ── Axis indices — must match your Rewired Custom Controller template ──
        public enum AxisId
        {
            LeftStickX  = 0,
            LeftStickY  = 1,
            RightStickX = 2,
            RightStickY = 3,
            L2          = 4,
            R2          = 5,
        }

        // ── Button indices — must match your Rewired Custom Controller template ─
        public enum ButtonId
        {
            Cross     = 0,
            Circle    = 1,
            Square    = 2,
            Triangle  = 3,
            L1        = 4,
            R1        = 5,
            L3        = 6,
            R3        = 7,
            DpadUp    = 8,
            DpadDown  = 9,
            DpadLeft  = 10,
            DpadRight = 11,
            Options   = 12,
            Share     = 13,
            Touchpad  = 14,
            PS        = 15,
        }

        public const int AxisCount   = 6;
        public const int ButtonCount = 16;

        // ── References ────────────────────────────────────────────────
        public readonly VirtualControllerState State;
        public CustomController RewiredController { get; private set; }
        public int PlayerId => State.playerId;

        // ── Constructor ───────────────────────────────────────────────
        public VirtualRewiredController(VirtualControllerState state, CustomController rewiredController)
        {
            State             = state ?? throw new ArgumentNullException(nameof(state));
            RewiredController = rewiredController ?? throw new ArgumentNullException(nameof(rewiredController));
        }

        // ── Called every frame by VirtualRewiredBridge ─────────────────
        public void PushToRewired()
        {
            if (!State.isActive) return;

            // Axes
            RewiredController.SetAxisValue((int)AxisId.LeftStickX,  State.leftStickX);
            RewiredController.SetAxisValue((int)AxisId.LeftStickY,  State.leftStickY);
            RewiredController.SetAxisValue((int)AxisId.RightStickX, State.rightStickX);
            RewiredController.SetAxisValue((int)AxisId.RightStickY, State.rightStickY);
            RewiredController.SetAxisValue((int)AxisId.L2,          State.l2);
            RewiredController.SetAxisValue((int)AxisId.R2,          State.r2);

            // Buttons
            RewiredController.SetButtonValue((int)ButtonId.Cross,     State.cross);
            RewiredController.SetButtonValue((int)ButtonId.Circle,    State.circle);
            RewiredController.SetButtonValue((int)ButtonId.Square,    State.square);
            RewiredController.SetButtonValue((int)ButtonId.Triangle,  State.triangle);
            RewiredController.SetButtonValue((int)ButtonId.L1,        State.l1);
            RewiredController.SetButtonValue((int)ButtonId.R1,        State.r1);
            RewiredController.SetButtonValue((int)ButtonId.L3,        State.l3);
            RewiredController.SetButtonValue((int)ButtonId.R3,        State.r3);
            RewiredController.SetButtonValue((int)ButtonId.DpadUp,    State.dpadUp);
            RewiredController.SetButtonValue((int)ButtonId.DpadDown,  State.dpadDown);
            RewiredController.SetButtonValue((int)ButtonId.DpadLeft,  State.dpadLeft);
            RewiredController.SetButtonValue((int)ButtonId.DpadRight, State.dpadRight);
            RewiredController.SetButtonValue((int)ButtonId.Options,   State.options);
            RewiredController.SetButtonValue((int)ButtonId.Share,     State.share);
            RewiredController.SetButtonValue((int)ButtonId.Touchpad,  State.touchpad);
            RewiredController.SetButtonValue((int)ButtonId.PS,        State.ps);
        }
    }
}
