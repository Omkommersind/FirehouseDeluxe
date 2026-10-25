using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace FirehouseDeluxe.Editor
{
    internal static class SceneCameraAnchorGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(SceneCameraAnchor anchor, GizmoType gizmoType)
        {
            var prefab = Resources.Load<GameObject>(PersistentGameCamera.ResourceName);
            if (prefab == null)
                return;
            var pixels = prefab.GetComponent<PixelPerfectCamera>();
            if (pixels == null)
                return;

            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Vector3 center = anchor.transform.position;
            center.z = 0f;
            Gizmos.matrix = Matrix4x4.TRS(center, anchor.transform.rotation, Vector3.one);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(
                (float)pixels.refResolutionX / pixels.assetsPPU,
                (float)pixels.refResolutionY / pixels.assetsPPU, 0f));
            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
        }
    }
}
