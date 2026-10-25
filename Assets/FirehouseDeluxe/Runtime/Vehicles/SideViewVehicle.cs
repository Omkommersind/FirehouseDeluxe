using UnityEngine;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SideViewVehicle : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("Maximum horizontal speed in world units per second.")]
        private float maxSpeed = 12f;
        [SerializeField, Min(0.1f), Tooltip("Acceleration while a direction is held, including changing direction.")]
        private float acceleration = 50f;
        [SerializeField, Min(0.1f), Tooltip("How quickly the truck slows down after releasing the keys.")]
        private float deceleration = 40f;

        private Rigidbody2D body;
        private float moveInput;

        public float HorizontalSpeed => body != null ? body.linearVelocity.x : 0f;
        public bool HasMoveInput => !Mathf.Approximately(moveInput, 0f);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0f;
            body.constraints |= RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }

        public void SetMoveInput(float input) => moveInput = Mathf.Clamp(input, -1f, 1f);

        private void FixedUpdate()
        {
            float targetSpeed = moveInput * maxSpeed;
            float rate = Mathf.Approximately(moveInput, 0f) ? deceleration : acceleration;
            float speed = Mathf.MoveTowards(body.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(speed, 0f);
        }

        private void OnDisable()
        {
            moveInput = 0f;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }
}
