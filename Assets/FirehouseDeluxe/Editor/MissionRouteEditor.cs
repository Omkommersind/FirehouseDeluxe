using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FirehouseDeluxe.Editor
{
    [CustomEditor(typeof(MissionRoute))]
    internal sealed class MissionRouteEditor : UnityEditor.Editor
    {
        private const string Folder = "Assets/FirehouseDeluxe/Missions";
        private const string PaletteName = "Mission Palette";
        private const string MaterialPath = "Assets/FirehouseDeluxe/Art/VehicleUnlit.mat";
        private SerializedProperty route;
        private ReorderableList routeList;
        private bool addingPoints;
        private Tool previousTool;

        private MissionRoute Mission => (MissionRoute)target;
        private Tilemap Points => Mission.GetComponent<Tilemap>();

        [MenuItem("Firehouse Deluxe/Mission Points")]
        private static void OpenMissionPoints()
        {
            Scene scene = SceneManager.GetActiveScene();
            Tilemap terrain = null;
            MissionRoute mission = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in root.GetComponentsInChildren<Tilemap>(true))
                    if (tilemap.name == "Road Terrain")
                        terrain = tilemap;
                if (mission == null)
                    mission = root.GetComponentInChildren<MissionRoute>(true);
            }
            if (terrain == null || terrain.layoutGrid == null)
            {
                Debug.LogError("Открой RoadPlayground: не найдена тайлмапа Road Terrain с сеткой.");
                return;
            }

            GameObject palette = GetOrCreatePalette();
            if (palette == null)
                return;

            if (mission == null)
            {
                var layer = new GameObject("Mission Points", typeof(Tilemap), typeof(TilemapRenderer));
                SceneManager.MoveGameObjectToScene(layer, scene);
                Undo.RegisterCreatedObjectUndo(layer, "Add mission points layer");
                Undo.SetTransformParent(layer.transform, terrain.transform.parent, "Parent mission points layer");
                layer.transform.localPosition = terrain.transform.localPosition;
                layer.transform.localRotation = terrain.transform.localRotation;
                layer.transform.localScale = terrain.transform.localScale;
                var renderer = layer.GetComponent<TilemapRenderer>();
                var terrainRenderer = terrain.GetComponent<TilemapRenderer>();
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                renderer.sortingLayerID = terrainRenderer.sortingLayerID;
                renderer.sortingOrder = terrainRenderer.sortingOrder + 1;
                mission = Undo.AddComponent<MissionRoute>(layer);
                // This layer stores destinations, not terrain; it intentionally has no collider.
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Selection.activeGameObject = mission.gameObject;
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            if (GridPaintingState.palettes.Contains(palette))
                GridPaintingState.palette = palette;
            GridPaintingState.scenePaintTarget = mission.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.in2DMode = true;
            Debug.Log("Mission Points: нарисуй места кистью из Mission Palette. Затем в Inspector включи «Добавлять в маршрут» и щёлкай по отметкам. Сохрани сцену Ctrl+S.", mission);
        }

        [MenuItem("Firehouse Deluxe/Mission Points", true)]
        private static bool CanOpenMissionPoints() => !EditorApplication.isPlayingOrWillChangePlaymode
            && SceneManager.GetActiveScene().isLoaded && PrefabStageUtility.GetCurrentPrefabStage() == null;

        private static GameObject GetOrCreatePalette()
        {
            string palettePath = Folder + "/" + PaletteName + ".prefab";
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
            if (palette != null)
                return palette;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Debug.LogError("Не найден VehicleUnlit.mat для маркеров точек.");
                return null;
            }
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/FirehouseDeluxe", "Missions");

            string spritePath = Folder + "/MissionPoint8x8.png";
            if (!File.Exists(spritePath))
            {
                var texture = new Texture2D(MissionPointMarker.Pixels, MissionPointMarker.Pixels,
                    TextureFormat.RGBA32, false);
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        texture.SetPixel(x, y, MissionPointMarker.MarkerColor);
                texture.Apply();
                File.WriteAllBytes(spritePath, texture.EncodeToPNG());
                DestroyImmediate(texture);
            }
            Sprite sprite = PixelSpriteImporter.Import(spritePath, DrivingSceneSetup.PixelsPerUnit, true);
            string tilePath = Folder + "/MissionPoint.asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            GridPaletteUtility.CreateNewPalette(Folder, PaletteName, GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Manual, new Vector3(MissionPointMarker.WorldSize,
                    MissionPointMarker.WorldSize, 1f), GridLayout.CellSwizzle.XYZ);
            GameObject contents = PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                Tilemap tilemap = contents.GetComponentInChildren<Tilemap>();
                tilemap.SetTile(Vector3Int.zero, tile);
                tilemap.GetComponent<TilemapRenderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(contents, palettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(palettePath);
        }

        private void OnEnable()
        {
            route = serializedObject.FindProperty("route");
            routeList = new ReorderableList(serializedObject, route, true, true, true, true);
            routeList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Порядок посещения (перетаскивай строки)");
            routeList.drawElementCallback = (rect, index, active, focused) =>
            {
                Vector3Int cell = route.GetArrayElementAtIndex(index).vector3IntValue;
                rect.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.LabelField(rect, $"{index + 1}.   {cell}" + (Points.HasTile(cell) ? "" : " — нет отметки!"));
            };
            routeList.onAddCallback = list =>
            {
                SetAddingPoints(true);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.HelpBox("Кисть Mission Palette отмечает возможные места. Для порядка включи кнопку ниже и щёлкай по отметкам в Scene. Повторные посещения разрешены. Esc завершает выбор.", MessageType.Info);
                bool shouldAdd = GUILayout.Toggle(addingPoints, "Добавлять в маршрут: " + (addingPoints ? "ВКЛ" : "ВЫКЛ"), "Button");
                if (shouldAdd != addingPoints)
                    SetAddingPoints(shouldAdd);
                routeList.DoLayoutList();
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Внешний вид активной точки", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blinkFrequency"),
                new GUIContent("Частота мигания (Гц)", "Полных циклов в секунду. 0 — постоянно виден."));
            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
                EditorGUILayout.LabelField($"Посещено: {Mission.CompletedCount}/{Mission.Route.Count}");
            else if (GUILayout.Button("Проверить маршрут"))
                ValidateRoute();
        }

        private void OnSceneGUI()
        {
            if (Application.isPlaying)
                return;

            var labels = new Dictionary<Vector3Int, string>();
            for (int i = 0; i < Mission.Route.Count; i++)
            {
                Vector3Int cell = Mission.Route[i];
                labels[cell] = labels.TryGetValue(cell, out string previous) ? previous + ", " + (i + 1) : (i + 1).ToString();
            }
            foreach (var label in labels)
                Handles.Label(Points.GetCellCenterWorld(label.Key), label.Value, EditorStyles.whiteBoldLabel);

            if (!addingPoints)
                return;
            Event current = Event.current;
            if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Escape)
            {
                SetAddingPoints(false);
                current.Use();
                Repaint();
                return;
            }
            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (current.type != EventType.MouseDown || current.button != 0 || current.alt)
                return;

            Ray ray = HandleUtility.GUIPointToWorldRay(current.mousePosition);
            var plane = new Plane(Points.transform.forward, Points.transform.position);
            if (!plane.Raycast(ray, out float distance))
                return;
            Vector3Int selectedCell = Points.WorldToCell(ray.GetPoint(distance));
            selectedCell.z = 0;
            if (Points.HasTile(selectedCell))
            {
                serializedObject.Update();
                int index = route.arraySize;
                route.arraySize++;
                route.GetArrayElementAtIndex(index).vector3IntValue = selectedCell;
                serializedObject.ApplyModifiedProperties();
                routeList.index = index;
                Repaint();
                SceneView.RepaintAll();
            }
            current.Use();
        }

        private void SetAddingPoints(bool value)
        {
            if (value == addingPoints)
                return;
            if (value)
            {
                // Leave the palette brush before consuming clicks, so route editing cannot paint tiles.
                ToolManager.RestorePreviousPersistentTool();
                previousTool = Tools.current;
                Tools.current = Tool.None;
            }
            else if (Tools.current == Tool.None)
            {
                Tools.current = previousTool;
            }
            addingPoints = value;
            SceneView.RepaintAll();
        }

        private void OnDisable() => SetAddingPoints(false);

        private void ValidateRoute()
        {
            bool valid = Mission.Route.Count > 0;
            if (!valid)
                Debug.LogWarning("Маршрут пуст: добавь точки щелчками в Scene.", Mission);
            for (int i = 0; i < Mission.Route.Count; i++)
            {
                Vector3Int cell = Mission.Route[i];
                if (cell.z == 0 && Points.HasTile(cell))
                    continue;
                valid = false;
                Debug.LogError($"Шаг {i + 1}: в клетке {cell} нет отметки. Верни её кистью или удали шаг из списка.", Mission);
            }
            if (valid)
                Debug.Log($"Маршрут готов: {Mission.Route.Count} посещений. Убедись, что все точки стоят на свободном проезде.", Mission);
        }
    }
}
