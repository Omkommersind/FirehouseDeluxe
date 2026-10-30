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
        [SerializeField, Min(0f), Tooltip("Full on/off cycles per second. Zero keeps the marker visible.")]
        private float blinkFrequency = 1f;

        private Tilemap points;
        private MissionPointMarker marker;
        private int currentStep;
        private bool launchPending;
        private bool missionActive;
        private double runStartedAt;
        private BackgroundMusic music;

        public IReadOnlyList<Vector3Int> Route => route;
        public int CompletedCount => currentStep;
        public double ElapsedSeconds => GetElapsedTime(Time.realtimeSinceStartupAsDouble);
        public bool IsComplete => currentStep >= route.Count;
        internal bool IsLastMission => currentStep == route.Count - 1;
        public float BlinkFrequency => blinkFrequency;

        private void Awake()
        {
            runStartedAt = Time.realtimeSinceStartupAsDouble;
            music = BackgroundMusic.CreateForRun();
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
            if (!launchPending)
                return;

            launchPending = false;
            WindowRescueMission.Launch(this);
        }

        internal void FinishCurrentMission(bool completed)
        {
            missionActive = false;
            if (completed)
                currentStep++;
            if (IsComplete)
            {
                marker.gameObject.SetActive(false);
                Debug.Log("Маршрут завершён: все задания выполнены.", this);
                return;
            }
            ShowCurrentPoint(true);
        }

        internal double GetElapsedTime(double completedAt) => completedAt - runStartedAt;

        internal void SetMusicDucked(bool ducked)
        {
            if (music != null)
                music.SetDucked(ducked);
        }

        internal void ReachCurrentPoint()
        {
            if (missionActive || IsComplete || !isActiveAndEnabled)
                return;

            missionActive = true;
            // Launch outside the trigger callback so suspending the map is safe for physics.
            launchPending = true;
            marker.gameObject.SetActive(false);
            Debug.Log($"Точка {currentStep + 1}/{route.Count}: спасение из окон.", this);
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
            if (marker != null && !IsComplete && !missionActive)
                marker.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            // Loading the ending or restarting destroys the map and ends this run's music.
            if (music != null)
                music.EndRun();
        }
    }
}
