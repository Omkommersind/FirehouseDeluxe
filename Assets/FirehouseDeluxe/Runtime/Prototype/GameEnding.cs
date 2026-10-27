using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class GameEnding : MonoBehaviour
    {
        private const string ScenePath = "Assets/Scenes/GameEnding.unity";
        private static double completionSeconds;

        private Camera gameCamera;
        private PixelPerfectCamera pixelPerfectCamera;
        private Texture2D textTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetResult() => completionSeconds = 0d;

        internal static bool TryShow(double elapsedSeconds)
        {
            if (!Application.CanStreamedLevelBeLoaded(ScenePath))
            {
                Debug.LogError("Не удалось открыть финальный экран: добавь GameEnding в Build Profiles.");
                return false;
            }

            completionSeconds = Math.Max(0d, elapsedSeconds);
            SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            return true;
        }

        private void Start()
        {
            gameCamera = Camera.main;
            if (gameCamera != null)
                pixelPerfectCamera = gameCamera.GetComponent<PixelPerfectCamera>();

            string time = RunTimeFormatter.Format(completionSeconds);
            string message = $"Поздравляем,\nвы прошли игру.\n\nВремя прохождения:\n{time}";
            textTexture = PixelFont.CreateCenteredText(message);
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || gameCamera == null
                || pixelPerfectCamera == null || textTexture == null)
                return;

            int pixelSize = Mathf.Max(1, pixelPerfectCamera.pixelRatio);
            Rect viewport = gameCamera.pixelRect;
            int width = textTexture.width;
            int height = textTexture.height;
            // Centre on the game-pixel grid, including when the remaining margin is odd.
            int x = Mathf.FloorToInt((viewport.width / pixelSize - width) * 0.5f);
            int y = Mathf.FloorToInt((viewport.height / pixelSize - height) * 0.5f);

            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(Mathf.Round(viewport.x) + x * pixelSize,
                Mathf.Round(Screen.height - viewport.yMax) + y * pixelSize,
                width * pixelSize, height * pixelSize), textTexture);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void OnDestroy()
        {
            if (textTexture != null)
                Destroy(textTexture);
        }
    }
}
