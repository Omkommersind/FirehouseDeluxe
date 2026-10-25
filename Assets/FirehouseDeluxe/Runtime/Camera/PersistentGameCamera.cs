using UnityEngine;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PersistentGameCamera : MonoBehaviour
    {
        public const string ResourceName = "MainCamera";
        private static PersistentGameCamera instance;
        private Camera gameCamera;
        private VehicleFollowCamera follower;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateCamera()
        {
            if (instance != null)
                return;

            var prefab = Resources.Load<GameObject>(ResourceName);
            if (prefab == null)
            {
                Debug.LogError("Не найден Resources/MainCamera.prefab.");
                return;
            }

            Instantiate(prefab).name = "Main Camera";
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            gameCamera = GetComponent<Camera>();
            DontDestroyOnLoad(gameObject);
        }

        public static void UseAnchor(Transform anchor)
        {
            if (instance == null)
            {
                Debug.LogError("Постоянная камера не создана.", anchor);
                return;
            }

            if (instance.follower != null)
                instance.follower.SetCamera(null);

            instance.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            instance.follower = anchor.GetComponent<VehicleFollowCamera>();
            if (instance.follower != null)
                instance.follower.SetCamera(instance.gameCamera);
        }
    }
}
