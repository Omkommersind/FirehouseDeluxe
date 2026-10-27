using UnityEngine;
using UnityEngine.InputSystem;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SideViewVehicle))]
    public sealed class SideViewVehicleKeyboardInput : MonoBehaviour
    {
        private SideViewVehicle vehicle;
        private bool hasFocus = true;

        private void Awake() => vehicle = GetComponent<SideViewVehicle>();

        private void Update()
        {
            if (!hasFocus)
            {
                vehicle.SetMoveInput(0f);
                return;
            }

            float input = GamepadControls.ReadHorizontal(Gamepad.current);
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float keyboardInput = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                input = GamepadControls.StrongerAxis(keyboardInput, input);
            }
            vehicle.SetMoveInput(input);
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            if (!focused && vehicle != null)
                vehicle.SetMoveInput(0f);
        }

        private void OnDisable()
        {
            if (vehicle != null)
                vehicle.SetMoveInput(0f);
        }
    }
}
