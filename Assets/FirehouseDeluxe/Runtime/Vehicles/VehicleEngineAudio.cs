using UnityEngine;

namespace FirehouseDeluxe.Vehicles
{
    [DisallowMultipleComponent]
    public sealed class VehicleEngineAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip idleClip;
        [SerializeField, Tooltip("High engine sound while the vehicle applies throttle.")]
        private AudioClip movingClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [SerializeField, Min(0.01f), Tooltip("Seconds to crossfade between idle and moving sounds.")]
        private float transitionDuration = 0.12f;

        private IVehicleEngineState engineState;
        private AudioSource idleSource;
        private AudioSource movingSource;
        private float movingBlend;
        private float fadeIn;

        private void Awake()
        {
            engineState = GetComponent<IVehicleEngineState>();
            idleSource = CreateSource(idleClip);
            movingSource = CreateSource(movingClip);
        }

        private AudioSource CreateSource(AudioClip clip)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = 0f;
            source.clip = clip;
            return source;
        }

        private void OnEnable()
        {
            movingBlend = IsAccelerating ? 1f : 0f;
            fadeIn = 0f;

            // Keep both loops running: switching volume must not restart these short samples.
            double startTime = AudioSettings.dspTime + 0.05;
            if (idleSource.clip != null)
                idleSource.PlayScheduled(startTime);
            if (movingSource.clip != null)
                movingSource.PlayScheduled(startTime);
        }

        private bool IsAccelerating => engineState != null && engineState.IsAccelerating;

        private void LateUpdate()
        {
            float step = Time.deltaTime / Mathf.Max(0.01f, transitionDuration);
            movingBlend = Mathf.MoveTowards(movingBlend, IsAccelerating ? 1f : 0f, step);
            fadeIn = Mathf.MoveTowards(fadeIn, 1f, step);
            idleSource.volume = volume * fadeIn * (1f - movingBlend);
            movingSource.volume = volume * fadeIn * movingBlend;
        }

        private void OnDisable()
        {
            idleSource.Stop();
            movingSource.Stop();
            idleSource.volume = 0f;
            movingSource.volume = 0f;
        }

        private void OnDestroy()
        {
            // Sources belong to this component, including when it is removed on its own.
            if (idleSource != null)
                Destroy(idleSource);
            if (movingSource != null)
                Destroy(movingSource);
        }
    }
}
