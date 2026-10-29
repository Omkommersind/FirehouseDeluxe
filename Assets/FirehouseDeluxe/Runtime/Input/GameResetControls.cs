using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class GameResetControls : MonoBehaviour
    {
        private const string StartupScenePath = "Assets/Scenes/StudioSplash.unity";

        private void LateUpdate()
        {
            Keyboard keyboard = Keyboard.current;
            if (!Application.isFocused || keyboard == null || !keyboard.tabKey.wasPressedThisFrame)
                return;

            if (!Application.CanStreamedLevelBeLoaded(StartupScenePath))
            {
                Debug.LogError("Не удалось перезапустить игру: добавь StudioSplash в Build Profiles.", this);
                return;
            }

            // Reset after this frame's scene transitions; Single also removes the suspended rescue map.
            SceneManager.LoadScene(StartupScenePath, LoadSceneMode.Single);
        }
    }
}
