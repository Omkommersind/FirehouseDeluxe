using UnityEngine;
using UnityEngine.InputSystem;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TopDownVehicle))]
    public sealed class VehicleKeyboardInput : MonoBehaviour
    {
        private TopDownVehicle vehicle;
        private bool hasFocus = true;

        private void Awake()
        {
            vehicle = GetComponent<TopDownVehicle>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !hasFocus)
            {
                vehicle.SetDriveInput(0f, 0f, false);
                return;
            }

            float throttle = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            float steering = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);

            vehicle.SetDriveInput(throttle, steering, keyboard.spaceKey.isPressed);

            if (keyboard.rKey.wasPressedThisFrame)
                vehicle.ResetToSpawn();
        }

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
