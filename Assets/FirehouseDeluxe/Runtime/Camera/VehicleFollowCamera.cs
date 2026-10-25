using UnityEngine;
using UnityEngine.Tilemaps;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    public sealed class VehicleFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField, Tooltip("Optional map renderer. Keeps the camera view inside the map.")]
        private Renderer mapBounds;
        [SerializeField, Tooltip("Width and height of the central free-movement area as fractions of the view. 0.3 means 30%; 0 disables it on that axis.")]
        private Vector2 deadZoneSize = new Vector2(0.3f, 0.3f);
        [SerializeField, Min(0.01f)] private float smoothTime = 0.22f;
        [SerializeField, Min(0f)] private float snapDistance = 30f;
        private Camera followCamera;
        private Bounds? tilemapBounds;
        private Vector3 velocity;

        public Transform Target { get => target; set => target = value; }
        public Renderer MapBounds { get => mapBounds; set => mapBounds = value; }

        public void SetCamera(Camera camera)
        {
            followCamera = camera;
            velocity = Vector3.zero;
        }

        private void Awake()
        {
            var tilemap = mapBounds != null ? mapBounds.GetComponent<Tilemap>() : null;
            if (tilemap != null)
            {
                // Empty road cells still belong to the level. Renderer bounds only cover visible tiles.
                Bounds localBounds = tilemap.localBounds;
                var worldBounds = new Bounds(tilemap.transform.TransformPoint(localBounds.center), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = localBounds.center + Vector3.Scale(localBounds.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f,
                            (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    worldBounds.Encapsulate(tilemap.transform.TransformPoint(point));
                }
                tilemapBounds = worldBounds;
            }
        }

        private void LateUpdate()
        {
            if (target == null || followCamera == null)
                return;

            Vector3 position = followCamera.transform.position;
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, position.z);
            Vector3 centeredPosition = ConstrainToMap(targetPosition);
            if (Vector3.Distance(position, centeredPosition) > snapDistance)
            {
                followCamera.transform.position = centeredPosition;
                velocity = Vector3.zero;
                return;
            }

            Vector2 halfDeadZone = GetDeadZoneHalfSize(followCamera);
            Vector3 destination = ConstrainToMap(new Vector3(
                FollowAxis(position.x, targetPosition.x, halfDeadZone.x),
                FollowAxis(position.y, targetPosition.y, halfDeadZone.y), position.z));

            // Discard smoothing momentum on each idle axis so the camera stays still inside the zone.
            if (Mathf.Approximately(destination.x, position.x))
                velocity.x = 0f;
            if (Mathf.Approximately(destination.y, position.y))
                velocity.y = 0f;

            // Follow position only: screen north stays fixed while the truck rotates.
            followCamera.transform.position = ConstrainToMap(Vector3.SmoothDamp(position, destination,
                ref velocity, smoothTime, Mathf.Infinity, Time.deltaTime));
        }

        private Vector2 GetDeadZoneHalfSize(Camera camera)
        {
            return new Vector2(camera.orthographicSize * camera.aspect * deadZoneSize.x,
                camera.orthographicSize * deadZoneSize.y);
        }

        private static float FollowAxis(float position, float targetPosition, float halfDeadZone)
        {
            return targetPosition - Mathf.Clamp(targetPosition - position, -halfDeadZone, halfDeadZone);
        }

        private void OnValidate()
        {
            deadZoneSize.x = Mathf.Clamp01(deadZoneSize.x);
            deadZoneSize.y = Mathf.Clamp01(deadZoneSize.y);
        }

        private void OnDrawGizmosSelected()
        {
            Camera camera = followCamera;
            if (camera == null)
            {
                var prefab = Resources.Load<GameObject>(PersistentGameCamera.ResourceName);
                if (prefab != null)
                    camera = prefab.GetComponent<Camera>();
            }
            if (camera == null)
                return;

            Vector2 halfDeadZone = GetDeadZoneHalfSize(camera);
            Vector3 position = followCamera != null ? followCamera.transform.position : transform.position;
            Vector3 center = new Vector3(position.x, position.y,
                target != null ? target.position.z : 0f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(center, new Vector3(halfDeadZone.x * 2f, halfDeadZone.y * 2f, 0f));
        }

        private Vector3 ConstrainToMap(Vector3 position)
        {
            if (mapBounds == null)
                return position;

            Bounds bounds = tilemapBounds ?? mapBounds.bounds;
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
