using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FirehouseDeluxe
{
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public sealed class PixelPerfectCanvas : MonoBehaviour
    {
        [SerializeField] private RectTransform viewport;
        private Canvas canvas;
        private Camera gameCamera;
        private PixelPerfectCamera pixelPerfectCamera;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();
            gameCamera = Camera.main;
            if (gameCamera != null)
                pixelPerfectCamera = gameCamera.GetComponent<PixelPerfectCamera>();
        }

        private void LateUpdate()
        {
            // Follow the shared camera's integer zoom without a second resolution setting.
            if (pixelPerfectCamera != null)
                canvas.scaleFactor = Mathf.Max(1, pixelPerfectCamera.pixelRatio);
            if (viewport != null && gameCamera != null && Screen.width > 0 && Screen.height > 0)
            {
                // UI anchors stay inside the camera frame, excluding its letterbox margins.
                Rect frame = gameCamera.pixelRect;
                viewport.anchorMin = new Vector2(frame.xMin / Screen.width, frame.yMin / Screen.height);
                viewport.anchorMax = new Vector2(frame.xMax / Screen.width, frame.yMax / Screen.height);
                viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            }
        }
    }
}
