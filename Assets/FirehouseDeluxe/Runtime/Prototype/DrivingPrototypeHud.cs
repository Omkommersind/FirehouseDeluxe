using FirehouseDeluxe.Vehicles;
using UnityEngine;

namespace FirehouseDeluxe
{
    public sealed class DrivingPrototypeHud : MonoBehaviour
    {
        [SerializeField] private TopDownVehicle vehicle;
        private GUIStyle headingStyle;
        private GUIStyle textStyle;

        public TopDownVehicle Vehicle { get => vehicle; set => vehicle = value; }

        private void OnGUI()
        {
            if (vehicle == null)
                return;

            if (textStyle == null)
            {
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 };
                textStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
                headingStyle = new GUIStyle(textStyle) { fontSize = 23, fontStyle = FontStyle.Bold };
            }

            Matrix4x4 previousMatrix = GUI.matrix;
            float scale = Mathf.Clamp(Screen.height / 720f, 0.6f, 2f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float width = Screen.width / scale;
            GUI.Label(new Rect(20f, 16f, width - 40f, 34f),
                $"{vehicle.SpeedKph:0} km/h   {(vehicle.ForwardSpeed < -0.1f ? "R" : "D")}", headingStyle);
            Vector3 position = vehicle.transform.position;
            GUI.Label(new Rect(20f, 52f, width - 40f, 28f),
                $"X {position.x:0.0} m   Y {position.y:0.0} m", textStyle);
            GUI.matrix = previousMatrix;
        }
    }
}
