using FirehouseDeluxe.Vehicles;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FirehouseDeluxe.Editor
{
    internal static class DrivingSceneSetup
    {
        public const int PixelsPerUnit = 4;
        public const string VehiclePrefabPath = "Assets/FirehouseDeluxe/Prefabs/FireEngine.prefab";

        public static void CreateCameraAndHud(TopDownVehicle vehicle, Renderer mapBounds)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = vehicle.transform.position + new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = mapBounds != null ? 22.5f : 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var followCamera = cameraObject.AddComponent<VehicleFollowCamera>();
            followCamera.Target = vehicle.transform;
            followCamera.MapBounds = mapBounds;
            if (mapBounds != null)
            {
                var pixelCamera = cameraObject.AddComponent<PixelPerfectCamera>();
                pixelCamera.assetsPPU = PixelsPerUnit;
                pixelCamera.refResolutionX = 320;
                pixelCamera.refResolutionY = 180;
                pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
                var pixelSettings = new SerializedObject(pixelCamera);
                pixelSettings.FindProperty("m_FilterMode").enumValueIndex =
                    (int)PixelPerfectCamera.PixelPerfectFilterMode.Point;
                pixelSettings.ApplyModifiedPropertiesWithoutUndo();
            }
            new GameObject("Driving HUD").AddComponent<DrivingPrototypeHud>().Vehicle = vehicle;
        }
    }
}
