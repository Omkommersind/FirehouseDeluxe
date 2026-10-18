using System.Collections.Generic;
using System.IO;
using FirehouseDeluxe.Vehicles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace FirehouseDeluxe.Editor
{
    public static class RoadTilemapEditor
    {
        public const string ScenePath = "Assets/Scenes/RoadPlayground.unity";
        internal const string TileFolder = "Assets/FirehouseDeluxe/Tiles";
        private const string PaletteName = "Road Palette";
        private const string PalettePath = TileFolder + "/" + PaletteName + ".prefab";
        private const string MaterialPath = "Assets/FirehouseDeluxe/Art/VehicleUnlit.mat";
        private const int TilePixels = 8;
        private const int Width = 64;
        private const int Height = 48;
        private const float CellSize = (float)TilePixels / DrivingSceneSetup.PixelsPerUnit;

        [MenuItem("Firehouse Deluxe/Tile Editor")]
        public static void Open()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DrivingSceneSetup.VehiclePrefabPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (prefab == null || material == null)
            {
                Debug.LogError("Не найдены FireEngine.prefab или VehicleUnlit.mat в Assets/FirehouseDeluxe. Восстанови эти ассеты перед открытием редактора.");
                return;
            }

            Directory.CreateDirectory(TileFolder);
            AssetDatabase.Refresh();
            Sprite sprite = GetTileSprite();
            Tile obstacle = GetTile("Obstacle", sprite, Color.black);
            Tile ground = GetTile("Ground", sprite, new Color(0.25f, 0.45f, 0.25f));
            GameObject palette = GetPalette(obstacle, ground, material);
            SolidTileRules.ApplyToProjectTiles();

            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath);
            else
                CreateScene(prefab, material, ground);

            SolidTileRules.RefreshLoadedTilemaps();

            var terrain = GameObject.Find("Road Terrain");
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            if (GridPaintingState.palettes.Contains(palette))
                GridPaintingState.palette = palette;
            if (terrain != null)
            {
                Selection.activeGameObject = terrain;
                GridPaintingState.scenePaintTarget = terrain;
                var sceneView = SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    sceneView.in2DMode = true;
                    sceneView.Frame(terrain.GetComponent<TilemapRenderer>().bounds, true);
                }
            }
        }

        [MenuItem("Firehouse Deluxe/Tile Editor", true)]
        private static bool CanOpen() => !EditorApplication.isPlayingOrWillChangePlaymode;

        private static Sprite GetTileSprite()
        {
            string path = TileFolder + "/Tile8x8.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(TilePixels, TilePixels, TextureFormat.RGBA32, false);
                var pixels = new Color32[TilePixels * TilePixels];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }
            return PixelSpriteImporter.Import(path, DrivingSceneSetup.PixelsPerUnit, false);
        }

        private static Tile GetTile(string name, Sprite sprite, Color color)
        {
            string path = TileFolder + "/" + name + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile != null)
                return tile;
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = sprite;
            tile.color = color;
            tile.colliderType = Tile.ColliderType.Sprite;
            AssetDatabase.CreateAsset(tile, path);
            return tile;
        }

        private static GameObject GetPalette(Tile obstacle, Tile ground, Material material)
        {
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
            if (palette != null)
                return palette;
            GridPaletteUtility.CreateNewPalette(TileFolder, PaletteName, GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Manual, new Vector3(CellSize, CellSize, 1f), GridLayout.CellSwizzle.XYZ);
            GameObject contents = PrefabUtility.LoadPrefabContents(PalettePath);
            try
            {
                var tiles = contents.GetComponentInChildren<Tilemap>();
                tiles.SetTile(Vector3Int.zero, obstacle);
                tiles.SetTile(Vector3Int.right, ground);
                tiles.GetComponent<TilemapRenderer>().sharedMaterial = material;
                PrefabUtility.SaveAsPrefabAsset(contents, PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath);
        }

        private static void CreateScene(GameObject truckPrefab, Material material, Tile ground)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var gridObject = new GameObject("Road Grid");
            var grid = gridObject.AddComponent<Grid>();
            grid.cellSize = new Vector3(CellSize, CellSize, 1f);
            // A normal grid with no image border or pixel offset.
            gridObject.transform.position = new Vector3(-Width * CellSize * 0.5f, -Height * CellSize * 0.5f);
            var terrain = new GameObject("Road Terrain");
            terrain.transform.SetParent(grid.transform, false);
            var tilemap = terrain.AddComponent<Tilemap>();
            var renderer = terrain.AddComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = -100;

            var tiles = new TileBase[Width * Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    tiles[y * Width + x] = IsStarterRoad(x, y) ? null : ground;
            tilemap.SetTilesBlock(new BoundsInt(0, 0, 0, Width, Height, 1), tiles);
            tilemap.CompressBounds();

            PhysicsMaterial2D boundaryMaterial = GetBoundaryMaterial();
            terrain.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var composite = terrain.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
            composite.sharedMaterial = boundaryMaterial;
            var collider = terrain.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;
            collider.extrusionFactor = 0.001f;
            collider.ProcessTilemapChanges();

            var truck = (GameObject)PrefabUtility.InstantiatePrefab(truckPrefab, scene);
            truck.name = "FireEngine";
            truck.transform.SetPositionAndRotation(tilemap.GetCellCenterWorld(new Vector3Int(20, 9, 0)),
                Quaternion.Euler(0f, 0f, -90f));
            DrivingSceneSetup.CreateCameraAndHud(truck.GetComponent<TopDownVehicle>(), renderer);

            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var existing in EditorBuildSettings.scenes)
                if (existing.path != ScenePath)
                    scenes.Add(existing);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Тайловая карта готова. Любой нарисованный тайл — препятствие; пустая клетка — дорога. Стирай ластиком, чтобы сделать проезд. Сохраняй сцену Ctrl+S.");
        }

        private static bool IsStarterRoad(int x, int y)
        {
            // A four-cell-wide loop and crossing leave room to try the truck before painting a level.
            bool horizontal = x >= 8 && x < Width - 8
                && ((y >= 8 && y < 12) || (y >= Height - 12 && y < Height - 8));
            bool vertical = y >= 8 && y < Height - 8
                && ((x >= 8 && x < 12) || (x >= Width - 12 && x < Width - 8));
            bool crossing = x >= 8 && x < Width - 8 && y >= Height / 2 - 2 && y < Height / 2 + 2;
            return horizontal || vertical || crossing;
        }

        private static PhysicsMaterial2D GetBoundaryMaterial()
        {
            string path = TileFolder + "/RoadBoundary.physicsMaterial2D";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (material == null)
            {
                material = new PhysicsMaterial2D("RoadBoundary") { friction = 0f, bounciness = 0f };
                AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }

    }
}
