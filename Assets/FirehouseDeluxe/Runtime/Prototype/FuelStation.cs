using FirehouseDeluxe.Vehicles;
using UnityEngine;

namespace FirehouseDeluxe
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class FuelStation : MonoBehaviour
    {
        private const float PixelsPerUnit = 4f;
        [SerializeField, Min(0.1f), Tooltip("Fuel units added per second while a vehicle remains inside the trigger.")]
        private float fuelPerSecond = 5f;

        public float FuelPerSecond { get => fuelPerSecond; set => fuelPerSecond = Mathf.Max(0.1f, value); }

        private void OnTriggerStay2D(Collider2D other)
        {
            TopDownVehicle vehicle = other.GetComponentInParent<TopDownVehicle>();
            if (vehicle != null)
                vehicle.AddFuel(fuelPerSecond * Time.fixedDeltaTime);
        }

        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
            SnapToPixelGrid();
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (!Application.isPlaying)
                SnapToPixelGrid();
        }
#endif

        private void SnapToPixelGrid()
        {
            Vector3 position = transform.position;
            Vector3 snappedPosition = new Vector3(
                Mathf.Round(position.x * PixelsPerUnit) / PixelsPerUnit,
                Mathf.Round(position.y * PixelsPerUnit) / PixelsPerUnit,
                position.z);
            if (position != snappedPosition)
                transform.position = snappedPosition;
        }
    }
}
