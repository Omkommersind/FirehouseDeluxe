using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirehouseDeluxe.Editor
{
    internal static class FuelStationEditor
    {
        private const string SpritePath = "Assets/FirehouseDeluxe/Art/Environment/gas_station.png";
        private const string PrefabPath = "Assets/FirehouseDeluxe/Prefabs/GasStation.prefab";
        private const string MaterialPath = "Assets/FirehouseDeluxe/Art/VehicleUnlit.mat";
        private const float PixelSize = 0.25f;

        [MenuItem("Firehouse Deluxe/Add Fuel Station")]
        private static void AddFuelStation()
        {
            GameObject prefab = GetOrCreatePrefab();
            if (prefab == null)
                return;

            Scene scene = SceneManager.GetActiveScene();
            var station = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Vector3 pivot = SceneView.lastActiveSceneView != null
                ? SceneView.lastActiveSceneView.pivot
                : Vector3.zero;
            Grid grid = FindRoadGrid(scene);
            station.transform.position = grid != null
                ? SnapToGridIntersection(grid, pivot)
                : new Vector3(
                    Mathf.Round(pivot.x / PixelSize) * PixelSize,
                    Mathf.Round(pivot.y / PixelSize) * PixelSize,
                    0f);

            Undo.RegisterCreatedObjectUndo(station, "Add fuel station");
            Selection.activeGameObject = station;
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Firehouse Deluxe/Add Fuel Station", true)]
        private static bool CanAddFuelStation()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && SceneManager.GetActiveScene().isLoaded;
        }

        private static GameObject GetOrCreatePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
                return prefab;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (sprite == null || material == null)
            {
                Debug.LogError("Gas station sprite or VehicleUnlit material is missing.");
                return null;
            }

            var station = new GameObject("Gas Station");
            try
            {
                var renderer = station.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sharedMaterial = material;
                renderer.sortingOrder = 0;

                var trigger = station.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = sprite.bounds.size;
                trigger.offset = sprite.bounds.center;
                station.AddComponent<FuelStation>();

                prefab = PrefabUtility.SaveAsPrefabAsset(station, PrefabPath);
                AssetDatabase.SaveAssets();
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(station);
            }
        }

        private static Grid FindRoadGrid(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Grid grid in root.GetComponentsInChildren<Grid>(true))
                    if (grid.gameObject.name == "Road Grid")
                        return grid;
            }
            return null;
        }

        private static Vector3 SnapToGridIntersection(Grid grid, Vector3 worldPosition)
        {
            Vector3 localPosition = grid.transform.InverseTransformPoint(worldPosition);
            Vector3 cellSize = grid.cellSize;
            localPosition.x = Mathf.Round(localPosition.x / cellSize.x) * cellSize.x;
            localPosition.y = Mathf.Round(localPosition.y / cellSize.y) * cellSize.y;
            localPosition.z = 0f;
            return grid.transform.TransformPoint(localPosition);
        }
    }
}
