using UnityEngine;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TopDownVehicle : MonoBehaviour, IVehicleEngineState
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

        [Header("Fuel")]
        [SerializeField, Min(0.1f)] private float fuelCapacity = 100f;
        [SerializeField, Min(0f), Tooltip("Fuel consumed per second at full throttle.")]
        private float fuelConsumptionPerSecond = 1f;
        [SerializeField, Min(0.1f), Tooltip("Maximum speed while pushing the vehicle with an empty tank.")]
        private float emptyFuelPushSpeed = 1.2f;
        [SerializeField, Min(0.1f), Tooltip("Acceleration while pushing the vehicle with an empty tank.")]
        private float emptyFuelPushAcceleration = 0.75f;

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
        private float throttle;
        private float steering;
        private float currentSteerAngle;
        private float fuel;
        private bool handbrake;

        public float SpeedKph => body == null ? 0f : body.linearVelocity.magnitude * 3.6f;
        public float ForwardSpeed => body == null ? 0f : Vector2.Dot(body.linearVelocity, Forward);
        public float Fuel => fuel;
        public float FuelCapacity => fuelCapacity;
        public float FuelNormalized => fuelCapacity <= 0f ? 0f : fuel / fuelCapacity;
        public bool HasFuel => fuel > 0f;
        public bool IsAccelerating => HasFuel && !handbrake
            && !Mathf.Approximately(throttle, 0f) && !IsBraking;

        private bool IsBraking => throttle * ForwardSpeed < 0f;

        // Use the physics pose rather than the interpolated render transform.
        private Vector2 Forward => new Vector2(-Mathf.Sin(body.rotation * Mathf.Deg2Rad),
            Mathf.Cos(body.rotation * Mathf.Deg2Rad));

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0f;
            body.angularDamping = 0f;
            RefillFuel();
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
                if (IsBraking)
                {
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, 0f,
                        brakingDeceleration * Mathf.Abs(throttle) * dt);
                }
                else if (HasFuel)
                {
                    float target = throttle > 0f ? maxForwardSpeed * throttle : maxReverseSpeed * throttle;
                    float rate = throttle > 0f ? acceleration : reverseAcceleration;
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, target,
                        rate * Mathf.Abs(throttle) * dt);
                    ConsumeFuel(Mathf.Abs(throttle) * fuelConsumptionPerSecond * dt);
                }
                else
                {
                    float target = emptyFuelPushSpeed * Mathf.Sign(throttle);
                    forwardSpeed = Mathf.MoveTowards(forwardSpeed, target,
                        emptyFuelPushAcceleration * Mathf.Abs(throttle) * dt);
                }
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

        public void Stop()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            currentSteerAngle = 0f;
            SetDriveInput(0f, 0f, false);
        }

        public void AddFuel(float amount)
        {
            fuel = Mathf.Clamp(fuel + Mathf.Max(0f, amount), 0f, fuelCapacity);
        }

        public void RefillFuel()
        {
            fuel = fuelCapacity;
        }

        private void ConsumeFuel(float amount)
        {
            fuel = Mathf.Max(0f, fuel - amount);
        }

        private void OnDisable()
        {
            SetDriveInput(0f, 0f, false);
        }
    }
}
