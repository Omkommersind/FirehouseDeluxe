using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirehouseDeluxe.Rescue
{
    [DisallowMultipleComponent]
    public sealed class WindowRescueRound : MonoBehaviour
    {
        [SerializeField] private RescuePerson personPrefab;
        [SerializeField] private RescueWindow[] windows = new RescueWindow[4];
        [SerializeField, Min(1)] private int initialRescueTarget = 2;
        [SerializeField, Tooltip("Remove a falling person when their feet reach this world Y coordinate.")]
        private float fallDespawnY = -22.5f;

        private readonly Dictionary<RescuePerson, int> activePeople = new Dictionary<RescuePerson, int>();

        public int SavedCount { get; private set; }
        public int RescueTarget { get; private set; }
        public bool IsComplete => RescueTarget > 0 && SavedCount >= RescueTarget;
        public event Action Completed;

        private void Start()
        {
            if (!HasValidSetup())
            {
                Debug.LogError("Для миниигры нужны prefab персонажа и четыре разных окна.", this);
                enabled = false;
                return;
            }

            RescueTarget = initialRescueTarget;
            FillAvailablePlaces();
        }

        private bool HasValidSetup()
        {
            if (personPrefab == null || windows == null || windows.Length != 4)
                return false;
            for (int index = 0; index < windows.Length; index++)
            {
                if (windows[index] == null)
                    return false;
                for (int other = 0; other < index; other++)
                    if (windows[index] == windows[other])
                        return false;
            }
            return true;
        }

        private void FillAvailablePlaces(int excludedWindow = -1)
        {
            // Never spawn more passengers than still need rescuing.
            int desiredCount = Mathf.Min(2, RescueTarget - SavedCount);
            while (activePeople.Count < desiredCount)
                SpawnNextPerson(excludedWindow);
        }

        private void SpawnNextPerson(int excludedWindow)
        {
            var availableWindows = new List<int>(windows.Length);
            for (int index = 0; index < windows.Length; index++)
                if (index != excludedWindow && !activePeople.ContainsValue(index))
                    availableWindows.Add(index);

            int next = availableWindows[UnityEngine.Random.Range(0, availableWindows.Count)];

            RescuePerson person = Instantiate(personPrefab, windows[next].SpawnPosition, Quaternion.identity, transform);
            person.Initialize(fallDespawnY);
            activePeople.Add(person, next);
            person.Finished += OnPersonFinished;
        }

        private void OnPersonFinished(RescuePerson person, bool rescued)
        {
            if (!activePeople.TryGetValue(person, out int windowIndex))
                return;

            Unsubscribe(person);
            activePeople.Remove(person);
            if (rescued)
                SavedCount++;

            if (IsComplete)
                Completed?.Invoke();
            else
                FillAvailablePlaces(rescued ? -1 : windowIndex);
        }

        private void Unsubscribe(RescuePerson person)
        {
            person.Finished -= OnPersonFinished;
        }

        private void OnDestroy()
        {
            foreach (RescuePerson person in activePeople.Keys)
                if (person != null)
                    Unsubscribe(person);
            activePeople.Clear();
        }

        private void OnDrawGizmosSelected()
        {
            Color previousColor = Gizmos.color;
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.identity;
            Vector3 center = new Vector3(transform.position.x, fallDespawnY, 0f);
            Gizmos.DrawLine(center + Vector3.left * 40f, center + Vector3.right * 40f);
            Gizmos.color = previousColor;
            Gizmos.matrix = previousMatrix;
        }
    }
}
