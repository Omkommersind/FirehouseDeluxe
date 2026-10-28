using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class GameEnding : MonoBehaviour
    {
        private const string ScenePath = "Assets/Scenes/GameEnding.unity";
        private static double completionSeconds;

        [SerializeField] private Text completionTime;

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
            if (completionTime != null)
                completionTime.text = RunTimeFormatter.Format(completionSeconds);
            else
                Debug.LogError("GameEnding: assign the Completion Time text.", this);
        }
    }
}
