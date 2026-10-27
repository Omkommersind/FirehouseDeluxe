using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace FirehouseDeluxe
{
    internal static class GamepadControls
    {
        public static float StrongerAxis(float first, float second) =>
            Mathf.Abs(first) >= Mathf.Abs(second) ? first : second;

        public static float ReadHorizontal(Gamepad gamepad)
        {
            if (gamepad == null)
                return 0f;

            // Read the whole stick so the Input System's radial dead zone is applied.
            return StrongerAxis(gamepad.dpad.ReadValue().x, gamepad.leftStick.ReadValue().x);
        }

        public static bool AnyButtonHeld() => HasButton(false);
        public static bool AnyButtonPressedThisFrame() => HasButton(true);

        private static bool HasButton(bool pressedThisFrame)
        {
            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
                return false;

            foreach (var control in gamepad.allControls)
            {
                if (!(control is ButtonControl button))
                    continue;
                // Ignore synthetic stick directions: drift must never advance a startup screen.
                if (button.parent != gamepad && button.parent != gamepad.dpad)
                    continue;
                if (pressedThisFrame ? button.wasPressedThisFrame : button.isPressed)
                    return true;
            }
            return false;
        }
    }
}
