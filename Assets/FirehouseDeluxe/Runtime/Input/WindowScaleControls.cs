using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PixelPerfectCamera))]
    public sealed class WindowScaleControls : MonoBehaviour
    {
        [SerializeField, Min(1), Tooltip("Largest integer window scale; the available display area may limit it further.")]
        private int maximumScale = 5;

        private PixelPerfectCamera pixelCamera;

        private void Awake() => pixelCamera = GetComponent<PixelPerfectCamera>();

        private void Update()
        {
            // Changing the standalone window must never resize the editor's Game View or exit fullscreen.
            if (Application.isEditor || !Application.isFocused || Screen.fullScreenMode != FullScreenMode.Windowed)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            int direction = 0;
            foreach (var key in keyboard.allKeys)
                if (key.wasPressedThisFrame)
                    direction += ScaleDirection(key.keyCode);
            direction = Mathf.Clamp(direction, -1, 1);
            if (direction == 0)
                return;

            int baseWidth = pixelCamera.refResolutionX;
            int baseHeight = pixelCamera.refResolutionY;
            int limit = Mathf.Max(1, maximumScale);
            RectInt workArea = Screen.mainWindowDisplayInfo.workArea;
            if (workArea.width > 0 && workArea.height > 0)
                limit = Mathf.Clamp(Mathf.Min(workArea.width / baseWidth, workArea.height / baseHeight), 1, limit);

            int currentScale = Mathf.Max(1, Mathf.Min(Screen.width / baseWidth, Screen.height / baseHeight));
            int nextScale = Mathf.Clamp(currentScale + direction, 1, limit);
            int width = baseWidth * nextScale;
            int height = baseHeight * nextScale;
            if (Screen.width != width || Screen.height != height)
                Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        // Startup screens reserve the same keys instead of treating window resizing as "press any key".
        internal static int ScaleDirection(Key key) => key switch
        {
            Key.Equals or Key.NumpadPlus => 1,
            Key.Minus or Key.NumpadMinus => -1,
            _ => 0
        };
    }
}
