using UnityEngine;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownVehicle : MonoBehaviour
    {
        [Header("Engine (world units = metres)")]
        [SerializeField, Min(0.1f), Tooltip("Maximum forward speed in metres per second. Multiply by 3.6 for km/h.")]
        private float maxForwardSpeed = 18f;
        [SerializeField, Min(0.1f)] private float maxReverseSpeed = 5f;
        [SerializeField, Min(0.1f)] private float acceleration = 5f;
        [SerializeField, Min(0.1f)] private float reverseAcceleration = 3f;
        [SerializeField, Min(0.1f)] private float brakingDeceleration = 12f;
        [SerializeField, Min(0f)] private float rollingResistance = 0.65f;
        [SerializeField, Min(0f)] private float aerodynamicDrag = 0.008f;

        [Header("Steering")]
        [SerializeField, Min(0.1f), Tooltip("Distance between the axles, in world units.")]
        private float wheelbase = 3.2f;
        [SerializeField, Range(1f, 60f)] private float lowSpeedSteerAngle = 32f;
        [SerializeField, Range(1f, 60f)] private float highSpeedSteerAngle = 14f;
        [SerializeField, Min(1f), Tooltip("How quickly the front wheels turn, in degrees per second.")]
        private float steeringResponse = 110f;
        [SerializeField, Min(0f), Tooltip("How quickly sideways sliding is corrected. Higher means more grip.")]
        private float lateralGrip = 9f;

        [Header("Handbrake")]
        [SerializeField, Min(0f)] private float handbrakeDeceleration = 14f;
        [SerializeField, Min(0f)] private float handbrakeLateralGrip = 2f;

        private Rigidbody2D body;
        private Vector2 spawnPosition;
        private float spawnRotation;
        private float throttle;
        private float steering;
        private float currentSteerAngle;
        private bool handbrake;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeed => body == null ? 0f : Vector2.Dot(body.linearVelocity, Forward);

        // Use the physics pose rather than the interpolated render transform.
        private Vector2 Forward => new Vector2(-Mathf.Sin(body.rotation * Mathf.Deg2Rad),
            Mathf.Cos(body.rotation * Mathf.Deg2Rad));

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            spawnPosition = body.position;
            spawnRotation = body.rotation;
        }

        /// <summary>Positive throttle drives forwards; positive steering turns the wheels right.</summary>
        public void SetDriveInput(float throttleInput, float steeringInput, bool useHandbrake)
        {
            throttle = Mathf.Clamp(throttleInput, -1f, 1f);
            steering = Mathf.Clamp(steeringInput, -1f, 1f);
            handbrake = useHandbrake;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 forward = Forward;
            Vector2 right = new Vector2(forward.y, -forward.x);
            float forwardSpeed = Vector2.Dot(body.linearVelocity, forward);
            float sidewaysSpeed = Vector2.Dot(body.linearVelocity, right);

            if (handbrake)
            {
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, handbrakeDeceleration * dt);
            }
            else if (!Mathf.Approximately(throttle, 0f))
            {
                // The opposite pedal brakes to a stop first. Reverse starts on a later physics tick.
                bool braking = (throttle > 0f && forwardSpeed < 0f)
                    || (throttle < 0f && forwardSpeed > 0f);
                float target = throttle > 0f ? maxForwardSpeed * throttle : maxReverseSpeed * throttle;
                float rate = braking ? brakingDeceleration
                    : throttle > 0f ? acceleration : reverseAcceleration;
                forwardSpeed = Mathf.MoveTowards(forwardSpeed, braking ? 0f : target,
                    rate * Mathf.Abs(throttle) * dt);
            }

            float resistance = rollingResistance + aerodynamicDrag * forwardSpeed * forwardSpeed;
            forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f, resistance * dt);
            forwardSpeed = Mathf.Clamp(forwardSpeed, -maxReverseSpeed, maxForwardSpeed);

            float grip = handbrake ? handbrakeLateralGrip : lateralGrip;
            sidewaysSpeed *= Mathf.Exp(-grip * dt);
            body.linearVelocity = forward * forwardSpeed + right * sidewaysSpeed;

            float speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxForwardSpeed);
            float maxSteer = Mathf.Lerp(lowSpeedSteerAngle, highSpeedSteerAngle, speedRatio);
            currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, steering * maxSteer,
                steeringResponse * dt);

            // Bicycle steering: no rotation at rest, a wider circle at speed, reversed yaw in reverse.
            float yawRate = forwardSpeed / wheelbase
                * Mathf.Tan(currentSteerAngle * Mathf.Deg2Rad) * Mathf.Rad2Deg;
            body.angularVelocity = 0f;
            body.MoveRotation(body.rotation - yawRate * dt);
        }

        public void ResetToSpawn()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = spawnPosition;
            body.rotation = spawnRotation;
            currentSteerAngle = 0f;
            SetDriveInput(0f, 0f, false);
        }

        private void OnDisable()
        {
            SetDriveInput(0f, 0f, false);
        }
    }
}
