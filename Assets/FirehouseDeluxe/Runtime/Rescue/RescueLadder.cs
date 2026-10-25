using FirehouseDeluxe.Vehicles;
using UnityEngine;

namespace FirehouseDeluxe.Rescue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RescueLadder : MonoBehaviour
    {
        [SerializeField] private Transform descentEnd;
        [SerializeField, Min(0f), Tooltip("Maximum residual speed considered stationary.")]
        private float stationarySpeed = 0.05f;
        private SideViewVehicle vehicle;
        private BoxCollider2D pickup;

        public bool IsMoving => vehicle != null
            && (vehicle.HasMoveInput || Mathf.Abs(vehicle.HorizontalSpeed) > stationarySpeed);
        public bool CanCatch => isActiveAndEnabled && vehicle != null && descentEnd != null && !IsMoving;
        public Vector2 TopPosition => pickup.bounds.center;
        public Vector2 EndPosition => descentEnd.position;
        public float HorizontalSpeed => vehicle != null ? vehicle.HorizontalSpeed : 0f;

        private void Awake()
        {
            vehicle = GetComponentInParent<SideViewVehicle>();
            pickup = GetComponent<BoxCollider2D>();
            pickup.isTrigger = true;
            if (vehicle == null || descentEnd == null)
            {
                Debug.LogError("У лестницы должны быть машина и точка Descent End.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryCatch(other);

        // A truck arriving with inertia must be able to catch after coming to rest in the overlap.
        private void OnTriggerStay2D(Collider2D other) => TryCatch(other);

        private void TryCatch(Collider2D other)
        {
            if (CanCatch && other.TryGetComponent(out RescuePerson person))
                person.TryBeginDescent(this);
        }

        private void OnDrawGizmos()
        {
            if (descentEnd == null)
                return;
            var area = GetComponent<BoxCollider2D>();
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.cyan;
            Vector3 top = area != null ? transform.TransformPoint(area.offset) : transform.position;
            Gizmos.DrawLine(top, descentEnd.position);
            Gizmos.DrawWireSphere(descentEnd.position, 0.3f);
            Gizmos.color = previousColor;
        }
    }
}
