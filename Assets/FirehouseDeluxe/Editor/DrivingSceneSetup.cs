using FirehouseDeluxe.Vehicles;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FirehouseDeluxe.Editor
{
    internal static class DrivingSceneSetup
    {
        public const int PixelsPerUnit = 4;
        public const string VehiclePrefabPath = "Assets/FirehouseDeluxe/Prefabs/FireEngine.prefab";
        public const string HudCanvasPrefabPath = "Assets/FirehouseDeluxe/Prefabs/DrivingHudCanvas.prefab";

        public static void CreateCameraAndHud(TopDownVehicle vehicle, Renderer mapBounds)
        {
            var cameraObject = new GameObject("Camera Anchor", typeof(SceneCameraAnchor));
            cameraObject.transform.position = vehicle.transform.position + new Vector3(0f, 0f, -10f);
            var followCamera = cameraObject.AddComponent<VehicleFollowCamera>();
            followCamera.Target = vehicle.transform;
            followCamera.MapBounds = mapBounds;
            var hud = new GameObject("Driving HUD").AddComponent<DrivingPrototypeHud>();
            hud.Vehicle = vehicle;
            AttachTimer(hud);
        }

        public static void AttachTimer(DrivingPrototypeHud hud)
        {
            var hudData = new SerializedObject(hud);
            if (hudData.FindProperty("timer").objectReferenceValue != null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudCanvasPrefabPath);
            if (prefab == null)
                throw new System.InvalidOperationException("DrivingHudCanvas prefab is missing.");
            var canvas = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hud.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(canvas, "Create driving HUD canvas");
            Undo.SetTransformParent(canvas.transform, hud.transform, "Parent driving HUD canvas");
            canvas.transform.localScale = Vector3.one;
            Text timer = canvas.GetComponentInChildren<Text>();
            hudData.FindProperty("timer").objectReferenceValue = timer;
            hudData.FindProperty("timerPanel").objectReferenceValue = timer.rectTransform.parent;
            hudData.ApplyModifiedProperties();
        }
    }
}
