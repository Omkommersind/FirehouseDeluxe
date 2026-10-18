using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FirehouseDeluxe.Editor
{
    internal static class SolidTileRules
    {
        public static void ApplyToProjectTiles()
        {
            if (!AssetDatabase.IsValidFolder(RoadTilemapEditor.TileFolder))
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:Tile", new[] { RoadTilemapEditor.TileFolder }))
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(AssetDatabase.GUIDToAssetPath(guid));
                if (tile == null || tile.colliderType == Tile.ColliderType.Sprite)
                    continue;

                // Every painted tile is solid. Its sprite physics shape preserves transparent gaps.
                tile.colliderType = Tile.ColliderType.Sprite;
                EditorUtility.SetDirty(tile);
                AssetDatabase.SaveAssetIfDirty(tile);
            }
        }

        public static void RefreshLoadedTilemaps()
        {
            foreach (var tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (EditorUtility.IsPersistent(tilemap) || !tilemap.gameObject.scene.isLoaded
                    || EditorSceneManager.IsPreviewScene(tilemap.gameObject.scene))
                    continue;

                tilemap.RefreshAllTiles();
                var collider = tilemap.GetComponent<TilemapCollider2D>();
                if (collider != null && collider.hasTilemapChanges)
                    collider.ProcessTilemapChanges();
            }
        }
    }

    // Apply the same rule to tiles dragged into the palette later, without an extra user step.
    internal sealed class SolidTilePostprocessor : AssetPostprocessor
    {
        private static bool refreshQueued;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved,
            string[] movedFrom, bool didDomainReload)
        {
            if (!didDomainReload && !ContainsTileAsset(imported) && !ContainsTileAsset(moved))
                return;
            if (refreshQueued)
                return;

            refreshQueued = true;
            EditorApplication.delayCall += ApplyRules;
        }

        private static bool ContainsTileAsset(string[] paths)
        {
            foreach (string path in paths)
                if (path.StartsWith(RoadTilemapEditor.TileFolder + "/", StringComparison.Ordinal)
                    && path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        private static void ApplyRules()
        {
            refreshQueued = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            SolidTileRules.ApplyToProjectTiles();
            SolidTileRules.RefreshLoadedTilemaps();
        }
    }
}
