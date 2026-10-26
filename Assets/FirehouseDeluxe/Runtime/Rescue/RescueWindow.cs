using UnityEngine;

namespace FirehouseDeluxe.Rescue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RescueWindow : MonoBehaviour
    {
        // The bottom centre places the waiting head on the window sill.
        public Vector3 SpawnPosition
        {
            get
            {
                var area = GetComponent<BoxCollider2D>();
                return transform.TransformPoint(area.offset + Vector2.down * area.size.y * 0.5f);
            }
        }

        private void OnDrawGizmos()
        {
            var area = GetComponent<BoxCollider2D>();
            if (area == null)
                return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(area.offset, new Vector3(area.size.x, area.size.y, 0f));
            Gizmos.matrix = previousMatrix;
            Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
            Gizmos.color = previousColor;
        }
    }
}
