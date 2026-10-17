using UnityEngine;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class VehicleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Tooltip("Optional map renderer. Keeps the camera view inside the map.")]
        private Renderer mapBounds;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.22f;
        [SerializeField, Min(0f)] private float snapDistance = 30f;
        private Camera followCamera;
        private Vector3 velocity;

        public Transform Target { get => target; set => target = value; }
        public Renderer MapBounds { get => mapBounds; set => mapBounds = value; }

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 destination = ConstrainToMap(new Vector3(target.position.x, target.position.y,
                transform.position.z));
            if (Vector3.Distance(transform.position, destination) > snapDistance)
            {
                transform.position = destination;
                velocity = Vector3.zero;
                return;
            }

            // Follow position only: screen north stays fixed while the truck rotates.
            transform.position = ConstrainToMap(Vector3.SmoothDamp(transform.position, destination,
                ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime));
        }

        private Vector3 ConstrainToMap(Vector3 position)
        {
            if (mapBounds == null)
                return position;

            Bounds bounds = mapBounds.bounds;
            float halfHeight = followCamera.orthographicSize;
            float halfWidth = halfHeight * followCamera.aspect;
            position.x = ClampAxis(position.x, bounds.min.x, bounds.max.x, halfWidth);
            position.y = ClampAxis(position.y, bounds.min.y, bounds.max.y, halfHeight);
            return position;
        }

        private static float ClampAxis(float position, float min, float max, float halfView)
        {
            // When the map is smaller than the viewport, keep its centre in view.
            return max - min <= halfView * 2f ? (min + max) * 0.5f
                : Mathf.Clamp(position, min + halfView, max - halfView);
        }
    }
}
