using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirehouseDeluxe
{
    public abstract class StartupScreen : MonoBehaviour
    {
        [SerializeField] private RawImage logoImage;

        private double finishTime;
        private bool shown;
        private bool skipArmed;
        private bool loading;

        protected abstract string NextScenePath { get; }
        protected virtual double DisplayDuration => double.PositiveInfinity;

        protected virtual IEnumerator Start()
        {
            Camera gameCamera = Camera.main;
            if (logoImage == null || logoImage.texture == null || gameCamera == null
                || gameCamera.GetComponent<PixelPerfectCamera>() == null
                || !Application.CanStreamedLevelBeLoaded(NextScenePath)
                || NextScenePath == gameObject.scene.path)
            {
                Debug.LogError($"{name}: check the UI image, shared Pixel Perfect Camera and next scene in Build Profiles.", this);
                enabled = false;
                yield break;
            }

            // Count display time from the first visible frame, including startup and shader compilation delays.
            yield return new WaitForEndOfFrame();
            finishTime = Time.realtimeSinceStartupAsDouble + DisplayDuration;
            shown = true;
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
                if (key.wasPressedThisFrame && WindowScaleControls.ScaleDirection(key.keyCode) == 0
                    && key.keyCode != Key.LeftShift && key.keyCode != Key.RightShift
                    && key.keyCode != Key.Tab)
                    return true;
            return false;
        }
    }
}
