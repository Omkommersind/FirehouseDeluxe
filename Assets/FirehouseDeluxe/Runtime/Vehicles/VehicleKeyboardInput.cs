using UnityEngine;
using UnityEngine.InputSystem;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TopDownVehicle))]
    public sealed class VehicleKeyboardInput : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.25f), Tooltip("Ignore this small amount of trigger travel, then use the remaining range as an analog pedal.")]
        private float triggerDeadZone = 0.02f;

        private TopDownVehicle vehicle;
        private bool hasFocus = true;

        private void Awake()
        {
            vehicle = GetComponent<TopDownVehicle>();
        }

        private void Update()
        {
            if (!hasFocus)
            {
                vehicle.SetDriveInput(0f, 0f, false);
                return;
            }

            float throttle = 0f;
            float steering = 0f;
            bool handbrake = false;
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                throttle = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
                steering = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                    - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                handbrake = keyboard.spaceKey.isPressed;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                float accelerator = Mathf.Max(ReadPedal(gamepad.rightTrigger.ReadValue()),
                    gamepad.buttonSouth.isPressed ? 1f : 0f);
                float brake = Mathf.Max(ReadPedal(gamepad.leftTrigger.ReadValue()),
                    gamepad.buttonWest.isPressed ? 1f : 0f);
                // Braking still works while the accelerator is held.
                float pedalInput = brake > 0f ? -brake : accelerator;
                throttle = GamepadControls.StrongerAxis(throttle, pedalInput);
                steering = GamepadControls.StrongerAxis(steering, GamepadControls.ReadHorizontal(gamepad));
                handbrake |= gamepad.buttonEast.isPressed;
            }

            vehicle.SetDriveInput(throttle, steering, handbrake);

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                vehicle.ResetToSpawn();
        }

        private float ReadPedal(float value) =>
            Mathf.InverseLerp(triggerDeadZone, 1f, value);

        private void OnApplicationFocus(bool hasFocus)
        {
            this.hasFocus = hasFocus;
            if (!hasFocus && vehicle != null)
                vehicle.SetDriveInput(0f, 0f, false);
        }

        private void OnDisable()
        {
            if (vehicle != null)
                vehicle.SetDriveInput(0f, 0f, false);
        }
    }
}
