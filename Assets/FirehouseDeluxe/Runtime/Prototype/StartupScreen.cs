using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FirehouseDeluxe
{
    public abstract class StartupScreen : MonoBehaviour
    {
        [SerializeField] private Texture2D logo;

        private Camera gameCamera;
        private PixelPerfectCamera pixelPerfectCamera;
        private double finishTime;
        private bool shown;
        private bool skipArmed;
        private bool loading;

        protected abstract string NextScenePath { get; }
        protected virtual double DisplayDuration => double.PositiveInfinity;

        protected virtual void Start()
        {
            gameCamera = Camera.main;
            if (gameCamera != null)
                pixelPerfectCamera = gameCamera.GetComponent<PixelPerfectCamera>();

            if (logo == null || gameCamera == null || pixelPerfectCamera == null
                || !Application.CanStreamedLevelBeLoaded(NextScenePath)
                || NextScenePath == gameObject.scene.path)
            {
                Debug.LogError($"{name}: проверь изображение, общую Pixel Perfect Camera и следующую сцену в Build Profiles.", this);
                enabled = false;
            }
        }

        protected virtual void Update()
        {
            if (loading || !shown)
                return;

            bool skipRequested = false;
            if (skipArmed)
                skipRequested = AnyKeyPressed() || GamepadControls.AnyButtonPressedThisFrame();
            else
            {
                // Require a fresh press after releasing buttons used on the previous screen.
                Keyboard keyboard = Keyboard.current;
                skipArmed = (keyboard == null || !keyboard.anyKey.isPressed)
                    && !GamepadControls.AnyButtonHeld();
            }

            if (Time.realtimeSinceStartupAsDouble < finishTime && !skipRequested)
                return;

            loading = true;
            SceneManager.LoadSceneAsync(NextScenePath, LoadSceneMode.Single);
        }

        private static bool AnyKeyPressed()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return false;

            // Check keys individually so a new press also works while another key is held.
            foreach (var key in keyboard.allKeys)
                if (key.wasPressedThisFrame)
                    return true;
            return false;
        }

        protected virtual void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || logo == null
                || gameCamera == null || pixelPerfectCamera == null)
                return;

            int pixelSize = Mathf.Max(1, pixelPerfectCamera.pixelRatio);
            Rect viewport = gameCamera.pixelRect;
            int width = logo.width * pixelSize;
            int height = logo.height * pixelSize;
            var logoRect = new Rect(Mathf.Round(viewport.center.x - width * 0.5f),
                Mathf.Round(Screen.height - viewport.center.y - height * 0.5f), width, height);

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            GUI.DrawTexture(logoRect, logo);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;

            if (!shown)
            {
                // Startup and shader compilation can delay the first visible frame.
                finishTime = Time.realtimeSinceStartupAsDouble + DisplayDuration;
                shown = true;
            }
        }
    }
}
