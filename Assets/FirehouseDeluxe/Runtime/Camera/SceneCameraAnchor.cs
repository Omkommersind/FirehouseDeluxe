using UnityEngine;

namespace FirehouseDeluxe
{
    // Scenes provide a starting pose and optional following, but do not own a camera.
    [DisallowMultipleComponent]
    public sealed class SceneCameraAnchor : MonoBehaviour
    {
        private void Start() => PersistentGameCamera.UseAnchor(transform);
    }
}
