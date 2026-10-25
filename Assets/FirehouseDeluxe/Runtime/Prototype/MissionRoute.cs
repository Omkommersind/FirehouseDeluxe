using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace FirehouseDeluxe
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap), typeof(TilemapRenderer))]
    public sealed class MissionRoute : MonoBehaviour
    {
        [SerializeField] private List<Vector3Int> route = new List<Vector3Int>();
        [SerializeField, Range(0.1f, 1f)] private float opacity = 0.65f;
        [SerializeField, Min(0f)] private float rainbowSpeed = 0.35f;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.15f;
        [SerializeField, Min(0f)] private float pulseSpeed = 1f;

        private Tilemap points;
        private MissionPointMarker marker;
        private int currentStep;
        private bool advancePending;

        public IReadOnlyList<Vector3Int> Route => route;
        public int CompletedCount => currentStep;
        public bool IsComplete => currentStep >= route.Count;
        public float Opacity => opacity;
        public float RainbowSpeed => rainbowSpeed;
        public float PulseAmount => pulseAmount;
        public float PulseSpeed => pulseSpeed;

        private void Awake()
        {
            points = GetComponent<Tilemap>();
            var sourceRenderer = GetComponent<TilemapRenderer>();
            sourceRenderer.enabled = false;

            if (route.Count == 0)
            {
                Debug.LogWarning("Mission Points: маршрут пуст. Задай порядок точек в Inspector.", this);
                return;
            }

            // Never silently skip a missing point: the route must stay identical between runs.
            foreach (Vector3Int cell in route)
            {
                if (cell.z == 0 && points.HasTile(cell))
                    continue;
                Debug.LogError($"Mission Points: в клетке {cell} нет отметки. Исправь маршрут перед запуском.", this);
                return;
            }

            var markerObject = new GameObject("Active Mission Point");
            markerObject.transform.SetParent(transform, false);
            marker = markerObject.AddComponent<MissionPointMarker>();
            marker.Initialize(this, sourceRenderer);
            ShowCurrentPoint(false);
        }

        private void FixedUpdate()
        {
            if (!advancePending)
                return;

            advancePending = false;
            currentStep++;
            if (IsComplete)
            {
                marker.gameObject.SetActive(false);
                Debug.Log("Маршрут завершён: все точки посещены.", this);
                return;
            }
            ShowCurrentPoint(true);
        }

        internal void ReachCurrentPoint()
        {
            if (advancePending || IsComplete || !isActiveAndEnabled)
                return;

            advancePending = true;
            marker.gameObject.SetActive(false);
            Debug.Log($"Точка {currentStep + 1}/{route.Count} достигнута. TODO: запуск миниигры. Пока переходим к следующей точке.", this);
            // TODO: start the minigame here and advance only after its completion.
        }

        private void ShowCurrentPoint(bool requireClearArea)
        {
            marker.transform.localPosition = points.GetCellCenterLocal(route[currentStep]);
            Vector3 cellSize = points.layoutGrid.cellSize;
            marker.transform.localScale = new Vector3(cellSize.x / MissionPointMarker.WorldSize,
                cellSize.y / MissionPointMarker.WorldSize, 1f);
            marker.Prepare(requireClearArea);
            marker.gameObject.SetActive(true);
            Physics2D.SyncTransforms();
        }

        private void OnDisable()
        {
            if (marker != null)
                marker.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (marker != null && !IsComplete && !advancePending)
                marker.gameObject.SetActive(true);
        }
    }
}
