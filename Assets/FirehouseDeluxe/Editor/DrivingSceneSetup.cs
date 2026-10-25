using FirehouseDeluxe.Vehicles;
using UnityEngine;

namespace FirehouseDeluxe.Editor
{
    internal static class DrivingSceneSetup
    {
        public const int PixelsPerUnit = 4;
        public const string VehiclePrefabPath = "Assets/FirehouseDeluxe/Prefabs/FireEngine.prefab";

        public static void CreateCameraAndHud(TopDownVehicle vehicle, Renderer mapBounds)
        {
            var cameraObject = new GameObject("Camera Anchor", typeof(SceneCameraAnchor));
            cameraObject.transform.position = vehicle.transform.position + new Vector3(0f, 0f, -10f);
            var followCamera = cameraObject.AddComponent<VehicleFollowCamera>();
            followCamera.Target = vehicle.transform;
            followCamera.MapBounds = mapBounds;
            new GameObject("Driving HUD").AddComponent<DrivingPrototypeHud>().Vehicle = vehicle;
        }
    }
}
