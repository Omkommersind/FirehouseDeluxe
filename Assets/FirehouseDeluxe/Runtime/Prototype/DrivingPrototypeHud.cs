using FirehouseDeluxe.Vehicles;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FirehouseDeluxe
{
    public sealed class DrivingPrototypeHud : MonoBehaviour
    {
        [SerializeField] private TopDownVehicle vehicle;
        [SerializeField] private FuelGaugeSettings fuelGauge;
        private Camera hudCamera;
        private PixelPerfectCamera pixelPerfectCamera;

        public TopDownVehicle Vehicle { get => vehicle; set => vehicle = value; }

        private void Awake()
        {
            if (fuelGauge == null)
                fuelGauge = Resources.Load<FuelGaugeSettings>("FuelGaugeSettings");
            hudCamera = Camera.main;
            if (hudCamera != null)
                pixelPerfectCamera = hudCamera.GetComponent<PixelPerfectCamera>();
        }

        private void OnGUI()
        {
            if (vehicle == null)
                return;

            DrawFuelGauge();
        }

        private void DrawFuelGauge()
        {
            if (Event.current.type != EventType.Repaint || fuelGauge == null || fuelGauge.Icon == null
                || hudCamera == null || pixelPerfectCamera == null)
                return;

            // Use the camera's real viewport and integer zoom, including its black margins.
            int pixelSize = Mathf.Max(1, pixelPerfectCamera.pixelRatio);
            Rect viewport = hudCamera.pixelRect;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;
            GUI.matrix = Matrix4x4.identity;
            GUI.BeginGroup(new Rect(Mathf.Round(viewport.x), Mathf.Round(Screen.height - viewport.yMax),
                Mathf.Round(viewport.width), Mathf.Round(viewport.height)));

            Vector2Int position = fuelGauge.Position;
            Texture2D icon = fuelGauge.Icon;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(position.x * pixelSize, position.y * pixelSize,
                icon.width * pixelSize, icon.height * pixelSize), icon);

            Vector2Int barSize = fuelGauge.BarSize;
            int barX = position.x + icon.width + fuelGauge.Gap;
            int barY = position.y + (icon.height - barSize.y) / 2;
            var barRect = new Rect(barX * pixelSize, barY * pixelSize,
                barSize.x * pixelSize, barSize.y * pixelSize);
            GUI.color = fuelGauge.EmptyColor;
            GUI.DrawTexture(barRect, Texture2D.whiteTexture);

            // Keep a final pixel while fuel remains; zero fuel leaves the bar completely empty.
            int filledPixels = Mathf.CeilToInt(Mathf.Clamp01(vehicle.FuelNormalized) * barSize.x);
            if (filledPixels > 0)
            {
                barRect.width = filledPixels * pixelSize;
                GUI.color = fuelGauge.FillColor;
                GUI.DrawTexture(barRect, Texture2D.whiteTexture);
            }

            GUI.EndGroup();
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }
    }
}
