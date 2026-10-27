using UnityEngine;

namespace FirehouseDeluxe
{
    public sealed class FuelGaugeSettings : ScriptableObject
    {
        [SerializeField] private Texture2D icon;
        [SerializeField, Tooltip("Offset from the camera's top-left corner, in game pixels.")]
        private Vector2Int position;
        [SerializeField, Tooltip("Full bar width and height, in game pixels.")]
        private Vector2Int barSize;
        [SerializeField, Min(0), Tooltip("Space between the icon and bar, in game pixels.")]
        private int gap;
        [SerializeField] private Color fillColor;
        [SerializeField] private Color emptyColor;

        public Texture2D Icon => icon;
        public Vector2Int Position => position;
        public Vector2Int BarSize => new Vector2Int(Mathf.Max(1, barSize.x), Mathf.Max(1, barSize.y));
        public int Gap => Mathf.Max(0, gap);
        public Color FillColor => fillColor;
        public Color EmptyColor => emptyColor;
    }
}
