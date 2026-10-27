using System;
using System.Collections;
using System.Collections.Generic;
using FirehouseDeluxe.Rescue;
using FirehouseDeluxe.Vehicles;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirehouseDeluxe
{
    // Keeps the live map intact while a separate scene runs the rescue round.
    [DisallowMultipleComponent]
    public sealed class WindowRescueMission : MonoBehaviour
    {
        private const string ScenePath = "Assets/Scenes/WindowRescue.unity";
        private WindowRescueRound round;
        private bool completed;

        internal static void Launch(MissionRoute route)
        {
            if (!Application.CanStreamedLevelBeLoaded(ScenePath)
                || SceneManager.GetSceneByPath(ScenePath).isLoaded)
            {
                Debug.LogError("Не удалось запустить WindowRescue: добавь сцену в Build Profiles и закрой её перед запуском карты.", route);
                route.FinishCurrentMission(false);
                return;
            }

            var runnerObject = new GameObject("Window Rescue Mission");
            SceneManager.MoveGameObjectToScene(runnerObject, route.gameObject.scene);
            var runner = runnerObject.AddComponent<WindowRescueMission>();
            runner.StartCoroutine(runner.Run(route));
        }

        private IEnumerator Run(MissionRoute route)
        {
            Scene road = gameObject.scene;
            AsyncOperation loading = BeginLoading(route);
            if (loading == null)
            {
                route.FinishCurrentMission(false);
                Destroy(gameObject);
                yield break;
            }

            Camera gameCamera = Camera.main;
            Vector3 cameraPosition = gameCamera != null ? gameCamera.transform.position : Vector3.zero;
            Quaternion cameraRotation = gameCamera != null ? gameCamera.transform.rotation : Quaternion.identity;
            SceneCameraAnchor roadAnchor = FindInScene<SceneCameraAnchor>(road);
            var suspendedRoots = new List<GameObject>();
            foreach (GameObject root in road.GetRootGameObjects())
            {
                if (root == gameObject || !root.activeSelf)
                    continue;

                foreach (TopDownVehicle vehicle in root.GetComponentsInChildren<TopDownVehicle>())
                    vehicle.Stop();
                suspendedRoots.Add(root);
                root.SetActive(false);
            }

            yield return loading;
            Scene rescue = SceneManager.GetSceneByPath(ScenePath);
            if (rescue.IsValid() && rescue.isLoaded)
            {
                SceneManager.SetActiveScene(rescue);
                round = FindInScene<WindowRescueRound>(rescue);
                if (round != null && round.isActiveAndEnabled)
                {
                    round.Completed += OnRoundCompleted;
                    // Start validates the round on its first frame; an invalid setup disables it.
                    while (round != null && round.isActiveAndEnabled && !completed)
                        yield return null;
                    if (round != null)
                        round.Completed -= OnRoundCompleted;
                }

                if (!completed)
                    Debug.LogError("WindowRescue не завершена: проверь активный WindowRescueRound, prefab персонажа и ссылки на окна.", route);
                yield return SceneManager.UnloadSceneAsync(rescue);
            }
            else
            {
                Debug.LogError("Сцена WindowRescue не загрузилась. Текущая точка остаётся невыполненной.", route);
            }

            SceneManager.SetActiveScene(road);
            foreach (GameObject root in suspendedRoots)
                if (root != null)
                    root.SetActive(true);

            if (roadAnchor != null)
                PersistentGameCamera.UseAnchor(roadAnchor.transform);
            if (gameCamera != null)
                gameCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);

            route.FinishCurrentMission(completed);
            Destroy(gameObject);
        }

        private static AsyncOperation BeginLoading(MissionRoute route)
        {
            try
            {
                return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Additive);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Не удалось загрузить WindowRescue: {exception.Message}", route);
                return null;
            }
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>();
                if (component != null)
                    return component;
            }
            return null;
        }

        private void OnRoundCompleted() => completed = true;

        private void OnDestroy()
        {
            if (round != null)
                round.Completed -= OnRoundCompleted;
        }
    }
}
