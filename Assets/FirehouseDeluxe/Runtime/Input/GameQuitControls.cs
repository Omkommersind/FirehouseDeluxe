using UnityEngine;
using UnityEngine.InputSystem;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class GameQuitControls : MonoBehaviour
    {
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (Application.isFocused && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                Application.Quit();
        }
    }
}
