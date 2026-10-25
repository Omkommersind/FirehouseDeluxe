using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirehouseDeluxe.Editor
{
    internal static class WindowRescueBackgroundPicking
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => DisablePicking(scene);
            EditorApplication.delayCall += () =>
            {
                for (int index = 0; index < SceneManager.sceneCount; index++)
                    DisablePicking(SceneManager.GetSceneAt(index));
            };
        }

        private static void DisablePicking(Scene scene)
        {
            if (!scene.isLoaded || scene.path != "Assets/Scenes/WindowRescue.unity")
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "Background")
                    continue;

                if (!SceneVisibilityManager.instance.IsPickingDisabled(root))
                {
                    SceneVisibilityManager.instance.DisablePicking(root, true);
                    Debug.Log("WindowRescue: Background selection with the Scene mouse is disabled.", root);
                }
                if (Selection.activeGameObject == root)
                    Selection.activeGameObject = null;
            }
        }
    }
}
