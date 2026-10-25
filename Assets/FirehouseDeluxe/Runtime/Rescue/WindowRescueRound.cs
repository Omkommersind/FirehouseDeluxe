using System;
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

        private RescuePerson activePerson;
        private int previousWindow = -1;

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
            SpawnNextPerson();
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

        private void SpawnNextPerson()
        {
            // Skipping the previous index keeps all other windows equally likely.
            int next = UnityEngine.Random.Range(0, windows.Length - (previousWindow >= 0 ? 1 : 0));
            if (previousWindow >= 0 && next >= previousWindow)
                next++;
            previousWindow = next;

            activePerson = Instantiate(personPrefab, windows[next].SpawnPosition, Quaternion.identity, transform);
            activePerson.Initialize(fallDespawnY);
            activePerson.Finished += OnPersonFinished;
        }

        private void OnPersonFinished(RescuePerson person, bool rescued)
        {
            if (person != activePerson)
                return;

            Unsubscribe(person);
            activePerson = null;
            if (rescued)
                SavedCount++;

            if (IsComplete)
                Completed?.Invoke();
            else
                SpawnNextPerson();
        }

        private void Unsubscribe(RescuePerson person)
        {
            person.Finished -= OnPersonFinished;
        }

        private void OnDestroy()
        {
            if (activePerson != null)
                Unsubscribe(activePerson);
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
